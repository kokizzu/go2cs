// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// Export guts for testing.
namespace go;

using sys = @internal.runtime.sys_package;
using @unsafe = unsafe_package;
using @internal.runtime;
using static global::go.runtime_package;

partial class runtime_internal_test_package {

public static UntypedInt MaxArgs => /* maxArgs */ 42;

public static Action OsYield;
internal static void initᴛOsYield() { OsYield = osyield; }
public static ж<uint32> TimeBeginPeriodRetValue;
internal static void initᴛTimeBeginPeriodRetValue() { TimeBeginPeriodRetValue = ᏑtimeBeginPeriodRetValue; }

public static int32 NumberOfProcessors() {
    ref var info = ref heap(new global::go.runtime_package.systeminfo(), out var Ꮡinfo);
    stdcall1(_GetSystemInfo, (uintptr)Ꮡinfo);
    return (int32)info.dwnumberofprocessors;
}

[GoType] public partial struct ContextStub {
    internal partial ref global::go.runtime_package.context context { get; }
}

public static uintptr GetPC(this ContextStub c) {
    return c.context.ip();
}

public static ж<ContextStub> NewContextStub() {
    ref var ctx = ref heap(new global::go.runtime_package.context(), out var Ꮡctx);
    ctx.set_ip(sys.GetCallerPC());
    ctx.set_sp(sys.GetCallerSP());
    ctx.set_fp(getcallerfp());
    return Ꮡ(new ContextStub(ctx.ΔClone()));
}

} // end runtime_internal_test_package
