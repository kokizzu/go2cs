// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package flate -- go2cs converted at 2022 March 13 05:29:10 UTC
// import "compress/flate" ==> using flate = go.compress.flate_package
// Original source: C:\Program Files\Go\src\compress\flate\huffman_bit_writer.go
namespace go.compress;

using io = io_package;

public static partial class flate_package {

 
// The largest offset code.
private static readonly nint offsetCodeCount = 30; 

// The special code used to mark the end of a block.
private static readonly nint endBlockMarker = 256; 

// The first length code.
private static readonly nint lengthCodesStart = 257; 

// The number of codegen codes.
private static readonly nint codegenCodeCount = 19;
private static readonly nint badCode = 255; 

// bufferFlushSize indicates the buffer size
// after which bytes are flushed to the writer.
// Should preferably be a multiple of 6, since
// we accumulate 6 bytes between writes to the buffer.
private static readonly nint bufferFlushSize = 240; 

// bufferSize is the actual output byte buffer size.
// It must have additional headroom for a flush
// which can contain up to 8 bytes.
private static readonly var bufferSize = bufferFlushSize + 8;

// The number of extra bits needed by length code X - LENGTH_CODES_START.
private static sbyte lengthExtraBits = new slice<sbyte>(new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 });

// The length indicated by length code X - LENGTH_CODES_START.
private static uint lengthBase = new slice<uint>(new uint[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 255 });

// offset code word extra bits.
private static sbyte offsetExtraBits = new slice<sbyte>(new sbyte[] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 });

private static uint offsetBase = new slice<uint>(new uint[] { 0x000000, 0x000001, 0x000002, 0x000003, 0x000004, 0x000006, 0x000008, 0x00000c, 0x000010, 0x000018, 0x000020, 0x000030, 0x000040, 0x000060, 0x000080, 0x0000c0, 0x000100, 0x000180, 0x000200, 0x000300, 0x000400, 0x000600, 0x000800, 0x000c00, 0x001000, 0x001800, 0x002000, 0x003000, 0x004000, 0x006000 });

// The odd order in which the codegen code sizes are written.
private static uint codegenOrder = new slice<uint>(new uint[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 });

private partial struct huffmanBitWriter {
    public io.Writer writer; // Data waiting to be written is bytes[0:nbytes]
// and then the low nbits of bits.  Data is always written
// sequentially into the bytes array.
    public ulong bits;
    public nuint nbits;
    public array<byte> bytes;
    public array<int> codegenFreq;
    public nint nbytes;
    public slice<int> literalFreq;
    public slice<int> offsetFreq;
    public slice<byte> codegen;
    public ptr<huffmanEncoder> literalEncoding;
    public ptr<huffmanEncoder> offsetEncoding;
    public ptr<huffmanEncoder> codegenEncoding;
    public error err;
}

private static ptr<huffmanBitWriter> newHuffmanBitWriter(io.Writer w) {
    return addr(new huffmanBitWriter(writer:w,literalFreq:make([]int32,maxNumLit),offsetFreq:make([]int32,offsetCodeCount),codegen:make([]uint8,maxNumLit+offsetCodeCount+1),literalEncoding:newHuffmanEncoder(maxNumLit),codegenEncoding:newHuffmanEncoder(codegenCodeCount),offsetEncoding:newHuffmanEncoder(offsetCodeCount),));
}

private static void reset(this ptr<huffmanBitWriter> _addr_w, io.Writer writer) {
    ref huffmanBitWriter w = ref _addr_w.val;

    w.writer = writer;
    (w.bits, w.nbits, w.nbytes, w.err) = (0, 0, 0, null);
}

private static void flush(this ptr<huffmanBitWriter> _addr_w) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        w.nbits = 0;
        return ;
    }
    var n = w.nbytes;
    while (w.nbits != 0) {
        w.bytes[n] = byte(w.bits);
        w.bits>>=8;
        if (w.nbits > 8) { // Avoid underflow
            w.nbits -= 8;
        }
        else
 {
            w.nbits = 0;
        }
        n++;
    }
    w.bits = 0;
    w.write(w.bytes[..(int)n]);
    w.nbytes = 0;
}

private static void write(this ptr<huffmanBitWriter> _addr_w, slice<byte> b) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    _, w.err = w.writer.Write(b);
}

private static void writeBits(this ptr<huffmanBitWriter> _addr_w, int b, nuint nb) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    w.bits |= uint64(b) << (int)(w.nbits);
    w.nbits += nb;
    if (w.nbits >= 48) {
        var bits = w.bits;
        w.bits>>=48;
        w.nbits -= 48;
        var n = w.nbytes;
        var bytes = w.bytes[(int)n..(int)n + 6];
        bytes[0] = byte(bits);
        bytes[1] = byte(bits >> 8);
        bytes[2] = byte(bits >> 16);
        bytes[3] = byte(bits >> 24);
        bytes[4] = byte(bits >> 32);
        bytes[5] = byte(bits >> 40);
        n += 6;
        if (n >= bufferFlushSize) {
            w.write(w.bytes[..(int)n]);
            n = 0;
        }
        w.nbytes = n;
    }
}

private static void writeBytes(this ptr<huffmanBitWriter> _addr_w, slice<byte> bytes) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    var n = w.nbytes;
    if (w.nbits & 7 != 0) {
        w.err = InternalError("writeBytes with unfinished bits");
        return ;
    }
    while (w.nbits != 0) {
        w.bytes[n] = byte(w.bits);
        w.bits>>=8;
        w.nbits -= 8;
        n++;
    }
    if (n != 0) {
        w.write(w.bytes[..(int)n]);
    }
    w.nbytes = 0;
    w.write(bytes);
}

// RFC 1951 3.2.7 specifies a special run-length encoding for specifying
// the literal and offset lengths arrays (which are concatenated into a single
// array).  This method generates that run-length encoding.
//
// The result is written into the codegen array, and the frequencies
// of each code is written into the codegenFreq array.
// Codes 0-15 are single byte codes. Codes 16-18 are followed by additional
// information. Code badCode is an end marker
//
//  numLiterals      The number of literals in literalEncoding
//  numOffsets       The number of offsets in offsetEncoding
//  litenc, offenc   The literal and offset encoder to use
private static void generateCodegen(this ptr<huffmanBitWriter> _addr_w, nint numLiterals, nint numOffsets, ptr<huffmanEncoder> _addr_litEnc, ptr<huffmanEncoder> _addr_offEnc) {
    ref huffmanBitWriter w = ref _addr_w.val;
    ref huffmanEncoder litEnc = ref _addr_litEnc.val;
    ref huffmanEncoder offEnc = ref _addr_offEnc.val;

    {
        var i__prev1 = i;

        foreach (var (__i) in w.codegenFreq) {
            i = __i;
            w.codegenFreq[i] = 0;
        }
        i = i__prev1;
    }

    var codegen = w.codegen; // cache
    // Copy the concatenated code sizes to codegen. Put a marker at the end.
    var cgnl = codegen[..(int)numLiterals];
    {
        var i__prev1 = i;

        foreach (var (__i) in cgnl) {
            i = __i;
            cgnl[i] = uint8(litEnc.codes[i].len);
        }
        i = i__prev1;
    }

    cgnl = codegen[(int)numLiterals..(int)numLiterals + numOffsets];
    {
        var i__prev1 = i;

        foreach (var (__i) in cgnl) {
            i = __i;
            cgnl[i] = uint8(offEnc.codes[i].len);
        }
        i = i__prev1;
    }

    codegen[numLiterals + numOffsets] = badCode;

    var size = codegen[0];
    nint count = 1;
    nint outIndex = 0;
    for (nint inIndex = 1; size != badCode; inIndex++) { 
        // INVARIANT: We have seen "count" copies of size that have not yet
        // had output generated for them.
        var nextSize = codegen[inIndex];
        if (nextSize == size) {
            count++;
            continue;
        }
        if (size != 0) {
            codegen[outIndex] = size;
            outIndex++;
            w.codegenFreq[size]++;
            count--;
            while (count >= 3) {
                nint n = 6;
                if (n > count) {
                    n = count;
                }
                codegen[outIndex] = 16;
                outIndex++;
                codegen[outIndex] = uint8(n - 3);
                outIndex++;
                w.codegenFreq[16]++;
                count -= n;
            }
        else
        } {
            while (count >= 11) {
                n = 138;
                if (n > count) {
                    n = count;
                }
                codegen[outIndex] = 18;
                outIndex++;
                codegen[outIndex] = uint8(n - 11);
                outIndex++;
                w.codegenFreq[18]++;
                count -= n;
            }

            if (count >= 3) { 
                // count >= 3 && count <= 10
                codegen[outIndex] = 17;
                outIndex++;
                codegen[outIndex] = uint8(count - 3);
                outIndex++;
                w.codegenFreq[17]++;
                count = 0;
            }
        }
        count--;
        while (count >= 0) {
            codegen[outIndex] = size;
            outIndex++;
            w.codegenFreq[size]++;
            count--;
        } 
        // Set up invariant for next time through the loop.
        size = nextSize;
        count = 1;
    } 
    // Marker indicating the end of the codegen.
    codegen[outIndex] = badCode;
}

// dynamicSize returns the size of dynamically encoded data in bits.
private static (nint, nint) dynamicSize(this ptr<huffmanBitWriter> _addr_w, ptr<huffmanEncoder> _addr_litEnc, ptr<huffmanEncoder> _addr_offEnc, nint extraBits) {
    nint size = default;
    nint numCodegens = default;
    ref huffmanBitWriter w = ref _addr_w.val;
    ref huffmanEncoder litEnc = ref _addr_litEnc.val;
    ref huffmanEncoder offEnc = ref _addr_offEnc.val;

    numCodegens = len(w.codegenFreq);
    while (numCodegens > 4 && w.codegenFreq[codegenOrder[numCodegens - 1]] == 0) {
        numCodegens--;
    }
    nint header = 3 + 5 + 5 + 4 + (3 * numCodegens) + w.codegenEncoding.bitLength(w.codegenFreq[..]) + int(w.codegenFreq[16]) * 2 + int(w.codegenFreq[17]) * 3 + int(w.codegenFreq[18]) * 7;
    size = header + litEnc.bitLength(w.literalFreq) + offEnc.bitLength(w.offsetFreq) + extraBits;

    return (size, numCodegens);
}

// fixedSize returns the size of dynamically encoded data in bits.
private static nint fixedSize(this ptr<huffmanBitWriter> _addr_w, nint extraBits) {
    ref huffmanBitWriter w = ref _addr_w.val;

    return 3 + fixedLiteralEncoding.bitLength(w.literalFreq) + fixedOffsetEncoding.bitLength(w.offsetFreq) + extraBits;
}

// storedSize calculates the stored size, including header.
// The function returns the size in bits and whether the block
// fits inside a single block.
private static (nint, bool) storedSize(this ptr<huffmanBitWriter> _addr_w, slice<byte> @in) {
    nint _p0 = default;
    bool _p0 = default;
    ref huffmanBitWriter w = ref _addr_w.val;

    if (in == null) {
        return (0, false);
    }
    if (len(in) <= maxStoreBlockSize) {
        return ((len(in) + 5) * 8, true);
    }
    return (0, false);
}

private static void writeCode(this ptr<huffmanBitWriter> _addr_w, hcode c) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    w.bits |= uint64(c.code) << (int)(w.nbits);
    w.nbits += uint(c.len);
    if (w.nbits >= 48) {
        var bits = w.bits;
        w.bits>>=48;
        w.nbits -= 48;
        var n = w.nbytes;
        var bytes = w.bytes[(int)n..(int)n + 6];
        bytes[0] = byte(bits);
        bytes[1] = byte(bits >> 8);
        bytes[2] = byte(bits >> 16);
        bytes[3] = byte(bits >> 24);
        bytes[4] = byte(bits >> 32);
        bytes[5] = byte(bits >> 40);
        n += 6;
        if (n >= bufferFlushSize) {
            w.write(w.bytes[..(int)n]);
            n = 0;
        }
        w.nbytes = n;
    }
}

// Write the header of a dynamic Huffman block to the output stream.
//
//  numLiterals  The number of literals specified in codegen
//  numOffsets   The number of offsets specified in codegen
//  numCodegens  The number of codegens used in codegen
private static void writeDynamicHeader(this ptr<huffmanBitWriter> _addr_w, nint numLiterals, nint numOffsets, nint numCodegens, bool isEof) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    int firstBits = 4;
    if (isEof) {
        firstBits = 5;
    }
    w.writeBits(firstBits, 3);
    w.writeBits(int32(numLiterals - 257), 5);
    w.writeBits(int32(numOffsets - 1), 5);
    w.writeBits(int32(numCodegens - 4), 4);

    {
        nint i__prev1 = i;

        for (nint i = 0; i < numCodegens; i++) {
            var value = uint(w.codegenEncoding.codes[codegenOrder[i]].len);
            w.writeBits(int32(value), 3);
        }

        i = i__prev1;
    }

    i = 0;
    while (true) {
        nint codeWord = int(w.codegen[i]);
        i++;
        if (codeWord == badCode) {
            break;
        }
        w.writeCode(w.codegenEncoding.codes[uint32(codeWord)]);

        switch (codeWord) {
            case 16: 
                w.writeBits(int32(w.codegen[i]), 2);
                i++;
                break;
                break;
            case 17: 
                w.writeBits(int32(w.codegen[i]), 3);
                i++;
                break;
                break;
            case 18: 
                w.writeBits(int32(w.codegen[i]), 7);
                i++;
                break;
                break;
        }
    }
}

private static void writeStoredHeader(this ptr<huffmanBitWriter> _addr_w, nint length, bool isEof) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    int flag = default;
    if (isEof) {
        flag = 1;
    }
    w.writeBits(flag, 3);
    w.flush();
    w.writeBits(int32(length), 16);
    w.writeBits(int32(~uint16(length)), 16);
}

private static void writeFixedHeader(this ptr<huffmanBitWriter> _addr_w, bool isEof) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    int value = 2;
    if (isEof) {
        value = 3;
    }
    w.writeBits(value, 3);
}

// writeBlock will write a block of tokens with the smallest encoding.
// The original input can be supplied, and if the huffman encoded data
// is larger than the original bytes, the data will be written as a
// stored block.
// If the input is nil, the tokens will always be Huffman encoded.
private static void writeBlock(this ptr<huffmanBitWriter> _addr_w, slice<token> tokens, bool eof, slice<byte> input) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    tokens = append(tokens, endBlockMarker);
    var (numLiterals, numOffsets) = w.indexTokens(tokens);

    nint extraBits = default;
    var (storedSize, storable) = w.storedSize(input);
    if (storable) { 
        // We only bother calculating the costs of the extra bits required by
        // the length of offset fields (which will be the same for both fixed
        // and dynamic encoding), if we need to compare those two encodings
        // against stored encoding.
        for (var lengthCode = lengthCodesStart + 8; lengthCode < numLiterals; lengthCode++) { 
            // First eight length codes have extra size = 0.
            extraBits += int(w.literalFreq[lengthCode]) * int(lengthExtraBits[lengthCode - lengthCodesStart]);
        }
        for (nint offsetCode = 4; offsetCode < numOffsets; offsetCode++) { 
            // First four offset codes have extra size = 0.
            extraBits += int(w.offsetFreq[offsetCode]) * int(offsetExtraBits[offsetCode]);
        }
    }
    var literalEncoding = fixedLiteralEncoding;
    var offsetEncoding = fixedOffsetEncoding;
    var size = w.fixedSize(extraBits); 

    // Dynamic Huffman?
    nint numCodegens = default; 

    // Generate codegen and codegenFrequencies, which indicates how to encode
    // the literalEncoding and the offsetEncoding.
    w.generateCodegen(numLiterals, numOffsets, w.literalEncoding, w.offsetEncoding);
    w.codegenEncoding.generate(w.codegenFreq[..], 7);
    var (dynamicSize, numCodegens) = w.dynamicSize(w.literalEncoding, w.offsetEncoding, extraBits);

    if (dynamicSize < size) {
        size = dynamicSize;
        literalEncoding = w.literalEncoding;
        offsetEncoding = w.offsetEncoding;
    }
    if (storable && storedSize < size) {
        w.writeStoredHeader(len(input), eof);
        w.writeBytes(input);
        return ;
    }
    if (literalEncoding == fixedLiteralEncoding) {
        w.writeFixedHeader(eof);
    }
    else
 {
        w.writeDynamicHeader(numLiterals, numOffsets, numCodegens, eof);
    }
    w.writeTokens(tokens, literalEncoding.codes, offsetEncoding.codes);
}

// writeBlockDynamic encodes a block using a dynamic Huffman table.
// This should be used if the symbols used have a disproportionate
// histogram distribution.
// If input is supplied and the compression savings are below 1/16th of the
// input size the block is stored.
private static void writeBlockDynamic(this ptr<huffmanBitWriter> _addr_w, slice<token> tokens, bool eof, slice<byte> input) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    tokens = append(tokens, endBlockMarker);
    var (numLiterals, numOffsets) = w.indexTokens(tokens); 

    // Generate codegen and codegenFrequencies, which indicates how to encode
    // the literalEncoding and the offsetEncoding.
    w.generateCodegen(numLiterals, numOffsets, w.literalEncoding, w.offsetEncoding);
    w.codegenEncoding.generate(w.codegenFreq[..], 7);
    var (size, numCodegens) = w.dynamicSize(w.literalEncoding, w.offsetEncoding, 0); 

    // Store bytes, if we don't get a reasonable improvement.
    {
        var (ssize, storable) = w.storedSize(input);

        if (storable && ssize < (size + size >> 4)) {
            w.writeStoredHeader(len(input), eof);
            w.writeBytes(input);
            return ;
        }
    } 

    // Write Huffman table.
    w.writeDynamicHeader(numLiterals, numOffsets, numCodegens, eof); 

    // Write the tokens.
    w.writeTokens(tokens, w.literalEncoding.codes, w.offsetEncoding.codes);
}

// indexTokens indexes a slice of tokens, and updates
// literalFreq and offsetFreq, and generates literalEncoding
// and offsetEncoding.
// The number of literal and offset tokens is returned.
private static (nint, nint) indexTokens(this ptr<huffmanBitWriter> _addr_w, slice<token> tokens) {
    nint numLiterals = default;
    nint numOffsets = default;
    ref huffmanBitWriter w = ref _addr_w.val;

    {
        var i__prev1 = i;

        foreach (var (__i) in w.literalFreq) {
            i = __i;
            w.literalFreq[i] = 0;
        }
        i = i__prev1;
    }

    {
        var i__prev1 = i;

        foreach (var (__i) in w.offsetFreq) {
            i = __i;
            w.offsetFreq[i] = 0;
        }
        i = i__prev1;
    }

    foreach (var (_, t) in tokens) {
        if (t < matchType) {
            w.literalFreq[t.literal()]++;
            continue;
        }
        var length = t.length();
        var offset = t.offset();
        w.literalFreq[lengthCodesStart + lengthCode(length)]++;
        w.offsetFreq[offsetCode(offset)]++;
    }    numLiterals = len(w.literalFreq);
    while (w.literalFreq[numLiterals - 1] == 0) {
        numLiterals--;
    } 
    // get the number of offsets
    numOffsets = len(w.offsetFreq);
    while (numOffsets > 0 && w.offsetFreq[numOffsets - 1] == 0) {
        numOffsets--;
    }
    if (numOffsets == 0) { 
        // We haven't found a single match. If we want to go with the dynamic encoding,
        // we should count at least one offset to be sure that the offset huffman tree could be encoded.
        w.offsetFreq[0] = 1;
        numOffsets = 1;
    }
    w.literalEncoding.generate(w.literalFreq, 15);
    w.offsetEncoding.generate(w.offsetFreq, 15);
    return ;
}

// writeTokens writes a slice of tokens to the output.
// codes for literal and offset encoding must be supplied.
private static void writeTokens(this ptr<huffmanBitWriter> _addr_w, slice<token> tokens, slice<hcode> leCodes, slice<hcode> oeCodes) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    foreach (var (_, t) in tokens) {
        if (t < matchType) {
            w.writeCode(leCodes[t.literal()]);
            continue;
        }
        var length = t.length();
        var lengthCode = lengthCode(length);
        w.writeCode(leCodes[lengthCode + lengthCodesStart]);
        var extraLengthBits = uint(lengthExtraBits[lengthCode]);
        if (extraLengthBits > 0) {
            var extraLength = int32(length - lengthBase[lengthCode]);
            w.writeBits(extraLength, extraLengthBits);
        }
        var offset = t.offset();
        var offsetCode = offsetCode(offset);
        w.writeCode(oeCodes[offsetCode]);
        var extraOffsetBits = uint(offsetExtraBits[offsetCode]);
        if (extraOffsetBits > 0) {
            var extraOffset = int32(offset - offsetBase[offsetCode]);
            w.writeBits(extraOffset, extraOffsetBits);
        }
    }
}

// huffOffset is a static offset encoder used for huffman only encoding.
// It can be reused since we will not be encoding offset values.
private static ptr<huffmanEncoder> huffOffset;

private static void init() {
    var offsetFreq = make_slice<int>(offsetCodeCount);
    offsetFreq[0] = 1;
    huffOffset = newHuffmanEncoder(offsetCodeCount);
    huffOffset.generate(offsetFreq, 15);
}

// writeBlockHuff encodes a block of bytes as either
// Huffman encoded literals or uncompressed bytes if the
// results only gains very little from compression.
private static void writeBlockHuff(this ptr<huffmanBitWriter> _addr_w, bool eof, slice<byte> input) {
    ref huffmanBitWriter w = ref _addr_w.val;

    if (w.err != null) {
        return ;
    }
    foreach (var (i) in w.literalFreq) {
        w.literalFreq[i] = 0;
    }    histogram(input, w.literalFreq);

    w.literalFreq[endBlockMarker] = 1;

    const var numLiterals = endBlockMarker + 1;

    w.offsetFreq[0] = 1;
    const nint numOffsets = 1;



    w.literalEncoding.generate(w.literalFreq, 15); 

    // Figure out smallest code.
    // Always use dynamic Huffman or Store
    nint numCodegens = default; 

    // Generate codegen and codegenFrequencies, which indicates how to encode
    // the literalEncoding and the offsetEncoding.
    w.generateCodegen(numLiterals, numOffsets, w.literalEncoding, huffOffset);
    w.codegenEncoding.generate(w.codegenFreq[..], 7);
    var (size, numCodegens) = w.dynamicSize(w.literalEncoding, huffOffset, 0); 

    // Store bytes, if we don't get a reasonable improvement.
    {
        var (ssize, storable) = w.storedSize(input);

        if (storable && ssize < (size + size >> 4)) {
            w.writeStoredHeader(len(input), eof);
            w.writeBytes(input);
            return ;
        }
    } 

    // Huffman.
    w.writeDynamicHeader(numLiterals, numOffsets, numCodegens, eof);
    var encoding = w.literalEncoding.codes[..(int)257];
    var n = w.nbytes;
    foreach (var (_, t) in input) { 
        // Bitwriting inlined, ~30% speedup
        var c = encoding[t];
        w.bits |= uint64(c.code) << (int)(w.nbits);
        w.nbits += uint(c.len);
        if (w.nbits < 48) {
            continue;
        }
        var bits = w.bits;
        w.bits>>=48;
        w.nbits -= 48;
        var bytes = w.bytes[(int)n..(int)n + 6];
        bytes[0] = byte(bits);
        bytes[1] = byte(bits >> 8);
        bytes[2] = byte(bits >> 16);
        bytes[3] = byte(bits >> 24);
        bytes[4] = byte(bits >> 32);
        bytes[5] = byte(bits >> 40);
        n += 6;
        if (n < bufferFlushSize) {
            continue;
        }
        w.write(w.bytes[..(int)n]);
        if (w.err != null) {
            return ; // Return early in the event of write failures
        }
        n = 0;
    }    w.nbytes = n;
    w.writeCode(encoding[endBlockMarker]);
}

// histogram accumulates a histogram of b in h.
//
// len(h) must be >= 256, and h's elements must be all zeroes.
private static void histogram(slice<byte> b, slice<int> h) {
    h = h[..(int)256];
    foreach (var (_, t) in b) {
        h[t]++;
    }
}

} // end flate_package
