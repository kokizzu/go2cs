using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;

namespace GolibTests;

[TestClass]
public class GoTypeDefinednessTests
{
    // GoReflect.HasGoName is the managed stand-in for the type descriptor's TFlagNamed bit, which a
    // SYNTHESIZED abi.Type never carries -- it is the gate behind reflect.Type.Name(), which reports
    // a name for a DEFINED type and "" for every other one. It stood as `ElementType(t) is not null`
    // until 2026-08-11: a shape test that is true of a defined container exactly as it is of an
    // unnamed one, so `type testSET []int` reported no name and encoding/asn1's getUniversalType --
    // which picks SET over SEQUENCE on `strings.HasSuffix(t.Name(), "SET")` and nothing else --
    // emitted 0x30 SEQUENCE where Go writes 0x31 SET.
    //
    // WHAT IS GUARDED WHERE, and why this file is not the whole story. The DEFINED-container arm --
    // the regression itself -- belongs to the ReflectStructTagCopy behavioral test, which runs the
    // real converter-emitted `type S []int` / `[N]byte` / `map[K]V` / `chan T` / `*int` through the
    // real reflect API and compares against `go run`, each paired with its unnamed control. That is
    // strictly stronger than any hand-built stand-in could be, so no stand-in is built here.
    //
    // What a Go program cannot reach, and what this file therefore owns, is the rest of the
    // predicate: golib's own container generics, `interface {}`, `struct {}`, and the
    // [GoType("dyn")] anonymous-struct LIFT -- three arms whose answers this change also corrected
    // (each used to return its STRUCTURAL spelling from Name(), since there is no package dot to
    // trim, and Go can only ever answer "" for them).
    //
    // The invariant every case below serves: HasGoName must agree with GoTypeName arm for arm,
    // because Name() IS GoTypeName's output with the package qualifier trimmed. A type that renders
    // STRUCTURALLY has no name; anything GoTypeName package-qualifies does.

    // Stands in for a converted `<pkg>_package` static class -- what GoReflect's package
    // qualification looks for when it builds a Go type name.
    private static class nametest_package
    {
        // `type Point struct{ X, Y int }` -- a plain converted struct, the ordinary named case.
        [GoType]
        public struct Point
        {
            public nint X;
            public nint Y;
        }

        // `type Celsius float64` -- a DEFINED scalar. Never broken, and here as the control showing
        // the fix reaches the container case without special-casing scalars to get there.
        [GoType("num:float64")]
        public readonly struct Celsius;

        // An anonymous struct the converter LIFTED: rendered structurally by GoTypeName, so it has
        // no Go name. The [GoType("dyn")] stamp is what distinguishes a lift from a declared struct.
        [GoType("dyn")]
        public struct Anonymous;

        // A lift that ALSO carries [GoLocalName] is not anonymous at all -- it is a NAMED
        // function-local type (`type Person struct{...}` inside a func), which Go renders by name.
        [GoType("dyn")]
        [GoLocalName("Person")]
        public struct Lifted;

        // An anonymous INTERFACE the converter lifted -- `interface{ F() }` written inline. The
        // same [GoType("dyn")] stamp as the struct lift, one type category over: Go renders it
        // structurally and it has no name (internal/reflectlite's TestNames asserts "" exactly).
        [GoType("dyn")]
        public interface AnonymousIface
        {
            void F();
        }

        // `type A struct{}` and `type B[T any] struct{}` -- the generic-instantiation naming
        // shapes internal/reflectlite's TestNames asserts (B[A], B[B[A]]).
        [GoType]
        public struct A;

        [GoType]
        public struct B<T>;
    }

    // The ordinary named cases, which must keep answering as they always did.
    [TestMethod]
    public void DeclaredStructsScalarsAndPredeclaredTypesHaveNames()
    {
        Assert.IsTrue(GoReflect.HasGoName(typeof(nametest_package.Point)), "a declared struct is a named type");
        Assert.IsTrue(GoReflect.HasGoName(typeof(nametest_package.Celsius)), "a defined scalar is a named type");
        Assert.IsTrue(GoReflect.HasGoName(typeof(nint)), "Go's predeclared int IS a named type");
        Assert.IsTrue(GoReflect.HasGoName(typeof(@string)), "Go's predeclared string IS a named type");
        Assert.IsTrue(GoReflect.HasGoName(typeof(bool)), "Go's predeclared bool IS a named type");
        Assert.IsTrue(GoReflect.HasGoName(typeof(Complex)), "Go's predeclared complex128 IS a named type");
    }

    // The raw golib containers ARE Go's unnamed composites: []T, [N]T, map[K]V, chan T, *T. They are
    // matched by open generic DEFINITION, which is what separates them from a defined container --
    // the converter emits that as its own wrapper type that merely IMPLEMENTS the same interface,
    // so a test asking "does this have an element type?" cannot tell the two apart and this one can.
    [TestMethod]
    public void TheRawGolibContainersAreUnnamed()
    {
        Assert.IsFalse(GoReflect.HasGoName(typeof(slice<nint>)), "[]int is an unnamed composite");
        Assert.IsFalse(GoReflect.HasGoName(typeof(array<byte>)), "[N]uint8 is an unnamed composite");
        Assert.IsFalse(GoReflect.HasGoName(typeof(map<@string, nint>)), "map[string]int is an unnamed composite");
        Assert.IsFalse(GoReflect.HasGoName(typeof(channel<nint>)), "chan int is an unnamed composite");
        Assert.IsFalse(GoReflect.HasGoName(typeof(ж<nint>)), "*int is an unnamed composite");
    }

    // The structural arms GoTypeName renders as text rather than as a name. Each of these used to
    // come back from Name() AS that text -- there is no dot to trim, so the whole spelling escaped.
    [TestMethod]
    public void StructurallyRenderedTypesHaveNoName()
    {
        Assert.IsFalse(GoReflect.HasGoName(typeof(object)), "interface {} is an unnamed type");
        Assert.IsFalse(GoReflect.HasGoName(typeof(EmptyStruct)), "struct {} is an unnamed type");
        Assert.IsFalse(GoReflect.HasGoName(typeof(nametest_package.Anonymous)), "a lifted anonymous struct is unnamed");
    }

    // A lift carrying [GoLocalName] is a NAMED function-local type, which GoTypeName prefers the
    // stamp for -- so the structural arm must not swallow it. The name pairing is asserted with it,
    // since the two methods disagreeing is the failure mode this predicate exists to prevent.
    [TestMethod]
    public void ALiftedTypeWithALocalNameStampIsNamed()
    {
        Assert.IsTrue(GoReflect.HasGoName(typeof(nametest_package.Lifted)));
        Assert.AreEqual("nametest.Person", GoReflect.GoTypeName(typeof(nametest_package.Lifted)));
    }

    // A nil type is not a type at all; Name() answers "" for it rather than faulting.
    [TestMethod]
    public void ANullTypeIsNotNamed()
    {
        Assert.IsFalse(GoReflect.HasGoName(null));
    }

    // An anonymous-interface LIFT is the struct lift's rule one type category over: the
    // [GoType("dyn")] stamp marks it structural, so it has no Go name and renders as Go's
    // `interface { F() }` -- never as the lifted C# identifier. Before this arm the C# name
    // escaped both methods (internal/reflectlite's TestNames measured `typeᴛ30` where Go
    // answers "").
    [TestMethod]
    public void AnAnonymousLiftedInterfaceIsUnnamedAndRendersStructurally()
    {
        Assert.IsFalse(GoReflect.HasGoName(typeof(nametest_package.AnonymousIface)), "a lifted anonymous interface is unnamed");
        Assert.AreEqual("interface { F() }", GoReflect.GoTypeName(typeof(nametest_package.AnonymousIface)));
    }

    // A generic INSTANTIATION is a named Go type spelled `B[<args>]`, with each type ARGUMENT
    // qualified by its IMPORT PATH (Go: `B[internal/reflectlite_test.A]`; here the test package
    // is not under the `go.` emission root, so the path collapses to the bare package name).
    // Before this arm the CLR arity spelling escaped (`nametest.B`1`). Predeclared arguments
    // keep their Go spelling, and nesting recurses.
    [TestMethod]
    public void AGenericInstantiationRendersGoStyle()
    {
        Assert.IsTrue(GoReflect.HasGoName(typeof(nametest_package.B<nametest_package.A>)), "an instantiated generic is a named type");
        Assert.AreEqual("nametest.B[nametest.A]", GoReflect.GoTypeName(typeof(nametest_package.B<nametest_package.A>)));
        Assert.AreEqual("nametest.B[nametest.B[nametest.A]]", GoReflect.GoTypeName(typeof(nametest_package.B<nametest_package.B<nametest_package.A>>)));
        Assert.AreEqual("nametest.B[int]", GoReflect.GoTypeName(typeof(nametest_package.B<nint>)));
    }
}
