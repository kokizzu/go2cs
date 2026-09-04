using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go.testing_runtime;

namespace GolibTests;

[TestClass]
public class TempDirNameMappingTests
{
    // WHY THIS EXISTS.
    //
    // Go's testing has TWO name mappers and they do different jobs. testing.rewrite maps a test
    // NAME: it escapes a non-printable rune and leaves ':', '*', '[' and ']' alone, because a test
    // name may legitimately carry them. (*common).TempDir maps a PATH, and its own comment says why
    // it is stricter -- "Drop unusual characters (such as path separators or characters interacting
    // with globs) from the directory name to avoid surprising os.MkdirTemp behavior."
    //
    // This host used the NAME mapper for the PATH, and Go's own TestTempDir found it (measured
    // 2026-09-04, four of its ten leaves): "test*" and "test:subtest" could not be created on
    // Windows at all, and "test[]" was created fine and then failed the test's own
    // filepath.Glob(dir + "/*.txt") with "syntax error in pattern" -- the surviving brackets read as
    // a character class in the DIRECTORY part of the pattern, which is the failure Go's comment is
    // literally describing.
    //
    // Both directions are asserted, because a mapper that drops everything would pass a
    // one-directional test: an illegal name must map to Go's spelling, AND a legal one must come
    // through untouched.

    [TestMethod]
    public void GoTempDirPatternDropsExactlyWhatGoDrops()
    {
        // The four leaves that failed, plus the two that passed for the same reason they pass in
        // Go: every expected value is Go's mapper applied by hand to the same input.
        (string Name, string Expected)[] cases =
        [
            ("TestTempDir/test:subtest", "TestTempDirtestsubtest"),
            ("TestTempDir/test*",        "TestTempDirtest"),
            ("TestTempDir/test[]",       "TestTempDirtest"),
            ("TestTempDir/test\\subtest","TestTempDirtestsubtest"),
            ("TestTempDir/test/subtest", "TestTempDirtestsubtest"),
            ("TestTempDir/../test",      "TestTempDir..test"),
        ];

        foreach ((string name, string expected) in cases)
            Assert.AreEqual(expected, TestExecution.GoTempDirPattern(name), $"mapping of {name}");
    }

    [TestMethod]
    public void GoTempDirPatternKeepsWhatGoKeeps()
    {
        // The OTHER direction. Go keeps ASCII alphanumerics, its `allowed` set (note the SPACE),
        // and any non-ASCII LETTER or NUMBER -- which is why TestTempDir's own "aouee" leaf with
        // diacritics passes in Go and must here.
        Assert.AreEqual("TestTempDirInSubtest", TestExecution.GoTempDirPattern("TestTempDir/InSubtest"));
        Assert.AreEqual("!#$%&()+,-.=@^_{}~ ", TestExecution.GoTempDirPattern("!#$%&()+,-.=@^_{}~ "),
            "Go's allowed set survives in full, the trailing space included");
        Assert.AreEqual("abcXYZ0189", TestExecution.GoTempDirPattern("abcXYZ0189"));
        Assert.AreEqual("äöüéè", TestExecution.GoTempDirPattern("äöüéè"),
            "non-ASCII letters survive (unicode.IsLetter)");
        Assert.AreEqual("一二", TestExecution.GoTempDirPattern("一二"),
            "non-ASCII letters outside Latin survive too");

        // A non-ASCII rune that is NEITHER a letter nor a number is dropped, which is the half of
        // the non-ASCII branch a letters-only reading would get wrong.
        Assert.AreEqual("", TestExecution.GoTempDirPattern("☃"), "a non-letter non-number symbol is dropped");
    }

    [TestMethod]
    public void TempDirNameIsPathSafeAndDisambiguatesALossyMapping()
    {
        // The mapper is lossy by design, so the host's hash is what stops two different subtest
        // names sharing one directory -- a hazard Go does not have, because os.MkdirTemp appends
        // randomness. Two names that map to the SAME pattern must still get different directories.
        string a = TestExecution.TempDirName("TestTempDir/test/..");
        string b = TestExecution.TempDirName("TestTempDir/test..");

        Assert.AreEqual(TestExecution.GoTempDirPattern("TestTempDir/test/.."),
                        TestExecution.GoTempDirPattern("TestTempDir/test.."),
                        "these two names must collide under the mapper, or this test proves nothing");
        Assert.AreNotEqual(a, b, "the hash must disambiguate a lossy mapping");

        // And the composed name must be usable as a single path component on this platform.
        char[] invalid = Path.GetInvalidFileNameChars();

        foreach (string name in new[] { a, b, TestExecution.TempDirName("TestTempDir/test*"), TestExecution.TempDirName("TestTempDir/test:subtest") })
        {
            Assert.IsFalse(name.Any(invalid.Contains), $"{name} must contain no character invalid in a file name");
            Assert.IsFalse(name.Contains('[') || name.Contains(']') || name.Contains('*') || name.Contains('?'),
                $"{name} must contain no glob metacharacter -- filepath.Glob reads the directory part of its pattern too");
        }
    }
}
