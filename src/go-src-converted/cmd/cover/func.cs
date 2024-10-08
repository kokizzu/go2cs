// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// This file implements the visitor that computes the (line, column)-(line-column) range for each function.

// package main -- go2cs converted at 2022 March 13 06:28:38 UTC
// Original source: C:\Program Files\Go\src\cmd\cover\func.go
namespace go;

using bufio = bufio_package;
using bytes = bytes_package;
using json = encoding.json_package;
using errors = errors_package;
using fmt = fmt_package;
using ast = go.ast_package;
using parser = go.parser_package;
using token = go.token_package;
using exec = @internal.execabs_package;
using io = io_package;
using os = os_package;
using path = path_package;
using filepath = path.filepath_package;
using runtime = runtime_package;
using strings = strings_package;
using tabwriter = text.tabwriter_package;

using cover = golang.org.x.tools.cover_package;


// funcOutput takes two file names as arguments, a coverage profile to read as input and an output
// file to write ("" means to write to standard output). The function reads the profile and produces
// as output the coverage data broken down by function, like this:
//
//    fmt/format.go:30:    init            100.0%
//    fmt/format.go:57:    clearflags        100.0%
//    ...
//    fmt/scan.go:1046:    doScan            100.0%
//    fmt/scan.go:1075:    advance            96.2%
//    fmt/scan.go:1119:    doScanf            96.8%
//    total:        (statements)            91.9%

public static partial class main_package {

private static error funcOutput(@string profile, @string outputFile) => func((defer, _, _) => {
    var (profiles, err) = cover.ParseProfiles(profile);
    if (err != null) {
        return error.As(err)!;
    }
    var (dirs, err) = findPkgs(profiles);
    if (err != null) {
        return error.As(err)!;
    }
    ptr<bufio.Writer> @out;
    if (outputFile == "") {
        out = bufio.NewWriter(os.Stdout);
    }
    else
 {
        var (fd, err) = os.Create(outputFile);
        if (err != null) {
            return error.As(err)!;
        }
        defer(fd.Close());
        out = bufio.NewWriter(fd);
    }
    defer(@out.Flush());

    var tabber = tabwriter.NewWriter(out, 1, 8, 1, '\t', 0);
    defer(tabber.Flush());

    long total = default;    long covered = default;

    foreach (var (_, profile) in profiles) {
        var fn = profile.FileName;
        var (file, err) = findFile(dirs, fn);
        if (err != null) {
            return error.As(err)!;
        }
        var (funcs, err) = findFuncs(file);
        if (err != null) {
            return error.As(err)!;
        }
        foreach (var (_, f) in funcs) {
            var (c, t) = f.coverage(profile);
            fmt.Fprintf(tabber, "%s:%d:\t%s\t%.1f%%\n", fn, f.startLine, f.name, percent(c, t));
            total += t;
            covered += c;
        }
    }    fmt.Fprintf(tabber, "total:\t(statements)\t%.1f%%\n", percent(covered, total));

    return error.As(null!)!;
});

// findFuncs parses the file and returns a slice of FuncExtent descriptors.
private static (slice<ptr<FuncExtent>>, error) findFuncs(@string name) {
    slice<ptr<FuncExtent>> _p0 = default;
    error _p0 = default!;

    var fset = token.NewFileSet();
    var (parsedFile, err) = parser.ParseFile(fset, name, null, 0);
    if (err != null) {
        return (null, error.As(err)!);
    }
    ptr<FuncVisitor> visitor = addr(new FuncVisitor(fset:fset,name:name,astFile:parsedFile,));
    ast.Walk(visitor, visitor.astFile);
    return (visitor.funcs, error.As(null!)!);
}

// FuncExtent describes a function's extent in the source by file and position.
public partial struct FuncExtent {
    public @string name;
    public nint startLine;
    public nint startCol;
    public nint endLine;
    public nint endCol;
}

// FuncVisitor implements the visitor that builds the function position list for a file.
public partial struct FuncVisitor {
    public ptr<token.FileSet> fset;
    public @string name; // Name of file.
    public ptr<ast.File> astFile;
    public slice<ptr<FuncExtent>> funcs;
}

// Visit implements the ast.Visitor interface.
private static ast.Visitor Visit(this ptr<FuncVisitor> _addr_v, ast.Node node) {
    ref FuncVisitor v = ref _addr_v.val;

    switch (node.type()) {
        case ptr<ast.FuncDecl> n:
            if (n.Body == null) { 
                // Do not count declarations of assembly functions.
                break;
            }
            var start = v.fset.Position(n.Pos());
            var end = v.fset.Position(n.End());
            ptr<FuncExtent> fe = addr(new FuncExtent(name:n.Name.Name,startLine:start.Line,startCol:start.Column,endLine:end.Line,endCol:end.Column,));
            v.funcs = append(v.funcs, fe);
            break;
    }
    return v;
}

// coverage returns the fraction of the statements in the function that were covered, as a numerator and denominator.
private static (long, long) coverage(this ptr<FuncExtent> _addr_f, ptr<cover.Profile> _addr_profile) {
    long num = default;
    long den = default;
    ref FuncExtent f = ref _addr_f.val;
    ref cover.Profile profile = ref _addr_profile.val;
 
    // We could avoid making this n^2 overall by doing a single scan and annotating the functions,
    // but the sizes of the data structures is never very large and the scan is almost instantaneous.
    long covered = default;    long total = default; 
    // The blocks are sorted, so we can stop counting as soon as we reach the end of the relevant block.
 
    // The blocks are sorted, so we can stop counting as soon as we reach the end of the relevant block.
    foreach (var (_, b) in profile.Blocks) {
        if (b.StartLine > f.endLine || (b.StartLine == f.endLine && b.StartCol >= f.endCol)) { 
            // Past the end of the function.
            break;
        }
        if (b.EndLine < f.startLine || (b.EndLine == f.startLine && b.EndCol <= f.startCol)) { 
            // Before the beginning of the function
            continue;
        }
        total += int64(b.NumStmt);
        if (b.Count > 0) {
            covered += int64(b.NumStmt);
        }
    }    return (covered, total);
}

// Pkg describes a single package, compatible with the JSON output from 'go list'; see 'go help list'.
public partial struct Pkg {
    public @string ImportPath;
    public @string Dir;
}

private static (map<@string, ptr<Pkg>>, error) findPkgs(slice<ptr<cover.Profile>> profiles) {
    map<@string, ptr<Pkg>> _p0 = default;
    error _p0 = default!;
 
    // Run go list to find the location of every package we care about.
    var pkgs = make_map<@string, ptr<Pkg>>();
    slice<@string> list = default;
    foreach (var (_, profile) in profiles) {
        if (strings.HasPrefix(profile.FileName, ".") || filepath.IsAbs(profile.FileName)) { 
            // Relative or absolute path.
            continue;
        }
        ref var pkg = ref heap(path.Dir(profile.FileName), out ptr<var> _addr_pkg);
        {
            var (_, ok) = pkgs[pkg];

            if (!ok) {
                pkgs[pkg] = null;
                list = append(list, pkg);
            }

        }
    }    if (len(list) == 0) {
        return (pkgs, error.As(null!)!);
    }
    var goTool = filepath.Join(runtime.GOROOT(), "bin/go");
    var cmd = exec.Command(goTool, append(new slice<@string>(new @string[] { "list", "-e", "-json" }), list));
    ref bytes.Buffer stderr = ref heap(out ptr<bytes.Buffer> _addr_stderr);
    _addr_cmd.Stderr = _addr_stderr;
    cmd.Stderr = ref _addr_cmd.Stderr.val;
    var (stdout, err) = cmd.Output();
    if (err != null) {
        return (null, error.As(fmt.Errorf("cannot run go list: %v\n%s", err, stderr.Bytes()))!);
    }
    var dec = json.NewDecoder(bytes.NewReader(stdout));
    while (true) {
        pkg = default;
        var err = dec.Decode(_addr_pkg);
        if (err == io.EOF) {
            break;
        }
        if (err != null) {
            return (null, error.As(fmt.Errorf("decoding go list json: %v", err))!);
        }
        _addr_pkgs[pkg.ImportPath] = _addr_pkg;
        pkgs[pkg.ImportPath] = ref _addr_pkgs[pkg.ImportPath].val;
    }
    return (pkgs, error.As(null!)!);
}

// findFile finds the location of the named file in GOROOT, GOPATH etc.
private static (@string, error) findFile(map<@string, ptr<Pkg>> pkgs, @string file) {
    @string _p0 = default;
    error _p0 = default!;

    if (strings.HasPrefix(file, ".") || filepath.IsAbs(file)) { 
        // Relative or absolute path.
        return (file, error.As(null!)!);
    }
    var pkg = pkgs[path.Dir(file)];
    if (pkg != null) {
        if (pkg.Dir != "") {
            return (filepath.Join(pkg.Dir, path.Base(file)), error.As(null!)!);
        }
        if (pkg.Error != null) {
            return ("", error.As(errors.New(pkg.Error.Err))!);
        }
    }
    return ("", error.As(fmt.Errorf("did not find package for %s in go list output", file))!);
}

private static double percent(long covered, long total) {
    if (total == 0) {
        total = 1; // Avoid zero denominator.
    }
    return 100.0F * float64(covered) / float64(total);
}

} // end main_package
