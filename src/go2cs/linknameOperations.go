// linknameOperations.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/ast"
	"go/types"
	"strings"
)

// linknameHandles is the set of THIS package's symbol names that carry a definition-side
// one-argument `//go:linkname <name>` directive — Go 1.23's opt-in that AUTHORIZES other packages to
// linkname-PULL <name> (runtime/linkname.go lists overflowError, divideError, write, doInit, … each
// "used in" a named consumer). The authorization is puller-AGNOSTIC (it opens the symbol to linkname
// generally, it does not name a specific puller), so the faithful C# emission is `public` — a
// puller in a SEPARATE assembly can then reach the symbol through its forwarding property. Reset per
// package alongside projectImports; populated by collectLinknameHandles.
var linknameHandles HashSet[string]

// conversionGraph is the -stdlib convert-set dependency graph (nil for a single-package or -tests
// conversion, where no cross-package cycle can arise from the one package under conversion). Set once
// the graph is built; read by linknamePullWouldCycle. currentPackagePath is the import path of the
// package currently being converted (set per package in resetPackageState).
var conversionGraph *DependencyGraph
var currentPackagePath string

// linknamePullWouldCycle reports whether forwarding a var pull to targetPath would create a CYCLIC C#
// project reference — targetPath is (or transitively depends on) the current package. Go's linkname is
// link-time and cycle-free; a C# project reference is compile-time and cannot be circular. A downward
// pull (math/bits → runtime) is safe; the reverse (runtime → internal/syscall/windows.CanUseLongPaths)
// is not, and keeps its null-field form. Without the graph (single-package / -tests) a lone package
// cannot form a cross-package cycle, so only a self-reference is rejected.
func linknamePullWouldCycle(targetPath string) bool {
	if targetPath == currentPackagePath {
		return true
	}

	if conversionGraph == nil {
		return false
	}

	return conversionGraph.DependsOn(targetPath, currentPackagePath)
}

// collectLinknameHandles scans every file's comments for a definition-side one-argument
// `//go:linkname <name>` handle and records <name> in linknameHandles. A TWO-argument form
// (`//go:linkname local pkgpath.remote`) is a PULL, not a handle, and is skipped — the pull is
// emitted as a forwarding property on the LOCAL side (see varLinknamePull). Mirrors the
// collectPublicizedTypes analysis pass: a package-wide pre-pass whose result the per-file emission
// consults (a handle in runtime/linkname.go must widen a var defined in runtime/panic.go).
func collectLinknameHandles(files []*ast.File) {
	for _, file := range files {
		for _, group := range file.Comments {
			for _, comment := range group.List {
				fields := strings.Fields(comment.Text)

				// One-arg handle: exactly the directive + the authorized symbol name.
				if len(fields) == 2 && fields[0] == "//go:linkname" {
					linknameHandles.Add(fields[1])
				}
			}
		}
	}
}

// packageVarAccess returns the C# access modifier for a package-level var. It is normally the Go
// name's exported-ness (getAccess), but a var carrying a definition-side one-arg //go:linkname handle
// is emitted `public`: Go 1.23 has deliberately opened it to cross-package linkname pulls, so a
// puller in another assembly must be able to reach it — a lowercase-name `internal` would hide it.
//
// A handle var is publicized ONLY when its TYPE is itself publicly accessible: a public member cannot
// expose a less-accessible type (CS0052/CS0053), and runtime's handle list includes deep-internal
// state whose type is unexported (`sched` of `schedt`, `writeBarrier` of an anonymous struct,
// `lastmoduledatap` of `*moduledata`). Such a var could not be linkname-pulled across a C# assembly
// boundary anyway — a foreign package cannot name the internal type — so it keeps its `internal` form.
func packageVarAccess(goIDName string, varType types.Type) string {
	if linknameHandles.Contains(goIDName) && typeIsPubliclyAccessible(varType) {
		return "public"
	}

	return getAccess(goIDName)
}

// packageFuncAccess returns the C# access modifier for a package-level FUNC. It is normally the Go
// name's exported-ness (getAccess), with ONE exception: a func that this converter FORWARDS a
// cross-package linkname pull to (linknameForwardTargets) is emitted `public`, because the puller
// compiles into a different assembly and an unexported Go name would otherwise be `internal` there.
//
// This is deliberately far narrower than the var rule above, which publicizes on the one-arg
// `//go:linkname` HANDLE alone. Go 1.23 carries 340 such handles outside cmd/ — publicizing every
// one would widen the corpus's whole surface for pulls that are never emitted. The forward-target
// list is the converter's own record of which pulls actually become a call, so gating on it moves
// exactly the symbols that need to move. The handle is still required: it is Go's authorization for
// the pull, and forwarding to a symbol its own package never opened would not be faithful.
func packageFuncAccess(goIDName string, isFreeFunction bool) string {
	if isFreeFunction && linknameHandles.Contains(goIDName) && linknameForwardTargets[currentPackagePath+"."+goIDName] {
		return "public"
	}

	// The PUSH direction's mirror image: the pushing DEFINITION (runtime's
	// unique_runtime_registerUniqueMapCleanup) is what the consumer's forwarder calls across the
	// assembly boundary, so it must be public for the same reason. Its own package never opened it
	// with a handle — a push carries its authorization on the CONSUMER's side — so this arm reads
	// the push registry alone. See linknamePushTargets.
	if isFreeFunction && linknamePushSources[currentPackagePath+"."+goIDName] {
		return "public"
	}

	return getAccess(goIDName)
}

// linknamePush is the disposition of a `//go:linkname` PUSH — the direction where the DEFINING
// package carries the body and names ANOTHER package's bodyless declaration as the symbol it
// defines (`//go:linkname unique_runtime_registerUniqueMapCleanup unique.runtime_registerUniqueMapCleanup`
// in runtime/mgc.go). The consuming side is an ordinary bodyless func under a one-arg handle, so
// without this the converter emitted a throwing PartialStubGenerator stub and the pushed body was
// unreachable.
//
// `source` always names the pushing definition ("<pkgPath>.<funcName>"). An empty `reason` means the
// pushed body is honorable and the consumer's declaration becomes a FORWARDER to it. A non-empty
// `reason` says why the pushed body CANNOT be honored in the managed model, and the declaration
// becomes a stub that panics naming both halves of the pair — never a fabricated body that would
// look truthful (the inverse of the atomic rule: a contract that cannot be honored must announce
// itself, not answer plausibly).
type linknamePush struct {
	source string
	reason string

	// bareDecl records that the CONSUMER declares the symbol with NO `//go:linkname` directive of its
	// own — a plain bodyless func whose only directive is the two-arg one on the PUSHING side
	// (`func runtime_envs() []string // in package runtime` in syscall/env_unix.go, pushed by
	// runtime/runtime.go). That is the standard library's older push shape, every bit as legal as the
	// one-arg-handle form `unique` and `internal/weak` use, and the corpus contains both — so the
	// matcher has to accept both.
	//
	// It is recorded per entry rather than inferred, so the match still FAILS CLOSED in both
	// directions: a handle entry will not forward a declaration that carries no handle, and a bare
	// entry will not forward one that does. Neither shape is verifiable from the consumer's own
	// syntax — the pushing package's directive is invisible while converting the consumer, which is
	// the whole reason this registry is curated — so the consumer's shape is part of the same
	// recorded judgment as the disposition.
	bareDecl bool
}

// linknamePushTargets is the registry of `//go:linkname` PUSH destinations the converter resolves,
// keyed "<consumerPkgPath>.<symbol>" — the consumer's own fully-qualified declaration.
//
// Curated, for the same reason linknameForwardTargets is: a bodyless func under a one-arg handle is
// INDISTINGUISHABLE at conversion time from an ordinary assembly stub, and the converter never sees
// the pushing package's comments while converting the consumer (a package is converted from its own
// syntax; dependencies contribute types, not directives). Go 1.23 carries ~200 pushes outside cmd/,
// of which the converted corpus exposes ELEVEN as bodyless one-arg-handle declarations — and most of
// those (time's timer trio, internal/syscall/windows's stdcall wrappers, internal/coverage/cfile's
// linker-section walk) push a body the managed model cannot run, several already answered by a
// hand-written companion that a converter-emitted body would collide with. Linking them wholesale
// would be a regression dressed as a feature, so each entry is a judgment recorded here.
var linknamePushTargets = map[string]linknamePush{
	// unique's cleanup registration, pushed by runtime/mgc.go. The pushed body is ORDINARY converted
	// Go — it makes a `chan struct{}` and starts a goroutine that drains it and calls the callback —
	// so the managed model runs the real thing: the registration succeeds and the cleanup goroutine
	// parks on the channel. Nothing signals it, because the converted runtime's clearpools() is
	// driven by Go's own GC, which does not run; that is Go's OWN behavior for a program whose GC
	// never fires (unique's map simply keeps its entries), not a fabricated answer. Before this the
	// stub threw out of `unique.Make`'s setupMake.Do, taking net/netip and gob's TestNetIP with it.
	"unique.runtime_registerUniqueMapCleanup": {source: "runtime.unique_runtime_registerUniqueMapCleanup"},
	// syscall's environment snapshot, pushed by runtime/runtime.go. The pushed body is ordinary
	// converted Go — `append([]string{}, envs...)` — and `runtime.envs` is genuinely populated in the
	// managed model by the hand-owned runtime/goenvs_impl.cs module initializer, so the forwarder
	// hands back the real process environment rather than a plausible-looking empty one.
	//
	// This is the BARE consumer shape: syscall/env_unix.go declares `func runtime_envs() []string //
	// in package runtime` with no directive of its own. Until this row existed the declaration took a
	// throwing PartialStubGenerator stub, and because `envs` is a package-level var INITIALIZED from
	// it, the throw came out of syscall's type initializer — taking os.init() and therefore every
	// Linux program that so much as touches fmt down with it. The Windows corpus never surfaced it
	// because env_unix.go is `//go:build unix || (js && wasm) || plan9 || wasip1`, so the declaration
	// does not exist there at all.
	"syscall.runtime_envs": {source: "runtime.syscall_runtime_envs", bareDecl: true},
	// os's command-line snapshot, pushed by runtime/runtime.go — the exact sibling of the envs row
	// above, in the same BARE consumer shape: os/proc.go declares `func runtime_args() []string //
	// in package runtime` with no directive of its own, and runtime pushes it with
	// `//go:linkname os_runtime_args os.runtime_args`.
	//
	// The pushed body is ordinary converted Go (`append([]string{}, argslice...)`), and — this is
	// the part that had to be TRUE before the row could be honorable — `runtime.argslice` is
	// genuinely populated in the managed model by the hand-owned runtime/goargs_impl.cs module
	// initializer, which fills it from Environment.GetCommandLineArgs(). Without that companion the
	// forwarder would have returned an EMPTY os.Args: not an error, just a plausible-looking wrong
	// answer, which is the failure mode this project rules against. Forwarding and populating are
	// therefore one change, not two.
	//
	// Windows is unaffected in both halves: os.init() returns early there (Args comes from
	// exec_windows.go) and goargs_impl.cs keeps goargs()'s own `if GOOS == "windows" { return }`
	// guard, so argslice stays unset exactly as in Go.
	"os.runtime_args": {source: "runtime.os_runtime_args", bareDecl: true},
	// internal/syscall/windows's system-directory query, pushed by runtime/os_windows.go. Unlike the
	// two rows above this is the HANDLE consumer shape — security_windows.go carries its own one-arg
	// `//go:linkname GetSystemDirectory` above a bodyless `func GetSystemDirectory() string` — so
	// bareDecl stays false. It is the first FORWARDED handle-shape row since `unique`.
	//
	// The pushed body is one line of ordinary converted Go (`unsafe.String(&sysDirectory[0],
	// sysDirectoryLen)`), and the precondition the os.runtime_args row spells out had to be made true
	// first. Go fills `runtime.sysDirectory` in initSysDirectory() with
	// `stdcall2(_GetSystemDirectoryA, …)`, called from osinit — and NEITHER half runs in the managed
	// model: osinit is Go's runtime bootstrap, which the converter emits already marked not-run, and
	// stdcall bottoms out in asmstdcall, a throwing stub. So the buffer stays all-zero and its length
	// zero, and a forwarder ALONE would have returned "" — turning net's
	// `hostsFilePath = windows.GetSystemDirectory() + "/Drivers/etc/hosts"` into
	// "/Drivers/etc/hosts", a plausible-looking wrong answer. The hand-owned
	// runtime/windows/os_windows_impl.cs module initializer fills the buffer from
	// Environment.GetFolderPath(SpecialFolder.System), trailing backslash and all, so forwarding and
	// populating are one change here too.
	//
	// What the stub was costing: the throw came out of a package-level VAR INITIALIZER, so it
	// surfaced from net_package's type initializer — and every httptest consumer dies in net's cctor,
	// whatever it was actually testing (net/http/cgi's TestCopyError is where it was found).
	"internal/syscall/windows.GetSystemDirectory": {source: "runtime.windows_GetSystemDirectory"},
	// internal/weak's two halves, pushed by runtime/mheap.go — the UNHONORABLE class. Both pushed
	// bodies reach the span allocator: registerWeakPointer → getOrAddWeakHandle → spanOfHeap →
	// `throw("getWeakHandle on invalid pointer")`, and makeStrongFromWeak reads a handle word out of
	// the heap and re-derives a pointer from it. The managed model populates no mheap_ span metadata
	// and cannot re-derive an object from an address, so a forwarder here would either fault or, far
	// worse, hand back a plausible-looking pointer derived from garbage.
	//
	// The remedy these two announced has since LANDED: src/core/internal/weak/pointer.cs is a
	// hand-owned managed weak reference over the ж<T> box under [module: go.GoManualConversion] —
	// System.WeakReference plus a ConditionalWeakTable canonical index (see
	// ConversionStrategies-Reference, "internal/weak.Pointer"). These rows therefore no longer
	// describe the deployed corpus, where the marked file is never regenerated; they describe what a
	// conversion into a root that does NOT already carry the hand-own emits, and that must still be
	// the loud pair rather than a fabricated body. Keep them, with the reason naming the file the
	// caller should be reaching for.
	"internal/weak.runtime_registerWeakPointer": {
		source: "runtime.internal_weak_runtime_registerWeakPointer",
		reason: "the pushed body walks mheap_ span metadata the managed model does not populate; use the hand-owned managed weak reference in internal/weak/pointer.cs",
	},
	"internal/weak.runtime_makeStrongFromWeak": {
		source: "runtime.internal_weak_runtime_makeStrongFromWeak",
		reason: "the pushed body re-derives an object pointer from a heap address, which the managed model cannot do; use the hand-owned managed weak reference in internal/weak/pointer.cs",
	},
}

// linknamePushSources is the reverse index of the FORWARDED entries of linknamePushTargets: the set
// of pushing definitions ("<pkgPath>.<funcName>") a forwarder calls, so packageFuncAccess can emit
// each public. Built once from the registry so the two can never disagree. An unhonorable entry is
// excluded deliberately — nothing calls it, so widening it would be noise.
var linknamePushSources = func() map[string]bool {
	sources := map[string]bool{}

	for _, push := range linknamePushTargets {
		if push.reason == "" {
			sources[push.source] = true
		}
	}

	return sources
}()

// typeIsPubliclyAccessible reports whether a value of type t can be exposed by a `public` member —
// every NAMED type it references must be exported (or already publicized, or a universe type like
// `error`). Composites recurse to their element; an anonymous struct/signature and any other shape is
// conservatively NOT accessible (its emission may reference unexported members), so its handle var
// stays internal rather than risk CS0052/CS0053.
func typeIsPubliclyAccessible(t types.Type) bool {
	switch t := t.(type) {
	case *types.Basic:
		return true
	case *types.Alias:
		return typeIsPubliclyAccessible(types.Unalias(t))
	case *types.Named:
		obj := t.Obj()

		// A universe type (`error`, `comparable`) has no package and is always accessible.
		if obj == nil || obj.Pkg() == nil {
			return true
		}

		return obj.Exported() || packagePublicizedTypes[obj]
	case *types.Pointer:
		return typeIsPubliclyAccessible(t.Elem())
	case *types.Slice:
		return typeIsPubliclyAccessible(t.Elem())
	case *types.Array:
		return typeIsPubliclyAccessible(t.Elem())
	default:
		return false
	}
}

// varLinknamePull recognizes a package var carrying a TWO-argument `//go:linkname <name>
// <pkgpath>.<remote>` PULL directive (name matches the var). It returns the fully-qualified C#
// reference to the remote symbol (`go.runtime_package.overflowError`) and the remote's import path,
// so the var is emitted as a forwarding property to it and the path is queued for a project
// reference. The fully-qualified form resolves inside `namespace go;` without a file-local using.
// Go 1.23 requires the remote's package to authorize the pull with a matching one-arg handle, which
// the converter honors by emitting that remote public (see packageVarAccess) — so the forwarding
// property compiles across the assembly boundary. The comment may sit on the GenDecl or the spec.
func varLinknamePull(name string, docs ...*ast.CommentGroup) (ref string, pkgPath string, ok bool) {
	for _, doc := range docs {
		if doc == nil {
			continue
		}

		for _, comment := range doc.List {
			fields := strings.Fields(comment.Text)

			// //go:linkname <local> <pkgpath>.<remote>
			if len(fields) != 3 || fields[0] != "//go:linkname" || fields[1] != name {
				continue
			}

			target := fields[2]
			dot := strings.LastIndex(target, ".")

			if dot < 0 {
				continue
			}

			pkgPath = target[:dot]

			// A pull whose forwarding reference would form a project-ref cycle keeps its null-field
			// form (the pre-feature behavior) — runtime pulling internal/syscall/windows.CanUseLongPaths.
			if linknamePullWouldCycle(pkgPath) {
				return "", "", false
			}

			remote := getSanitizedIdentifier(target[dot+1:])
			class := RootNamespace + "." + convertImportPathToNamespace(pkgPath, PackageSuffix)

			return class + "." + remote, pkgPath, true
		}
	}

	return "", "", false
}
