// deferredMarkerOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// This file owns the shared mechanism for DEFERRED MARKERS: placeholders the converter writes into
// an output file while it still lacks the information to write the final text, then rewrites once
// that information exists.
//
// The converter visits a package's files CONCURRENTLY, so while file A is being emitted the name a
// reference needs may not have been decided yet — it may be lifted in file B, or depend on the
// package-wide record set that is only final after every file is written. Rather than serialize the
// visit or guess, the emitter writes a self-describing sentinel (`«KIND:payload:KIND»`) and comes
// back for it after the barrier. Two passes use this today:
//
//	dynamicTypeOperations.go   «DYNTYPE:…»  — anonymous struct/interface types lifted in a sibling file
//	adapterNameCollisions.go   «ADAPTER:…»  — pointer-adapter class names that depend on the final records
//
// What lives here is only the file-walking scaffold those two share. Each pass keeps its own marker
// spelling, payload encoding and resolution rules, because those are what actually differ.
package main

import (
	"os"
	"strings"
)

// rewriteDeferredMarkers replaces every `prefix…suffix` marker in the given output files with the
// text resolve returns for its payload, then writes each changed file back.
//
// description names the marker kind for the write-failure warning ("dynamic type", "adapter-name").
//
// resolve receives the file name (so it can report WHERE an unresolvable marker was found) and the
// raw payload between the sentinels. It returns the replacement text plus replaceEvery: true
// substitutes every identical marker in the file in one step, which is what a resolver wants when
// the answer is the same everywhere; false substitutes only this one occurrence, which lets a
// resolver that warns per marker report each one rather than collapsing them into a single warning.
// Either way the final content is the same — only the diagnostics differ.
//
// Files are handled independently and best-effort: an unreadable file is skipped rather than
// failing the whole conversion, since a marker left in one file cannot corrupt another.
func rewriteDeferredMarkers(outputFileNames []string, description, prefix, suffix string, resolve func(fileName, payload string) (replacement string, replaceEvery bool)) {
	for _, fileName := range outputFileNames {
		contentBytes, err := os.ReadFile(fileName)

		if err != nil {
			continue
		}

		content := string(contentBytes)

		// Most files contain no marker at all, so this one scan avoids the rewrite loop entirely
		// for them.
		if !strings.Contains(content, prefix) {
			continue
		}

		for {
			start := strings.Index(content, prefix)

			if start == -1 {
				break
			}

			// Search for the closing sentinel FROM the opening one, so a suffix belonging to an
			// earlier, already-rewritten marker cannot be mistaken for this marker's end.
			end := strings.Index(content[start:], suffix)

			if end == -1 {
				break
			}

			end += start
			marker := content[start : end+len(suffix)]
			replacement, replaceEvery := resolve(fileName, content[start+len(prefix):end])

			if replaceEvery {
				content = strings.ReplaceAll(content, marker, replacement)
			} else {
				content = strings.Replace(content, marker, replacement, 1)
			}
		}

		if err := os.WriteFile(fileName, []byte(content), 0644); err != nil {
			showWarning("Failed to resolve %s markers in \"%s\": %s", description, fileName, err)
		}
	}
}
