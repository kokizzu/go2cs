// Copyright 2013 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

// Exported for testing only.
using utf8 = unicode.utf8_package;
using unicode;

partial class bufio_package {

public static Func<rune, bool> IsSpace = isSpace;

public static UntypedInt DefaultBufSize => /* defaultBufSize */ 4096;

[GoRecv] public static void MaxTokenSize(this ref Scanner s, nint n) {
    if (n < utf8.UTFMax || n > 1000000000) {
        throw panic("bad max token size");
    }
    if (n < len(s.buf)) {
        s.buf = new slice<byte>(n);
    }
    s.maxTokenSize = n;
}

// ErrOrEOF is like Err, but returns EOF. Used to test a corner case.
[GoRecv] public static error ErrOrEOF(this ref Scanner s) {
    return s.err;
}

} // end bufio_package
