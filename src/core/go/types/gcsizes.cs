// Code generated by "go test -run=Generate -write=all"; DO NOT EDIT.
// Source: ../../cmd/compile/internal/types2/gcsizes.go
// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.go;

partial class types_package {

[GoType] partial struct gcSizes {
    public int64 WordSize; // word size in bytes - must be >= 4 (32bits)
    public int64 MaxAlign; // maximum alignment in bytes - must be >= 1
}

[GoRecv] internal static int64 /*result*/ Alignof(this ref gcSizes s, ΔType T) => func((defer, _) => {
    int64 result = default!;

    defer(() => {
        assert(result >= 1);
    });
    // For arrays and structs, alignment is defined in terms
    // of alignment of the elements and fields, respectively.
    switch (under(T).type()) {
    case Array.val t: {
        return s.Alignof((~t).elem);
    }
    case Struct.val t: {
        if (len((~t).fields) == 0 && _IsSyncAtomicAlign64(T)) {
            // spec: "For a variable x of array type: unsafe.Alignof(x)
            // is the same as unsafe.Alignof(x[0]), but at least 1."
            // Special case: sync/atomic.align64 is an
            // empty struct we recognize as a signal that
            // the struct it contains must be
            // 64-bit-aligned.
            //
            // This logic is equivalent to the logic in
            // cmd/compile/internal/types/size.go:calcStructOffset
            return 8;
        }
        var max = ((int64)1);
        foreach (var (_, f) in (~t).fields) {
            // spec: "For a variable x of struct type: unsafe.Alignof(x)
            // is the largest of the values unsafe.Alignof(x.f) for each
            // field f of x, but at least 1."
            {
                var aΔ1 = s.Alignof(f.typ); if (aΔ1 > max) {
                    max = aΔ1;
                }
            }
        }
        return max;
    }
    case Slice.val t: {
        assert(!isTypeParam(T));
        return s.WordSize;
    }
    case Interface.val t: {
        assert(!isTypeParam(T));
        return s.WordSize;
    }
    case Basic.val t: {
        if ((BasicInfo)(t.Info() & IsString) != 0) {
            // Multiword data structures are effectively structs
            // in which each element has size WordSize.
            // Type parameters lead to variable sizes/alignments;
            // StdSizes.Alignof won't be called for them.
            // Strings are like slices and interfaces.
            return s.WordSize;
        }
        break;
    }
    case TypeParam.val t: {
        throw panic("unreachable");
        break;
    }
    case Union.val t: {
        throw panic("unreachable");
        break;
    }}
    var a = s.Sizeof(T);
    // may be 0 or negative
    // spec: "For a variable x of any type: unsafe.Alignof(x) is at least 1."
    if (a < 1) {
        return 1;
    }
    // complex{64,128} are aligned like [2]float{32,64}.
    if (isComplex(T)) {
        a /= 2;
    }
    if (a > s.MaxAlign) {
        return s.MaxAlign;
    }
    return a;
});

[GoRecv] internal static slice<int64> Offsetsof(this ref gcSizes s, slice<ж<Var>> fields) {
    var offsets = new slice<int64>(len(fields));
    int64 offs = default!;
    foreach (var (i, f) in fields) {
        if (offs < 0) {
            // all remaining offsets are too large
            offsets[i] = -1;
            continue;
        }
        // offs >= 0
        var a = s.Alignof(f.typ);
        offs = align(offs, a);
        // possibly < 0 if align overflows
        offsets[i] = offs;
        {
            var d = s.Sizeof(f.typ); if (d >= 0 && offs >= 0){
                offs += d;
            } else {
                // ok to overflow to < 0
                offs = -1;
            }
        }
    }
    // f.typ or offs is too large
    return offsets;
}

[GoRecv] internal static int64 Sizeof(this ref gcSizes s, ΔType T) {
    switch (under(T).type()) {
    case Basic.val t: {
        assert(isTyped(T));
        BasicKind k = t.val.kind;
        if (((nint)k) < len(basicSizes)) {
            {
                var sΔ1 = basicSizes[k]; if (sΔ1 > 0) {
                    return ((int64)sΔ1);
                }
            }
        }
        if (k == ΔString) {
            return s.WordSize * 2;
        }
        break;
    }
    case Array.val t: {
        var n = t.val.len;
        if (n <= 0) {
            return 0;
        }
        var esize = s.Sizeof((~t).elem);
        if (esize < 0) {
            // n > 0
            return -1;
        }
        if (esize == 0) {
            // element too large
            return 0;
        }
// 0-size element

        // esize > 0
        // Final size is esize * n; and size must be <= maxInt64.
        static readonly UntypedInt maxInt64 = /* 1<<63 - 1 */ 9223372036854775807;
        if (esize > maxInt64 / n) {
            return -1;
        }
        return esize * n;
    }
    case Slice.val t: {
        return s.WordSize * 3;
    }
    case Struct.val t: {
        n = t.NumFields();
        if (n == 0) {
            // esize * n overflows
            return 0;
        }
        var offsets = s.Offsetsof((~t).fields);
        var offs = offsets[n - 1];
        var size = s.Sizeof((~t).fields[n - 1].typ);
        if (offs < 0 || size < 0) {
            return -1;
        }
        if (offs > 0 && size == 0) {
            // type too large
            // gc: The last field of a non-zero-sized struct is not allowed to
            // have size 0.
            size = 1;
        }
        return align(offs + size, // gc: Size includes alignment padding.
 s.Alignof(~t));
    }
    case Interface.val t: {
        assert(!isTypeParam(T));
        return s.WordSize * 2;
    }
    case TypeParam.val t: {
        throw panic("unreachable");
        break;
    }
    case Union.val t: {
        throw panic("unreachable");
        break;
    }}
    // may overflow to < 0 which is ok
    // Type parameters lead to variable sizes/alignments;
    // StdSizes.Sizeof won't be called for them.
    return s.WordSize;
}

// catch-all

// gcSizesFor returns the Sizes used by gc for an architecture.
// The result is a nil *gcSizes pointer (which is not a valid types.Sizes)
// if a compiler/architecture pair is not known.
internal static ж<gcSizes> gcSizesFor(@string compiler, @string arch) {
    if (compiler != "gc"u8) {
        return default!;
    }
    return gcArchSizes[arch];
}

} // end types_package
