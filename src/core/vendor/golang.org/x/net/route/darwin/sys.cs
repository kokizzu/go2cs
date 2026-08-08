// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build darwin || dragonfly || freebsd || netbsd || openbsd
namespace go.vendor.golang.org.x.net;

using syscall = syscall_package;
using @unsafe = unsafe_package;

partial class route_package {

internal static binaryByteOrder nativeEndian;
internal static nint kernelAlign;
internal static byte rtmVersion;
internal static map<nint, ж<wireFormat>> wireFormats;

[GoInit] internal static void init() {
    ref var i = ref heap<uint32>(out var Ꮡi);
    i = (uint32)1;
    var b = (ж<array<byte>>)(uintptr)(new @unsafe.Pointer(Ꮡi));
    if (b.Value[0] == 1){
        nativeEndian = littleEndian;
    } else {
        nativeEndian = bigEndian;
    }
    // might get overridden in probeRoutingStack
    rtmVersion = syscall.RTM_VERSION;
    (kernelAlign, wireFormats) = probeRoutingStack();
}

internal static nint roundup(nint l) {
    if (l == 0) {
        return kernelAlign;
    }
    return (nint)((l + kernelAlign - 1) & ~(kernelAlign - 1));
}

[GoType] partial struct wireFormat {
    internal nint extOff; // offset of header extension
    internal nint bodyOff; // offset of message body
    internal Func<RIBType, slice<byte>, (Message, error)> parse;
}

} // end route_package
