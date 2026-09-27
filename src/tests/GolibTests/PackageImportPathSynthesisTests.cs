using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using go.golib;

namespace GolibTests;

/// <summary>
/// A synthesized struct's package container (reflect.StructOf with an unexported field's PkgPath)
/// must report the VERBATIM import path. The container's C# name is the path flattened with '.', so a
/// '.' inside a segment (<c>example.com</c>, <c>golang.org</c>) read back as a separator:
/// <c>example.com/dotted/v2</c> came back <c>example/com/dotted/v2</c>. Every container is now stamped
/// with its path (GoPackageAttribute.ImportPath), which the decoder reads first.
/// </summary>
[TestClass]
public class PackageImportPathSynthesisTests
{
    private static GoSynthField Field(string name, Type type) => new(name, type, "", false, null, null, "");

    [TestMethod]
    public void ADottedModulePathSurvivesTheSynthesizedContainer()
    {
        Type t = GoStructSynthesis.SynthesizeStructType([Field("Shown", typeof(long)), Field("hidden", typeof(long))], "example.com/dotted/v2");

        Assert.AreEqual("example.com/dotted/v2", GoReflect.GoPackagePath(t));
    }

    [TestMethod]
    public void AVendoredStdlibPathSurvivesTheSynthesizedContainer()
    {
        Type t = GoStructSynthesis.SynthesizeStructType([Field("hidden", typeof(long))], "vendor/golang.org/x/net/idna");

        Assert.AreEqual("vendor/golang.org/x/net/idna", GoReflect.GoPackagePath(t));
    }

    [TestMethod]
    public void AnOrdinaryPathStillRoundTrips()
    {
        Type t = GoStructSynthesis.SynthesizeStructType([Field("hidden", typeof(int))], "encoding/gob");

        Assert.AreEqual("encoding/gob", GoReflect.GoPackagePath(t));
    }
}
