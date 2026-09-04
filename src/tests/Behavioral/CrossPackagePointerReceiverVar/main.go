// CrossPackagePointerReceiverVar guards a pointer-receiver method call on ANOTHER package's
// package-level value var: `lib.Boxed.RLock()` must operate on lib's own storage, exactly as
// `syscall.ForkLock.RLock()` does in os.Pipe and net.sysSocket on darwin, where the copying emission
// `Ꮡ(syscall.ForkLock).RLock()` locked one fresh copy and unlocked another
// (`fatal error: sync: RUnlock of unlocked RWMutex`, the first full darwin behavioral census).
//
// Two shapes, both exercised BEFORE any in-library use of the var (see xpkgmulib.Touch for the mask):
// a var the library boxes because it calls the methods itself, and a var the library never touches.
package main

import (
	"fmt"

	lib "go/xpkgmu"
)

func main() {
	lib.Boxed.RLock()
	lib.Boxed.RUnlock()
	fmt.Println("boxed: rlock/runlock ok")

	lib.Plain.RLock()
	lib.Plain.RUnlock()
	fmt.Println("plain: rlock/runlock ok")

	lib.Boxed.Lock()
	lib.Boxed.Unlock()
	lib.Plain.Lock()
	lib.Plain.Unlock()
	fmt.Println("lock/unlock ok")

	lib.Cnt.Inc()
	lib.Cnt.Inc()
	fmt.Println("counter:", lib.Cnt.Value())

	lib.Touch()
	lib.Boxed.RLock()
	lib.Boxed.RUnlock()
	fmt.Println("after touch ok")
}
