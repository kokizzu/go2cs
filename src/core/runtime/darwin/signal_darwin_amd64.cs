// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

[GoType] partial struct sigctxt {
    internal ж<siginfo> info;
    internal @unsafe.Pointer ctxt;
}

//go:nosplit
//go:nowritebarrierrec
[GoRecv] internal static ж<regs64> regs(this ref sigctxt c) {
    return ((ж<ucontext>)(uintptr)(c.ctxt)).Value.uc_mcontext.of(mcontext64.Ꮡss);
}

[GoRecv] internal static uint64 rax(this ref sigctxt c) {
    return (~c.regs()).rax;
}

[GoRecv] internal static uint64 rbx(this ref sigctxt c) {
    return (~c.regs()).rbx;
}

[GoRecv] internal static uint64 rcx(this ref sigctxt c) {
    return (~c.regs()).rcx;
}

[GoRecv] internal static uint64 rdx(this ref sigctxt c) {
    return (~c.regs()).rdx;
}

[GoRecv] internal static uint64 rdi(this ref sigctxt c) {
    return (~c.regs()).rdi;
}

[GoRecv] internal static uint64 rsi(this ref sigctxt c) {
    return (~c.regs()).rsi;
}

[GoRecv] internal static uint64 rbp(this ref sigctxt c) {
    return (~c.regs()).rbp;
}

[GoRecv] internal static uint64 rsp(this ref sigctxt c) {
    return (~c.regs()).rsp;
}

[GoRecv] internal static uint64 r8(this ref sigctxt c) {
    return (~c.regs()).r8;
}

[GoRecv] internal static uint64 r9(this ref sigctxt c) {
    return (~c.regs()).r9;
}

[GoRecv] internal static uint64 r10(this ref sigctxt c) {
    return (~c.regs()).r10;
}

[GoRecv] internal static uint64 r11(this ref sigctxt c) {
    return (~c.regs()).r11;
}

[GoRecv] internal static uint64 r12(this ref sigctxt c) {
    return (~c.regs()).r12;
}

[GoRecv] internal static uint64 r13(this ref sigctxt c) {
    return (~c.regs()).r13;
}

[GoRecv] internal static uint64 r14(this ref sigctxt c) {
    return (~c.regs()).r14;
}

[GoRecv] internal static uint64 r15(this ref sigctxt c) {
    return (~c.regs()).r15;
}

//go:nosplit
//go:nowritebarrierrec
[GoRecv] internal static uint64 rip(this ref sigctxt c) {
    return (~c.regs()).rip;
}

[GoRecv] internal static uint64 rflags(this ref sigctxt c) {
    return (~c.regs()).rflags;
}

[GoRecv] internal static uint64 cs(this ref sigctxt c) {
    return (~c.regs()).cs;
}

[GoRecv] internal static uint64 fs(this ref sigctxt c) {
    return (~c.regs()).fs;
}

[GoRecv] internal static uint64 gs(this ref sigctxt c) {
    return (~c.regs()).gs;
}

[GoRecv] internal static uint64 sigcode(this ref sigctxt c) {
    return (uint64)(~c.info).si_code;
}

[GoRecv] internal static uint64 sigaddr(this ref sigctxt c) {
    return (~c.info).si_addr;
}

[GoRecv] internal static void set_rip(this ref sigctxt c, uint64 x) {
    c.regs().Value.rip = x;
}

[GoRecv] internal static void set_rsp(this ref sigctxt c, uint64 x) {
    c.regs().Value.rsp = x;
}

[GoRecv] internal static void set_sigcode(this ref sigctxt c, uint64 x) {
    c.info.Value.si_code = (int32)x;
}

[GoRecv] internal static void set_sigaddr(this ref sigctxt c, uint64 x) {
    c.info.Value.si_addr = x;
}

//go:nosplit
[GoRecv] internal static void fixsigcode(this ref sigctxt c, uint32 sig) {
    var exprᴛ1 = sig;
    if (exprᴛ1 == _SIGTRAP) {
        var pc = (uintptr)c.rip();
        var code = (ж<array<byte>>)(uintptr)((@unsafe.Pointer)(pc - 2));
        if (code.Value[1] != 0xCC && (code.Value[0] != 0xCD || code.Value[1] != 3)) {
            // OS X sets c.sigcode() == TRAP_BRKPT unconditionally for all SIGTRAPs,
            // leaving no way to distinguish a breakpoint-induced SIGTRAP
            // from an asynchronous signal SIGTRAP.
            // They all look breakpoint-induced by default.
            // Try looking at the code to see if it's a breakpoint.
            // The assumption is that we're very unlikely to get an
            // asynchronous SIGTRAP at just the moment that the
            // PC started to point at unmapped memory.
            // OS X will leave the pc just after the INT 3 instruction.
            // INT 3 is usually 1 byte, but there is a 2-byte form.
            // SIGTRAP on something other than INT 3.
            c.set_sigcode(_SI_USER);
        }
    }
    else if (exprᴛ1 == _SIGSEGV) {
        if (c.sigcode() == _SI_USER) {
            // x86-64 has 48-bit virtual addresses. The top 16 bits must echo bit 47.
            // The hardware delivers a different kind of fault for a malformed address
            // than it does for an attempt to access a valid but unmapped address.
            // OS X 10.9.2 mishandles the malformed address case, making it look like
            // a user-generated signal (like someone ran kill -SEGV ourpid).
            // We pass user-generated signals to os/signal, or else ignore them.
            // Doing that here - and returning to the faulting code - results in an
            // infinite loop. It appears the best we can do is rewrite what the kernel
            // delivers into something more like the truth. The address used below
            // has very little chance of being the one that caused the fault, but it is
            // malformed, it is clearly not a real pointer, and if it does get printed
            // in real life, people will probably search for it and find this code.
            // There are no Google hits for b01dfacedebac1e or 0xb01dfacedebac1e
            // as I type this comment.
            //
            // Note: if this code is removed, please consider
            // enabling TestSignalForwardingGo for darwin-amd64 in
            // misc/cgo/testcarchive/carchive_test.go.
            c.set_sigcode(_SI_USER + 1);
            c.set_sigaddr(0xb01dfacedebac1eUL);
        }
    }

}

} // end runtime_package
