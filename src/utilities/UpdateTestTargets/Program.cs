// Program.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// Move up from the working directory this is run from -- "go2cs/src/utilities/UpdateTestTargets/
// bin/Debug/net9.0" -- to "go2cs/src". Built from Path.Combine SEGMENTS rather than an embedded
// @"..\..\..\..\..\": .NET does not normalize backslashes on Unix, so that literal is a single
// directory NAME there and every path below resolves to somewhere that does not exist. Identical
// bytes on Windows, correct on both (F4, docs/PLAN-linux-operation.md).
string rootPath = Path.Combine("..", "..", "..", "..", "..");

// Behavioral tree, relative to that root.
string behavioralRoot = Path.Combine(rootPath, "tests", "Behavioral");

// The generated blocks below are written with explicit CRLF, matching every other emitted artifact
// in this repository (and the eol=crlf pin in .gitattributes). File.WriteAllLines would otherwise
// use Environment.NewLine and emit LF-terminated *Tests.cs on a non-Windows host while the injected
// test-method lines kept their own "\r\n" -- a mixed file, from the one utility the documented
// add-a-test flow requires.
const string NewLine = "\r\n";

// Scan all behavioral test folders
string[] behavioralTestDirs = Directory.GetDirectories(behavioralRoot);

List<string> targetTests = [];

foreach (string testDir in behavioralTestDirs)
{
    if (testDir.EndsWith("Tests"))
        continue;

    // Only real behavioral test projects (those containing Go source) are test targets. Utility folders
    // that live under tests\Behavioral but have no .go files -- e.g. BehavioralRunner -- are not tests
    // and must not get phantom Check<Name>() methods injected.
    if (Directory.GetFiles(testDir, "*.go").Length == 0)
        continue;

    // Get last subdirectory name which is the test project name
    string[] dirParts = testDir.Split(Path.DirectorySeparatorChar);
    targetTests.Add(dirParts[^1]);
}

(string testClass, Func<string, bool>? filter)[] testClasses =
[
    ("TranspileTests", null),                       // Tests transpilation of Go code to C# code
    ("CompileTests", null),                         // Tests compilation of transpiled C# code
    ("TargetComparisonTests", null),                // Tests comparison of transpiled C# code to expected target
    ("OutputComparisonTests", MatchConsoleOutput)   // Tests comparison of console output to expected output
];

foreach ((string testClass, Func<string, bool>? filter) in testClasses)
{
    string testFile = Path.GetFullPath(Path.Combine(behavioralRoot, "BehavioralTests", $"{testClass}.cs"));
    string[] testFileLines = File.ReadAllLines(testFile);
    int startLineIndex = -1;
    int endLineIndex = -1;

    for (int i = 0; i < testFileLines.Length; i++)
    {
        if (testFileLines[i].Contains("// <TestMethods>"))
        {
            startLineIndex = i + 1;
            continue;
        }
        
        if (testFileLines[i].Contains("// </TestMethods>"))
        {
            endLineIndex = i;
            break;
        }
    }

    if (startLineIndex >= 0 && endLineIndex >= 0 && startLineIndex < endLineIndex)
    {
        // Add all lines up to the start of the test methods
        List<string> lines = [ ..testFileLines[..startLineIndex] ];

        // Set up a filter predicate to include only specific test targets
        Func<string, bool> includeTestTarget = filter ?? (_ => true);

        // Add new test methods for each target test
        lines.AddRange(targetTests.Where(includeTestTarget).Select(targetTest =>
            $"{NewLine}    [TestMethod]{NewLine}    public void Check{targetTest}() => CheckTarget(\"{targetTest}\");"));

        lines.Add("");

        // Add all lines after the end of the test methods
        lines.AddRange(testFileLines[endLineIndex..]);

        File.WriteAllText(testFile, string.Join(NewLine, lines) + NewLine);
    }
    else
    {
        throw new InvalidOperationException($"Could not find '<TestMethods>...</TestMethods>' section in \"{testFile}\"");
    }
}

if (args.Length > 0 && args[0] == "--createTargetFiles")
{
    // For each Go file converted to C#, create a target file for regression testing comparisons
    foreach (string targetTest in targetTests)
    {
        string projPath = Path.GetFullPath(Path.Combine(behavioralRoot, targetTest));

        // Iterate over each PRODUCTION .go file in project path. `_test.go` is excluded from a
        // production transpile by go/packages, so an in-package test file legitimately has no .cs and
        // needs no golden -- warning about it would be noise, not signal.
        foreach (string goSrcFile in Directory.GetFiles(projPath, "*.go"))
        {
            if (goSrcFile.EndsWith("_test.go", StringComparison.OrdinalIgnoreCase))
                continue;

            string transpiledFile = Path.Combine(projPath, $"{Path.GetFileNameWithoutExtension(goSrcFile)}.cs");
            string targetFile = $"{transpiledFile}.target";

            if (!File.Exists(transpiledFile))
                Console.Error.WriteLine($"WARNING: Transpiled file \"{transpiledFile}\" does not exist -- skipping target file creation...");
            else
                File.Copy(transpiledFile, targetFile, true);
        }
    }
}

return;

static bool MatchConsoleOutput(string targetTest)
{
    // Access "package_info.cs" file for the target test project. Recomputed from Path.Combine
    // segments rather than captured, because a local function cannot close over a top-level local.
    string packageInfoFile = Path.GetFullPath(
        Path.Combine("..", "..", "..", "..", "..", "tests", "Behavioral", targetTest, "package_info.cs"));

    if (!File.Exists(packageInfoFile))
        return false;

    string[] packageInfoLines = File.ReadAllLines(packageInfoFile);

    // Check for "GoTestMatchingConsoleOutput" attribute -- for now, just check for its presence
    // by looking for the attribute name in the file on its own line. Future implementations could
    // load assembly and verify attribute presence via reflection - this is a simpler approach.
    return packageInfoLines.Any(line => line.Trim().Equals("[GoTestMatchingConsoleOutput]"));
}
