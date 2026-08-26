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
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
// </ImportedTypeAliases>

using go;
using static go.@internal.poll_package;

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
[assembly: GoImplement<DeadlineExceededError, error>(Pointer = true)]
[assembly: GoImplement<errNetClosing, error>]
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
[assembly: go.GoPositionMap("internal/poll/errno_unix.go", "errno_unix.cs", "ABgqopSkpKSk")]
[assembly: go.GoPositionMap("internal/poll/fd.go", "fd.cs", "AB0wwKSAooAADh6SgpQADRzAooCigO6SgoKCgpSCgg==")]
[assembly: go.GoPositionMap("internal/poll/fd_fsync_darwin.go", "fd_fsync_darwin.cs", "ABIg0oCCpIKCuoKU")]
[assembly: go.GoPositionMap("internal/poll/fd_mutex.go", "fd_mutex.cs", "ADRqAA4CgoKClIKClILOooKCgqaCgqaCpoKClIKClM6igoKClIKCzqKigoKCgoKUgoKClIKCgpSClIKCuIKCpoKClAAEEMKigoKCgoKUgoKClIKCgqaCgpSCgpTMopjCgpSssoKUqsKClKyygrzCgpSssoI=")]
[assembly: go.GoPositionMap("internal/poll/fd_opendir_darwin.go", "fd_opendir_darwin.cs", "AA0e1oKClIKCgoKmgoKUrA==")]
[assembly: go.GoPositionMap("internal/poll/fd_poll_runtime.go", "fd_poll_runtime.cs", "ABooxJKSkpKSkpKSAAgQooKCgpSCpoKClIKokoKUpoKClIKmgqaC1oKClIKmgqaCpoKClKaCAAwYgpSkpKSkgqiSqJKokqbCgoKCgqaAgqSCgpSCAAUeAAwC")]
[assembly: go.GoPositionMap("internal/poll/fd_posix.go", "fd_posix.cs", "ABEgooKUqNKAgqSC2LKAgqSC3LKAgqSC3uKAgqSCggAFFPKCgoI=")]
[assembly: go.GoPositionMap("internal/poll/fd_unix.go", "fd_unix.cs", "ACpu8paClIKClIKmlKrmhISCgqrCggAGEKgABhCClqjSgIKkuIIADBbSgIKkgtyUgIKkgpSCgoKCgoCCyILq6ICCpIKUmIKCgqaClIKCqNKAgqSCgIKkgoKCgpSCgoCCyILq0oCCpIKAgqSCgoKClIKCgILIgurSgIKkgoCCpIKCgoKUgoKAgsiC6tKAgqSCgIKkgoKCgqaCgILIgurSgIKkgoCCpIKCgoKmgoCCyILq0oCCpIKAgqSCgoKCpoKAgsiC6tKAgqSCgIKkgoKCgpSCgtyUlIKUgoCCtoKUgvwACAiAgqSCgoKCgpSCgpSClIKUgpSC/NKAgqSCgIKkgoKClIKAgraClOrSgIKkgoCCpIKCgpSCgIK2gpTq0oCCpIKAgqSCgoKUgoCCtoKU6tKAgqSCgIKkgoKClIKAgraClOrSgIKkgoCCpIKCgpSCgIK2gpTq0oCCpIKAgqSCgoKUgoCCtoKU6tKAgqSEgIKkgoKClJSkgoCC3tTqsoCCpILcsoCCpIIADBKSgoKClJqk5qjSgIKkgtyyqNKAgqSC2NKAgqSCgIKkgoKUgIIACAzSgIKkgoCCpIKClICCAAgMkoKCgg==")]
[assembly: go.GoPositionMap("internal/poll/fd_unixjs.go", "fd_unixjs.cs", "AA8gpNzawoKCgoKUgtjSgIKkgtzygIKkgoKCgoKCgILa6tKAgqSC")]
[assembly: go.GoPositionMap("internal/poll/fd_writev_libc.go", "fd_writev_libc.cs", "AAse")]
[assembly: go.GoPositionMap("internal/poll/iovec_unix.go", "iovec_unix.cs", "AAoWgg==")]
[assembly: go.GoPositionMap("internal/poll/sendfile_bsd.go", "sendfile_bsd.cs", "AA4gAAgCgpSAgqSEgIKmgoKCgpSCkoKCgoLugswACBCUgpSAgraClII=")]
[assembly: go.GoPositionMap("internal/poll/sockopt.go", "sockopt.cs", "AAoY0oCCpILY4oCCpILY0oCCpILY0oCCpII=")]
[assembly: go.GoPositionMap("internal/poll/sockopt_unix.go", "sockopt_unix.cs", "AAoY0oCCpII=")]
[assembly: go.GoPositionMap("internal/poll/sockoptip.go", "sockoptip.cs", "AAoY0oCCpILY0oCCpII=")]
[assembly: go.GoPositionMap("internal/poll/sys_cloexec.go", "sys_cloexec.cs", "ABAk/IKClIKUgIKCpA==")]
[assembly: go.GoPositionMap("internal/poll/writev.go", "writev.cs", "ABIg4oCCpIKAgqaCgsqClJaCgoKCgoKUgoKClIKCpoKUgpSEgoKClIKCgoKCgpSCgIK2lIKCpg==")]
// </GoSourcePositionMaps>

namespace go.@internal;

[GoPackage("poll")]
public static partial class poll_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct fdMutex {}
    internal partial struct pollDesc {}
    public partial struct DeadlineExceededError {}
    public partial struct FD {}
    public partial struct String {}
    public partial struct SysFile {}
    public partial struct errNetClosing {}
    // </TypeAccessibility>
}
