// Copyright 2012 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// This file implements typechecking of conversions.

// package types -- go2cs converted at 2022 March 13 05:52:51 UTC
// import "go/types" ==> using types = go.go.types_package
// Original source: C:\Program Files\Go\src\go\types\conversions.go
namespace go.go;

using constant = go.constant_package;
using unicode = unicode_package;


// Conversion type-checks the conversion T(x).
// The result is in x.

public static partial class types_package {

private static void conversion(this ptr<Checker> _addr_check, ptr<operand> _addr_x, Type T) {
    ref Checker check = ref _addr_check.val;
    ref operand x = ref _addr_x.val;

    var constArg = x.mode == constant_;

    bool ok = default;
    ref @string reason = ref heap(out ptr<@string> _addr_reason);

    if (constArg && isConstType(T)) 
        // constant conversion
        {
            var t = asBasic(T);


            if (representableConst(x.val, check, t, _addr_x.val)) 
                ok = true;
            else if (isInteger(x.typ) && isString(t)) 
                var codepoint = unicode.ReplacementChar;
                {
                    var (i, ok) = constant.Uint64Val(x.val);

                    if (ok && i <= unicode.MaxRune) {
                        codepoint = rune(i);
                    }
                }
                x.val = constant.MakeString(string(codepoint));
                ok = true;

        }
    else if (x.convertibleTo(check, T, _addr_reason)) 
        // non-constant conversion
        x.mode = value;
        ok = true;
        if (!ok) {
        if (reason != "") {
            check.errorf(x, _InvalidConversion, "cannot convert %s to %s (%s)", x, T, reason);
        }
        else
 {
            check.errorf(x, _InvalidConversion, "cannot convert %s to %s", x, T);
        }
        x.mode = invalid;
        return ;
    }
    if (isUntyped(x.typ)) {
        var final = T; 
        // - For conversions to interfaces, use the argument's default type.
        // - For conversions of untyped constants to non-constant types, also
        //   use the default type (e.g., []byte("foo") should report string
        //   not []byte as type for the constant "foo").
        // - Keep untyped nil for untyped nil arguments.
        // - For integer to string conversions, keep the argument type.
        //   (See also the TODO below.)
        if (IsInterface(T) || constArg && !isConstType(T) || x.isNil()) {
            final = Default(x.typ); // default type of untyped nil is untyped nil
        }
        else if (isInteger(x.typ) && isString(T)) {
            final = x.typ;
        }
        check.updateExprType(x.expr, final, true);
    }
    x.typ = T;
}

// TODO(gri) convertibleTo checks if T(x) is valid. It assumes that the type
// of x is fully known, but that's not the case for say string(1<<s + 1.0):
// Here, the type of 1<<s + 1.0 will be UntypedFloat which will lead to the
// (correct!) refusal of the conversion. But the reported error is essentially
// "cannot convert untyped float value to string", yet the correct error (per
// the spec) is that we cannot shift a floating-point value: 1 in 1<<s should
// be converted to UntypedFloat because of the addition of 1.0. Fixing this
// is tricky because we'd have to run updateExprType on the argument first.
// (Issue #21982.)

// convertibleTo reports whether T(x) is valid.
// The check parameter may be nil if convertibleTo is invoked through an
// exported API call, i.e., when all methods have been type-checked.
private static bool convertibleTo(this ptr<operand> _addr_x, ptr<Checker> _addr_check, Type T, ptr<@string> _addr_reason) {
    ref operand x = ref _addr_x.val;
    ref Checker check = ref _addr_check.val;
    ref @string reason = ref _addr_reason.val;
 
    // "x is assignable to T"
    {
        var (ok, _) = x.assignableTo(check, T, null);

        if (ok) {
            return true;
        }
    } 

    // "x's type and T have identical underlying types if tags are ignored"
    var V = x.typ;
    var Vu = under(V);
    var Tu = under(T);
    if (check.identicalIgnoreTags(Vu, Tu)) {
        return true;
    }
    {
        var V__prev1 = V;

        ptr<Pointer> (V, ok) = V._<ptr<Pointer>>();

        if (ok) {
            {
                ptr<Pointer> (T, ok) = T._<ptr<Pointer>>();

                if (ok) {
                    if (check.identicalIgnoreTags(under(V.@base), under(T.@base))) {
                        return true;
                    }
                }

            }
        }
        V = V__prev1;

    } 

    // "x's type and T are both integer or floating point types"
    if (isIntegerOrFloat(V) && isIntegerOrFloat(T)) {
        return true;
    }
    if (isComplex(V) && isComplex(T)) {
        return true;
    }
    if ((isInteger(V) || isBytesOrRunes(Vu)) && isString(T)) {
        return true;
    }
    if (isString(V) && isBytesOrRunes(Tu)) {
        return true;
    }
    if ((isPointer(Vu) || isUintptr(Vu)) && isUnsafePointer(T)) {
        return true;
    }
    if (isUnsafePointer(V) && (isPointer(Tu) || isUintptr(Tu))) {
        return true;
    }
    {
        var s = asSlice(V);

        if (s != null) {
            {
                var p = asPointer(T);

                if (p != null) {
                    {
                        var a = asArray(p.Elem());

                        if (a != null) {
                            if (check.identical(s.Elem(), a.Elem())) {
                                if (check == null || check.allowVersion(check.pkg, 1, 17)) {
                                    return true;
                                }
                                if (reason != null) {
                                    reason = "conversion of slices to array pointers requires go1.17 or later";
                                }
                            }
                        }

                    }
                }

            }
        }
    }

    return false;
}

private static bool isUintptr(Type typ) {
    var t = asBasic(typ);
    return t != null && t.kind == Uintptr;
}

private static bool isUnsafePointer(Type typ) { 
    // TODO(gri): Is this asBasic(typ) instead of typ.(*Basic) correct?
    //            (The former calls under(), while the latter doesn't.)
    //            The spec does not say so, but gc claims it is. See also
    //            issue 6326.
    var t = asBasic(typ);
    return t != null && t.kind == UnsafePointer;
}

private static bool isPointer(Type typ) {
    return asPointer(typ) != null;
}

private static bool isBytesOrRunes(Type typ) {
    {
        var s = asSlice(typ);

        if (s != null) {
            var t = asBasic(s.elem);
            return t != null && (t.kind == Byte || t.kind == Rune);
        }
    }
    return false;
}

} // end types_package
