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
global using runtimeꓸError = go.runtime_package.ΔError;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
// </ImportedTypeAliases>

using go;
using static go.time_package;

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
[assembly: GoTypeAlias("Hour", "const:ΔHour")]
[assembly: GoTypeAlias("Local", "const:ΔLocal")]
[assembly: GoTypeAlias("Location", "ΔLocation")]
[assembly: GoTypeAlias("Minute", "const:ΔMinute")]
[assembly: GoTypeAlias("Month", "ΔMonth")]
[assembly: GoTypeAlias("Nanosecond", "const:ΔNanosecond")]
[assembly: GoTypeAlias("Second", "const:ΔSecond")]
[assembly: GoTypeAlias("UTC", "const:ΔUTC")]
[assembly: GoTypeAlias("Weekday", "ΔWeekday")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<ParseError, error>(Pointer = true)]
[assembly: GoImplement<fileSizeError, error>]
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
[assembly: go.GoPositionMap("time/format.go", "format.cs", "AL8B7AKigpSCAAIeAAwCgoCkkoKUgtqSgoKUgqaC2pKUgsiSlKaSlKakgpSUgsimpqaSyJLIkpSClIKUgpSCyJKUgpSClIKUgsiSgoKCpoKCgpSC+gA0bqKCgoKUgoKCuKaCgoKmrLKCgoKogpSkqJKClIKogqiClKiCgoKCgpSC3uKCgoKUgoKClIKUrMSClKaCpoKClKqigoKClIKCgoKUgoKUgqYABRwACwKWgoKCgoKUkpKCgoKCgoKUgoKClKqigoKEgoKCgoK4lIKCgoKCgoKCgoKCgKSkAAIiAA8CgrSCAAIS4oKCgoKClJSCqrSUpKTIggAJGoKCgpSClJaCgqiClpSCgpSkpKSCpKSkpIKkpIKUpKSCgoKmpKSmgpKUpoKSlKSkpKSkgpS2gpTKooKUgoKCgoKUlIKClIKYkoKU2IKCmIKigpSUgrS2AA0gopKSAAoWgoKCgo7igoKCppSCgoKmgpSmgqiSgpyqsoKUgqyygpSCgpSUrLKSgpSClKaCgpSqooKCgpSCgpSClIKUAAJaACsEgoCCtgACEOSCgIK2AA8GopKCgoYAFh6CgoKCgoKUgoKUlIKCgrSCgpSCgoKUkpTWgoKUgrSCpIKkgoK4tKSClKiigqaoooK2goK2goLGgoKUgoKYooKClKaClILWgoKUgpSkpOaCgpSClKSk5oKCgpTEsoKCgpSCgpSkgoKUpIKClIKClKSCgpSUgoKUlKKCgpSCnLKUgpSCloLGtPiSgoKUgoKClMqCooKUgsaUmIKilIK0gpSCpoKkqIKCgoKCgqSmgpSCgoKUuIKUgoKUlIKUgrqCloKWgoKogoKCqIKCloKmgoKCgqiCgpSCgqgAAhoADAKCpoKmgoKmgoKClpKCgpSAgraUpJLIksakrLKCgpassoKClJaClIKUpoLW4oKClIKClICCpIKCuIKClMzigoKCgpSUlIKUpqzigoKCgoKClIKUlIKUgoKClIKUAA8m5IKCloKCgoK4gpSClIKqloKmgoKClJaCgoKCgpSUqIKCgoKmgpSCgoKClJSUgqaClKaCgqaClIKU")]
[assembly: go.GoPositionMap("time/format_rfc3339.go", "format_rfc3339.cs", "AAkkAAgCloKCgoKChJaCgoKCgoSCgpaCqIKCgpSUgoKC5oKCuoKUpIKSxqYACAqCkoKCgpSUgoKUqIKUgoKCgoKCgpSGkoKClIKogoKClIKCgpSCgpSWgIKUtqaCgoKCgsyCmKTUtJS05sY=")]
[assembly: go.GoPositionMap("time/sleep.go", "sleep.cs", "AAscAAUQAAgGgoIAECSuwoKUgqaUAAIUAAgGpgAQRgAVAoKUAAI0ABcCgoKCAAIwABcCgpSCqOwACSAACgKuwqaC")]
[assembly: go.GoPositionMap("time/sys_unix.go", "sys_unix.cs", "AAwg7IK4goKClKaCpoLWgoKClICCpIKCgoKUlJQ=")]
[assembly: go.GoPositionMap("time/time.go", "time.cs", "AKYB3gLSqJKClKiQppKCgoKCgriogoKklLqygpSCqJKCggADEMKCgoKUlAACENKClKiSgpSCgqiSgpSCgqqikoKUgoKmlKSkAAIQ0oKUABEskoKUgoIADCKSgpSCggBZ0gGiqqKUgpSCgoKUgqba4oKCpoKCgoKUlJSUgqjSgqiSgqiSgqiSgqiSqKSCrgAPFoKUgqaCgqiSAAIcAA8CgoKCgoKokqiSqJKqoqqiggAfTMaigqrUhIKCgpaGooKCgpSCpoK2hILmgsSClIKEloKWgoKCgqiCgoK6goKWrtSCgoKCgoKClJSCgpSqooKCgpSCgoKmqJCmkKaQAAIYAAoCgoKokoKCqJKCgqqigpSqogACEuKClIKCgoKUgIKklIKUgIKkqqKUpKTKkoKCgoKkgpSCgoKClJSmrsKClJSUpKTIgoKClIKUqqKUlKqilJQAAigAEQKCggAKHqIAAh4AEgSWgoLMgoKCuoKCzIKCgoSChIKWgpSWtoKC/IKCgoKClJaCggAVLIKClKyyloKCloKCloKCloKEqKoADBzCgoKCuJSmgqiSgqiSggACENKClIKokoKClKrSggACEAAIAoKCgpSCgpQAAhLiAAIQ0gACENIAAhLiAAgSkoKChIKUgoKCloKClJaCggAUIoKW+JKCgpaCgpbCgpSCloKGgoSCgoKWgoKEgqKClKYAAhDSqJKuwoKCgoKClNqigqaClIKCgq7CgoKClKqigoIAAhDSgoKCgoKCpqqiqqKokoKmgq7CgoKClIKCgpQAAioAEgKCqIKCloKCgpaWgoKoloKEzIKCpoKUloKCAAIU8oKClIIAAhYACAKCgpSCgpSs4oKCgpSCgoKCgqiWgriCggADEIKCgoKCkvKUgoKagqKUgoKClIKCgpSUgpSCgpS27oKU")]
[assembly: go.GoPositionMap("time/zoneinfo.go", "zoneinfo.cs", "AFy2AYKClIKUqqIABxCmooKCgoKCgqaUpoLuggACFAAOAoSCgoKCgoKWgIKCgoKCgqaCgoKCgoKUlIK6goKCgoKCgoKClKaCgoKUqIKAgrgAAiQAEASCqIKCgsyCgrqqooKCpgAFEgAIArqCgpSCzISUloKCgpSCpoKWlKaClISSgoKUgoKCloSWgoKEgoK4goKCgt6CpJS8ooKUgoKUgpS2gpSUgoKmvtKClIKCpIKaooKClIKCgpSWgoKClIKCgpSWgoKClISClAARLqKCgpSCgoKCgpSCpIKCgpaCgoKUgoKClIKCgpSCgoKUgpaCgpaCgpSErNKClIKCgoKUlIKCgqaClKyygpSCgramgoKSlIKCgoKYgqKUgoKUlIKClKas8gAGEIKCgoKCzIKCgroADjQADwKClIKUppSCkpSCgoCCgIKkpLaAgqSkqJKClIKCpg==")]
[assembly: go.GoPositionMap("time/zoneinfo_goroot.go", "zoneinfo_goroot.cs", "AAgSgoKU")]
[assembly: go.GoPositionMap("time/zoneinfo_read.go", "zoneinfo_read.cs", "ABIwwgAPIIIADyCCgoKClIKCpoKCgoKUpoKCgoKClKaCgoKClKiSgoKokoCCpAAEEsKWgIKokoKAgpSUtLS0AAQWABEQgoKCgpSClAAFEIKUAAEQgoSWgoKClIKUqIKCqJaWlpaoqISSloKCgsyCppSCgoKCgIKkgpSCgoCCpIKAgqSCppTMgoKCgoCClLaAgpS2goKUgoKUgqimqKiCgoKCgoKCyICygpSAgpQACRCopoKCgqaqooKUgpQAAhgACQKClKiSgpSqwoKClIQABBKCgIKkgoKEgoCCpgAXMIKUgoKCgoKCgoKClIIAESaCgIqkhIKAgqaWAAoYwoKUrvKCgoKAgraCpoKCgoCCtoKmgIKCgoCCtoK2gpSu4oKClIKqgoKClIKUgqY=")]
[assembly: go.GoPositionMap("time/zoneinfo_unix.go", "zoneinfo_unix.cs", "ABw4AAYQgpSCgoKCxoKUgoCCgoKUlMaAgoL8")]
// </GoSourcePositionMaps>

namespace go;

[GoPackage("time")]
public static partial class time_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct dataIO {}
    internal partial struct fileSizeError {}
    internal partial struct rule {}
    internal partial struct ruleKind {}
    internal partial struct zone {}
    internal partial struct zoneTrans {}
    public partial struct Duration {}
    public partial struct ParseError {}
    public partial struct Time {}
    public partial struct Timer {}
    public partial struct ΔLocation {}
    public partial struct ΔMonth {}
    public partial struct ΔWeekday {}
    // </TypeAccessibility>
}
