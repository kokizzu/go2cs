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
[assembly: GoImplement<InterfaceAddrMessage, RoutingMessage>(Pointer = true)]
[assembly: GoImplement<InterfaceMessage, RoutingMessage>(Pointer = true)]
[assembly: GoImplement<InterfaceMulticastAddrMessage, RoutingMessage>(Pointer = true)]
[assembly: GoImplement<RouteMessage, RoutingMessage>(Pointer = true)]
[assembly: GoImplement<SockaddrDatalink, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrInet4, Sockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrInet6, Sockaddr>(Pointer = true)]
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
    internal partial struct anyMessage {}
    [GoValueClone("name")] internal partial struct ivalue {}
    internal partial struct mmapper {}
    [GoLocalName("linkLayerAddr")] internal partial struct parseLinkLayerAddr_linkLayerAddr {}
    public partial interface Conn {}
    public partial interface RawConn {}
    public partial interface RoutingMessage {}
    public partial interface Sockaddr {}
    [GoValueClone("Pad_cgo_0")] public partial struct BpfHdr {}
    public partial struct BpfInsn {}
    [GoValueClone("Pad_cgo_0")] public partial struct BpfProgram {}
    public partial struct BpfStat {}
    public partial struct BpfVersion {}
    public partial struct Cmsghdr {}
    public partial struct Credential {}
    [GoValueClone("Name", "Pad_cgo_0")] public partial struct Dirent {}
    public partial struct Errno {}
    public partial struct Fbootstraptransfer_t {}
    [GoValueClone("Bits")] public partial struct FdSet {}
    public partial struct Flock_t {}
    [GoValueClone("Val")] public partial struct Fsid {}
    public partial struct Fstore_t {}
    [GoValueClone("Filt")] public partial struct ICMPv6Filter {}
    [GoValueClone("Multiaddr", "Interface")] public partial struct IPMreq {}
    [GoValueClone("Addr")] public partial struct IPv6MTUInfo {}
    [GoValueClone("Multiaddr")] public partial struct IPv6Mreq {}
    public partial struct IfData {}
    [GoValueClone("Pad_cgo_0")] public partial struct IfMsghdr {}
    [GoValueClone("Pad_cgo_0")] public partial struct IfaMsghdr {}
    [GoValueClone("Pad_cgo_0")] public partial struct IfmaMsghdr {}
    [GoValueClone("Pad_cgo_0")] public partial struct IfmaMsghdr2 {}
    [GoValueClone("Spec_dst", "Addr")] public partial struct Inet4Pktinfo {}
    [GoValueClone("Addr")] public partial struct Inet6Pktinfo {}
    [GoValueClone("Header")] public partial struct InterfaceAddrMessage {}
    [GoValueClone("Header")] public partial struct InterfaceMessage {}
    [GoValueClone("Header")] public partial struct InterfaceMulticastAddrMessage {}
    public partial struct Iovec {}
    public partial struct Kevent_t {}
    public partial struct Linger {}
    public partial struct Log2phys_t {}
    [GoValueClone("Pad_cgo_0", "Pad_cgo_1")] public partial struct Msghdr {}
    public partial struct ProcAttr {}
    [GoValueClone("Pad_cgo_0")] public partial struct Radvisory_t {}
    [GoValueClone("Data")] public partial struct RawSockaddr {}
    [GoValueClone("Addr", "Pad")] public partial struct RawSockaddrAny {}
    [GoValueClone("Data")] public partial struct RawSockaddrDatalink {}
    [GoValueClone("Addr", "Zero")] public partial struct RawSockaddrInet4 {}
    [GoValueClone("Addr")] public partial struct RawSockaddrInet6 {}
    [GoValueClone("Path")] public partial struct RawSockaddrUnix {}
    public partial struct Rlimit {}
    [GoValueClone("Header")] public partial struct RouteMessage {}
    [GoValueClone("Filler")] public partial struct RtMetrics {}
    [GoValueClone("Pad_cgo_0", "Rmx")] public partial struct RtMsghdr {}
    [GoValueClone("Utime", "Stime")] public partial struct Rusage {}
    [GoValueClone("Data", "raw")] public partial struct SockaddrDatalink {}
    [GoValueClone("Addr", "raw")] public partial struct SockaddrInet4 {}
    [GoValueClone("Addr", "raw")] public partial struct SockaddrInet6 {}
    [GoValueClone("raw")] public partial struct SockaddrUnix {}
    public partial struct SocketControlMessage {}
    [GoValueClone("Pad_cgo_0", "Qspare")] public partial struct Stat_t {}
    [GoValueClone("Fsid", "Fstypename", "Mntonname", "Mntfromname", "Reserved")] public partial struct Statfs_t {}
    public partial struct SysProcAttr {}
    [GoValueClone("Cc", "Pad_cgo_0")] public partial struct Termios {}
    public partial struct Timespec {}
    [GoValueClone("Pad_cgo_0")] public partial struct Timeval {}
    public partial struct Timeval32 {}
    public partial struct WaitStatus {}
    public partial struct _Socklen {}
    public partial struct ΔSignal {}
    // </TypeAccessibility>
}
