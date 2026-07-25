// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

partial class runtime_package {

internal static readonly UntypedInt _CONTEXT_CONTROL = 0x100001;

[GoType] partial struct m128a {
    internal uint64 low;
    internal int64 high;
}

[GoType] partial struct context {
    internal uint64 p1home;
    internal uint64 p2home;
    internal uint64 p3home;
    internal uint64 p4home;
    internal uint64 p5home;
    internal uint64 p6home;
    internal uint32 contextflags;
    internal uint32 mxcsr;
    internal uint16 segcs;
    internal uint16 segds;
    internal uint16 seges;
    internal uint16 segfs;
    internal uint16 seggs;
    internal uint16 segss;
    internal uint32 eflags;
    internal uint64 dr0;
    internal uint64 dr1;
    internal uint64 dr2;
    internal uint64 dr3;
    internal uint64 dr6;
    internal uint64 dr7;
    internal uint64 rax;
    internal uint64 rcx;
    internal uint64 rdx;
    internal uint64 rbx;
    internal uint64 rsp;
    internal uint64 rbp;
    internal uint64 rsi;
    internal uint64 rdi;
    internal uint64 r8;
    internal uint64 r9;
    internal uint64 r10;
    internal uint64 r11;
    internal uint64 r12;
    internal uint64 r13;
    internal uint64 r14;
    internal uint64 r15;
    internal uint64 rip;
    internal array<byte> anon0 = new(512);
    internal array<m128a> vectorregister = new(26);
    internal uint64 vectorcontrol;
    internal uint64 debugcontrol;
    internal uint64 lastbranchtorip;
    internal uint64 lastbranchfromrip;
    internal uint64 lastexceptiontorip;
    internal uint64 lastexceptionfromrip;
}

[GoRecv] internal static uintptr ip(this ref context c) {
    return (uintptr)c.rip;
}

[GoRecv] internal static uintptr sp(this ref context c) {
    return (uintptr)c.rsp;
}

// AMD64 does not have link register, so this returns 0.
[GoRecv] internal static uintptr lr(this ref context c) {
    return 0;
}

[GoRecv] internal static void set_lr(this ref context c, uintptr x) {
}

[GoRecv] internal static void set_ip(this ref context c, uintptr x) {
    c.rip = (uint64)x;
}

[GoRecv] internal static void set_sp(this ref context c, uintptr x) {
    c.rsp = (uint64)x;
}

[GoRecv] internal static void set_fp(this ref context c, uintptr x) {
    c.rbp = (uint64)x;
}

internal static void prepareContextForSigResume(ж<context> Ꮡc) {
    ref var c = ref Ꮡc.Value;

    c.r8 = c.rsp;
    c.r9 = c.rip;
}

internal static void dumpregs(ж<context> Ꮡr) {
    ref var r = ref Ꮡr.Value;

    print((@string)"rax     ", ((Δhex)r.rax), (@string)"\n");
    print((@string)"rbx     ", ((Δhex)r.rbx), (@string)"\n");
    print((@string)"rcx     ", ((Δhex)r.rcx), (@string)"\n");
    print((@string)"rdx     ", ((Δhex)r.rdx), (@string)"\n");
    print((@string)"rdi     ", ((Δhex)r.rdi), (@string)"\n");
    print((@string)"rsi     ", ((Δhex)r.rsi), (@string)"\n");
    print((@string)"rbp     ", ((Δhex)r.rbp), (@string)"\n");
    print((@string)"rsp     ", ((Δhex)r.rsp), (@string)"\n");
    print((@string)"r8      ", ((Δhex)r.r8), (@string)"\n");
    print((@string)"r9      ", ((Δhex)r.r9), (@string)"\n");
    print((@string)"r10     ", ((Δhex)r.r10), (@string)"\n");
    print((@string)"r11     ", ((Δhex)r.r11), (@string)"\n");
    print((@string)"r12     ", ((Δhex)r.r12), (@string)"\n");
    print((@string)"r13     ", ((Δhex)r.r13), (@string)"\n");
    print((@string)"r14     ", ((Δhex)r.r14), (@string)"\n");
    print((@string)"r15     ", ((Δhex)r.r15), (@string)"\n");
    print((@string)"rip     ", ((Δhex)r.rip), (@string)"\n");
    print((@string)"rflags  ", ((Δhex)(uint64)r.eflags), (@string)"\n");
    print((@string)"cs      ", ((Δhex)(uint64)r.segcs), (@string)"\n");
    print((@string)"fs      ", ((Δhex)(uint64)r.segfs), (@string)"\n");
    print((@string)"gs      ", ((Δhex)(uint64)r.seggs), (@string)"\n");
}

[GoType] partial struct _DISPATCHER_CONTEXT {
    internal uint64 controlPc;
    internal uint64 imageBase;
    internal uintptr functionEntry;
    internal uint64 establisherFrame;
    internal uint64 targetIp;
    internal ж<context> context;
    internal uintptr languageHandler;
    internal uintptr handlerData;
}

[GoRecv] internal static ж<context> ctx(this ref _DISPATCHER_CONTEXT c) {
    return c.context;
}

} // end runtime_package
