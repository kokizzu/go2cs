// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.math.rand_package;
global using static global::go.math.rand_internal_test_package;

// <ImportedTypeAliases>
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
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
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.math.rand_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<go.math.rand_package.Rand, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<statsResults, ж<statsResults>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("math/rand/auto_test.go", "auto_test.cs", "AAsg2LKC3oKCgoKCgoI=")]
[assembly: go.GoPositionMap("math/rand/default_test.go", "default_test.cs", "ABkoyIKWgoCCgqaEgoKCgoKClIKCgoKUgoKUggAFFOKCgpaCgpaCAAgWloKSooLWgoKyggAICoKSsoIACAyCgqKigtaCgrKCAAgItpKCloKCgpQ=")]
[assembly: go.GoPositionMap("math/rand/race_test.go", "race_test.cs", "AAwewpiCgoKCooKCgoKCgoKCgoKCgoKClIKClA==")]
[assembly: go.GoPositionMap("math/rand/rand_test.go", "rand_test.cs", "AC9KgoKSlM7CgoKClIKCgpSmgoKSgoKUgoKmgoKCgoK4goKCgoKCgpSUAAMQsoKCgpSmloKClpaWqpKCuIKCgoKClIKCgoKCAAYWsoKCgpSmlpKUgoKWlpaqkoK4goKCgoIABRTygqyCgoSCgoKCgoKCgoKCgoKUpsKCrIKChIKCgoKCgoKCgoKCgpSssoKClJSCgqassoKClJSCgqamgoKAgqSAgqSAgsiCgoCCpICCpICCAAgIgpSq1NaUuIKWgoKCgsqCgoKCgpSCmAAIDJaCgqamgqaCgsqCgoKCgpSCuIKCgoKClIKCgoKUgriCgoKCgpSCgoKClIK4lIKSgM7EgoKCgsqCgoKClPiSgoKClJKkgoKoggANEoKUkJKostqCgpSChJKCgqaCgoKClIKokoKCgoIABhKygriCgoLKooKCuKKCgriigoK4ooKCuKKCgriigoK4ooKCuKKCgriigoKCgpSQzMKCgoKC3KKCgoKCuKKCgoKCuKKCgoKCuIKCgoKCooKC6A==")]
[assembly: go.GoPositionMap("math/rand/regress_test.go", "regress_test.cs", "ABswooKCgoKEgoKCgpSCgoKCgpSCgoKCgoKqgoKUgoKCgpSClLampoKCAAM1AAI8gpaCgoKUgpSCgoKUpJSUlIKClIKmpoI=")]
// </GoSourcePositionMaps>

namespace go.math;

[GoPackage("rand_test")]
public static partial class rand_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct statsResults {}
    public partial struct TestUniformFactorial_tests {}
    // </TypeAccessibility>
}
