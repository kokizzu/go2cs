// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.@internal.reflectlite_package;
global using static global::go.@internal.reflectlite_internal_test_package;

// <ImportedTypeAliases>
global using Kind = go.@internal.abi_package.ΔKind;
global using abiꓸArrayType = go.@internal.abi_package.ΔArrayType;
global using abiꓸChanDir = go.@internal.abi_package.ΔChanDir;
global using abiꓸFuncType = go.@internal.abi_package.ΔFuncType;
global using abiꓸInterfaceType = go.@internal.abi_package.ΔInterfaceType;
global using abiꓸKind = go.@internal.abi_package.ΔKind;
global using abiꓸMapType = go.@internal.abi_package.ΔMapType;
global using abiꓸName = go.@internal.abi_package.ΔName;
global using abiꓸStructType = go.@internal.abi_package.ΔStructType;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectliteꓸKind = go.@internal.abi_package.ΔKind;
global using reflectliteꓸType = go.@internal.reflectlite_package.ΔType;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
// </ImportedTypeAliases>

using go;
using static global::go.@internal.reflectlite_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Loopy", "object")]
[assembly: GoTypeAlias("String", "const:ΔString")]
[assembly: GoTypeAlias("Tint2", "go.@internal.reflectlite_test_package.Tint")]
[assembly: GoTypeAlias("Type", "ΔType")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<mapError, error>(Pointer = true)]
[assembly: GoImplement<mapError, error>]
[assembly: GoImplement<visitor, go.go.ast_package.Visitor>]
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
[assembly: global::go.GoPositionMap("internal/reflectlite/all_test.go", "all_test.cs", "ABkmgqaCABAggoKCAKAC4gKCgoK4goKCuIKC6IKCgpSkpKSkpKSkpKSkpKSkpKSkgoIAMAqCACWAAbKSgoKCgpSUgIIAFyyCgoKCyoKSkoKCgriCkoKCggAICIKChIiCggAKBqKGgoKUgoSCgIL4gpKCgpQACxSCgpSCgqYACgaCgoKGggAHEIKCgoKCABUoABEWgoKEggBAigGUgIKkgoKClIKCyoKCgriCgoIAKwimAAgSgoKCmJaCgoSGgoKEhoKChIaCgoSGgoKEhoKCrLKClNaCgoCCAAgSkqikqJKCqLrSgoKCgpYADxSiAB5EgoCCAAkKgoKUgpSCgoKUgriCgoKEgoKCpoKChIKCggAcCoKAkoKAlAAAMpKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQpoKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQkpCSkJKQtqKCgqYAChCCgoKCgoIACQqEhoKUgoIAIz6CgoKAggAJFNKChpKSogAWKoKCgoKCloKCgoI=")]
[assembly: global::go.GoPositionMap("internal/reflectlite/reflect_mirror_test.go", "reflect_mirror_test.cs", "ACZIgoKEpIKCgqamgpSCgIKCgoKClOqmgoSGgoKWhIIADAiUgoKApqaChL6SgrKC1oSCloKCgpSCgII=")]
[assembly: global::go.GoPositionMap("internal/reflectlite/set_test.go", "set_test.cs", "ABIghqKCgoKCABowgKKAogANFIDqooKCgoCCABUuooKCgoCC")]
[assembly: global::go.GoPositionMap("internal/reflectlite/tostring_test.go", "tostring_test.cs", "ABAmovaCgoKUgpSkpKSCpKSClLaCgoKUlIKkgoKCgoKUlIKkgoKCgqSCpIKCgoKCgpSUgqSkpA==")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("reflectlite_test")]
public static partial class reflectlite_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface notASTExpr {}
    internal partial struct appendTestsᴛ1 {}
    internal partial struct big {}
    internal partial struct implementsTestsᴛ1 {}
    internal partial struct integer {}
    internal partial struct mapError {}
    internal partial struct nameTest {}
    internal partial struct notAnExpr {}
    internal partial struct pair {}
    internal partial struct self {}
    internal partial struct typeᴛ18_x {}
    internal partial struct typeᴛ20_x {}
    internal partial struct typeᴛ21_x {}
    internal partial struct typeᴛ22_x {}
    internal partial struct typeᴛ23_x {}
    internal partial struct typeᴛ24_x {}
    internal partial struct typeᴛ25_x {}
    internal partial struct typeᴛ26_x {}
    internal partial struct typeᴛ27_x {}
    internal partial struct typeᴛ28_x {}
    internal partial struct typeᴛ29_x {}
    internal partial struct visitor {}
    public partial class IntPtr {}
    public partial class IntPtr1 {}
    public partial class Loop {}
    public partial interface Δtypeᴛ30 {}
    public partial struct A {}
    public partial struct B<T> {}
    public partial struct Basic {}
    public partial struct Ch {}
    public partial struct D1 {}
    public partial struct D2 {}
    public partial struct DeepEqualTest {}
    public partial struct NotBasic {}
    public partial struct Point {}
    public partial struct S {}
    public partial struct T {}
    public partial struct Talias1 {}
    public partial struct Talias2 {}
    public partial struct TestBigUnnamedStruct_b {}
    public partial struct TestBigUnnamedStruct_type {}
    [GoLocalName("Embed")] public partial struct TestCanSetField_Embed {}
    [GoLocalName("S1")] public partial struct TestCanSetField_S1 {}
    [GoLocalName("S2")] public partial struct TestCanSetField_S2 {}
    [GoLocalName("S3")] public partial struct TestCanSetField_S3 {}
    [GoLocalName("S4")] public partial struct TestCanSetField_S4 {}
    [GoLocalName("embed")] public partial struct TestCanSetField_embed {}
    [GoLocalName("testCase")] public partial struct TestCanSetField_testCase {}
    public partial struct TestCanSetField_tests {}
    public partial struct TestImportPath_tests {}
    public partial struct TestInterfaceValue_inter {}
    [GoLocalName("T")] public partial struct TestInvalid_T {}
    public partial struct TestIsNil_doNil {}
    public partial struct TestIsNil_doNilᴛ1 {}
    public partial struct TestIsNil_doNilᴛ2 {}
    public partial struct TestIsNil_doNilᴛ3 {}
    public partial struct TestIsNil_doNilᴛ4 {}
    public partial struct TestIsNil_doNilᴛ5 {}
    public partial struct TestIsNil_doNilᴛ6 {}
    public partial struct TestIsNil_fi {}
    public partial struct TestIsNil_mi {}
    public partial struct TestIsNil_si {}
    public partial struct TestMirrorWithReflect_type {}
    [GoLocalName("T")] public partial struct TestSetPanic_T {}
    [GoLocalName("T2")] public partial struct TestSetPanic_T2 {}
    [GoLocalName("t0")] public partial struct TestSetPanic_t0 {}
    [GoLocalName("t1")] public partial struct TestSetPanic_t1 {}
    public partial struct TestUnaddressableField_localBuffer {}
    public partial struct TheNameOfThisTypeIsExactly255BytesLongSoWhenTheCompilerPrependsTheReflectTestPackageNameAndExtraStarTheLinkerRuntimeAndReflectPackagesWillHaveToCorrectlyDecodeTheSecondLengthByte0123456789_0123456789_0123456789_0123456789_0123456789_012345678 {}
    public partial struct Tint {}
    public partial struct Δtype {}
    public partial struct Δtypeᴛ1 {}
    public partial struct Δtypeᴛ10 {}
    public partial struct Δtypeᴛ11 {}
    public partial struct Δtypeᴛ12 {}
    public partial struct Δtypeᴛ13 {}
    [GoValueClone("x")] public partial struct Δtypeᴛ14 {}
    public partial struct Δtypeᴛ15 {}
    public partial struct Δtypeᴛ16 {}
    public partial struct Δtypeᴛ17 {}
    public partial struct Δtypeᴛ18 {}
    public partial struct Δtypeᴛ19 {}
    public partial struct Δtypeᴛ2 {}
    public partial struct Δtypeᴛ20 {}
    public partial struct Δtypeᴛ21 {}
    public partial struct Δtypeᴛ22 {}
    public partial struct Δtypeᴛ23 {}
    public partial struct Δtypeᴛ24 {}
    public partial struct Δtypeᴛ25 {}
    public partial struct Δtypeᴛ26 {}
    public partial struct Δtypeᴛ27 {}
    public partial struct Δtypeᴛ28 {}
    public partial struct Δtypeᴛ29 {}
    public partial struct Δtypeᴛ3 {}
    public partial struct Δtypeᴛ4 {}
    public partial struct Δtypeᴛ5 {}
    public partial struct Δtypeᴛ6 {}
    public partial struct Δtypeᴛ7 {}
    public partial struct Δtypeᴛ8 {}
    public partial struct Δtypeᴛ9 {}
    // </TypeAccessibility>
}
