// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static go.go.types_package;
using static go.go.types_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
// </InterfaceImplementations>

// <ImplicitConversions>
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("go/types/context_test.go", "context_test.cs", "ABEcooIACRzSlIKUlIKUlIKClpaCgILKgILKgIK4gII=")]
[assembly: global::go.GoPositionMap("go/types/errors_test.go", "errors_test.cs", "ABAYgoKCgIKmgoKAgqaCgoCCAAgIggAGEoKC")]
[assembly: global::go.GoPositionMap("go/types/sizeof_test.go", "sizeof_test.cs", "ABEakoQAHUiCgoKClII=")]
[assembly: global::go.GoPositionMap("go/types/termlist_test.go", "termlist_test.cs", "AA0gkoKCgoKUpoKCuIIAChaAgtqCAAgSgoKCyoIACRSCgoIACAqCAA0ggoKCAAgKggAPJoKCgoIACAqCABEqgoKCggAJCoIAChyCgoKCAAkKggAMIIKCgoIACQqCABY0goKCggAJCoIAESqCgoKC")]
[assembly: global::go.GoPositionMap("go/types/token_test.go", "token_test.cs", "ABw6hJKCuJSCgoKCgg==")]
[assembly: global::go.GoPositionMap("go/types/typeset_test.go", "typeset_test.cs", "ABEcgoLoggAXOIKCgoKYkoKCqIKClIKCqIKC")]
[assembly: global::go.GoPositionMap("go/types/typeterm_test.go", "typeterm_test.cs", "ABAgggANGoKCgILagoKClKaCgoKUpoIADyCCgoKCgIK2goCC2qIAH0KCgoKCgoCC2oIAESSCgoKCgIK2goCC2oIACRSCgoKCgILaggASJoKCgoKAgtqCAAsYgoKCgoCCtoKAgg==")]
[assembly: global::go.GoPositionMap("go/types/util_test.go", "util_test.cs", "AA8igKSgooA=")]
// </GoSourcePositionMaps>

namespace go.go;

[GoPackage("types")]
public static partial class types_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
