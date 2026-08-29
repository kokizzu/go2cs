// go2cs metadata anchor for the INTERNAL (white-box bridge) test class: GoImplement /
// GoImplicitConv attributes whose GENERATED code must merge with a bridge-declared type
// anchor here — the source generators host output in the first class of the
// attribute-bearing file, and only this file's first class is the bridge. Records for
// production and external-test types stay in package_test_info.cs.

// <ImportedTypeAliases>
using rand = go.math.rand_package;
using testing = go.testing_package;
// </ImportedTypeAliases>

using go;
using static go.math.big_package;
using static go.math.big_internal_test_package;

// <ExportedTypeAliases>
// </ExportedTypeAliases>

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
[assembly: go.GoPositionMap("math/big/arith_test.go", "arith_test.cs", "AEtIgoKCgoKCpoIADQiCgoKChIKChIKChIKC7oKmgoKClMqCgoKUgoKCsoKC3IKCgpSCgoKygoIAMmyCgoKCgoKmgriUgoKEgoKCpoK4goIADQiCgoKChIKCloKCgoKCloKCgoKCupKCgpQACgy2opSCgoKCgpaCgpaCgoKWgoIASIYBlIKCgoKCgoKCpoK4goKCloKCuISSgoKCgoLcgoKCgoKCgriCgoKUgoKCooKC3pKCgpSCgoKigoLcgoKClIKCgqKCgt6SgoKUgoKCooKCACdOgoKCgoKCpoIADyCCgoKCgoKmggAICIKCgoKEgoIADhqCgoKCABEggoKCggAQHoKCgoKCgriCgoKCgpSCgoKCyoKCgpSCgoKCooKC3IKCgpSCgoKigoLagoKClIKCgqKCgtyCgoKUgoKCooKigqaigg==", "95-100:1;112-117:1;201-203:1;403-408:1;421-426:1;438-443:1;456-461:1;633-638:1;650-655:1;666-671:1;683-695:1;685-689:1.1;690-694:1.2")]
[assembly: go.GoPositionMap("math/big/bits_test.go", "bits_test.cs", "ABc4gqaCgoKCpuaCAAgWgoKCAAQSsoKigoKUlIKCgqaC5oIABxSCgoIABBCihqKCgpSCpoKCqrKCgoKUtLaSlNqCgpSUgpSsxIKqkoKCuoKClIKClKiCgIKmpPaCABEmgoCC")]
[assembly: go.GoPositionMap("math/big/calibrate_test.go", "calibrate_test.cs", "AC5KgoKWlpSCgpSUgpS4gqqigoKC5oKCloKWgoKEgpSWhJaCgqiCgpSEloKWuJSUlKSkpoKCgJKUhISmgoKCgoKCgoKCgoKCgpSCgpSWgpSU", "135-135:1")]
[assembly: go.GoPositionMap("math/big/decimal_test.go", "decimal_test.cs", "ABAYggAKFoCCAAsKggAMIoKCgIIACwqCABU+hIKCgoCCpoKCgIKmgoKAgv6igoKCgsqCgoKCkoKC", "127-132:1")]
[assembly: go.GoPositionMap("math/big/float_test.go", "float_test.cs", "AB4mgoKClKaCgoKUAAgGhJKAgriAgriCgoKmlAASMoKCgoKUgs6SgoKUAAkGggASNIKCgpSAgqSAkgAKCoKCAAwggoCCAAoKggAHFoKCgszCppSmlAAIBoIACh6CgoKCgsqCgoCCpICCAAoIggAVLIKCgoKCpoKCAAoKggAHGIKAgqSAgqSAggAICqIADyCCgoCC2oKCgpSmgqaEkpSkgpS2gpS2pKSkgqiClLTIloKCgoK43oKCzLqCgswACAqSAEGYAYKCgoKChIKCgoSChIK+soKCgoKCgoLKggAJFIKCgIK6koKCgoKCyoIACxSCgpSCgoCCzJKCgoKCgsqiABMkgoKUgoKAgsySgoKCgoK6goCStoKU1oIABxCCgoKClISCloKUgIK4goLOkgAKFoKCgoKUhJKCgpaClICCpoKCAAoKgoIABRSCgIIACwqCABAqgoKCAAoKggAaPoKCggAKCoIAWqQBlKKCgoKogIKmgoKCmJKCggALDIKCAE2WAZSigoKCqICCpoKCgpiSgoIADgyCABIugoKCgpSCuoKCgoCCAAoKggAPJoKCgoKUgoKUgoKogoKCzIKCgoCC2oIACBKCgoKWgoKCyoIACBKCgoKCgpSCyoKCgoKUkoKCgpSCABw04oKUgoKChIKCgoKCgqiCgoIACBjSgoKCgoKUgoKCpoKCAAUStJKCgoKCloKChIKCgoKWgoKCggAFEKSSgoKCgpaCgoSCgoKCloKCgoIACQyCAAUSgoKCgoKWgoKCggAEFOKClIKCgoSCgoKCgoKogpSCgoIADBaiAAkYgoKClIKUgpaCgoSCgoKCloKUgoKCgtyCgoKCgoKEgoKWgoKWgoKEgpSClILKtIKElIKClIKUgpSWhIKqgILKgoKCgoKCAAkYooKCloKCgoKCloKCloKCgoKEgoKClIIABxaygoKCgoKCgoK4gIKkgoKqlIKCtIKCtIKCtIKCtLSU0oKAgoK2xIKClJSCgpSCggAODoIAJ0qCgoKUtLS0tLSAggAOHvIADSyigoKClLS0tLS0gIIABhSygoKCgoKCuICCpIKCgoKUtLSC7oKCgoSCgoKEsoKC3IKCgoSCgoKEsoKC", "48-55:1;645-649:1;1641-1649:1;1642-1647:1.1;1830-1835:1;1849-1854:1")]
[assembly: go.GoPositionMap("math/big/floatconv_test.go", "floatconv_test.cs", "AB4kgoKEAIQBqgKCgoKUgpSmgoKUgoKCyoAADg6iAHy0AoKCgoKWgpSCqIKCzJKAlKQACwaihAB+tAKCgoKUgpaCgt6CgoKClIKCAAsMggBp6gGClLS0xKaAgtqiggASJoKCgtyiggAWLoKCggANDKIADzCCgoKCgoKCgpSWgpaClII=")]
[assembly: go.GoPositionMap("math/big/floatmarsh_test.go", "floatmarsh_test.cs", "ADA8goKCgoKCgoKChIKCgoKogpaAgoKmgoCCgqaCgpaCloKWggAKEIKCgoCCpISCgIKmgIKmgoCCyIKCgoKClIKCgoKClIKCgpSCgoCCgqSCAAkOgriCggAJCoIACRqCgg==")]
[assembly: go.GoPositionMap("math/big/gcd_test.go", "gcd_test.cs", "AA4gkoKC9oKClIKUgriigoKCgpKCgpSCgriAooCigKKAooCigKKAooCigKKAooCigKKAooCigA==", "26-28:1;29-31:2")]
[assembly: go.GoPositionMap("math/big/hilbert_test.go", "hilbert_test.cs", "ABMqgoKUpoKClKaCgpSCgoKCpoKCgoKCgpSmpoKCgoKmpoKCgoKCgoKEgoKChIKWpqaigpSCgoKCgpSmpqKClIKCgrimgoKCgpSUpoKCgoKCgoKUgoKCuIKmooI=")]
[assembly: go.GoPositionMap("math/big/int_test.go", "int_test.cs", "ACMoooKmABkygoKCgoKCyoKCgoKClILKgoKCgoKCgoKUgsqCgoKClIIACQiCgoKCgoSChIKEgviCgoKChIK+spaCgoKCgoKCgoKUlKiCgpamgqKCgoSChKaCgIIAKkaClJSClIKCuIKCggAJCoKCABQwgILaooKCABMkgoKCgoKCgoSCgoKUgpSCloKClIKUgpaCgoKUgpSCloKClIKUgsqCgoKUpoIABxCCgoKCgpaCloKCyoKChIKWgpamgoCCyKaClIKmgoCCyIKChIKWgoSCloKChAAWKoKAgqaCgoKChIKEgsqogoSCgoKCgoKEuKKCgoKEgoIAFyyCgoKCgpaAggBUqgGCgoKEkpSClJaClJaCgpaCgpSClqaCgoLcooKCgoKCAAgIgoKCABAogoKCwoKC3KKCgoKCgriCgoKChIKCgoSs4oKogoSChIKEgoKEloKCgpaCgoKUpoKChIKWgoSmgoKCgoSCloKEABcu4oKClIKCloKClIKUgqiCgoKClIKUgpaCgoKClIKUgpaCgoKClIKUgpaCgoKClIKUgriCgoKCgoKEgoKCloCCpoCCpoCCACVKgoKCgoSClILKgoKCgoSClIIAGjaCgoKChIKUgsqCgoKChIKUgsqCgoKChIKUgqaCgoKEgpSCABMigoKCgoKClIKUgpaClIKSgoKClIKWgoKUtLSC7oKCgoKUgoKCABcygoKCgoKCloKCgoKmlJaCloKCABUuooKCgoKCloKUgoKmlJaCloKCADRmgoKEgoK4goKCgoSCgriCgoKClKaCgoKUpKSmooKCgoKCgoKUgoKClIKUgoKClIKUgoKCpoIAGC6CgoKCgoKClIKCgoKCpoKCggASIoKCgoKEgsqigoKCgriigoKCgriigoKCgriigoKCgryigoKCgoKmooKCgoK4ooKCgoK4ooKCgoKCgriigoKCgoKCAAgIgoKCgoKEgoKCgoKCggAUJoKCgoKCgoKCgpSCggASIIKygoKCgoKCgoK4goKCgoKClIKCgoLcooKCgoKCgrzCooKCgoKogoKClIKCgqiCgqiClIKCpoLCgpKCloKCgoKogqiCqIKCgpSWgoKClKiCgoKCAAoMggASLpSCgoKCgoLKooKCgoKUlIKUgtaUgqaCgoKCgpSCgoKCqIKCgoKCuoKCgryigoKChKiCgoCC2qKCgoKCuKKCgoKCgriCgoKUgsqigoKCgpSChIKCuIK4goKCyoKCgoKCprKAkoLWAAkUooKCgqiCkpaCgpSWgoKQku6UgoCCyIKCgoLIlIIACgqCAB04goKCqIKCqIKC", "102-102:1;103-103:2;120-120:1;700-705:1;1756-1762:1;1852-1854:1;1881-1883:1;1888-1894:1;1895-1899:2;1896-1896:2.1;1911-1937:3;1933-1933:3.1;1953-1958:1")]
[assembly: go.GoPositionMap("math/big/intconv_test.go", "intconv_test.cs", "AFmyAYKCgoKWgoKCloKCloCC2oKCgoKCloKCgpaCgpaCgoCC2oKUpKSkpoKCgoKUhIKAgriCgoKCpoLcooKmgoKCgoKClIKClJSCgpSWgpSCloKUggCSAbwCgoKCgoKCgqaCggAdOIKCgoKCgoCCpIKUgg==")]
[assembly: go.GoPositionMap("math/big/intmarsh_test.go", "intmarsh_test.cs", "ACAygoKCgoKCgoKCgoCCgqSCgIKCpIIABRCigoKEooKClIKCgpSClIKCuIKCgoKCgoKCgpSCgIKCpIIACAyCgoKClIKCgriCgoKCgoKCgoKUgoCCgqSC")]
[assembly: go.GoPositionMap("math/big/link_test.go", "link_test.cs", "ACwkooKUgoKCgqiAgqSCgoCCpoKCgoKUgpSUyoKCpoI=")]
[assembly: go.GoPositionMap("math/big/nat_test.go", "nat_test.cs", "ACtGgoKCggA1aIKCgpSmgoKCgsqCgoIACwiCgoKEgoSChIKWgoKEggAiPIKCgoLMkoKCgoKCrNKCgoKilIKAgv6yqJKCgpSmooKCgoKCuKKCgoKCgtyCgoKUgsqCgoKClAATJoKCgoKCgoIAECCCgoKCgoKCAAsMgoSSgoKmkoKokoKCppKCABImgoKCgoSCgsqCgpSCAEyWAaKCgoKCgoKClIKWgoKUgoKWgoKUvIKCgoKCqIKCgoKCqIKCggAkUIKCgoKEgoKWgoLKgoKClIKCgoKCgsqCgqaCgoLcgpSkpIKCgoKClAAQIoKCgoKCyqKCgoKCgoIAGzqCgoKAggAUOIKCgoCCpJSCgIL+goKCgoKCuIKCgpSClILKooKCgoIACRSCgoKUggAXLIKCgoKCgoKCgoKUpKSCgpSClILugoLcgoKCooLcgriCgoKUgqaCgoKmgoSSgoKUggAFEuiChIKCgoCCzrKCgoI=", "191-193:1;243-245:1;313-318:1;319-323:2;325-330:3;331-335:4;558-570:1;578-583:1;757-759:1;782-807:1;822-826:1")]
[assembly: go.GoPositionMap("math/big/natconv_test.go", "natconv_test.cs", "ABAggoK+sqaUlLS4gpaWgoKCgpYAFSiEkqKClMSCloKCgpaCgpSClIIAe/wBgoKCgoKUgpSClIKUgoKClIIABnaSgoKClICCyIKCgoKSgqaCuKKCgriCgoKClJKCyoKCgoKClIKCgoSCgIKkhILugoKCgoKUgoKCgoKEgu6CkoC2goDIooKCgoKEgoKCgoKEgqiCgoKmgoKCgoLKgoKCgoKUgoKCgqaC", "76-81:1;77-79:1.1;322-325:1;345-349:1;359-373:1;385-395:1;402-402:1;406-406:2")]
[assembly: go.GoPositionMap("math/big/prime_test.go", "prime_test.cs", "AHXsAYKClKaCgoKUgoKCqKKCgoK6goKygoKmggAMDIKCgpKCupKCppKC+oIACAyC3IKCgoKClIKmgoKCpJSCpoI=", "148-157:1;149-153:1.1;164-168:1;171-175:2;176-180:3;185-185:1;192-192:1")]
[assembly: go.GoPositionMap("math/big/rat_test.go", "rat_test.cs", "AAsYoqKEgpaAgqaAgqaCgIKmgoCCpoKAgriCgIK21oKCgpKClIKCggAVKIKCgoSCgsqCgoKCgpSCgoLKgoKCgoKUgoKUgoLKgoKCgoKUgoKCyoKCgoKClIKUgoKCAAoUgoKCgoSCACc4goKChIKEgoSChIKEgoSCgpaCgsqCgoKCgoKWgoKCgoKWgoKCggASIoKCgoLKlIKCgoKogoKCqIKCgqiCgoKogoKCgoK4goKCgpaCgoKogoKCqIKCgqiCgoKCgqiCgqiCgoKogoKCgqiCgoKogoKCgoKCuIgACxKSgpaCgoKCgpSCgoKUlIKElKjuiAALEpKCloKCgoKClIKCgpSUgoSUqAAGEqKCgoCC3qKClIKCgpSCgs6igpSCgoKUgoLMkoKs0qaogpaCgoKCgpSCgpSCgpSCgpSs0qaogpaCgoKCgpSCgpSCgpSCgpSmgKKApIIABxCCgqbKgoLKggAIEoKCgoKUgoKUgoLKgtyCgoKClIKClIKCyqKCggAIDMKYoqKioqLsgoKAgtqCgoKCgsaCgqaC", "44-48:1;711-711:1;712-712:2;713-713:3;714-714:4;715-715:5;716-716:6;735-741:1")]
[assembly: go.GoPositionMap("math/big/ratconv_test.go", "ratconv_test.cs", "AEeCAYKCgoKClIKUgpSCgoKUggBkygGCgoKEgoSCgqSmgqTcgoKCgriCgoKCgoSCgoKUlJSCAB46goKEggC9AZoDotaiooKClJaCgoKUuoKCuvzogqiUqJaAgtqiooKClJaCgoKUuoK6/OiCqJSoloCC2oKCyoKCggAJCoKCAAwggoIACwiiADpSgpaCgoK6goKCgpSCgqaCgtyClIKChpKEgoKCgu6ClIKChpKEgoKCgu6ClIKCgoaShIKCgoI=", "711-718:1;733-740:1;756-763:1")]
[assembly: go.GoPositionMap("math/big/ratmarsh_test.go", "ratmarsh_test.cs", "ABAegoKCgoKCgoKAgoKkgoCCgqSCzqKCgoSigoKUgoKClIKUgoIAFi6CgoKCgoKCgpSCgIKCpILcgoKCgoKCgoKUgoCCgqSC3ILKgoI=")]
[assembly: go.GoPositionMap("math/big/sqrt_test.go", "sqrt_test.cs", "AA4gooKClISCgoKCAAkKggAQLIKChIKCgoIADSCCgpaChIIACw6CAAQQgoIABRKSgoKCooKC", "119-124:1")]
// </GoSourcePositionMaps>

namespace go.math;

[GoPackage("big")]
public static partial class big_internal_test_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    // </TypeAccessibility>
}
