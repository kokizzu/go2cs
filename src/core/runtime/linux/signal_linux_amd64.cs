// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using goarch = @internal.goarch_package;
using @unsafe = unsafe_package;
using @internal;

partial class runtime_package {

[GoType] partial struct sigctxt {
    internal ж<siginfo> info;
    internal @unsafe.Pointer ctxt;
}

//go:nosplit
//go:nowritebarrierrec
[GoRecv] internal static ж<sigcontext> regs(this ref sigctxt c) {
    return Ꮡ(((ж<ucontext>)(uintptr)(c.ctxt)).Value.uc_mcontext).Reinterpret<mcontext, sigcontext>();
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
    return (~c.regs()).eflags;
}

[GoRecv] internal static uint64 cs(this ref sigctxt c) {
    return (uint64)(~c.regs()).cs;
}

[GoRecv] internal static uint64 fs(this ref sigctxt c) {
    return (uint64)(~c.regs()).fs;
}

[GoRecv] internal static uint64 gs(this ref sigctxt c) {
    return (uint64)(~c.regs()).gs;
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
    ((ж<uintptr>)(uintptr)((uintptr)add(new @unsafe.Pointer(c.info), 2 * goarch.PtrSize))).Value = (uintptr)x;
}

} // end runtime_package
