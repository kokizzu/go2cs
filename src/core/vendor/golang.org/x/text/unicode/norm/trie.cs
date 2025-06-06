// Copyright 2011 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.vendor.golang.org.x.text.unicode;

partial class norm_package {

[GoType] partial struct valueRange {
    internal uint16 value; // header: value:stride
    internal byte lo;   // header: lo:n
    internal byte hi;
}

[GoType] partial struct sparseBlocks {
    internal slice<valueRange> values;
    internal slice<uint16> offset;
}

internal static sparseBlocks nfcSparse = new sparseBlocks(
    values: nfcSparseValues[..],
    offset: nfcSparseOffset[..]
);

internal static sparseBlocks nfkcSparse = new sparseBlocks(
    values: nfkcSparseValues[..],
    offset: nfkcSparseOffset[..]
);

internal static ж<nfcTrie> nfcData = newNfcTrie(0);
internal static ж<nfkcTrie> nfkcData = newNfkcTrie(0);

// lookup determines the type of block n and looks up the value for b.
// For n < t.cutoff, the block is a simple lookup table. Otherwise, the block
// is a list of ranges with an accompanying value. Given a matching range r,
// the value for b is by r.value + (b - r.lo) * stride.
[GoRecv] internal static uint16 lookup(this ref sparseBlocks t, uint32 n, byte b) {
    var offset = t.offset[n];
    var header = t.values[offset];
    var lo = offset + 1;
    var hi = lo + ((uint16)header.lo);
    while (lo < hi) {
        var m = lo + (hi - lo) / 2;
        var r = t.values[m];
        if (r.lo <= b && b <= r.hi) {
            return r.value + ((uint16)(b - r.lo)) * header.value;
        }
        if (b < r.lo){
            hi = m;
        } else {
            lo = m + 1;
        }
    }
    return 0;
}

} // end norm_package
