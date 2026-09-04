// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.testing_package;

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.testing_test_package;

// <ExportedTypeAliases>
[assembly: GoDynamicTypeLift("696e746572666163657b4973426f6f6c466c6167282920626f6f6c7d", "testFlagHelper_type")]
[assembly: GoDynamicTypeLift("7374727563747b6e616d6520737472696e673b20666e2066756e6328293b20616c6c6f637320666c6f617436347d", "allocsPerRunTestsᴛ1")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
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
[assembly: go.GoPositionMap("testing/allocs_test.go", "allocs_test.cs", "ABMgoqKioraCgoCC")]
[assembly: go.GoPositionMap("testing/flag_test.go", "flag_test.cs", "ABYmooKCloSCgoKCgoKUgoKUgoK4lIIADw6SgoKWgoKkloKClISClKSkpKaC", "29-50:1")]
[assembly: go.GoPositionMap("testing/helper_test.go", "helper_test.cs", "ACkegoKogoKWgoSCgpaCgoKEAAAmgviCgoKWgoSCgpaCgoKEhIq0gpSCgILIooKUgpSCgoKClA==", "101-103:1;104-106:2")]
[assembly: go.GoPositionMap("testing/helperfuncs_test.go", "helperfuncs_test.cs", "AAockqaCgqaCpoKCpoKCyoL2xoKCgpaCgpSEgoKmgrqCgpSCgrqohILWgoKCgoKCptaCgoKCuKKCgoKAgrbWgoI=", "52-55:1;58-65:2;69-72:3;73-76:4;94-97:1;104-107:1;112-117:1")]
[assembly: go.GoPositionMap("testing/panic_test.go", "panic_test.cs", "ACQwgoQAgQHiAbKSgoKCgoKCgoCCAAsMgoKCgtaCgpSCgpS2pIKCgqaCgoKCgoKCgriClIKCgoKCgrgACwyihAAOJIKCgoKCgoKAggAJCoKCloKCyoKCloCSgg==", "141-152:1;176-181:1;184-209:2;187-192:2.1;200-205:2.2;252-256:1;253-255:1.1;264-264:1")]
[assembly: go.GoPositionMap("testing/testing_test.go", "testing_test.cs", "AB02soKWAAUcAAoChIKClJaCgpSC6KKClIKmAA4KgoKCgoKCgoKCggAIBoKCpKSClJSClLSCyoKClIKClIKUgoKClIKUgoKUgpaCgIIADAiiABY8soKAgraWkoKCqIKClIIACQqigoKAgriEAAgGooKCgIK4hAAIBoKEooKCgIK4AAkIgoSCooKCgIK4AA0YkoIAGCKigpSClIKWgpSEgoCCpoKCgpaCggAIDMKEhIKCloKCgoKEqqKCgpKClILmgpSClJaWgoKClIL6koKCloSCAAgIgoKCgpKSgoKUkoKCpoKWzIKCgpSCuoKCgpSC6IKCgoKCppaWgoKClIK6goKClIIACAiCgoKCgqaUloSClIKUgriCgoKCgpKCgpaSkoKClJKCgqiWhIKUgpSC6IKCgoKUloSClIKUguiihIKCloKCgoKEhIKClILogoKElIKUgriigpSC6IKChKaClIL4goKWgoKClJaCgqjWgryCgoKCgoKCggAGEIKCgoKCgoCC7t6CgpSClJSCuKKEgoKogoKogoLegoKCgoKCgIIABxC6goKUgpSUgriigoKUgIKAgoLaloKopqiCkoKCgpKCgqaCggAIBsiCgpI=", "49-54:1;50-52:1.1;66-75:1;67-72:1.1;93-111:1;186-191:1;204-209:1;217-222:1;232-241:1;233-238:1.1;247-258:1;248-257:1.1;249-254:1.1.1;346-349:1;357-359:1;394-405:1;395-399:1.1;400-404:1.2;441-444:1;474-481:1;475-479:1.1;476-478:1.1.1;503-507:1;509-520:2;510-514:2.1;515-519:2.2;540-543:1;628-634:1;636-640:2;654-664:1;657-662:1.1;708-723:1;712-716:1.1;719-721:1.2;794-798:1;809-814:1;811-813:1.1")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("testing_test")]
public static partial class testing_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface testFlagHelper_type {}
    internal partial struct TestMorePanic_testCases {}
    internal partial struct TestPanic_testCases {}
    internal partial struct TestSetenv_tests {}
    internal partial struct allocsPerRunTestsᴛ1 {}
    // </TypeAccessibility>

    // Go initializes an imported package before the importing package, for every import
    // form - not only the blank one. .NET would never load an assembly nothing has touched
    // yet, so each import that initializes anything is forced below: once per assembly, and
    // ahead of this package's own `init` functions, which this file being the first compile
    // item of the project guarantees.

    // <ImportInitializers>
    [GoInit] internal static void initᴛᴛimportꓸbytes() => builtin.initPackage(typeof(bytes_package));
    [GoInit] internal static void initᴛᴛimportꓸflag() => builtin.initPackage(typeof(flag_package));
    [GoInit] internal static void initᴛᴛimportꓸfmt() => builtin.initPackage(typeof(fmt_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸtestenv() => builtin.initPackage(typeof(@internal.testenv_package));
    [GoInit] internal static void initᴛᴛimportꓸos() => builtin.initPackage(typeof(os_package));
    [GoInit] internal static void initᴛᴛimportꓸosꓸexec() => builtin.initPackage(typeof(go.os.exec_package));
    [GoInit] internal static void initᴛᴛimportꓸpathꓸfilepath() => builtin.initPackage(typeof(path.filepath_package));
    [GoInit] internal static void initᴛᴛimportꓸregexp() => builtin.initPackage(typeof(regexp_package));
    [GoInit] internal static void initᴛᴛimportꓸruntime() => builtin.initPackage(typeof(runtime_package));
    [GoInit] internal static void initᴛᴛimportꓸslices() => builtin.initPackage(typeof(slices_package));
    [GoInit] internal static void initᴛᴛimportꓸstrings() => builtin.initPackage(typeof(strings_package));
    [GoInit] internal static void initᴛᴛimportꓸsync() => builtin.initPackage(typeof(sync_package));
    [GoInit] internal static void initᴛᴛimportꓸtesting() => builtin.initPackage(typeof(testing_package));
    [GoInit] internal static void initᴛᴛimportꓸtime() => builtin.initPackage(typeof(time_package));
    // </ImportInitializers>
    // Go runs every `init` in the package under test - the production files' included -
    // before the first test. The production package is a REFERENCED assembly here, whose
    // module constructor .NET would not run until something in it is touched, so that
    // initialization is forced before anything else in this test module runs.
    [GoInit] internal static void initᴛᴛproduction() {
        builtin.initPackage(typeof(global::go.testing_package));
    }
}
