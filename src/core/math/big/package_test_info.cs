// go2cs metadata anchor for a production-reference test project: the test assembly
// REFERENCES the colocated production project instead of
// recompiling its sources, so the production assembly is the single identity for the
// production types and no production class partial may be declared here. The first —
// and only — class is the test metadata class the go2cs-gen generators anchor
// generated adapters and partials to.
global using static global::go.math.big_package;
global using static global::go.math.big_internal_test_package;

// <ImportedTypeAliases>
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using execꓸError = go.os.exec_package.ΔError;
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using xmlꓸToken = object;
global using xmlꓸΔToken = object;
using rand = go.math.rand_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static global::go.math.big_test_package;

// <ExportedTypeAliases>
[assembly: GoTypeAlias("Bits", "ΔBits")]
[assembly: GoTypeAlias("Int", "ΔInt")]
[assembly: GoTypeAlias("Rat", "ΔRat")]
// </ExportedTypeAliases>

// <InterfaceImplementations>
[assembly: GoImplement<bytes_package.Buffer, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.ByteScanner>(Pointer = true)]
[assembly: GoImplement<bytes_package.Reader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<global::go.math.big_package.ErrNaN, error>]
[assembly: GoImplement<go.math.rand_package.Rand, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<strings_package.Reader, io_package.ByteScanner>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<global::go.math.big_package.ΔInt, ж<global::go.math.big_package.ΔInt>>(Indirect = true)]
[assembly: GoImplicitConv<global::go.math.big_package.ΔRat, ж<global::go.math.big_package.ΔRat>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("math/big/alias_test.go", "alias_test.cs", "ADYggu6igoKCpqKCgpTuooKClIKU7oKC7qKCgpTuou6iAAIYAAwCloCCuICCgrgAAhwADwKWgKakuIKAgoK2goCCgtyCgoCCpLiAgoK2goCCgrimggAbIrq6AAQUgqKCuIKigrgAEEwABBAABxySkpSkpKSkgKY=", "183-185:1;186-188:2;189-191:3;192-194:4;195-197:5;198-202:6;199-201:6.1;203-207:7;204-206:7.1;208-212:8;209-211:8.1;213-217:9;214-216:9.1;218-224:10;220-223:10.1;225-231:11;227-230:11.1;232-236:12;233-235:12.1;237-239:13;240-242:14;243-245:15;246-248:16;249-251:17;252-254:18;255-257:19;258-260:20;261-266:21;262-265:21.1;267-269:22;270-274:23;271-273:23.1;275-277:24;278-282:25;279-281:25.1;283-285:26;286-288:27;289-291:28;293-310:29")]
[assembly: go.GoPositionMap("math/big/example_rat_test.go", "example_rat_test.cs", "ABIoAAgCgoKUloK6hKyygro=")]
// </GoSourcePositionMaps>

namespace go.math;

[GoPackage("big_test")]
public static partial class big_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct bigInt {}
    internal partial struct notZeroInt {}
    internal partial struct positiveInt {}
    internal partial struct prime {}
    internal partial struct smallUint {}
    internal partial struct zeroOrOne {}
    // </TypeAccessibility>
}
