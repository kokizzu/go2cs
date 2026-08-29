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
global using flagꓸErrorHandling = go.flag_package.ΔErrorHandling;
global using jsonꓸToken = object;
global using jsonꓸΔToken = object;
global using netipꓸAddr = go.net.netip_package.ΔAddr;
global using netipꓸPrefix = go.net.netip_package.ΔPrefix;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using reflectꓸChanDir = go.reflect_package.ΔChanDir;
global using reflectꓸKind = go.reflect_package.ΔKind;
global using reflectꓸMethod = go.reflect_package.ΔMethod;
global using reflectꓸType = go.reflect_package.ΔType;
global using reflectꓸValue = go.reflect_package.ΔValue;
global using runtimeꓸError = go.runtime_package.ΔError;
// </ImportedTypeAliases>

using go;
using static go.net.netip_package;
using static go.net.netip_test_package;

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
[assembly: GoTypeAlias("Addr", "ΔAddr")]
[assembly: GoTypeAlias("AddrDetail", "go.net.netip_package.addrDetail")]
[assembly: GoTypeAlias("Prefix", "ΔPrefix")]
[assembly: GoTypeAlias("Uint128", "go.net.netip_package.uint128")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<AddrPort, go.net.netip_package.appendMarshaler>]
[assembly: GoImplement<AddrPort, netipTypeCmp>]
[assembly: GoImplement<parseAddrError, error>]
[assembly: GoImplement<parsePrefixError, error>]
[assembly: GoImplement<testing_package.B, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<testing_package.T, testing_package.TB>(Pointer = true)]
[assembly: GoImplement<ΔAddr, go.net.netip_package.appendMarshaler>]
[assembly: GoImplement<ΔAddr, netipTypeCmp>]
[assembly: GoImplement<ΔPrefix, go.net.netip_package.appendMarshaler>]
[assembly: GoImplement<ΔPrefix, netipTypeCmp>]
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
[assembly: go.GoPositionMap("net/netip/export_test.go", "export_test.cs", "ABQmgqaCpoKmgMiAooCkgA==")]
[assembly: go.GoPositionMap("net/netip/fuzz_test.go", "fuzz_test.cs", "AKMB/gGigpaCgoKWgoKCloKCgpSCgpSClIKUgpSClIKUgpSClIKUgpSCzIKUgpaCgoKUgoKEgoKClIKCupKCgpSCgoKClIKCgoKUgoKCgpSCgoKCupKCgpSCgoKClIKCgoKUgoKCgpSCgoKCuIKCgpSCggAJFJKCgpaCgoIAECKylJaCgoKUgpSCgriCgoKCloCC", "132-211:1")]
[assembly: go.GoPositionMap("net/netip/inlining_test.go", "inlining_test.cs", "ACogooKCyoKUgpKClAAsWpbugoKClJSCgpQ=", "28-31:1")]
[assembly: go.GoPositionMap("net/netip/netip.go", "netip.cs", "AGeYAaCooKaQppCmkKayAAUS0gAIGLKClKSo1qqigoKUAAgSgoKClKaCkoKCgoKClIKCguyCpoKUgoKClKaClIKosoKCgpSoksyCgoKClKiCloKClIK6gqaCgoKCgqSklJSUlJSmlKiClJSUloKClIKClIKCqIKCloKCqIKklJaCkpSCgpLMgqiCgpSCgpS2lKyylKSkqqKqoqqiAAIQ9qywrtKUpKSokoKUrLKSgpSClIKClIKUgoKUgpSCkoKUgqassKqyqJKqoq7CgpSssoKUgoKUgqqigpSCqJKokoK6griClKiSgrqCuIKUqJKCuoK4gpSqxoKUqJKCuoK4gpQAAhgACQKUloK6gpYAAhTCgqimmqaWrsIABRDSgpSClKSClKSCtoIAAhDygoKs0oKClIKUqJKUpIKCpIKCgsyigoKUppSmqqKCgraUggAFHgAMApSkpIKUzrKUpKSClAAIEriClIKUqLiClIKUgpSokqaCgoKCpoKCgoKCgoKCpoKCgoKmgoKCgoKUAAIQAAUQ8oKCpoKCgoKClICCuIKCgoKCtpaWgoKUrLKUpoKCgoKWlqaClKyylKSCgqSCgoKUgoIABBLSgoKUgoKmgoKUpIKkgoKCgqSuwtqigpSCpIKkgqSCpAAIFqCmkKaQAAgOAAkCgoKWgoKUgpSCgpSClq7CgoKClIKClIKCgpSCpJSqooKClKqgqrKAgqTWgoKUpIKCpIKCgoKUgoKClKSCgqyylKSkgoKUlKSCgqyygrakpIKCrLKCgpSCgqyygoKqooKUgoKClIIADS4ACAKCgpTekKqwqrCkgKaQAAIUAAgCgIKkgIKkAAcQggACFPKCgpSCgqaClpaCloKClIKClIKUqqKCgpSuwoIAAhTygpSAkqQACBLKAAMS0oKUgpSClIKAkpSkgpzCgIKkgIKkrLKClIKogpSCgpSogoKssoK2pKSCgqyygoKUgoKssoKCqqKClIKCgpSC2JKClA==")]
[assembly: go.GoPositionMap("net/netip/netip_pkg_test.go", "netip_pkg_test.cs", "ABgogoKCABEugoKCqIKCABgsgoS4goKogoKWuIKCyqKCuIKCgoKUgpSClIIADQqCAAcWgoKCAAoKggAdRIKCggANCoIAFDCykoKClICCpIKUgIIADAyCAAkaspKCgoKUlIKUgqiSgoLekoKCgoKClJSClIKClIIACgyCrrKShIKAgqaCgpaCgpYAChiigoKClIKCgriCgoKUgoKAgg==", "219-233:1;252-266:1;268-273:2;278-298:3;311-330:1")]
[assembly: go.GoPositionMap("net/netip/netip_test.go", "netip_test.cs", "AEpAogBh0AGykoKCgpSUgpSCqIKClIKogoKClIKogoKUgqiCgoKUgqjMgoKAgqSClIKClIKCgroAPoABgoKCgpaCgpaCgpamlIKCgIIADwyCABU0goKCyqKCAAgIggAHFIKCgpSCgpSClIKAgqSCuoKCgIIAEAqCAAcWgoCCpIKCgpSCAAkKggAFEoKCgpSCgpSClIKAgqSCuoKCgIIAEAqCAAcUgoCCpIKCgpSCAAkKggAGFKaCgoKClIKUgoCCpIK6goKAgtq4goKAgqSCloKClIKCAAkIggALILKSgoIAIQyCABU+AJ4B1gKykoKCloKCloKCloKCloKCloKCloKCloKCAAsMggAaQLKSgoSCAA8MogAYQoKCgpSCgpSClIKUgqiCgoK6goK6AAcQgoKCggAMCIIACiSCgoKogoKAgtwABxCCgoKCAA8IggAKJIKCgqiCgoCC3AAIEoKCgoIACwiCABU0spKChIIAFwyCAAAQgoKWAB5AADNwspKypISCgpSClIKCloKWgIIACBCCAAcSgqaEgoCCpoKCloKCAAgMgoKAggAPCIIACBqCgoKWgoIAEQqCAAwigoKClIKCAAkKggARKrKSgoIAFAyCADN0spKCgpSClIKUgoKmgoKmgoKUgIKmAAkKgoKCAA4kgoKAggAKCoK+soKSkpSClICCAA4MggAybLKSgoKUgIIACwyCAAgYgoKCyqKCgpT2ooIABBCykoKCgoKUgoCC/qKCgoKCgoLKooKCgoKCggAQIoKosoKCgoKCgsqigoKCgoKCyqKCgoKCgoLKooKCgoK4ooKCgoIAEB6CgrKSgoLcgrKSgoLcgoKSkoKC3IKCkpKCgtyCgpKSgoLcgoKCkpKCgtyCgoKSkoKCAAsMggAuaLKShILcooKCgriCgoKClJSChIIACwyCABY0woKCgqaC1KKCgoKUggAJCoKCAB1KgoCCtoCCAFg0poKClJSCgpSUgoKUlIKCgoLMgJKAkoCSkKKQooCSgJKAkoCSgKaAkoCSgJKAkoCSgoKClIKCgpSAkoCSgJKAkoCSgJKAkoCSgJKAkoCSgJKAkpCikKKAkoCSgJKApoCSkKKApoCSkKKQooCmgJKCgpSAkoCSgAANBoIABxiCspKUlJKUggAKDIIABBKCgIIACgqCAAMQgoCCAA0KggAEEoKCgu6igoI=", "138-215:1;283-309:2;570-575:1;783-823:1;861-868:1;1088-1095:1;1108-1143:1;1202-1229:2;1204-1227:2.1;1244-1263:1;1354-1359:1;1423-1453:1;1496-1506:1;1497-1499:1.1;1500-1502:1.2;1566-1574:1;1618-1630:1;1743-1748:1;1754-1759:1;1766-1771:1;1778-1783:1;1790-1795:1;1803-1808:1;1816-1821:1;1879-1885:1;1905-1911:1;1942-1951:1;1943-1948:1.1;2038-2043:1;2044-2049:2;2050-2055:3;2056-2063:4;2057-2062:4.1;2066-2066:5;2067-2067:6;2068-2068:7;2069-2069:8;2070-2070:9;2071-2071:10;2072-2072:11;2073-2073:12;2074-2074:13;2075-2075:14;2078-2078:15;2079-2079:16;2080-2080:17;2081-2081:18;2082-2082:19;2083-2087:20;2088-2092:21;2093-2093:22;2094-2094:23;2095-2095:24;2096-2096:25;2097-2097:26;2098-2098:27;2099-2099:28;2100-2100:29;2101-2101:30;2102-2102:31;2103-2103:32;2104-2104:33;2105-2105:34;2106-2106:35;2107-2107:36;2108-2108:37;2109-2109:38;2110-2110:39;2111-2111:40;2114-2114:41;2115-2115:42;2116-2116:43;2119-2119:44;2120-2120:45;2121-2121:46;2122-2122:47;2125-2125:48;2126-2129:49;2130-2130:50;2131-2131:51;2132-2132:52;2150-2161:1;2155-2157:1.1")]
[assembly: go.GoPositionMap("net/netip/slow_test.go", "slow_test.cs", "ABlQABUGooLIgpLGuIKClJaCgrqCgpSCgoKClIKWAAIqABICloKClIKCgpSCqILIlrKClIKUgpSGopSCgpS2tq7CgoKUgoKCgpSUqqKClIKClA==")]
[assembly: go.GoPositionMap("net/netip/uint128.go", "uint128.cs", "ABkoogACENCmkqiSqJKokqiSgqiSgq7CqqKqog==")]
[assembly: go.GoPositionMap("net/netip/uint128_test.go", "uint128_test.cs", "AA8WgoKCAAsigoKUpKSkggAJCoIACBiCgoKCAAkKggAIGIKCgoI=")]
// </GoSourcePositionMaps>

namespace go.net;

[GoPackage("netip")]
public static partial class netip_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial struct TestAddrPortMarshalUnmarshal_tests {}
    internal partial struct TestBitsClearedFrom_tests {}
    internal partial struct TestBitsSetFrom_tests {}
    internal partial struct TestIPBitLen_tests {}
    internal partial struct TestParseAddrPort_tests {}
    internal partial struct TestParseIPError_tests {}
    internal partial struct TestPrefixContains_tests {}
    internal partial struct TestPrefixValid_tests {}
    internal partial struct TestUint128AddSub_tests {}
    internal partial struct addrDetail {}
    internal partial struct nextPrevTestsᴛ1 {}
    internal partial struct parseAddrError {}
    internal partial struct parsePrefixError {}
    internal partial struct uint128 {}
    public partial interface appendMarshaler {}
    public partial struct AddrPort {}
    public partial struct ΔAddr {}
    public partial struct ΔPrefix {}
    // </TypeAccessibility>
}

[GoPackage("netip_test")]
public static partial class netip_test_package
{
}
