// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// MakeFunc implementation.
using go;
using System;
using abi = go.@internal.abi_package;

// Hand-finished conversion (the reflection bridge — Phase 4, MakeFunc). The auto body is runtime
// machinery end to end: it reinterprets the descriptor into a funcType sub-record no synthesized
// abi.Type has behind it (the box comes back zero, Kind 0), asks funcLayout for a stack map over
// that nothing ("reflect: funcLayout of non-func type <nil>" — net/http/httptrace's compose was the
// first operational hit), and pairs an assembly stub (makeFuncStub) with a closure context the
// managed runtime cannot execute. The managed form is the exact INVERSE of the hand-owned
// Value.Call (value_impl.cs): where Call marshals a slice<Value> into a delegate's DynamicInvoke,
// MakeFunc builds a delegate of the descriptor's carried System.Type (golib
// GoReflect.MakeGoFuncDelegate — expression-compiled, one factory per delegate type) whose
// invocation marshals its CLR arguments INTO a slice<Value> (makeTypedValue, so an interface-typed
// parameter reports Kind Interface and a [N]byte parameter carries the descriptor's funcParamDims
// cargo), runs fn, and marshals the result Values back out under the same assignability rule Call's
// arguments use (marshalIntoSlot — one renderer for both directions). The returned Value carries
// typ's OWN descriptor box, so Type() interns to the very wrapper the caller passed in (canonType
// keys on the sysType plus the dims cargo, and both ride the box). makeMethodValue's identical
// funcLayout read stays AUTO deliberately: it is only reachable through flagMethod, which the
// bridge never sets — Value.Method binds the receiver into an ordinary delegate instead
// (GoReflect.GoMethodValue), so no Value ever takes that path. The converter skips the auto
// MakeFunc via the manualConversionFuncs registry (go2cs/manualTypeOperations.go); this module
// marker also makes go2cs skip re-converting this file.
// See docs/phase4/DESIGN-reflection-bridge.md.

[module: GoManualConversion]

namespace go;

partial class reflect_package {

// MakeFunc returns a new function of the given [Type]
// that wraps the function fn. When called, that new function
// does the following:
//
//   - converts its arguments to a slice of Values.
//   - runs results := fn(args).
//   - returns the results as a slice of Values, one per formal result.
//
// The implementation fn can assume that the argument [Value] slice
// has the number and type of arguments given by typ.
// If typ describes a variadic function, the final Value is itself
// a slice representing the variadic arguments, as in the
// body of a variadic function. The result Value slice returned by fn
// must have the number and type of results given by typ.
//
// The [Value.Call] method allows the caller to invoke a typed function
// in terms of Values; in contrast, MakeFunc allows the caller to implement
// a typed function in terms of Values.
//
// The Examples section of the documentation includes an illustration
// of how to use MakeFunc to build a swap function for different types.
public static ΔValue MakeFunc(ΔType typ, Func<slice<ΔValue>, slice<ΔValue>> fn) {
    if (typ == default! || typ.Kind() != Func) {
        throw panic("reflect: call of MakeFunc with non-Func type");
    }
    var Ꮡt = typ.common();
    System.Type? st = Ꮡt == nil ? null : Ꮡt.Value.sysType;
    if (st is null || !GoReflect.TryFuncShape(st, out System.Type[]? ins, out System.Type[]? outs, out bool isVariadic)) {
        throw panic("reflect: call of MakeFunc with non-Func type");
    }
    if (isVariadic) {
        // The delegate factory would refuse the Span<T> tail anyway (no expression tree can carry a
        // byref-like parameter); refusing HERE names the operation rather than the mechanism. The
        // route that exists is the reverse of GoReflect.InvokeVariadic's typed family trampolines —
        // unbuilt for want of a demonstrated consumer, exactly as Value.CallSlice records.
        throw new NotImplementedException(
            "reflect.MakeFunc of the variadic func type '" + GoReflect.GoTypeName(st) +
            "' is not implemented (no demonstrated consumer; see GoReflect.MakeGoFuncDelegate)");
    }
    // A Go multi-return arrives back as the delegate's declared ValueTuple — captured from the
    // Invoke signature rather than re-derived from outs, so the packed tuple is the exact type the
    // delegate returns. ValueTuple nests beyond seven elements (TRest), where a flat constructor
    // lookup no longer holds; no Go func in the corpus returns eight values, so that shape fails
    // loud rather than packing a wrong tuple.
    System.Type returnType = st.GetMethod("Invoke")!.ReturnType;
    if (outs.Length > 7) {
        throw new NotImplementedException(
            "reflect.MakeFunc of a func type with " + outs.Length.ToString() +
            " results is not implemented (ValueTuple nests beyond seven; no demonstrated consumer)");
    }
    nint[]?[]? paramDims = Ꮡt.Value.funcParamDims;

    object? invoke(object?[] rawArgs) {
        var args = new slice<ΔValue>(ins.Length);
        for (int i = 0; i < ins.Length; i++) {
            // Each argument is typed by the func's STATIC parameter type (Go's contract: fn can
            // assume the types given by typ) — an interface-typed parameter is a Kind Interface
            // Value over the dynamic argument, a nil pointer a VALID typed-nil Value, and an array
            // parameter carries the descriptor's per-parameter dims cargo (the one route a
            // `[32]byte` parameter's length reaches reflect at all — see rtype.In).
            nint[]? dims = paramDims is not null && i < paramDims.Length ? paramDims[i] : null;
            args[i] = makeTypedValue(rawArgs[i], ins[i], dims, default);
        }
        slice<ΔValue> results = fn(args);
        if (len(results) != outs.Length) {
            throw panic("reflect: wrong return count from function created by MakeFunc");
        }
        if (outs.Length == 0) {
            return null;
        }
        object?[] marshalled = new object?[outs.Length];
        for (int i = 0; i < outs.Length; i++) {
            marshalled[i] = marshalMakeFuncResult(results[i], outs[i]);
        }
        return outs.Length == 1 ? marshalled[0] : System.Activator.CreateInstance(returnType, marshalled);
    }

    Delegate del = GoReflect.MakeGoFuncDelegate(st, invoke);
    // The returned Value rides typ's OWN descriptor box (not a fresh synthType), so the dims cargo
    // survives and Type() interns back to the caller's wrapper — `MakeFunc(t, fn).Type() == t`.
    var v = new ΔValue(Ꮡt, default!, ((flag)(uintptr)(nuint)Func));
    v.boxed = del;
    return v;
}

// One result Value, marshalled into the delegate's CLR result slot under the SAME assignability
// rule Value.Call's arguments use (marshalIntoSlot — an interface-typed result packs the way
// Interface() packs, a concrete one hands over the box it holds). The guards are Go's own, in Go's
// order: the zero Value and a read-only Value (obtained from an unexported field) are refused
// before assignability is asked at all.
private static object? marshalMakeFuncResult(ΔValue result, System.Type want) {
    if (result.flag == 0) {
        throw panic("reflect: function created by MakeFunc returned zero Value");
    }
    if ((flag)(result.flag & flagRO) != 0) {
        throw panic("reflect: function created by MakeFunc returned value obtained from unexported field");
    }
    if (!marshalIntoSlot(result, want, out object? marshalled)) {
        throw panic("reflect.MakeFunc: value of type " + GoReflect.GoTypeName(result.live?.GetType()) +
                    " is not assignable to type " + GoReflect.GoTypeName(want));
    }
    return marshalled;
}

} // end reflect_package
