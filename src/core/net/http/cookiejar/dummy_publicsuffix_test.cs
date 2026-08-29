// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.net.http;

using cookiejar = go.net.http.cookiejar_package;
using go.net.http;
using static go.net.http.cookiejar_internal_test_package;

partial class cookiejar_test_package {

// Go runs an imported package's `init` before this package's own; .NET would never load
// an assembly nothing has touched yet, so that initialization is forced here.
[GoInit] internal static void initᴛᴛimportꓸnetꓸhttpꓸcookiejar() {
    builtin.initPackage(typeof(go.net.http.cookiejar_package));
}

[GoType] partial struct dummypsl {
    public cookiejar.PublicSuffixList List;
}

internal static @string PublicSuffix(this dummypsl _, @string domain) {
    return domain;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string dummyˢ = "dummy"u8;

internal static @string String(this dummypsl _) {
    return dummyˢ;
}

internal static dummypsl publicsuffix = new dummypsl(nil);

} // end cookiejar_test_package
