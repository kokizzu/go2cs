// IByteSeq.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Runtime.CompilerServices;

namespace go;

// IByteSeq models Go's `string | []byte` union constraint, which C# cannot express directly
// (C# generic constraints are conjunctive — "and", never "or"). It exposes only the read
// operations common to both members — length, indexing, and sub-slicing — which is all a
// `string | []byte`-constrained function body can legally use (the union is neither comparable
// nor additive, so no operator interfaces are implied). The converter emits this interface
// (instantiated as IByteSeq<byte>) for such constraints; see getGenericDefinition.
//
// It is generic so the generic slice<T> can implement it (a byte-specific interface could not be
// conditionally implemented only when T is byte). @string implements IByteSeq<byte>; slice<T>
// implements IByteSeq<T>, so slice<byte> satisfies the IByteSeq<byte> constraint as well.
public interface IByteSeq
{
    // Length is the element count (Go's len). nint so it matches the indexer/slice domain.
    nint Length { get; }
}

public interface IByteSeq<T> : IByteSeq
{
    // Read-only element access (Go's s[i]). A ref-returning indexer (e.g. slice<T>'s) satisfies
    // this through an explicit implementation that reads the element by value.
    T this[nint index] { get; }

    // Spread source (Go's s...). A `string | []byte`-constrained body may spread the value into a
    // variadic — `append(dst, src[lo:hi]...)` in encoding/json's generic appendString — where the
    // sub-slice is typed as the constraint type parameter again. A type parameter has no members of
    // its own, so the spread `ꓸꓸꓸ` must live on the constraint interface for `((Bytes)(…)).ꓸꓸꓸ` to
    // bind. Both members already expose it: slice<T> as Span<T>, @string as Span<byte>.
    Span<T> ꓸꓸꓸ { get; }
}

// The SELF-REFERENTIAL form, and the one the converter emits as the constraint
// (`where bytes : IByteSeq<bytes, byte>`). TSelf is the implementing type itself — @string
// implements IByteSeq<@string, byte>, slice<T> implements IByteSeq<slice<T>, T> — so the
// sub-slice indexer returns the CONCRETE sequence type rather than the interface.
//
// That is the whole point of the extra type parameter. A Go body constrained on
// `string | []byte` sub-slices constantly (time's parseRFC3339 takes seven sub-slices per
// call), and Go types each result as the type parameter again. When the indexer returned
// `IByteSeq<T>`, every one of those sub-slices BOXED the struct result — 48 bytes for
// slice<T>, 24 for @string — and the converter's `((bytes)(…))` cast then unboxed it, so a
// Go function that allocates nothing allocated per sub-slice in C#. Returning TSelf makes the
// call a `constrained.` direct call on the value type: no box, and the cast becomes an
// identity conversion that emits no IL. See GolibTests ByteSeqAllocationTests, which measures
// the difference against a deliberately boxed control.
//
// Sub-slicing is the only member that needs TSelf, so it lives alone on this interface and
// IByteSeq<T> keeps the members whose signatures do not mention the sequence type. Consumers
// that legitimately hold a byte sequence as an interface continue to bind IByteSeq<T> unchanged.
public interface IByteSeq<TSelf, T> : IByteSeq<T> where TSelf : IByteSeq<TSelf, T>
{
    // Sub-slice (Go's s[lo:hi]), returned as the implementing type so a chain stays concrete.
    // For @string this is an @string, for slice<T> a slice<T> — both already return exactly
    // that from their public Range indexer, so this is satisfied implicitly.
    TSelf this[Range range] { get; }
}

// The `[]byte(s)` and `string(s)` CONVERSIONS of a union-constrained value, which the converter
// emits as `s.ToSlice()` and `s.ToGoString()`.
//
// These are extension methods over the constrained type parameter rather than constructors or
// static factories, for two independent reasons:
//
//   1. Allocation. A constructor can only take the sequence as an interface — C# has no generic
//      constructor — so `new slice<byte>(seq)` boxes the caller's struct on every conversion.
//      Here TSeq is the caller's CONCRETE type, so the argument passes unboxed and the type
//      tests below fold to constants per instantiation.
//   2. Name binding. Converted code carries `using static go.builtin`, which brings the builtin
//      `slice<T>(…)` and `@string(…)` conversion METHODS into scope and shadows the type names.
//      `slice<byte>.From(s)` therefore does not compile (CS0119 — "is a method, which is not
//      valid in the given context"), and neither would any other static-factory spelling. An
//      extension call is member access on the receiver, so it is unaffected.
//
// Both keep Go's per-instantiation conversion semantics exactly: `[]byte([]byte)` is the same
// slice, `[]byte(string)` copies, `string(string)` is free, `string([]byte)` copies.
public static class ByteSeqExtensions
{
    // Go's `[]byte(s)` over a `string | []byte`-constrained value — time's parseRFC3339 ranges
    // `[]byte(s)` inside parseUint, six times per parse.
    public static slice<byte> ToSlice<TSeq>(this TSeq seq) where TSeq : IByteSeq<TSeq, byte>
    {
        // A slice source IS the result — Go's []byte([]byte) shares its backing. The type test
        // is folded to a constant by the JIT, so only this arm is compiled for that instantiation.
        if (typeof(TSeq) == typeof(slice<byte>))
            return Unsafe.As<TSeq, slice<byte>>(ref seq);

        // Any other source materializes its own bytes, as Go's []byte(string) does.
        return new slice<byte>(seq.ꓸꓸꓸ.ToArray());
    }

    // Go's `string(s)` over the same — bytealg's Rabin-Karp compares `string(s[i:j])`.
    public static @string ToGoString<TSeq>(this TSeq seq) where TSeq : IByteSeq<TSeq, byte>
    {
        // An @string source IS the result — `string(s)` over a string is the identity conversion,
        // and @string is immutable, so there is nothing to copy or rebuild. (This used to
        // reconstruct the value from its backing array; now that @string is a WINDOW over shared
        // storage, that would silently widen a sub-string back to its whole backing.)
        if (typeof(TSeq) == typeof(@string))
            return Unsafe.As<TSeq, @string>(ref seq);

        return new @string(seq.ꓸꓸꓸ.ToArray());
    }
}
