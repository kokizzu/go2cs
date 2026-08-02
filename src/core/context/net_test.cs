// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go;

using context = context_package;
using Δnet = net_package;
using testing = testing_package;
using static go.context_internal_test_package;

partial class context_test_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly object deadlineExceededDoesNotˢ = (@string)"DeadlineExceeded does not implement net.Error"u8;

public static void TestDeadlineExceededIsNetError(ж<testing.T> Ꮡt) {
    var (err, ok) = context.DeadlineExceeded._<netꓸError>(ᐧ);
    if (!ok) {
        Ꮡt.Fatal(deadlineExceededDoesNotˢ);
    }
    if (!err.Timeout() || !err.Temporary()) {
        Ꮡt.Fatalf("Timeout() = %v, Temporary() = %v, want true, true"u8, err.Timeout(), err.Temporary());
    }
}

} // end context_test_package
