// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.math_package;
global using static global::go.math_internal_test_package;

// <ImportedTypeAliases>
// </ImportedTypeAliases>

using go;
using static global::go.math_test_package;

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
[assembly: go.GoPositionMap("math/all_test.go", "all_test.cs", "AMsS8CC4gpSCgrqCgoKmpICigKKAooKUpKSmgoKClIKCuIKCgoCCtoKAgtqCgoKAgraCgILagoKCgIK2goCC2oKCgIK2goCC2oKCgIK2goCC2oKCgoCCtoKAgtqCgoCCtoKAgtqCgoCCtoKAgtqCgoCCtoKAgtqCgoCCtoKAgraCgILagoKAgraCgILagoKAgraCgILagoKCgIK2goCC2oKCgoCCtoKAgtqCgoKAgraCgIK2goCCtoKAgtqCgoKAgraCgIK2goCCtoKAggAJCoKCpoKCgIK2goCC2oKCgoCCtoKCgIK2goCCAAkKgoKmgoKAgraCgIK2goKCgsqCgoCCtoKAgtqCgoCCtoKAgraCgILagoKAgraCgILagoKAgraCgIK2goCC2oKCgIK2goCCtoKAgtqCgoCCtoKAgsiAgsiCgoCCtoKAgraCgILagoKAgraigoKCpJSUgsqCgoKAgraCgILagoKCgIK2goCC2oKCgoCCtoKAgraCgILagoKAgraCgILagoKAgraCgILagoKAgqSAgraCgIKkgILagoKAgraCgIK2goCCtoKAgraCgILagoKAgraCgILagoKCgIK2gIKkgoCC2oKCgIK2goCCtoKAgtqCgoKAgraAgqSCgILagoKCgIK2goCCpIKAgtqCgoKAgraAgqSCgIK2goKCgsqCgoCCtoKAgtqCgoKAgraCgILagoKAgraCgILagoKAgraCgILagoKAgtqCgoCCtoKAgsiAgraCgIK2goKCgoLKgoKAgraCgILagoKAgraCgILaooKAgraCgILYgoKAgraCgILagoKAgtqCgoCCtoKAgtqCgoKAgqSCgIK2goCCpICC2oKCgILIgoCC2oKCgIK2goCC2oKCgIK2goCC2oKCgoCCtoKAgtqCgoKAgraCgILagoKCgIKkgIK2goCCpICCtoCC7IKCgoKUgoLMkqiSqJKmyoKCgoKUgoKClIKCggAEEsKCgoKCgsqCgoKCgoLKgoKCgoKCyoKCgoKCgs6ilJSCgqaClIKEgIKkgIKkgIKkgoKCABQsgoKCgsqCgIKkgILukoKCgoKClJSCABAiooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpTKooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpTKooKClKiigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKCgpSCpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaigoKClIKmooKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgoKUgqaigoKUpqKCgpSmooKClKaigoKUyqKCgpTKooKClMqigoKUpqKCgpSmooKClMqigoKUpqKCgpSmooKCgpSCpqKCgpSmooKCgpSmooKClKaigoKClKaigoKUptyCgqamooKClKaigoKUpqKCgpSkooKClKaigoKUpqKCgpSmooKClKaigoKUyqKCgpTKooKClMqigoKUpqKCgpQ=")]
[assembly: go.GoPositionMap("math/const_test.go", "const_test.cs", "AAsagoCCpICCpICCpICCpICCyIKAgqSAgqSAgqSAgqSAgg==")]
[assembly: go.GoPositionMap("math/huge_test.go", "huge_test.cs", "AESQAbKCgoKClIKCyoKCgoKClIKCyoKCgoKClIKCyoKCgoKClIKC")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("math_test")]
public static partial class math_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct fi {}
    internal partial struct floatTest {}
    internal partial struct fmaCᴛ1 {}
    // </TypeAccessibility>
}
