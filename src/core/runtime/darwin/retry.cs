// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build unix
namespace go;

partial class runtime_package {

// retryOnEAGAIN retries a function until it does not return EAGAIN.
// It will use an increasing delay between calls, and retry up to 20 times.
// The function argument is expected to return an errno value,
// and retryOnEAGAIN will return any errno value other than EAGAIN.
// If all retries return EAGAIN, then retryOnEAGAIN will return EAGAIN.
internal static int32 retryOnEAGAIN(Func<int32> fn) {
    for (nint tries = 0; tries < 20; tries++) {
        var errno = fn();
        if (errno != _EAGAIN) {
            return errno;
        }
        usleep_no_g((uint32)(tries + 1) * 1000); // milliseconds
    }
    return _EAGAIN;
}

} // end runtime_package
