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
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<Errno, error>]
[assembly: GoImplement<SockaddrInet4, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrInet6, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrLinklayer, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrNetlink, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrUnix, Sockaddr>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<WaitStatus, ΔSignal>(Inverted = true, ValueType = "uint32")]
[assembly: GoImplicitConv<_C_int, WaitStatus>(Inverted = true, ValueType = "int32")]
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
    internal partial struct capData {}
    internal partial struct capHeader {}
    [GoValueClone("data")] internal partial struct caps {}
    internal partial struct cloneArgs {}
    [GoValueClone("name")] internal partial struct iflags {}
    internal partial struct mmapper {}
    internal partial struct pollFd {}
    public partial interface Conn {}
    public partial interface RawConn {}
    public partial interface Sockaddr {}
    public partial struct Cmsghdr {}
    public partial struct Credential {}
    [GoValueClone("Name", "Pad_cgo_0")] public partial struct Dirent {}
    public partial struct EpollEvent {}
    public partial struct Errno {}
    [GoValueClone("Bits")] public partial struct FdSet {}
    [GoValueClone("Pad_cgo_0", "Pad_cgo_1")] public partial struct Flock_t {}
    [GoValueClone("X__val")] public partial struct Fsid {}
    [GoValueClone("Data")] public partial struct ICMPv6Filter {}
    [GoValueClone("Multiaddr", "Interface")] public partial struct IPMreq {}
    [GoValueClone("Multiaddr", "Address")] public partial struct IPMreqn {}
    [GoValueClone("Addr")] public partial struct IPv6MTUInfo {}
    [GoValueClone("Multiaddr")] public partial struct IPv6Mreq {}
    public partial struct IfAddrmsg {}
    public partial struct IfInfomsg {}
    [GoValueClone("Spec_dst", "Addr")] public partial struct Inet4Pktinfo {}
    [GoValueClone("Addr")] public partial struct Inet6Pktinfo {}
    [GoValueClone("Name")] public partial struct InotifyEvent {}
    public partial struct Iovec {}
    public partial struct Linger {}
    [GoValueClone("Pad_cgo_0", "Pad_cgo_1")] public partial struct Msghdr {}
    public partial struct NetlinkMessage {}
    public partial struct NetlinkRouteAttr {}
    public partial struct NetlinkRouteRequest {}
    public partial struct NlAttr {}
    public partial struct NlMsgerr {}
    public partial struct NlMsghdr {}
    public partial struct ProcAttr {}
    public partial struct PtraceRegs {}
    [GoValueClone("Data")] public partial struct RawSockaddr {}
    [GoValueClone("Addr", "Pad")] public partial struct RawSockaddrAny {}
    [GoValueClone("Addr", "Zero")] public partial struct RawSockaddrInet4 {}
    [GoValueClone("Addr")] public partial struct RawSockaddrInet6 {}
    [GoValueClone("Addr")] public partial struct RawSockaddrLinklayer {}
    public partial struct RawSockaddrNetlink {}
    [GoValueClone("Path")] public partial struct RawSockaddrUnix {}
    public partial struct Rlimit {}
    public partial struct RtAttr {}
    public partial struct RtGenmsg {}
    public partial struct RtMsg {}
    public partial struct RtNexthop {}
    public partial struct Rusage {}
    public partial struct SockFilter {}
    [GoValueClone("Pad_cgo_0")] public partial struct SockFprog {}
    [GoValueClone("Addr", "raw")] public partial struct SockaddrInet4 {}
    [GoValueClone("Addr", "raw")] public partial struct SockaddrInet6 {}
    [GoValueClone("Addr", "raw")] public partial struct SockaddrLinklayer {}
    public partial struct SockaddrNetlink {}
    [GoValueClone("raw")] public partial struct SockaddrUnix {}
    public partial struct SocketControlMessage {}
    [GoValueClone("X__unused")] public partial struct Stat_t {}
    [GoValueClone("Fsid", "Spare")] public partial struct Statfs_t {}
    public partial struct SysProcAttr {}
    public partial struct SysProcIDMap {}
    [GoValueClone("Loads", "Pad_cgo_0", "X_f", "Pad_cgo_1")] public partial struct Sysinfo_t {}
    [GoValueClone("Pad_cgo_0")] public partial struct TCPInfo {}
    [GoValueClone("Cc", "Pad_cgo_0")] public partial struct Termios {}
    public partial struct Time_t {}
    public partial struct Timespec {}
    public partial struct Timeval {}
    [GoValueClone("Pad_cgo_0", "Pad_cgo_1", "Pad_cgo_2", "Pad_cgo_3")] public partial struct Timex {}
    public partial struct Tms {}
    public partial struct Ucred {}
    [GoValueClone("Pad_cgo_0", "Fname", "Fpack", "Pad_cgo_1")] public partial struct Ustat_t {}
    public partial struct Utimbuf {}
    [GoValueClone("Sysname", "Nodename", "Release", "Version", "Machine", "Domainname")] public partial struct Utsname {}
    public partial struct WaitStatus {}
    public partial struct _Socklen {}
    public partial struct ΔSignal {}
    // </TypeAccessibility>
}
