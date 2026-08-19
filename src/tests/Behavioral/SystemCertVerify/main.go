// SystemCertVerify exercises the Windows CryptoAPI certificate-chain seam:
// crypto/x509's systemVerify, which is what Certificate.Verify routes to on
// Windows whenever VerifyOptions.Roots is nil.
//
// Two structures cross the managed/native boundary in that one call and NEITHER
// can do so by address under go2cs. CERT_CHAIN_PARA is handed to the kernel to
// READ while the converted CertChainPara holds RequestedUsage.Usage.
// UsageIdentifiers as a pointer-to-pointer and CacheResync as a pointer — both
// managed references — so its 80-byte native size is written over a much smaller
// managed object and every field past the first is read from the wrong offset,
// dwUrlRetrievalTimeout (a blocking network budget) among them. The
// CERT_CHAIN_CONTEXT the kernel WRITES is read back the same way.
//
// The assertions below are VALUES, not liveness: a chain call that silently
// misreads its parameters can still answer, and the answer is what has to be
// right. The certificate is generated here rather than embedded so the program
// never expires and never depends on the host's root store: a self-signed leaf
// chains to no trusted root on any Windows machine, which is a determinate
// verdict Go and C# must agree on.
package main

import (
	"bytes"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/x509"
	"crypto/x509/pkix"
	"fmt"
	"math/big"
	"syscall"
	"time"
	"unsafe"
)

func main() {
	key, err := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	if err != nil {
		fmt.Println("generate key error:", err)
		return
	}

	// NotBefore/NotAfter straddle "now" by an hour, so the only thing wrong with
	// this certificate is that nothing trusts it -- the verdict stays
	// UnknownAuthority rather than drifting to Expired with the calendar.
	now := time.Now()
	template := &x509.Certificate{
		SerialNumber:          big.NewInt(20260818),
		Subject:               pkix.Name{CommonName: "go2cs.example"},
		NotBefore:             now.Add(-time.Hour),
		NotAfter:              now.Add(time.Hour),
		KeyUsage:              x509.KeyUsageDigitalSignature | x509.KeyUsageCertSign,
		ExtKeyUsage:           []x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth},
		DNSNames:              []string{"go2cs.example"},
		IsCA:                  true,
		BasicConstraintsValid: true,
	}

	der, err := x509.CreateCertificate(rand.Reader, template, template, &key.PublicKey, key)
	if err != nil {
		fmt.Println("create certificate error:", err)
		return
	}
	fmt.Println("created der:", len(der) > 0)

	cert, err := x509.ParseCertificate(der)
	if err != nil {
		fmt.Println("parse certificate error:", err)
		return
	}
	fmt.Println("parsed cn:", cert.Subject.CommonName)

	// Roots nil -> systemVerify. The default KeyUsages is ExtKeyUsageServerAuth,
	// which is what makes RequestedUsage.Usage.UsageIdentifiers a NON-EMPTY array
	// of C string pointers: the field whose managed form has no native meaning at
	// all, exercised rather than skipped.
	chains, err := cert.Verify(x509.VerifyOptions{})
	fmt.Println("verify chains:", len(chains))
	fmt.Println("verify error:", err)
	fmt.Println("unknown authority:", isUnknownAuthority(err))

	// The same call with a DNS name attached takes the second CryptoAPI route --
	// CertVerifyCertificateChainPolicy with an SSL policy parameter -- when the
	// chain is trusted, and must still reach the same verdict when it is not.
	_, err = cert.Verify(x509.VerifyOptions{DNSName: "go2cs.example"})
	fmt.Println("verify with dnsname error:", err)

	// A name the certificate does not carry is rejected by Go's own hostname
	// check before any platform call, which pins that this program's Windows
	// path is reached through the options above and not by accident.
	fmt.Println("hostname mismatch:", cert.VerifyHostname("other.example") != nil)

	walkChain(der)
}

// walkChain drives the same CryptoAPI sequence crypto/x509's systemVerify drives,
// but reads the RESULT back by value: the store handle the context reports, the
// element count of the simple chain, and the leaf's own DER recovered through
// CertChainElement -> CertContext -> EncodedCert. A verdict alone cannot tell a
// correct chain walk from a wrong one -- an untrusted-root answer is what a
// misread status produces too -- so the round trip is the evidence.
func walkChain(der []byte) {
	ctx, err := syscall.CertCreateCertificateContext(syscall.X509_ASN_ENCODING|syscall.PKCS_7_ASN_ENCODING, &der[0], uint32(len(der)))
	if err != nil {
		fmt.Println("create context error:", err)
		return
	}
	defer syscall.CertFreeCertificateContext(ctx)

	store, err := syscall.CertOpenStore(syscall.CERT_STORE_PROV_MEMORY, 0, 0, syscall.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG, 0)
	if err != nil {
		fmt.Println("open store error:", err)
		return
	}
	defer syscall.CertCloseStore(store, 0)

	// The `**CertContext` out-parameter, and then the field read that decides the
	// next call's fourth argument -- the pair that made the parameter fix alone
	// insufficient.
	var storeCtx *syscall.CertContext
	if err := syscall.CertAddCertificateContextToStore(store, ctx, syscall.CERT_STORE_ADD_ALWAYS, &storeCtx); err != nil {
		fmt.Println("add to store error:", err)
		return
	}
	defer syscall.CertFreeCertificateContext(storeCtx)

	fmt.Println("store context non-nil:", storeCtx != nil)
	fmt.Println("store handle round-trips:", storeCtx.Store == store)
	fmt.Println("store context der length:", int(storeCtx.Length) == len(der))

	// CERT_CHAIN_PARA, with a NON-EMPTY UsageIdentifiers: an array of C string
	// pointers into Go byte slices, which is the field that has no native form at
	// all. serverAuth, spelled the way crypto/x509 spells it.
	serverAuth := []byte("1.3.6.1.5.5.7.3.1\x00")
	oids := []*byte{&serverAuth[0]}

	para := new(syscall.CertChainPara)
	para.Size = uint32(unsafe.Sizeof(*para))
	para.RequestedUsage.Type = syscall.USAGE_MATCH_TYPE_OR
	para.RequestedUsage.Usage.Length = uint32(len(oids))
	para.RequestedUsage.Usage.UsageIdentifiers = &oids[0]

	var chainCtx *syscall.CertChainContext
	if err := syscall.CertGetCertificateChain(syscall.Handle(0), storeCtx, nil, storeCtx.Store, para, 0, 0, &chainCtx); err != nil {
		fmt.Println("get chain error:", err)
		return
	}
	defer syscall.CertFreeCertificateChain(chainCtx)

	fmt.Println("chain count:", chainCtx.ChainCount)
	fmt.Println("chain reports untrusted root:", chainCtx.TrustStatus.ErrorStatus&syscall.CERT_TRUST_IS_UNTRUSTED_ROOT != 0)

	simpleChains := unsafe.Slice(chainCtx.Chains, chainCtx.ChainCount)
	last := simpleChains[chainCtx.ChainCount-1]
	fmt.Println("simple chain elements:", last.NumElements)

	elements := unsafe.Slice(last.Elements, last.NumElements)
	leaf := elements[0].CertContext
	encoded := unsafe.Slice(leaf.EncodedCert, leaf.Length)
	fmt.Println("leaf der round-trips:", bytes.Equal(encoded, der))

	checkSSLPolicy(chainCtx)
}

// checkSSLPolicy drives CertVerifyCertificateChainPolicy -- the second CryptoAPI
// call systemVerify makes, and the one whose CERT_CHAIN_POLICY_PARA carries
// pvExtraPolicyPara as an opaque unsafe.Pointer over an
// SSL_EXTRA_CERT_CHAIN_POLICY_PARA whose ServerName is itself a pointer: the
// MINT-SITE round trip, built here exactly the way crypto/x509's
// checkChainSSLServerPolicy builds it. The evidence is the VALUE of
// status.Error under three parameterizations: with unknown-CA errors waived, a
// MATCHING server name answers 0 and a MISMATCHED one answers
// CERT_E_CN_NO_MATCH -- an answer crypt32 can only give if the name crossed the
// boundary intact -- and with nothing waived the same chain answers
// CERT_E_UNTRUSTEDROOT, which pins that the chain context reached the call as
// itself and that the status wrote back from the right offsets.
func checkSSLPolicy(chainCtx *syscall.CertChainContext) {
	// CERT_CHAIN_POLICY_ALLOW_UNKNOWN_CA_FLAG: waive the untrusted root this
	// program's certificate has BY DESIGN, so the name check's own verdict
	// shows through. Package syscall does not carry it; the value is
	// wincrypt.h's.
	const allowUnknownCA = 0x00000010

	fmt.Println("policy match:", sslPolicyError(chainCtx, "go2cs.example", allowUnknownCA))
	fmt.Println("policy mismatch:", sslPolicyError(chainCtx, "other.example", allowUnknownCA))
	fmt.Println("policy untrusted:", sslPolicyError(chainCtx, "go2cs.example", 0))
}

func sslPolicyError(chainCtx *syscall.CertChainContext, serverName string, flags uint32) string {
	servernamep, err := syscall.UTF16PtrFromString(serverName)
	if err != nil {
		return "utf16 error"
	}

	sslPara := &syscall.SSLExtraCertChainPolicyPara{
		AuthType:   syscall.AUTHTYPE_SERVER,
		ServerName: servernamep,
	}
	sslPara.Size = uint32(unsafe.Sizeof(*sslPara))

	para := &syscall.CertChainPolicyPara{
		Flags:           flags,
		ExtraPolicyPara: (syscall.Pointer)(unsafe.Pointer(sslPara)),
	}
	para.Size = uint32(unsafe.Sizeof(*para))

	status := syscall.CertChainPolicyStatus{}
	if err := syscall.CertVerifyCertificateChainPolicy(syscall.CERT_CHAIN_POLICY_SSL, chainCtx, para, &status); err != nil {
		return "call error: " + err.Error()
	}

	switch status.Error {
	case 0:
		return "ok"
	case syscall.CERT_E_CN_NO_MATCH:
		return "cn mismatch"
	case syscall.CERT_E_UNTRUSTEDROOT:
		return "untrusted root"
	default:
		return fmt.Sprintf("0x%08x", status.Error)
	}
}

func isUnknownAuthority(err error) bool {
	_, ok := err.(x509.UnknownAuthorityError)
	return ok
}
