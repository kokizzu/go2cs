// go2cs code converter defines `global using` statements here for imported type
// aliases as package references are encountered via `import' statements. Exported
// type aliases that need a `global using` declaration will be loaded from the
// referenced package by parsing its 'package_info.cs' source file and reading its
// defined `GoTypeAlias` attributes.

// Package name separator "dot" used in imported type aliases is extended Unicode
// character '\uA4F8' which is a valid character in a C# identifier name. This is
// used to simulate Go's package level type aliases since C# does not yet support
// importing type aliases at a namespace level.

// <ImportedTypeAliases>
global using execꓸError = go.os.exec_package.ΔError;
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using tokenꓸFile = go.go.token_package.ΔFile;
global using tokenꓸPos = go.go.token_package.ΔPos;
global using tokenꓸPosition = go.go.token_package.ΔPosition;
// </ImportedTypeAliases>

using go;
using static go.@internal.fuzz_package;

// For encountered type alias declarations, e.g., `type Table = map[string]int`,
// go2cs code converter will generate a `global using` statement for the alias in
// the converted source, e.g.: `global using Table = go.map<go.@string, nint>;`.
// Although scope of `global using` is available to all files in the project, all
// converted Go code for the project targets the same package, so `global using`
// statements will effectively have package level scope.

// Additionally, `GoTypeAlias` attributes will be generated here for exported type
// aliases. This allows the type alias to be imported and used from other packages
// when referenced.

// <ExportedTypeAliases>
[assembly: GoDynamicTypeLift("7374727563747b506172656e7420737472696e673b205061746820737472696e673b2044617461205b5d627974653b2056616c756573205b5d616e793b2047656e65726174696f6e20696e743b2049735365656420626f6f6c7d", "CorpusEntryᴛ1")]
[assembly: GoTypeAlias("CorpusEntry", "go.@internal.fuzz_package.CorpusEntryᴛ1")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<MalformedCorpusError, error>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<contextReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<crashError, error>(Pointer = true)]
[assembly: GoImplement<os_package.File, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<pcgRand, mutatorRand>(Pointer = true)]
[assembly: GoImplement<syscall_package.ΔSignal, os_package.ΔSignal>]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<mutator, ж<mutator>>]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: global::go.GoPositionMap("internal/fuzz/counters_supported.go", "counters_supported.cs", "AAoiwoKC")]
[assembly: global::go.GoPositionMap("internal/fuzz/coverage.go", "coverage.cs", "ABccooKuwoKigoKCgryigpSCgoKCpoKUgoKUqqKCgpSqooKCpqqigoKmpoKCgpQ=")]
[assembly: global::go.GoPositionMap("internal/fuzz/encoding.go", "encoding.cs", "AEgwwoKUpoIACgS0AAwaAAoWxoKUxgADHAANApTK5NS2qJKClIKClIKClIKigoKUgoKUlAAKBoKCgoKUgoKUgpSEgIKClIKClIKClIKClKaCgIKCgpSUpKS2goKUgoKClIKklMqYgIKUgpaCtJKUgpSUxKaUtIKUxLiApIKUtIKUpLaClIKClIKClIKUgpSkgpSkgpSkgpSCpIKUtIKUgoKUpIKUgoKUpNqSntKkgqSCpIKktMqSlIKkgqSCpIKktA==")]
[assembly: global::go.GoPositionMap("internal/fuzz/fuzz.go", "fuzz.cs", "AIkBqgEADgKAgqSClIKUlJaCgpaCgoK4gpKWkoKygoKClKi4lIKUgpSCgrqCooKUgoKClIIABxKCgoKEgoKCgoKCpoKCsoKClIKClN6CgpKUgpSCloKCgpaCgoKW+LiCgrLakpSEgoKCgoKUgqaClMqCgsiCgoLKggAHEraCggAGEIKCgoKC/gAIEqamgoKCgpSCgrqUgoKCggALGoIACxaCgoKCAAgUuLYAFiCCpoKmggAIGOKCsoKClIKCgpSCgILalIKUABlCooKWAIEBxgKUgoKmAAgSgIKkgoKCgrqCgriCgqaCgqaUhIKCgoKUgoKClqaCgoKmgoKCgoKUtpSCgoKUpoIAAhoACgKmlIKmlJaCgpTKgpSmgpaCgoKUgoKmqJKCqqKCvKKCAAYQgpbKqqKmlIKClISClIKkgpSCgriCgoKmqqKCAAIgAA0CrLKClIKCgoKUqqKoggACENKAgqSCgoCm7ICCpAAJFIKCgpSuwoKCpJSCgtyClIKCgpSCgoKClJSClKaCgoKUgIKkqqKClIKClIKCpq7igoKAgqSAgoKkpoKmgoKCpgAYMoKmooI=", "119-144:1;149-164:2;184-194:3")]
[assembly: global::go.GoPositionMap("internal/fuzz/mem.go", "mem.cs", "ADF2tNr0goKUooKCuoKAgriC6JKqooKCqJKCrLKCgpSCAAIS4oKClA==", "72-77:1")]
[assembly: global::go.GoPositionMap("internal/fuzz/minimize.go", "minimize.cs", "AAkWgqai3JaCgoKUgoKmuoKClIKCgoKmgqaogoKCgpSCgoKmgoLMgoKCloKClKY=")]
[assembly: global::go.GoPositionMap("internal/fuzz/mutator.go", "mutator.cs", "ABgmgqaCpoKClKqigKSkpNoACRCogpS0tLS0tLS0tLS0gsbExIKUgpSClIK0gpSClIKUgsTIgoKCgpaSlJSUgqaSlJSUgtqCgoKClpKUlJaCppKUlJSC2oKCgpaSlIKUlIKmkpSClJSCpoKSlIKUlIKmkpSCAB040pKCgpSWgoKAgoIADyCCgpSC", "259-264:1")]
[assembly: global::go.GoPositionMap("internal/fuzz/mutators_byteslice.go", "mutators_byteslice.cs", "AAoQsoKUgoKCgqrCgoKClIKCgpSqwoKUgoKClLiClKamppSCgqiygpSCgoKUgoKosoKUgoKosoKUuIKosoKUgoKClIKosoKUgoKClJSosoKUgoKUgoKCqLKClIKClIKCgqiygpSCgpSCgoKqwoKUgoKqwoKUgoKCqsKClIKCgqiygpQACBKCgpSCgoKClKiygpSCgoKClKiygpSCgoLKgoKUqLKClIKCgsqCgpSmgsqClIKCgoKCgg==")]
[assembly: global::go.GoPositionMap("internal/fuzz/pcg.go", "pcg.cs", "ADZagoKCgpKClKaokoKCgIKkgoKCgoKCpoKCprKCpoKCqJKCgqqigpQAAhDSgoKCgoKCgoKmqJKokgAKFrI=")]
[assembly: global::go.GoPositionMap("internal/fuzz/queue.go", "queue.cs", "ABcugqaCgoKClIKCgpSCpoKClIKCpoKClIKCgoKmgoKUpoI=")]
[assembly: global::go.GoPositionMap("internal/fuzz/sys_windows.go", "sys_windows.cs", "ACAm4oKCugAMEIKoggAKDoKCloIACxSosriClIKCpqqigoKCgoKCggAICLKCgpSigIKmgoKCgoKUgoKUgoKClIKEpqaokqiS", "20-24:1")]
[assembly: global::go.GoPositionMap("internal/fuzz/trace.go", "trace.cs", "AAowAAwCoqKkoqKipKI=")]
[assembly: global::go.GoPositionMap("internal/fuzz/worker.go", "worker.cs", "ADt+goKClIKCAAoWkoKClIIAAhoADQSUgoCCuNaCkpSogpKUAAoWlICmqMr8goKUgpSUppSA3KSmyoKUAAkUuJLK3IKmggAHEgAFEgAKAoKCgqbKgpSC3AAHEAAIFIKWAA4WggACFgAKAoKUgIKkgIKCgpSUpqQAAhoADwKClIKChIKCAAgUgoKUkoKCgpSSloCCgoLcgoKCgoSCgpYABhgACwKClKakloKCggAIDoKSgpaCuJaCgraCgoKCqIKWgoKogoKoAAwgAAoCkoKUxoKUkoKC6ACBAa4CAAwCgoKCgoCCgpS4gpS0tLSmgIIAEzYAEAKCgoKUlJKQlIKCgqSCgpKClIKCloKCgpSChKaygoKCgoKClJSClJaCgoKClIKClJaCpKSCgpSEgoKCgpSCgoKUggAKDNKSkJKCkJKCgpSCgoKCyoKCgoKCgoLugpS4AAUQAAkCkpKCgqiCzIKCgoK2lMzCgpS0tLSCgoKCgoKCuJSmgpSClJS0xKSmooKqogAQJIKs8oKogIKCyoCCgqQADhwACwKChJKClICSgoKClIKCgoKUgoKWgoKCgoKCloKClMqUtLS0lJaUgoKCuoKCgqaCgoK4goKCAAgIAAsCgoSCgpSCgoKClIKEgoKClIKClJCShIKUhIKCgpSClIKCpoSCgtymqOjSgoKCgtqigoKAgqQADyCigIKkiKKCooKWtKQ=", "386-389:1;426-429:1;491-499:1;492-494:1.1;715-715:1;724-727:2;741-743:3;744-759:4;805-805:1;807-807:2;855-858:1;881-912:2;996-996:1;1100-1100:1;1184-1187:1")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("fuzz")]
public static partial class fuzz_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface mutatorRand {}
    internal partial struct call {}
    internal partial struct contextReader {}
    internal partial struct coordinator {}
    internal partial struct corpus {}
    internal partial struct crashError {}
    internal partial struct fuzzInput {}
    internal partial struct fuzzMinimizeInput {}
    internal partial struct fuzzResponse {}
    internal partial struct fuzzResult {}
    internal partial struct minimizeResponse {}
    internal partial struct mutator {}
    internal partial struct noCopy {}
    internal partial struct pcgRand {}
    internal partial struct pingResponse {}
    internal partial struct queue {}
    internal partial struct sharedMem {}
    internal partial struct sharedMemHeader {}
    internal partial struct sharedMemSys {}
    internal partial struct worker {}
    internal partial struct workerClient {}
    internal partial struct workerComm {}
    internal partial struct workerServer {}
    public partial struct CoordinateFuzzingOpts {}
    public partial struct CorpusEntryᴛ1 {}
    public partial struct MalformedCorpusError {}
    public partial struct fuzzArgs {}
    public partial struct minimizeArgs {}
    public partial struct pingArgs {}
    // </TypeAccessibility>
}
