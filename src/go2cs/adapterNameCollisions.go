// adapterNameCollisions.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"encoding/hex"
	"os"
	"strings"
)

// A pointer-interface adapter class is named `[<pkg>_]<structSimple>ж<ifaceSimple>` — the STRUCT
// side is package-qualified when foreign (two same-named foreign structs adapting to one interface
// would otherwise compose one class), but the INTERFACE side composes from its bare last-dot
// segment. That is only unambiguous while a given struct adapts to at most one interface of each
// simple name. compress/flate breaks it: its own `Reader` interface and `io.Reader` share a simple
// name, and its tests hand a *bufio.Reader and a *bytes.Reader to NewReader, which casts to both —
// so `bufio_ReaderжReader` and `bytes_ReaderжReader` were each composed TWICE (CS0102 + CS0111 ×6
// + CS8646 per pair). The whole 302-package production corpus has ZERO such collisions; it takes a
// test closure's extra casts to produce one, which is why this survived the Phase-3 milestone.
//
// The rule is COLLISION-CONDITIONAL: within a group of records that compose the same adapter name,
// every FOREIGN interface takes a package qualifier (`bufio_Readerжio_Reader`) and a LOCAL one stays
// bare. Unconditional qualification was measured and rejected — 3,688 construction sites across 644
// adapter names would churn. Because collisions do not occur in production, this is byte-neutral
// there by construction.
//
// The converter and go2cs-gen must agree on every name, and neither may guess: the authority is the
// FINAL set of `[assembly: GoImplement<…>(Pointer = true)]` lines, which is exactly what the
// generator reads and what writePackageInfoFile emits after its alias-covered skip and its
// interface-inheritance prune. Those lines are not known until the whole package has been visited,
// long after the cast sites were rendered — so a cast emits a deferred marker (mirroring
// dynamicTypeOperations' DYNTYPE marker) that resolveAdapterNameMarkers rewrites once the records
// are final.
const (
	adapterNameMarkerPrefix = "«ADAPTER:"
	adapterNameMarkerSuffix = ":ADAPTER»"
	adapterNameMarkerSep    = "|"
)

// emittedPointerAdapterPairs holds the (structRef, interfaceRef) pairs of the pointer-form
// GoImplement records writePackageInfoFile actually emitted, in the spelling it wrote them. Reset
// per package-info write; consumed by resolveAdapterNameMarkers. Guarded by packageLock like the
// registries it is derived from.
var emittedPointerAdapterPairs [][2]string

// testAdapterResolveNames accumulates every -tests emission PATH across both variants, so the
// deferred adapter names can be resolved in one pass once the merged metadata file is final.
var testAdapterResolveNames []string

// recordEmittedPointerAdapterPairs captures the pointer-form GoImplement pairs from the FINAL
// package-info lines — the exact text go2cs-gen will read. Replaces (not appends to) the previous
// set: writePackageInfoFile is called once per emitted info file (production, then each -tests
// variant), and each variant's cast sites resolve against its OWN records.
func recordEmittedPointerAdapterPairs(lines []string) {
	pairs := [][2]string{}

	for _, line := range lines {
		inner, ok := strings.CutPrefix(strings.TrimSpace(line), "[assembly: GoImplement<")

		if !ok || !strings.HasSuffix(inner, ">(Pointer = true)]") {
			continue
		}

		inner = strings.TrimSuffix(inner, ">(Pointer = true)]")

		// Split on the record's separating comma. A GENERIC struct reference carries its own
		// commas inside `<…>` (`nistCurve<ж<P224Point>>`), so track angle-bracket depth rather
		// than taking the first comma.
		depth := 0
		split := -1

		for i, r := range inner {
			switch r {
			case '<':
				depth++
			case '>':
				depth--
			case ',':
				if depth == 0 && split < 0 {
					split = i
				}
			}
		}

		if split < 0 {
			continue
		}

		pairs = append(pairs, [2]string{strings.TrimSpace(inner[:split]), strings.TrimSpace(inner[split+1:])})
	}

	packageLock.Lock()
	emittedPointerAdapterPairs = pairs
	packageLock.Unlock()
}

// captureAdapterPairsFromInfoFile re-reads a written package-info file and captures its pointer
// records as the authoritative pair set. Used by the -tests flow, whose variants reach the metadata
// file through more than one writer.
func captureAdapterPairsFromInfoFile(packageInfoFileName string) {
	contentBytes, err := os.ReadFile(packageInfoFileName)

	if err != nil {
		showWarning("Failed to read \"%s\" for adapter-name resolution: %s", packageInfoFileName, err)
		return
	}

	recordEmittedPointerAdapterPairs(strings.Split(string(contentBytes), "\n"))
}

// adapterNameMarker returns the deferred marker for a pointer-adapter reference. The payload is
// HEX-ENCODED for the same reason the DYNTYPE payload is: a rendered type name flows through string
// transformation passes (convertToCSTypeName, getAliasedTypeName, …) before reaching the output
// file, and raw type text would be corrupted in transit. Equal pairs yield an identical marker, so
// string comparisons on rendered references behave exactly as pair comparisons.
func adapterNameMarker(structBase string, interfaceTypeName string) string {
	payload := hex.EncodeToString([]byte(structBase + adapterNameMarkerSep + interfaceTypeName))
	return adapterNameMarkerPrefix + payload + adapterNameMarkerSuffix
}

// adapterNameMarkerPair decodes a marker payload back to its (structBase, interfaceTypeName) pair.
func adapterNameMarkerPair(payload string) (string, string, bool) {
	decoded, err := hex.DecodeString(payload)

	if err != nil {
		return "", "", false
	}

	structBase, interfaceTypeName, ok := strings.Cut(string(decoded), adapterNameMarkerSep)

	if !ok {
		return "", "", false
	}

	return structBase, interfaceTypeName, true
}

// adapterInterfaceSimpleName reduces an interface reference to the bare last-dot segment the
// adapter name composes from — the same reduction adapterTypeRef applied inline before markers,
// and the same one the generator's GetSimpleName performs.
func adapterInterfaceSimpleName(interfaceTypeName string) string {
	simple := interfaceTypeName

	if idx := strings.LastIndex(simple, "."); idx >= 0 {
		simple = simple[idx+1:]
	}

	return stripSanitizationMarkers(simple)
}

// adapterInterfacePackagePrefix returns the disambiguating prefix ("io_") for a FOREIGN interface
// reference, derived from the package class segment that precedes the type ("io_package.Reader").
// Returns "" for a LOCAL interface (a bare, undotted name), which never takes a qualifier: at most
// one member of a colliding group can be local, so leaving it bare is always unambiguous and keeps
// the Go-like short form for the package's own interface. Mirrors the generator's
// ForeignPackagePrefix on the struct side, and the converter's own `getSanitizedIdentifier(pkg) +
// "_"` composition at foreign-struct cast sites.
func adapterInterfacePackagePrefix(interfaceTypeName string) string {
	idx := strings.LastIndex(interfaceTypeName, ".")

	if idx < 0 {
		return ""
	}

	return packageClassPrefix(interfaceTypeName[:idx])
}

// packageClassPrefix reduces a type reference's qualifier to the generator's flattened package
// prefix: "go.compress.flate_package" → "flate_". A qualifier that is not a package class (an
// enclosing type, say) yields "" — no qualification is possible or wanted there.
func packageClassPrefix(qualifier string) string {
	if dot := strings.LastIndex(qualifier, "."); dot >= 0 {
		qualifier = qualifier[dot+1:]
	}

	if !strings.HasSuffix(qualifier, PackageSuffix) {
		return ""
	}

	return strings.TrimSuffix(qualifier, PackageSuffix) + "_"
}

// adapterNameCollisionSet returns the composed adapter names that MORE THAN ONE distinct interface
// maps to, computed over the final emitted pointer records. Grouping keys on the composed name —
// struct side included — because two records sharing an interface simple name do NOT collide when
// their struct sides differ: compress/gzip records both `<Reader, io.Reader>` and
// `<bufio.Reader, flate.Reader>`, which compose `ReaderжReader` and `bufio_ReaderжReader`. Keying on
// the interface simple name alone would call that a collision and rename a validated package's
// adapters for nothing.
func adapterNameCollisionSet(pairs [][2]string) map[string]bool {
	groups := map[string]map[string]bool{}

	for _, pair := range pairs {
		key := adapterGroupKey(pair[0], pair[1])

		if groups[key] == nil {
			groups[key] = map[string]bool{}
		}

		groups[key][pair[1]] = true
	}

	colliding := map[string]bool{}

	for name, interfaces := range groups {
		if len(interfaces) > 1 {
			colliding[name] = true
		}
	}

	return colliding
}

// adapterGroupKey is the collision-grouping key for a pair. ONLY a key — never emitted. The same
// struct reaches this code in three spellings that must all group together: a GoImplement record's
// package-class form ("bufio_package.Reader"), a cast site's flattened foreign form
// ("bufio_Reader"), and a cast site's NAMESPACE-qualified form ("os.File", naming an adapter class
// that lives in another assembly). All normalize to the generator's "<pkg>_<Simple>", which is what
// AdapterStructKey computes from the symbol on the other side.
func adapterGroupKey(structBase string, interfaceTypeName string) string {
	return adapterStructKey(structBase) + PointerPrefix + adapterInterfaceSimpleName(interfaceTypeName)
}

// adapterStructKey normalizes any of those struct spellings to "<pkg>_<Simple>".
func adapterStructKey(structBase string) string {
	base := structBase

	if idx := strings.Index(base, "<"); idx >= 0 {
		base = base[:idx]
	}

	idx := strings.LastIndex(base, ".")

	if idx < 0 {
		return stripSanitizationMarkers(base)
	}

	qualifier := base[:idx]

	if dot := strings.LastIndex(qualifier, "."); dot >= 0 {
		qualifier = qualifier[dot+1:]
	}

	return strings.TrimSuffix(qualifier, PackageSuffix) + "_" + stripSanitizationMarkers(base[idx+1:])
}

// adapterResolvedName renders the final adapter class REFERENCE for a pair. The struct side is
// emitted VERBATIM — it is not merely a name fragment but the reference's path, and rewriting it
// broke `new os.FileжWriter(f)` (namespace `os`, adapter class `FileжWriter`, generated in os's own
// assembly) into a bare `FileжWriter` that resolves nowhere, CS0246. Only the interface side is
// ever rewritten, and only for a colliding group.
func adapterResolvedName(structBase string, interfaceTypeName string, colliding map[string]bool) string {
	ifaceSimple := adapterInterfaceSimpleName(interfaceTypeName)

	if !colliding[adapterGroupKey(structBase, interfaceTypeName)] {
		return structBase + PointerPrefix + ifaceSimple
	}

	// The LOCAL member of a colliding group keeps the bare name (prefix is empty for it).
	return structBase + PointerPrefix + adapterInterfacePackagePrefix(interfaceTypeName) + ifaceSimple
}

// resolveAdapterNameMarkers rewrites deferred pointer-adapter markers in the given output files
// once the package's GoImplement records are final. Called after writePackageInfoFile (whose
// alias-covered skip and interface-inheritance prune decide the authoritative set), mirroring
// resolveDynamicTypeMarkers' post-barrier text pass. A marker whose pair never reached a record —
// possible when a cast is emitted for a pair the prune later drops — still resolves, to the
// unqualified name it would have had, so no marker can survive into the output.
func resolveAdapterNameMarkers(outputFileNames []string) {
	packageLock.Lock()
	pairs := make([][2]string, len(emittedPointerAdapterPairs))
	copy(pairs, emittedPointerAdapterPairs)
	packageLock.Unlock()

	colliding := adapterNameCollisionSet(pairs)

	for _, fileName := range outputFileNames {
		contentBytes, err := os.ReadFile(fileName)

		if err != nil {
			continue
		}

		content := string(contentBytes)

		if !strings.Contains(content, adapterNameMarkerPrefix) {
			continue
		}

		for {
			start := strings.Index(content, adapterNameMarkerPrefix)

			if start == -1 {
				break
			}

			end := strings.Index(content[start:], adapterNameMarkerSuffix)

			if end == -1 {
				break
			}

			end += start
			marker := content[start : end+len(adapterNameMarkerSuffix)]
			structBase, interfaceTypeName, ok := adapterNameMarkerPair(content[start+len(adapterNameMarkerPrefix) : end])

			if !ok {
				showWarning("Unresolved adapter-name marker in \"%s\"", fileName)
				content = strings.Replace(content, marker, "", 1)
				continue
			}

			content = strings.ReplaceAll(content, marker, adapterResolvedName(structBase, interfaceTypeName, colliding))
		}

		if err := os.WriteFile(fileName, []byte(content), 0644); err != nil {
			showWarning("Failed to resolve adapter-name markers in \"%s\": %s", fileName, err)
		}
	}
}
