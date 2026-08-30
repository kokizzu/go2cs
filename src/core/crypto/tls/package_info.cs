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
global using cryptoꓸDecrypterOpts = object;
global using cryptoꓸPrivateKey = object;
global using cryptoꓸPublicKey = object;
global using ecdhꓸCurve = go.crypto.ecdh_package.ΔCurve;
global using ecdhꓸPublicKey = go.crypto.ecdh_package.ΔPublicKey;
global using netꓸAddr = go.net_package.ΔAddr;
global using netꓸError = go.net_package.ΔError;
global using osꓸDirEntry = go.io.fs_package.DirEntry;
global using osꓸFileInfo = go.io.fs_package.FileInfo;
global using osꓸFileMode = go.io.fs_package.FileMode;
global using osꓸPathError = go.io.fs_package.PathError;
global using osꓸSignal = go.os_package.ΔSignal;
global using runtimeꓸError = go.runtime_package.ΔError;
global using timeꓸLocation = go.time_package.ΔLocation;
global using timeꓸMonth = go.time_package.ΔMonth;
global using timeꓸWeekday = go.time_package.ΔWeekday;
using net = go.net_package;
using Δx509 = go.crypto.x509_package;
// </ImportedTypeAliases>

using go;
using static go.crypto.tls_package;

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
[assembly: GoDynamicTypeLift("63727970746f2f746c732e62696e6172794d61727368616c6572", "cloneHash_binaryMarshaler")]
[assembly: GoDynamicTypeLift("696e746572666163657b4d61727368616c42696e6172792829202864617461205b5d627974652c20657272206572726f72293b20556e6d61727368616c42696e6172792864617461205b5d6279746529206572726f727d", "cloneHash_binaryMarshaler")]
[assembly: GoDynamicTypeLift("7374727563747b736368656d652063727970746f2f746c732e5369676e6174757265536368656d653b206d696e4d6f64756c7573427974657320696e743b206d617856657273696f6e2075696e7431367d", "rsaSignatureSchemesᴛ1")]
[assembly: GoTypeAlias("ConnectionState", "ΔConnectionState")]
// </ExportedTypeAliases>

// As types are cast to interfaces in Go source code, the go2cs code converter
// will generate an assembly level `GoImplement` attribute for each unique cast.
// This allows the interface to be implemented in the C# source code using source
// code generation (see go2cs-gen). Resolving each duck-typed cast at compile time
// this way is what keeps startup free of reflection.

// <InterfaceImplementations>
[assembly: GoImplement<CertificateVerificationError, error>(Pointer = true)]
[assembly: GoImplement<Conn, net_package.Conn>(Pointer = true)]
[assembly: GoImplement<ECHRejectionError, error>(Pointer = true)]
[assembly: GoImplement<RecordHeaderError, error>]
[assembly: GoImplement<alert, error>]
[assembly: GoImplement<atLeastReader, io_package.Reader>(Pointer = true)]
[assembly: GoImplement<bytes_package.Buffer, io_package.Writer>(Pointer = true)]
[assembly: GoImplement<certificateMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<certificateMsgTLS13, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<certificateRequestMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<certificateRequestMsgTLS13, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<certificateStatusMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<certificateVerifyMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<clientHelloMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<clientKeyExchangeMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<cthWrapper, hash_package.Hash>(Pointer = true)]
[assembly: GoImplement<ecdheKeyAgreement, keyAgreement>(Pointer = true)]
[assembly: GoImplement<encryptedExtensionsMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<endOfEarlyDataMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<finishedMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<go.crypto.rsa_package.PSSOptions, crypto_package.SignerOpts>(Pointer = true)]
[assembly: GoImplement<hash_package.Hash, transcriptHash>]
[assembly: GoImplement<helloRequestMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<keyUpdateMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<listener, net_package.Listener>(Pointer = true)]
[assembly: GoImplement<lruSessionCache, ClientSessionCache>(Pointer = true)]
[assembly: GoImplement<marshalingFunction, go.vendor.golang.org.x.crypto.cryptobyte_package.MarshalingValue>]
[assembly: GoImplement<net_package.Conn, io_package.Reader>]
[assembly: GoImplement<net_package.ΔError, error>]
[assembly: GoImplement<newSessionTicketMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<newSessionTicketMsgTLS13, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<permanentError, error>(Pointer = true)]
[assembly: GoImplement<prefixNonceAEAD, aead>(Pointer = true)]
[assembly: GoImplement<rsaKeyAgreement, keyAgreement>]
[assembly: GoImplement<serverHelloDoneMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<serverHelloMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<serverKeyExchangeMsg, handshakeMessage>(Pointer = true)]
[assembly: GoImplement<xorNonceAEAD, aead>(Pointer = true)]
[assembly: GoImplement<ΔfinishedHash, transcriptHash>(Pointer = true)]
// </InterfaceImplementations>

// <ImplicitConversions>
[assembly: GoImplicitConv<AlertError, alert>(Inverted = true, ValueType = "uint8")]
[assembly: GoImplicitConv<Certificate, ж<Certificate>>(Indirect = true)]
[assembly: GoImplicitConv<Config, ж<Config>>(Indirect = true)]
[assembly: GoImplicitConv<QUICConfig, ж<QUICConfig>>(Indirect = true)]
[assembly: GoImplicitConv<SessionState, ж<SessionState>>]
[assembly: GoImplicitConv<alert, AlertError>(Inverted = true, ValueType = "uint8")]
[assembly: GoImplicitConv<clientHelloMsg, ж<clientHelloMsg>>(Indirect = true)]
[assembly: GoImplicitConv<clientKeyExchangeMsg, ж<clientKeyExchangeMsg>>(Indirect = true)]
[assembly: GoImplicitConv<echContext, ж<echContext>>(Indirect = true)]
[assembly: GoImplicitConv<serverHelloMsg, ж<serverHelloMsg>>(Indirect = true)]
[assembly: GoImplicitConv<serverKeyExchangeMsg, ж<serverKeyExchangeMsg>>(Indirect = true)]
[assembly: GoImplicitConv<Δx509.Certificate, ж<Δx509.Certificate>>(Indirect = true)]
// </ImplicitConversions>

// Go source positions are recorded here, one `GoPositionMap` attribute per converted
// source file in this compilation, so that `runtime.Caller` and the tracebacks built on it
// can name the GO file and line a frame was converted from rather than the emitted C# one.
// Each record carries the Go file's identity and an encoded C#-line to Go-line table
// TOGETHER: a frame either has a record and reports a position that exists in the Go tree,
// or has none - golib, the BCL and hand-written conversions - and reports its own C# position.

// <GoSourcePositionMaps>
[assembly: go.GoPositionMap("crypto/tls/alert.go", "alert.cs", "ABEeggBPqgGCgoKUpoI=")]
[assembly: go.GoPositionMap("crypto/tls/auth.go", "auth.cs", "AFQsopSCgpSCxoKClILGgoKUgILWgoKUgoCC1qQAECqigoKCgoKUgoKCgqrSlKSkpKSklKSkpKSkpKyylKSspAAdOuKCgpaCpKbclJSkpKTWgoKCgtjEloKCgoKmlNyygoKUpriCgpSCpqqipLamgoKopPrW5paClg==")]
[assembly: go.GoPositionMap("crypto/tls/cache.go", "cache.cs", "AEt4AAsCgoKCgqaosq7CgIKmgoKWgoCCpA==", "63-67:1")]
[assembly: go.GoPositionMap("crypto/tls/cipher_suites.go", "cipher_suites.cs", "AGNs8gAQLvYADyaygoKmgoKmADGCAaKCgoKWgoK4ALkBlgOigoCCpICCtqaCgqaCgoKUpoKCgpSokqaClKqiABYygKKAooCkgoKmgoIACRSAooCigKSCgpSCgpamgoKUgoKWpoKClIKClIKClIKUgpaCggACGgAKAoKUgoKUgoKWgoKmgoKUgoKWgoIADhyAooCigKKAooCkgoKskoKCgoKCgpSmgqaC3IIABRCigoKmpoKCgqamgoKCpqaCgoKm", "598-600:1")]
[assembly: go.GoPositionMap("crypto/tls/common.go", "common.cs", "AF1aspSkpKSkpADrAYgEAAoCAApCopSkAFLaAbIAFDayAKkC0gXiiLKCgoIABhLigpSCggAqVAAIBoSogoKCgoKAguykAAYcABAGgoKClIKCgoKUloKCgpSCgqaCqIKCgpSCgoCCpIKCtIKmlAAFIAAPAoKWgqKWgoKmgoKClKaCgoKUpoKCgpSUgoKYABEiooKCgpSCgqaClIKUgpSUpoKCgpSssoKCgpSUpqKCgoKCupSUgpimgoKCpqqigoKCgrgADSDChIKCqIKWlJaCgoCCpIKCgoKAgsqygILKAAkcABIMgoKUgoK6goKClICCAAcQpILKgIKAgrbIgoKUgpSUgpS6goCC7oKogpaCgIKkgpSkpKSkgoKCgqaClLSClPam3IKClIKCpoKmgpSUgpbc0oCCpoKWgqaCgoCCuIKCuAACFPKCgoKCgriClIIAChqCgpaEgoKEAB5AooKUAB5GsoSClAAGEuKChICCgoKUgoKUpoKCgpaCgoKCgoLa4oKEgIKCpAAICoKmgqiSgpSmgoKCpgAJFIKmgg==", "1067-1069:1;1140-1142:1;1150-1152:2;1278-1308:1;1295-1303:1.1;1372-1389:2")]
[assembly: go.GoPositionMap("crypto/tls/common_string.go", "common_string.cs", "/oaigoKCgoKCgoKCgoIADiaClKSkpKSkpKSkgqTGhqKCgoKCAAgagpSCpKSkxoaigoKCgu6CgpQ=")]
[assembly: go.GoPositionMap("crypto/tls/conn.go", "conn.cs", "AHuKAtKokqyyqqKssqyyABY2gKKAooCigKSCgIKUpKqigoKqooKUgoKCgoKUpqKCgoKCgrqSgoKC3qyygpaUpKaSlLTO4oKWgpSWlIKWgpSCgrqCgoIACRaEgqaCAAkWooKCqIKWgoSEgpS0gpSCgpSEgoKUgoKCloKCgsaCgoKWgoKUAAAQxAAIBoKClIKmgoKCgpSCypaCgoKWgoKCgoIABxKCgpaWgqzigIKUgqSCqqKCloKAgoKAAAkUlICC6oKUgoKCtIKCloKWgoSCgoSmgoLGgoKCgoKCgoKUgpTEuIKCgoQAECKgpKKCgoKmgqaCAAgiABACgpSWgpSEgqiAuIKUgIKkpILMgoKWgoKmlIKCgoKUyoKmgoKClICCgIKkuIKCgpSCqIKWlKiClpqClIKUgpSClJaktMiClpKe0pSClICCyIKYopqmspQAAoMBAAKKAdrCgoKClAAKFoKClIKCgpSClKqigpS4goKosoKWlKSkhIKUlqiygoIACEIAEAKCloKogoKUtLSGhsT2gqiCgoKWgoKUpoKCgpaCgqaCgpaCgoKCAAsY4oKClIKCgIK2loKS7IKWgoKCgIKmgoKCpsiUgoKChIKCgpSAgqSCloKAgrjc0oKEgoKUgpbawoKCgtiygpSCgIK2rNKAgqSEuLiWgoKClICCpIKmooKUtLS0gpTGgpTGgpTqtLS0tNi0tLS03ISCloKWAAQaAAsEgoKClIKmhICCpoKEgIKmgpaCAAkYgoKAgoKClLiCAAkI0oKWgoKWgoKCloKWlKSC2oK2goSCgIKkAAgKwoKWgoKUgoKClpSk3ILW0oKCloKCloKEgoKEgoKClIKUgpaClgAFEgAKAoCCpKaWgoSCgIKkgoCCygAHEoSAgrjYtJKCgoKUgqbuloKCgIK4gIKkAAQQsoKWpsKChJSCgpSUAAUgAA0CAAIaAAwG1gAICIKWuJSCgv6CgqKCgJS2sraCtNqChICCpIKWgoSCgriWgpSCloKCuJSCgoKUypSCluiygoL6ooKCgoKClIKCgoKCgoKCgoKUpoKkgoKClKaUgqrigoQACgzygoKClIKUgpQ=", "990-998:1;1536-1542:1;1543-1552:2;1646-1652:1")]
[assembly: go.GoPositionMap("crypto/tls/defaults.go", "defaults.cs", "ABAkgoKmABkygoI=", "50-54:1")]
[assembly: go.GoPositionMap("crypto/tls/ech.go", "ech.cs", "ADNcsoSSgpSClIKCgoKClIKUgpSCgoKUgpSClIKUgoKUgoKClIKUlIKUgoKUgoKClIKCgpSClJaUpoKygIKkgoKAgqSAgqSClIKUgpSCuIKmgpSU1oK4gIKkgIKklKaigoKUhIKClJSEpoKCgoKCgpCSkJKmooKClIKCyoKCgpSCgpSCgoKUgoKUrsKClIKClIKCgpSCgpSCuAAPGoI=", "204-204:1;205-205:2")]
[assembly: go.GoPositionMap("crypto/tls/handshake_client.go", "handshake_client.cs", "AEBYooKCloKCgIKUtoKWgoKUlAAQJIKWgpaCgpSChIKCgriClJaCggAGEIKCgIK4gpSCloKUgpSClJaSgoKCgpSCgIKkgoLc3ICCpIKClKiCgoKUgpSWgoKClIKUgoKUgoKUgriCgoKEgoKUgoKUgoKCgqgAEBzigrqEgoKWgoKUgu6CgILclKiUgoKCzoCCuISAgqaCgoKAgqSCqIKCloKCgpaAgtyCgoKEgpaCAAoWlu7m9IKWqISmzIKogoKUgoKUloKCgoKmgsyUgpSClJSAgrimgpaCqIKCuoKClIKCgoKCpoKWgoK6goKCgt6CuIKWgoKCgIKmprKCgpaCgoKWgoKChKrChIKClsyCloCCpICCpoKCgoCCpICCpICCpLiCgIKCtoCCpICCtoCCpICCpICCpICCpIKAgqSAgraAgqaChNaCgIKCpoKClIKCloLmooSCgpSCgoKWgoKWgqi6gpaEgoKopoCCAAkSgoKohIKCgoKClJKWgoKogoKCgoSCgIKCpoKCqIKCgsyCgoKAgriCgoKUgoCCuIKCpqaAgoKmgoSCgoKWgoKCgoKClIKClIKUgoKCqIKCgpSCgoKWgIK4hKaChISSkoKSgpKUgpaCgqamAAoIgoSAgqaCgpaCgoKCqIKCgoKCgqiAgoKkhISCloKCloKCqIKCqIKCgoKCpoKW+qKClJSUgpSCgqbWooSAgtyCgpSCgoKWgoSCloCCpoLWooKUhIKCloKClIKCgpaCpoKClISCgpaCgoSCgqaihICCpoKCgIKkggAIEoKAgoCCgpTGqsKCgoKCgoKUgoKAgoK2gpaCgoKAgoK23oKUgoKCgsjegpSCgoKCqLS0gqaChIKAgoK4goCCgriqosySgpSkuNyU6tjYuoKCgoKUlILGgtqmooKWsoCCpKiqooKUgpSssoKClICCpIKUgpSmooKClIKC", "272-284:1")]
[assembly: go.GoPositionMap("crypto/tls/handshake_client_tls13.go", "handshake_client_tls13.cs", "ADNawoSCuoKCqIKWgIKmhICCpoKCgIK4goCCpICCuIKCgoKCgsyCgoKChIKCloKCpqaogIKmgoCCpICCpICCpICCpICCpICCpICCpICCpICCpoKCloQACgqihIKCloKCloKClo6CloKCloKCloKCgpSCgpSChKqigpSClIQACwqiuoKCgoKAgqaCgoKCgoKEgoKCloKCgoKCzIKCgoKogILYgsyCgpaCloKCzICSgoKUloIABxCAgoKkgoKClIKmgoKClJSChIKCgoCCpoCCyIKogoKWyoKCgIKmgIK2loCCuIKCloKCgpSEgIKmggAKBqKEgoKWgoKWgoKWgoKUhoKWgpaCgpaClIKClIKCloKCgoKCgoL2goSCgoKClJSCgoKUgoKClIKClIKCgoKUlISCgpaWlIKUhIKClIKWgoKClIKCgpaWAAoGgoSCgpaCgoKWgMqCpISClIKUlIKCqIKClIKUgoKClIKCpoKCluaCqLiCgIKCtpaCgpaCgoSCgqiCgoKUgoKWgoSAgtyCgpaCgoKogoKUgoKUgoKUgoCUgqaAgqbWgrqCgpaCgoKWgoKCloCCupSUhIKCgpSCgoKWhKaChIKWgoCCpJbcgpaEgoKEgIK4gpaChIKmgpaSgpaCgoKUgoKClISAgqamgoSogIKmhIKogoKUlgAIBrKCgpaCqIKUgoKCqIKCloKClpaCgoKCgoKCgpSCgIKm", "321-323:1;443-445:1")]
[assembly: go.GoPositionMap("crypto/tls/handshake_messages.go", "handshake_messages.cs", "ABAogqqikoKUgrqygqrCkoKUgqrCqsKqwgAhRKKClIKCgoKCypSCgoK4lIKCppSCgoK4lIKUlIKUlIKUpIKCpoKCgq7ClIKUgoKCgriUgpSCgoKC3JSClIKCgoLclIKUgoKCgtyUgpSCgoKCgu6UgpSCgoKC3JSClIKCgsqUgpSCgoKygpLulIKUgoKCyoKCkpKCyqSCgoKykpSmgoKS3IKCloKCkoKCgoKmgoKmgpaCkrqmgqzSgoKCloKClIKCgqbaooKUgoKmhKaigpSWloKClIKCgoKClIKUloKWlJaCgpaCgoKChJaClISWkoKUgoKChpSClJSUlILakoKGlLaSgpSCgoKUyJTIgsaSgpSCgoKU2pKClIKCgpTakpS2trKClIKCgpTItrKClIKCgpTIlMiSgpSCgoaUyLaixoKCyJKUgoKUgoKGlJSCgpSCgoSUyMaCqKaCpoIAOXaigoKClIKClIKCgoK4goKUgoKCgoLKgoKCgoKS3IKCgqaCgoKCgriCgoKogoKCgriCgoKmgoKCgriCgoKmgoKWgoKWgoKSgoKClIKEgpK6pqKClJqWlJaCgpaCgoKChJaClISUtLSClLS0goKUgoSUtIKClIKChJTGgsaEyqKCpoTYgoLIlMaCksaClLbGgqimggAJFIKCgoKCgoKCgoLKpIKCppSClIKCgsymgoKEgpSWgoKChJaUgoKUgoSUtIKCyLSCksjGgqjagoKCpoLugoKCgoKUqKaChIKUlJS0tKQAChaCgoKCgoKClIKWgoKCgsymooKUgp6WgoKChJaUgsjGgqgAChaCgoKmhIKCgpTcgpSCgoKCgsqCgoKCgsqCgoKCgpIABxKmgoKEkpiWgoKChJaUtLSCgpSCgoKU2IKClIKCgpTYgoKUgoKClMjGgqjugoKCloKCgoKChIKCgoSCgoKCgoKWpoKCloKCloKCgoKUgoKUgoKWgoKCgoKWAAgSgoKCgoSCgpSClJamgpKCkpSSlJSCgpKCkriCgpKSgpIAChaigpSCmJaChKaygoKUgoKChJSCgoKChJSUlpSChsaCgpSCgoSU2saCuO6CgoKCgoKChKaCgpSC7oKCgoKCgqimopSCmJTagoKCpoLugoKCgoKCgoSmgoKUgoKUgu6CgoKClqaCkgALHpSCgoKUhIKWgoKCgoSEgoSCgoKCgoKCgqiCgoKCgoKCgpamgoKWgoKWgoKCloKCloSCgpSCgoKUgpSCgoKCqIKUgoKClIKChIKCgpSChIKWgpYACBKCgoKCgpSCqKailJKUgoKm7pSCgoKCgoKCgoKEpoKCloKCloKCloTcgqaCAAYkAAsCgIKAgoLIgoKUgg==", "27-33:1;107-114:1;108-113:1.1;110-112:1.1.1;119-123:2;120-122:2.1;128-130:3;135-139:4;136-138:4.1;159-161:5;165-167:6;180-184:7;193-199:8;194-198:8.1;208-214:9;209-213:9.1;223-229:10;224-228:10.1;238-246:11;239-245:11.1;241-243:11.1.1;255-261:12;256-260:12.1;270-274:13;271-273:13.1;283-292:14;284-291:14.1;287-289:14.1.1;301-305:15;302-304:15.1;310-316:16;311-315:16.1;321-337:17;322-329:17.1;324-326:17.1.1;330-336:17.2;332-334:17.2.1;346-368:18;349-353:18.1;354-358:18.2;359-361:18.3;364-366:18.4;751-755:1;752-754:1.1;763-769:2;764-768:2.1;765-767:2.1.1;773-781:3;774-780:3.1;776-778:3.1.1;785-787:4;791-796:5;793-795:5.1;800-802:6;807-811:7;808-810:7.1;815-817:8;821-825:9;822-824:9.1;829-831:10;845-859:11;848-850:11.1;855-857:11.2;1006-1037:1;1007-1036:1.1;1010-1016:1.1.1;1011-1015:1.1.1.1;1012-1014:1.1.1.1.1;1021-1023:1.1.2;1032-1034:1.1.3;1117-1123:1;1158-1176:1;1161-1163:1.1;1164-1166:1.2;1168-1175:1.3;1171-1173:1.3.1;1233-1285:1;1238-1284:1.1;1254-1260:1.1.1;1255-1259:1.1.1.1;1264-1270:1.1.2;1265-1269:1.1.2.1;1274-1282:1.1.3;1275-1281:1.1.3.1;1277-1279:1.1.3.1.1;1445-1456:1;1462-1495:1;1464-1466:1.1;1467-1493:1.2;1474-1479:1.2.1;1476-1478:1.2.1.1;1483-1491:1.2.2;1484-1490:1.2.2.1;1486-1488:1.2.2.1.1;1607-1612:1;1609-1611:1.1;1677-1679:1;1834-1841:1;1838-1840:1.1")]
[assembly: go.GoPositionMap("crypto/tls/handshake_server.go", "handshake_server.cs", "ACtUsoKCloLKltqmooSAgriCgIKklICCpICCpICCpICCpICCpIKAgtqAgqSAgqSAgqSAgqSCgoCCpICCpICCuIKEqNaCgpSCgoKWgoKCgoCCgqS2hIKClIKCgpSCgoSCgpbWgoSChJSCgoKogoKWgpSCgoKUlJSCgoKWgoKWgoKCgpaCgoKUgoSCgoKUlJSCloTcloCClLS0tILGgIKUtILIrLKClJSUgoKCgpSCAAcQgpSqooKCgoKogoKCgu6CluaihIKCloKCgoKCgrqCgoKUhIKClIKCloKUgoKUqKaigoKUgoK2tpSClNiyhIKWgoKCgpSClJSCgpSCgpTMgoKogpaUgoKCpoKolIKWgoKClIKUgpSEqIKUppaCgoKCgoKCgqaihIKmuIKCgoCCpICCpoKAgoK4hNaihIKWgoSCppSAgqSAgqaCgoCCpoKCgoCCuIKCgoKUgpKUgIK4gpSCuIKCAAYQgpSAgriCgIKmgIKmhIKCuoKCgoKWgKakgpaCgqaCgIKCyoKCgpaCgoKUgoKmpoCCggAIEriCgpSCgoKWgoKCgoKUgoKmgoKCqIKAgoKmgIK4hKaChIaSlIKSgpKUgpaChNaihICC3IKClIKCgpaChIKWgIKmgqaigpaChIKCppSCgoKCpoKClIKCqICCpqaihICCpoKCgIKmhNrCgoKCgoCCgqSCgoCCgsqCgpSUloLegpaCgoKCpJSUlpaCgoSC5oK4goCCgrimgoKClg==")]
[assembly: go.GoPositionMap("crypto/tls/handshake_server_tls13.go", "handshake_server_tls13.cs", "ADRegoSCqICCpICCpICCpIKAgqSAgqSAgtqAgqSAgqSAgqaEAA8GgoSogoSCggAKGIKmgoKUqISCloKAgoKmgoKWgoKCAAkSgpaChIKClIKCgqaCgpSCgoiigoKChoKCgoKClKaCgoKCuIKClIKCgpSUhIKCgoKCgpSUgIKCpIKCgpSCgoKClIKCgpSCgoKClIKWgoKClISUgoKCuIKClJSCgqiC9oKEgpaCgoKCpoKWgoKUgpaCgpaCgoKCgpSCpoKClIKCgqiCloKCloKCzIKCgpSClIKUhJaCgIK4gpSCgoKUgoKClIKCgoKWhoSCgIKkgpaCgoKChIKCgpYACAy0ioKUgoKUgoKClICCpKaCloKogpaCgoKUlJSCpoKUhKqigpSClIQACAaCqICCpIKCgoQACBSAgqaAgriCgpaCgoKWgoKUhIKCloKCloKCloKCrLKMlIKCpoKCpoKCpoKCpoKCpoKCpgACIIKEgIKkgIKmgIKmgoKUlpSClISCgpSCloKCgpSCgoKWgoSCgoKUgpaAgqamgqaCloKWlIKCgoKCloCCuISCgoSAgqaCgoSSgpaCgoKUgoKCgJSUpJSEgIKmpoKEqICCupaUlISClJSWgoKClIKCgpa6goCCuKaCgqiCqIKCpqaChIKmgIKmloKU1qKCgriWhIKCgoKCgoKCpoKCgpSCgqa6goCCpISUloCCpqaChKaCgIKCtryCgpaCgoKWgIKmgoCCgri4goKWgoKCqIKClIKClIKClIKAlIKmgILcgIKm1oKWgoKWgoKCloKCloQ=", "188-190:1")]
[assembly: go.GoPositionMap("crypto/tls/key_agreement.go", "key_agreement.cs", "ADpagtbCgpSCgpSEgoKmgoIABxDWgtbCgoKCgoKWgoKUgoKUgoKCgoKokoKClKqigoKClIKCruKCgoKUlIKCgpSClIKUABQk0oKCgoKogpSAgqaCgpSWgoKCgoKChIKCloKCgoKCgpSCgqaCgqaCloSCgpSCgpaCgoKUgoKCgoKClIKChKaigpaCgpSCgpbW4oKUkpSEgoKUgoSCgpaAgqaCgpSEgoKUgoKWgoKCgoSCgoKCgoKWgpSCgqaCgqaCloKClISCgIKk1oKClg==")]
[assembly: go.GoPositionMap("crypto/tls/key_schedule.go", "key_schedule.cs", "AC1KkoKCgoKUkpSCAAwalIKCgpSosoKUqLKClKrC2NKCgtzSgoKC2qKCkoKCggAJFpKCgpSo0oKClKa4goKCgoKC/qKCgpamgpSkpKSkyIKUpKSkpA==", "40-43:1;44-46:2;114-119:1")]
[assembly: go.GoPositionMap("crypto/tls/notboring.go", "notboring.cs", "AAgSgA==")]
[assembly: go.GoPositionMap("crypto/tls/prf.go", "prf.cs", "ABIowoKCqJKCgoSCgoKCgoKChIKCupKChIKChIKCgoSCupKCgoKEABAegpSkgpSkyIKCqqKCgoSCgqqigoKsAAoCgoKEgoKCgoKCgoKCgoKCgoKmgoKCloKClgAQKIKChIKCloKWpoKCloKCqqKCgqqigoKqooKWgpaCgoKWgpaqotyy3LKokrKWtoKClISChIKClIKWgoI=", "68-74:1;271-298:1")]
[assembly: go.GoPositionMap("crypto/tls/quic.go", "quic.cs", "ABowgpSkpKSkAEyYAuKu4qaiyoIACBLigpSCgpSCgIKkqqKCgKakgoKClIKCgpSCgoKokoKUgqba4oKClIKCgpSmgpKCgoKCgoKClIKUgIK2gpQAEBiygoKUgpSClILewoKClIKClIKCqJKuwoKUgoKCvKKClIKClIKCuKaigoCCtqaCAAcQggAHEIKCgpSCuJSmoriCgoCCtqaC3ILcooK4goCCtqaCyoIABBQACwaCuObKtIK0lA==")]
[assembly: go.GoPositionMap("crypto/tls/ticket.go", "ticket.cs", "AGfYAQAIAoKCgpSUgoKClIKCkriClJSClJTKgoKkgoKUgpLcgoKmgoKCpqaCgoKU6JKCkqKCggAAFpSCgoKUlJS0tKSUtLSkgoKClIKUgoKCgpSCgoKUgoKUgoKCgpSCgpSClJSCgoKUlICCgpSkgoKUgoKUlIKUqqIADiLCgoKClNaCgpaCgoKChICCpIKCgpSEgoKEruKCgoKUgoKUpoKCloKCgoSigoKEgpaCgpSChJYACBzygpQAAhDygg==", "118-120:1;121-127:2;123-125:2.1;143-158:3;145-156:3.1;152-154:3.1.1;160-162:4")]
[assembly: go.GoPositionMap("crypto/tls/tls.go", "tls.cs", "AC9GwriSrsLKkgAIFqKCgpSu4oKCgt70hJSCgpQACAqAooCigAACGAAKAqbSgoKCpoKCgqaCgpaCgpSEgriUgoKWgoCCgqQABRLiABAy8qaCgpQAAhgACQKClJQAAhYACAKCgpSCgpQADxbihIKCgoKCgpSClKiCgpSClJaCgoKCgoKUgpSUgpS6goKWgpSWgoKWlIKClILGgoKUgsaCgpSC1pbssoCCpICCxLS2gIKm", "256-256:1")]
// </GoSourcePositionMaps>

namespace go.crypto;

[GoPackage("tls")]
public static partial class tls_package
{
    // C# nested types declared with no access modifier are always private, and the
    // `[GoType]` declarations in this package's converted sources are deliberately
    // bare so they read more like the original Go code. The real accessibility for
    // the types - public for a Go-exported name, internal otherwise - are defined
    // via declarations below.

    // <TypeAccessibility>
    internal partial interface aead {}
    internal partial interface cbcMode {}
    [GoLocalName("binaryMarshaler")] internal partial interface cloneHash_binaryMarshaler {}
    internal partial interface constantTimeHash {}
    internal partial interface handshakeMessage {}
    internal partial interface handshakeMessageWithOriginalBytes {}
    internal partial interface keyAgreement {}
    internal partial interface transcriptHash {}
    internal partial struct activeCert {}
    internal partial struct alert {}
    internal partial struct atLeastReader {}
    internal partial struct cacheEntry {}
    internal partial struct certCache {}
    internal partial struct certificateMsg {}
    internal partial struct certificateMsgTLS13 {}
    internal partial struct certificateRequestMsg {}
    internal partial struct certificateRequestMsgTLS13 {}
    internal partial struct certificateStatusMsg {}
    internal partial struct certificateVerifyMsg {}
    internal partial struct cipherSuite {}
    internal partial struct cipherSuiteTLS13 {}
    internal partial struct clientHandshakeState {}
    internal partial struct clientHandshakeStateTLS13 {}
    internal partial struct clientHelloMsg {}
    internal partial struct clientKeyExchangeMsg {}
    internal partial struct cthWrapper {}
    internal partial struct ecdheKeyAgreement {}
    internal partial struct echConfig {}
    internal partial struct echContext {}
    internal partial struct encryptedExtensionsMsg {}
    internal partial struct endOfEarlyDataMsg {}
    internal partial struct finishedMsg {}
    [GoValueClone("seq", "scratchBuf")] internal partial struct halfConn {}
    internal partial struct helloRequestMsg {}
    internal partial struct keyShare {}
    internal partial struct keySharePrivateKeys {}
    internal partial struct keyUpdateMsg {}
    internal partial struct listener {}
    internal partial struct lruSessionCache {}
    internal partial struct lruSessionCacheEntry {}
    internal partial struct newSessionTicketMsg {}
    internal partial struct newSessionTicketMsgTLS13 {}
    internal partial struct permanentError {}
    [GoValueClone("nonce")] internal partial struct prefixNonceAEAD {}
    internal partial struct pskIdentity {}
    [GoValueClone("eventArr")] internal partial struct quicState {}
    internal partial struct recordType {}
    internal partial struct rsaKeyAgreement {}
    internal partial struct rsaSignatureSchemesᴛ1 {}
    internal partial struct serverHandshakeState {}
    internal partial struct serverHandshakeStateTLS13 {}
    internal partial struct serverHelloDoneMsg {}
    internal partial struct serverHelloMsg {}
    internal partial struct serverKeyExchangeMsg {}
    [GoValueClone("aesKey", "hmacKey")] internal partial struct ticketKey {}
    internal partial struct timeoutError {}
    [GoValueClone("nonceMask")] internal partial struct xorNonceAEAD {}
    internal partial struct ΔfinishedHash {}
    public partial interface ClientSessionCache {}
    public partial struct AlertError {}
    public partial struct Certificate {}
    public partial struct CertificateRequestInfo {}
    public partial struct CertificateVerificationError {}
    public partial struct CipherSuite {}
    public partial struct ClientAuthType {}
    public partial struct ClientHelloInfo {}
    public partial struct ClientSessionState {}
    [GoValueClone("SessionTicketKey")] public partial struct Config {}
    [GoValueClone("clientFinished", "serverFinished", "@in", "@out", "tmp")] public partial struct Conn {}
    public partial struct CurveID {}
    public partial struct Dialer {}
    public partial struct ECHRejectionError {}
    public partial struct QUICConfig {}
    public partial struct QUICConn {}
    public partial struct QUICEncryptionLevel {}
    public partial struct QUICEvent {}
    public partial struct QUICEventKind {}
    public partial struct QUICSessionTicketOptions {}
    [GoValueClone("RecordHeader")] public partial struct RecordHeaderError {}
    public partial struct RenegotiationSupport {}
    public partial struct SessionState {}
    public partial struct SignatureScheme {}
    public partial struct echCipher {}
    public partial struct echExtension {}
    public partial struct ΔConnectionState {}
    // </TypeAccessibility>
}
