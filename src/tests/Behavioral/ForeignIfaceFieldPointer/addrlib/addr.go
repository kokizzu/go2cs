// Guards `%#v` of a pointer held in an interface field whose type and interface come from
// ANOTHER assembly -- so the Go-syntax walk crosses an assembly boundary to reach the
// pointee.
//
// It was built to test a hypothesis that the EMISSION then falsified, and that is recorded
// here so nobody re-runs it: the guess was that a consumer's cast mints the
// <Concrete>ж<Iface> adapter in the CONSUMER's assembly, making this the production twin of
// the test dimension (where a _test.go's adapter for a production type is minted in the test
// assembly). It does not. The converter attributes the cast to the DECLARING package --
// `[assembly: GoImplement<UnixAddr, Addr>(Pointer = true)]` lands in addrlib's own
// package_info.cs and the consumer references `addrlib.UnixAddrжAddr` -- so a cross-PACKAGE
// production cast is NOT the cross-assembly minting shape. Only a cast appearing solely in a
// _test.go reaches that, because its record lands in package_test_info.cs, which is why the
// remaining discriminator needs a `-tests` dimension run and not another behavioral project.
//
// What survives is the axis this genuinely covers, which the same-package twin
// (GoSyntaxIfaceFieldPointer) does not: the walk resolving a pointee across an assembly
// boundary. Both render identically to `go run`.
package addrlib

type Addr interface {
	Network() string
	String() string
}

type UnixAddr struct {
	Name string
	Net  string
}

func (a *UnixAddr) Network() string { return a.Net }
func (a *UnixAddr) String() string  { return a.Name }
