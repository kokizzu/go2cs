// Copyright 2019 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package codehost -- go2cs converted at 2022 March 13 06:32:05 UTC
// import "cmd/go/internal/modfetch/codehost" ==> using codehost = go.cmd.go.@internal.modfetch.codehost_package
// Original source: C:\Program Files\Go\src\cmd\go\internal\modfetch\codehost\svn.go
namespace go.cmd.go.@internal.modfetch;

using zip = archive.zip_package;
using xml = encoding.xml_package;
using fmt = fmt_package;
using io = io_package;
using os = os_package;
using path = path_package;
using filepath = path.filepath_package;
using time = time_package;
using System.ComponentModel;

public static partial class codehost_package {

private static (ptr<RevInfo>, error) svnParseStat(@string rev, @string @out) {
    ptr<RevInfo> _p0 = default!;
    error _p0 = default!;

    ref var log = ref heap(out ptr<var> _addr_log);
    {
        var err = xml.Unmarshal((slice<byte>)out, _addr_log);

        if (err != null) {
            return (_addr_null!, error.As(vcsErrorf("unexpected response from svn log --xml: %v\n%s", err, out))!);
        }
    }

    var (t, err) = time.Parse(time.RFC3339, log.Logentry.Date);
    if (err != null) {
        return (_addr_null!, error.As(vcsErrorf("unexpected response from svn log --xml: %v\n%s", err, out))!);
    }
    ptr<RevInfo> info = addr(new RevInfo(Name:fmt.Sprintf("%d",log.Logentry.Revision),Short:fmt.Sprintf("%012d",log.Logentry.Revision),Time:t.UTC(),Version:rev,));
    return (_addr_info!, error.As(null!)!);
}

private static error svnReadZip(io.Writer dst, @string workDir, @string rev, @string subdir, @string remote) => func((defer, _, _) => {
    error err = default!;
 
    // The subversion CLI doesn't provide a command to write the repository
    // directly to an archive, so we need to export it to the local filesystem
    // instead. Unfortunately, the local filesystem might apply arbitrary
    // normalization to the filenames, so we need to obtain those directly.
    //
    // 'svn export' prints the filenames as they are written, but from reading the
    // svn source code (as of revision 1868933), those filenames are encoded using
    // the system locale rather than preserved byte-for-byte from the origin. For
    // our purposes, that won't do, but we don't want to go mucking around with
    // the user's locale settings either — that could impact error messages, and
    // we don't know what locales the user has available or what LC_* variables
    // their platform supports.
    //
    // Instead, we'll do a two-pass export: first we'll run 'svn list' to get the
    // canonical filenames, then we'll 'svn export' and look for those filenames
    // in the local filesystem. (If there is an encoding problem at that point, we
    // would probably reject the resulting module anyway.)

    var remotePath = remote;
    if (subdir != "") {
        remotePath += "/" + subdir;
    }
    var (out, err) = Run(workDir, new slice<@string>(new @string[] { "svn", "list", "--non-interactive", "--xml", "--incremental", "--recursive", "--revision", rev, "--", remotePath }));
    if (err != null) {
        return error.As(err)!;
    }
    private partial struct listEntry {
        [Description("xml:\"kind,attr\"")]
        public @string Kind;
        [Description("xml:\"name\"")]
        public @string Name;
        [Description("xml:\"size\"")]
        public long Size;
    }
    ref var list = ref heap(out ptr<var> _addr_list);
    {
        var err__prev1 = err;

        var err = xml.Unmarshal(out, _addr_list);

        if (err != null) {
            return error.As(vcsErrorf("unexpected response from svn list --xml: %v\n%s", err, out))!;
        }
        err = err__prev1;

    }

    var exportDir = filepath.Join(workDir, "export"); 
    // Remove any existing contents from a previous (failed) run.
    {
        var err__prev1 = err;

        err = os.RemoveAll(exportDir);

        if (err != null) {
            return error.As(err)!;
        }
        err = err__prev1;

    }
    defer(os.RemoveAll(exportDir)); // best-effort

    _, err = Run(workDir, new slice<@string>(new @string[] { "svn", "export", "--non-interactive", "--quiet", "--native-eol", "LF", "--ignore-externals", "--ignore-keywords", "--revision", rev, "--", remotePath, exportDir }));
    if (err != null) {
        return error.As(err)!;
    }
    var basePath = path.Join(path.Base(remote), subdir);

    var zw = zip.NewWriter(dst);
    foreach (var (_, e) in list.Entries) {
        if (e.Kind != "file") {
            continue;
        }
        var (zf, err) = zw.Create(path.Join(basePath, e.Name));
        if (err != null) {
            return error.As(err)!;
        }
        var (f, err) = os.Open(filepath.Join(exportDir, e.Name));
        if (err != null) {
            if (os.IsNotExist(err)) {
                return error.As(vcsErrorf("file reported by 'svn list', but not written by 'svn export': %s", e.Name))!;
            }
            return error.As(fmt.Errorf("error opening file created by 'svn export': %v", err))!;
        }
        var (n, err) = io.Copy(zf, f);
        f.Close();
        if (err != null) {
            return error.As(err)!;
        }
        if (n != e.Size) {
            return error.As(vcsErrorf("file size differs between 'svn list' and 'svn export': file %s listed as %v bytes, but exported as %v bytes", e.Name, e.Size, n))!;
        }
    }    return error.As(zw.Close())!;
});

} // end codehost_package
