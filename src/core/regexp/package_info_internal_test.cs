// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.regexp_package;
using static go.regexp_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<FindTest, ж<FindTest>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("regexp/all_test.go", "all_test.cs", "AEBwgoKClIKklKaCgriCgriygoKUgoKmgoK4grK4soKClIK4grK4soKClIKCgsqCsgB6hgKCgoKCgpSCgriCgtyUgoKUgoKClIKCuIKCzIKCgoKUgoK4goLcgrKCgoKUgoK4koIAKUiClIKClLqCgoKClIKCgoKC7qKUgoKClIIAIEKCgoKCgoKUgoKClIKCgriCgoIAJkqCgoKCgpaCgpaCgoIACg6ygoS+goKUgoKUgIIACAySgoKUgoKUgryigpaC1qKCgoKCgoKCgoLKooKCgoKCgoLKooKCgoKCgoKCggAICqKCgoKCgoKCgoKUgsqigoKCgoKCgoKClILKooKCgoKCgsqigoKCgoKCAAgKooKCgoKCgvqipoKCgoKC+qKCgoKCguiigoKCgoK4ooKCgpSCgoLoooKCgoKCuKKCgoKUgoKC6KKCgoKCguiigoKCgoLoooKCgoKC6KKCgoKCgriigoKCgoK4ooKCgoKC6KKCgoKigsqigoKCooKC7qKCgoKmgoKCgriigoKCggANFIKykoKCgIIACw6CgoKSloKCloKCloKCABcsgoKCgoIACAqCgoKCgoKClICCgqSCpoKCgoI=")]
[assembly: go.GoPositionMap("regexp/exec2_test.go", "exec2_test.cs", "AA8eooKU")]
[assembly: go.GoPositionMap("regexp/find_test.go", "find_test.cs", "ABkwggBo5AHCgoKCgoKCgoKmqpKCgoKUgsjEtIKClIL8goKCyMaSxoKC/KLIxLSCguqCsriCsriCsrySgoLIxLSCgpSCgoKUgoIACA6CgoLIxLSCgpSCgoIACA6iyMS0goKUgoL8grK4grK8soKClIKCgpSUgoKClIKCgsqCsoLIxLTqooKClIKCgpSUgoKCyoKygsjEtOqCgoKUgoLKosjEtNiCsriCsriCsrySsoLIxLS0gvyCsoLIxLS0gvyiyMS0tILqgrK4grI=")]
[assembly: go.GoPositionMap("regexp/onepass_test.go", "onepass_test.cs", "AH/6AYKygoKUggAvXIKqgoCCgraCgIKCpIKCAA8agoKCgoKUgoKUgg==")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("regexp")]
public static partial class regexp_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
