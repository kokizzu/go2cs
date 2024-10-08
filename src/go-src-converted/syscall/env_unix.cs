// Copyright 2010 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

//go:build aix || darwin || dragonfly || freebsd || (js && wasm) || linux || netbsd || openbsd || solaris || plan9
// +build aix darwin dragonfly freebsd js,wasm linux netbsd openbsd solaris plan9

// Unix environment variables.

// package syscall -- go2cs converted at 2022 March 13 05:23:59 UTC
// import "syscall" ==> using syscall = go.syscall_package
// Original source: C:\Program Files\Go\src\syscall\env_unix.go
namespace go;

using runtime = runtime_package;
using sync = sync_package;

public static partial class syscall_package {

 
// envOnce guards initialization by copyenv, which populates env.
private static sync.Once envOnce = default;private static sync.RWMutex envLock = default;private static map<@string, nint> env = default;private static slice<@string> envs = runtime_envs();

private static slice<@string> runtime_envs(); // in package runtime

// setenv_c and unsetenv_c are provided by the runtime but are no-ops
// if cgo isn't loaded.
private static void setenv_c(@string k, @string v);
private static void unsetenv_c(@string k);

private static void copyenv() {
    env = make_map<@string, nint>();
    foreach (var (i, s) in envs) {
        for (nint j = 0; j < len(s); j++) {>>MARKER:FUNCTION_unsetenv_c_BLOCK_PREFIX<<
            if (s[j] == '=') {>>MARKER:FUNCTION_setenv_c_BLOCK_PREFIX<<
                var key = s[..(int)j];
                {
                    var (_, ok) = env[key];

                    if (!ok) {>>MARKER:FUNCTION_runtime_envs_BLOCK_PREFIX<<
                        env[key] = i; // first mention of key
                    }
                    else
 { 
                        // Clear duplicate keys. This permits Unsetenv to
                        // safely delete only the first item without
                        // worrying about unshadowing a later one,
                        // which might be a security problem.
                        envs[i] = "";
                    }

                }
                break;
            }
        }
    }
}

public static error Unsetenv(@string key) => func((defer, _, _) => {
    envOnce.Do(copyenv);

    envLock.Lock();
    defer(envLock.Unlock());

    {
        var (i, ok) = env[key];

        if (ok) {
            envs[i] = "";
            delete(env, key);
        }
    }
    unsetenv_c(key);
    return error.As(null!)!;
});

public static (@string, bool) Getenv(@string key) => func((defer, _, _) => {
    @string value = default;
    bool found = default;

    envOnce.Do(copyenv);
    if (len(key) == 0) {
        return ("", false);
    }
    envLock.RLock();
    defer(envLock.RUnlock());

    var (i, ok) = env[key];
    if (!ok) {
        return ("", false);
    }
    var s = envs[i];
    {
        var i__prev1 = i;

        for (nint i = 0; i < len(s); i++) {
            if (s[i] == '=') {
                return (s[(int)i + 1..], true);
            }
        }

        i = i__prev1;
    }
    return ("", false);
});

public static error Setenv(@string key, @string value) => func((defer, _, _) => {
    envOnce.Do(copyenv);
    if (len(key) == 0) {
        return error.As(EINVAL)!;
    }
    {
        nint i__prev1 = i;

        for (nint i = 0; i < len(key); i++) {
            if (key[i] == '=' || key[i] == 0) {
                return error.As(EINVAL)!;
            }
        }

        i = i__prev1;
    } 
    // On Plan 9, null is used as a separator, eg in $path.
    if (runtime.GOOS != "plan9") {
        {
            nint i__prev1 = i;

            for (i = 0; i < len(value); i++) {
                if (value[i] == 0) {
                    return error.As(EINVAL)!;
                }
            }


            i = i__prev1;
        }
    }
    envLock.Lock();
    defer(envLock.Unlock());

    var (i, ok) = env[key];
    var kv = key + "=" + value;
    if (ok) {
        envs[i] = kv;
    }
    else
 {
        i = len(envs);
        envs = append(envs, kv);
    }
    env[key] = i;
    setenv_c(key, value);
    return error.As(null!)!;
});

public static void Clearenv() => func((defer, _, _) => {
    envOnce.Do(copyenv); // prevent copyenv in Getenv/Setenv

    envLock.Lock();
    defer(envLock.Unlock());

    foreach (var (k) in env) {
        unsetenv_c(k);
    }    env = make_map<@string, nint>();
    envs = new slice<@string>(new @string[] {  });
});

public static slice<@string> Environ() => func((defer, _, _) => {
    envOnce.Do(copyenv);
    envLock.RLock();
    defer(envLock.RUnlock());
    var a = make_slice<@string>(0, len(envs));
    foreach (var (_, env) in envs) {
        if (env != "") {
            a = append(a, env);
        }
    }    return a;
});

} // end syscall_package
