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
[assembly: GoTypeAlias("Handle", "ΔHandle")]
[assembly: GoTypeAlias("Signal", "ΔSignal")]
[assembly: GoTypeAlias("Sockaddr", "ΔSockaddr")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<DLLError, error>(Pointer = true)]
[assembly: GoImplement<Errno, error>]
[assembly: GoImplement<SockaddrInet4, ΔSockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrInet6, ΔSockaddr>(Pointer = true)]
[assembly: GoImplement<SockaddrUnix, ΔSockaddr>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<win32finddata1, ж<win32finddata1>>(Indirect = true)]
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
    internal partial struct _STARTUPINFOEXW {}
    internal partial struct connectExFuncᴛ1 {}
    internal partial struct mountPointReparseBuffer {}
    internal partial struct reparseDataBuffer {}
    internal partial struct symbolicLinkReparseBuffer {}
    internal partial struct sysLinger {}
    internal partial struct win32finddata1 {}
    public partial class Pointer {}
    public partial interface Conn {}
    public partial interface RawConn {}
    public partial interface ΔSockaddr {}
    public partial struct AddrinfoW {}
    public partial struct ByHandleFileInformation {}
    public partial struct CertChainContext {}
    public partial struct CertChainElement {}
    public partial struct CertChainPara {}
    public partial struct CertChainPolicyPara {}
    public partial struct CertChainPolicyStatus {}
    public partial struct CertContext {}
    public partial struct CertEnhKeyUsage {}
    public partial struct CertInfo {}
    public partial struct CertRevocationCrlInfo {}
    public partial struct CertRevocationInfo {}
    public partial struct CertSimpleChain {}
    public partial struct CertTrustListInfo {}
    public partial struct CertTrustStatus {}
    public partial struct CertUsageMatch {}
    public partial struct DNSMXData {}
    public partial struct DNSPTRData {}
    public partial struct DNSRecord {}
    public partial struct DNSSRVData {}
    public partial struct DNSTXTData {}
    public partial struct Errno {}
    public partial struct FileNotifyInformation {}
    public partial struct Filetime {}
    public partial struct GUID {}
    public partial struct Hostent {}
    public partial struct IPMreq {}
    public partial struct IPv6Mreq {}
    public partial struct InterfaceInfo {}
    public partial struct IpAdapterInfo {}
    public partial struct IpAddrString {}
    public partial struct IpAddressString {}
    public partial struct IpMaskString {}
    public partial struct Linger {}
    public partial struct MibIfRow {}
    public partial struct Overlapped {}
    public partial struct ProcessEntry32 {}
    public partial struct ProcessInformation {}
    public partial struct Protoent {}
    public partial struct RawSockaddr {}
    public partial struct RawSockaddrAny {}
    public partial struct RawSockaddrInet4 {}
    public partial struct RawSockaddrInet6 {}
    public partial struct RawSockaddrUnix {}
    public partial struct Rusage {}
    public partial struct SID {}
    public partial struct SIDAndAttributes {}
    public partial struct SSLExtraCertChainPolicyPara {}
    public partial struct SecurityAttributes {}
    public partial struct Servent {}
    public partial struct SockaddrGen {}
    public partial struct SockaddrInet4 {}
    public partial struct SockaddrInet6 {}
    public partial struct SockaddrUnix {}
    public partial struct StartupInfo {}
    public partial struct Systemtime {}
    public partial struct TCPKeepalive {}
    public partial struct Timespec {}
    public partial struct Timeval {}
    public partial struct Timezoneinformation {}
    public partial struct Token {}
    public partial struct Tokenprimarygroup {}
    public partial struct Tokenuser {}
    public partial struct TransmitFileBuffers {}
    public partial struct UserInfo10 {}
    public partial struct WSABuf {}
    public partial struct WSAData {}
    public partial struct WSAProtocolChain {}
    public partial struct WSAProtocolInfo {}
    public partial struct WaitStatus {}
    public partial struct Win32FileAttributeData {}
    public partial struct Win32finddata {}
    public partial struct _PROC_THREAD_ATTRIBUTE_LIST {}
    public partial struct ΔHandle {}
    public partial struct ΔSignal {}
    // </TypeAccessibility>
}
