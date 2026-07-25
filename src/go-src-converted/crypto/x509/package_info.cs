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
using rsa = go.crypto.rsa_package;
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
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). An alternate interface implementation exists
// that can resolve duck-typed interfaces at run-time, but handling interface
// implementations at compile-time results in faster startup times, avoiding
// reflection-based interface resolution.

// <InterfaceImplementations>
[assembly: GoImplement<CertificateInvalidError, error>]
[assembly: GoImplement<ConstraintViolationError, error>]
[assembly: GoImplement<HostnameError, error>]
[assembly: GoImplement<InsecureAlgorithmError, error>]
[assembly: GoImplement<SystemRootsError, error>]
[assembly: GoImplement<UnhandledCriticalExtension, error>]
[assembly: GoImplement<UnknownAuthorityError, error>]
[assembly: GoImplement<crypto_package.Hash, crypto_package.SignerOpts>]
[assembly: GoImplement<go.crypto.rsa_package.PSSOptions, crypto_package.SignerOpts>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<VerifyOptions, ж<VerifyOptions>>(Indirect = true)]
[assembly: GoImplicitConv<rsa.PSSOptions, ж<rsa.PSSOptions>>(Indirect = true)]
[assembly: GoImplicitConv<syscall.CertChainContext, ж<syscall.CertChainContext>>(Indirect = true)]
// </ImplicitConversions>

namespace go.crypto;

[GoPackage("x509")]
public static partial class x509_package
{
    // A C# nested type declared with no access modifier is PRIVATE, and the `[GoType]`
    // declarations in this package's converted sources are deliberately bare so they read
    // like the Go original. Their real accessibility — public for a Go-exported name,
    // internal otherwise — is supplied by the partial that go2cs-gen's TypeGenerator emits,
    // and a source generator cannot see its own output: while the generators run, every one
    // of those types is still private, so a semantic query that reaches across package
    // classes resolves them as Inaccessible and silently drops whatever it was about to
    // build from them.

    // The declarations below close that gap. A C# partial type may carry its access modifier
    // on any ONE of its parts, so pinning it here fixes each type's accessibility IN SOURCE,
    // ahead of generation, while the `[GoType]` declaration itself stays Go-shaped — the
    // section declares `public partial interface Closer {}` for a `[GoType] partial interface
    // Closer`, and `internal partial struct dirEntry {}` for an unexported one.

    // <TypeAccessibility>
    internal partial interface alreadyInChain_pubKeyEqual {}
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
    internal partial struct parseCSRExtensions_pkcs10Attribute {}
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
    public partial interface CreateCertificate_privateKey {}
    public partial struct AppendCertsFromPEM_lazyCert {}
    public partial struct CertPool {}
    public partial struct Certificate {}
    public partial struct CertificateInvalidError {}
    public partial struct CertificateRequest {}
    public partial struct ConstraintViolationError {}
    public partial struct CreateCertificateRequest_attr {}
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
