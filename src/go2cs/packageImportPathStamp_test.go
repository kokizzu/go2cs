package main

import (
	"strings"
	"testing"
)

// The [GoPackage] ImportPath stamp: golib's package-path decoders rebuild a path from the namespace
// ('.' read as '/') plus the package name, which cannot carry a '.' inside a segment, a major-version
// directory, or a name that differs from its directory. The converter stamps the verbatim path
// exactly there, and REGENERATES the stamp on every write so a persisted package_info.cs can never
// keep a stale one.

func TestGoPackageImportPathStampNamesExactlyTheUnrecoverablePaths(t *testing.T) {
	cases := []struct {
		namespace, name, importPath, want string
	}{
		{"go.encoding", "gob", "encoding/gob", ""},
		{"go", "fmt", "fmt", ""},
		{"go.@internal", "abi", "internal/abi", ""}, // the '@' escape is a source spelling only
		{"go", "main", "main", ""},
		{"go.vendor.golang.org.x.crypto", "chacha20", "vendor/golang.org/x/crypto/chacha20", "vendor/golang.org/x/crypto/chacha20"},
		{"go.example.com.dotted", "dotted", "example.com/dotted/v2", "example.com/dotted/v2"},
		{"go.math.rand", "rand", "math/rand/v2", "math/rand/v2"},
		{"go.crypto.@internal", "fipsdeps", "crypto/internal/fips140deps", "crypto/internal/fips140deps"},
		{"go.encoding", "gob", "", ""}, // no path known: never stamp
	}

	for _, c := range cases {
		if got := goPackageImportPathStamp(c.namespace, c.name, c.importPath); got != c.want {
			t.Errorf("goPackageImportPathStamp(%q, %q, %q) = %q, want %q", c.namespace, c.name, c.importPath, got, c.want)
		}
	}
}

// withPackage sets the conversion globals the stamp classifier reads, restoring them afterwards.
func withPackage(t *testing.T, name, path string) {
	t.Helper()
	savedName, savedPath := packageName, currentPackagePath
	packageName, currentPackagePath = name, path
	t.Cleanup(func() { packageName, currentPackagePath = savedName, savedPath })
}

func TestConvergeGoPackageStampsReplacesAStalePathAndAddsAMissingOne(t *testing.T) {
	withPackage(t, "dotted", "example.com/dotted/v2")

	for _, stale := range []string{
		`[GoPackage("dotted")]`,                                // written before the stamp existed
		`[GoPackage("dotted", ImportPath = "example/com/dotted")]`, // a stale path from an older rule
	} {
		lines := []string{"namespace go.example.com;", "", stale, "public static partial class dotted_package", "{", "}"}
		got := convergeGoPackageStamps(lines, "go.example.com")[2]
		want := `[GoPackage("dotted", ImportPath = "example.com/dotted/v2")]`

		if got != want {
			t.Errorf("converged %s to %s, want %s", stale, got, want)
		}
	}
}

func TestConvergeGoPackageStampsDropsAStampTheDerivationNoLongerNeeds(t *testing.T) {
	withPackage(t, "gob", "encoding/gob")

	lines := []string{`[GoPackage("gob", ImportPath = "stale/path")]`, "public static partial class gob_package"}

	if got := convergeGoPackageStamps(lines, "go.encoding")[0]; got != `[GoPackage("gob")]` {
		t.Errorf("a recoverable package must converge to the plain stamp, got %s", got)
	}
}

func TestConvergeGoPackageStampsGivesEachTestVariantItsGoPath(t *testing.T) {
	// The EXTERNAL variant writes package_test_info.cs, which also carries the production class and
	// the internal-test bridge: each class keeps its own Go path.
	withPackage(t, "dotted_test", "example.com/dotted/v2_test")

	lines := []string{
		`[GoPackage("dotted")]`, "public static partial class dotted_package",
		`[GoPackage("dotted")]`, "public static partial class dotted_internal_test_package",
		`[GoPackage("dotted_test")]`, "public static partial class dotted_test_package",
		`[GoPackage("other")]`, "public static partial class other_package", // not this conversion's
	}

	got := convergeGoPackageStamps(lines, "go.example.com")
	want := []string{
		`[GoPackage("dotted", ImportPath = "example.com/dotted/v2")]`,
		`[GoPackage("dotted", ImportPath = "example.com/dotted/v2")]`,
		`[GoPackage("dotted_test", ImportPath = "example.com/dotted/v2_test")]`,
		`[GoPackage("other")]`,
	}

	for i, w := range want {
		if got[i*2] != w {
			t.Errorf("class %s: got %s, want %s", strings.TrimPrefix(got[i*2+1], "public static partial class "), got[i*2], w)
		}
	}
}

func TestConvergeGoPackageStampsNamesPackageMainByGosRule(t *testing.T) {
	withPackage(t, "main", "go2cs/DottedModulePath")

	lines := []string{`[GoPackage("main")]`, "public static partial class main_package"}

	if got := convergeGoPackageStamps(lines, "go")[0]; got != `[GoPackage("main")]` {
		t.Errorf("package main's reflect path is \"main\", so it needs no stamp; got %s", got)
	}
}
