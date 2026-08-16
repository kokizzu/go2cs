// Copyright 2015 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// go2cs NATIVE IMPLEMENTATION (hand-owned; replaces the converted xor.go output).
// Both functions are the converted output verbatim EXCEPT for the little-endian branch's
// reinterpret, which cannot work as literally converted. Go views the sponge state as raw bytes —
//
//     ab := (*[25 * 64 / 8]byte)(unsafe.Pointer(&d.a))
//
// — and a `byte[]` view over a `uint64[]` does not exist in the managed model, exactly as a
// `uintptr[]` view over a `byte[]` does not (crypto/subtle's xor_generic.cs, the sibling case).
// golib's `array<T>`/`slice<T>` are windows on a real `T[]`, so the converted form takes the
// raw-ADDRESS route instead: `(ж<array<byte>>)(uintptr)(new unsafe.Pointer(Ꮡd.of(state.Ꮡa)))`
// builds a NATIVE-backed pointer, and dereferencing it reads an `array<byte>` STRUCT — a backing
// reference plus bounds — out of the keccak state's own DATA. That is a fabricated managed
// reference: `copy(b, (~ab)[..])` faulted with an uncatchable AccessViolationException inside
// `slice<byte>`'s constructor, killing the process. Reached from crypto/tls through
// mlkem768.NewKeyFromSeed -> kemKeyGen -> sha3.Sum512, i.e. on every TLS 1.3 ClientHello.
//
// The reinterpret is taken over the array's own SPAN instead: MemoryMarshal.AsBytes is a genuine
// ALIASING view of the same backing storage, so the XOR lands in the real state and the copy reads
// the real state. Go's big-endian branch is unchanged (it never reinterprets), and the two branches
// remain the same computation the Go source says they are.

using System.Runtime.InteropServices;
using go;

[module: GoManualConversion]

namespace go.vendor.golang.org.x.crypto;

using binary = encoding.binary_package;
using cpu = go.vendor.golang.org.x.sys.cpu_package;
using encoding;
using go.vendor.golang.org.x.sys;

partial class sha3_package {

// xorIn xors the bytes in buf into the state.
internal static void xorIn(ж<state> Ꮡd, slice<byte> buf) {
    ref var d = ref Ꮡd.DerefOrNull();

    if (cpu.IsBigEndian){
        for (nint i = 0; len(buf) >= 8; i++) {
            var a = binary.LittleEndian.Uint64(buf);
            d.a[i] ^= (uint64)(a);
            buf = buf[8..];
        }
    } else {
        // Go: subtle.XORBytes(ab[:], ab[:], buf) over the reinterpreted state (see the header).
        // Word-at-a-time for the bulk and a byte tail for the remainder — the shape XORBytes
        // itself uses, and the shape crypto/subtle's hand-owned xorWords/xorLoop pair uses.
        Span<byte> ab = MemoryMarshal.AsBytes(d.a.ToSpan());
        Span<byte> x = buf.ToSpan();
        int n = Math.Min(ab.Length, x.Length);
        Span<uint64> abWords = MemoryMarshal.Cast<byte, uint64>(ab[..n]);
        Span<uint64> xWords = MemoryMarshal.Cast<byte, uint64>(x[..n]);
        for (int i = 0; i < abWords.Length; i++) {
            abWords[i] ^= xWords[i];
        }
        for (int i = abWords.Length * 8; i < n; i++) {
            ab[i] ^= x[i];
        }
    }
}

// copyOut copies uint64s to a byte buffer.
internal static void copyOut(ж<state> Ꮡd, slice<byte> b) {
    ref var d = ref Ꮡd.DerefOrNull();

    if (cpu.IsBigEndian){
        for (nint i = 0; len(b) >= 8; i++) {
            binary.LittleEndian.PutUint64(b, d.a[i]);
            b = b[8..];
        }
    } else {
        // Go: copy(b, ab[:]) from the reinterpreted state (see the header).
        Span<byte> ab = MemoryMarshal.AsBytes(d.a.ToSpan());
        Span<byte> dst = b.ToSpan();
        int n = Math.Min(ab.Length, dst.Length);
        ab[..n].CopyTo(dst[..n]);
    }
}

} // end sha3_package
