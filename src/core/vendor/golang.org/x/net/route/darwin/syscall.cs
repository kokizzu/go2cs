// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build darwin || dragonfly || freebsd || netbsd || openbsd
namespace go.vendor.golang.org.x.net;

// blank import: unsafe_package (side effects only; no using emitted — a `using _` alias hijacks C# discards) // for linkname

partial class route_package {

//go:linkname sysctl syscall.sysctl
internal static partial error sysctl(slice<int32> mib, ж<byte> old, ж<uintptr> oldlen, ж<byte> @new, uintptr newlen);

} // end route_package
