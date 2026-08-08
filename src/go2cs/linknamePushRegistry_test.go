// linknamePushRegistry_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/ast"
	"go/build"
	"go/parser"
	"go/token"
	"path/filepath"
	"runtime"
	"strings"
	"testing"
)

// TestLinknamePushRegistryMatchesGoSource checks every linknamePushTargets row against the REAL Go
// source in GOROOT. This is the one verification the converter structurally cannot do for itself:
// while converting the CONSUMER it sees only that package's syntax, so a bodyless declaration is
// indistinguishable from an ordinary assembly stub and the pushing package's two-arg directive is
// invisible. The registry exists precisely to record that missing half as a human judgment — and an
// unverified judgment is exactly the thing that rots.
//
// It is also the check that would have caught the defect this test was written with. The matcher
// required a one-arg `//go:linkname` handle on every consumer, which is only ONE of the two push
// shapes Go's own standard library uses; `syscall.runtime_envs` has none (the older shape, where the
// only directive sits on the pushing side), so it fell through to a throwing PartialStubGenerator
// stub. Because `syscall.envs` is a package-level var initialized from that call, the throw came out
// of syscall's type initializer and took os.init() — and every Linux program that touches fmt — with
// it. Nothing on Windows could see it: env_unix.go is `//go:build unix || ...`.
//
// Three things are asserted per row, and each maps to a way the row can be wrong:
//
//   - the consumer's declaration EXISTS and is BODYLESS (a row naming a symbol Go has since given a
//     body, renamed, or deleted would otherwise sit in the registry doing nothing, silently);
//   - its SHAPE matches the row's bareDecl flag (the defect above, in both directions);
//   - the pushing side really does carry `//go:linkname <pusherFunc> <consumerPkg>.<symbol>` (the
//     authorization the converter is taking the row's word for).
//
// Build constraints are deliberately ignored — every .go file in the package directory is scanned,
// so a unix-only or windows-only declaration is found whatever host the test runs on. That is the
// point: the registry is platform-agnostic and must be verifiable from any lane.
func TestLinknamePushRegistryMatchesGoSource(t *testing.T) {
	goRoot := build.Default.GOROOT

	if goRoot == "" {
		goRoot = runtime.GOROOT()
	}

	if goRoot == "" {
		t.Skip("GOROOT not resolvable; nothing to verify the registry against")
	}

	if len(linknamePushTargets) == 0 {
		t.Fatal("linknamePushTargets is empty: the registry guard is vacuous")
	}

	for key, push := range linknamePushTargets {
		consumerPkg, symbol, ok := splitLastDot(key)

		if !ok {
			t.Errorf("registry key %q is not <pkgPath>.<symbol>", key)
			continue
		}

		pusherPkg, pusherFunc, ok := splitLastDot(push.source)

		if !ok {
			t.Errorf("registry row %q has source %q, which is not <pkgPath>.<funcName>", key, push.source)
			continue
		}

		// The consumer's declaration: present, bodyless, and the recorded shape.
		decl := findGoFuncDecl(t, goRoot, consumerPkg, symbol)

		if decl == nil {
			t.Errorf("registry row %q: no func %s declared in %s — the row names a symbol Go's source does not have (renamed? deleted?)", key, symbol, consumerPkg)
			continue
		}

		if decl.Body != nil {
			t.Errorf("registry row %q: %s.%s HAS a body in Go's source, so it is not a linkname push consumer at all", key, consumerPkg, symbol)
			continue
		}

		if got := !declHasLinknameDirective(decl); got != push.bareDecl {
			t.Errorf("registry row %q records bareDecl=%v but %s.%s %s a //go:linkname directive of its own — the shape is half the judgment and the matcher fails closed on a mismatch, so this row silently forwards nothing",
				key, push.bareDecl, consumerPkg, symbol, directivePhrase(!got))
		}

		// The pushing side's two-arg directive — the authorization the row is vouching for.
		if !pkgHasLinknamePush(t, goRoot, pusherPkg, pusherFunc, key) {
			t.Errorf("registry row %q: %s does not carry `//go:linkname %s %s` — the push this row claims authorizes the forward does not exist in Go's source", key, pusherPkg, pusherFunc, key)
		}
	}
}

// splitLastDot splits a "<pkgPath>.<name>" record at its LAST dot, so an import path containing dots
// or slashes (internal/weak, golang.org/x/...) keeps its shape.
func splitLastDot(qualified string) (pkgPath string, name string, ok bool) {
	dot := strings.LastIndex(qualified, ".")

	if dot <= 0 || dot == len(qualified)-1 {
		return "", "", false
	}

	return qualified[:dot], qualified[dot+1:], true
}

// directivePhrase renders the assertion failure in the direction the reader needs to act on.
func directivePhrase(hasDirective bool) string {
	if hasDirective {
		return "DOES carry"
	}

	return "does NOT carry"
}

// parseGoPackageDir parses every .go file directly in GOROOT/src/<pkgPath>, comments included and
// build constraints ignored, so a platform-gated declaration is visible from any host.
func parseGoPackageDir(t *testing.T, goRoot string, pkgPath string) []*ast.File {
	t.Helper()

	dir := filepath.Join(goRoot, "src", filepath.FromSlash(pkgPath))

	matches, err := filepath.Glob(filepath.Join(dir, "*.go"))

	if err != nil || len(matches) == 0 {
		t.Errorf("no Go source found for package %s at %s", pkgPath, dir)
		return nil
	}

	fileSet := token.NewFileSet()
	files := make([]*ast.File, 0, len(matches))

	for _, match := range matches {
		if strings.HasSuffix(match, "_test.go") {
			continue
		}

		file, err := parser.ParseFile(fileSet, match, nil, parser.ParseComments)

		if err != nil {
			// A file this converter never reads (assembly stubs with odd build tags) must not fail
			// the guard; a genuinely unparseable package will simply yield no match below.
			continue
		}

		files = append(files, file)
	}

	return files
}

// findGoFuncDecl returns the package-level func declaration named symbol, or nil.
func findGoFuncDecl(t *testing.T, goRoot string, pkgPath string, symbol string) *ast.FuncDecl {
	t.Helper()

	for _, file := range parseGoPackageDir(t, goRoot, pkgPath) {
		for _, decl := range file.Decls {
			funcDecl, isFunc := decl.(*ast.FuncDecl)

			// Recv != nil is a method, which a linkname push consumer never is.
			if !isFunc || funcDecl.Recv != nil || funcDecl.Name == nil || funcDecl.Name.Name != symbol {
				continue
			}

			return funcDecl
		}
	}

	return nil
}

// declHasLinknameDirective reports whether the declaration carries ANY //go:linkname directive of
// its own — the same question linknamePushDeclMatches asks, asked here of Go's source rather than of
// the converter's input so the two can never drift apart.
func declHasLinknameDirective(funcDecl *ast.FuncDecl) bool {
	if funcDecl.Doc == nil {
		return false
	}

	for _, comment := range funcDecl.Doc.List {
		if fields := strings.Fields(comment.Text); len(fields) > 0 && fields[0] == "//go:linkname" {
			return true
		}
	}

	return false
}

// pkgHasLinknamePush reports whether pkgPath carries `//go:linkname <pusherFunc> <target>` anywhere
// in its own source — the two-arg directive that actually performs the push. The whole comment set
// is scanned rather than just each func's doc, because Go places these directives freely (mgc.go
// carries unique's beside the definition, mheap.go carries weak's above it).
func pkgHasLinknamePush(t *testing.T, goRoot string, pkgPath string, pusherFunc string, target string) bool {
	t.Helper()

	for _, file := range parseGoPackageDir(t, goRoot, pkgPath) {
		for _, group := range file.Comments {
			for _, comment := range group.List {
				fields := strings.Fields(comment.Text)

				if len(fields) == 3 && fields[0] == "//go:linkname" && fields[1] == pusherFunc && fields[2] == target {
					return true
				}
			}
		}
	}

	return false
}
