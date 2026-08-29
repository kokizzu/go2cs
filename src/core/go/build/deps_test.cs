// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
// This file exercises the import parser but also checks that
// some low-level packages do not have new dependencies added.
namespace go.go;

using bytes = bytes_package;
using fmt = fmt_package;
using token = global::go.go.token_package;
using dag = global::go.@internal.dag_package;
using testenv = global::go.@internal.testenv_package;
using fs = global::go.io.fs_package;
using os = os_package;
using filepath = global::go.path.filepath_package;
using runtime = runtime_package;
using slices = slices_package;
using strings = strings_package;
using testing = testing_package;
using global::go.@internal;
using global::go.go;
using global::go.io;
using global::go.path;
using io = io_package;
using static global::go.go.build_package;

partial class build_internal_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸbytes() {
    builtin.initPackage(typeof(bytes_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸgoꓸtoken() {
    builtin.initPackage(typeof(global::go.go.token_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸinternalꓸdag() {
    builtin.initPackage(typeof(global::go.@internal.dag_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸioꓸfs() {
    builtin.initPackage(typeof(global::go.io.fs_package));
}

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸslices() {
    builtin.initPackage(typeof(slices_package));
}

// depsRules defines the expected dependencies between packages in
// the Go source tree. It is a statement of policy.
//
// DO NOT CHANGE THIS DATA TO FIX BUILDS.
// Existing packages should not have their constraints relaxed
// without prior discussion.
// Negative assertions should almost never be removed.
//
// "a < b" means package b can import package a.
//
// See `go doc internal/dag' for the full syntax.
//
// All-caps names are pseudo-names for specific points
// in the dependency lattice.
internal static @string depsRules = """

	# No dependencies allowed for any of these packages.
	NONE
	< unsafe
	< cmp,
	  container/list,
	  container/ring,
	  internal/byteorder,
	  internal/cfg,
	  internal/coverage,
	  internal/coverage/rtcov,
	  internal/coverage/uleb128,
	  internal/coverage/calloc,
	  internal/cpu,
	  internal/goarch,
	  internal/godebugs,
	  internal/goexperiment,
	  internal/goos,
	  internal/goversion,
	  internal/nettrace,
	  internal/platform,
	  internal/profilerecord,
	  internal/trace/traceviewer/format,
	  log/internal,
	  math/bits,
	  unicode,
	  unicode/utf8,
	  unicode/utf16;

	internal/goarch < internal/abi;
	internal/byteorder, internal/goarch < internal/chacha8rand;

	# RUNTIME is the core runtime group of packages, all of them very light-weight.
	internal/abi,
	internal/chacha8rand,
	internal/coverage/rtcov,
	internal/cpu,
	internal/goarch,
	internal/godebugs,
	internal/goexperiment,
	internal/goos,
	internal/profilerecord,
	math/bits
	< internal/bytealg
	< internal/stringslite
	< internal/itoa
	< internal/unsafeheader
	< runtime/internal/sys
	< internal/runtime/syscall
	< internal/runtime/atomic
	< internal/runtime/exithook
	< runtime/internal/math
	< runtime
	< sync/atomic
	< internal/race
	< internal/msan
	< internal/asan
	< internal/weak
	< sync
	< internal/bisect
	< internal/godebug
	< internal/reflectlite
	< errors
	< internal/oserror;

	cmp, internal/race, math/bits
	< iter
	< maps, slices;

	internal/oserror, maps, slices
	< RUNTIME;

	RUNTIME
	< sort
	< container/heap;

	RUNTIME
	< io;

	RUNTIME
	< arena;

	syscall !< io;
	reflect !< sort;

	RUNTIME, unicode/utf8
	< path;

	unicode !< path;

	# SYSCALL is RUNTIME plus the packages necessary for basic system calls.
	RUNTIME, unicode/utf8, unicode/utf16
	< internal/syscall/windows/sysdll, syscall/js
	< syscall
	< internal/syscall/unix, internal/syscall/windows, internal/syscall/windows/registry
	< internal/syscall/execenv
	< SYSCALL;

	# TIME is SYSCALL plus the core packages about time, including context.
	SYSCALL
	< time/tzdata
	< time
	< context
	< TIME;

	TIME, io, path, slices
	< io/fs;

	# MATH is RUNTIME plus the basic math packages.
	RUNTIME
	< math
	< MATH;

	unicode !< math;

	MATH
	< math/cmplx;

	MATH
	< math/rand, math/rand/v2;

	MATH
	< runtime/metrics;

	RUNTIME, math/rand/v2
	< internal/concurrent;

	MATH, unicode/utf8
	< strconv;

	unicode !< strconv;

	# STR is basic string and buffer manipulation.
	RUNTIME, io, unicode/utf8, unicode/utf16, unicode
	< bytes, strings
	< bufio;

	bufio, path, strconv
	< STR;

	RUNTIME, internal/concurrent
	< unique;

	# OS is basic OS access, including helpers (path/filepath, os/exec, etc).
	# OS includes string routines, but those must be layered above package os.
	# OS does not include reflection.
	io/fs
	< internal/testlog
	< internal/poll
	< internal/filepathlite
	< os
	< os/signal;

	io/fs
	< embed;

	unicode, fmt !< net, os, os/signal;

	os/signal, internal/filepathlite, STR
	< path/filepath
	< io/ioutil;

	path/filepath, internal/godebug < os/exec;

	io/ioutil, os/exec, os/signal
	< OS;

	reflect !< OS;

	OS
	< golang.org/x/sys/cpu;

	# FMT is OS (which includes string routines) plus reflect and fmt.
	# It does not include package log, which should be avoided in core packages.
	arena, strconv, unicode
	< reflect;

	os, reflect
	< internal/fmtsort
	< fmt;

	OS, fmt
	< FMT;

	log !< FMT;

	# Misc packages needing only FMT.
	FMT
	< html,
	  internal/dag,
	  internal/goroot,
	  internal/types/errors,
	  mime/quotedprintable,
	  net/internal/socktest,
	  net/url,
	  runtime/trace,
	  text/scanner,
	  text/tabwriter;

	io, reflect
	< internal/saferio;

	# encodings
	# core ones do not use fmt.
	io, strconv, slices
	< encoding;

	encoding, reflect
	< encoding/binary
	< encoding/base32, encoding/base64;

	FMT, encoding < flag;

	fmt !< encoding/base32, encoding/base64;

	FMT, encoding/base32, encoding/base64, internal/saferio
	< encoding/ascii85, encoding/csv, encoding/gob, encoding/hex,
	  encoding/json, encoding/pem, encoding/xml, mime;

	# hashes
	io
	< hash
	< hash/adler32, hash/crc32, hash/crc64, hash/fnv;

	# math/big
	FMT, math/rand
	< math/big;

	# compression
	FMT, encoding/binary, hash/adler32, hash/crc32, sort
	< compress/bzip2, compress/flate, compress/lzw, internal/zstd
	< archive/zip, compress/gzip, compress/zlib;

	# templates
	FMT
	< text/template/parse;

	net/url, text/template/parse
	< text/template
	< internal/lazytemplate;

	# regexp
	FMT, sort
	< regexp/syntax
	< regexp
	< internal/lazyregexp;

	encoding/json, html, text/template, regexp
	< html/template;

	# suffix array
	encoding/binary, regexp
	< index/suffixarray;

	# executable parsing
	FMT, encoding/binary, compress/zlib, internal/saferio, internal/zstd, sort
	< runtime/debug
	< debug/dwarf
	< debug/elf, debug/gosym, debug/macho, debug/pe, debug/plan9obj, internal/xcoff
	< debug/buildinfo
	< DEBUG;

	# go parser and friends.
	FMT, sort
	< internal/gover
	< go/version
	< go/token
	< go/scanner
	< go/ast
	< go/internal/typeparams;

	FMT
	< go/build/constraint;

	FMT, sort
	< go/doc/comment;

	go/internal/typeparams, go/build/constraint
	< go/parser;

	go/doc/comment, go/parser, text/tabwriter
	< go/printer
	< go/format;

	math/big, go/token
	< go/constant;

	FMT, internal/goexperiment
	< internal/buildcfg;

	container/heap, go/constant, go/parser, internal/buildcfg, internal/goversion, internal/types/errors
	< go/types;

	# The vast majority of standard library packages should not be resorting to regexp.
	# go/types is a good chokepoint. It shouldn't use regexp, nor should anything
	# that is low-enough level to be used by go/types.
	regexp !< go/types;

	go/doc/comment, go/parser, internal/lazyregexp, text/template
	< go/doc;

	go/build/constraint, go/doc, go/parser, internal/buildcfg, internal/goroot, internal/goversion, internal/platform
	< go/build;

	# databases
	FMT
	< database/sql/internal
	< database/sql/driver;

	database/sql/driver, math/rand/v2 < database/sql;

	# images
	FMT, compress/lzw, compress/zlib
	< image/color
	< image, image/color/palette
	< image/internal/imageutil
	< image/draw
	< image/gif, image/jpeg, image/png;

	# cgo, delayed as long as possible.
	# If you add a dependency on CGO, you must add the package
	# to cgoPackages in cmd/dist/test.go as well.
	RUNTIME
	< C
	< runtime/cgo
	< CGO
	< runtime/msan, runtime/asan;

	# runtime/race
	NONE < runtime/race/internal/amd64v1;
	NONE < runtime/race/internal/amd64v3;
	CGO, runtime/race/internal/amd64v1, runtime/race/internal/amd64v3 < runtime/race;

	# Bulk of the standard library must not use cgo.
	# The prohibition stops at net and os/user.
	C !< fmt, go/types, CRYPTO-MATH, log/slog;

	CGO, OS
	< plugin;

	CGO, FMT
	< os/user
	< archive/tar;

	sync
	< internal/singleflight;

	os
	< golang.org/x/net/dns/dnsmessage,
	  golang.org/x/net/lif,
	  golang.org/x/net/route;

	internal/bytealg, internal/itoa, math/bits, slices, strconv, unique
	< net/netip;

	# net is unavoidable when doing any networking,
	# so large dependencies must be kept out.
	# This is a long-looking list but most of these
	# are small with few dependencies.
	CGO,
	golang.org/x/net/dns/dnsmessage,
	golang.org/x/net/lif,
	golang.org/x/net/route,
	internal/godebug,
	internal/nettrace,
	internal/poll,
	internal/singleflight,
	net/netip,
	os,
	sort
	< net;

	fmt, unicode !< net;
	math/rand !< net; # net uses runtime instead

	# NET is net plus net-helper packages.
	FMT, net
	< net/textproto;

	mime, net/textproto, net/url
	< NET;

	# logging - most packages should not import; http and up is allowed
	FMT, log/internal
	< log;

	log, log/slog !< crypto/tls, database/sql, go/importer, testing;

	FMT, log, net
	< log/syslog;

	RUNTIME
	< log/slog/internal, log/slog/internal/buffer;

	FMT,
	encoding, encoding/json,
	log, log/internal,
	log/slog/internal, log/slog/internal/buffer,
	slices
	< log/slog
	< log/slog/internal/slogtest, log/slog/internal/benchmarks;

	NET, log
	< net/mail;

	NONE < crypto/internal/boring/sig, crypto/internal/boring/syso;
	sync/atomic < crypto/internal/boring/bcache, crypto/internal/boring/fipstls;
	crypto/internal/boring/sig, crypto/internal/boring/fipstls < crypto/tls/fipsonly;

	# CRYPTO is core crypto algorithms - no cgo, fmt, net.
	crypto/internal/boring/sig,
	crypto/internal/boring/syso,
	golang.org/x/sys/cpu,
	hash, embed
	< crypto
	< crypto/subtle
	< crypto/internal/alias
	< crypto/cipher;

	crypto/cipher,
	crypto/internal/boring/bcache
	< crypto/internal/boring
	< crypto/boring;

	crypto/internal/alias
	< crypto/internal/randutil
	< crypto/internal/nistec/fiat
	< crypto/internal/nistec
	< crypto/internal/edwards25519/field
	< crypto/internal/edwards25519;

	crypto/boring
	< crypto/aes, crypto/des, crypto/hmac, crypto/md5, crypto/rc4,
	  crypto/sha1, crypto/sha256, crypto/sha512;

	crypto/boring, crypto/internal/edwards25519/field
	< crypto/ecdh;

	# Unfortunately, stuck with reflect via encoding/binary.
	encoding/binary, crypto/boring < golang.org/x/crypto/sha3;

	crypto/aes,
	crypto/des,
	crypto/ecdh,
	crypto/hmac,
	crypto/internal/edwards25519,
	crypto/md5,
	crypto/rc4,
	crypto/sha1,
	crypto/sha256,
	crypto/sha512,
	golang.org/x/crypto/sha3
	< CRYPTO;

	CGO, fmt, net !< CRYPTO;

	# CRYPTO-MATH is core bignum-based crypto - no cgo, net; fmt now ok.
	CRYPTO, FMT, math/big
	< crypto/internal/boring/bbig
	< crypto/rand
	< crypto/internal/mlkem768
	< crypto/ed25519
	< encoding/asn1
	< golang.org/x/crypto/cryptobyte/asn1
	< golang.org/x/crypto/cryptobyte
	< crypto/internal/bigmod
	< crypto/dsa, crypto/elliptic, crypto/rsa
	< crypto/ecdsa
	< CRYPTO-MATH;

	CGO, net !< CRYPTO-MATH;

	# TLS, Prince of Dependencies.
	CRYPTO-MATH, NET, container/list, encoding/hex, encoding/pem
	< golang.org/x/crypto/internal/alias
	< golang.org/x/crypto/internal/subtle
	< golang.org/x/crypto/chacha20
	< golang.org/x/crypto/internal/poly1305
	< golang.org/x/crypto/chacha20poly1305
	< golang.org/x/crypto/hkdf
	< crypto/internal/hpke
	< crypto/x509/internal/macos
	< crypto/x509/pkix;

	crypto/internal/boring/fipstls, crypto/x509/pkix
	< crypto/x509
	< crypto/tls;

	# crypto-aware packages

	DEBUG, go/build, go/types, text/scanner, crypto/md5
	< internal/pkgbits
	< go/internal/gcimporter, go/internal/gccgoimporter, go/internal/srcimporter
	< go/importer;

	NET, crypto/rand, mime/quotedprintable
	< mime/multipart;

	crypto/tls
	< net/smtp;

	crypto/rand
	< hash/maphash; # for purego implementation

	# HTTP, King of Dependencies.

	FMT
	< golang.org/x/net/http2/hpack
	< net/http/internal, net/http/internal/ascii, net/http/internal/testcert;

	FMT, NET, container/list, encoding/binary, log
	< golang.org/x/text/transform
	< golang.org/x/text/unicode/norm
	< golang.org/x/text/unicode/bidi
	< golang.org/x/text/secure/bidirule
	< golang.org/x/net/idna
	< golang.org/x/net/http/httpguts, golang.org/x/net/http/httpproxy;

	NET, crypto/tls
	< net/http/httptrace;

	compress/gzip,
	golang.org/x/net/http/httpguts,
	golang.org/x/net/http/httpproxy,
	golang.org/x/net/http2/hpack,
	net/http/internal,
	net/http/internal/ascii,
	net/http/internal/testcert,
	net/http/httptrace,
	mime/multipart,
	log
	< net/http;

	# HTTP-aware packages

	encoding/json, net/http
	< expvar;

	net/http, net/http/internal/ascii
	< net/http/cookiejar, net/http/httputil;

	net/http, flag
	< net/http/httptest;

	net/http, regexp
	< net/http/cgi
	< net/http/fcgi;

	# Profiling
	FMT, compress/gzip, encoding/binary, sort, text/tabwriter
	< runtime/pprof;

	OS, compress/gzip, internal/lazyregexp
	< internal/profile;

	html, internal/profile, net/http, runtime/pprof, runtime/trace
	< net/http/pprof;

	# RPC
	encoding/gob, encoding/json, go/token, html/template, net/http
	< net/rpc
	< net/rpc/jsonrpc;

	# System Information
	bufio, bytes, internal/cpu, io, os, strings, sync
	< internal/sysinfo;

	# Test-only
	log
	< testing/iotest
	< testing/fstest;

	FMT, flag, math/rand
	< testing/quick;

	FMT, DEBUG, flag, runtime/trace, internal/sysinfo, math/rand
	< testing;

	log/slog, testing
	< testing/slogtest;

	FMT, crypto/sha256, encoding/json, go/ast, go/parser, go/token,
	internal/godebug, math/rand, encoding/hex, crypto/sha256
	< internal/fuzz;

	OS, flag, testing, internal/cfg, internal/platform, internal/goroot
	< internal/testenv;

	OS, encoding/base64
	< internal/obscuretestdata;

	CGO, OS, fmt
	< internal/testpty;

	NET, testing, math/rand
	< golang.org/x/net/nettest;

	syscall
	< os/exec/internal/fdtest;

	FMT, sort
	< internal/diff;

	FMT
	< internal/txtar;

	CRYPTO-MATH, testing
	< crypto/internal/cryptotest;

	# v2 execution trace parser.
	FMT
	< internal/trace/event;

	internal/trace/event
	< internal/trace/event/go122;

	FMT, io, internal/trace/event/go122
	< internal/trace/version;

	FMT, encoding/binary, internal/trace/version
	< internal/trace/raw;

	FMT, internal/trace/event, internal/trace/version, io, sort, encoding/binary
	< internal/trace/internal/oldtrace;

	FMT, encoding/binary, internal/trace/version, internal/trace/internal/oldtrace, container/heap, math/rand
	< internal/trace;

	regexp, internal/trace, internal/trace/raw, internal/txtar
	< internal/trace/testtrace;

	regexp, internal/txtar, internal/trace, internal/trace/raw
	< internal/trace/internal/testgen/go122;

	# cmd/trace dependencies.
	FMT,
	embed,
	encoding/json,
	html/template,
	internal/profile,
	internal/trace,
	internal/trace/traceviewer/format,
	net/http
	< internal/trace/traceviewer;

	# Coverage.
	FMT, crypto/md5, encoding/binary, regexp, sort, text/tabwriter,
	internal/coverage, internal/coverage/uleb128
	< internal/coverage/cmerge,
	  internal/coverage/pods,
	  internal/coverage/slicereader,
	  internal/coverage/slicewriter;

	internal/coverage/slicereader, internal/coverage/slicewriter
	< internal/coverage/stringtab
	< internal/coverage/decodecounter, internal/coverage/decodemeta,
	  internal/coverage/encodecounter, internal/coverage/encodemeta;

	internal/coverage/cmerge
	< internal/coverage/cformat;

	internal/coverage, crypto/sha256, FMT
	< cmd/internal/cov/covcmd;

	encoding/json,
	runtime/debug,
	internal/coverage/calloc,
	internal/coverage/cformat,
	internal/coverage/decodecounter, internal/coverage/decodemeta,
	internal/coverage/encodecounter, internal/coverage/encodemeta,
	internal/coverage/pods
	< internal/coverage/cfile
	< runtime/coverage;

	internal/coverage/cfile, internal/fuzz, internal/testlog, runtime/pprof, regexp
	< testing/internal/testdeps;

	# Test-only packages can have anything they want
	CGO, internal/syscall/unix < net/internal/cgotest;



"""u8;

// listStdPkgs returns the same list of packages as "go list std".
internal static (slice<@string>, error) listStdPkgs(@string goroot) {
    // Based on cmd/go's matchPackages function.
    ref var pkgs = ref heap<slice<@string>>(out var Ꮡpkgs);
    @string src = filepath.Join(goroot, srcˢ) + ((@string)(rune)filepath.Separator);
    var walkFn = error (@string path, fs.DirEntry d, error err) => {
        if (err != default! || !d.IsDir() || path == src) {
            return default!;
        }
        @string @base = filepath.Base(path);
        if (strings.HasPrefix(@base, "."u8) || strings.HasPrefix(@base, "_"u8) || @base == "testdata"u8) {
            return filepath.SkipDir;
        }
        @string name = filepath.ToSlash(path[(int)(len(src))..]);
        if (name == "builtin"u8 || name == "cmd"u8) {
            return filepath.SkipDir;
        }
        Ꮡpkgs.ValueSlot = append(Ꮡpkgs.ValueSlot, strings.TrimPrefix(name, vendorˢ2));
        return default!;
    };
    {
        var err = filepath.WalkDir(src, new Func<@string, fs.DirEntry, error, error>(walkFn)); if (err != default!) {
            return (default!, err);
        }
    }
    return (pkgs, default!);
}

public static void TestDependencies(ж<testing.T> Ꮡt) {
    if (!testenv.HasSrc()) {
        // Tests run in a limited file system and we do not
        // provide access to every source file.
        Ꮡt.Skipf("skipping on %s/%s, missing full GOROOT"u8, runtime.GOOS, runtime.GOARCH);
    }
    var ctxt = Default;
    var (all, err) = listStdPkgs(ctxt.GOROOT);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    slices.Sort<slice<@string>, @string>(all);
    var sawImport = new map<@string, map<@string, bool>>{}; // from package => to package => true
    var policy = depsPolicy(Ꮡt);
    foreach (var (_, pkg) in all) {
        var (imports, errΔ1) = findImports(pkg);
        if (errΔ1 != default!) {
            Ꮡt.Error(errΔ1);
            continue;
        }
        if (sawImport[pkg] == default!) {
            sawImport[pkg] = new map<@string, bool>{};
        }
        slice<@string> bad = default!;
        foreach (var (_, imp) in imports) {
            sawImport[pkg].Set(imp, true);
            if (!policy.HasEdge(pkg, imp)) {
                bad = append(bad, imp);
            }
        }
        if (bad != default!) {
            Ꮡt.Errorf("unexpected dependency: %s imports %v"u8, pkg, bad);
        }
    }
}

internal static slice<byte> buildIgnore = slice<byte>("\n//go:build ignore"u8);

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string golangOrgˢ = "golang.org"u8;

internal static (slice<@string>, error) findImports(@string pkg) {
    @string vpkg = pkg;
    if (strings.HasPrefix(pkg, golangOrgˢ)) {
        vpkg = "vendor/"u8 + pkg;
    }
    @string dir = filepath.Join(Default.GOROOT, srcˢ, vpkg);
    var (files, err) = os.ReadDir(dir);
    if (err != default!) {
        return (default!, err);
    }
    slice<@string> imports = default!;
    map<@string, bool> haveImport = new map<@string, bool>{};
    if (pkg == "crypto/internal/boring"u8) {
        haveImport["C"u8] = true; // kludge: prevent C from appearing in crypto/internal/boring imports
    }
    var fset = token.NewFileSet();
    foreach (var (_, @file) in files) {
        @string name = @file.Name();
        if (name == "slice_go14.go"u8 || name == "slice_go18.go"u8) {
            // These files are for compiler bootstrap with older versions of Go and not built in the standard build.
            continue;
        }
        if (!strings.HasSuffix(name, ".go"u8) || strings.HasSuffix(name, testGoˢ)) {
            continue;
        }
        ref var info = ref heap<global::go.go.build_package.fileInfo>(out var Ꮡinfo);
        info = new fileInfo(
            name: filepath.Join(dir, name),
            fset: fset
        );
        var (f, errΔ1) = os.Open(info.name);
        if (errΔ1 != default!) {
            return (default!, errΔ1);
        }
        errΔ1 = readGoInfo(new build_internal_test_package.os_FileжReader(f), ref info);
        f.Close();
        if (errΔ1 != default!) {
            return (default!, fmt.Errorf("reading %v: %v"u8, name, errΔ1));
        }
        if ((~(~info.parsed).Name).Name == "main"u8) {
            continue;
        }
        if (bytes.Contains(info.header, buildIgnore)) {
            continue;
        }
        foreach (var (_, imp) in info.imports) {
            @string path = imp.path;
            if (!haveImport[path]) {
                haveImport[path] = true;
                imports = append(imports, path);
            }
        }
    }
    slices.Sort<slice<@string>, @string>(imports);
    return (imports, default!);
}

// depsPolicy returns a map m such that m[p][d] == true when p can import d.
internal static ж<dag.Graph> depsPolicy(ж<testing.T> Ꮡt) {
    var (g, err) = dag.Parse(depsRules);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    return g;
}

// TestStdlibLowercase tests that all standard library package names are
// lowercase. See Issue 40065.
public static void TestStdlibLowercase(ж<testing.T> Ꮡt) {
    if (!testenv.HasSrc()) {
        Ꮡt.Skipf("skipping on %s/%s, missing full GOROOT"u8, runtime.GOOS, runtime.GOARCH);
    }
    var ctxt = Default;
    var (all, err) = listStdPkgs(ctxt.GOROOT);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    foreach (var (_, pkgname) in all) {
        if (strings.ToLower(pkgname) != pkgname) {
            Ꮡt.Errorf("package %q should not use upper-case path"u8, pkgname);
        }
    }
}

// TestFindImports tests that findImports works.  See #43249.
public static void TestFindImports(ж<testing.T> Ꮡt) {
    var (imports, err) = findImports(goBuildˢ);
    if (err != default!) {
        Ꮡt.Fatal(err);
    }
    Ꮡt.Logf("go/build imports %q"u8, imports);
    var want = new @string[]{"bytes"u8, "os"u8, "path/filepath"u8, "strings"u8}.slice();
wantLoop:
    foreach (var (_, w) in want) {
        foreach (var (_, imp) in imports) {
            if (imp == w) {
                goto continue_wantLoop;
            }
        }
        Ꮡt.Errorf("expected to find %q in import list"u8, w);
continue_wantLoop:;
    }
break_wantLoop:;
}

} // end build_internal_test_package
