// array.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using go.golib;

namespace go;

public interface IArray : IEnumerable, ICloneable
{
    Array? Source { get; }

    nint Length { get; }

    object? this[nint index] { get; set; }

    public bool IndexIsValid(nint index)
    {
        return index > -1 && index < Length;
    }
}

public interface IArray<T> : IArray, IEnumerable<(nint, T)>
{
    new T[] Source { get; }
    
    new ref T this[nint index] { get; }

    Span<T> ꓸꓸꓸ { get; }

    Span<T> ToSpan();
}

[Serializable]
public readonly struct array<T> : IArray<T>, IList<T>, IReadOnlyList<T>, IEquatable<IArray>, IGoZeroShaped
{
    internal readonly T[] m_array;

    // The WINDOW this array occupies inside m_array. Every ordinary construction spans the whole
    // backing (m_low = 0, m_length = m_array.Length) — the window exists ONLY so that Go's two
    // array-POINTER conversions can ALIAS existing storage instead of snapshotting it: the slice
    // form `(*[N]T)(s)` (see Alias) and the element-pointer form `(*[N]T)(unsafe.Pointer(&s[i]))`
    // (see AliasPointer). Reads and writes therefore go through m_low/m_length, never through
    // m_array's own bounds.
    private readonly int m_low;
    private readonly int m_length;

    public array()
    {
        m_array = [];
        m_length = 0;
    }

    public array(int length)
    {
        m_array = AllocationCounter.NewArray<T>(length);
        m_length = length;
    }

    public array(nint length)
    {
        m_array = AllocationCounter.NewArray<T>(length);
        m_length = (int)length;
    }

    public array(ulong length)
    {
        m_array = AllocationCounter.NewArray<T>(length);
        m_length = (int)length;
    }

    // Fixed-size array whose ELEMENT zero value must itself be constructed, because default(T)
    // is not usable storage: a nested fixed array (`[2][4]byte` -> array<array<byte>>, whose
    // inner length exists only in the Go type, never in array<T>) or a struct whose own zero
    // value needs construction. The converter supplies the factory since only it knows the
    // element's shape; every other element type keeps the plain length ctors' fill of default(T).
    //
    // Mirrors the int/nint/ulong length overloads above: a Go array length can be a NAMED integer
    // constant (`quant [nQuantIndex][blockSize]byte` - image/jpeg), which reaches this ctor as its
    // wrapper's underlying nint rather than an int (CS1503 against an int-only overload).
    public array(nint length, Func<T> elementFactory)
    {
        m_array = AllocationCounter.NewArray<T>(length);
        m_length = (int)length;

        for (nint i = 0; i < length; i++)
            m_array[i] = elementFactory();
    }

    public array(int length, Func<T> elementFactory) : this((nint)length, elementFactory)
    {
    }

    public array(ulong length, Func<T> elementFactory) : this((nint)length, elementFactory)
    {
    }

    public array(T[]? array)
    {
        m_array = array ?? [];
        m_length = m_array.Length;
    }

    // Go 1.20 slice-to-array VALUE conversion (`[4]byte(s)`): copies exactly 'length' elements,
    // panicking like Go when the slice is too short. The POINTER form (`(*[4]byte)(s)`, Go 1.17)
    // is a different conversion with different semantics and takes Alias below.
    public array(slice<T> source, nint length)
    {
        checkArrayConversionLength(source.Length, length);

        m_array = AllocationCounter.CopyOf<T>(source.ToSpan()[..(int)length]);
        m_length = (int)length;
    }

    // Private window constructor — see m_low/m_length and Alias.
    private array(T[] backing, int low, int length)
    {
        m_array = backing;
        m_low = low;
        m_length = length;
    }

    /// <summary>
    /// Go's slice-to-array-POINTER conversion, <c>(*[N]T)(s)</c> (Go 1.17): an array that ALIASES
    /// the slice's own backing storage rather than a snapshot of it.
    /// </summary>
    /// <param name="source">Slice whose storage the array addresses.</param>
    /// <param name="length">Go array length <c>N</c>; the slice must be at least this long.</param>
    /// <returns>An <c>array&lt;T&gt;</c> window over <paramref name="source"/>'s first
    /// <paramref name="length"/> elements.</returns>
    /// <remarks>
    /// <para>
    /// The distinction from the <c>array(slice&lt;T&gt;, nint)</c> constructor is Go's, not an
    /// implementation detail: <c>[N]T(s)</c> yields a VALUE (a copy) while <c>(*[N]T)(s)</c> yields
    /// a POINTER INTO <c>s</c> — "the slice and array share their underlying array" (Go spec,
    /// Conversions from slice to array or array pointer). A copy behind the pointer form is not a
    /// performance detail but a silent wrong answer: every write through the pointer is discarded.
    /// image/png's encoder is the corpus witness — its <c>cbTCA8</c> row loop converts each 4-byte
    /// destination window with <c>d := (*[4]byte)(dst)</c> and writes the un-premultiplied pixel
    /// through <c>d</c>, so against a copy it emitted an all-zero image for every non-opaque RGBA
    /// source (TestWriteRGBA).
    /// </para>
    /// <para>
    /// A window never escapes into an ordinary array: <see cref="Clone"/> (Go's by-value array copy)
    /// materializes the window's own storage, so the copy is a full, offset-free array again.
    /// </para>
    /// </remarks>
    public static array<T> Alias(slice<T> source, nint length)
    {
        checkArrayConversionLength(source.Length, length);

        // A nil/zero slice with length 0 has no backing to window — the empty array is the whole
        // of what the conversion can address, and its (absent) storage is shared vacuously.
        return source.m_array is null
            ? new array<T>([], 0, 0)
            : new array<T>(source.m_array, (int)source.Low, (int)length);
    }

    /// <summary>
    /// Go's element-pointer-to-array reinterpret, <c>(*[N]T)(unsafe.Pointer(p))</c> where <c>p</c>
    /// is a <c>*T</c>: an array pointer that ALIASES the storage <paramref name="element"/> is an
    /// element of, rather than a snapshot of it.
    /// </summary>
    /// <param name="element">Pointer to the element the array starts at.</param>
    /// <param name="length">Go array length <c>N</c>.</param>
    /// <returns>A pointer to an <c>array&lt;T&gt;</c> window that begins at
    /// <paramref name="element"/>, when it addresses managed array/slice storage; otherwise a
    /// pointer over its raw address.</returns>
    /// <remarks>
    /// <para>
    /// This is the same aliasing requirement <see cref="Alias"/> answers for the slice form, reached
    /// from a POINTER instead: Go's array is a view of the bytes at <c>p</c>, so a write through it
    /// must land in the caller's buffer. os's <c>TestReadStdin</c> is the corpus witness — its
    /// <c>poll.ReadConsole</c> fake fills internal/poll's read buffer with
    /// <c>copy((*[10000]uint16)(unsafe.Pointer(buf))[:n:n], s16)</c>, and against a snapshot every
    /// one of the test's 462 subtests read back zeros.
    /// </para>
    /// <para>
    /// <paramref name="length"/> is CLAMPED to the storage that actually exists from the element.
    /// Go's <c>N</c> in this idiom is a promise ("at least this many"), not a length — the huge
    /// constants it is spelled with (<c>10000</c>, <c>1&lt;&lt;16</c>, <c>0xffff</c>) never describe
    /// a real allocation, and the result is always immediately re-sliced to the real count. Honoring
    /// <c>N</c> literally would put the window's own bounds past its backing store, where a
    /// full-slice expression is an <c>ArgumentException</c> rather than the window Go asks for; a
    /// clamped window addresses exactly the storage that is there, so an overrun surfaces as a
    /// Go-style index panic instead of the silent corruption the same overrun is in Go.
    /// </para>
    /// <para>
    /// A pointer with no managed element storage behind it — a heap box, a struct field, a native
    /// address — keeps the raw-address route: no <c>T[]</c> exists to window, and an
    /// <c>array&lt;T&gt;</c> can neither view native memory nor be fabricated from a scalar's bytes.
    /// That is the raw-metal fork, unchanged here.
    /// </para>
    /// </remarks>
    public static ж<array<T>> AliasPointer(ж<T>? element, nint length)
    {
        if (length >= 0 && element is not null && element.TryGetElementStorage(out T[]? backing, out nint index))
        {
            nint available = backing.Length - index;

            return new ж<array<T>>(new array<T>(backing, (int)index, (int)(length < available ? length : available)));
        }

        return (ж<array<T>>)(uintptr)element!;
    }

    private static void checkArrayConversionLength(nint sourceLength, nint length)
    {
        // A PANIC, not an IndexOutOfRangeException: Go's `[N]T(s)` on a short slice is a
        // recoverable runtime panic, and only a PanicException is both visible to `recover()` and
        // excluded from a host containment policy (see the note on slice<T>'s windowing ctors).
        // The message text is unchanged — it was already Go's.
        if (sourceLength < length)
            throw RuntimeErrorPanic.ArrayConversionLength(sourceLength, length);
    }

    public array(Span<T> source)
    {
        m_array = AllocationCounter.CopyOf((ReadOnlySpan<T>)source);
        m_length = m_array.Length;
    }

    public array(ReadOnlySpan<T> source)
    {
        m_array = AllocationCounter.CopyOf(source);
        m_length = m_array.Length;
    }

    public array(Memory<T> source)
    {
        m_array = AllocationCounter.CopyOf((ReadOnlySpan<T>)source.Span);
        m_length = m_array.Length;
    }

    public array(ReadOnlyMemory<T> source)
    {
        m_array = AllocationCounter.CopyOf(source.Span);
        m_length = m_array.Length;
    }

    // Source intentionally stays the RAW backing reference: a null result is the discriminator
    // for a never-constructed zero value (see the generated struct constructors, which use it to
    // keep an array field's `= new(N)` initializer when the incoming argument is a zero value).
    // For an ALIAS window it is likewise the raw storage — the identity every address question is
    // asked about — so a consumer walking elements through it must add Low (ж<T>.CanonicalElement
    // does; everything else in the corpus holds a full-window array, where Low is 0).
    public T[] Source => m_array;

    /// <summary>
    /// Gets the offset of this array's first element inside <see cref="Source"/> — nonzero only for
    /// an <see cref="Alias"/> window over a slice's storage.
    /// </summary>
    public nint Low => m_low;

    // Null-safe view of the backing store: `default(array<T>)` runs no constructor, so m_array is
    // null; treat that zero value as an EMPTY array for all reads (length, index, enumerate,
    // print, compare) instead of throwing NRE — mirroring @string's null-safe zero value. (Go's
    // true zero `[N]T` has N zeroed elements, but the declared length only exists where a
    // constructor or field initializer ran; empty is the closest zero-value behavior a bare
    // `default` can have, and any index into it panics Go-style rather than crashing the host.)
    private T[] Backing => m_array ?? [];

    public Span<T> ꓸꓸꓸ => ToSpan(); // Spread operator

    public nint Length => m_length;

    // Returning by-ref value allows array to be a struct instead of a class and still allow read and write
    // Allows for implicit index support: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/ranges#implicit-index-support
    public ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= m_length)
                throw RuntimeErrorPanic.IndexOutOfRange(index, m_length);

            return ref Backing[m_low + index];
        }
    }

    public ref T this[nint index]
    {
        get
        {
            if (index < 0 || index >= m_length)
                throw RuntimeErrorPanic.IndexOutOfRange(index, m_length);

            return ref Backing[m_low + (int)index];
        }
    }

    public ref T this[ulong index] => ref this[(nint)index];

    public slice<T> this[Range range] => Slice(range.Start.GetOffset(m_length), range.End.GetOffset(m_length) - range.Start.GetOffset(m_length));

    public slice<T> Slice(int start, int length)
    {
        // Capacity stops at the ARRAY's end, never the backing store's: Go's `a[:]` on a `[4]byte`
        // has cap 4. Identical to the previous unbounded form for every full-window array (there
        // the array IS the backing); it is an Alias window that would otherwise report the rest of
        // the slice it aliases as spare capacity.
        return new slice<T>(Backing, m_low + start, m_low + start + length, m_low + m_length);
    }

    public slice<T> Slice(nint start, nint length)
    {
        return Slice((int)start, (int)length);
    }

    public int IndexOf(in T item)
    {
        int index = Array.IndexOf(Backing, item, m_low, m_length);
        return index >= 0 ? index - m_low : -1;
    }

    public bool Contains(in T item)
    {
        return IndexOf(item) >= 0;
    }

    // The window's own storage — the RAW backing whenever this array spans all of it, which every
    // ordinary construction does (so the structural paths below keep their exact previous behavior
    // and allocate nothing); only an Alias window materializes its elements.
    private T[] WindowArray
    {
        get
        {
            T[] backing = Backing;
            return m_low == 0 && m_length == backing.Length ? backing : AllocationCounter.CopyOf<T>(backing.AsSpan(m_low, m_length));
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        ToSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    public T[] ToArray()
    {
        return AllocationCounter.CopyOf<T>(ToSpan());
    }

    public Span<T> ToSpan()
    {
        return new Span<T>(Backing, m_low, m_length);
    }

    public array<T> Clone()
    {
        // ToSpan().ToArray() rather than Backing.Clone(): a Go array copy is of the ARRAY, and an
        // Alias window's array is its window, not the slice storage behind it — so the copy is a
        // full, offset-free array again.
        T[] copy = AllocationCounter.CopyOf<T>(ToSpan());

        // Go array copy semantics are DEEP for nested arrays: assigning a [2][3]int copies the
        // inner arrays too. An element that is itself an array wrapper (array<T> or a generated
        // named-array wrapper — both implement IArray) is a struct over a shared T[] backing, so
        // the shallow element copy above would leave every nested backing aliased; re-clone each
        // element through its ICloneable surface, which returns the properly-wrapped clone for
        // unboxing back to T (recursing through deeper nestings). The typeof test is a JIT-time
        // constant, so non-nested element types keep the single shallow copy with no per-element
        // work.
        //
        // A STRUCT element carrying array fields (IGoValueClone — see GoValueCloneAttribute) has
        // exactly the same problem one level down: Go's `[2]digest` copy copies both digests'
        // arrays inline, while the shallow element copy shares their backing. It clones through
        // the same ICloneable surface.
        if (typeof(IArray).IsAssignableFrom(typeof(T)) || typeof(IGoValueClone).IsAssignableFrom(typeof(T)))
        {
            for (int i = 0; i < copy.Length; i++)
                copy[i] = (T)((ICloneable)copy[i]!).Clone();
        }

        return new array<T>(copy);
    }

    /// <summary>
    /// Go by-value copy under the UNIFORM member name generated code uses
    /// (<c>go2cs.Symbols.ValueCloneMethod</c>) — an alias of <see cref="Clone"/>.
    /// </summary>
    /// <remarks>
    /// A generated struct clone (see <see cref="GoValueCloneAttribute"/>) copies each of its
    /// clone-needing fields with ONE call form. A nested struct field declares this name itself; the
    /// array kinds — this type and the generated named-array / array-view wrappers — alias it here so
    /// the generated body needs no per-field branch. Copy SITES keep the public <c>Clone()</c>.
    /// </remarks>
    public array<T> ΔClone()
    {
        return Clone();
    }

    /// <summary>
    /// Gets a NEW array of this one's Go LENGTH whose elements are all the Go zero value — the
    /// shaped zero <see cref="builtin.GoZero{T}"/> hands back for a built-in like <c>clear</c>.
    /// </summary>
    /// <remarks>
    /// The Go length of a fixed-size array lives only in the instance ([4]int32 and [8]int32 are
    /// both <c>array&lt;int32&gt;</c>), so <c>default</c> would leave a LENGTH-ZERO array behind.
    /// The fill recurses through <see cref="builtin.GoZero{T}"/> so a NESTED shape
    /// (<c>[2][3]int32</c>) keeps its inner lengths too; the check is a per-T constant, so a plain
    /// element type allocates the zeroed backing and stops.
    /// </remarks>
    object IGoZeroShaped.GoZeroLike()
    {
        array<T> zero = new((nint)m_length);

        if (!builtin.ZeroIsDefault<T>())
        {
            for (int i = 0; i < m_length; i++)
                zero.m_array[i] = builtin.GoZero(Backing[m_low + i]);
        }

        return zero;
    }

    public IEnumerator<(nint, T)> GetEnumerator()
    {
        T[] backing = Backing;

        for (nint index = 0; index < m_length; index++)
            yield return (index, backing[m_low + index]);
    }

    public override string ToString()
    {
        return $"[{string.Join(" ", ((IEnumerable<T>)this).Take(200))}{(Length > 200 ? " ..." : "")}]";
    }

    public override int GetHashCode()
    {
        // Structural (content-based) hash to match the structural Equals below: Go arrays are
        // comparable VALUES — a `map[[2]int]V` key must be found again by an equal array with
        // different backing storage (e.g. the defensive `.Clone()` a stored key takes). The
        // previous `m_array.GetHashCode()` hashed the backing REFERENCE, so every structural-
        // equal key missed.
        IStructuralEquatable equatable = WindowArray;
        return equatable.GetHashCode(EqualityComparer<T>.Default);
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            slice<T> slice => Equals(slice),
            array<T> array => Equals(array),
            ISlice<T> slice => Equals(slice),
            IArray<T> array => Equals(array),
            ISlice slice => Equals(slice),
            IArray array => Equals(array),
            _ => false
        };
    }

    // Structural equality comparers are called per-ELEMENT by IStructuralEquatable (each side
    // boxed), so they must be typed to the element — the previous container-typed comparers
    // (EqualityComparer<T[]>/<object[]>) threw "Type of argument is not compatible with the
    // generic comparer" on the first equal-length comparison of arrays with distinct backing
    // stores (e.g. a map<array<nint>, V> key lookup). An element that is itself an array
    // wrapper recurses through its own Equals(object) via the element comparer, so nested
    // arrays compare by content Go-style.
    public bool Equals(IArray? other)
    {
        IStructuralEquatable equatable = WindowArray;
        return equatable.Equals(otherWindow(other), EqualityComparer<object>.Default);
    }

    public bool Equals(IArray<T>? other)
    {
        IStructuralEquatable equatable = WindowArray;
        return equatable.Equals(otherWindow(other), EqualityComparer<T>.Default);
    }

    public bool Equals(array<T> other)
    {
        IStructuralEquatable equatable = WindowArray;
        return equatable.Equals(other.WindowArray, EqualityComparer<T>.Default);
    }

    // The comparand's own elements: Source is the RAW backing, which for an Alias window is wider
    // than the array it stands for (see Low).
    private static Array? otherWindow(IArray? other)
    {
        return other switch
        {
            null => null,
            array<T> arr => arr.WindowArray,
            _ => other.Source
        };
    }

    #region [ Operators ]

    // Enable implicit conversions between array<T> and T[]
    public static implicit operator array<T>(T[] value)
    {
        return new array<T>(value);
    }

    public static implicit operator array<T>(Span<T> value)
    {
        return new array<T>(value);
    }

    public static implicit operator array<T>(ReadOnlySpan<T> value)
    {
        return new array<T>(value);
    }

    public static implicit operator array<T>(Memory<T> value)
    {
        return new array<T>(value);
    }

    public static implicit operator array<T>(ReadOnlyMemory<T> value)
    {
        return new array<T>(value);
    }

    public static implicit operator T[](array<T> value)
    {
        return value.WindowArray;
    }

    // array<T> to array<T> comparisons
    public static bool operator ==(array<T> a, array<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(array<T> a, array<T> b)
    {
        return !(a == b);
    }

    // Slice<T> to IArray comparisons
    public static bool operator ==(IArray a, array<T> b)
    {
        return b.Equals(a);
    }

    public static bool operator !=(IArray a, array<T> b)
    {
        return !(a == b);
    }

    public static bool operator ==(array<T> a, IArray b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(array<T> a, IArray b)
    {
        return !(a == b);
    }

    // array<T> to nil comparisons
    public static bool operator ==(array<T> array, NilType _)
    {
        return array.Length == 0;
    }

    public static bool operator !=(array<T> array, NilType nil)
    {
        return !(array == nil);
    }

    public static bool operator ==(NilType nil, array<T> array)
    {
        return array == nil;
    }

    public static bool operator !=(NilType nil, array<T> array)
    {
        return array != nil;
    }

    public static implicit operator array<T>(NilType _)
    {
        return default;
    }

    #endregion

    #region [ Interface Implementations ]

    object ICloneable.Clone()
    {
        // Returns the boxed array<T> wrapper (not the raw T[]): the deep-clone element pass in
        // Clone() above — and the generated named-array wrappers — unbox this result back to the
        // element type, which requires the exact wrapped form. Clone() reads through Backing,
        // so the zero-value (null m_array) case stays null-safe here too.
        return Clone();
    }

    Array IArray.Source => m_array;

    object? IArray.this[nint index]
    {
        get => this[index];
        set => this[index] = (T)value!;
    }

    T IList<T>.this[int index]
    {
        get => this[index];
        set
        {
            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            this[index] = value;
        }
    }

    int IList<T>.IndexOf(T item)
    {
        return IndexOf(item);
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    int IReadOnlyCollection<T>.Count => (int)Length;

    T IReadOnlyList<T>.this[int index] => this[index];

    bool ICollection<T>.IsReadOnly => false;

    int ICollection<T>.Count => (int)Length;

    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Contains(T item)
    {
        return Contains(item);
    }

    void ICollection<T>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return WindowArray.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return WindowArray.GetEnumerator();
    }

    #endregion
}

public static class ArrayExtensions
{
    // array initializer from C# array
    public static array<T> array<T>(this T[] array)
    {
        return new array<T>(array);
    }

    // array initializer from a C# array that is SHORT of the Go array's declared length - the
    // `[8]byte{1, 2}` composite-literal form, where Go zero-fills the omitted elements. Emitted
    // by the converter only when the literal supplies fewer elements than the declared length,
    // so a full literal keeps the plain `.array()` projection.
    //
    // The padded copy is built HERE rather than in an `array<T>(T[], int)` constructor on purpose:
    // such a constructor is ambiguous with the `array(slice<T>, nint)` slice-to-array CONVERSION
    // ctor for a call like `new array<byte>(s, 4)` (CS0121 - `slice<T>` is the exact match on the
    // source but `int` is the exact match on the length, so neither overload is better;
    // NamedPointerReinterpret's `sliceToArray` hit exactly this). Keeping the padding out of the
    // constructor set leaves array<T>'s ctor overloads untouched.
    public static array<T> array<T>(this T[] array, int length)
    {
        T[] padded = AllocationCounter.NewArray<T>(length);

        // An over-long source cannot arise from a valid Go literal (the compiler rejects an index
        // past the declared length), so it is truncated rather than checked.
        array.AsSpan(0, Math.Min(array.Length, length)).CopyTo(padded);

        return new array<T>(padded);
    }

    // Same padding for the SparseArray projection of an INDEX-KEYED literal whose highest key
    // falls short of the declared length (`[8]byte{i: 1}` with a non-literal constant index):
    // SparseArray's own Count is `max index + 1`, which is the literal's extent, not the array's.
    public static array<T> array<T>(this IEnumerable<T> source, int length)
    {
        // Enumerable.ToArray's own result is charged here; the growth buffers it discards on the
        // way are BCL-internal and, like every other BCL internal, deliberately uncharged.
        AllocationCounter.Count();
        return source.ToArray().array(length);
    }

    // array initializer from Span
    public static array<T> array<T>(this Span<T> source)
    {
        return new array<T>(source);
    }

    // array initializer from an enumerable
    public static array<T> array<T>(this IEnumerable<T> source)
    {
        // As above: the materialized array is charged, its BCL-internal growth buffers are not.
        AllocationCounter.Count();
        return new array<T>(source.ToArray());
    }
}
