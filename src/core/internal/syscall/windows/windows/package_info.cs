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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
// </ImportedTypeAliases>

using go;
using static go.@internal.syscall.windows_package;

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
[assembly: go.GoPositionMap("internal/syscall/windows/reparse_windows.go", "reparse_windows.cs", "AD+EAZKCggATKpKCgg==")]
[assembly: go.GoPositionMap("internal/syscall/windows/security_windows.go", "security_windows.cs", "ACNcsoKUppSUAA0eggAxeg==")]
[assembly: go.GoPositionMap("internal/syscall/windows/syscall_windows.go", "syscall_windows.cs", "ABcqwoKUgoKCgpQA9AH+BIKCgpSCgpQAJ2SSADiGAbKCgoKClIKUlKzM")]
[assembly: go.GoPositionMap("internal/syscall/windows/version_windows.go", "version_windows.cs", "ABw60pKCggALFIKEgoKCgpSCgoKUgoLaooKqooKssoL+ggAJDoaChIKCgpSCgqY=")]
[assembly: go.GoPositionMap("internal/syscall/windows/zsyscall_windows.go", "zsyscall_windows.cs", "ABk0opSk2gA5kAGigoKUpqKCgpSmooKClKaygoKClKaygoKClKaigoKUgoKUpqKCgpSmooKClKaigoKUpqKCgpSCgpSmooKClKaygoKClKaigoKmooKClKaigoKmsoKCgpSmooKClKaygoKClKaygoKClKaygoKClKaigoKUpqKCgpSmooKClKaigoKUpqKCgpSmooKClKaygoKClKaigoKmooKCpqKCgpSmooKClKaigoKUAAQWooKClKaigoKUpoKCpqKCgpSmooKClIKClKaigoKUpqKCgpQABB6ygoKClA==")]
// </GoSourcePositionMaps>

namespace go.@internal.syscall;

[GoPackage("windows")]
public static partial class windows_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    [GoValueClone("csdVersion")] internal partial struct _OSVERSIONINFOW {}
    internal partial struct sendRecvMsgFuncᴛ1 {}
    public partial struct FILE_ATTRIBUTE_TAG_INFO {}
    public partial struct FILE_BASIC_INFO {}
    [GoValueClone("FileName")] public partial struct FILE_FULL_DIR_INFO {}
    [GoValueClone("ShortName", "FileName")] public partial struct FILE_ID_BOTH_DIR_INFO {}
    [GoValueClone("PhysicalAddress", "ZoneIndices")] public partial struct IpAdapterAddresses {}
    public partial struct IpAdapterAnycastAddress {}
    public partial struct IpAdapterDnsServerAdapter {}
    public partial struct IpAdapterGatewayAddress {}
    public partial struct IpAdapterMulticastAddress {}
    public partial struct IpAdapterPrefix {}
    public partial struct IpAdapterUnicastAddress {}
    public partial struct IpAdapterWinsServerAddress {}
    public partial struct LUID {}
    public partial struct LUID_AND_ATTRIBUTES {}
    public partial struct LocalGroupUserInfo0 {}
    public partial struct MemoryBasicInformation {}
    [GoValueClone("Module", "ExePath")] public partial struct ModuleEntry32 {}
    [GoValueClone("PathBuffer")] public partial struct MountPointReparseBuffer {}
    public partial struct PROCESS_MEMORY_COUNTERS {}
    public partial struct REPARSE_DATA_BUFFER {}
    public partial struct REPARSE_DATA_BUFFER_HEADER {}
    public partial struct SERVICE_STATUS {}
    public partial struct SHARE_INFO_2 {}
    public partial struct SID_AND_ATTRIBUTES {}
    public partial struct SecurityAttributes {}
    public partial struct SocketAddress {}
    [GoValueClone("PathBuffer")] public partial struct SymbolicLinkReparseBuffer {}
    public partial struct TCP_INITIAL_RTO_PARAMETERS {}
    public partial struct TOKEN_MANDATORY_LABEL {}
    [GoValueClone("Privileges")] public partial struct TOKEN_PRIVILEGES {}
    public partial struct TokenType {}
    public partial struct UserInfo4 {}
    public partial struct WSAMsg {}
    // </TypeAccessibility>
}
