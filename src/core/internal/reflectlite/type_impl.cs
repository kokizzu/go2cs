// type_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

namespace go.@internal;

partial class reflectlite_package
{
    // Implementation of resolveTypeOff
    internal static partial unsafe_package.Pointer resolveTypeOff(unsafe_package.Pointer rtype, int32 off)
    {
        return default!;
    }

    // Implementation of resolveNameOff
    internal static partial unsafe_package.Pointer resolveNameOff(unsafe_package.Pointer ptrInModule, int32 off)
    {
        return default!;
    }

    // ==== Phase-3 write-back: the errors.As TYPE surface (rtype.Elem/Implements/AssignableTo) ====
    // The auto forms read descriptor sub-records (ptrType.Elem, interface method tables) that the
    // Phase-1 synthetic abi.Type never populates — Elem panicked "Elem of invalid type" and
    // implements() reinterpreted the descriptor as an eface. Bridged over the abi.Type's carried
    // System.Type and the SAME golib method-set machinery emitted asserts use (GoReflect), so
    // reflection and direct asserts can never disagree. See docs/phase4/DESIGN-reflection-bridge.md.

    // Elem returns the element type of a pointer/slice/array/map/chan type.
    internal static ΔType Elem(this rtype t)
    {
        return toType(abi_package.synthType(GoReflect.ElementType(t.Type == nil ? null : t.Type.Value.sysType)));
    }

    // Implements reports whether the type implements the interface type u (Go method-set rules:
    // nominal or structural via golib StructurallyImplements).
    internal static bool Implements(this rtype t, ΔType u)
    {
        if (u == default!)
            throw panic("reflect: nil type passed to Type.Implements");

        if (u.Kind() != Interface)
            throw panic("reflect: non-interface type passed to Type.Implements");

        return GoReflect.GoImplements(sysTypeOfLiteType(u), t.Type == nil ? null : t.Type.Value.sysType);
    }

    // AssignableTo reports whether a value of the type is assignable to type u — identity on the
    // carried System.Type (named-type distinctness is free: distinct Go types are distinct managed
    // types), or interface-implements. The Go unnamed<->named underlying rule is deferred with a
    // named consumer (encoding/binary).
    internal static bool AssignableTo(this rtype t, ΔType u)
    {
        if (u == default!)
            throw panic("reflect: nil type passed to Type.AssignableTo");

        System.Type? uu = sysTypeOfLiteType(u);
        System.Type? tt = t.Type == nil ? null : t.Type.Value.sysType;

        if (uu is not null && uu == tt)
            return true;

        return GoReflect.GoImplements(uu, tt);
    }

    // ==== The type NAME (rtype.String) ====
    // The auto form reads a name OFFSET into the linker-built name blob — `t.nameOff(t.Str).Name()`
    // — which a synthesized descriptor never populates, so every reflectlite.TypeOf(x).String()
    // answered "". Silently: "" is a legal name for an unnamed type, so nothing panicked and the
    // empty string simply propagated into whatever the caller was building (context's `stringify`
    // fallback printed `WithValue(, c1k1)` where Go prints `WithValue(context_test.key1, c1k1)`).
    // Answered from the descriptor's carried System.Type through the SAME golib naming that backs
    // reflect's hand-owned rtype.String and %T, so the full bridge and the mini-bridge cannot
    // disagree about what a type is called. Array dims ride along as they do on the reflect side —
    // a descriptor that knows its length renders Go's [N]T rather than []T.

    // String returns the Go source type string (`context_test.key1`, `[]int`, `*T`).
    internal static @string String(this rtype t)
    {
        if (t.Type == nil)
            return (@string)GoReflect.GoTypeName(null);

        return (@string)GoReflect.GoTypeName(t.Type.Value.sysType, t.Type.Value.arrayDims);
    }

    // sysTypeOfLiteType recovers the managed System.Type a reflectlite Type wrapper describes
    // (the rtype's abi.Type carries it — synthType stamped it).
    private static System.Type? sysTypeOfLiteType(ΔType u)
    {
        var (rt, ok) = u._<rtype>(ᐧ);
        return ok && rt.Type != nil ? rt.Type.Value.sysType : null;
    }
}
