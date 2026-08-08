// Copyright 2024 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build aix || darwin || (openbsd && !mips64) || solaris
namespace go;

// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards)

partial class syscall_package {

// used by internal/poll
//go:linkname writev

} // end syscall_package
