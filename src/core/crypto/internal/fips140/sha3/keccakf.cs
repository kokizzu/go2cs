// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.crypto.@internal.fips140;

using byteorder = go.crypto.@internal.fips140deps.byteorder_package;
using cpu = go.crypto.@internal.fips140deps.cpu_package;
using bits = math.bits_package;
using @unsafe = unsafe_package;
using go.crypto.@internal.fips140deps;
using math;

partial class sha3_package {

// rc stores the round constants for use in the ι step.
internal static array<uint64> rc = new uint64[]{
    0x0000000000000001,
    0x0000000000008082,
    0x800000000000808AUL,
    0x8000000080008000UL,
    0x000000000000808B,
    0x0000000080000001U,
    0x8000000080008081UL,
    0x8000000000008009UL,
    0x000000000000008A,
    0x0000000000000088,
    0x0000000080008009U,
    0x000000008000000AU,
    0x000000008000808BU,
    0x800000000000008BUL,
    0x8000000000008089UL,
    0x8000000000008003UL,
    0x8000000000008002UL,
    0x8000000000000080UL,
    0x000000000000800A,
    0x800000008000000AUL,
    0x8000000080008081UL,
    0x8000000000008080UL,
    0x0000000080000001U,
    0x8000000080008008UL
}.array();

// keccakF1600Generic applies the Keccak permutation.
internal static void keccakF1600Generic([GoArrayDims(200)] ж<array<byte>> Ꮡda) {
    GoFrame ᒐ = default;
    try {
        ref var da = ref Ꮡda.DerefOrNull();

        ref var a = ref heap<ж<array<uint64>>>(out var Ꮡa);
        if (cpu.BigEndian){
            a = Ꮡ(new array<uint64>(25));
            foreach (var (i, _) in a.Value) {
                a.Value[i] = byteorder.LEUint64(da[(int)(i * 8)..]);
            }
            defer(() => {
                foreach (var (i, _) in Ꮡa.ValueSlot.Value) {
                    byteorder.LEPutUint64(Ꮡda.Value[(int)(i * 8)..], Ꮡa.ValueSlot.Value[i]);
                }
            }, ref ᒐ);
        } else {
            a = (ж<array<uint64>>)(uintptr)(@unsafe.Pointer.FromPinnedBox(Ꮡda));
        }
        // Implementation translated from Keccak-inplace.c
        // in the keccak reference code.
        uint64 t = default!;
        uint64 bc0 = default!;
        uint64 bc1 = default!;
        uint64 bc2 = default!;
        uint64 bc3 = default!;
        uint64 bc4 = default!;
        uint64 d0 = default!;
        uint64 d1 = default!;
        uint64 d2 = default!;
        uint64 d3 = default!;
        uint64 d4 = default!;
        for (nint i = 0; i < 24; i += 4) {
            // Combines the 5 steps in each round into 2 steps.
            // Unrolls 4 rounds per loop and spreads some steps across rounds.
            // Round 1
            bc0 = (uint64)((uint64)((uint64)((uint64)(a.Value[0] ^ a.Value[5]) ^ a.Value[10]) ^ a.Value[15]) ^ a.Value[20]);
            bc1 = (uint64)((uint64)((uint64)((uint64)(a.Value[1] ^ a.Value[6]) ^ a.Value[11]) ^ a.Value[16]) ^ a.Value[21]);
            bc2 = (uint64)((uint64)((uint64)((uint64)(a.Value[2] ^ a.Value[7]) ^ a.Value[12]) ^ a.Value[17]) ^ a.Value[22]);
            bc3 = (uint64)((uint64)((uint64)((uint64)(a.Value[3] ^ a.Value[8]) ^ a.Value[13]) ^ a.Value[18]) ^ a.Value[23]);
            bc4 = (uint64)((uint64)((uint64)((uint64)(a.Value[4] ^ a.Value[9]) ^ a.Value[14]) ^ a.Value[19]) ^ a.Value[24]);
            d0 = (uint64)(bc4 ^ ((uint64)((bc1 << (int)(1)) | (bc1 >> (int)(63)))));
            d1 = (uint64)(bc0 ^ ((uint64)((bc2 << (int)(1)) | (bc2 >> (int)(63)))));
            d2 = (uint64)(bc1 ^ ((uint64)((bc3 << (int)(1)) | (bc3 >> (int)(63)))));
            d3 = (uint64)(bc2 ^ ((uint64)((bc4 << (int)(1)) | (bc4 >> (int)(63)))));
            d4 = (uint64)(bc3 ^ ((uint64)((bc0 << (int)(1)) | (bc0 >> (int)(63)))));
            bc0 = (uint64)(a.Value[0] ^ d0);
            t = (uint64)(a.Value[6] ^ d1);
            bc1 = bits.RotateLeft64(t, 44);
            t = (uint64)(a.Value[12] ^ d2);
            bc2 = bits.RotateLeft64(t, 43);
            t = (uint64)(a.Value[18] ^ d3);
            bc3 = bits.RotateLeft64(t, 21);
            t = (uint64)(a.Value[24] ^ d4);
            bc4 = bits.RotateLeft64(t, 14);
            a.Value[0] = (uint64)((uint64)(bc0 ^ ((uint64)(bc2 & ~bc1))) ^ rc[i]);
            a.Value[6] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[12] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[18] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[24] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[10] ^ d0);
            bc2 = bits.RotateLeft64(t, 3);
            t = (uint64)(a.Value[16] ^ d1);
            bc3 = bits.RotateLeft64(t, 45);
            t = (uint64)(a.Value[22] ^ d2);
            bc4 = bits.RotateLeft64(t, 61);
            t = (uint64)(a.Value[3] ^ d3);
            bc0 = bits.RotateLeft64(t, 28);
            t = (uint64)(a.Value[9] ^ d4);
            bc1 = bits.RotateLeft64(t, 20);
            a.Value[10] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[16] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[22] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[3] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[9] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[20] ^ d0);
            bc4 = bits.RotateLeft64(t, 18);
            t = (uint64)(a.Value[1] ^ d1);
            bc0 = bits.RotateLeft64(t, 1);
            t = (uint64)(a.Value[7] ^ d2);
            bc1 = bits.RotateLeft64(t, 6);
            t = (uint64)(a.Value[13] ^ d3);
            bc2 = bits.RotateLeft64(t, 25);
            t = (uint64)(a.Value[19] ^ d4);
            bc3 = bits.RotateLeft64(t, 8);
            a.Value[20] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[1] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[7] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[13] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[19] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[5] ^ d0);
            bc1 = bits.RotateLeft64(t, 36);
            t = (uint64)(a.Value[11] ^ d1);
            bc2 = bits.RotateLeft64(t, 10);
            t = (uint64)(a.Value[17] ^ d2);
            bc3 = bits.RotateLeft64(t, 15);
            t = (uint64)(a.Value[23] ^ d3);
            bc4 = bits.RotateLeft64(t, 56);
            t = (uint64)(a.Value[4] ^ d4);
            bc0 = bits.RotateLeft64(t, 27);
            a.Value[5] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[11] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[17] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[23] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[4] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[15] ^ d0);
            bc3 = bits.RotateLeft64(t, 41);
            t = (uint64)(a.Value[21] ^ d1);
            bc4 = bits.RotateLeft64(t, 2);
            t = (uint64)(a.Value[2] ^ d2);
            bc0 = bits.RotateLeft64(t, 62);
            t = (uint64)(a.Value[8] ^ d3);
            bc1 = bits.RotateLeft64(t, 55);
            t = (uint64)(a.Value[14] ^ d4);
            bc2 = bits.RotateLeft64(t, 39);
            a.Value[15] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[21] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[2] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[8] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[14] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            // Round 2
            bc0 = (uint64)((uint64)((uint64)((uint64)(a.Value[0] ^ a.Value[5]) ^ a.Value[10]) ^ a.Value[15]) ^ a.Value[20]);
            bc1 = (uint64)((uint64)((uint64)((uint64)(a.Value[1] ^ a.Value[6]) ^ a.Value[11]) ^ a.Value[16]) ^ a.Value[21]);
            bc2 = (uint64)((uint64)((uint64)((uint64)(a.Value[2] ^ a.Value[7]) ^ a.Value[12]) ^ a.Value[17]) ^ a.Value[22]);
            bc3 = (uint64)((uint64)((uint64)((uint64)(a.Value[3] ^ a.Value[8]) ^ a.Value[13]) ^ a.Value[18]) ^ a.Value[23]);
            bc4 = (uint64)((uint64)((uint64)((uint64)(a.Value[4] ^ a.Value[9]) ^ a.Value[14]) ^ a.Value[19]) ^ a.Value[24]);
            d0 = (uint64)(bc4 ^ ((uint64)((bc1 << (int)(1)) | (bc1 >> (int)(63)))));
            d1 = (uint64)(bc0 ^ ((uint64)((bc2 << (int)(1)) | (bc2 >> (int)(63)))));
            d2 = (uint64)(bc1 ^ ((uint64)((bc3 << (int)(1)) | (bc3 >> (int)(63)))));
            d3 = (uint64)(bc2 ^ ((uint64)((bc4 << (int)(1)) | (bc4 >> (int)(63)))));
            d4 = (uint64)(bc3 ^ ((uint64)((bc0 << (int)(1)) | (bc0 >> (int)(63)))));
            bc0 = (uint64)(a.Value[0] ^ d0);
            t = (uint64)(a.Value[16] ^ d1);
            bc1 = bits.RotateLeft64(t, 44);
            t = (uint64)(a.Value[7] ^ d2);
            bc2 = bits.RotateLeft64(t, 43);
            t = (uint64)(a.Value[23] ^ d3);
            bc3 = bits.RotateLeft64(t, 21);
            t = (uint64)(a.Value[14] ^ d4);
            bc4 = bits.RotateLeft64(t, 14);
            a.Value[0] = (uint64)((uint64)(bc0 ^ ((uint64)(bc2 & ~bc1))) ^ rc[i + 1]);
            a.Value[16] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[7] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[23] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[14] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[20] ^ d0);
            bc2 = bits.RotateLeft64(t, 3);
            t = (uint64)(a.Value[11] ^ d1);
            bc3 = bits.RotateLeft64(t, 45);
            t = (uint64)(a.Value[2] ^ d2);
            bc4 = bits.RotateLeft64(t, 61);
            t = (uint64)(a.Value[18] ^ d3);
            bc0 = bits.RotateLeft64(t, 28);
            t = (uint64)(a.Value[9] ^ d4);
            bc1 = bits.RotateLeft64(t, 20);
            a.Value[20] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[11] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[2] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[18] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[9] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[15] ^ d0);
            bc4 = bits.RotateLeft64(t, 18);
            t = (uint64)(a.Value[6] ^ d1);
            bc0 = bits.RotateLeft64(t, 1);
            t = (uint64)(a.Value[22] ^ d2);
            bc1 = bits.RotateLeft64(t, 6);
            t = (uint64)(a.Value[13] ^ d3);
            bc2 = bits.RotateLeft64(t, 25);
            t = (uint64)(a.Value[4] ^ d4);
            bc3 = bits.RotateLeft64(t, 8);
            a.Value[15] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[6] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[22] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[13] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[4] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[10] ^ d0);
            bc1 = bits.RotateLeft64(t, 36);
            t = (uint64)(a.Value[1] ^ d1);
            bc2 = bits.RotateLeft64(t, 10);
            t = (uint64)(a.Value[17] ^ d2);
            bc3 = bits.RotateLeft64(t, 15);
            t = (uint64)(a.Value[8] ^ d3);
            bc4 = bits.RotateLeft64(t, 56);
            t = (uint64)(a.Value[24] ^ d4);
            bc0 = bits.RotateLeft64(t, 27);
            a.Value[10] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[1] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[17] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[8] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[24] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[5] ^ d0);
            bc3 = bits.RotateLeft64(t, 41);
            t = (uint64)(a.Value[21] ^ d1);
            bc4 = bits.RotateLeft64(t, 2);
            t = (uint64)(a.Value[12] ^ d2);
            bc0 = bits.RotateLeft64(t, 62);
            t = (uint64)(a.Value[3] ^ d3);
            bc1 = bits.RotateLeft64(t, 55);
            t = (uint64)(a.Value[19] ^ d4);
            bc2 = bits.RotateLeft64(t, 39);
            a.Value[5] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[21] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[12] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[3] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[19] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            // Round 3
            bc0 = (uint64)((uint64)((uint64)((uint64)(a.Value[0] ^ a.Value[5]) ^ a.Value[10]) ^ a.Value[15]) ^ a.Value[20]);
            bc1 = (uint64)((uint64)((uint64)((uint64)(a.Value[1] ^ a.Value[6]) ^ a.Value[11]) ^ a.Value[16]) ^ a.Value[21]);
            bc2 = (uint64)((uint64)((uint64)((uint64)(a.Value[2] ^ a.Value[7]) ^ a.Value[12]) ^ a.Value[17]) ^ a.Value[22]);
            bc3 = (uint64)((uint64)((uint64)((uint64)(a.Value[3] ^ a.Value[8]) ^ a.Value[13]) ^ a.Value[18]) ^ a.Value[23]);
            bc4 = (uint64)((uint64)((uint64)((uint64)(a.Value[4] ^ a.Value[9]) ^ a.Value[14]) ^ a.Value[19]) ^ a.Value[24]);
            d0 = (uint64)(bc4 ^ ((uint64)((bc1 << (int)(1)) | (bc1 >> (int)(63)))));
            d1 = (uint64)(bc0 ^ ((uint64)((bc2 << (int)(1)) | (bc2 >> (int)(63)))));
            d2 = (uint64)(bc1 ^ ((uint64)((bc3 << (int)(1)) | (bc3 >> (int)(63)))));
            d3 = (uint64)(bc2 ^ ((uint64)((bc4 << (int)(1)) | (bc4 >> (int)(63)))));
            d4 = (uint64)(bc3 ^ ((uint64)((bc0 << (int)(1)) | (bc0 >> (int)(63)))));
            bc0 = (uint64)(a.Value[0] ^ d0);
            t = (uint64)(a.Value[11] ^ d1);
            bc1 = bits.RotateLeft64(t, 44);
            t = (uint64)(a.Value[22] ^ d2);
            bc2 = bits.RotateLeft64(t, 43);
            t = (uint64)(a.Value[8] ^ d3);
            bc3 = bits.RotateLeft64(t, 21);
            t = (uint64)(a.Value[19] ^ d4);
            bc4 = bits.RotateLeft64(t, 14);
            a.Value[0] = (uint64)((uint64)(bc0 ^ ((uint64)(bc2 & ~bc1))) ^ rc[i + 2]);
            a.Value[11] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[22] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[8] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[19] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[15] ^ d0);
            bc2 = bits.RotateLeft64(t, 3);
            t = (uint64)(a.Value[1] ^ d1);
            bc3 = bits.RotateLeft64(t, 45);
            t = (uint64)(a.Value[12] ^ d2);
            bc4 = bits.RotateLeft64(t, 61);
            t = (uint64)(a.Value[23] ^ d3);
            bc0 = bits.RotateLeft64(t, 28);
            t = (uint64)(a.Value[9] ^ d4);
            bc1 = bits.RotateLeft64(t, 20);
            a.Value[15] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[1] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[12] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[23] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[9] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[5] ^ d0);
            bc4 = bits.RotateLeft64(t, 18);
            t = (uint64)(a.Value[16] ^ d1);
            bc0 = bits.RotateLeft64(t, 1);
            t = (uint64)(a.Value[2] ^ d2);
            bc1 = bits.RotateLeft64(t, 6);
            t = (uint64)(a.Value[13] ^ d3);
            bc2 = bits.RotateLeft64(t, 25);
            t = (uint64)(a.Value[24] ^ d4);
            bc3 = bits.RotateLeft64(t, 8);
            a.Value[5] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[16] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[2] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[13] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[24] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[20] ^ d0);
            bc1 = bits.RotateLeft64(t, 36);
            t = (uint64)(a.Value[6] ^ d1);
            bc2 = bits.RotateLeft64(t, 10);
            t = (uint64)(a.Value[17] ^ d2);
            bc3 = bits.RotateLeft64(t, 15);
            t = (uint64)(a.Value[3] ^ d3);
            bc4 = bits.RotateLeft64(t, 56);
            t = (uint64)(a.Value[14] ^ d4);
            bc0 = bits.RotateLeft64(t, 27);
            a.Value[20] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[6] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[17] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[3] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[14] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[10] ^ d0);
            bc3 = bits.RotateLeft64(t, 41);
            t = (uint64)(a.Value[21] ^ d1);
            bc4 = bits.RotateLeft64(t, 2);
            t = (uint64)(a.Value[7] ^ d2);
            bc0 = bits.RotateLeft64(t, 62);
            t = (uint64)(a.Value[18] ^ d3);
            bc1 = bits.RotateLeft64(t, 55);
            t = (uint64)(a.Value[4] ^ d4);
            bc2 = bits.RotateLeft64(t, 39);
            a.Value[10] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[21] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[7] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[18] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[4] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            // Round 4
            bc0 = (uint64)((uint64)((uint64)((uint64)(a.Value[0] ^ a.Value[5]) ^ a.Value[10]) ^ a.Value[15]) ^ a.Value[20]);
            bc1 = (uint64)((uint64)((uint64)((uint64)(a.Value[1] ^ a.Value[6]) ^ a.Value[11]) ^ a.Value[16]) ^ a.Value[21]);
            bc2 = (uint64)((uint64)((uint64)((uint64)(a.Value[2] ^ a.Value[7]) ^ a.Value[12]) ^ a.Value[17]) ^ a.Value[22]);
            bc3 = (uint64)((uint64)((uint64)((uint64)(a.Value[3] ^ a.Value[8]) ^ a.Value[13]) ^ a.Value[18]) ^ a.Value[23]);
            bc4 = (uint64)((uint64)((uint64)((uint64)(a.Value[4] ^ a.Value[9]) ^ a.Value[14]) ^ a.Value[19]) ^ a.Value[24]);
            d0 = (uint64)(bc4 ^ ((uint64)((bc1 << (int)(1)) | (bc1 >> (int)(63)))));
            d1 = (uint64)(bc0 ^ ((uint64)((bc2 << (int)(1)) | (bc2 >> (int)(63)))));
            d2 = (uint64)(bc1 ^ ((uint64)((bc3 << (int)(1)) | (bc3 >> (int)(63)))));
            d3 = (uint64)(bc2 ^ ((uint64)((bc4 << (int)(1)) | (bc4 >> (int)(63)))));
            d4 = (uint64)(bc3 ^ ((uint64)((bc0 << (int)(1)) | (bc0 >> (int)(63)))));
            bc0 = (uint64)(a.Value[0] ^ d0);
            t = (uint64)(a.Value[1] ^ d1);
            bc1 = bits.RotateLeft64(t, 44);
            t = (uint64)(a.Value[2] ^ d2);
            bc2 = bits.RotateLeft64(t, 43);
            t = (uint64)(a.Value[3] ^ d3);
            bc3 = bits.RotateLeft64(t, 21);
            t = (uint64)(a.Value[4] ^ d4);
            bc4 = bits.RotateLeft64(t, 14);
            a.Value[0] = (uint64)((uint64)(bc0 ^ ((uint64)(bc2 & ~bc1))) ^ rc[i + 3]);
            a.Value[1] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[2] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[3] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[4] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[5] ^ d0);
            bc2 = bits.RotateLeft64(t, 3);
            t = (uint64)(a.Value[6] ^ d1);
            bc3 = bits.RotateLeft64(t, 45);
            t = (uint64)(a.Value[7] ^ d2);
            bc4 = bits.RotateLeft64(t, 61);
            t = (uint64)(a.Value[8] ^ d3);
            bc0 = bits.RotateLeft64(t, 28);
            t = (uint64)(a.Value[9] ^ d4);
            bc1 = bits.RotateLeft64(t, 20);
            a.Value[5] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[6] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[7] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[8] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[9] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[10] ^ d0);
            bc4 = bits.RotateLeft64(t, 18);
            t = (uint64)(a.Value[11] ^ d1);
            bc0 = bits.RotateLeft64(t, 1);
            t = (uint64)(a.Value[12] ^ d2);
            bc1 = bits.RotateLeft64(t, 6);
            t = (uint64)(a.Value[13] ^ d3);
            bc2 = bits.RotateLeft64(t, 25);
            t = (uint64)(a.Value[14] ^ d4);
            bc3 = bits.RotateLeft64(t, 8);
            a.Value[10] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[11] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[12] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[13] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[14] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[15] ^ d0);
            bc1 = bits.RotateLeft64(t, 36);
            t = (uint64)(a.Value[16] ^ d1);
            bc2 = bits.RotateLeft64(t, 10);
            t = (uint64)(a.Value[17] ^ d2);
            bc3 = bits.RotateLeft64(t, 15);
            t = (uint64)(a.Value[18] ^ d3);
            bc4 = bits.RotateLeft64(t, 56);
            t = (uint64)(a.Value[19] ^ d4);
            bc0 = bits.RotateLeft64(t, 27);
            a.Value[15] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[16] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[17] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[18] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[19] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
            t = (uint64)(a.Value[20] ^ d0);
            bc3 = bits.RotateLeft64(t, 41);
            t = (uint64)(a.Value[21] ^ d1);
            bc4 = bits.RotateLeft64(t, 2);
            t = (uint64)(a.Value[22] ^ d2);
            bc0 = bits.RotateLeft64(t, 62);
            t = (uint64)(a.Value[23] ^ d3);
            bc1 = bits.RotateLeft64(t, 55);
            t = (uint64)(a.Value[24] ^ d4);
            bc2 = bits.RotateLeft64(t, 39);
            a.Value[20] = (uint64)(bc0 ^ ((uint64)(bc2 & ~bc1)));
            a.Value[21] = (uint64)(bc1 ^ ((uint64)(bc3 & ~bc2)));
            a.Value[22] = (uint64)(bc2 ^ ((uint64)(bc4 & ~bc3)));
            a.Value[23] = (uint64)(bc3 ^ ((uint64)(bc0 & ~bc4)));
            a.Value[24] = (uint64)(bc4 ^ ((uint64)(bc1 & ~bc0)));
        }
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

} // end sha3_package
