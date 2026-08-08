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
// </ImportedTypeAliases>

using go;
using static go.syscall_package;

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
[assembly: GoTypeAlias("Signal", "ΔSignal")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<Errno, error>]
[assembly: GoImplement<SockaddrInet4, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrInet6, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrLinklayer, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrNetlink, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrUnix, Sockaddr>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<WaitStatus, ΔSignal>(Inverted = true, ValueType = "WaitStatus")]
[assembly: GoImplicitConv<_C_int, WaitStatus>(Inverted = true, ValueType = "_C_int")]
// </ImplicitConversions>

namespace go;

[GoPackage("syscall")]
public static partial class syscall_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct _C_int {}
    internal partial struct _C_long {}
    internal partial struct _C_long_long {}
    internal partial struct _C_short {}
    internal partial struct _Gid_t {}
    internal partial struct _Socklen {}
    internal partial struct capData {}
    internal partial struct capHeader {}
    internal partial struct caps {}
    internal partial struct cloneArgs {}
    internal partial struct iflags {}
    internal partial struct mmapper {}
    internal partial struct pollFd {}
    public partial interface Conn {}
    public partial interface RawConn {}
    public partial interface Sockaddr {}
    public partial struct Cmsghdr {}
    public partial struct Credential {}
    public partial struct Dirent {}
    public partial struct EpollEvent {}
    public partial struct Errno {}
    public partial struct FdSet {}
    public partial struct Flock_t {}
    public partial struct Fsid {}
    public partial struct ICMPv6Filter {}
    public partial struct IPMreq {}
    public partial struct IPMreqn {}
    public partial struct IPv6MTUInfo {}
    public partial struct IPv6Mreq {}
    public partial struct IfAddrmsg {}
    public partial struct IfInfomsg {}
    public partial struct Inet4Pktinfo {}
    public partial struct Inet6Pktinfo {}
    public partial struct InotifyEvent {}
    public partial struct Iovec {}
    public partial struct Linger {}
    public partial struct Msghdr {}
    public partial struct NetlinkMessage {}
    public partial struct NetlinkRouteAttr {}
    public partial struct NetlinkRouteRequest {}
    public partial struct NlAttr {}
    public partial struct NlMsgerr {}
    public partial struct NlMsghdr {}
    public partial struct ProcAttr {}
    public partial struct PtraceRegs {}
    public partial struct RawSockaddr {}
    public partial struct RawSockaddrAny {}
    public partial struct RawSockaddrInet4 {}
    public partial struct RawSockaddrInet6 {}
    public partial struct RawSockaddrLinklayer {}
    public partial struct RawSockaddrNetlink {}
    public partial struct RawSockaddrUnix {}
    public partial struct Rlimit {}
    public partial struct RtAttr {}
    public partial struct RtGenmsg {}
    public partial struct RtMsg {}
    public partial struct RtNexthop {}
    public partial struct Rusage {}
    public partial struct SockFilter {}
    public partial struct SockFprog {}
    public partial struct SockaddrInet4 {}
    public partial struct SockaddrInet6 {}
    public partial struct SockaddrLinklayer {}
    public partial struct SockaddrNetlink {}
    public partial struct SockaddrUnix {}
    public partial struct SocketControlMessage {}
    public partial struct Stat_t {}
    public partial struct Statfs_t {}
    public partial struct SysProcAttr {}
    public partial struct SysProcIDMap {}
    public partial struct Sysinfo_t {}
    public partial struct TCPInfo {}
    public partial struct Termios {}
    public partial struct Time_t {}
    public partial struct Timespec {}
    public partial struct Timeval {}
    public partial struct Timex {}
    public partial struct Tms {}
    public partial struct Ucred {}
    public partial struct Ustat_t {}
    public partial struct Utimbuf {}
    public partial struct Utsname {}
    public partial struct WaitStatus {}
    public partial struct ΔSignal {}
    // </TypeAccessibility>
}
