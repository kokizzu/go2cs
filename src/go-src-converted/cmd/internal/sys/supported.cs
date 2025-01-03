// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package sys -- go2cs converted at 2022 March 13 05:43:20 UTC
// import "cmd/internal/sys" ==> using sys = go.cmd.@internal.sys_package
// Original source: C:\Program Files\Go\src\cmd\internal\sys\supported.go
namespace go.cmd.@internal;

public static partial class sys_package {

// RaceDetectorSupported reports whether goos/goarch supports the race
// detector. There is a copy of this function in cmd/dist/test.go.
// Race detector only supports 48-bit VMA on arm64. But it will always
// return true for arm64, because we don't have VMA size information during
// the compile time.
public static bool RaceDetectorSupported(@string goos, @string goarch) {
    switch (goos) {
        case "linux": 
            return goarch == "amd64" || goarch == "ppc64le" || goarch == "arm64";
            break;
        case "darwin": 
            return goarch == "amd64" || goarch == "arm64";
            break;
        case "freebsd": 

        case "netbsd": 

        case "openbsd": 

        case "windows": 
            return goarch == "amd64";
            break;
        default: 
            return false;
            break;
    }
}

// MSanSupported reports whether goos/goarch supports the memory
// sanitizer option.
// There is a copy of this function in misc/cgo/testsanitizers/cc_test.go.
public static bool MSanSupported(@string goos, @string goarch) {
    switch (goos) {
        case "linux": 
            return goarch == "amd64" || goarch == "arm64";
            break;
        default: 
            return false;
            break;
    }
}

// MustLinkExternal reports whether goos/goarch requires external linking.
// (This is the opposite of internal/testenv.CanInternalLink. Keep them in sync.)
public static bool MustLinkExternal(@string goos, @string goarch) {
    switch (goos) {
        case "android": 
            if (goarch != "arm64") {
                return true;
            }
            break;
        case "ios": 
            if (goarch == "arm64") {
                return true;
            }
            break;
    }
    return false;
}

// BuildModeSupported reports whether goos/goarch supports the given build mode
// using the given compiler.
public static bool BuildModeSupported(@string compiler, @string buildmode, @string goos, @string goarch) {
    if (compiler == "gccgo") {
        return true;
    }
    var platform = goos + "/" + goarch;

    switch (buildmode) {
        case "archive": 
            return true;
            break;
        case "c-archive": 
            // TODO(bcmills): This seems dubious.
            // Do we really support c-archive mode on js/wasm‽
            return platform != "linux/ppc64";
            break;
        case "c-shared": 
            switch (platform) {
                case "linux/amd64": 

                case "linux/arm": 

                case "linux/arm64": 

                case "linux/386": 

                case "linux/ppc64le": 

                case "linux/s390x": 

                case "android/amd64": 

                case "android/arm": 

                case "android/arm64": 

                case "android/386": 

                case "freebsd/amd64": 

                case "darwin/amd64": 

                case "darwin/arm64": 

                case "windows/amd64": 

                case "windows/386": 

                case "windows/arm64": 
                    return true;
                    break;
            }
            return false;
            break;
        case "default": 
            return true;
            break;
        case "exe": 
            return true;
            break;
        case "pie": 
            switch (platform) {
                case "linux/386": 

                case "linux/amd64": 

                case "linux/arm": 

                case "linux/arm64": 

                case "linux/ppc64le": 

                case "linux/riscv64": 

                case "linux/s390x": 

                case "android/amd64": 

                case "android/arm": 

                case "android/arm64": 

                case "android/386": 

                case "freebsd/amd64": 

                case "darwin/amd64": 

                case "darwin/arm64": 

                case "ios/amd64": 

                case "ios/arm64": 

                case "aix/ppc64": 

                case "windows/386": 

                case "windows/amd64": 

                case "windows/arm": 
                    return true;
                    break;
            }
            return false;
            break;
        case "shared": 
            switch (platform) {
                case "linux/386": 

                case "linux/amd64": 

                case "linux/arm": 

                case "linux/arm64": 

                case "linux/ppc64le": 

                case "linux/s390x": 
                    return true;
                    break;
            }
            return false;
            break;
        case "plugin": 
            switch (platform) {
                case "linux/amd64": 

                case "linux/arm": 

                case "linux/arm64": 

                case "linux/386": 

                case "linux/s390x": 

                case "linux/ppc64le": 

                case "android/amd64": 

                case "android/arm": 

                case "android/arm64": 

                case "android/386": 

                case "darwin/amd64": 

                case "darwin/arm64": 

                case "freebsd/amd64": 
                    return true;
                    break;
            }
            return false;
            break;
        default: 
            return false;
            break;
    }
}

public static bool InternalLinkPIESupported(@string goos, @string goarch) {
    switch (goos + "/" + goarch) {
        case "darwin/amd64": 

        case "darwin/arm64": 

        case "linux/amd64": 

        case "linux/arm64": 

        case "android/arm64": 

        case "windows-amd64": 

        case "windows-386": 

        case "windows-arm": 
            return true;
            break;
    }
    return false;
}

} // end sys_package
