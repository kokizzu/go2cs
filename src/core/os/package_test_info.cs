// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.os_package;
global using static global::go.os_internal_test_package;

// <ImportedTypeAliases>
global using DirEntry = go.io.fs_package.DirEntry;
global using FileInfo = go.io.fs_package.FileInfo;
global using FileMode = go.io.fs_package.FileMode;
global using PathError = go.io.fs_package.PathError;
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static global::go.os_test_package;

// <ExportedTypeAliases>
[assembly: GoDynamicTypeLift("7374727563747b696e20737472696e673b206f757420737472696e677d", "expandTestsᴛ1")]
[assembly: GoDynamicTypeLift("7374727563747b6e616d6520737472696e673b20662066756e63282a6f732e46696c6529206572726f727d", "nilFileMethodTestsᴛ1")]
[assembly: GoTypeAlias("Kill", "const:ΔKill")]
[assembly: GoTypeAlias("Signal", "ΔSignal")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<myErrorIs, error>(Promoted = true)]
[assembly: GoImplement<myErrorIs, error>]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<net_package.Conn, io_package.Writer>]
[assembly: GoImplement<os_package.File, io_package.ReadCloser>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.WriteCloser>(Pointer = true)]
[assembly: GoImplement<randReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<syscall_package.ΔSignal, os_package.ΔSignal>]
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
[assembly: go.GoPositionMap("os/copy_test.go", "copy_test.cs", "ABky0oKEgoKUkoCCpICCpoKClJSCgoLCgoCC5sKCkoCC5oSAgqSAggAKCIKCgoKCgpSCgpSClIKm7oKmgoKCgoKClIKClKaygoKWgoKUkoKUgpSCpoKigoKClJSCgoKU", "50-55:1;56-62:2;128-138:1;140-147:2")]
[assembly: go.GoPositionMap("os/env_test.go", "env_test.cs", "ABcekpSkpKSkpKSkpAAdNIKCgoIADQ6CgoKCgpSUgoKCgpS4goKCgoIACAqCgoKCgoKmlICCpIKUgIKkgrjCgpaCpoKAgsqAgqSAgqSCgIIACgiigoKClIKCgpSCgu7ShIKCypSCloKCgoKUuoCClA==", "72-79:1;76-76:1.1;80-87:2;84-84:2.1;102-110:1;130-139:1")]
[assembly: go.GoPositionMap("os/error_test.go", "error_test.cs", "ABYgooSCgoKUgpKCgoKUgIIACwiCgoKWgoKClICCpoKCgIKklICCpOaCgoKAgqaCgILIgoKUgpQAGDKCgoCCpICCpICCpICCABAggoKAgqSAggAKCqKEgoKClIKClIKSgoKUgoKCggAJCIKCgvyA1IKC")]
[assembly: go.GoPositionMap("os/error_windows_test.go", "error_windows_test.cs", "AA4egoQADCI=")]
[assembly: go.GoPositionMap("os/exec_test.go", "exec_test.cs", "ABciooKUgpaCgoSCgIK4tAAKDLKChIKClJKUyoKUgIKmhpKCloI=")]
[assembly: go.GoPositionMap("os/exec_windows_test.go", "exec_windows_test.cs", "ABok1IKUhJKClIKClJKClJSAgqSCgoKClICCgqSAgoKkgIK8soKCsoKClIKAgraCgug=", "66-80:1")]
[assembly: go.GoPositionMap("os/executable_test.go", "executable_test.cs", "ABckgoKEgoKmgoKClpSCgsqUgoKClIKClIK4goKClIKClKaCgJSCgoKClJSCgIKUpAAJCIKClKSkhISChIKCloKCgpaCgoI=")]
[assembly: go.GoPositionMap("os/os_test.go", "os_test.cs", "ACg+goKCgpaEABgolAAIEIKClO6CgoKAgoK2groACRIACBYACRIADBSigoKUkoCCtoKClNailKSkpqKCgoKUkoCCpICCtgALDIKEgoKClIKUgoIACAiihISCgpSClICCpoSCgoKWgoKUgpSAgviihISCgpSEgoKUhIKAggAKCKKEgoKClJKCgpSClIKCAAkIgoSCgoKUgpSCgoLMsoSCgoKUlIKCgpSCgoLqkoSCgoKUhIKEgoIACAiCsoSCgpSSgoKUgoKCgpSClIKUlIKmggAKCoKyhIKClJKCgpSCgoKClIKUgpSUgqaCAAoKgrKEgoKUkoKClIKCgoKUgpSClIKCgpSClIKUgoKClIKmgqaCAAsKgoSCgqaChIKCpoKEgoKmooKCgoKUgoKClJSmooKCgoKUgoKClJSmooKCgoKUgoKClJSmgqaCpoKmooKCgoLKooKCgoLKgtaC1oKmgqaCpoKosoKCgoKClIKUgpSClAAICuKWgpSkgoKUpKSkgoKUkoKClIKClJKCgpSCggAKCoKClISCgoKClIKWgoKCgoKogoKCgpSAkriCgoKClICSuIKCgoKUgJK4lIKCgpaCgoKCloKCgoKCuIKCgpSAggALCMKc5oKCgpSUgJSCgoKCooKClJLEkoKClJSCgoKUgpaAhKaCgISmgoCCAAsKsoSCgpSCgoKClJSCgpSCgpSCAA0IooSCkoKClICCpIKCloKUgIKmgoKUgoKUgqaClIKUgpSClILGxAAKDKKCgpSCgpSAgqSCgIKk+KKEgpKCgpSAgqSCgpSCgpSClIKClIKUgoKUgpSCgpSClIKUgoKUgpSCgpQACAaihIKUgoKCgpSCgpSCAAoIooKUgoKUgIKkgoKUgoLoooKUgoSCgpaCgpSCgpaCgpSClIKClIIACQiigpSClIKUgpSCxsQACwiigpSEgIL4ooKUgoSClIKUgpSCxsQAEAiCkgACEIKClKiysoSAgqaAlIKUpoCCpoKCuoKCgpaAgqaCAAgMgrKEgoKUkoKCgpSEgoKEgoKCpgANCKKChJKClKSCgqSCgoKUgoKkgoKCpoKCgpSCuJSClISWgoKUgIKkhIKClICCpKaigoKClIK4goSEgoKCgoKCgoKCgoK4goSEgoKCgoKCgoKCgoLoooSCgoCCuISCloIACRSCpoKElISmgoSCgoL2goKUgoKClIKCloKClIKmkoKClIKCqICCuIKClIK6goK8soIACBKCtoKClLa2goKUgoKUttqChKaigoKUloKCgoKWgoKUhIKCgoK+woKUtriCuIKCgoCCpICCpoKAgqaCgpaAgsiigoKUhIKClJSAgqaAgqaCgpaCgpSCgpSCAAoIooKCuJSUpKSCtIKClLaCgoKClIKCgpSClIKUgoKCuIKUgoKUgoKUgoK4qNKCgoKEgoKClIKApgAIEoKEgoLCptyUtNiCgoKUgoKClIKClIKCgpSCguiAgsiCgpSCAA8GgoSEgoQADiqCgoKAgoKUtvqClKSEgoKUgoKUgIKkgoKUgIIAHziChIKCgoKClIKClIKCgoK4gpSUlKaCpvqCgoKC+LSCgpSUgoKClJaCgpSCgpSEgoKCgpSCgpSCgIKkgpbWgoKCgpSCguiChIKClIKUgrqWpJKmuoKCgoL6goSEgoSCgoKUggAGEMKEhIKEgoSCgpSCloKClIK6koSEgoSChISCguiChISChIKCloKClIK6koSEhIKC6rKCgoKUlIKC6IKCgpSCgpSCgoKUAAsGooKCgoKUgoKUgoKUgoKUgoKUgoKUgoIACQiigoKAgqSCgpSClICC+IKWloCCuIKAgviCgoKCuKKCgoKUgoKClISCgpSCgpSCloKClILogoKClIKUgpSC+KKCgpSUgoKUhIKClAAJBoKEgoKCAAgMgoKUgoKUgoKClIKUgoKUgriCgpSEhIKAgqaCgpSCAAgIopSmgoKClIKWgoKWgoSCgpSAAAgIxoKClISCgqiC+KKChIKCgoKUlIKCloKCgpaCgpaCloKCgoKWgoKWgvqChISCuKTYgoSWgoKUgoKogIKkgoCCpICCpIKAgqSCgoCCpJSCgIKklIKCgoKClIKClJKCgriAgv6igpaCgoKClIKClIKClqKAgqS6hNaCgoKC+oKUloKCloKEgpaCgpaCgoK4goKCgpSCggAPGoCigKKAooCigKKAxoCigKKAuJKEgoKCgsqCgpSCgoKAgqT84sqUgpaCgoKClIKCgoKCsoKCgoLoggAMCLKUpKSkpKaWgpaCgoKCgoKClJSCloSCgoKygoKAgqSAgqSogryCgIKkgIKkAAkIgoKEgoKUgIKkgIKigqSUAAoKgoKCpoKEgoKUgpaCgoKClJSCuIKEgoKUgpaCgoKClJSC6IKEgoKUppaCgriClJSCuIKEgoKUgoKUgoKWgoKUgpaCgpaClIKCggAICriEgoKUgoKUgoKUgIKkgoKUgryigpSClKiShIKCgoKUgIKkgoKUgIKkpICCpICCpMiChIK6gIKClIKClIKClIKUgIKklAANCIKEhIKAgqaCgpSAgqqigoKUgpSCuoKCgqiCgriChIKClIKCloKCgpSmgoSCgtiCggAICoKClISCgIKkgIKmgoKClJSUguiC3oKAgqSCgpSC6IKEgoKAgqSCgpSCAAgIooKUgoKClJKSlIIACwrEgpSEgoKWgoSygoKCpgAEENSUgoLqsoKCgoKCgpSUgurCuoSAgqSAguioxIKUhIKClIKC0oKCxIKSkpKCgoKUpoK4goKCgpSCgqaCgoKUgoKCAA4KgpaCkoKAgqSCkoCCpICCgpaCgpSCgpSClJTKgIK87oKAgqSCgoCCpICCgpaCgpSCgpSClJTKgIIAFAyUloKCgpSEgIK4goKUgoCCuIKAgqSCgIK4goCCuIKClIKAgriCgpSCgIK4goKCgsyAgqSEgoKAgqSAooKWgoKUgoKUgoKUgqiCgpSCgpSCloKApKS0goKClIKolA==", "139-143:1;167-174:1;342-375:1;379-412:1;416-467:1;685-691:1;693-702:2;704-713:3;715-724:4;771-776:1;777-777:2;783-790:3;791-797:4;798-805:5;925-930:1;1139-1141:1;1142-1148:2;1152-1191:3;1196-1222:1;1347-1352:1;1690-1696:1;1708-1749:2;2478-2532:1;2556-2561:1;2571-2576:1;2610-2619:1;2695-2702:1;2750-2760:1;2782-2801:1;3010-3029:1;3132-3139:1;3193-3195:1;3217-3241:1;3243-3258:2;3260-3274:3;3293-3297:1;3354-3371:1;3400-3417:2;3513-3565:1")]
[assembly: go.GoPositionMap("os/os_windows_test.go", "os_windows_test.cs", "ACpMooKClICCpoKAgoLagoKEgoKUhIKCloKClIKClIKWgoKUgoKUggAMFIKChIKCgpSCgpSCgpSCgoKCgpaCgoKUgoKWgoKClICCgqSCgpSCgpaCgoKUgpSmlICCABEigoKCpoKCpoKmgqaCgoKmgqaCqJIACxjCgoKWgpSClISCguiigoKCgoKCgoKChIKCgoT2guyCgoIABhCCgoK4goKCyoKClLqUpqKCgpSCgoKUhISCgpSCgpSCgtaigoKCgoKCgoKClIKEgoKChNaigoKCgriCgpS6qIKEgoKUhIKClAAIFoKCgtyCgoKoAAkGooKClJSCgpSCgoKClIIACQjChISChIKCgoSCgpaCgpSCggABGAAJBAAKFoKCgpSUkoKCqISCgpSCgoKUgpaChIKClISCgpSCloKClIIADQiigIKmgoSChIKUloKCloKClICUyIKCgriihIKClJIACgaCgpSEgoKUguiCgoLoooSCgpSUgoKWgoKWgoKWguiigoSCgoKCgqiCgpSUgoKUhILogoSClIKClISAgqSAgsiigpKWgoKUgoKClIQABxKCgoKSkoKClIKClIKCgoKWgoKCgoKCgoKClKSUgqiCgpSCAA0QgoSCgoKClIKUgpSqwoKCgpSEgoKU3LKCgpSCgJKCAAgIooKUhIQADRiCgIKmgoKCgoKWADJmgoKCloKCgpSAkoIACAqkkoKClISCgpaCgoKUltiShIKClOaihIKClJSCgpaCgpSUgoKWguiChIKClIKClICCyIKEgoKUgIL+soKUhIKCgIKmgoCC/rKCgpaCgpau4oKEgoKAgqaEgoKClISCgpSSgIKkAAoQAAgCloKAgqaCgILKgoKUgoCCtoCCpISCgoKClLqAgqSEgoKkloKClJSCgpSCloKCloIACQqShIKC7taCgoKCptaCgoKWgoSCgpaCgpaCgoKCgoKClICS/KKCgoKClIIADAaCAAoggpKCgpSUgpSUgpSUgpSWkoKmgoKUgoKCgIK2gIK2goKCgpSUgoKUgpSCgpSUpoKCgIK2gIK2goKUggAIDKKogoKAgqSCgpSCgoKClIKCggAJCOqCgpaChIKCuoKCloKCloKCgpaClICmpoKUgIKkgIKkgKamgoKUguiigoKCqLqCgoKClISClIKClICCtoKClIKCgpSCuIKCuIKCgoKCpoKUguiCgoCC+IKCgoKUgoK4ooKAlIKCgoCCpICCpoKClIKogoKUgoKUgpSClIKogoKUgoKUgpSC", "47-52:1;278-283:1;289-294:2;304-310:3;377-383:1;407-409:2;414-419:3;423-428:4;512-517:1;713-715:1;740-784:2;742-755:2.1;1133-1137:1;1303-1364:1;1517-1523:1;1524-1526:2;1527-1529:3;1553-1553:1")]
[assembly: go.GoPositionMap("os/path_test.go", "path_test.cs", "ABUkwoSCgoKClJaCgqiCgoKUpoKClIKClIKogoKClIKClIKWgoKCgvqCgoSCgoCCpoKAgqaCgIL4gpSkgpaCgoKClIKUlA==")]
[assembly: go.GoPositionMap("os/path_windows_test.go", "path_windows_test.cs", "AB4kuIKClIK4ggA/jAGCgoSCgoKEgoKCgoKCyoKEgoKClICCpICCyIKChIKCgoKUlIKAgqaCgILIgoS6goKClLiUgoKAgraAgsiCgpaCgoKUpoKCloKClIKCgoKUgubGgoKCgoKUgoK4ooKCgpSUgILIpoCCyIKEgqaChIKmooKCgg==", "241-246:1")]
[assembly: go.GoPositionMap("os/pipe_test.go", "pipe_test.cs", "ACI8AAkWgoKUgIKmgpSmgoKClICCpICCpIIACwqilKaCgpSUpKSAgsbslrqCgpSAggAIEIKCgoKCgoKClICCgrSCooKkgqb8goKCgoKAgqKCooKkxICC+NqCpoKAgoCCxpaCgpSSuNiEgoKUlIKogoKUlIKigqSU+IKmguzSlKSCgoKCgoKAgraWgoSCgpSSkoKCgoKCggAKCIKEgoKCpqaChIKCpoKo0piigoKCgpaAgqSClMyChICCyIKmgoSCgpYAAhAACwqCgpSCkoCCpJaCwoSCgoKCgqiCgILogoKCgoKUloKCgur8goKUkpSCsoLGgoKCpAAIBsKEgoKUkpSEgoKCwoKCgoKAguiC0oK4grqCxg==", "194-209:1;306-321:1;364-369:1;372-388:2;422-425:1;450-458:1;461-475:2")]
[assembly: go.GoPositionMap("os/rawconn_test.go", "rawconn_test.cs", "AA8iooSCgpSSlIKClIKCloKCgpSClIKWgoKSgpSClIKUgpSC", "37-40:1;50-53:2")]
[assembly: go.GoPositionMap("os/read_test.go", "read_test.cs", "ABIegoKClIL4goSCgoKWgoKCltaihIKClJKEiICCpoKCloIACQjCgpSClJaCgpSChIKCgoKUgoKUgoKUguiChIKCgpaCgoKWgoKClLTGgpSC")]
[assembly: go.GoPositionMap("os/removeall_test.go", "removeall_test.cs", "ABsmooSCgIKmgoKCloKClIKAgqSAgriAgqSCgpSCgIKkgIK4gIKkgoKUgoKClIKAgqSAgriUgIKmgoKClJSAggAJFIKEgoCCyICCpICCAAgKkoKUhIKWgIKkgoKCgpSUgIKkgIIACAiipLSmgoKWgoKUhIKCqIKEgoKWgoKogoKWgoIACQiigoKUgoKUhIKCloKCloKC6IKEgoKCgIKkgIKkgoCCAAgMsoSCgoCCyoSAgqaAggALCrKUpoKWhIIAChamkoKClIKCppaCgIK2goKAgsqWgoK6gIKCgqamgoKCgqaCAAgMgpSmgpaEgoKAgqSAgqSAgvqygpSEgpaAgqSCgoKClLqAgqSohJSUgtyUloKClJSCggAMCIKCloKAgoCCpNyClICCpIKCzIKCgIKkgoKAgqSAgriCgoKCgpSCloCCyKKCgoKCgoKClIKAgg==", "301-312:1")]
[assembly: go.GoPositionMap("os/stat_test.go", "stat_test.cs", "ABkwxIKCgpSWgoKClISCgqaCuoKCgpSUgoKClISCloKCpoKogqaWgoKClJSCgoKUgoKCgqaCgpSEguqSgoKUgrqSgoKUgrqSgoKUgriCyqaCyqaCuIKUlKaCgoKCloKCgpSCloKCgpSCuKKCgoKUlIKCgpaCgoKWggAJCIKChIKCgIKkhIKAgqSCgoSCgIKkgoKmgoKEgoKAgqSEgoCCpIKChIKAgqSCgqiSgoSCgoCCpIKAgqSEhIKCgpSCgoKUguiCgpSCyoLKgviUlKaCgoKUgIKkgoKk")]
[assembly: go.GoPositionMap("os/tempfile_test.go", "tempfile_test.cs", "ABciooSCgpSEgoKCAAoIooTKgoKCgpSCgoKCAA0MooSCgpSEggAIGLKygoKkgoKUgrYAFAyihIKClgAEEoSigoKUhIKC2LKiggAIFJKC/MKEgoKUhIKCgIIADAiihIKClISCAAgYspKCgoKUgrY=", "80-95:1;118-129:1;132-135:2;144-147:3;191-203:1")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("os_test")]
public static partial class os_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct TestAddExtendedPrefix_type {}
    internal partial struct TestCreateTempBadPattern_tests {}
    internal partial struct TestCreateTempPattern_tests {}
    internal partial struct TestMkdirTempBadPattern_tests {}
    internal partial struct TestMkdirTemp_tests {}
    internal partial struct TestReadlink_tests {}
    internal partial struct TestRenameCaseDifference_tests {}
    [GoLocalName("test")] internal partial struct TestSeek_test {}
    [GoValueClone("detail")] internal partial struct _REPARSE_DATA_BUFFER {}
    internal partial struct dirLinkTest {}
    internal partial struct expandTestsᴛ1 {}
    internal partial struct isExistTest {}
    internal partial struct isPermissionTest {}
    internal partial struct myErrorIs {}
    internal partial struct namePosition {}
    internal partial struct nilFileMethodTestsᴛ1 {}
    internal partial struct openErrorTest {}
    internal partial struct randReader {}
    internal partial struct reparseData {}
    internal partial struct sysDir {}
    internal partial struct testStatAndLstatParams {}
    // </TypeAccessibility>

    // Go initializes an imported package before the importing package, for every import
    // form - not only the blank one. .NET would never load an assembly nothing has touched
    // yet, so each import that initializes anything is forced below: once per assembly, and
    // ahead of this package's own `init` functions, which this file being the first compile
    // item of the project guarantees.

    // <ImportInitializers>
    [GoInit] internal static void initᴛᴛimportꓸbufio() => builtin.initPackage(typeof(bufio_package));
    [GoInit] internal static void initᴛᴛimportꓸbytes() => builtin.initPackage(typeof(bytes_package));
    [GoInit] internal static void initᴛᴛimportꓸerrors() => builtin.initPackage(typeof(errors_package));
    [GoInit] internal static void initᴛᴛimportꓸflag() => builtin.initPackage(typeof(flag_package));
    [GoInit] internal static void initᴛᴛimportꓸfmt() => builtin.initPackage(typeof(fmt_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸgodebug() => builtin.initPackage(typeof(@internal.godebug_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸpoll() => builtin.initPackage(typeof(@internal.poll_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸsyscallꓸwindows() => builtin.initPackage(typeof(@internal.syscall.windows_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸsyscallꓸwindowsꓸregistry() => builtin.initPackage(typeof(@internal.syscall.windows.registry_package));
    [GoInit] internal static void initᴛᴛimportꓸinternalꓸtestenv() => builtin.initPackage(typeof(@internal.testenv_package));
    [GoInit] internal static void initᴛᴛimportꓸio() => builtin.initPackage(typeof(io_package));
    [GoInit] internal static void initᴛᴛimportꓸioꓸfs() => builtin.initPackage(typeof(go.io.fs_package));
    [GoInit] internal static void initᴛᴛimportꓸlog() => builtin.initPackage(typeof(log_package));
    [GoInit] internal static void initᴛᴛimportꓸmathꓸrandꓸv2() => builtin.initPackage(typeof(math.rand.rand_package));
    [GoInit] internal static void initᴛᴛimportꓸnet() => builtin.initPackage(typeof(net_package));
    [GoInit] internal static void initᴛᴛimportꓸos() => builtin.initPackage(typeof(os_package));
    [GoInit] internal static void initᴛᴛimportꓸosꓸexec() => builtin.initPackage(typeof(go.os.exec_package));
    [GoInit] internal static void initᴛᴛimportꓸosꓸsignal() => builtin.initPackage(typeof(go.os.signal_package));
    [GoInit] internal static void initᴛᴛimportꓸpathꓸfilepath() => builtin.initPackage(typeof(path.filepath_package));
    [GoInit] internal static void initᴛᴛimportꓸreflect() => builtin.initPackage(typeof(reflect_package));
    [GoInit] internal static void initᴛᴛimportꓸregexp() => builtin.initPackage(typeof(regexp_package));
    [GoInit] internal static void initᴛᴛimportꓸruntime() => builtin.initPackage(typeof(runtime_package));
    [GoInit] internal static void initᴛᴛimportꓸruntimeꓸdebug() => builtin.initPackage(typeof(go.runtime.debug_package));
    [GoInit] internal static void initᴛᴛimportꓸslices() => builtin.initPackage(typeof(slices_package));
    [GoInit] internal static void initᴛᴛimportꓸstrconv() => builtin.initPackage(typeof(strconv_package));
    [GoInit] internal static void initᴛᴛimportꓸstrings() => builtin.initPackage(typeof(strings_package));
    [GoInit] internal static void initᴛᴛimportꓸsync() => builtin.initPackage(typeof(sync_package));
    [GoInit] internal static void initᴛᴛimportꓸsyscall() => builtin.initPackage(typeof(syscall_package));
    [GoInit] internal static void initᴛᴛimportꓸtesting() => builtin.initPackage(typeof(testing_package));
    [GoInit] internal static void initᴛᴛimportꓸtestingꓸfstest() => builtin.initPackage(typeof(go.testing.fstest_package));
    [GoInit] internal static void initᴛᴛimportꓸtime() => builtin.initPackage(typeof(time_package));
    [GoInit] internal static void initᴛᴛimportꓸvendorꓸgolang_orgꓸxꓸnetꓸnettest() => builtin.initPackage(typeof(vendor.golang.org.x.net.nettest_package));
    // </ImportInitializers>
    // Go runs every `init` in the package under test - the production files' included -
    // before the first test. The production package is a REFERENCED assembly here, whose
    // module constructor .NET would not run until something in it is touched, so that
    // initialization is forced before anything else in this test module runs.
    [GoInit] internal static void initᴛᴛproduction() {
        builtin.initPackage(typeof(global::go.os_package));
    }
}
