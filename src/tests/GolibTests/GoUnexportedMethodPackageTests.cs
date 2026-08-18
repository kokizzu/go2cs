using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

// Go's UNEXPORTED-method rule: an interface method whose name is unexported can only be satisfied
// by a method declared in the INTERFACE's own package — two same-named unexported methods from
// different packages are DIFFERENT methods. go/ast's `Expr { Pos(); End(); exprNode() }` is the
// canonical consumer (the marker-method pattern): a foreign type carrying its own private
// `exprNode` must NOT implement it, which is exactly what internal/reflectlite's TestImplements
// asserts with `*reflectlite_test.notAnExpr` — and what the name+signature structural probe
// answered TRUE for. A false positive in an implements relation is read by every caller as
// permission, the most dangerous shape the bridge produces.
//
// The fixtures are shaped like converted emission: a top-level `<pkg>_package` static class holds
// the interface / the receiver type, and the receiver's methods are extension methods declared IN
// its package class — which is what gives the probe the two package identities to compare
// (GoReflect.GoPackageClassPath over the method's declaring class).
[TestClass]
public class GoUnexportedMethodPackageTests
{
    // The foreign satisfier: same names, same signatures, DIFFERENT package for the unexported
    // marker method. Go: does not implement.
    [TestMethod]
    public void AForeignTypesOwnUnexportedMethodDoesNotSatisfyTheInterface()
    {
        Assert.IsFalse(typeof(ж<markerimpl_package.ForeignNode>).StructurallyImplements(typeof(markerdecl_package.markedExpr)),
            "an unexported interface method is satisfiable only from the interface's own package");
    }

    // The same-package satisfier: identical shape, declared beside the interface. Go: implements.
    [TestMethod]
    public void ASamePackageUnexportedMethodSatisfiesTheInterface()
    {
        Assert.IsTrue(typeof(ж<markerdecl_package.HomeNode>).StructurallyImplements(typeof(markerdecl_package.markedExpr)),
            "the interface's own package satisfies its unexported method");
    }

    // EXPORTED methods carry no package constraint — the foreign type still satisfies an
    // all-exported interface cross-package (ordinary Go duck typing).
    [TestMethod]
    public void ExportedMethodsKeepCrossPackageSatisfaction()
    {
        Assert.IsTrue(typeof(ж<markerimpl_package.ForeignNode>).StructurallyImplements(typeof(markerdecl_package.PlainNode)),
            "exported interface methods stay satisfiable from any package");
    }
}

// "package markerdecl" — declares the marker interface, an all-exported control interface, and a
// same-package satisfier.
public static class markerdecl_package
{
    public interface markedExpr
    {
        nint Pos();
        void exprNode();
    }

    public interface PlainNode
    {
        nint Pos();
    }

    public struct HomeNode;

    [GoRecv] public static nint Pos(this ref HomeNode n) => 1;

    public static nint Pos(this ж<HomeNode> n) => 1;

    [GoRecv] public static void exprNode(this ref HomeNode n) { }

    public static void exprNode(this ж<HomeNode> n) { }
}

// "package markerimpl" — a foreign type with the SAME method names and signatures, its own
// private exprNode.
public static class markerimpl_package
{
    public struct ForeignNode;

    [GoRecv] public static nint Pos(this ref ForeignNode n) => 2;

    public static nint Pos(this ж<ForeignNode> n) => 2;

    [GoRecv] public static void exprNode(this ref ForeignNode n) { }

    public static void exprNode(this ж<ForeignNode> n) { }
}
