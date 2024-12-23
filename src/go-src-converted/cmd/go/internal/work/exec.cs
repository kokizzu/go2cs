// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Action graph execution.

// package work -- go2cs converted at 2022 March 13 06:30:54 UTC
// import "cmd/go/internal/work" ==> using work = go.cmd.go.@internal.work_package
// Original source: C:\Program Files\Go\src\cmd\go\internal\work\exec.go
namespace go.cmd.go.@internal;

using bytes = bytes_package;
using context = context_package;
using json = encoding.json_package;
using errors = errors_package;
using fmt = fmt_package;
using buildcfg = @internal.buildcfg_package;
using exec = @internal.execabs_package;
using lazyregexp = @internal.lazyregexp_package;
using io = io_package;
using fs = io.fs_package;
using log = log_package;
using rand = math.rand_package;
using os = os_package;
using filepath = path.filepath_package;
using regexp = regexp_package;
using runtime = runtime_package;
using strconv = strconv_package;
using strings = strings_package;
using sync = sync_package;
using time = time_package;

using @base = cmd.go.@internal.@base_package;
using cache = cmd.go.@internal.cache_package;
using cfg = cmd.go.@internal.cfg_package;
using fsys = cmd.go.@internal.fsys_package;
using load = cmd.go.@internal.load_package;
using modload = cmd.go.@internal.modload_package;
using str = cmd.go.@internal.str_package;
using trace = cmd.go.@internal.trace_package;


// actionList returns the list of actions in the dag rooted at root
// as visited in a depth-first post-order traversal.

using System;
using System.Threading;
public static partial class work_package {

private static slice<ptr<Action>> actionList(ptr<Action> _addr_root) {
    ref Action root = ref _addr_root.val;

    map seen = /* TODO: Fix this in ScannerBase_Expression::ExitCompositeLit */ new map<ptr<Action>, bool>{};
    ptr<Action> all = new slice<ptr<Action>>(new ptr<Action>[] {  });
    Action<ptr<Action>> walk = default;
    walk = a => {
        if (seen[a]) {
            return ;
        }
        seen[a] = true;
        foreach (var (_, a1) in a.Deps) {
            walk(a1);
        }        all = append(all, a);
    };
    walk(root);
    return all;
}

// do runs the action graph rooted at root.
private static void Do(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_root) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;
    ref Action root = ref _addr_root.val;

    var (ctx, span) = trace.StartSpan(ctx, "exec.Builder.Do (" + root.Mode + " " + root.Target + ")");
    defer(span.Done());

    if (!b.IsCmdList) { 
        // If we're doing real work, take time at the end to trim the cache.
        var c = cache.Default();
        defer(c.Trim());
    }
    var all = actionList(_addr_root);
    {
        var i__prev1 = i;
        var a__prev1 = a;

        foreach (var (__i, __a) in all) {
            i = __i;
            a = __a;
            a.priority = i;
        }
        i = i__prev1;
        a = a__prev1;
    }

    Action writeActionGraph = () => {
        {
            var file = cfg.DebugActiongraph;

            if (file != "") {
                if (strings.HasSuffix(file, ".go")) { 
                    // Do not overwrite Go source code in:
                    //    go build -debug-actiongraph x.go
                    @base.Fatalf("go: refusing to write action graph to %v\n", file);
                }
                var js = actionGraphJSON(root);
                {
                    var err__prev2 = err;

                    var err = os.WriteFile(file, (slice<byte>)js, 0666);

                    if (err != null) {
                        fmt.Fprintf(os.Stderr, "go: writing action graph: %v\n", err);
                        @base.SetExitStatus(1);
                    }

                    err = err__prev2;

                }
            }

        }
    };
    writeActionGraph();

    b.readySema = make_channel<bool>(len(all)); 

    // Initialize per-action execution state.
    {
        var a__prev1 = a;

        foreach (var (_, __a) in all) {
            a = __a;
            foreach (var (_, a1) in a.Deps) {
                a1.triggers = append(a1.triggers, a);
            }
            a.pending = len(a.Deps);
            if (a.pending == 0) {
                b.ready.push(a);
                b.readySema.Send(true);
            }
        }
        a = a__prev1;
    }

    Action<context.Context, ptr<Action>> handle = (ctx, a) => {
        if (a.json != null) {
            a.json.TimeStart = time.Now();
        }
        err = default!;
        if (a.Func != null && (!a.Failed || a.IgnoreFail)) { 
            // TODO(matloob): Better action descriptions
            @string desc = "Executing action ";
            if (a.Package != null) {
                desc += "(" + a.Mode + " " + a.Package.Desc() + ")";
            }
            (ctx, span) = trace.StartSpan(ctx, desc);
            a.traceSpan = span;
            foreach (var (_, d) in a.Deps) {
                trace.Flow(ctx, d.traceSpan, a.traceSpan);
            }
            err = a.Func(b, ctx, a);
            span.Done();
        }
        if (a.json != null) {
            a.json.TimeDone = time.Now();
        }
        b.exec.Lock();
        defer(b.exec.Unlock());

        if (err != null) {
            if (err == errPrintedOutput) {
                @base.SetExitStatus(2);
            }
            else
 {
                @base.Errorf("%s", err);
            }
            a.Failed = true;
        }
        foreach (var (_, a0) in a.triggers) {
            if (a.Failed) {
                a0.Failed = true;
            }
            a0.pending--;

            if (a0.pending == 0) {
                b.ready.push(a0);
                b.readySema.Send(true);
            }
        }        if (a == root) {
            close(b.readySema);
        }
    };

    sync.WaitGroup wg = default; 

    // Kick off goroutines according to parallelism.
    // If we are using the -n flag (just printing commands)
    // drop the parallelism to 1, both to make the output
    // deterministic and because there is no real work anyway.
    var par = cfg.BuildP;
    if (cfg.BuildN) {
        par = 1;
    }
    {
        var i__prev1 = i;

        for (nint i = 0; i < par; i++) {
            wg.Add(1);
            go_(() => () => {
                var ctx = trace.StartGoroutine(ctx);
                defer(wg.Done());
                while (true) {
                    if (!ok) {
                        return ;
                    } 
                    // Receiving a value from b.readySema entitles
                    // us to take from the ready queue.
                    b.exec.Lock();
                    var a = b.ready.pop();
                    b.exec.Unlock();
                    handle(ctx, a);
                    @base.SetExitStatus(1);
                    return ;
                }
            }());
        }

        i = i__prev1;
    }

    wg.Wait(); 

    // Write action graph again, this time with timing information.
    writeActionGraph();
});

// buildActionID computes the action ID for a build action.
private static cache.ActionID buildActionID(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var p = a.Package;
    var h = cache.NewHash("build " + p.ImportPath); 

    // Configuration independent of compiler toolchain.
    // Note: buildmode has already been accounted for in buildGcflags
    // and should not be inserted explicitly. Most buildmodes use the
    // same compiler settings and can reuse each other's results.
    // If not, the reason is already recorded in buildGcflags.
    fmt.Fprintf(h, "compile\n"); 
    // Only include the package directory if it may affect the output.
    // We trim workspace paths for all packages when -trimpath is set.
    // The compiler hides the exact value of $GOROOT
    // when building things in GOROOT.
    // Assume b.WorkDir is being trimmed properly.
    // When -trimpath is used with a package built from the module cache,
    // use the module path and version instead of the directory.
    if (!p.Goroot && !cfg.BuildTrimpath && !strings.HasPrefix(p.Dir, b.WorkDir)) {
        fmt.Fprintf(h, "dir %s\n", p.Dir);
    }
    else if (cfg.BuildTrimpath && p.Module != null) {
        fmt.Fprintf(h, "module %s@%s\n", p.Module.Path, p.Module.Version);
    }
    if (p.Module != null) {
        fmt.Fprintf(h, "go %s\n", p.Module.GoVersion);
    }
    fmt.Fprintf(h, "goos %s goarch %s\n", cfg.Goos, cfg.Goarch);
    fmt.Fprintf(h, "import %q\n", p.ImportPath);
    fmt.Fprintf(h, "omitdebug %v standard %v local %v prefix %q\n", p.Internal.OmitDebug, p.Standard, p.Internal.Local, p.Internal.LocalPrefix);
    if (cfg.BuildTrimpath) {
        fmt.Fprintln(h, "trimpath");
    }
    if (p.Internal.ForceLibrary) {
        fmt.Fprintf(h, "forcelibrary\n");
    }
    if (len(p.CgoFiles) + len(p.SwigFiles) + len(p.SwigCXXFiles) > 0) {
        fmt.Fprintf(h, "cgo %q\n", b.toolID("cgo"));
        var (cppflags, cflags, cxxflags, fflags, ldflags, _) = b.CFlags(p);

        var ccExe = b.ccExe();
        fmt.Fprintf(h, "CC=%q %q %q %q\n", ccExe, cppflags, cflags, ldflags); 
        // Include the C compiler tool ID so that if the C
        // compiler changes we rebuild the package.
        // But don't do that for standard library packages like net,
        // so that the prebuilt .a files from a Go binary install
        // don't need to be rebuilt with the local compiler.
        if (!p.Standard) {
            {
                var (ccID, err) = b.gccToolID(ccExe[0], "c");

                if (err == null) {
                    fmt.Fprintf(h, "CC ID=%q\n", ccID);
                }

            }
        }
        if (len(p.CXXFiles) + len(p.SwigCXXFiles) > 0) {
            var cxxExe = b.cxxExe();
            fmt.Fprintf(h, "CXX=%q %q\n", cxxExe, cxxflags);
            {
                var (cxxID, err) = b.gccToolID(cxxExe[0], "c++");

                if (err == null) {
                    fmt.Fprintf(h, "CXX ID=%q\n", cxxID);
                }

            }
        }
        if (len(p.FFiles) > 0) {
            var fcExe = b.fcExe();
            fmt.Fprintf(h, "FC=%q %q\n", fcExe, fflags);
            {
                var (fcID, err) = b.gccToolID(fcExe[0], "f95");

                if (err == null) {
                    fmt.Fprintf(h, "FC ID=%q\n", fcID);
                }

            }
        }
    }
    if (p.Internal.CoverMode != "") {
        fmt.Fprintf(h, "cover %q %q\n", p.Internal.CoverMode, b.toolID("cover"));
    }
    fmt.Fprintf(h, "modinfo %q\n", p.Internal.BuildInfo); 

    // Configuration specific to compiler toolchain.
    switch (cfg.BuildToolchainName) {
        case "gc": 
            fmt.Fprintf(h, "compile %s %q %q\n", b.toolID("compile"), forcedGcflags, p.Internal.Gcflags);
            if (len(p.SFiles) > 0) {
                fmt.Fprintf(h, "asm %q %q %q\n", b.toolID("asm"), forcedAsmflags, p.Internal.Asmflags);
            }
            var (key, val) = cfg.GetArchEnv();
            fmt.Fprintf(h, "%s=%s\n", key, val);

            {
                var goexperiment = buildcfg.GOEXPERIMENT();

                if (goexperiment != "") {
                    fmt.Fprintf(h, "GOEXPERIMENT=%q\n", goexperiment);
                } 

                // TODO(rsc): Convince compiler team not to add more magic environment variables,
                // or perhaps restrict the environment variables passed to subprocesses.
                // Because these are clumsy, undocumented special-case hacks
                // for debugging the compiler, they are not settable using 'go env -w',
                // and so here we use os.Getenv, not cfg.Getenv.

            } 

            // TODO(rsc): Convince compiler team not to add more magic environment variables,
            // or perhaps restrict the environment variables passed to subprocesses.
            // Because these are clumsy, undocumented special-case hacks
            // for debugging the compiler, they are not settable using 'go env -w',
            // and so here we use os.Getenv, not cfg.Getenv.
            @string magic = new slice<@string>(new @string[] { "GOCLOBBERDEADHASH", "GOSSAFUNC", "GOSSADIR", "GOSSAHASH" });
            {
                var env__prev1 = env;

                foreach (var (_, __env) in magic) {
                    env = __env;
                    {
                        var x__prev1 = x;

                        var x = os.Getenv(env);

                        if (x != "") {
                            fmt.Fprintf(h, "magic %s=%s\n", env, x);
                        }

                        x = x__prev1;

                    }
                }

                env = env__prev1;
            }

            if (os.Getenv("GOSSAHASH") != "") {
                for (nint i = 0; ; i++) {
                    var env = fmt.Sprintf("GOSSAHASH%d", i);
                    x = os.Getenv(env);
                    if (x == "") {
                        break;
                    }
                    fmt.Fprintf(h, "magic %s=%s\n", env, x);
                }
            }
            if (os.Getenv("GSHS_LOGFILE") != "") { 
                // Clumsy hack. Compiler writes to this log file,
                // so do not allow use of cache at all.
                // We will still write to the cache but it will be
                // essentially unfindable.
                fmt.Fprintf(h, "nocache %d\n", time.Now().UnixNano());
            }
            break;
        case "gccgo": 
            var (id, err) = b.gccToolID(BuildToolchain.compiler(), "go");
            if (err != null) {
                @base.Fatalf("%v", err);
            }
            fmt.Fprintf(h, "compile %s %q %q\n", id, forcedGccgoflags, p.Internal.Gccgoflags);
            fmt.Fprintf(h, "pkgpath %s\n", gccgoPkgpath(p));
            fmt.Fprintf(h, "ar %q\n", BuildToolchain._<gccgoToolchain>().ar());
            if (len(p.SFiles) > 0) {
                id, _ = b.gccToolID(BuildToolchain.compiler(), "assembler-with-cpp"); 
                // Ignore error; different assembler versions
                // are unlikely to make any difference anyhow.
                fmt.Fprintf(h, "asm %q\n", id);
            }
            break;
        default: 
            @base.Fatalf("buildActionID: unknown build toolchain %q", cfg.BuildToolchainName);
            break;
    } 

    // Input files.
    var inputFiles = str.StringList(p.GoFiles, p.CgoFiles, p.CFiles, p.CXXFiles, p.FFiles, p.MFiles, p.HFiles, p.SFiles, p.SysoFiles, p.SwigFiles, p.SwigCXXFiles, p.EmbedFiles);
    foreach (var (_, file) in inputFiles) {
        fmt.Fprintf(h, "file %s %s\n", file, b.fileHash(filepath.Join(p.Dir, file)));
    }    foreach (var (_, a1) in a.Deps) {
        var p1 = a1.Package;
        if (p1 != null) {
            fmt.Fprintf(h, "import %s %s\n", p1.ImportPath, contentID(a1.buildID));
        }
    }    return h.Sum();
}

// needCgoHdr reports whether the actions triggered by this one
// expect to be able to access the cgo-generated header file.
private static bool needCgoHdr(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
 
    // If this build triggers a header install, run cgo to get the header.
    if (!b.IsCmdList && (a.Package.UsesCgo() || a.Package.UsesSwig()) && (cfg.BuildBuildmode == "c-archive" || cfg.BuildBuildmode == "c-shared")) {
        {
            var t1__prev1 = t1;

            foreach (var (_, __t1) in a.triggers) {
                t1 = __t1;
                if (t1.Mode == "install header") {
                    return true;
                }
            }

            t1 = t1__prev1;
        }

        {
            var t1__prev1 = t1;

            foreach (var (_, __t1) in a.triggers) {
                t1 = __t1;
                foreach (var (_, t2) in t1.triggers) {
                    if (t2.Mode == "install header") {
                        return true;
                    }
                }
            }

            t1 = t1__prev1;
        }
    }
    return false;
}

// allowedVersion reports whether the version v is an allowed version of go
// (one that we can compile).
// v is known to be of the form "1.23".
private static bool allowedVersion(@string v) { 
    // Special case: no requirement.
    if (v == "") {
        return true;
    }
    if (v == "1.0") {
        return true;
    }
    foreach (var (_, tag) in cfg.BuildContext.ReleaseTags) {
        if (strings.HasPrefix(tag, "go") && tag[(int)2..] == v) {
            return true;
        }
    }    return false;
}

private static readonly uint needBuild = 1 << (int)(iota);
private static readonly var needCgoHdr = 0;
private static readonly var needVet = 1;
private static readonly var needCompiledGoFiles = 2;
private static readonly var needStale = 3;

// build is the action for building a single package.
// Note that any new influence on this logic must be reported in b.buildActionID above as well.
private static error build(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) => func((defer, _, _) => {
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var p = a.Package;

    Func<uint, bool, uint> bit = (x, b) => {
        if (b) {
            return error.As(x)!;
        }
        return error.As(0)!;
    };

    var cachedBuild = false;
    var need = bit(needBuild, !b.IsCmdList && a.needBuild || b.NeedExport) | bit(needCgoHdr, b.needCgoHdr(a)) | bit(needVet, a.needVet) | bit(needCompiledGoFiles, b.NeedCompiledGoFiles);

    if (!p.BinaryOnly) {
        if (b.useCache(a, b.buildActionID(a), p.Target)) { 
            // We found the main output in the cache.
            // If we don't need any other outputs, we can stop.
            // Otherwise, we need to write files to a.Objdir (needVet, needCgoHdr).
            // Remember that we might have them in cache
            // and check again after we create a.Objdir.
            cachedBuild = true;
            a.output = new slice<byte>(new byte[] {  }); // start saving output in case we miss any cache results
            need &= needBuild;
            if (b.NeedExport) {
                p.Export = a.built;
                p.BuildID = a.buildID;
            }
            if (need & needCompiledGoFiles != 0) {
                {
                    var err__prev4 = err;

                    var err = b.loadCachedSrcFiles(a);

                    if (err == null) {
                        need &= needCompiledGoFiles;
                    }

                    err = err__prev4;

                }
            }
        }
        if (!cachedBuild && need & needCompiledGoFiles != 0) {
            {
                var err__prev3 = err;

                err = b.loadCachedSrcFiles(a);

                if (err == null) {
                    need &= needCompiledGoFiles;
                }

                err = err__prev3;

            }
        }
        if (need == 0) {
            return error.As(null!)!;
        }
        defer(b.flushOutput(a));
    }
    defer(() => {
        if (err != null && err != errPrintedOutput) {
            err = fmt.Errorf("go build %s: %v", a.Package.ImportPath, err);
        }
        if (err != null && b.IsCmdList && b.NeedError && p.Error == null) {
            p.Error = addr(new load.PackageError(Err:err));
        }
    }());
    if (cfg.BuildN) { 
        // In -n mode, print a banner between packages.
        // The banner is five lines so that when changes to
        // different sections of the bootstrap script have to
        // be merged, the banners give patch something
        // to use to find its context.
        b.Print("\n#\n# " + a.Package.ImportPath + "\n#\n\n");
    }
    if (cfg.BuildV) {
        b.Print(a.Package.ImportPath + "\n");
    }
    if (a.Package.BinaryOnly) {
        p.Stale = true;
        p.StaleReason = "binary-only packages are no longer supported";
        if (b.IsCmdList) {
            return error.As(null!)!;
        }
        return error.As(errors.New("binary-only packages are no longer supported"))!;
    }
    {
        var err__prev1 = err;

        err = b.Mkdir(a.Objdir);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }
    var objdir = a.Objdir; 

    // Load cached cgo header, but only if we're skipping the main build (cachedBuild==true).
    if (cachedBuild && need & needCgoHdr != 0) {
        {
            var err__prev2 = err;

            err = b.loadCachedCgoHdr(a);

            if (err == null) {
                need &= needCgoHdr;
            }

            err = err__prev2;

        }
    }
    if (need == needVet) {
        {
            var err__prev2 = err;

            err = b.loadCachedVet(a);

            if (err == null) {
                need &= needVet;
            }

            err = err__prev2;

        }
    }
    if (need == 0) {
        return error.As(null!)!;
    }
    {
        var err__prev1 = err;

        err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // make target directory
    var (dir, _) = filepath.Split(a.Target);
    if (dir != "") {
        {
            var err__prev2 = err;

            err = b.Mkdir(dir);

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
    }
    var gofiles = str.StringList(a.Package.GoFiles);
    var cgofiles = str.StringList(a.Package.CgoFiles);
    var cfiles = str.StringList(a.Package.CFiles);
    var sfiles = str.StringList(a.Package.SFiles);
    var cxxfiles = str.StringList(a.Package.CXXFiles);
    slice<@string> objects = default;    slice<@string> cgoObjects = default;    slice<@string> pcCFLAGS = default;    slice<@string> pcLDFLAGS = default;



    if (a.Package.UsesCgo() || a.Package.UsesSwig()) {
        pcCFLAGS, pcLDFLAGS, err = b.getPkgConfigFlags(a.Package);

        if (err != null) {
            return ;
        }
    }
    slice<@string> nonGoFileLists = new slice<slice<@string>>(new slice<@string>[] { a.Package.CFiles, a.Package.SFiles, a.Package.CXXFiles, a.Package.HFiles, a.Package.FFiles });
OverlayLoop:
    {
        var fs__prev1 = fs;

        foreach (var (_, __fs) in nonGoFileLists) {
            fs = __fs;
            {
                var f__prev2 = f;

                foreach (var (_, __f) in fs) {
                    f = __f;
                    {
                        var (_, ok) = fsys.OverlayPath(mkAbs(p.Dir, f));

                        if (ok) {
                            a.nonGoOverlay = make_map<@string, @string>();
                            _breakOverlayLoop = true;
                            break;
                        }

                    }
                }

                f = f__prev2;
            }
        }
        fs = fs__prev1;
    }
    if (a.nonGoOverlay != null) {
        {
            var fs__prev1 = fs;

            foreach (var (_, __fs) in nonGoFileLists) {
                fs = __fs;
                {
                    var i__prev2 = i;

                    foreach (var (__i) in fs) {
                        i = __i;
                        var from = mkAbs(p.Dir, fs[i]);
                        var (opath, _) = fsys.OverlayPath(from);
                        var dst = objdir + filepath.Base(fs[i]);
                        {
                            var err__prev2 = err;

                            err = b.copyFile(dst, opath, 0666, false);

                            if (err != null) {
                                return error.As(err)!;
                            }

                            err = err__prev2;

                        }
                        a.nonGoOverlay[from] = dst;
                    }

                    i = i__prev2;
                }
            }

            fs = fs__prev1;
        }
    }
    if (a.Package.UsesSwig()) {
        var (outGo, outC, outCXX, err) = b.swig(a, a.Package, objdir, pcCFLAGS);
        if (err != null) {
            return error.As(err)!;
        }
        cgofiles = append(cgofiles, outGo);
        cfiles = append(cfiles, outC);
        cxxfiles = append(cxxfiles, outCXX);
    }
    if (a.Package.Internal.CoverMode != "") {
        {
            var i__prev1 = i;
            var file__prev1 = file;

            foreach (var (__i, __file) in str.StringList(gofiles, cgofiles)) {
                i = __i;
                file = __file;
                @string sourceFile = default;
                @string coverFile = default;
                @string key = default;
                if (strings.HasSuffix(file, ".cgo1.go")) { 
                    // cgo files have absolute paths
                    var @base = filepath.Base(file);
                    sourceFile = file;
                    coverFile = objdir + base;
                    key = strings.TrimSuffix(base, ".cgo1.go") + ".go";
                }
                else
 {
                    sourceFile = filepath.Join(a.Package.Dir, file);
                    coverFile = objdir + file;
                    key = file;
                }
                coverFile = strings.TrimSuffix(coverFile, ".go") + ".cover.go";
                var cover = a.Package.Internal.CoverVars[key];
                if (cover == null || @base.IsTestFile(file)) { 
                    // Not covering this file.
                    continue;
                }
                {
                    var err__prev2 = err;

                    err = b.cover(a, coverFile, sourceFile, cover.Var);

                    if (err != null) {
                        return error.As(err)!;
                    }

                    err = err__prev2;

                }
                if (i < len(gofiles)) {
                    gofiles[i] = coverFile;
                }
                else
 {
                    cgofiles[i - len(gofiles)] = coverFile;
                }
            }

            i = i__prev1;
            file = file__prev1;
        }
    }
    if (a.Package.UsesCgo() || a.Package.UsesSwig()) { 
        // In a package using cgo, cgo compiles the C, C++ and assembly files with gcc.
        // There is one exception: runtime/cgo's job is to bridge the
        // cgo and non-cgo worlds, so it necessarily has files in both.
        // In that case gcc only gets the gcc_* files.
        slice<@string> gccfiles = default;
        gccfiles = append(gccfiles, cfiles);
        cfiles = null;
        if (a.Package.Standard && a.Package.ImportPath == "runtime/cgo") {
            Func<slice<@string>, slice<@string>, slice<@string>, (slice<@string>, slice<@string>)> filter = (files, nongcc, gcc) => {
                {
                    var f__prev1 = f;

                    foreach (var (_, __f) in files) {
                        f = __f;
                        if (strings.HasPrefix(f, "gcc_")) {
                            gcc = append(gcc, f);
                        }
                        else
 {
                            nongcc = append(nongcc, f);
                        }
                    }
        else

                    f = f__prev1;
                }

                return (error.As(nongcc)!, gcc);
            }
;
            sfiles, gccfiles = filter(sfiles, sfiles[..(int)0], gccfiles);
        } {
            foreach (var (_, sfile) in sfiles) {
                var (data, err) = os.ReadFile(filepath.Join(a.Package.Dir, sfile));
                if (err == null) {
                    if (bytes.HasPrefix(data, (slice<byte>)"TEXT") || bytes.Contains(data, (slice<byte>)"\nTEXT") || bytes.HasPrefix(data, (slice<byte>)"DATA") || bytes.Contains(data, (slice<byte>)"\nDATA") || bytes.HasPrefix(data, (slice<byte>)"GLOBL") || bytes.Contains(data, (slice<byte>)"\nGLOBL")) {
                        return error.As(fmt.Errorf("package using cgo has Go assembly file %s", sfile))!;
                    }
                }
            }
            gccfiles = append(gccfiles, sfiles);
            sfiles = null;
        }
        var (outGo, outObj, err) = b.cgo(a, @base.Tool("cgo"), objdir, pcCFLAGS, pcLDFLAGS, mkAbsFiles(a.Package.Dir, cgofiles), gccfiles, cxxfiles, a.Package.MFiles, a.Package.FFiles); 

        // The files in cxxfiles have now been handled by b.cgo.
        cxxfiles = null;

        if (err != null) {
            return error.As(err)!;
        }
        if (cfg.BuildToolchainName == "gccgo") {
            cgoObjects = append(cgoObjects, a.Objdir + "_cgo_flags");
        }
        cgoObjects = append(cgoObjects, outObj);
        gofiles = append(gofiles, outGo);

        switch (cfg.BuildBuildmode) {
            case "c-archive": 

            case "c-shared": 
                b.cacheCgoHdr(a);
                break;
        }
    }
    slice<@string> srcfiles = default; // .go and non-.go
    srcfiles = append(srcfiles, gofiles);
    srcfiles = append(srcfiles, sfiles);
    srcfiles = append(srcfiles, cfiles);
    srcfiles = append(srcfiles, cxxfiles);
    b.cacheSrcFiles(a, srcfiles); 

    // Running cgo generated the cgo header.
    need &= needCgoHdr; 

    // Sanity check only, since Package.load already checked as well.
    if (len(gofiles) == 0) {
        return error.As(addr(new load.NoGoError(Package:a.Package))!)!;
    }
    if (need & needVet != 0) {
        buildVetConfig(_addr_a, srcfiles);
        need &= needVet;
    }
    if (need & needCompiledGoFiles != 0) {
        {
            var err__prev2 = err;

            err = b.loadCachedSrcFiles(a);

            if (err != null) {
                return error.As(fmt.Errorf("loading compiled Go files from cache: %w", err))!;
            }

            err = err__prev2;

        }
        need &= needCompiledGoFiles;
    }
    if (need == 0) { 
        // Nothing left to do.
        return error.As(null!)!;
    }
    var (symabis, err) = BuildToolchain.symabis(b, a, sfiles);
    if (err != null) {
        return error.As(err)!;
    }
    ref bytes.Buffer icfg = ref heap(out ptr<bytes.Buffer> _addr_icfg);
    fmt.Fprintf(_addr_icfg, "# import config\n");
    {
        var i__prev1 = i;

        foreach (var (__i, __raw) in a.Package.Internal.RawImports) {
            i = __i;
            raw = __raw;
            var final = a.Package.Imports[i];
            if (final != raw) {
                fmt.Fprintf(_addr_icfg, "importmap %s=%s\n", raw, final);
            }
        }
        i = i__prev1;
    }

    foreach (var (_, a1) in a.Deps) {
        var p1 = a1.Package;
        if (p1 == null || p1.ImportPath == "" || a1.built == "") {
            continue;
        }
        fmt.Fprintf(_addr_icfg, "packagefile %s=%s\n", p1.ImportPath, a1.built);
    }    slice<byte> embedcfg = default;
    if (len(p.Internal.Embed) > 0) {
        ref var embed = ref heap(out ptr<var> _addr_embed);
        embed.Patterns = p.Internal.Embed;
        embed.Files = make_map<@string, @string>();
        {
            var file__prev1 = file;

            foreach (var (_, __file) in p.EmbedFiles) {
                file = __file;
                embed.Files[file] = filepath.Join(p.Dir, file);
            }

            file = file__prev1;
        }

        var (js, err) = json.MarshalIndent(_addr_embed, "", "\t");
        if (err != null) {
            return error.As(fmt.Errorf("marshal embedcfg: %v", err))!;
        }
        embedcfg = js;
    }
    if (p.Internal.BuildInfo != "" && cfg.ModulesEnabled) {
        {
            var err__prev2 = err;

            err = b.writeFile(objdir + "_gomod_.go", modload.ModInfoProg(p.Internal.BuildInfo, cfg.BuildToolchainName == "gccgo"));

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
        gofiles = append(gofiles, objdir + "_gomod_.go");
    }
    var objpkg = objdir + "_pkg_.a";
    var (ofile, out, err) = BuildToolchain.gc(b, a, objpkg, icfg.Bytes(), embedcfg, symabis, len(sfiles) > 0, gofiles);
    if (len(out) > 0) {
        var output = b.processOutput(out);
        if (p.Module != null && !allowedVersion(p.Module.GoVersion)) {
            output += "note: module requires Go " + p.Module.GoVersion + "\n";
        }
        b.showOutput(a, a.Package.Dir, a.Package.Desc(), output);
        if (err != null) {
            return error.As(errPrintedOutput)!;
        }
    }
    if (err != null) {
        if (p.Module != null && !allowedVersion(p.Module.GoVersion)) {
            b.showOutput(a, a.Package.Dir, a.Package.Desc(), "note: module requires Go " + p.Module.GoVersion + "\n");
        }
        return error.As(err)!;
    }
    if (ofile != objpkg) {
        objects = append(objects, ofile);
    }
    @string _goos_goarch = "_" + cfg.Goos + "_" + cfg.Goarch;
    @string _goos = "_" + cfg.Goos;
    @string _goarch = "_" + cfg.Goarch;
    {
        var file__prev1 = file;

        foreach (var (_, __file) in a.Package.HFiles) {
            file = __file;
            var (name, ext) = fileExtSplit(file);

            if (strings.HasSuffix(name, _goos_goarch)) 
                var targ = file[..(int)len(name) - len(_goos_goarch)] + "_GOOS_GOARCH." + ext;
                {
                    var err__prev1 = err;

                    err = b.copyFile(objdir + targ, filepath.Join(a.Package.Dir, file), 0666, true);

                    if (err != null) {
                        return error.As(err)!;
                    }

                    err = err__prev1;

                }
            else if (strings.HasSuffix(name, _goarch)) 
                targ = file[..(int)len(name) - len(_goarch)] + "_GOARCH." + ext;
                {
                    var err__prev1 = err;

                    err = b.copyFile(objdir + targ, filepath.Join(a.Package.Dir, file), 0666, true);

                    if (err != null) {
                        return error.As(err)!;
                    }

                    err = err__prev1;

                }
            else if (strings.HasSuffix(name, _goos)) 
                targ = file[..(int)len(name) - len(_goos)] + "_GOOS." + ext;
                {
                    var err__prev1 = err;

                    err = b.copyFile(objdir + targ, filepath.Join(a.Package.Dir, file), 0666, true);

                    if (err != null) {
                        return error.As(err)!;
                    }

                    err = err__prev1;

                }
                    }
        file = file__prev1;
    }

    {
        var file__prev1 = file;

        foreach (var (_, __file) in cfiles) {
            file = __file;
            var @out = file[..(int)len(file) - len(".c")] + ".o";
            {
                var err__prev1 = err;

                err = BuildToolchain.cc(b, a, objdir + out, file);

                if (err != null) {
                    return error.As(err)!;
                }

                err = err__prev1;

            }
            objects = append(objects, out);
        }
        file = file__prev1;
    }

    if (len(sfiles) > 0) {
        var (ofiles, err) = BuildToolchain.asm(b, a, sfiles);
        if (err != null) {
            return error.As(err)!;
        }
        objects = append(objects, ofiles);
    }
    if (a.buildID != "" && cfg.BuildToolchainName == "gccgo") {
        switch (cfg.Goos) {
            case "aix": 

            case "android": 

            case "dragonfly": 

            case "freebsd": 

            case "illumos": 

            case "linux": 

            case "netbsd": 

            case "openbsd": 

            case "solaris": 
                var (asmfile, err) = b.gccgoBuildIDFile(a);
                if (err != null) {
                    return error.As(err)!;
                }
                (ofiles, err) = BuildToolchain.asm(b, a, new slice<@string>(new @string[] { asmfile }));
                if (err != null) {
                    return error.As(err)!;
                }
                objects = append(objects, ofiles);
                break;
        }
    }
    objects = append(objects, cgoObjects); 

    // Add system object files.
    foreach (var (_, syso) in a.Package.SysoFiles) {
        objects = append(objects, filepath.Join(a.Package.Dir, syso));
    }    if (len(objects) > 0) {
        {
            var err__prev2 = err;

            err = BuildToolchain.pack(b, a, objpkg, objects);

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
    }
    {
        var err__prev1 = err;

        err = b.updateBuildID(a, objpkg, true);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    a.built = objpkg;
    return error.As(null!)!;
});

private static error cacheObjdirFile(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<cache.Cache> _addr_c, @string name) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref cache.Cache c = ref _addr_c.val;

    var (f, err) = os.Open(a.Objdir + name);
    if (err != null) {
        return error.As(err)!;
    }
    defer(f.Close());
    _, _, err = c.Put(cache.Subkey(a.actionID, name), f);
    return error.As(err)!;
});

private static (@string, error) findCachedObjdirFile(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<cache.Cache> _addr_c, @string name) {
    @string _p0 = default;
    error _p0 = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref cache.Cache c = ref _addr_c.val;

    var (file, _, err) = c.GetFile(cache.Subkey(a.actionID, name));
    if (err != null) {
        return ("", error.As(fmt.Errorf("loading cached file %s: %w", name, err))!);
    }
    return (file, error.As(null!)!);
}

private static error loadCachedObjdirFile(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<cache.Cache> _addr_c, @string name) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref cache.Cache c = ref _addr_c.val;

    var (cached, err) = b.findCachedObjdirFile(a, c, name);
    if (err != null) {
        return error.As(err)!;
    }
    return error.As(b.copyFile(a.Objdir + name, cached, 0666, true))!;
}

private static void cacheCgoHdr(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var c = cache.Default();
    b.cacheObjdirFile(a, c, "_cgo_install.h");
}

private static error loadCachedCgoHdr(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var c = cache.Default();
    return error.As(b.loadCachedObjdirFile(a, c, "_cgo_install.h"))!;
}

private static void cacheSrcFiles(this ptr<Builder> _addr_b, ptr<Action> _addr_a, slice<@string> srcfiles) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var c = cache.Default();
    bytes.Buffer buf = default;
    foreach (var (_, file) in srcfiles) {
        if (!strings.HasPrefix(file, a.Objdir)) { 
            // not generated
            buf.WriteString("./");
            buf.WriteString(file);
            buf.WriteString("\n");
            continue;
        }
        var name = file[(int)len(a.Objdir)..];
        buf.WriteString(name);
        buf.WriteString("\n");
        {
            var err = b.cacheObjdirFile(a, c, name);

            if (err != null) {
                return ;
            }

        }
    }    c.PutBytes(cache.Subkey(a.actionID, "srcfiles"), buf.Bytes());
}

private static error loadCachedVet(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var c = cache.Default();
    var (list, _, err) = c.GetBytes(cache.Subkey(a.actionID, "srcfiles"));
    if (err != null) {
        return error.As(fmt.Errorf("reading srcfiles list: %w", err))!;
    }
    slice<@string> srcfiles = default;
    foreach (var (_, name) in strings.Split(string(list), "\n")) {
        if (name == "") { // end of list
            continue;
        }
        if (strings.HasPrefix(name, "./")) {
            srcfiles = append(srcfiles, name[(int)2..]);
            continue;
        }
        {
            var err = b.loadCachedObjdirFile(a, c, name);

            if (err != null) {
                return error.As(err)!;
            }

        }
        srcfiles = append(srcfiles, a.Objdir + name);
    }    buildVetConfig(_addr_a, srcfiles);
    return error.As(null!)!;
}

private static error loadCachedSrcFiles(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var c = cache.Default();
    var (list, _, err) = c.GetBytes(cache.Subkey(a.actionID, "srcfiles"));
    if (err != null) {
        return error.As(fmt.Errorf("reading srcfiles list: %w", err))!;
    }
    slice<@string> files = default;
    foreach (var (_, name) in strings.Split(string(list), "\n")) {
        if (name == "") { // end of list
            continue;
        }
        if (strings.HasPrefix(name, "./")) {
            files = append(files, name[(int)len("./")..]);
            continue;
        }
        var (file, err) = b.findCachedObjdirFile(a, c, name);
        if (err != null) {
            return error.As(fmt.Errorf("finding %s: %w", name, err))!;
        }
        files = append(files, file);
    }    a.Package.CompiledGoFiles = files;
    return error.As(null!)!;
}

// vetConfig is the configuration passed to vet describing a single package.
private partial struct vetConfig {
    public @string ID; // package ID (example: "fmt [fmt.test]")
    public @string Compiler; // compiler name (gc, gccgo)
    public @string Dir; // directory containing package
    public @string ImportPath; // canonical import path ("package path")
    public slice<@string> GoFiles; // absolute paths to package source files
    public slice<@string> NonGoFiles; // absolute paths to package non-Go files
    public slice<@string> IgnoredFiles; // absolute paths to ignored source files

    public map<@string, @string> ImportMap; // map import path in source code to package path
    public map<@string, @string> PackageFile; // map package path to .a file with export data
    public map<@string, bool> Standard; // map package path to whether it's in the standard library
    public map<@string, @string> PackageVetx; // map package path to vetx data from earlier vet run
    public bool VetxOnly; // only compute vetx data; don't report detected problems
    public @string VetxOutput; // write vetx data to this output file

    public bool SucceedOnTypecheckFailure; // awful hack; see #18395 and below
}

private static void buildVetConfig(ptr<Action> _addr_a, slice<@string> srcfiles) {
    ref Action a = ref _addr_a.val;
 
    // Classify files based on .go extension.
    // srcfiles does not include raw cgo files.
    slice<@string> gofiles = default;    slice<@string> nongofiles = default;

    foreach (var (_, name) in srcfiles) {
        if (strings.HasSuffix(name, ".go")) {
            gofiles = append(gofiles, name);
        }
        else
 {
            nongofiles = append(nongofiles, name);
        }
    }    var ignored = str.StringList(a.Package.IgnoredGoFiles, a.Package.IgnoredOtherFiles); 

    // Pass list of absolute paths to vet,
    // so that vet's error messages will use absolute paths,
    // so that we can reformat them relative to the directory
    // in which the go command is invoked.
    ptr<vetConfig> vcfg = addr(new vetConfig(ID:a.Package.ImportPath,Compiler:cfg.BuildToolchainName,Dir:a.Package.Dir,GoFiles:mkAbsFiles(a.Package.Dir,gofiles),NonGoFiles:mkAbsFiles(a.Package.Dir,nongofiles),IgnoredFiles:mkAbsFiles(a.Package.Dir,ignored),ImportPath:a.Package.ImportPath,ImportMap:make(map[string]string),PackageFile:make(map[string]string),Standard:make(map[string]bool),));
    a.vetCfg = vcfg;
    foreach (var (i, raw) in a.Package.Internal.RawImports) {
        var final = a.Package.Imports[i];
        vcfg.ImportMap[raw] = final;
    }    var vcfgMapped = make_map<@string, bool>();
    foreach (var (_, p) in vcfg.ImportMap) {
        vcfgMapped[p] = true;
    }    foreach (var (_, a1) in a.Deps) {
        var p1 = a1.Package;
        if (p1 == null || p1.ImportPath == "") {
            continue;
        }
        if (!vcfgMapped[p1.ImportPath]) {
            vcfg.ImportMap[p1.ImportPath] = p1.ImportPath;
        }
        if (a1.built != "") {
            vcfg.PackageFile[p1.ImportPath] = a1.built;
        }
        if (p1.Standard) {
            vcfg.Standard[p1.ImportPath] = true;
        }
    }
}

// VetTool is the path to an alternate vet tool binary.
// The caller is expected to set it (if needed) before executing any vet actions.
public static @string VetTool = default;

// VetFlags are the default flags to pass to vet.
// The caller is expected to set them before executing any vet actions.
public static slice<@string> VetFlags = default;

// VetExplicit records whether the vet flags were set explicitly on the command line.
public static bool VetExplicit = default;

private static error vet(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
 
    // a.Deps[0] is the build of the package being vetted.
    // a.Deps[1] is the build of the "fmt" package.

    a.Failed = false; // vet of dependency may have failed but we can still succeed

    if (a.Deps[0].Failed) { 
        // The build of the package has failed. Skip vet check.
        // Vet could return export data for non-typecheck errors,
        // but we ignore it because the package cannot be compiled.
        return error.As(null!)!;
    }
    var vcfg = a.Deps[0].vetCfg;
    if (vcfg == null) { 
        // Vet config should only be missing if the build failed.
        return error.As(fmt.Errorf("vet config not found"))!;
    }
    vcfg.VetxOnly = a.VetxOnly;
    vcfg.VetxOutput = a.Objdir + "vet.out";
    vcfg.PackageVetx = make_map<@string, @string>();

    var h = cache.NewHash("vet " + a.Package.ImportPath);
    fmt.Fprintf(h, "vet %q\n", b.toolID("vet"));

    var vetFlags = VetFlags; 

    // In GOROOT, we enable all the vet tests during 'go test',
    // not just the high-confidence subset. This gets us extra
    // checking for the standard library (at some compliance cost)
    // and helps us gain experience about how well the checks
    // work, to help decide which should be turned on by default.
    // The command-line still wins.
    //
    // Note that this flag change applies even when running vet as
    // a dependency of vetting a package outside std.
    // (Otherwise we'd have to introduce a whole separate
    // space of "vet fmt as a dependency of a std top-level vet"
    // versus "vet fmt as a dependency of a non-std top-level vet".)
    // This is OK as long as the packages that are farther down the
    // dependency tree turn on *more* analysis, as here.
    // (The unsafeptr check does not write any facts for use by
    // later vet runs, nor does unreachable.)
    if (a.Package.Goroot && !VetExplicit && VetTool == "") { 
        // Turn off -unsafeptr checks.
        // There's too much unsafe.Pointer code
        // that vet doesn't like in low-level packages
        // like runtime, sync, and reflect.
        // Note that $GOROOT/src/buildall.bash
        // does the same for the misc-compile trybots
        // and should be updated if these flags are
        // changed here.
        vetFlags = new slice<@string>(new @string[] { "-unsafeptr=false" }); 

        // Also turn off -unreachable checks during go test.
        // During testing it is very common to make changes
        // like hard-coded forced returns or panics that make
        // code unreachable. It's unreasonable to insist on files
        // not having any unreachable code during "go test".
        // (buildall.bash still runs with -unreachable enabled
        // for the overall whole-tree scan.)
        if (cfg.CmdName == "test") {
            vetFlags = append(vetFlags, "-unreachable=false");
        }
    }
    fmt.Fprintf(h, "vetflags %q\n", vetFlags);

    fmt.Fprintf(h, "pkg %q\n", a.Deps[0].actionID);
    foreach (var (_, a1) in a.Deps) {
        if (a1.Mode == "vet" && a1.built != "") {
            fmt.Fprintf(h, "vetout %q %s\n", a1.Package.ImportPath, b.fileHash(a1.built));
            vcfg.PackageVetx[a1.Package.ImportPath] = a1.built;
        }
    }    var key = cache.ActionID(h.Sum());

    if (vcfg.VetxOnly && !cfg.BuildA) {
        var c = cache.Default();
        {
            var (file, _, err) = c.GetFile(key);

            if (err == null) {
                a.built = file;
                return error.As(null!)!;
            }

        }
    }
    var (js, err) = json.MarshalIndent(vcfg, "", "\t");
    if (err != null) {
        return error.As(fmt.Errorf("internal error marshaling vet config: %v", err))!;
    }
    js = append(js, '\n');
    {
        var err = b.writeFile(a.Objdir + "vet.cfg", js);

        if (err != null) {
            return error.As(err)!;
        }
    } 

    // TODO(rsc): Why do we pass $GCCGO to go vet?
    var env = b.cCompilerEnv();
    if (cfg.BuildToolchainName == "gccgo") {
        env = append(env, "GCCGO=" + BuildToolchain.compiler());
    }
    var p = a.Package;
    var tool = VetTool;
    if (tool == "") {
        tool = @base.Tool("vet");
    }
    var runErr = b.run(a, p.Dir, p.ImportPath, env, cfg.BuildToolexec, tool, vetFlags, a.Objdir + "vet.cfg"); 

    // If vet wrote export data, save it for input to future vets.
    {
        var (f, err) = os.Open(vcfg.VetxOutput);

        if (err == null) {
            a.built = vcfg.VetxOutput;
            cache.Default().Put(key, f);
            f.Close();
        }
    }

    return error.As(runErr)!;
}

// linkActionID computes the action ID for a link action.
private static cache.ActionID linkActionID(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var p = a.Package;
    var h = cache.NewHash("link " + p.ImportPath); 

    // Toolchain-independent configuration.
    fmt.Fprintf(h, "link\n");
    fmt.Fprintf(h, "buildmode %s goos %s goarch %s\n", cfg.BuildBuildmode, cfg.Goos, cfg.Goarch);
    fmt.Fprintf(h, "import %q\n", p.ImportPath);
    fmt.Fprintf(h, "omitdebug %v standard %v local %v prefix %q\n", p.Internal.OmitDebug, p.Standard, p.Internal.Local, p.Internal.LocalPrefix);
    if (cfg.BuildTrimpath) {
        fmt.Fprintln(h, "trimpath");
    }
    b.printLinkerConfig(h, p); 

    // Input files.
    foreach (var (_, a1) in a.Deps) {
        var p1 = a1.Package;
        if (p1 != null) {
            if (a1.built != "" || a1.buildID != "") {
                var buildID = a1.buildID;
                if (buildID == "") {
                    buildID = b.buildID(a1.built);
                }
                fmt.Fprintf(h, "packagefile %s=%s\n", p1.ImportPath, contentID(buildID));
            } 
            // Because we put package main's full action ID into the binary's build ID,
            // we must also put the full action ID into the binary's action ID hash.
            if (p1.Name == "main") {
                fmt.Fprintf(h, "packagemain %s\n", a1.buildID);
            }
            if (p1.Shlib != "") {
                fmt.Fprintf(h, "packageshlib %s=%s\n", p1.ImportPath, contentID(b.buildID(p1.Shlib)));
            }
        }
    }    return h.Sum();
}

// printLinkerConfig prints the linker config into the hash h,
// as part of the computation of a linker-related action ID.
private static void printLinkerConfig(this ptr<Builder> _addr_b, io.Writer h, ptr<load.Package> _addr_p) {
    ref Builder b = ref _addr_b.val;
    ref load.Package p = ref _addr_p.val;

    switch (cfg.BuildToolchainName) {
        case "gc": 
            fmt.Fprintf(h, "link %s %q %s\n", b.toolID("link"), forcedLdflags, ldBuildmode);
            if (p != null) {
                fmt.Fprintf(h, "linkflags %q\n", p.Internal.Ldflags);
            }
            var (key, val) = cfg.GetArchEnv();
            fmt.Fprintf(h, "%s=%s\n", key, val);

            {
                var goexperiment = buildcfg.GOEXPERIMENT();

                if (goexperiment != "") {
                    fmt.Fprintf(h, "GOEXPERIMENT=%q\n", goexperiment);
                } 

                // The linker writes source file paths that say GOROOT_FINAL, but
                // only if -trimpath is not specified (see ld() in gc.go).

            } 

            // The linker writes source file paths that say GOROOT_FINAL, but
            // only if -trimpath is not specified (see ld() in gc.go).
            var gorootFinal = cfg.GOROOT_FINAL;
            if (cfg.BuildTrimpath) {
                gorootFinal = trimPathGoRootFinal;
            }
            fmt.Fprintf(h, "GOROOT=%s\n", gorootFinal); 

            // GO_EXTLINK_ENABLED controls whether the external linker is used.
            fmt.Fprintf(h, "GO_EXTLINK_ENABLED=%s\n", cfg.Getenv("GO_EXTLINK_ENABLED")); 

            // TODO(rsc): Do cgo settings and flags need to be included?
            // Or external linker settings and flags?
            break;
        case "gccgo": 
            var (id, err) = b.gccToolID(BuildToolchain.linker(), "go");
            if (err != null) {
                @base.Fatalf("%v", err);
            }
            fmt.Fprintf(h, "link %s %s\n", id, ldBuildmode); 
            // TODO(iant): Should probably include cgo flags here.
            break;
        default: 
            @base.Fatalf("linkActionID: unknown toolchain %q", cfg.BuildToolchainName);
            break;
    }
}

// link is the action for linking a single command.
// Note that any new influence on this logic must be reported in b.linkActionID above as well.
private static error link(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) => func((defer, _, _) => {
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    if (b.useCache(a, b.linkActionID(a), a.Package.Target) || b.IsCmdList) {
        return error.As(null!)!;
    }
    defer(b.flushOutput(a));

    {
        var err__prev1 = err;

        var err = b.Mkdir(a.Objdir);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    var importcfg = a.Objdir + "importcfg.link";
    {
        var err__prev1 = err;

        err = b.writeLinkImportcfg(a, importcfg);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    {
        var err__prev1 = err;

        err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // make target directory
    var (dir, _) = filepath.Split(a.Target);
    if (dir != "") {
        {
            var err__prev2 = err;

            err = b.Mkdir(dir);

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
    }
    {
        var err__prev1 = err;

        err = BuildToolchain.ld(b, a, a.Target, importcfg, a.Deps[0].built);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // Update the binary with the final build ID.
    // But if OmitDebug is set, don't rewrite the binary, because we set OmitDebug
    // on binaries that we are going to run and then delete.
    // There's no point in doing work on such a binary.
    // Worse, opening the binary for write here makes it
    // essentially impossible to safely fork+exec due to a fundamental
    // incompatibility between ETXTBSY and threads on modern Unix systems.
    // See golang.org/issue/22220.
    // We still call updateBuildID to update a.buildID, which is important
    // for test result caching, but passing rewrite=false (final arg)
    // means we don't actually rewrite the binary, nor store the
    // result into the cache. That's probably a net win:
    // less cache space wasted on large binaries we are not likely to
    // need again. (On the other hand it does make repeated go test slower.)
    // It also makes repeated go run slower, which is a win in itself:
    // we don't want people to treat go run like a scripting environment.
    {
        var err__prev1 = err;

        err = b.updateBuildID(a, a.Target, !a.Package.Internal.OmitDebug);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    a.built = a.Target;
    return error.As(null!)!;
});

private static error writeLinkImportcfg(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string file) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
 
    // Prepare Go import cfg.
    ref bytes.Buffer icfg = ref heap(out ptr<bytes.Buffer> _addr_icfg);
    foreach (var (_, a1) in a.Deps) {
        var p1 = a1.Package;
        if (p1 == null) {
            continue;
        }
        fmt.Fprintf(_addr_icfg, "packagefile %s=%s\n", p1.ImportPath, a1.built);
        if (p1.Shlib != "") {
            fmt.Fprintf(_addr_icfg, "packageshlib %s=%s\n", p1.ImportPath, p1.Shlib);
        }
    }    return error.As(b.writeFile(file, icfg.Bytes()))!;
}

// PkgconfigCmd returns a pkg-config binary name
// defaultPkgConfig is defined in zdefaultcc.go, written by cmd/dist.
private static @string PkgconfigCmd(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    return envList("PKG_CONFIG", cfg.DefaultPkgConfig)[0];
}

// splitPkgConfigOutput parses the pkg-config output into a slice of
// flags. This implements the algorithm from pkgconf/libpkgconf/argvsplit.c.
private static (slice<@string>, error) splitPkgConfigOutput(slice<byte> @out) {
    slice<@string> _p0 = default;
    error _p0 = default!;

    if (len(out) == 0) {
        return (null, error.As(null!)!);
    }
    slice<@string> flags = default;
    var flag = make_slice<byte>(0, len(out));
    var escaped = false;
    var quote = byte(0);

    foreach (var (_, c) in out) {
        if (escaped) {
            if (quote != 0) {
                switch (c) {
                    case '$': 

                    case '`': 

                    case '"': 

                    case '\\': 

                        break;
                    default: 
                        flag = append(flag, '\\');
                        break;
                }
                flag = append(flag, c);
            }
            else
 {
                flag = append(flag, c);
            }
            escaped = false;
        }
        else if (quote != 0) {
            if (c == quote) {
                quote = 0;
            }
            else
 {
                switch (c) {
                    case '\\': 
                        escaped = true;
                        break;
                    default: 
                        flag = append(flag, c);
                        break;
                }
            }
        }
        else if (strings.IndexByte(" \t\n\v\f\r", c) < 0) {
            switch (c) {
                case '\\': 
                    escaped = true;
                    break;
                case '\'': 

                case '"': 
                    quote = c;
                    break;
                default: 
                    flag = append(flag, c);
                    break;
            }
        }
        else if (len(flag) != 0) {
            flags = append(flags, string(flag));
            flag = flag[..(int)0];
        }
    }    if (escaped) {
        return (null, error.As(errors.New("broken character escaping in pkgconf output "))!);
    }
    if (quote != 0) {
        return (null, error.As(errors.New("unterminated quoted string in pkgconf output "))!);
    }
    else if (len(flag) != 0) {
        flags = append(flags, string(flag));
    }
    return (flags, error.As(null!)!);
}

// Calls pkg-config if needed and returns the cflags/ldflags needed to build the package.
private static (slice<@string>, slice<@string>, error) getPkgConfigFlags(this ptr<Builder> _addr_b, ptr<load.Package> _addr_p) {
    slice<@string> cflags = default;
    slice<@string> ldflags = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref load.Package p = ref _addr_p.val;

    {
        var pcargs = p.CgoPkgConfig;

        if (len(pcargs) > 0) { 
            // pkg-config permits arguments to appear anywhere in
            // the command line. Move them all to the front, before --.
            slice<@string> pcflags = default;
            slice<@string> pkgs = default;
            foreach (var (_, pcarg) in pcargs) {
                if (pcarg == "--") { 
                    // We're going to add our own "--" argument.
                }
                else if (strings.HasPrefix(pcarg, "--")) {
                    pcflags = append(pcflags, pcarg);
                }
                else
 {
                    pkgs = append(pkgs, pcarg);
                }
            }
            foreach (var (_, pkg) in pkgs) {
                if (!load.SafeArg(pkg)) {
                    return (null, null, error.As(fmt.Errorf("invalid pkg-config package name: %s", pkg))!);
                }
            }
            slice<byte> @out = default;
            out, err = b.runOut(null, p.Dir, null, b.PkgconfigCmd(), "--cflags", pcflags, "--", pkgs);
            if (err != null) {
                b.showOutput(null, p.Dir, b.PkgconfigCmd() + " --cflags " + strings.Join(pcflags, " ") + " -- " + strings.Join(pkgs, " "), string(out));
                b.Print(err.Error() + "\n");
                return (null, null, error.As(errPrintedOutput)!);
            }
            if (len(out) > 0) {
                cflags, err = splitPkgConfigOutput(out);
                if (err != null) {
                    return (null, null, error.As(err)!);
                }
                {
                    var err__prev3 = err;

                    var err = checkCompilerFlags("CFLAGS", "pkg-config --cflags", cflags);

                    if (err != null) {
                        return (null, null, error.As(err)!);
                    }

                    err = err__prev3;

                }
            }
            out, err = b.runOut(null, p.Dir, null, b.PkgconfigCmd(), "--libs", pcflags, "--", pkgs);
            if (err != null) {
                b.showOutput(null, p.Dir, b.PkgconfigCmd() + " --libs " + strings.Join(pcflags, " ") + " -- " + strings.Join(pkgs, " "), string(out));
                b.Print(err.Error() + "\n");
                return (null, null, error.As(errPrintedOutput)!);
            }
            if (len(out) > 0) {
                ldflags = strings.Fields(string(out));
                {
                    var err__prev3 = err;

                    err = checkLinkerFlags("LDFLAGS", "pkg-config --libs", ldflags);

                    if (err != null) {
                        return (null, null, error.As(err)!);
                    }

                    err = err__prev3;

                }
            }
        }
    }

    return ;
}

private static error installShlibname(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    {
        var err__prev1 = err;

        var err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // TODO: BuildN
    var a1 = a.Deps[0];
    err = os.WriteFile(a.Target, (slice<byte>)filepath.Base(a1.Target) + "\n", 0666);
    if (err != null) {
        return error.As(err)!;
    }
    if (cfg.BuildX) {
        b.Showcmd("", "echo '%s' > %s # internal", filepath.Base(a1.Target), a.Target);
    }
    return error.As(null!)!;
}

private static cache.ActionID linkSharedActionID(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var h = cache.NewHash("linkShared"); 

    // Toolchain-independent configuration.
    fmt.Fprintf(h, "linkShared\n");
    fmt.Fprintf(h, "goos %s goarch %s\n", cfg.Goos, cfg.Goarch); 

    // Toolchain-dependent configuration, shared with b.linkActionID.
    b.printLinkerConfig(h, null); 

    // Input files.
    {
        var a1__prev1 = a1;

        foreach (var (_, __a1) in a.Deps) {
            a1 = __a1;
            var p1 = a1.Package;
            if (a1.built == "") {
                continue;
            }
            if (p1 != null) {
                fmt.Fprintf(h, "packagefile %s=%s\n", p1.ImportPath, contentID(b.buildID(a1.built)));
                if (p1.Shlib != "") {
                    fmt.Fprintf(h, "packageshlib %s=%s\n", p1.ImportPath, contentID(b.buildID(p1.Shlib)));
                }
            }
        }
        a1 = a1__prev1;
    }

    {
        var a1__prev1 = a1;

        foreach (var (_, __a1) in a.Deps[0].Deps) {
            a1 = __a1;
            p1 = a1.Package;
            fmt.Fprintf(h, "top %s=%s\n", p1.ImportPath, contentID(b.buildID(a1.built)));
        }
        a1 = a1__prev1;
    }

    return h.Sum();
}

private static error linkShared(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) => func((defer, _, _) => {
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    if (b.useCache(a, b.linkSharedActionID(a), a.Target) || b.IsCmdList) {
        return error.As(null!)!;
    }
    defer(b.flushOutput(a));

    {
        var err__prev1 = err;

        var err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    {
        var err__prev1 = err;

        err = b.Mkdir(a.Objdir);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    var importcfg = a.Objdir + "importcfg.link";
    {
        var err__prev1 = err;

        err = b.writeLinkImportcfg(a, importcfg);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // TODO(rsc): There is a missing updateBuildID here,
    // but we have to decide where to store the build ID in these files.
    a.built = a.Target;
    return error.As(BuildToolchain.ldShared(b, a, a.Deps[0].Deps, a.Target, importcfg, a.Deps))!;
});

// BuildInstallFunc is the action for installing a single package or executable.
public static error BuildInstallFunc(ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) => func((defer, _, _) => {
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    defer(() => {
        if (err != null && err != errPrintedOutput) { 
            // a.Package == nil is possible for the go install -buildmode=shared
            // action that installs libmangledname.so, which corresponds to
            // a list of packages, not just one.
            @string sep = "";
            @string path = "";
            if (a.Package != null) {
                (sep, path) = (" ", a.Package.ImportPath);
            }
            err = fmt.Errorf("go %s%s%s: %v", cfg.CmdName, sep, path, err);
        }
    }());

    var a1 = a.Deps[0];
    a.buildID = a1.buildID;
    if (a.json != null) {
        a.json.BuildID = a.buildID;
    }
    if (a1.built == a.Target) {
        a.built = a.Target;
        if (!a.buggyInstall) {
            b.cleanup(a1);
        }
        if (!a.buggyInstall && !b.IsCmdList) {
            if (cfg.BuildN) {
                b.Showcmd("", "touch %s", a.Target);
            }            {
                var err__prev4 = err;

                var err = allowInstall(a);


                else if (err == null) {
                    var now = time.Now();
                    os.Chtimes(a.Target, now, now);
                }

                err = err__prev4;

            }
        }
        return error.As(null!)!;
    }
    if (b.IsCmdList) {
        a.built = a1.built;
        return error.As(null!)!;
    }
    {
        var err__prev1 = err;

        err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    {
        var err__prev1 = err;

        err = b.Mkdir(a.Objdir);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    var perm = fs.FileMode(0666);
    if (a1.Mode == "link") {
        switch (cfg.BuildBuildmode) {
            case "c-archive": 

            case "c-shared": 

            case "plugin": 

                break;
            default: 
                perm = 0777;
                break;
        }
    }
    var (dir, _) = filepath.Split(a.Target);
    if (dir != "") {
        {
            var err__prev2 = err;

            err = b.Mkdir(dir);

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
    }
    if (!a.buggyInstall) {
        defer(b.cleanup(a1));
    }
    return error.As(b.moveOrCopyFile(a.Target, a1.built, perm, false))!;
});

// allowInstall returns a non-nil error if this invocation of the go command is
// allowed to install a.Target.
//
// (The build of cmd/go running under its own test is forbidden from installing
// to its original GOROOT.)
private static Func<ptr<Action>, error> allowInstall = _p0 => null;

// cleanup removes a's object dir to keep the amount of
// on-disk garbage down in a large build. On an operating system
// with aggressive buffering, cleaning incrementally like
// this keeps the intermediate objects from hitting the disk.
private static void cleanup(this ptr<Builder> _addr_b, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    if (!cfg.BuildWork) {
        if (cfg.BuildX) { 
            // Don't say we are removing the directory if
            // we never created it.
            {
                var (_, err) = os.Stat(a.Objdir);

                if (err == null || cfg.BuildN) {
                    b.Showcmd("", "rm -r %s", a.Objdir);
                }

            }
        }
        os.RemoveAll(a.Objdir);
    }
}

// moveOrCopyFile is like 'mv src dst' or 'cp src dst'.
private static error moveOrCopyFile(this ptr<Builder> _addr_b, @string dst, @string src, fs.FileMode perm, bool force) {
    ref Builder b = ref _addr_b.val;

    if (cfg.BuildN) {
        b.Showcmd("", "mv %s %s", src, dst);
        return error.As(null!)!;
    }
    if (strings.HasPrefix(src, cache.DefaultDir())) {
        return error.As(b.copyFile(dst, src, perm, force))!;
    }
    if (runtime.GOOS == "windows") {
        return error.As(b.copyFile(dst, src, perm, force))!;
    }
    {
        var fi__prev1 = fi;
        var err__prev1 = err;

        var (fi, err) = os.Stat(filepath.Dir(dst));

        if (err == null) {
            if (fi.IsDir() && (fi.Mode() & fs.ModeSetgid) != 0) {
                return error.As(b.copyFile(dst, src, perm, force))!;
            }
        }
        fi = fi__prev1;
        err = err__prev1;

    } 

    // The perm argument is meant to be adjusted according to umask,
    // but we don't know what the umask is.
    // Create a dummy file to find out.
    // This avoids build tags and works even on systems like Plan 9
    // where the file mask computation incorporates other information.
    var mode = perm;
    var (f, err) = os.OpenFile(filepath.Clean(dst) + "-go-tmp-umask", os.O_WRONLY | os.O_CREATE | os.O_EXCL, perm);
    if (err == null) {
        (fi, err) = f.Stat();
        if (err == null) {
            mode = fi.Mode() & 0777;
        }
        var name = f.Name();
        f.Close();
        os.Remove(name);
    }
    {
        var err__prev1 = err;

        var err = os.Chmod(src, mode);

        if (err == null) {
            {
                var err__prev2 = err;

                err = os.Rename(src, dst);

                if (err == null) {
                    if (cfg.BuildX) {
                        b.Showcmd("", "mv %s %s", src, dst);
                    }
                    return error.As(null!)!;
                }

                err = err__prev2;

            }
        }
        err = err__prev1;

    }

    return error.As(b.copyFile(dst, src, perm, force))!;
}

// copyFile is like 'cp src dst'.
private static error copyFile(this ptr<Builder> _addr_b, @string dst, @string src, fs.FileMode perm, bool force) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;

    if (cfg.BuildN || cfg.BuildX) {
        b.Showcmd("", "cp %s %s", src, dst);
        if (cfg.BuildN) {
            return error.As(null!)!;
        }
    }
    var (sf, err) = os.Open(src);
    if (err != null) {
        return error.As(err)!;
    }
    defer(sf.Close()); 

    // Be careful about removing/overwriting dst.
    // Do not remove/overwrite if dst exists and is a directory
    // or a non-empty non-object file.
    {
        var (fi, err) = os.Stat(dst);

        if (err == null) {
            if (fi.IsDir()) {
                return error.As(fmt.Errorf("build output %q already exists and is a directory", dst))!;
            }
            if (!force && fi.Mode().IsRegular() && fi.Size() != 0 && !isObject(dst)) {
                return error.As(fmt.Errorf("build output %q already exists and is not an object file", dst))!;
            }
        }
    } 

    // On Windows, remove lingering ~ file from last attempt.
    if (@base.ToolIsWindows) {
        {
            var (_, err) = os.Stat(dst + "~");

            if (err == null) {
                os.Remove(dst + "~");
            }

        }
    }
    mayberemovefile(dst);
    var (df, err) = os.OpenFile(dst, os.O_WRONLY | os.O_CREATE | os.O_TRUNC, perm);
    if (err != null && @base.ToolIsWindows) { 
        // Windows does not allow deletion of a binary file
        // while it is executing. Try to move it out of the way.
        // If the move fails, which is likely, we'll try again the
        // next time we do an install of this binary.
        {
            var err = os.Rename(dst, dst + "~");

            if (err == null) {
                os.Remove(dst + "~");
            }

        }
        df, err = os.OpenFile(dst, os.O_WRONLY | os.O_CREATE | os.O_TRUNC, perm);
    }
    if (err != null) {
        return error.As(fmt.Errorf("copying %s: %w", src, err))!; // err should already refer to dst
    }
    _, err = io.Copy(df, sf);
    df.Close();
    if (err != null) {
        mayberemovefile(dst);
        return error.As(fmt.Errorf("copying %s to %s: %v", src, dst, err))!;
    }
    return error.As(null!)!;
});

// writeFile writes the text to file.
private static error writeFile(this ptr<Builder> _addr_b, @string file, slice<byte> text) {
    ref Builder b = ref _addr_b.val;

    if (cfg.BuildN || cfg.BuildX) {
        b.Showcmd("", "cat >%s << 'EOF' # internal\n%sEOF", file, text);
    }
    if (cfg.BuildN) {
        return error.As(null!)!;
    }
    return error.As(os.WriteFile(file, text, 0666))!;
}

// Install the cgo export header file, if there is one.
private static error installHeader(this ptr<Builder> _addr_b, context.Context ctx, ptr<Action> _addr_a) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var src = a.Objdir + "_cgo_install.h";
    {
        var err__prev1 = err;

        var (_, err) = os.Stat(src);

        if (os.IsNotExist(err)) { 
            // If the file does not exist, there are no exported
            // functions, and we do not install anything.
            // TODO(rsc): Once we know that caching is rebuilding
            // at the right times (not missing rebuilds), here we should
            // probably delete the installed header, if any.
            if (cfg.BuildX) {
                b.Showcmd("", "# %s not created", src);
            }
            return error.As(null!)!;
        }
        err = err__prev1;

    }

    {
        var err__prev1 = err;

        var err = allowInstall(a);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    var (dir, _) = filepath.Split(a.Target);
    if (dir != "") {
        {
            var err__prev2 = err;

            err = b.Mkdir(dir);

            if (err != null) {
                return error.As(err)!;
            }

            err = err__prev2;

        }
    }
    return error.As(b.moveOrCopyFile(a.Target, src, 0666, true))!;
}

// cover runs, in effect,
//    go tool cover -mode=b.coverMode -var="varName" -o dst.go src.go
private static error cover(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string dst, @string src, @string varName) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return error.As(b.run(a, a.Objdir, "cover " + a.Package.ImportPath, null, cfg.BuildToolexec, @base.Tool("cover"), "-mode", a.Package.Internal.CoverMode, "-var", varName, "-o", dst, src))!;
}

private static slice<byte> objectMagic = new slice<slice<byte>>(new slice<byte>[] { {'!','<','a','r','c','h','>','\n'}, {'<','b','i','g','a','f','>','\n'}, {'\x7F','E','L','F'}, {0xFE,0xED,0xFA,0xCE}, {0xFE,0xED,0xFA,0xCF}, {0xCE,0xFA,0xED,0xFE}, {0xCF,0xFA,0xED,0xFE}, {0x4d,0x5a,0x90,0x00,0x03,0x00}, {0x4d,0x5a,0x78,0x00,0x01,0x00}, {0x00,0x00,0x01,0xEB}, {0x00,0x00,0x8a,0x97}, {0x00,0x00,0x06,0x47}, {0x00,0x61,0x73,0x6D}, {0x01,0xDF}, {0x01,0xF7} });

private static bool isObject(@string s) => func((defer, _, _) => {
    var (f, err) = os.Open(s);
    if (err != null) {
        return false;
    }
    defer(f.Close());
    var buf = make_slice<byte>(64);
    io.ReadFull(f, buf);
    foreach (var (_, magic) in objectMagic) {
        if (bytes.HasPrefix(buf, magic)) {
            return true;
        }
    }    return false;
});

// mayberemovefile removes a file only if it is a regular file
// When running as a user with sufficient privileges, we may delete
// even device files, for example, which is not intended.
private static void mayberemovefile(@string s) {
    {
        var (fi, err) = os.Lstat(s);

        if (err == null && !fi.Mode().IsRegular()) {
            return ;
        }
    }
    os.Remove(s);
}

// fmtcmd formats a command in the manner of fmt.Sprintf but also:
//
//    If dir is non-empty and the script is not in dir right now,
//    fmtcmd inserts "cd dir\n" before the command.
//
//    fmtcmd replaces the value of b.WorkDir with $WORK.
//    fmtcmd replaces the value of goroot with $GOROOT.
//    fmtcmd replaces the value of b.gobin with $GOBIN.
//
//    fmtcmd replaces the name of the current directory with dot (.)
//    but only when it is at the beginning of a space-separated token.
//
private static @string fmtcmd(this ptr<Builder> _addr_b, @string dir, @string format, params object[] args) {
    args = args.Clone();
    ref Builder b = ref _addr_b.val;

    var cmd = fmt.Sprintf(format, args);
    if (dir != "" && dir != "/") {
        @string dot = " .";
        if (dir[len(dir) - 1] == filepath.Separator) {
            dot += string(filepath.Separator);
        }
        cmd = strings.ReplaceAll(" " + cmd, " " + dir, dot)[(int)1..];
        if (b.scriptDir != dir) {
            b.scriptDir = dir;
            cmd = "cd " + dir + "\n" + cmd;
        }
    }
    if (b.WorkDir != "") {
        cmd = strings.ReplaceAll(cmd, b.WorkDir, "$WORK");
        var escaped = strconv.Quote(b.WorkDir);
        escaped = escaped[(int)1..(int)len(escaped) - 1]; // strip quote characters
        if (escaped != b.WorkDir) {
            cmd = strings.ReplaceAll(cmd, escaped, "$WORK");
        }
    }
    return cmd;
}

// showcmd prints the given command to standard output
// for the implementation of -n or -x.
private static void Showcmd(this ptr<Builder> _addr_b, @string dir, @string format, params object[] args) => func((defer, _, _) => {
    args = args.Clone();
    ref Builder b = ref _addr_b.val;

    b.output.Lock();
    defer(b.output.Unlock());
    b.Print(b.fmtcmd(dir, format, args) + "\n");
});

// showOutput prints "# desc" followed by the given output.
// The output is expected to contain references to 'dir', usually
// the source directory for the package that has failed to build.
// showOutput rewrites mentions of dir with a relative path to dir
// when the relative path is shorter. This is usually more pleasant.
// For example, if fmt doesn't compile and we are in src/html,
// the output is
//
//    $ go build
//    # fmt
//    ../fmt/print.go:1090: undefined: asdf
//    $
//
// instead of
//
//    $ go build
//    # fmt
//    /usr/gopher/go/src/fmt/print.go:1090: undefined: asdf
//    $
//
// showOutput also replaces references to the work directory with $WORK.
//
// If a is not nil and a.output is not nil, showOutput appends to that slice instead of
// printing to b.Print.
//
private static void showOutput(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string dir, @string desc, @string @out) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    @string prefix = "# " + desc;
    @string suffix = "\n" + out;
    {
        var reldir = @base.ShortPath(dir);

        if (reldir != dir) {
            suffix = strings.ReplaceAll(suffix, " " + dir, " " + reldir);
            suffix = strings.ReplaceAll(suffix, "\n" + dir, "\n" + reldir);
        }
    }
    suffix = strings.ReplaceAll(suffix, " " + b.WorkDir, " $WORK");

    if (a != null && a.output != null) {
        a.output = append(a.output, prefix);
        a.output = append(a.output, suffix);
        return ;
    }
    b.output.Lock();
    defer(b.output.Unlock());
    b.Print(prefix, suffix);
});

// errPrintedOutput is a special error indicating that a command failed
// but that it generated output as well, and that output has already
// been printed, so there's no point showing 'exit status 1' or whatever
// the wait status was. The main executor, builder.do, knows not to
// print this error.
private static var errPrintedOutput = errors.New("already printed output - no need to show error");

private static var cgoLine = lazyregexp.New("\\[[^\\[\\]]+\\.(cgo1|cover)\\.go:[0-9]+(:[0-9]+)?\\]");
private static var cgoTypeSigRe = lazyregexp.New("\\b_C2?(type|func|var|macro)_\\B");

// run runs the command given by cmdline in the directory dir.
// If the command fails, run prints information about the failure
// and returns a non-nil error.
private static error run(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string dir, @string desc, slice<@string> env, params object[] cmdargs) {
    cmdargs = cmdargs.Clone();
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var (out, err) = b.runOut(a, dir, env, cmdargs);
    if (len(out) > 0) {
        if (desc == "") {
            desc = b.fmtcmd(dir, "%s", strings.Join(str.StringList(cmdargs), " "));
        }
        b.showOutput(a, dir, desc, b.processOutput(out));
        if (err != null) {
            err = errPrintedOutput;
        }
    }
    return error.As(err)!;
}

// processOutput prepares the output of runOut to be output to the console.
private static @string processOutput(this ptr<Builder> _addr_b, slice<byte> @out) {
    ref Builder b = ref _addr_b.val;

    if (out[len(out) - 1] != '\n') {
        out = append(out, '\n');
    }
    var messages = string(out); 
    // Fix up output referring to cgo-generated code to be more readable.
    // Replace x.go:19[/tmp/.../x.cgo1.go:18] with x.go:19.
    // Replace *[100]_Ctype_foo with *[100]C.foo.
    // If we're using -x, assume we're debugging and want the full dump, so disable the rewrite.
    if (!cfg.BuildX && cgoLine.MatchString(messages)) {
        messages = cgoLine.ReplaceAllString(messages, "");
        messages = cgoTypeSigRe.ReplaceAllString(messages, "C.");
    }
    return messages;
}

// runOut runs the command given by cmdline in the directory dir.
// It returns the command output and any errors that occurred.
// It accumulates execution time in a.
private static (slice<byte>, error) runOut(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string dir, slice<@string> env, params object[] cmdargs) => func((defer, _, _) => {
    slice<byte> _p0 = default;
    error _p0 = default!;
    cmdargs = cmdargs.Clone();
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var cmdline = str.StringList(cmdargs);

    foreach (var (_, arg) in cmdline) { 
        // GNU binutils commands, including gcc and gccgo, interpret an argument
        // @foo anywhere in the command line (even following --) as meaning
        // "read and insert arguments from the file named foo."
        // Don't say anything that might be misinterpreted that way.
        if (strings.HasPrefix(arg, "@")) {
            return (null, error.As(fmt.Errorf("invalid command-line argument %s in command: %s", arg, joinUnambiguously(cmdline)))!);
        }
    }    if (cfg.BuildN || cfg.BuildX) {
        @string envcmdline = default;
        foreach (var (_, e) in env) {
            {
                var j = strings.IndexByte(e, '=');

                if (j != -1) {
                    if (strings.ContainsRune(e[(int)j + 1..], '\'')) {
                        envcmdline += fmt.Sprintf("%s=%q", e[..(int)j], e[(int)j + 1..]);
                    }
                    else
 {
                        envcmdline += fmt.Sprintf("%s='%s'", e[..(int)j], e[(int)j + 1..]);
                    }
                    envcmdline += " ";
                }

            }
        }        envcmdline += joinUnambiguously(cmdline);
        b.Showcmd(dir, "%s", envcmdline);
        if (cfg.BuildN) {
            return (null, error.As(null!)!);
        }
    }
    ref bytes.Buffer buf = ref heap(out ptr<bytes.Buffer> _addr_buf);
    var cmd = exec.Command(cmdline[0], cmdline[(int)1..]);
    if (cmd.Path != "") {
        cmd.Args[0] = cmd.Path;
    }
    _addr_cmd.Stdout = _addr_buf;
    cmd.Stdout = ref _addr_cmd.Stdout.val;
    _addr_cmd.Stderr = _addr_buf;
    cmd.Stderr = ref _addr_cmd.Stderr.val;
    var cleanup = passLongArgsInResponseFiles(_addr_cmd);
    defer(cleanup());
    cmd.Dir = dir;
    cmd.Env = @base.AppendPWD(os.Environ(), cmd.Dir); 

    // Add the TOOLEXEC_IMPORTPATH environment variable for -toolexec tools.
    // It doesn't really matter if -toolexec isn't being used.
    // Note that a.Package.Desc is not really an import path,
    // but this is consistent with 'go list -f {{.ImportPath}}'.
    // Plus, it is useful to uniquely identify packages in 'go list -json'.
    if (a != null && a.Package != null) {
        cmd.Env = append(cmd.Env, "TOOLEXEC_IMPORTPATH=" + a.Package.Desc());
    }
    cmd.Env = append(cmd.Env, env);
    var start = time.Now();
    var err = cmd.Run();
    if (a != null && a.json != null) {
        var aj = a.json;
        aj.Cmd = append(aj.Cmd, joinUnambiguously(cmdline));
        aj.CmdReal += time.Since(start);
        {
            var ps = cmd.ProcessState;

            if (ps != null) {
                aj.CmdUser += ps.UserTime();
                aj.CmdSys += ps.SystemTime();
            }

        }
    }
    if (err != null) {
        err = errors.New(cmdline[0] + ": " + err.Error());
    }
    return (buf.Bytes(), error.As(err)!);
});

// joinUnambiguously prints the slice, quoting where necessary to make the
// output unambiguous.
// TODO: See issue 5279. The printing of commands needs a complete redo.
private static @string joinUnambiguously(slice<@string> a) {
    bytes.Buffer buf = default;
    foreach (var (i, s) in a) {
        if (i > 0) {
            buf.WriteByte(' ');
        }
        var q = strconv.Quote(s); 
        // A gccgo command line can contain -( and -).
        // Make sure we quote them since they are special to the shell.
        // The trimpath argument can also contain > (part of =>) and ;. Quote those too.
        if (s == "" || strings.ContainsAny(s, " ()>;") || len(q) > len(s) + 2) {
            buf.WriteString(q);
        }
        else
 {
            buf.WriteString(s);
        }
    }    return buf.String();
}

// cCompilerEnv returns environment variables to set when running the
// C compiler. This is needed to disable escape codes in clang error
// messages that confuse tools like cgo.
private static slice<@string> cCompilerEnv(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    return new slice<@string>(new @string[] { "TERM=dumb" });
}

// mkdir makes the named directory.
private static error Mkdir(this ptr<Builder> _addr_b, @string dir) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;
 
    // Make Mkdir(a.Objdir) a no-op instead of an error when a.Objdir == "".
    if (dir == "") {
        return error.As(null!)!;
    }
    b.exec.Lock();
    defer(b.exec.Unlock()); 
    // We can be a little aggressive about being
    // sure directories exist. Skip repeated calls.
    if (b.mkdirCache[dir]) {
        return error.As(null!)!;
    }
    b.mkdirCache[dir] = true;

    if (cfg.BuildN || cfg.BuildX) {
        b.Showcmd("", "mkdir -p %s", dir);
        if (cfg.BuildN) {
            return error.As(null!)!;
        }
    }
    {
        var err = os.MkdirAll(dir, 0777);

        if (err != null) {
            return error.As(err)!;
        }
    }
    return error.As(null!)!;
});

// symlink creates a symlink newname -> oldname.
private static error Symlink(this ptr<Builder> _addr_b, @string oldname, @string newname) {
    ref Builder b = ref _addr_b.val;
 
    // It's not an error to try to recreate an existing symlink.
    {
        var (link, err) = os.Readlink(newname);

        if (err == null && link == oldname) {
            return error.As(null!)!;
        }
    }

    if (cfg.BuildN || cfg.BuildX) {
        b.Showcmd("", "ln -s %s %s", oldname, newname);
        if (cfg.BuildN) {
            return error.As(null!)!;
        }
    }
    return error.As(os.Symlink(oldname, newname))!;
}

// mkAbs returns an absolute path corresponding to
// evaluating f in the directory dir.
// We always pass absolute paths of source files so that
// the error messages will include the full path to a file
// in need of attention.
private static @string mkAbs(@string dir, @string f) { 
    // Leave absolute paths alone.
    // Also, during -n mode we use the pseudo-directory $WORK
    // instead of creating an actual work directory that won't be used.
    // Leave paths beginning with $WORK alone too.
    if (filepath.IsAbs(f) || strings.HasPrefix(f, "$WORK")) {
        return f;
    }
    return filepath.Join(dir, f);
}

private partial interface toolchain {
    @string gc(ptr<Builder> b, ptr<Action> a, @string archive, slice<byte> importcfg, slice<byte> embedcfg, @string symabis, bool asmhdr, slice<@string> gofiles); // cc runs the toolchain's C compiler in a directory on a C file
// to produce an output file.
    @string cc(ptr<Builder> b, ptr<Action> a, @string ofile, @string cfile); // asm runs the assembler in a specific directory on specific files
// and returns a list of named output files.
    @string asm(ptr<Builder> b, ptr<Action> a, slice<@string> sfiles); // symabis scans the symbol ABIs from sfiles and returns the
// path to the output symbol ABIs file, or "" if none.
    @string symabis(ptr<Builder> b, ptr<Action> a, slice<@string> sfiles); // pack runs the archive packer in a specific directory to create
// an archive from a set of object files.
// typically it is run in the object directory.
    @string pack(ptr<Builder> b, ptr<Action> a, @string afile, slice<@string> ofiles); // ld runs the linker to create an executable starting at mainpkg.
    @string ld(ptr<Builder> b, ptr<Action> root, @string @out, @string importcfg, @string mainpkg); // ldShared runs the linker to create a shared library containing the pkgs built by toplevelactions
    @string ldShared(ptr<Builder> b, ptr<Action> root, slice<ptr<Action>> toplevelactions, @string @out, @string importcfg, slice<ptr<Action>> allactions);
    @string compiler();
    @string linker();
}

private partial struct noToolchain {
}

private static error noCompiler() {
    log.Fatalf("unknown compiler %q", cfg.BuildContext.Compiler);
    return error.As(null!)!;
}

private static @string compiler(this noToolchain _p0) {
    noCompiler();
    return "";
}

private static @string linker(this noToolchain _p0) {
    noCompiler();
    return "";
}

private static (@string, slice<byte>, error) gc(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_a, @string archive, slice<byte> importcfg, slice<byte> embedcfg, @string symabis, bool asmhdr, slice<@string> gofiles) {
    @string ofile = default;
    slice<byte> @out = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return ("", null, error.As(noCompiler())!);
}

private static (slice<@string>, error) asm(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_a, slice<@string> sfiles) {
    slice<@string> _p0 = default;
    error _p0 = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return (null, error.As(noCompiler())!);
}

private static (@string, error) symabis(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_a, slice<@string> sfiles) {
    @string _p0 = default;
    error _p0 = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return ("", error.As(noCompiler())!);
}

private static error pack(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_a, @string afile, slice<@string> ofiles) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return error.As(noCompiler())!;
}

private static error ld(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_root, @string @out, @string importcfg, @string mainpkg) {
    ref Builder b = ref _addr_b.val;
    ref Action root = ref _addr_root.val;

    return error.As(noCompiler())!;
}

private static error ldShared(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_root, slice<ptr<Action>> toplevelactions, @string @out, @string importcfg, slice<ptr<Action>> allactions) {
    ref Builder b = ref _addr_b.val;
    ref Action root = ref _addr_root.val;

    return error.As(noCompiler())!;
}

private static error cc(this noToolchain _p0, ptr<Builder> _addr_b, ptr<Action> _addr_a, @string ofile, @string cfile) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    return error.As(noCompiler())!;
}

// gcc runs the gcc C compiler to create an object from a single C file.
private static error gcc(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string workdir, @string @out, slice<@string> flags, @string cfile) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    return error.As(b.ccompile(a, p, out, flags, cfile, b.GccCmd(p.Dir, workdir)))!;
}

// gxx runs the g++ C++ compiler to create an object from a single C++ file.
private static error gxx(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string workdir, @string @out, slice<@string> flags, @string cxxfile) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    return error.As(b.ccompile(a, p, out, flags, cxxfile, b.GxxCmd(p.Dir, workdir)))!;
}

// gfortran runs the gfortran Fortran compiler to create an object from a single Fortran file.
private static error gfortran(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string workdir, @string @out, slice<@string> flags, @string ffile) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    return error.As(b.ccompile(a, p, out, flags, ffile, b.gfortranCmd(p.Dir, workdir)))!;
}

// ccompile runs the given C or C++ compiler and creates an object from a single source file.
private static error ccompile(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string outfile, slice<@string> flags, @string file, slice<@string> compiler) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    file = mkAbs(p.Dir, file);
    var desc = p.ImportPath;
    outfile = mkAbs(p.Dir, outfile); 

    // Elide source directory paths if -trimpath or GOROOT_FINAL is set.
    // This is needed for source files (e.g., a .c file in a package directory).
    // TODO(golang.org/issue/36072): cgo also generates files with #line
    // directives pointing to the source directory. It should not generate those
    // when -trimpath is enabled.
    if (b.gccSupportsFlag(compiler, "-fdebug-prefix-map=a=b")) {
        if (cfg.BuildTrimpath) { 
            // Keep in sync with Action.trimpath.
            // The trimmed paths are a little different, but we need to trim in the
            // same situations.
            @string from = default;            @string toPath = default;

            {
                var m = p.Module;

                if (m != null) {
                    from = m.Dir;
                    toPath = m.Path + "@" + m.Version;
                }
                else
 {
                    from = p.Dir;
                    toPath = p.ImportPath;
                } 
                // -fdebug-prefix-map requires an absolute "to" path (or it joins the path
                // with the working directory). Pick something that makes sense for the
                // target platform.

            } 
            // -fdebug-prefix-map requires an absolute "to" path (or it joins the path
            // with the working directory). Pick something that makes sense for the
            // target platform.
            @string to = default;
            if (cfg.BuildContext.GOOS == "windows") {
                to = filepath.Join("\\\\_\\_", toPath);
            }
            else
 {
                to = filepath.Join("/_", toPath);
            }
            flags = append(flags.slice(-1, len(flags), len(flags)), "-fdebug-prefix-map=" + from + "=" + to);
        }
        else if (p.Goroot && cfg.GOROOT_FINAL != cfg.GOROOT) {
            flags = append(flags.slice(-1, len(flags), len(flags)), "-fdebug-prefix-map=" + cfg.GOROOT + "=" + cfg.GOROOT_FINAL);
        }
    }
    var overlayPath = file;
    {
        var (p, ok) = a.nonGoOverlay[overlayPath];

        if (ok) {
            overlayPath = p;
        }
    }
    var (output, err) = b.runOut(a, filepath.Dir(overlayPath), b.cCompilerEnv(), compiler, flags, "-o", outfile, "-c", filepath.Base(overlayPath));
    if (len(output) > 0) { 
        // On FreeBSD 11, when we pass -g to clang 3.8 it
        // invokes its internal assembler with -dwarf-version=2.
        // When it sees .section .note.GNU-stack, it warns
        // "DWARF2 only supports one section per compilation unit".
        // This warning makes no sense, since the section is empty,
        // but it confuses people.
        // We work around the problem by detecting the warning
        // and dropping -g and trying again.
        if (bytes.Contains(output, (slice<byte>)"DWARF2 only supports one section per compilation unit")) {
            var newFlags = make_slice<@string>(0, len(flags));
            foreach (var (_, f) in flags) {
                if (!strings.HasPrefix(f, "-g")) {
                    newFlags = append(newFlags, f);
                }
            }
            if (len(newFlags) < len(flags)) {
                return error.As(b.ccompile(a, p, outfile, newFlags, file, compiler))!;
            }
        }
        b.showOutput(a, p.Dir, desc, b.processOutput(output));
        if (err != null) {
            err = errPrintedOutput;
        }
        else if (os.Getenv("GO_BUILDER_NAME") != "") {
            return error.As(errors.New("C compiler warning promoted to error on Go builders"))!;
        }
    }
    return error.As(err)!;
}

// gccld runs the gcc linker to create an executable from a set of object files.
private static error gccld(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string objdir, @string outfile, slice<@string> flags, slice<@string> objs) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    slice<@string> cmd = default;
    if (len(p.CXXFiles) > 0 || len(p.SwigCXXFiles) > 0) {
        cmd = b.GxxCmd(p.Dir, objdir);
    }
    else
 {
        cmd = b.GccCmd(p.Dir, objdir);
    }
    var dir = p.Dir;
    var (out, err) = b.runOut(a, @base.Cwd(), b.cCompilerEnv(), cmdargs);

    if (len(out) > 0) { 
        // Filter out useless linker warnings caused by bugs outside Go.
        // See also cmd/link/internal/ld's hostlink method.
        slice<slice<byte>> save = default;
        nint skipLines = default;
        foreach (var (_, line) in bytes.SplitAfter(out, (slice<byte>)"\n")) { 
            // golang.org/issue/26073 - Apple Xcode bug
            if (bytes.Contains(line, (slice<byte>)"ld: warning: text-based stub file")) {
                continue;
            }
            if (skipLines > 0) {
                skipLines--;
                continue;
            } 

            // Remove duplicate main symbol with runtime/cgo on AIX.
            // With runtime/cgo, two main are available:
            // One is generated by cgo tool with {return 0;}.
            // The other one is the main calling runtime.rt0_go
            // in runtime/cgo.
            // The second can't be used by cgo programs because
            // runtime.rt0_go is unknown to them.
            // Therefore, we let ld remove this main version
            // and used the cgo generated one.
            if (p.ImportPath == "runtime/cgo" && bytes.Contains(line, (slice<byte>)"ld: 0711-224 WARNING: Duplicate symbol: .main")) {
                skipLines = 1;
                continue;
            }
            save = append(save, line);
        }        out = bytes.Join(save, null);
        if (len(out) > 0) {
            b.showOutput(null, dir, p.ImportPath, b.processOutput(out));
            if (err != null) {
                err = errPrintedOutput;
            }
        }
    }
    return error.As(err)!;
}

// Grab these before main helpfully overwrites them.
private static var origCC = cfg.Getenv("CC");private static var origCXX = cfg.Getenv("CXX");

// gccCmd returns a gcc command line prefix
// defaultCC is defined in zdefaultcc.go, written by cmd/dist.
private static slice<@string> GccCmd(this ptr<Builder> _addr_b, @string incdir, @string workdir) {
    ref Builder b = ref _addr_b.val;

    return b.compilerCmd(b.ccExe(), incdir, workdir);
}

// gxxCmd returns a g++ command line prefix
// defaultCXX is defined in zdefaultcc.go, written by cmd/dist.
private static slice<@string> GxxCmd(this ptr<Builder> _addr_b, @string incdir, @string workdir) {
    ref Builder b = ref _addr_b.val;

    return b.compilerCmd(b.cxxExe(), incdir, workdir);
}

// gfortranCmd returns a gfortran command line prefix.
private static slice<@string> gfortranCmd(this ptr<Builder> _addr_b, @string incdir, @string workdir) {
    ref Builder b = ref _addr_b.val;

    return b.compilerCmd(b.fcExe(), incdir, workdir);
}

// ccExe returns the CC compiler setting without all the extra flags we add implicitly.
private static slice<@string> ccExe(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    return b.compilerExe(origCC, cfg.DefaultCC(cfg.Goos, cfg.Goarch));
}

// cxxExe returns the CXX compiler setting without all the extra flags we add implicitly.
private static slice<@string> cxxExe(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    return b.compilerExe(origCXX, cfg.DefaultCXX(cfg.Goos, cfg.Goarch));
}

// fcExe returns the FC compiler setting without all the extra flags we add implicitly.
private static slice<@string> fcExe(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    return b.compilerExe(cfg.Getenv("FC"), "gfortran");
}

// compilerExe returns the compiler to use given an
// environment variable setting (the value not the name)
// and a default. The resulting slice is usually just the name
// of the compiler but can have additional arguments if they
// were present in the environment value.
// For example if CC="gcc -DGOPHER" then the result is ["gcc", "-DGOPHER"].
private static slice<@string> compilerExe(this ptr<Builder> _addr_b, @string envValue, @string def) {
    ref Builder b = ref _addr_b.val;

    var compiler = strings.Fields(envValue);
    if (len(compiler) == 0) {
        compiler = strings.Fields(def);
    }
    return compiler;
}

// compilerCmd returns a command line prefix for the given environment
// variable and using the default command when the variable is empty.
private static slice<@string> compilerCmd(this ptr<Builder> _addr_b, slice<@string> compiler, @string incdir, @string workdir) {
    ref Builder b = ref _addr_b.val;
 
    // NOTE: env.go's mkEnv knows that the first three
    // strings returned are "gcc", "-I", incdir (and cuts them off).
    @string a = new slice<@string>(new @string[] { compiler[0], "-I", incdir });
    a = append(a, compiler[(int)1..]); 

    // Definitely want -fPIC but on Windows gcc complains
    // "-fPIC ignored for target (all code is position independent)"
    if (cfg.Goos != "windows") {
        a = append(a, "-fPIC");
    }
    a = append(a, b.gccArchArgs()); 
    // gcc-4.5 and beyond require explicit "-pthread" flag
    // for multithreading with pthread library.
    if (cfg.BuildContext.CgoEnabled) {
        switch (cfg.Goos) {
            case "windows": 
                a = append(a, "-mthreads");
                break;
            default: 
                a = append(a, "-pthread");
                break;
        }
    }
    if (cfg.Goos == "aix") { 
        // mcmodel=large must always be enabled to allow large TOC.
        a = append(a, "-mcmodel=large");
    }
    if (b.gccSupportsFlag(compiler, "-fno-caret-diagnostics")) {
        a = append(a, "-fno-caret-diagnostics");
    }
    if (b.gccSupportsFlag(compiler, "-Qunused-arguments")) {
        a = append(a, "-Qunused-arguments");
    }
    a = append(a, "-fmessage-length=0"); 

    // Tell gcc not to include the work directory in object files.
    if (b.gccSupportsFlag(compiler, "-fdebug-prefix-map=a=b")) {
        if (workdir == "") {
            workdir = b.WorkDir;
        }
        workdir = strings.TrimSuffix(workdir, string(filepath.Separator));
        a = append(a, "-fdebug-prefix-map=" + workdir + "=/tmp/go-build");
    }
    if (b.gccSupportsFlag(compiler, "-gno-record-gcc-switches")) {
        a = append(a, "-gno-record-gcc-switches");
    }
    if (cfg.Goos == "darwin" || cfg.Goos == "ios") {
        a = append(a, "-fno-common");
    }
    return a;
}

// gccNoPie returns the flag to use to request non-PIE. On systems
// with PIE (position independent executables) enabled by default,
// -no-pie must be passed when doing a partial link with -Wl,-r.
// But -no-pie is not supported by all compilers, and clang spells it -nopie.
private static @string gccNoPie(this ptr<Builder> _addr_b, slice<@string> linker) {
    ref Builder b = ref _addr_b.val;

    if (b.gccSupportsFlag(linker, "-no-pie")) {
        return "-no-pie";
    }
    if (b.gccSupportsFlag(linker, "-nopie")) {
        return "-nopie";
    }
    return "";
}

// gccSupportsFlag checks to see if the compiler supports a flag.
private static bool gccSupportsFlag(this ptr<Builder> _addr_b, slice<@string> compiler, @string flag) => func((defer, _, _) => {
    ref Builder b = ref _addr_b.val;

    array<@string> key = new array<@string>(new @string[] { compiler[0], flag });

    b.exec.Lock();
    defer(b.exec.Unlock());
    {
        var (b, ok) = b.flagCache[key];

        if (ok) {
            return b;
        }
    }
    if (b.flagCache == null) {
        b.flagCache = make_map<array<@string>, bool>();
    }
    var tmp = os.DevNull;
    if (runtime.GOOS == "windows") {
        var (f, err) = os.CreateTemp(b.WorkDir, "");
        if (err != null) {
            return false;
        }
        f.Close();
        tmp = f.Name();
        defer(os.Remove(tmp));
    }
    var cmdArgs = str.StringList(compiler, flag, "-c", "-x", "c", "-", "-o", tmp);
    if (cfg.BuildN || cfg.BuildX) {
        b.Showcmd(b.WorkDir, "%s || true", joinUnambiguously(cmdArgs));
        if (cfg.BuildN) {
            return false;
        }
    }
    var cmd = exec.Command(cmdArgs[0], cmdArgs[(int)1..]);
    cmd.Dir = b.WorkDir;
    cmd.Env = @base.AppendPWD(os.Environ(), cmd.Dir);
    cmd.Env = append(cmd.Env, "LC_ALL=C");
    var (out, _) = cmd.CombinedOutput(); 
    // GCC says "unrecognized command line option".
    // clang says "unknown argument".
    // Older versions of GCC say "unrecognised debug output level".
    // For -fsplit-stack GCC says "'-fsplit-stack' is not supported".
    var supported = !bytes.Contains(out, (slice<byte>)"unrecognized") && !bytes.Contains(out, (slice<byte>)"unknown") && !bytes.Contains(out, (slice<byte>)"unrecognised") && !bytes.Contains(out, (slice<byte>)"is not supported");
    b.flagCache[key] = supported;
    return supported;
});

// gccArchArgs returns arguments to pass to gcc based on the architecture.
private static slice<@string> gccArchArgs(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    switch (cfg.Goarch) {
        case "386": 
            return new slice<@string>(new @string[] { "-m32" });
            break;
        case "amd64": 
            if (cfg.Goos == "darwin") {
                return new slice<@string>(new @string[] { "-arch", "x86_64", "-m64" });
            }
            return new slice<@string>(new @string[] { "-m64" });
            break;
        case "arm64": 
            if (cfg.Goos == "darwin") {
                return new slice<@string>(new @string[] { "-arch", "arm64" });
            }
            break;
        case "arm": 
            return new slice<@string>(new @string[] { "-marm" }); // not thumb
            break;
        case "s390x": 
            return new slice<@string>(new @string[] { "-m64", "-march=z196" });
            break;
        case "mips64": 

        case "mips64le": 
            @string args = new slice<@string>(new @string[] { "-mabi=64" });
            if (cfg.GOMIPS64 == "hardfloat") {
                return append(args, "-mhard-float");
            }
            else if (cfg.GOMIPS64 == "softfloat") {
                return append(args, "-msoft-float");
            }
            break;
        case "mips": 

        case "mipsle": 
            args = new slice<@string>(new @string[] { "-mabi=32", "-march=mips32" });
            if (cfg.GOMIPS == "hardfloat") {
                return append(args, "-mhard-float", "-mfp32", "-mno-odd-spreg");
            }
            else if (cfg.GOMIPS == "softfloat") {
                return append(args, "-msoft-float");
            }
            break;
        case "ppc64": 
            if (cfg.Goos == "aix") {
                return new slice<@string>(new @string[] { "-maix64" });
            }
            break;
    }
    return null;
}

// envList returns the value of the given environment variable broken
// into fields, using the default value when the variable is empty.
private static slice<@string> envList(@string key, @string def) {
    var v = cfg.Getenv(key);
    if (v == "") {
        v = def;
    }
    return strings.Fields(v);
}

// CFlags returns the flags to use when invoking the C, C++ or Fortran compilers, or cgo.
private static (slice<@string>, slice<@string>, slice<@string>, slice<@string>, slice<@string>, error) CFlags(this ptr<Builder> _addr_b, ptr<load.Package> _addr_p) {
    slice<@string> cppflags = default;
    slice<@string> cflags = default;
    slice<@string> cxxflags = default;
    slice<@string> fflags = default;
    slice<@string> ldflags = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref load.Package p = ref _addr_p.val;

    @string defaults = "-g -O2";

    cppflags, err = buildFlags("CPPFLAGS", "", p.CgoCPPFLAGS, checkCompilerFlags);

    if (err != null) {
        return ;
    }
    cflags, err = buildFlags("CFLAGS", defaults, p.CgoCFLAGS, checkCompilerFlags);

    if (err != null) {
        return ;
    }
    cxxflags, err = buildFlags("CXXFLAGS", defaults, p.CgoCXXFLAGS, checkCompilerFlags);

    if (err != null) {
        return ;
    }
    fflags, err = buildFlags("FFLAGS", defaults, p.CgoFFLAGS, checkCompilerFlags);

    if (err != null) {
        return ;
    }
    ldflags, err = buildFlags("LDFLAGS", defaults, p.CgoLDFLAGS, checkLinkerFlags);

    if (err != null) {
        return ;
    }
    return ;
}

private static (slice<@string>, error) buildFlags(@string name, @string defaults, slice<@string> fromPackage, Func<@string, @string, slice<@string>, error> check) {
    slice<@string> _p0 = default;
    error _p0 = default!;

    {
        var err = check(name, "#cgo " + name, fromPackage);

        if (err != null) {
            return (null, error.As(err)!);
        }
    }
    return (str.StringList(envList("CGO_" + name, defaults), fromPackage), error.As(null!)!);
}

private static var cgoRe = lazyregexp.New("[/\\\\:]");

private static (slice<@string>, slice<@string>, error) cgo(this ptr<Builder> _addr_b, ptr<Action> _addr_a, @string cgoExe, @string objdir, slice<@string> pcCFLAGS, slice<@string> pcLDFLAGS, slice<@string> cgofiles, slice<@string> gccfiles, slice<@string> gxxfiles, slice<@string> mfiles, slice<@string> ffiles) {
    slice<@string> outGo = default;
    slice<@string> outObj = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;

    var p = a.Package;
    var (cgoCPPFLAGS, cgoCFLAGS, cgoCXXFLAGS, cgoFFLAGS, cgoLDFLAGS, err) = b.CFlags(p);
    if (err != null) {
        return (null, null, error.As(err)!);
    }
    cgoCPPFLAGS = append(cgoCPPFLAGS, pcCFLAGS);
    cgoLDFLAGS = append(cgoLDFLAGS, pcLDFLAGS); 
    // If we are compiling Objective-C code, then we need to link against libobjc
    if (len(mfiles) > 0) {
        cgoLDFLAGS = append(cgoLDFLAGS, "-lobjc");
    }
    if (len(ffiles) > 0) {
        var fc = cfg.Getenv("FC");
        if (fc == "") {
            fc = "gfortran";
        }
        if (strings.Contains(fc, "gfortran")) {
            cgoLDFLAGS = append(cgoLDFLAGS, "-lgfortran");
        }
    }
    if (cfg.BuildMSan) {
        cgoCFLAGS = append(new slice<@string>(new @string[] { "-fsanitize=memory" }), cgoCFLAGS);
        cgoLDFLAGS = append(new slice<@string>(new @string[] { "-fsanitize=memory" }), cgoLDFLAGS);
    }
    cgoCPPFLAGS = append(cgoCPPFLAGS, "-I", objdir); 

    // cgo
    // TODO: CGO_FLAGS?
    @string gofiles = new slice<@string>(new @string[] { objdir+"_cgo_gotypes.go" });
    @string cfiles = new slice<@string>(new @string[] { "_cgo_export.c" });
    foreach (var (_, fn) in cgofiles) {
        var f = strings.TrimSuffix(filepath.Base(fn), ".go");
        gofiles = append(gofiles, objdir + f + ".cgo1.go");
        cfiles = append(cfiles, f + ".cgo2.c");
    }    @string cgoflags = new slice<@string>(new @string[] {  });
    if (p.Standard && p.ImportPath == "runtime/cgo") {
        cgoflags = append(cgoflags, "-import_runtime_cgo=false");
    }
    if (p.Standard && (p.ImportPath == "runtime/race" || p.ImportPath == "runtime/msan" || p.ImportPath == "runtime/cgo")) {
        cgoflags = append(cgoflags, "-import_syscall=false");
    }
    var cgoenv = b.cCompilerEnv();
    if (len(cgoLDFLAGS) > 0) {
        var flags = make_slice<@string>(len(cgoLDFLAGS));
        {
            var i__prev1 = i;
            var f__prev1 = f;

            foreach (var (__i, __f) in cgoLDFLAGS) {
                i = __i;
                f = __f;
                flags[i] = strconv.Quote(f);
            }

            i = i__prev1;
            f = f__prev1;
        }

        cgoenv = append(cgoenv, "CGO_LDFLAGS=" + strings.Join(flags, " "));
    }
    if (cfg.BuildToolchainName == "gccgo") {
        if (b.gccSupportsFlag(new slice<@string>(new @string[] { BuildToolchain.compiler() }), "-fsplit-stack")) {
            cgoCFLAGS = append(cgoCFLAGS, "-fsplit-stack");
        }
        cgoflags = append(cgoflags, "-gccgo");
        {
            var pkgpath = gccgoPkgpath(p);

            if (pkgpath != "") {
                cgoflags = append(cgoflags, "-gccgopkgpath=" + pkgpath);
            }

        }
    }
    switch (cfg.BuildBuildmode) {
        case "c-archive": 
            // Tell cgo that if there are any exported functions
            // it should generate a header file that C code can
            // #include.

        case "c-shared": 
            // Tell cgo that if there are any exported functions
            // it should generate a header file that C code can
            // #include.
            cgoflags = append(cgoflags, "-exportheader=" + objdir + "_cgo_install.h");
            break;
    }

    var execdir = p.Dir; 

    // Rewrite overlaid paths in cgo files.
    // cgo adds //line and #line pragmas in generated files with these paths.
    slice<@string> trimpath = default;
    {
        var i__prev1 = i;

        foreach (var (__i) in cgofiles) {
            i = __i;
            var path = mkAbs(p.Dir, cgofiles[i]);
            {
                var (opath, ok) = fsys.OverlayPath(path);

                if (ok) {
                    cgofiles[i] = opath;
                    trimpath = append(trimpath, opath + "=>" + path);
                }

            }
        }
        i = i__prev1;
    }

    if (len(trimpath) > 0) {
        cgoflags = append(cgoflags, "-trimpath", strings.Join(trimpath, ";"));
    }
    {
        var err__prev1 = err;

        var err = b.run(a, execdir, p.ImportPath, cgoenv, cfg.BuildToolexec, cgoExe, "-objdir", objdir, "-importpath", p.ImportPath, cgoflags, "--", cgoCPPFLAGS, cgoCFLAGS, cgofiles);

        if (err != null) {
            return (null, null, error.As(err)!);
        }
        err = err__prev1;

    }
    outGo = append(outGo, gofiles); 

    // Use sequential object file names to keep them distinct
    // and short enough to fit in the .a header file name slots.
    // We no longer collect them all into _all.o, and we'd like
    // tools to see both the .o suffix and unique names, so
    // we need to make them short enough not to be truncated
    // in the final archive.
    nint oseq = 0;
    Func<@string> nextOfile = () => {
        oseq++;
        return objdir + fmt.Sprintf("_x%03d.o", oseq);
    }; 

    // gcc
    var cflags = str.StringList(cgoCPPFLAGS, cgoCFLAGS);
    foreach (var (_, cfile) in cfiles) {
        var ofile = nextOfile();
        {
            var err__prev1 = err;

            err = b.gcc(a, p, a.Objdir, ofile, cflags, objdir + cfile);

            if (err != null) {
                return (null, null, error.As(err)!);
            }

            err = err__prev1;

        }
        outObj = append(outObj, ofile);
    }    {
        var file__prev1 = file;

        foreach (var (_, __file) in gccfiles) {
            file = __file;
            ofile = nextOfile();
            {
                var err__prev1 = err;

                err = b.gcc(a, p, a.Objdir, ofile, cflags, file);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outObj = append(outObj, ofile);
        }
        file = file__prev1;
    }

    var cxxflags = str.StringList(cgoCPPFLAGS, cgoCXXFLAGS);
    {
        var file__prev1 = file;

        foreach (var (_, __file) in gxxfiles) {
            file = __file;
            ofile = nextOfile();
            {
                var err__prev1 = err;

                err = b.gxx(a, p, a.Objdir, ofile, cxxflags, file);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outObj = append(outObj, ofile);
        }
        file = file__prev1;
    }

    {
        var file__prev1 = file;

        foreach (var (_, __file) in mfiles) {
            file = __file;
            ofile = nextOfile();
            {
                var err__prev1 = err;

                err = b.gcc(a, p, a.Objdir, ofile, cflags, file);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outObj = append(outObj, ofile);
        }
        file = file__prev1;
    }

    var fflags = str.StringList(cgoCPPFLAGS, cgoFFLAGS);
    {
        var file__prev1 = file;

        foreach (var (_, __file) in ffiles) {
            file = __file;
            ofile = nextOfile();
            {
                var err__prev1 = err;

                err = b.gfortran(a, p, a.Objdir, ofile, fflags, file);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outObj = append(outObj, ofile);
        }
        file = file__prev1;
    }

    switch (cfg.BuildToolchainName) {
        case "gc": 
            var importGo = objdir + "_cgo_import.go";
            {
                var err__prev1 = err;

                err = b.dynimport(a, p, objdir, importGo, cgoExe, cflags, cgoLDFLAGS, outObj);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outGo = append(outGo, importGo);
            break;
        case "gccgo": 
            var defunC = objdir + "_cgo_defun.c";
            var defunObj = objdir + "_cgo_defun.o";
            {
                var err__prev1 = err;

                err = BuildToolchain.cc(b, a, defunObj, defunC);

                if (err != null) {
                    return (null, null, error.As(err)!);
                }

                err = err__prev1;

            }
            outObj = append(outObj, defunObj);
            break;
        default: 
            noCompiler();
            break;
    } 

    // Double check the //go:cgo_ldflag comments in the generated files.
    // The compiler only permits such comments in files whose base name
    // starts with "_cgo_". Make sure that the comments in those files
    // are safe. This is a backstop against people somehow smuggling
    // such a comment into a file generated by cgo.
    if (cfg.BuildToolchainName == "gc" && !cfg.BuildN) {
        flags = default;
        {
            var f__prev1 = f;

            foreach (var (_, __f) in outGo) {
                f = __f;
                if (!strings.HasPrefix(filepath.Base(f), "_cgo_")) {
                    continue;
                }
                var (src, err) = os.ReadFile(f);
                if (err != null) {
                    return (null, null, error.As(err)!);
                }
                const @string cgoLdflag = "//go:cgo_ldflag";

                var idx = bytes.Index(src, (slice<byte>)cgoLdflag);
                while (idx >= 0) { 
                    // We are looking at //go:cgo_ldflag.
                    // Find start of line.
                    var start = bytes.LastIndex(src[..(int)idx], (slice<byte>)"\n");
                    if (start == -1) {
                        start = 0;
                    } 

                    // Find end of line.
                    var end = bytes.Index(src[(int)idx..], (slice<byte>)"\n");
                    if (end == -1) {
                        end = len(src);
                    }
                    else
 {
                        end += idx;
                    } 

                    // Check for first line comment in line.
                    // We don't worry about /* */ comments,
                    // which normally won't appear in files
                    // generated by cgo.
                    var commentStart = bytes.Index(src[(int)start..], (slice<byte>)"//");
                    commentStart += start; 
                    // If that line comment is //go:cgo_ldflag,
                    // it's a match.
                    if (bytes.HasPrefix(src[(int)commentStart..], (slice<byte>)cgoLdflag)) { 
                        // Pull out the flag, and unquote it.
                        // This is what the compiler does.
                        var flag = string(src[(int)idx + len(cgoLdflag)..(int)end]);
                        flag = strings.TrimSpace(flag);
                        flag = strings.Trim(flag, "\"");
                        flags = append(flags, flag);
                    }
                    src = src[(int)end..];
                    idx = bytes.Index(src, (slice<byte>)cgoLdflag);
                }
            } 

            // We expect to find the contents of cgoLDFLAGS in flags.

            f = f__prev1;
        }

        if (len(cgoLDFLAGS) > 0) {
outer:
            {
                var i__prev1 = i;

                foreach (var (__i) in flags) {
                    i = __i;
                    {
                        var f__prev2 = f;

                        foreach (var (__j, __f) in cgoLDFLAGS) {
                            j = __j;
                            f = __f;
                            if (f != flags[i + j]) {
                                _continueouter = true;
                                break;
                            }
                        }

                        f = f__prev2;
                    }

                    flags = append(flags[..(int)i], flags[(int)i + len(cgoLDFLAGS)..]);
                    break;
                }

                i = i__prev1;
            }
        }
        {
            var err__prev2 = err;

            err = checkLinkerFlags("LDFLAGS", "go:cgo_ldflag", flags);

            if (err != null) {
                return (null, null, error.As(err)!);
            }

            err = err__prev2;

        }
    }
    return (outGo, outObj, error.As(null!)!);
}

// dynimport creates a Go source file named importGo containing
// //go:cgo_import_dynamic directives for each symbol or library
// dynamically imported by the object files outObj.
private static error dynimport(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string objdir, @string importGo, @string cgoExe, slice<@string> cflags, slice<@string> cgoLDFLAGS, slice<@string> outObj) {
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    var cfile = objdir + "_cgo_main.c";
    var ofile = objdir + "_cgo_main.o";
    {
        var err__prev1 = err;

        var err = b.gcc(a, p, objdir, ofile, cflags, cfile);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }

    var linkobj = str.StringList(ofile, outObj, mkAbsFiles(p.Dir, p.SysoFiles));
    var dynobj = objdir + "_cgo_.o"; 

    // we need to use -pie for Linux/ARM to get accurate imported sym
    var ldflags = cgoLDFLAGS;
    if ((cfg.Goarch == "arm" && cfg.Goos == "linux") || cfg.Goos == "android") { 
        // -static -pie doesn't make sense, and causes link errors.
        // Issue 26197.
        var n = make_slice<@string>(0, len(ldflags));
        foreach (var (_, flag) in ldflags) {
            if (flag != "-static") {
                n = append(n, flag);
            }
        }        ldflags = append(n, "-pie");
    }
    {
        var err__prev1 = err;

        err = b.gccld(a, p, objdir, dynobj, ldflags, linkobj);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    } 

    // cgo -dynimport
    slice<@string> cgoflags = default;
    if (p.Standard && p.ImportPath == "runtime/cgo") {
        cgoflags = new slice<@string>(new @string[] { "-dynlinker" }); // record path to dynamic linker
    }
    return error.As(b.run(a, @base.Cwd(), p.ImportPath, b.cCompilerEnv(), cfg.BuildToolexec, cgoExe, "-dynpackage", p.Name, "-dynimport", dynobj, "-dynout", importGo, cgoflags))!;
}

// Run SWIG on all SWIG input files.
// TODO: Don't build a shared library, once SWIG emits the necessary
// pragmas for external linking.
private static (slice<@string>, slice<@string>, slice<@string>, error) swig(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string objdir, slice<@string> pcCFLAGS) {
    slice<@string> outGo = default;
    slice<@string> outC = default;
    slice<@string> outCXX = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    {
        var err = b.swigVersionCheck();

        if (err != null) {
            return (null, null, null, error.As(err)!);
        }
    }

    var (intgosize, err) = b.swigIntSize(objdir);
    if (err != null) {
        return (null, null, null, error.As(err)!);
    }
    {
        var f__prev1 = f;

        foreach (var (_, __f) in p.SwigFiles) {
            f = __f;
            var (goFile, cFile, err) = b.swigOne(a, p, f, objdir, pcCFLAGS, false, intgosize);
            if (err != null) {
                return (null, null, null, error.As(err)!);
            }
            if (goFile != "") {
                outGo = append(outGo, goFile);
            }
            if (cFile != "") {
                outC = append(outC, cFile);
            }
        }
        f = f__prev1;
    }

    {
        var f__prev1 = f;

        foreach (var (_, __f) in p.SwigCXXFiles) {
            f = __f;
            var (goFile, cxxFile, err) = b.swigOne(a, p, f, objdir, pcCFLAGS, true, intgosize);
            if (err != null) {
                return (null, null, null, error.As(err)!);
            }
            if (goFile != "") {
                outGo = append(outGo, goFile);
            }
            if (cxxFile != "") {
                outCXX = append(outCXX, cxxFile);
            }
        }
        f = f__prev1;
    }

    return (outGo, outC, outCXX, error.As(null!)!);
}

// Make sure SWIG is new enough.
private static sync.Once swigCheckOnce = default;private static error swigCheck = default!;

private static error swigDoVersionCheck(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    var (out, err) = b.runOut(null, "", null, "swig", "-version");
    if (err != null) {
        return error.As(err)!;
    }
    var re = regexp.MustCompile("[vV]ersion +([\\d]+)([.][\\d]+)?([.][\\d]+)?");
    var matches = re.FindSubmatch(out);
    if (matches == null) { 
        // Can't find version number; hope for the best.
        return error.As(null!)!;
    }
    var (major, err) = strconv.Atoi(string(matches[1]));
    if (err != null) { 
        // Can't find version number; hope for the best.
        return error.As(null!)!;
    }
    const @string errmsg = "must have SWIG version >= 3.0.6";

    if (major < 3) {
        return error.As(errors.New(errmsg))!;
    }
    if (major > 3) { 
        // 4.0 or later
        return error.As(null!)!;
    }
    if (len(matches[2]) > 0) {
        var (minor, err) = strconv.Atoi(string(matches[2][(int)1..]));
        if (err != null) {
            return error.As(null!)!;
        }
        if (minor > 0) { 
            // 3.1 or later
            return error.As(null!)!;
        }
    }
    if (len(matches[3]) > 0) {
        var (patch, err) = strconv.Atoi(string(matches[3][(int)1..]));
        if (err != null) {
            return error.As(null!)!;
        }
        if (patch < 6) { 
            // Before 3.0.6.
            return error.As(errors.New(errmsg))!;
        }
    }
    return error.As(null!)!;
}

private static error swigVersionCheck(this ptr<Builder> _addr_b) {
    ref Builder b = ref _addr_b.val;

    swigCheckOnce.Do(() => {
        swigCheck = b.swigDoVersionCheck();
    });
    return error.As(swigCheck)!;
}

// Find the value to pass for the -intgosize option to swig.
private static sync.Once swigIntSizeOnce = default;private static @string swigIntSize = default;private static error swigIntSizeError = default!;

// This code fails to build if sizeof(int) <= 32
private static readonly @string swigIntSizeCode = "\npackage main\nconst i int = 1 << 32\n";

// Determine the size of int on the target system for the -intgosize option
// of swig >= 2.0.9. Run only once.


// Determine the size of int on the target system for the -intgosize option
// of swig >= 2.0.9. Run only once.
private static (@string, error) swigDoIntSize(this ptr<Builder> _addr_b, @string objdir) {
    @string intsize = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;

    if (cfg.BuildN) {
        return ("$INTBITS", error.As(null!)!);
    }
    var src = filepath.Join(b.WorkDir, "swig_intsize.go");
    err = os.WriteFile(src, (slice<byte>)swigIntSizeCode, 0666);

    if (err != null) {
        return ;
    }
    @string srcs = new slice<@string>(new @string[] { src });

    var p = load.GoFilesPackage(context.TODO(), new load.PackageOpts(), srcs);

    {
        var (_, _, e) = BuildToolchain.gc(b, addr(new Action(Mode:"swigDoIntSize",Package:p,Objdir:objdir)), "", null, null, "", false, srcs);

        if (e != null) {
            return ("32", error.As(null!)!);
        }
    }
    return ("64", error.As(null!)!);
}

// Determine the size of int on the target system for the -intgosize option
// of swig >= 2.0.9.
private static (@string, error) swigIntSize(this ptr<Builder> _addr_b, @string objdir) {
    @string intsize = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;

    swigIntSizeOnce.Do(() => {
        swigIntSize, swigIntSizeError = b.swigDoIntSize(objdir);
    });
    return (swigIntSize, error.As(swigIntSizeError)!);
}

// Run SWIG on one SWIG input file.
private static (@string, @string, error) swigOne(this ptr<Builder> _addr_b, ptr<Action> _addr_a, ptr<load.Package> _addr_p, @string file, @string objdir, slice<@string> pcCFLAGS, bool cxx, @string intgosize) {
    @string outGo = default;
    @string outC = default;
    error err = default!;
    ref Builder b = ref _addr_b.val;
    ref Action a = ref _addr_a.val;
    ref load.Package p = ref _addr_p.val;

    var (cgoCPPFLAGS, cgoCFLAGS, cgoCXXFLAGS, _, _, err) = b.CFlags(p);
    if (err != null) {
        return ("", "", error.As(err)!);
    }
    slice<@string> cflags = default;
    if (cxx) {
        cflags = str.StringList(cgoCPPFLAGS, pcCFLAGS, cgoCXXFLAGS);
    }
    else
 {
        cflags = str.StringList(cgoCPPFLAGS, pcCFLAGS, cgoCFLAGS);
    }
    nint n = 5; // length of ".swig"
    if (cxx) {
        n = 8; // length of ".swigcxx"
    }
    var @base = file[..(int)len(file) - n];
    var goFile = base + ".go";
    var gccBase = base + "_wrap.";
    @string gccExt = "c";
    if (cxx) {
        gccExt = "cxx";
    }
    var gccgo = cfg.BuildToolchainName == "gccgo"; 

    // swig
    @string args = new slice<@string>(new @string[] { "-go", "-cgo", "-intgosize", intgosize, "-module", base, "-o", objdir+gccBase+gccExt, "-outdir", objdir });

    foreach (var (_, f) in cflags) {
        if (len(f) > 3 && f[..(int)2] == "-I") {
            args = append(args, f);
        }
    }    if (gccgo) {
        args = append(args, "-gccgo");
        {
            var pkgpath = gccgoPkgpath(p);

            if (pkgpath != "") {
                args = append(args, "-go-pkgpath", pkgpath);
            }

        }
    }
    if (cxx) {
        args = append(args, "-c++");
    }
    var (out, err) = b.runOut(a, p.Dir, null, "swig", args, file);
    if (err != null) {
        if (len(out) > 0) {
            if (bytes.Contains(out, (slice<byte>)"-intgosize") || bytes.Contains(out, (slice<byte>)"-cgo")) {
                return ("", "", error.As(errors.New("must have SWIG version >= 3.0.6"))!);
            }
            b.showOutput(a, p.Dir, p.Desc(), b.processOutput(out)); // swig error
            return ("", "", error.As(errPrintedOutput)!);
        }
        return ("", "", error.As(err)!);
    }
    if (len(out) > 0) {
        b.showOutput(a, p.Dir, p.Desc(), b.processOutput(out)); // swig warning
    }
    goFile = objdir + goFile;
    var newGoFile = objdir + "_" + base + "_swig.go";
    {
        var err = os.Rename(goFile, newGoFile);

        if (err != null) {
            return ("", "", error.As(err)!);
        }
    }
    return (newGoFile, objdir + gccBase + gccExt, error.As(null!)!);
}

// disableBuildID adjusts a linker command line to avoid creating a
// build ID when creating an object file rather than an executable or
// shared library. Some systems, such as Ubuntu, always add
// --build-id to every link, but we don't want a build ID when we are
// producing an object file. On some of those system a plain -r (not
// -Wl,-r) will turn off --build-id, but clang 3.0 doesn't support a
// plain -r. I don't know how to turn off --build-id when using clang
// other than passing a trailing --build-id=none. So that is what we
// do, but only on systems likely to support it, which is to say,
// systems that normally use gold or the GNU linker.
private static slice<@string> disableBuildID(this ptr<Builder> _addr_b, slice<@string> ldflags) {
    ref Builder b = ref _addr_b.val;

    switch (cfg.Goos) {
        case "android": 

        case "dragonfly": 

        case "linux": 

        case "netbsd": 
            ldflags = append(ldflags, "-Wl,--build-id=none");
            break;
    }
    return ldflags;
}

// mkAbsFiles converts files into a list of absolute files,
// assuming they were originally relative to dir,
// and returns that new list.
private static slice<@string> mkAbsFiles(@string dir, slice<@string> files) {
    var abs = make_slice<@string>(len(files));
    foreach (var (i, f) in files) {
        if (!filepath.IsAbs(f)) {
            f = filepath.Join(dir, f);
        }
        abs[i] = f;
    }    return abs;
}

// passLongArgsInResponseFiles modifies cmd such that, for
// certain programs, long arguments are passed in "response files", a
// file on disk with the arguments, with one arg per line. An actual
// argument starting with '@' means that the rest of the argument is
// a filename of arguments to expand.
//
// See issues 18468 (Windows) and 37768 (Darwin).
private static Action passLongArgsInResponseFiles(ptr<exec.Cmd> _addr_cmd) {
    Action cleanup = default;
    ref exec.Cmd cmd = ref _addr_cmd.val;

    cleanup = () => {
    }; // no cleanup by default

    nint argLen = default;
    {
        var arg__prev1 = arg;

        foreach (var (_, __arg) in cmd.Args) {
            arg = __arg;
            argLen += len(arg);
        }
        arg = arg__prev1;
    }

    if (!useResponseFile(cmd.Path, argLen)) {
        return ;
    }
    var (tf, err) = os.CreateTemp("", "args");
    if (err != null) {
        log.Fatalf("error writing long arguments to response file: %v", err);
    }
    cleanup = () => {
        os.Remove(tf.Name());
    };
    ref bytes.Buffer buf = ref heap(out ptr<bytes.Buffer> _addr_buf);
    {
        var arg__prev1 = arg;

        foreach (var (_, __arg) in cmd.Args[(int)1..]) {
            arg = __arg;
            fmt.Fprintf(_addr_buf, "%s\n", encodeArg(arg));
        }
        arg = arg__prev1;
    }

    {
        var (_, err) = tf.Write(buf.Bytes());

        if (err != null) {
            tf.Close();
            cleanup();
            log.Fatalf("error writing long arguments to response file: %v", err);
        }
    }
    {
        var err = tf.Close();

        if (err != null) {
            cleanup();
            log.Fatalf("error writing long arguments to response file: %v", err);
        }
    }
    cmd.Args = new slice<@string>(new @string[] { cmd.Args[0], "@"+tf.Name() });
    return cleanup;
}

// Windows has a limit of 32 KB arguments. To be conservative and not worry
// about whether that includes spaces or not, just use 30 KB. Darwin's limit is
// less clear. The OS claims 256KB, but we've seen failures with arglen as
// small as 50KB.
public static readonly nint ArgLengthForResponseFile = (30 << 10);



private static bool useResponseFile(@string path, nint argLen) { 
    // Unless the program uses objabi.Flagparse, which understands
    // response files, don't use response files.
    // TODO: do we need more commands? asm? cgo? For now, no.
    var prog = strings.TrimSuffix(filepath.Base(path), ".exe");
    switch (prog) {
        case "compile": 

        case "link": 

            break;
        default: 
            return false;
            break;
    }

    if (argLen > ArgLengthForResponseFile) {
        return true;
    }
    var isBuilder = os.Getenv("GO_BUILDER_NAME") != "";
    if (isBuilder && rand.Intn(10) == 0) {
        return true;
    }
    return false;
}

// encodeArg encodes an argument for response file writing.
private static @string encodeArg(@string arg) { 
    // If there aren't any characters we need to reencode, fastpath out.
    if (!strings.ContainsAny(arg, "\\\n")) {
        return arg;
    }
    strings.Builder b = default;
    foreach (var (_, r) in arg) {
        switch (r) {
            case '\\': 
                b.WriteByte('\\');
                b.WriteByte('\\');
                break;
            case '\n': 
                b.WriteByte('\\');
                b.WriteByte('n');
                break;
            default: 
                b.WriteRune(r);
                break;
        }
    }    return b.String();
}

} // end work_package
