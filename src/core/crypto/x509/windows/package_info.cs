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
global using bigꓸInt = go.math.big_package.ΔInt;
global using bigꓸRat = go.math.big_package.ΔRat;
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
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
global using syscallꓸHandle = go.syscall_package.ΔHandle;
global using syscallꓸSignal = go.syscall_package.ΔSignal;
global using syscallꓸSockaddr = go.syscall_package.ΔSockaddr;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
global using urlꓸError = go.net.url_package.ΔError;
using syscall = go.syscall_package;
// </ImportedTypeAliases>

using go;
using static go.crypto.x509_package;

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
[assembly: GoDynamicTypeLift("63727970746f2f783530392e7075624b6579457175616c", "alreadyInChain_pubKeyEqual")]
[assembly: GoDynamicTypeLift("696e746572666163657b457175616c2863727970746f2e5075626c69634b65792920626f6f6c7d", "alreadyInChain_pubKeyEqual")]
[assembly: GoDynamicTypeLift("7374727563747b616c676f2063727970746f2f783530392e5369676e6174757265416c676f726974686d3b206e616d6520737472696e673b206f696420656e636f64696e672f61736e312e4f626a6563744964656e7469666965723b20706172616d7320656e636f64696e672f61736e312e52617756616c75653b207075624b6579416c676f2063727970746f2f783530392e5075626c69634b6579416c676f726974686d3b20686173682063727970746f2e486173683b20697352534150535320626f6f6c7d", "signatureAlgorithmDetailsᴛ1")]
[assembly: GoDynamicTypeLift("7374727563747b6578744b657955736167652063727970746f2f783530392e4578744b657955736167653b206f696420656e636f64696e672f61736e312e4f626a6563744964656e7469666965727d", "extKeyUsageOIDsᴛ1")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<CertificateInvalidError, error>]
[assembly: GoImplement<ConstraintViolationError, error>]
[assembly: GoImplement<HostnameError, error>]
[assembly: GoImplement<InsecureAlgorithmError, error>]
[assembly: GoImplement<SystemRootsError, error>]
[assembly: GoImplement<UnhandledCriticalExtension, error>]
[assembly: GoImplement<UnknownAuthorityError, error>]
[assembly: GoImplement<go.crypto.rsa_package.PSSOptions, crypto_package.SignerOpts>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<VerifyOptions, ж<VerifyOptions>>(Indirect = true)]
[assembly: GoImplicitConv<syscall.CertChainContext, ж<syscall.CertChainContext>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/x509/cert_pool.go", "cert_pool.cs", "AFOAAZIABRDCgpSokoKoktyCgoKUopSCAAIcAAsCgIKmAAcU0oIAARIACAKCgoKUgpS2tMiCgpSCgoKCprKClKiygpQAAhTygqiCloLKAAcSAAgCgoKCgpSClpKCgpSIgpSClJSWAAIQ8oKClKjCgpSClKKCpgACEPKClA==", "184-186:1;239-246:1;240-244:1.1;291-293:1")]
[assembly: go.GoPositionMap("crypto/x509/notboring.go", "notboring.cs", "AAgSgA==")]
[assembly: go.GoPositionMap("crypto/x509/oid.go", "oid.cs", "AEM4koK2goKWgriClIKoqJKCloKCloKCgpSmgoKUpoKCgoKClJSmooKUpoKCloKCgoKUlKiSqJKmuIKCgqiagoKClISagIKkgIKmgpaChISCgoKCgpSWgqiSqJKCgpSCqLamwoKCpoKClIKmgoKUgoKClIKUpoKssoKUgqaUgpKCppKCqIKCgqaUgqiokoKCqs6CgoKCgqaCgpSClIKCgoKClIKCgoKClJSCgoKCgoKClIKmlIKmpoKErISCgpaChIKCgoKUgpSClIKo")]
[assembly: go.GoPositionMap("crypto/x509/parser.go", "parser.cs", "AHtGogAJMAAMApSkgoKmpIKUpIKYgJKmgoKClqSCgpSkgoKmpAAICsKCloKCgoKClIKCgpSCgpSCgoKUgoKClJaW5oKSgpSClIKCgpSCgvaigpSCxoLGpKaigoKUgoKW9oKCgpSCgqaCgpSCABQGgoKCkpiiloKClIKUgpaClIKWuKSCgoKUgoKUgoKUyqiilIKUqKKUtIKClAAHEJKIlISUpPiCgoKWgoKCptaigoKUgoKmkoKCuuaigpSCgoKClICCuPbigpSCgIKkpIKAgqSkgoCCpIKClIKAgraklKTIluaUlJSCgoKUgoKUlNaigoKClIKCgpSAgpS21qKCgpSCgoKClIKClJSokoSCgoKWlpTmuOYAECCCopKKlsqWwoKSgoSWvpSCgIKmgsqUgIKkpoKUlIK2gramgpamgoCCqqKAgsiCgpSAgramgoCCpoKWgsqUgIKkpriWgIKkgIKkhAAKBoKCgoSClIKCxoKClIK0goKWlMiCggAEIJIACwKUgoKClIKCgpSClIKUgoKUgoKU2oKCxoKCyKSUgoKClLSCgpSCgoCC6viUlJKClIKCgpSCgpSClIKUlLTqloKoABkKgoTIgpSCgpamgpSCgpaClIK4goKWgoKUgoKUpoSCgpqygpSClIKClISCgpSCgoKUhIKClIKCloKClIKCgpSEgoKUgoKUgoKUkoKUgoKClILIgqiCgpSClIKCgoKUgoKClIKCgpSCgpSCgpSClIKCzIKClIQABRDSgoKUgpSqooKCgoKUgpQADBKihMiClIKClqaClIKCloKClIKUgpaCgpqygpSClIKClISCgpSEgoKUgoKClISCgpSCgoKogoKClIKUgoKUgoKWgoKUgoKUgoKClIKCgoKUgoKUgoKCpqiCyqiCgoKUgoKUgoKClIKClIKCgraCgoKmqA==", "375-414:1;539-651:1")]
[assembly: go.GoPositionMap("crypto/x509/pem_decrypt.go", "pem_decrypt.cs", "AHekAbKCgoSCgoKCgoKUAAUSAAgCggANJAAOAoKCloKCloKClIKClIK6goKCloKWgoIABhCCgpSCgpSClIKCpgACFPKCgpSCgILIgoKClIKCuJSClIQACRSCgoKCpqaCgoKCpg==")]
[assembly: go.GoPositionMap("crypto/x509/pkcs1.go", "pkcs1.cs", "ADZesoKCgpSCgIKkgIKkloKWgpaCuoKCgoKCgpS6goKUhAACEPKEgoKWAAoYgoKCgpaC/LKCgoKAgqSUgpaClIKWAAUS0rg=")]
[assembly: go.GoPositionMap("crypto/x509/pkcs8.go", "pkcs8.cs", "ACJGAAwCgoCCgIKkgIKkpJSCgpSmgoKAgqSCgpSmgIKkgoCCpICCpKaAgqSCgIKktgAHFvKElLi2goKUgoKU3ICC2KaCgpS2gqaCgIK2goKUgoKU3ICC+pY=")]
[assembly: go.GoPositionMap("crypto/x509/root.go", "root.cs", "ACE8ooKCgtaigoKCggAIJgARAoK6hIKEgpSEgoKUlA==")]
[assembly: go.GoPositionMap("crypto/x509/root_windows.go", "root_windows.cs", "ABQeggACFAALAoSCgpSEgoKUhIKCloKCgoKUgoKWgoKCugAICLKCloKCgpSCgoKCgpSWqqKCgpSkpsaqwoKClLiEpoSSgoLMgpSkpKS4/oKC6NKCgpaCgoKogoKUggAGEIKClICUtqoACAKCgpSEgoSCgpSCgoKClICCtoKCgpSCgpaCgpKctpKCgpSEgoKWgIKCgoKCypSW")]
[assembly: go.GoPositionMap("crypto/x509/sec1.go", "sec1.cs", "ACVKsgAFEPKCgpbaooKUggAHFMIAChbigoCCgIKkgIKkpIKWgoKUlIKWgoKClIKChKiCgpTMgoQ=")]
[assembly: go.GoPositionMap("crypto/x509/verify.go", "verify.cs", "AFGMAYKUpKSkpKSkpKSkAAwUgoSCloKAlIKUgoKUpqaClAAQHIKCgoKCgpSmlAAKEIKCgpSmgAAsbOKCloQAChaCgoKClIKElLiSlIiClAAEJAAJBvqClISegtKUAAIUgqbYgt6ChqiClLqAgqaCgqrCgoCCgpSCgrbKlJaClJaClLqmpoKCgpS6pgAJEoKCloKCgoLegIKmpoKCloKAgrimpoKWgoIABRCCgoKWgoKWhJaCgqgAAhAABxCEgoKWgoKCgpaCqISCgpaCgoSCgIKmgqiClgAJCtKCloKCgqiCgpSC7N6CgpSUgoKohIKCgqaCgpSCgoKWgJjIgoCCpoCYyIKCgpaAmMiCgIKmgJj+loIAEyyCloKCgqi4lgACRgAnBoKUgoKClIK6poKClIK4grqCgoKogoKWgoKCqIKClIKCqIKWgqaogoKCqIKWpoKCgoIABhDSirKCgqiCgpSClIKygoKmgqSUgqYACxLCmpKCloKUgoKCloCCgoKUpoKCgoKUloKAgoKClLiUpIKCuIKUgpaClIKWpoCigKqygpSClKaWgpSUuJSCgpSClIKUgpSmlKimgoKUpoKChIKWgoSCloKClIKorMSCgqaClIKCqIKWgoKCpgACGAAMBIKClICmgoKmpoKE3IKCpoK6poKChIKWvIKCgpSWgpSohIKCgpaCgqiCgoL6", "617-678:1;627-629:1.1;640-642:1.2;654-656:1.3;667-669:1.4;909-958:1")]
[assembly: go.GoPositionMap("crypto/x509/x509.go", "x509.cs", "AFmOAQAIAoKAgoCCpKSk1sKUuIKUhrSCopSClIKCgoKClLSCtIKClIKClIKCgoKU1pYAAhgACQKCgoSAgqYABhCCAEOGAYKCgqamgoKCpqaCgoKmABEogoKUAJQBiAKCpoKogoKCpryCgIKmgoCCAAcQipaUpKSmAAg+spSkpKSkABo0gpSkpKSkpoKUpKSkpqaClKSkpKYAXrQBsoKCpqaygoKmAGr8AYKCgpQACxCCprKClKaCrgALDISWgpaClgACFPKmgqaCgoKmpoIACA7CgoSCgoKCqKSCtqaSgpSUpIKUgoKmlIKUgpTGgpSClKSClIKUlKzSggAICoIAK06CgoKCrLKEgoSCgpSoABIyooKCpqqigoKAgqSUgoCCpJSUgoKUlIKCgIKklKaClIKoyrKShISCgpSWhIKClJaCgoKUloKCgoKUloKCgoKUloSCgoLKgsqCgpSWhLiCgoKUloKEgoKUloqChIKCgoKClqKEgoCCpoKCuoKiorqCgIKmgoK6goCCpoKCupaCgpaCgpaCooKSqIKSuoKClJaEhIKC7paCgpTOpoKEgoKEgoKWgoKC1oKEgoKAgpS4hIKCpoK4gpSCgtaChIKigoKykpKCgpTKgpLMgoKmgoSEgoKWzKaCgpYACAyygoKEpIK2gpSkpKTYgsaWgpaCgoKUgpbepoKCkoKCgpaCgsyCgqiAgqYADoYBAD8CgoKWggAGEIKWgpaCgpaCgpSCloKCloKCloKCloLKgpiGgqSmgoKWggAJFoKClISCgpYADSjigoKCptyygoCCpKQAAhDygoKWgoKogqKClgAIFIKCgoKClJaCgpSEgoKWAESWAaKCgoKUgoKUgpSokoKCgqaCpgAMCqSMgoKCgJSmgpaCgIKkgoKClJSWAAgsABYCgoKWgoKWgoKCgpaCgqiCgoKCzIKUgoK6hIKCqIKEgqaWAAYQgoKogoK6ggADEIKCloKAgqaWgoKCgqgADBqCgpSEgoKWAAYSooSCgqSW1oIACh6CgoKCqIKAgqSmhICCpoKUgoLaqJIAggGSAgAPAoKUgpSClIKUgpSCloKClqaUgqKCyoKCgpSClt6CgoKUuoKCgpbMgpSogoKWgIKkgoKogoKWABEigpaCloKCuoSCgpYABhLChJaCloKW", "1219-1225:1;1227-1275:2;1235-1239:2.1;1236-1238:2.1.1;1243-1247:2.2;1244-1246:2.2.1;1255-1259:2.3;1256-1258:2.3.1;1267-1271:2.4;1268-1270:2.4.1;1288-1300:3;1290-1292:3.1;1296-1298:3.2;1393-1414:1;1397-1405:1.1;1398-1404:1.1.1;1409-1411:1.2")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("x509")]
public static partial class x509_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    [GoLocalName("pubKeyEqual")] internal partial interface alreadyInChain_pubKeyEqual {}
    internal partial struct AppendCertsFromPEM_lazyCert {}
    internal partial struct CreateCertificateRequest_attr {}
    internal partial struct authKeyId {}
    internal partial struct authorityInfoAccess {}
    internal partial struct basicConstraints {}
    internal partial struct certificate {}
    internal partial struct certificateList {}
    internal partial struct certificateRequest {}
    internal partial struct distributionPoint {}
    internal partial struct dsaAlgorithmParameters {}
    internal partial struct ecPrivateKey {}
    internal partial struct extKeyUsageOIDsᴛ1 {}
    internal partial struct lazyCert {}
    [GoLocalName("pkcs10Attribute")] internal partial struct parseCSRExtensions_pkcs10Attribute {}
    internal partial struct pkcs1PrivateKey {}
    internal partial struct pkcs1PublicKey {}
    internal partial struct pkcs8 {}
    internal partial struct pkixPublicKey {}
    internal partial struct policyInformation {}
    internal partial struct potentialParent {}
    internal partial struct pssParameters {}
    internal partial struct rfc1423Algo {}
    internal partial struct rfc2821Mailbox {}
    internal partial struct signatureAlgorithmDetailsᴛ1 {}
    internal partial struct sum224 {}
    public partial struct CertPool {}
    public partial struct Certificate {}
    public partial struct CertificateInvalidError {}
    public partial struct CertificateRequest {}
    public partial struct ConstraintViolationError {}
    public partial struct ExtKeyUsage {}
    public partial struct HostnameError {}
    public partial struct InsecureAlgorithmError {}
    public partial struct InvalidReason {}
    public partial struct KeyUsage {}
    public partial struct OID {}
    public partial struct PEMCipher {}
    public partial struct PublicKeyAlgorithm {}
    public partial struct RevocationList {}
    public partial struct RevocationListEntry {}
    public partial struct SignatureAlgorithm {}
    public partial struct SystemRootsError {}
    public partial struct UnhandledCriticalExtension {}
    public partial struct UnknownAuthorityError {}
    public partial struct VerifyOptions {}
    public partial struct distributionPointName {}
    public partial struct pkcs1AdditionalRSAPrime {}
    public partial struct publicKeyInfo {}
    public partial struct tbsCertificate {}
    public partial struct tbsCertificateList {}
    public partial struct tbsCertificateRequest {}
    public partial struct validity {}
    // </TypeAccessibility>
}
