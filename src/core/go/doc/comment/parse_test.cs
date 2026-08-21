// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: global::go.GoPositionMap("go/doc/comment/parse_test.go", "parse_test.cs", "AAwUkg==")]

namespace go.go.doc;

using testing = testing_package;
using static global::go.go.doc.comment_package;

partial class comment_internal_test_package {

// See https://golang.org/issue/52353
public static void Test52353(ж<testing.T> Ꮡt) {
    ident("𫕐ﯯ"u8);
}

} // end comment_internal_test_package
