// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Checking of compiler and linker flags.
// We must avoid flags like -fplugin=, which can allow
// arbitrary code execution during the build.
// Do not make changes here without carefully
// considering the implications.
// (That's why the code is isolated in a file named security.go.)
//
// Note that -Wl,foo means split foo on commas and pass to
// the linker, so that -Wl,-foo,bar means pass -foo bar to
// the linker. Similarly -Wa,foo for the assembler and so on.
// If any of these are permitted, the wildcard portion must
// disallow commas.
//
// Note also that GNU binutils accept any argument @foo
// as meaning "read more flags from the file foo", so we must
// guard against any command-line argument beginning with @,
// even things like "-I @foo".
// We use load.SafeArg (which is even more conservative)
// to reject these.
//
// Even worse, gcc -I@foo (one arg) turns into cc1 -I @foo (two args),
// so although gcc doesn't expand the @foo, cc1 will.
// So out of paranoia, we reject @ at the beginning of every
// flag argument that might be split into its own argument.

// package work -- go2cs converted at 2022 March 13 06:31:05 UTC
// import "cmd/go/internal/work" ==> using work = go.cmd.go.@internal.work_package
// Original source: C:\Program Files\Go\src\cmd\go\internal\work\security.go
namespace go.cmd.go.@internal;

using fmt = fmt_package;
using lazyregexp = @internal.lazyregexp_package;
using regexp = regexp_package;
using strings = strings_package;

using cfg = cmd.go.@internal.cfg_package;
using load = cmd.go.@internal.load_package;

public static partial class work_package {

private static var re = lazyregexp.New;

private static ptr<lazyregexp.Regexp> validCompilerFlags = new slice<ptr<lazyregexp.Regexp>>(new ptr<lazyregexp.Regexp>[] { re(`-D([A-Za-z_][A-Za-z0-9_]*)(=[^@\-]*)?`), re(`-U([A-Za-z_][A-Za-z0-9_]*)`), re(`-F([^@\-].*)`), re(`-I([^@\-].*)`), re(`-O`), re(`-O([^@\-].*)`), re(`-W`), re(`-W([^@,]+)`), re(`-Wa,-mbig-obj`), re(`-Wp,-D([A-Za-z_][A-Za-z0-9_]*)(=[^@,\-]*)?`), re(`-Wp,-U([A-Za-z_][A-Za-z0-9_]*)`), re(`-ansi`), re(`-f(no-)?asynchronous-unwind-tables`), re(`-f(no-)?blocks`), re(`-f(no-)builtin-[a-zA-Z0-9_]*`), re(`-f(no-)?common`), re(`-f(no-)?constant-cfstrings`), re(`-fdiagnostics-show-note-include-stack`), re(`-f(no-)?eliminate-unused-debug-types`), re(`-f(no-)?exceptions`), re(`-f(no-)?fast-math`), re(`-f(no-)?inline-functions`), re(`-finput-charset=([^@\-].*)`), re(`-f(no-)?fat-lto-objects`), re(`-f(no-)?keep-inline-dllexport`), re(`-f(no-)?lto`), re(`-fmacro-backtrace-limit=(.+)`), re(`-fmessage-length=(.+)`), re(`-f(no-)?modules`), re(`-f(no-)?objc-arc`), re(`-f(no-)?objc-nonfragile-abi`), re(`-f(no-)?objc-legacy-dispatch`), re(`-f(no-)?omit-frame-pointer`), re(`-f(no-)?openmp(-simd)?`), re(`-f(no-)?permissive`), re(`-f(no-)?(pic|PIC|pie|PIE)`), re(`-f(no-)?plt`), re(`-f(no-)?rtti`), re(`-f(no-)?split-stack`), re(`-f(no-)?stack-(.+)`), re(`-f(no-)?strict-aliasing`), re(`-f(un)signed-char`), re(`-f(no-)?use-linker-plugin`), re(`-f(no-)?visibility-inlines-hidden`), re(`-fsanitize=(.+)`), re(`-ftemplate-depth-(.+)`), re(`-fvisibility=(.+)`), re(`-g([^@\-].*)?`), re(`-m32`), re(`-m64`), re(`-m(abi|arch|cpu|fpu|tune)=([^@\-].*)`), re(`-m(no-)?v?aes`), re(`-marm`), re(`-m(no-)?avx[0-9a-z]*`), re(`-mfloat-abi=([^@\-].*)`), re(`-mfpmath=[0-9a-z,+]*`), re(`-m(no-)?avx[0-9a-z.]*`), re(`-m(no-)?ms-bitfields`), re(`-m(no-)?stack-(.+)`), re(`-mmacosx-(.+)`), re(`-mios-simulator-version-min=(.+)`), re(`-miphoneos-version-min=(.+)`), re(`-mtvos-simulator-version-min=(.+)`), re(`-mtvos-version-min=(.+)`), re(`-mwatchos-simulator-version-min=(.+)`), re(`-mwatchos-version-min=(.+)`), re(`-mnop-fun-dllimport`), re(`-m(no-)?sse[0-9.]*`), re(`-m(no-)?ssse3`), re(`-mthumb(-interwork)?`), re(`-mthreads`), re(`-mwindows`), re(`--param=ssp-buffer-size=[0-9]*`), re(`-pedantic(-errors)?`), re(`-pipe`), re(`-pthread`), re(`-?-std=([^@\-].*)`), re(`-?-stdlib=([^@\-].*)`), re(`--sysroot=([^@\-].*)`), re(`-w`), re(`-x([^@\-].*)`), re(`-v`) });

private static @string validCompilerFlagsWithNextArg = new slice<@string>(new @string[] { "-arch", "-D", "-U", "-I", "-framework", "-include", "-isysroot", "-isystem", "--sysroot", "-target", "-x" });

private static ptr<lazyregexp.Regexp> validLinkerFlags = new slice<ptr<lazyregexp.Regexp>>(new ptr<lazyregexp.Regexp>[] { re(`-F([^@\-].*)`), re(`-l([^@\-].*)`), re(`-L([^@\-].*)`), re(`-O`), re(`-O([^@\-].*)`), re(`-f(no-)?(pic|PIC|pie|PIE)`), re(`-f(no-)?openmp(-simd)?`), re(`-fsanitize=([^@\-].*)`), re(`-flat_namespace`), re(`-g([^@\-].*)?`), re(`-headerpad_max_install_names`), re(`-m(abi|arch|cpu|fpu|tune)=([^@\-].*)`), re(`-mfloat-abi=([^@\-].*)`), re(`-mmacosx-(.+)`), re(`-mios-simulator-version-min=(.+)`), re(`-miphoneos-version-min=(.+)`), re(`-mthreads`), re(`-mwindows`), re(`-(pic|PIC|pie|PIE)`), re(`-pthread`), re(`-rdynamic`), re(`-shared`), re(`-?-static([-a-z0-9+]*)`), re(`-?-stdlib=([^@\-].*)`), re(`-v`), re(`-Wl,--(no-)?allow-multiple-definition`), re(`-Wl,--(no-)?allow-shlib-undefined`), re(`-Wl,--(no-)?as-needed`), re(`-Wl,-Bdynamic`), re(`-Wl,-berok`), re(`-Wl,-Bstatic`), re(`-Wl,-Bsymbolic-functions`), re(`-Wl,-O([^@,\-][^,]*)?`), re(`-Wl,-d[ny]`), re(`-Wl,--disable-new-dtags`), re(`-Wl,-e[=,][a-zA-Z0-9]*`), re(`-Wl,--enable-new-dtags`), re(`-Wl,--end-group`), re(`-Wl,--(no-)?export-dynamic`), re(`-Wl,-E`), re(`-Wl,-framework,[^,@\-][^,]+`), re(`-Wl,--hash-style=(sysv|gnu|both)`), re(`-Wl,-headerpad_max_install_names`), re(`-Wl,--no-undefined`), re(`-Wl,-R([^@\-][^,@]*$)`), re(`-Wl,--just-symbols[=,]([^,@\-][^,@]+)`), re(`-Wl,-rpath(-link)?[=,]([^,@\-][^,]+)`), re(`-Wl,-s`), re(`-Wl,-search_paths_first`), re(`-Wl,-sectcreate,([^,@\-][^,]+),([^,@\-][^,]+),([^,@\-][^,]+)`), re(`-Wl,--start-group`), re(`-Wl,-?-static`), re(`-Wl,-?-subsystem,(native|windows|console|posix|xbox)`), re(`-Wl,-syslibroot[=,]([^,@\-][^,]+)`), re(`-Wl,-undefined[=,]([^,@\-][^,]+)`), re(`-Wl,-?-unresolved-symbols=[^,]+`), re(`-Wl,--(no-)?warn-([^,]+)`), re(`-Wl,-?-wrap[=,][^,@\-][^,]*`), re(`-Wl,-z,(no)?execstack`), re(`-Wl,-z,relro`), re(`[a-zA-Z0-9_/].*\.(a|o|obj|dll|dylib|so|tbd)`), re(`\./.*\.(a|o|obj|dll|dylib|so|tbd)`) });

private static @string validLinkerFlagsWithNextArg = new slice<@string>(new @string[] { "-arch", "-F", "-l", "-L", "-framework", "-isysroot", "--sysroot", "-target", "-Wl,-framework", "-Wl,-rpath", "-Wl,-R", "-Wl,--just-symbols", "-Wl,-undefined" });

private static error checkCompilerFlags(@string name, @string source, slice<@string> list) {
    return error.As(checkFlags(name, source, list, validCompilerFlags, validCompilerFlagsWithNextArg))!;
}

private static error checkLinkerFlags(@string name, @string source, slice<@string> list) {
    return error.As(checkFlags(name, source, list, validLinkerFlags, validLinkerFlagsWithNextArg))!;
}

private static error checkFlags(@string name, @string source, slice<@string> list, slice<ptr<lazyregexp.Regexp>> valid, slice<@string> validNext) { 
    // Let users override rules with $CGO_CFLAGS_ALLOW, $CGO_CFLAGS_DISALLOW, etc.
    ptr<regexp.Regexp> allow;    ptr<regexp.Regexp> disallow;
    {
        var env__prev1 = env;

        var env = cfg.Getenv("CGO_" + name + "_ALLOW");

        if (env != "") {
            var (r, err) = regexp.Compile(env);
            if (err != null) {
                return error.As(fmt.Errorf("parsing $CGO_%s_ALLOW: %v", name, err))!;
            }
            allow = r;
        }
        env = env__prev1;

    }
    {
        var env__prev1 = env;

        env = cfg.Getenv("CGO_" + name + "_DISALLOW");

        if (env != "") {
            (r, err) = regexp.Compile(env);
            if (err != null) {
                return error.As(fmt.Errorf("parsing $CGO_%s_DISALLOW: %v", name, err))!;
            }
            disallow = r;
        }
        env = env__prev1;

    }

Args:
    for (nint i = 0; i < len(list); i++) {
        var arg = list[i];
        if (disallow != null && disallow.FindString(arg) == arg) {
            goto Bad;
        }
        if (allow != null && allow.FindString(arg) == arg) {
            _continueArgs = true;
            break;
        }
        foreach (var (_, re) in valid) {
            if (re.FindString(arg) == arg) { // must be complete match
                _continueArgs = true;
                break;
            }
        }        foreach (var (_, x) in validNext) {
            if (arg == x) {
                if (i + 1 < len(list) && load.SafeArg(list[i + 1])) {
                    i++;
                    _continueArgs = true;
                    break;
                } 

                // Permit -Wl,-framework -Wl,name.
                if (i + 1 < len(list) && strings.HasPrefix(arg, "-Wl,") && strings.HasPrefix(list[i + 1], "-Wl,") && load.SafeArg(list[i + 1][(int)4..]) && !strings.Contains(list[i + 1][(int)4..], ",")) {
                    i++;
                    _continueArgs = true;
                    break;
                } 

                // Permit -I= /path, -I $SYSROOT.
                if (i + 1 < len(list) && arg == "-I") {
                    if ((strings.HasPrefix(list[i + 1], "=") || strings.HasPrefix(list[i + 1], "$SYSROOT")) && load.SafeArg(list[i + 1][(int)1..])) {
                        i++;
                        _continueArgs = true;
                        break;
                    }
                }
                if (i + 1 < len(list)) {
                    return error.As(fmt.Errorf("invalid flag in %s: %s %s (see https://golang.org/s/invalidflag)", source, arg, list[i + 1]))!;
                }
                return error.As(fmt.Errorf("invalid flag in %s: %s without argument (see https://golang.org/s/invalidflag)", source, arg))!;
            }
        }Bad:
        return error.As(fmt.Errorf("invalid flag in %s: %s", source, arg))!;
    }
    return error.As(null!)!;
}

} // end work_package
