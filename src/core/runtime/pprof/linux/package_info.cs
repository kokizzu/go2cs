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
global using runtimeꓸError = go.runtime_package.ΔError;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static go.runtime.pprof_package;

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
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<bufio_package.Writer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<go.text.tabwriter_package.Writer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<keysByCount, sort_package.Interface>(Pointer = true)]
[assembly: GoImplement<runtimeProfile, countProfile>(Pointer = true)]
[assembly: GoImplement<stackProfile, countProfile>]
[assembly: GoImplement<strings_package.Builder, io_package.Writer>(Pointer = true)]
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
[assembly: go.GoPositionMap("runtime/pprof/elf.go", "elf.cs", "ABAqwoKCgpSUgIK4gpaClKS01oKSlKSCgpKUtIKCkpS2goCCpICSpJKUgqaClIKCgJKkgoKCgoKSlIKUgIKkpg==")]
[assembly: go.GoPositionMap("runtime/pprof/label.go", "label.cs", "ABs2goKClAAEFMKClISCloSqooLIgpSClAACEgAIAoKUgoKUqqKCgqqigoKC")]
[assembly: go.GoPositionMap("runtime/pprof/map.go", "map.cs", "ABw4lIKCgpSChpKCgoKUgoK4goKClMiClIKCgoSCpoKEgpSClIKCgpSClA==")]
[assembly: go.GoPositionMap("runtime/pprof/pe.go", "pe.cs", "AAwawoKClA==")]
[assembly: go.GoPositionMap("runtime/pprof/pprof.go", "pprof.cs", "AOwBwgOCgpQAChaCAAISAAgCgoKClIKUuILYsoKC2LKChIKClobYkqjSgoKClAAFKAAVAoKUgpaCgoKUloKCgpTa4oKCAAUkABECgpSCqIKCgpSWhMqAooCigAAMHrKCgoKmgoKCpu7UgoKCgoSEgoKCgoKmgoKUgtqkkoKCgoKUgoKUlIKCgoKCgoKClJaElIKCgoKUqIKCgoSCgoKmgoKCgpKCuJSCAAgSgKKAooKSkoKU2qKCgoKCgoKCyIKUgqamgpSuwqiSgqiS2qKmgoKmggABFPKCuIKCgoK6gpaIgoKEgoKCgoKCzAAGEIKCgpa6goKmgpSCuoKCgoKCgoKEgoKCgoKEgoKCgoKEgoKCgoKCgpaEgqiSgtjIgrqS2JKClKa4goKCgoKUlJSUgqaO4oKEuIKCgoKCugAHEICigKKAAAsmAA0UAAkEgoKCpoKUgoKCAAUQ5IKCgoKCgoCCpIKmppSCrNKChIKUgoLYkoKokoLYktiS6JKCgoKCgoKCqIiCloKChIKCgpSCgoKCgoKClIKCqIKUqKampqampqY=")]
[assembly: go.GoPositionMap("runtime/pprof/pprof_rusage.go", "pprof_rusage.cs", "AA0ikoKUpKSkpoKCgg==")]
[assembly: go.GoPositionMap("runtime/pprof/proto.go", "proto.cs", "ABQusABS3AGigoKCgpSmgoKCgrqSgoKCqJKCgoKClIKokoKCgoKokoKCgqiSgoKCgoKC7oKUpsqCgqaWgoKWppSCgoKUAA8swoIACBKCrsKUgpSCuIKCpgAQJIKClIKUgpSCgoKChJSC7paClOiyhIKSgoKCgpaChJKChIKCkoK6hJaCgryCggACGAALApaEgoKAAAsYgoCCgsqAgrjegqaCkoCCpIKWgIKC2oCCuICCgpSCtoCSpAA4dIKCgoKCrLKAppKSkpSSlpKUkraCgoKCgpQACw4ACAKClISCio6C3oKCgsSCgoKCgu6UgoKEgoKCppaCgoKCgoKCloIADAwAFCymgoKCgpaCgoKCgpSCgpSCgpSClJSCgpSCgoKUloKCgpbKAAgWgriCpoI=")]
[assembly: go.GoPositionMap("runtime/pprof/proto_other.go", "proto_other.cs", "AA4i0oKCku6y")]
[assembly: go.GoPositionMap("runtime/pprof/protobuf.go", "protobuf.cs", "AA4cgoKClKaCgqaUgqaClIKClIKCgoKCgpSCuIKClKaCgqaCgpSmgpSCgpSCgoKCgoKUgriCgqaCgriCgpSmgoKUuIKClMqCgqaCgoKCgoKCgg==")]
[assembly: go.GoPositionMap("runtime/pprof/protomem.go", "protomem.cs", "ABUgkoKCgoKCgoKCloKCooKCuIKCgIK2gqaCgpSWgoKCgpSSgriCAAIYAAkCgpamloKE")]
[assembly: go.GoPositionMap("runtime/pprof/runtime.go", "runtime.cs", "AAsgysampqqyggACFAAJAoKCgg==")]
// </GoSourcePositionMaps>

namespace go.runtime;

[GoPackage("pprof")]
public static partial class pprof_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface countProfile {}
    internal partial struct cpuᴛ1 {}
    [GoLocalName("newFunc")] internal partial struct emitLocation_newFunc {}
    internal partial struct keysByCount {}
    internal partial struct label {}
    internal partial struct labelContextKey {}
    internal partial struct labelMap {}
    internal partial struct locInfo {}
    internal partial struct memMap {}
    internal partial struct msgOffset {}
    internal partial struct pcDeck {}
    internal partial struct profMap {}
    internal partial struct profMapEntry {}
    [GoValueClone("pb")] internal partial struct profileBuilder {}
    internal partial struct profilesᴛ1 {}
    [GoValueClone("tmp")] internal partial struct protobuf {}
    internal partial struct runtimeProfile {}
    internal partial struct stackProfile {}
    internal partial struct symbolizeFlag {}
    public partial struct LabelSet {}
    public partial struct Profile {}
    // </TypeAccessibility>
}
