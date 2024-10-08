// Code generated by running "go generate" in golang.org/x/text. DO NOT EDIT.

// package idna -- go2cs converted at 2022 March 13 06:46:19 UTC
// import "vendor/golang.org/x/net/idna" ==> using idna = go.vendor.golang.org.x.net.idna_package
// Original source: C:\Program Files\Go\src\vendor\golang.org\x\net\idna\trieval.go
namespace go.vendor.golang.org.x.net;

public static partial class idna_package {

// This file contains definitions for interpreting the trie value of the idna
// trie generated by "go run gen*.go". It is shared by both the generator
// program and the resultant package. Sharing is achieved by the generator
// copying gen_trieval.go to trieval.go and changing what's above this comment.

// info holds information from the IDNA mapping table for a single rune. It is
// the value returned by a trie lookup. In most cases, all information fits in
// a 16-bit value. For mappings, this value may contain an index into a slice
// with the mapped string. Such mappings can consist of the actual mapped value
// or an XOR pattern to be applied to the bytes of the UTF8 encoding of the
// input rune. This technique is used by the cases packages and reduces the
// table size significantly.
//
// The per-rune values have the following format:
//
//   if mapped {
//     if inlinedXOR {
//       15..13 inline XOR marker
//       12..11 unused
//       10..3  inline XOR mask
//     } else {
//       15..3  index into xor or mapping table
//     }
//   } else {
//       15..14 unused
//       13     mayNeedNorm
//       12..11 attributes
//       10..8  joining type
//        7..3  category type
//   }
//      2  use xor pattern
//   1..0  mapped category
//
// See the definitions below for a more detailed description of the various
// bits.
private partial struct info { // : ushort
}

private static readonly nuint catSmallMask = 0x3;
private static readonly nuint catBigMask = 0xF8;
private static readonly nint indexShift = 3;
private static readonly nuint xorBit = 0x4; // interpret the index as an xor pattern
private static readonly nuint inlineXOR = 0xE000; // These bits are set if the XOR pattern is inlined.

private static readonly nint joinShift = 8;
private static readonly nuint joinMask = 0x07; 

// Attributes
private static readonly nuint attributesMask = 0x1800;
private static readonly nuint viramaModifier = 0x1800;
private static readonly nuint modifier = 0x1000;
private static readonly nuint rtl = 0x0800;

private static readonly nuint mayNeedNorm = 0x2000;

// A category corresponds to a category defined in the IDNA mapping table.
private partial struct category { // : ushort
}

private static readonly category unknown = 0; // not currently defined in unicode.
private static readonly category mapped = 1;
private static readonly category disallowedSTD3Mapped = 2;
private static readonly category deviation = 3;

private static readonly category valid = 0x08;
private static readonly category validNV8 = 0x18;
private static readonly category validXV8 = 0x28;
private static readonly category disallowed = 0x40;
private static readonly category disallowedSTD3Valid = 0x80;
private static readonly category ignored = 0xC0;

// join types and additional rune information
private static readonly var joiningL = (iota + 1);
private static readonly var joiningD = 0;
private static readonly var joiningT = 1;
private static readonly var joiningR = 2; 

//the following types are derived during processing
private static readonly var joinZWJ = 3;
private static readonly var joinZWNJ = 4;
private static readonly var joinVirama = 5;
private static readonly var numJoinTypes = 6;

private static bool isMapped(this info c) {
    return c & 0x3 != 0;
}

private static category category(this info c) {
    var small = c & catSmallMask;
    if (small != 0) {
        return category(small);
    }
    return category(c & catBigMask);
}

private static info joinType(this info c) {
    if (c.isMapped()) {
        return 0;
    }
    return (c >> (int)(joinShift)) & joinMask;
}

private static bool isModifier(this info c) {
    return c & (modifier | catSmallMask) == modifier;
}

private static bool isViramaModifier(this info c) {
    return c & (attributesMask | catSmallMask) == viramaModifier;
}

} // end idna_package
