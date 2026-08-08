// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.debug;

using bytes = bytes_package;
using buildinfo = go.debug.buildinfo_package;
using pe = go.debug.pe_package;
using binary = encoding.binary_package;
using flag = flag_package;
using fmt = fmt_package;
using testenv = @internal.testenv_package;
using os = os_package;
using exec = go.os.exec_package;
using path = path_package;
using filepath = go.path.filepath_package;
using regexp = regexp_package;
using runtime = runtime_package;
using strings = strings_package;
using testing = testing_package;
using @internal;
using debug = go.runtime.debug_package;
using encoding;
using fs = io.fs_package;
using go.debug;
using go.os;
using go.path;
using go.runtime;
using io = io_package;

partial class buildinfo_test_package {

internal static ж<bool> flagAll = flag.Bool("all"u8, false, "test all supported GOOS/GOARCH platforms, instead of only the current platform"u8);

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object testRequiresCompilingAndˢ = (@string)"test requires compiling and linking, which may be slow"u8;
private static readonly @string goModˢ = "go.mod"u8;
private static readonly @string helloGoˢ = "hello.go"u8;
private static readonly @string buildˢ = "build"u8;
private static readonly @string srcExampleComMˢ = "src/example.com/m"u8;
private static readonly object goBuildinfNotFoundˢ = (@string)"Go buildinf not found"u8;
private static readonly @string mGoˢ = "(?m)^go\t.*\n"u8;
private static readonly @string mBuildˢ = "(?m)^build\t.*\n"u8;
private static readonly @string goGoversionˢ = "go\tGOVERSION\n"u8;
private static readonly @string buildCompilerˢ = "build\t-compiler="u8;
private static readonly @string doesnotexistTxtˢ = "doesnotexist.txt"u8;
private static readonly @string emptyˢ = "empty"u8;

[GoLocalName("platform")] [GoType("dyn")] partial struct TestReadFile_platform {
    internal @string goos, goarch;
}

[GoType("dyn")] partial struct TestReadFile_cases {
    internal @string name;
    internal Func<ж<testing.T>, @string, @string, @string, @string> build;
    internal @string want;
    internal @string wantErr;
}

// TestReadFile confirms that ReadFile can read build information from binaries
// on supported target platforms. It builds a trivial binary on the current
// platforms (or all platforms if -all is set) in various configurations and
// checks that build information can or cannot be read.
public static void TestReadFile(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    if (testing.Short()) {
        Ꮡt.Skip(testRequiresCompilingAndˢ);
    }
    testenv.MustHaveGoBuild(new testing_TжTB(Ꮡt));
    var platforms = new TestReadFile_platform[]{
        new("aix"u8, "ppc64"u8),
        new("darwin"u8, "amd64"u8),
        new("darwin"u8, "arm64"u8),
        new("linux"u8, "386"u8),
        new("linux"u8, "amd64"u8),
        new("windows"u8, "386"u8),
        new("windows"u8, "amd64"u8)
    }.slice();
    ref var runtimePlatform = ref heap<TestReadFile_platform>(out var ᏑruntimePlatform);
    runtimePlatform = new TestReadFile_platform(runtime.GOOS, runtime.GOARCH);
    var haveRuntimePlatform = false;
    foreach (var (_, p) in platforms) {
        if (p == runtimePlatform) {
            haveRuntimePlatform = true;
            break;
        }
    }
    if (!haveRuntimePlatform) {
        platforms = append(platforms, runtimePlatform);
    }
    var buildModes = new @string[]{"pie"u8, "exe"u8}.slice();
    if (testenv.HasCGO()) {
        buildModes = append(buildModes, "c-shared"u8);
    }
    // Keep in sync with src/cmd/go/internal/work/init.go:buildModeInit.
    @string badmode(@string goos, @string goarch, @string buildmode) => fmt.Sprintf("-buildmode=%s not supported on %s/%s"u8, buildmode, goos, goarch);
    var badmodeʗ1 = badmode;
    var buildWithModules = @string (ж<testing.T> tΔ1, @string goos, @string goarch, @string buildmode) => {
        @string dir = tΔ1.TempDir();
        @string gomodPath = filepath.Join(dir, goModˢ);
        var gomodData = slice<byte>("module example.com/m\ngo 1.18\n"u8);
        {
            var err = os.WriteFile(gomodPath, gomodData, 438); if (err != default!) {
                tΔ1.Fatal(err);
            }
        }
        @string helloPath = filepath.Join(dir, helloGoˢ);
        var helloData = slice<byte>("package main\nfunc main() {}\n"u8);
        {
            var err = os.WriteFile(helloPath, helloData, 438); if (err != default!) {
                tΔ1.Fatal(err);
            }
        }
        @string outPath = filepath.Join(dir, path.Base(tΔ1.Name()));
        var cmd = exec.Command(testenv.GoToolPath(new testing_TжTB(tΔ1)), buildˢ, "-o=" + outPath, "-buildmode=" + buildmode);
        cmd.Value.Dir = dir;
        cmd.Value.Env = append(os.Environ(), "GO111MODULE=on"u8, "GOOS=" + goos, "GOARCH=" + goarch);
        var stderr = Ꮡ(new strings.Builder(nil));
        cmd.Value.Stderr = new strings_BuilderжWriter(stderr);
        {
            var err = cmd.Run(); if (err != default!) {
                {
                    @string badmodeMsg = badmodeʗ1(goos, goarch, buildmode); if (strings.Contains(stderr.String(), badmodeMsg)) {
                        tΔ1.Skip(badmodeMsg);
                    }
                }
                tΔ1.Fatalf("failed building test file: %v\n%s"u8, err, stderr.String());
            }
        }
        return outPath;
    };
    var badmodeʗ2 = badmode;
    var buildWithGOPATH = @string (ж<testing.T> tΔ2, @string goos, @string goarch, @string buildmode) => {
        @string gopathDir = tΔ2.TempDir();
        @string pkgDir = filepath.Join(gopathDir, srcExampleComMˢ);
        {
            var err = os.MkdirAll(pkgDir, 511); if (err != default!) {
                tΔ2.Fatal(err);
            }
        }
        @string helloPath = filepath.Join(pkgDir, helloGoˢ);
        var helloData = slice<byte>("package main\nfunc main() {}\n"u8);
        {
            var err = os.WriteFile(helloPath, helloData, 438); if (err != default!) {
                tΔ2.Fatal(err);
            }
        }
        @string outPath = filepath.Join(gopathDir, path.Base(tΔ2.Name()));
        var cmd = exec.Command(testenv.GoToolPath(new testing_TжTB(tΔ2)), buildˢ, "-o=" + outPath, "-buildmode=" + buildmode);
        cmd.Value.Dir = pkgDir;
        cmd.Value.Env = append(os.Environ(), "GO111MODULE=off"u8, "GOPATH=" + gopathDir, "GOOS=" + goos, "GOARCH=" + goarch);
        var stderr = Ꮡ(new strings.Builder(nil));
        cmd.Value.Stderr = new strings_BuilderжWriter(stderr);
        {
            var err = cmd.Run(); if (err != default!) {
                {
                    @string badmodeMsg = badmodeʗ2(goos, goarch, buildmode); if (strings.Contains(stderr.String(), badmodeMsg)) {
                        tΔ2.Skip(badmodeMsg);
                    }
                }
                tΔ2.Fatalf("failed building test file: %v\n%s"u8, err, stderr.String());
            }
        }
        return outPath;
    };
    void damageBuildInfo(ж<testing.T> tΔ3, @string name) {
        var (data, err) = os.ReadFile(name);
        if (err != default!) {
            tΔ3.Fatal(err);
        }
        nint i = bytes.Index(data, slice<byte>(((@string)(new byte[]{0xff, 0x20, 0x47, 0x6f, 0x20, 0x62, 0x75, 0x69, 0x6c, 0x64, 0x69, 0x6e, 0x66, 0x3a}))));
        if (i < 0) {
            tΔ3.Fatal(goBuildinfNotFoundˢ);
        }
        data[i + 2] = (rune)'N';
        {
            var errΔ1 = os.WriteFile(name, data, 438); if (errΔ1 != default!) {
                tΔ3.Fatal(errΔ1);
            }
        }
    }
    var goVersionRe = regexp.MustCompile(mGoˢ);
    var buildRe = regexp.MustCompile(mBuildˢ);
    var buildReʗ1 = buildRe;
    var goVersionReʗ1 = goVersionRe;
    @string cleanOutputForComparison(@string got) {
        // Remove or replace anything that might depend on the test's environment
        // so we can check the output afterward with a string comparison.
        // We'll remove all build lines except the compiler, just to make sure
        // build lines are included.
        got = goVersionReʗ1.ReplaceAllString(got, goGoversionˢ);
        got = buildReʗ1.ReplaceAllStringFunc(got, (@string match) => {
            if (strings.HasPrefix(match, buildCompilerˢ)) {
                return match;
            }
            return ""u8;
        });
        return got;
    }


            var buildWithModulesʗ1 = buildWithModules;
            var damageBuildInfoʗ1 = damageBuildInfo;

            var buildWithGOPATHʗ1 = buildWithGOPATH;
            var damageBuildInfoʗ2 = damageBuildInfo;
    var cases = new TestReadFile_cases[]{
        new(
            name: "doesnotexist"u8,
            build: (ж<testing.T> tΔ4, @string goos, @string goarch, @string buildmode) => doesnotexistTxtˢ,
            wantErr: "doesnotexist"u8
        ),
        new(
            name: "empty"u8,
            build: (ж<testing.T> tΔ5, @string _Δp1, @string _Δp2, @string _Δp3) => {
                @string dir = tΔ5.TempDir();
                @string name = filepath.Join(dir, emptyˢ);
                {
                    var err = os.WriteFile(name, default!, 438); if (err != default!) {
                        tΔ5.Fatal(err);
                    }
                }
                return name;
            },
            wantErr: "unrecognized file format"u8
        ),
        new(
            name: "valid_modules"u8,
            build: buildWithModules,
            want: "go\tGOVERSION\n"u8 + "path\texample.com/m\n"u8 + "mod\texample.com/m\t(devel)\t\n"u8 + "build\t-compiler=gc\n"u8
        ),
        new(
            name: "invalid_modules"u8,
            build: (ж<testing.T> tΔ6, @string goos, @string goarch, @string buildmode) => {
                @string name = buildWithModulesʗ1(tΔ6, goos, goarch, buildmode);
                damageBuildInfoʗ1(tΔ6, name);
                return name;
            },
            wantErr: "not a Go executable"u8
        ),
        new(
            name: "valid_gopath"u8,
            build: buildWithGOPATH,
            want: "go\tGOVERSION\n"u8 + "path\texample.com/m\n"u8 + "build\t-compiler=gc\n"u8
        ),
        new(
            name: "invalid_gopath"u8,
            build: (ж<testing.T> tΔ7, @string goos, @string goarch, @string buildmode) => {
                @string name = buildWithGOPATHʗ1(tΔ7, goos, goarch, buildmode);
                damageBuildInfoʗ2(tΔ7, name);
                return name;
            },
            wantErr: "not a Go executable"u8
        )
    }.slice();
    foreach (var (_, p) in platforms) {
        ref var pΔ1 = ref heap<TestReadFile_platform>(out var ᏑpΔ1);
        pΔ1 = p;
        var buildModesʗ1 = buildModes;
        var casesʗ1 = cases;
        var cleanOutputForComparisonʗ1 = cleanOutputForComparison;
        var pʗ1 = pΔ1;
        var runtimePlatformʗ1 = runtimePlatform;
        Ꮡt.Run(pΔ1.goos + "_"u8 + pΔ1.goarch, (ж<testing.T> tΔ8) => {
            if (pʗ1 != runtimePlatformʗ1 && !flagAll.Value) {
                tΔ8.Skipf("skipping platforms other than %s_%s because -all was not set"u8, runtimePlatformʗ1.goos, runtimePlatformʗ1.goarch);
            }
            foreach (var (_, mode) in buildModesʗ1) {
                @string modeΔ1 = mode;
                var casesʗ2 = casesʗ1;
                var cleanOutputForComparisonʗ2 = cleanOutputForComparisonʗ1;
                var pʗ2 = pʗ1;
                tΔ8.Run(modeΔ1, (ж<testing.T> tΔ9) => {
                    foreach (var (_, tc) in casesʗ2) {
                        ref var tcΔ1 = ref heap<TestReadFile_cases>(out var ᏑtcΔ1);
                        tcΔ1 = tc;
                        var cleanOutputForComparisonʗ3 = cleanOutputForComparisonʗ2;
                        var pʗ3 = pʗ2;
                        var tcʗ1 = tcΔ1;
                        tΔ9.Run(tcΔ1.name, (ж<testing.T> tΔ10) => {
                            tΔ10.Parallel();
                            @string name = tcʗ1.build(tΔ10, pʗ3.goos, pʗ3.goarch, modeΔ1);
                            {
                                var (info, err) = buildinfo.ReadFile(name); if (err != default!){
                                    if (tcʗ1.wantErr == ""u8){
                                        tΔ10.Fatalf("unexpected error: %v"u8, err);
                                    } else 
                                    {
                                        @string errMsg = err.Error(); if (!strings.Contains(errMsg, tcʗ1.wantErr)) {
                                            tΔ10.Fatalf("got error %q; want error containing %q"u8, errMsg, tcʗ1.wantErr);
                                        }
                                    }
                                } else {
                                    if (tcʗ1.wantErr != ""u8) {
                                        tΔ10.Fatalf("unexpected success; want error containing %q"u8, tcʗ1.wantErr);
                                    }
                                    @string got = info.String();
                                    {
                                        @string clean = cleanOutputForComparisonʗ3(got); if (got != tcʗ1.want && clean != tcʗ1.want) {
                                            tΔ10.Fatalf("got:\n%s\nwant:\n%s"u8, got, tcʗ1.want);
                                        }
                                    }
                                }
                            }
                        });
                    }
                });
            }
        });
    }
}

// FuzzIssue57002 is a regression test for golang.org/issue/57002.
//
// The cause of issue 57002 is when pointerSize is not being checked,
// the read can panic with slice bounds out of range
public static void FuzzIssue57002(ж<testing.F> Ꮡf) {
    ref var f = ref Ꮡf.DerefOrNull();

    // input from issue
    f.Add(new byte[]{0x4d, 0x5a, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x50, 0x45, 0x0, 0x0, 0x0, 0x0, 0x5, 0x0, 0x20, 0x20, 0x20, 0x20, 0x0, 0x0, 0x0, 0x0, 0x20, 0x3f, 0x0, 0x20, 0x0, 0x0, 0x20, 0x20, 0x20, 0x20, 0x20, 0xff, 0x20, 0x20, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xb, 0x20, 0x20, 0x20, 0xfc, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x9, 0x0, 0x0, 0x0, 0x20, 0x0, 0x0, 0x0, 0x20, 0x20, 0x20, 0x20, 0x20, 0xef, 0x20, 0xff, 0xbf, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf, 0x0, 0x2, 0x0, 0x20, 0x0, 0x0, 0x9, 0x0, 0x4, 0x0, 0x20, 0xf6, 0x0, 0xd3, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x20, 0x1, 0x0, 0x0, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0xa, 0x20, 0xa, 0x20, 0x20, 0x20, 0xff, 0x20, 0x20, 0xff, 0x20, 0x47, 0x6f, 0x20, 0x62, 0x75, 0x69, 0x6c, 0x64, 0x69, 0x6e, 0x66, 0x3a, 0xde, 0xb5, 0xdf, 0xff, 0xff, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x6, 0x7f, 0x7f, 0x7f, 0x20, 0xf4, 0xb2, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x0, 0x0, 0xb, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x20, 0x20, 0x0, 0x0, 0x0, 0x0, 0x5, 0x0, 0x20, 0x20, 0x20, 0x20, 0x0, 0x0, 0x0, 0x0, 0x20, 0x3f, 0x27, 0x20, 0x0, 0xd, 0x0, 0xa, 0x20, 0x20, 0x20, 0x20, 0x20, 0xff, 0x20, 0x20, 0xff, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x0, 0x20, 0x20, 0x0, 0x0, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x5c, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20}.slice());
    Ꮡf.Fuzz((ж<testing.T> t, slice<byte> input) => {
        buildinfo.Read(new bytes_ReaderжReaderAt(bytes.NewReader(input)));
    });
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string notAGoExecutableˢ = "not a Go executable"u8;

// TestIssue54968 is a regression test for golang.org/issue/54968.
//
// The cause of issue 54968 is when the first buildInfoMagic is invalid, it
// enters an infinite loop.
public static void TestIssue54968(ж<testing.T> Ꮡt) {
    ref var t = ref Ꮡt.DerefOrNull();

    Ꮡt.Parallel();
    const nint paddingSize = 200;
    const nint buildInfoAlign = 16;
    var buildInfoMagic = slice<byte>(((@string)(new byte[]{0xff, 0x20, 0x47, 0x6f, 0x20, 0x62, 0x75, 0x69, 0x6c, 0x64, 0x69, 0x6e, 0x66, 0x3a})));
    // Construct a valid PE header.
    ref var buf = ref heap(new bytes.Buffer(), out var Ꮡbuf);
    buf.Write(new byte[]{(rune)'M', (rune)'Z'}.slice());
    buf.Write(bytes.Repeat(new byte[]{0}.slice(), 0x3c - 2));
    // At location 0x3c, the stub has the file offset to the PE signature.
    binary.Write(new bytes_BufferжWriter(Ꮡbuf), binary.LittleEndian, (int32)(0x3c + 4));
    buf.Write(new byte[]{(rune)'P', (rune)'E', 0, 0}.slice());
    binary.Write(new bytes_BufferжWriter(Ꮡbuf), binary.LittleEndian, new pe.FileHeader(NumberOfSections: 1));
    var sh = new pe.SectionHeader32(
        Name: new uint8[]{(rune)'t', 0}.array(8),
        SizeOfRawData: (uint32)(paddingSize + len(buildInfoMagic)),
        PointerToRawData: (uint32)buf.Len()
    );
    sh.PointerToRawData = (uint32)(buf.Len() + binary.Size(sh));
    binary.Write(new bytes_BufferжWriter(Ꮡbuf), binary.LittleEndian, sh);
    nint start = buf.Len();
    buf.Write(bytes.Repeat(new byte[]{0}.slice(), paddingSize + len(buildInfoMagic)));
    var data = buf.Bytes();
    {
        var (_, err) = pe.NewFile(new bytes_ReaderжReaderAt(bytes.NewReader(data))); if (err != default!) {
            Ꮡt.Fatalf("need a valid PE header for the misaligned buildInfoMagic test: %s"u8, err);
        }
    }
    // Place buildInfoMagic after the header.
    for (nint iᴛ1 = 1; iᴛ1 < paddingSize - len(buildInfoMagic); iᴛ1++) {
        var i = iᴛ1;
        // Test only misaligned buildInfoMagic.
        if (i % buildInfoAlign == 0) {
            continue;
        }
        var buildInfoMagicʗ1 = buildInfoMagic;
        var dataʗ1 = data;
        Ꮡt.Run(fmt.Sprintf("start_at_%d"u8, i), (ж<testing.T> tΔ1) => {
            var d = dataʗ1[..(int)(start)];
            // Construct intentionally-misaligned buildInfoMagic.
            d = append(d, bytes.Repeat(new byte[]{0}.slice(), i).ꓸꓸꓸ);
            d = append(d, buildInfoMagicʗ1.ꓸꓸꓸ);
            d = append(d, bytes.Repeat(new byte[]{0}.slice(), paddingSize - i).ꓸꓸꓸ);
            var (_, err) = buildinfo.Read(new bytes_ReaderжReaderAt(bytes.NewReader(d)));
            @string wantErr = notAGoExecutableˢ;
            if (err == default!){
                tΔ1.Errorf("got error nil; want error containing %q"u8, wantErr);
            } else 
            {
                @string errMsg = err.Error(); if (!strings.Contains(errMsg, wantErr)) {
                    tΔ1.Errorf("got error %q; want error containing %q"u8, errMsg, wantErr);
                }
            }
        });
    }
}

} // end buildinfo_test_package
