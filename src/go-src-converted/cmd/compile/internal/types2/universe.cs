// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// This file sets up the universe scope and the unsafe package.

// package types2 -- go2cs converted at 2022 March 06 23:13:09 UTC
// import "cmd/compile/internal/types2" ==> using types2 = go.cmd.compile.@internal.types2_package
// Original source: C:\Program Files\Go\src\cmd\compile\internal\types2\universe.go
using constant = go.go.constant_package;
using strings = go.strings_package;

namespace go.cmd.compile.@internal;

public static partial class types2_package {

    // The Universe scope contains all predeclared objects of Go.
    // It is the outermost scope of any chain of nested scopes.
public static ptr<Scope> Universe;

// The Unsafe package is the package returned by an importer
// for the import path "unsafe".
public static ptr<Package> Unsafe;

private static ptr<Const> universeIota;private static ptr<Basic> universeByte;private static ptr<Basic> universeRune;private static ptr<Interface> universeAny;private static ptr<Named> universeError;

// Typ contains the predeclared *Basic types indexed by their
// corresponding BasicKind.
//
// The *Basic type for Typ[Byte] will have the name "uint8".
// Use Universe.Lookup("byte").Type() to obtain the specific
// alias basic type named "byte" (and analogous for "rune").
public static array<ptr<Basic>> Typ = new array<ptr<Basic>>(InitKeyedValues<ptr<Basic>>((Invalid, {Invalid,0,"invalid type"}), (Bool, {Bool,IsBoolean,"bool"}), (Int, {Int,IsInteger,"int"}), (Int8, {Int8,IsInteger,"int8"}), (Int16, {Int16,IsInteger,"int16"}), (Int32, {Int32,IsInteger,"int32"}), (Int64, {Int64,IsInteger,"int64"}), (Uint, {Uint,IsInteger|IsUnsigned,"uint"}), (Uint8, {Uint8,IsInteger|IsUnsigned,"uint8"}), (Uint16, {Uint16,IsInteger|IsUnsigned,"uint16"}), (Uint32, {Uint32,IsInteger|IsUnsigned,"uint32"}), (Uint64, {Uint64,IsInteger|IsUnsigned,"uint64"}), (Uintptr, {Uintptr,IsInteger|IsUnsigned,"uintptr"}), (Float32, {Float32,IsFloat,"float32"}), (Float64, {Float64,IsFloat,"float64"}), (Complex64, {Complex64,IsComplex,"complex64"}), (Complex128, {Complex128,IsComplex,"complex128"}), (String, {String,IsString,"string"}), (UnsafePointer, {UnsafePointer,0,"Pointer"}), (UntypedBool, {UntypedBool,IsBoolean|IsUntyped,"untyped bool"}), (UntypedInt, {UntypedInt,IsInteger|IsUntyped,"untyped int"}), (UntypedRune, {UntypedRune,IsInteger|IsUntyped,"untyped rune"}), (UntypedFloat, {UntypedFloat,IsFloat|IsUntyped,"untyped float"}), (UntypedComplex, {UntypedComplex,IsComplex|IsUntyped,"untyped complex"}), (UntypedString, {UntypedString,IsString|IsUntyped,"untyped string"}), (UntypedNil, {UntypedNil,IsUntyped,"untyped nil"})));

private static array<ptr<Basic>> aliases = new array<ptr<Basic>>(new ptr<Basic>[] { {Byte,IsInteger|IsUnsigned,"byte"}, {Rune,IsInteger,"rune"} });

private static void defPredeclaredTypes() {
    {
        var t__prev1 = t;

        foreach (var (_, __t) in Typ) {
            t = __t;
            def(NewTypeName(nopos, null, t.name, t));
        }
        t = t__prev1;
    }

    {
        var t__prev1 = t;

        foreach (var (_, __t) in aliases) {
            t = __t;
            def(NewTypeName(nopos, null, t.name, t));
        }
        t = t__prev1;
    }

    def(NewTypeName(nopos, null, "any", _addr_emptyInterface)); 

    // Error has a nil package in its qualified name since it is in no package
 {
        var res = NewVar(nopos, null, "", Typ[String]);
        ptr<Signature> sig = addr(new Signature(results:NewTuple(res)));
        var err = NewFunc(nopos, null, "Error", sig);
        ptr<Named> typ = addr(new Named(underlying:NewInterfaceType([]*Func{err},nil).Complete()));
        sig.recv = NewVar(nopos, null, "", typ);
        def(NewTypeName(nopos, null, "error", typ));
    }
}



private static void defPredeclaredConsts() {
    foreach (var (_, c) in predeclaredConsts) {
        def(NewConst(nopos, null, c.name, Typ[c.kind], c.val));
    }
}

private static void defPredeclaredNil() {
    def(addr(new Nil(object{name:"nil",typ:Typ[UntypedNil],color_:black})));
}

// A builtinId is the id of a builtin function.
private partial struct builtinId { // : nint
}

 
// universe scope
private static readonly builtinId _Append = iota;
private static readonly var _Cap = 0;
private static readonly var _Close = 1;
private static readonly var _Complex = 2;
private static readonly var _Copy = 3;
private static readonly var _Delete = 4;
private static readonly var _Imag = 5;
private static readonly var _Len = 6;
private static readonly var _Make = 7;
private static readonly var _New = 8;
private static readonly var _Panic = 9;
private static readonly var _Print = 10;
private static readonly var _Println = 11;
private static readonly var _Real = 12;
private static readonly var _Recover = 13; 

// package unsafe
private static readonly var _Add = 14;
private static readonly var _Alignof = 15;
private static readonly var _Offsetof = 16;
private static readonly var _Sizeof = 17;
private static readonly var _Slice = 18; 

// testing support
private static readonly var _Assert = 19;
private static readonly var _Trace = 20;




private static void defPredeclaredFuncs() {
    foreach (var (i) in predeclaredFuncs) {
        var id = builtinId(i);
        if (id == _Assert || id == _Trace) {
            continue; // only define these in testing environment
        }
        def(newBuiltin(id));

    }
}

// DefPredeclaredTestFuncs defines the assert and trace built-ins.
// These built-ins are intended for debugging and testing of this
// package only.
public static void DefPredeclaredTestFuncs() {
    if (Universe.Lookup("assert") != null) {
        return ; // already defined
    }
    def(newBuiltin(_Assert));
    def(newBuiltin(_Trace));

}

private static void defPredeclaredComparable() { 
    // The "comparable" interface can be imagined as defined like
    //
    // type comparable interface {
    //         == () untyped bool
    //         != () untyped bool
    // }
    //
    // == and != cannot be user-declared but we can declare
    // a magic method == and check for its presence when needed.

    // Define interface { == () }. We don't care about the signature
    // for == so leave it empty except for the receiver, which is
    // set up later to match the usual interface method assumptions.
    ptr<Signature> sig = @new<Signature>();
    var eql = NewFunc(nopos, null, "==", sig);
    var iface = NewInterfaceType(new slice<ptr<Func>>(new ptr<Func>[] { eql }), null).Complete(); 

    // set up the defined type for the interface
    var obj = NewTypeName(nopos, null, "comparable", null);
    var named = NewNamed(obj, iface, null);
    obj.color_ = black;
    sig.recv = NewVar(nopos, null, "", named); // complete == signature

    def(obj);

}

private static void init() {
    Universe = NewScope(null, nopos, nopos, "universe");
    Unsafe = NewPackage("unsafe", "unsafe");
    Unsafe.complete = true;

    defPredeclaredTypes();
    defPredeclaredConsts();
    defPredeclaredNil();
    defPredeclaredFuncs();
    defPredeclaredComparable();

    universeIota = Universe.Lookup("iota")._<ptr<Const>>();
    universeByte = Universe.Lookup("byte")._<ptr<TypeName>>().typ._<ptr<Basic>>();
    universeRune = Universe.Lookup("rune")._<ptr<TypeName>>().typ._<ptr<Basic>>();
    universeAny = Universe.Lookup("any")._<ptr<TypeName>>().typ._<ptr<Interface>>();
    universeError = Universe.Lookup("error")._<ptr<TypeName>>().typ._<ptr<Named>>(); 

    // "any" is only visible as constraint in a type parameter list
    delete(Universe.elems, "any");

}

// Objects with names containing blanks are internal and not entered into
// a scope. Objects with exported names are inserted in the unsafe package
// scope; other objects are inserted in the universe scope.
//
private static void def(Object obj) => func((_, panic, _) => {
    assert(obj.color() == black);
    var name = obj.Name();
    if (strings.Contains(name, " ")) {
        return ; // nothing to do
    }
    {
        var typ = asNamed(obj.Type());

        if (typ != null) {
            typ.obj = obj._<ptr<TypeName>>();
        }
    } 
    // exported identifiers go into package unsafe
    var scope = Universe;
    if (obj.Exported()) {
        scope = Unsafe.scope; 
        // set Pkg field
        switch (obj.type()) {
            case ptr<TypeName> obj:
                obj.pkg = Unsafe;
                break;
            case ptr<Builtin> obj:
                obj.pkg = Unsafe;
                break;
            default:
            {
                var obj = obj.type();
                unreachable();
                break;
            }
        }

    }
    if (scope.Insert(obj) != null) {
        panic("internal error: double declaration");
    }
});

} // end types2_package