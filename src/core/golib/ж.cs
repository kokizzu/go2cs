// ж.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable StaticMemberInGenericType
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using go.golib;

[assembly:InternalsVisibleTo("unsafe")]
[assembly:InternalsVisibleTo("GolibTests")]

namespace go;

/// <summary>
/// Represents a pointer to a heap allocated instance of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type for heap based reference.</typeparam>
/// <remarks>
/// <para>
/// A new .NET class instance is always allocated on the heap and registered for garbage collection.
/// The <see cref="ж{T}"/> class is used to create a reference to a heap allocated instance of type
/// <typeparamref name="T"/> so that the type can (1) have scope beyond the current stack, and (2)
/// have the ability to create a safe pointer to the type, i.e., a reference.
/// </para>
/// <para>
/// If <typeparamref name="T"/> is a <see cref="System.ValueType"/>, e.g., a struct, note that value
/// will be "boxed" for heap allocation. Since boxed value will be a new copy of original value, make
/// sure to use ref-based <see cref="Value"/> for updates instead of a local stack copy of value.
/// See the <see cref="builtin.heap{T}(out ж{T})"/> and notes on boxing:
/// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing
/// </para>
/// <para>
/// So long as a reference to this class exists, so will the value of type <typeparamref name="T"/>.
/// </para>
/// <para>
/// This is the STANDARD implementation of the pointer contracts, not the only one: a Go named
/// pointer type (<c>type P *T</c>) is emitted as a generated wrapper class implementing the same
/// <see cref="IPointer{T}"/> and <see cref="INilPointer"/>. Those interfaces, the field-reference
/// delegates and the <see cref="FieldRef{T}"/> accessor factory live in <c>ж.Contracts.cs</c>,
/// whose banner covers what belongs on a contract versus on this box — and the structural-versus-
/// value-peeking nil distinction that everything about pointer identity turns on.
/// </para>
/// <para>
/// Operations ON a pointer that do not need this box's private state live in
/// <c>ж.PointerExtensions.cs</c> as extension methods — reinterpretation, the nil-safe re-alias, and
/// the address-token printer <see cref="ToString"/> calls. They are extension methods rather than
/// members precisely so they can answer for the OTHER managed spelling of a Go nil pointer: a
/// zero-valued pointer field is a plain <c>null</c> reference, on which an instance call throws
/// where Go yields nil.
/// </para>
/// </remarks>
public class ж<T> : IPointer<T>, IEquatable<ж<T>>, INilPointer
{
    // Item3 is the field's IDENTITY token for pointer equality: the ORIGINAL (typically static,
    // compiler-cached) field accessor delegate, carried separately because the typed
    // `of(FieldRefFunc<T, TElem>)` overload WRAPS the accessor in a per-call closure (getFieldRef).
    // Equality must compare the ORIGINAL, not the wrapper: comparing wrappers makes every distinct
    // `&x.field` box unequal — `&x.f == &x.f` evaluates FALSE, violating Go pointer identity and
    // breaking the address-keyed runtime semaphores in the hand-owned sync/internal-poll
    // implementations. Delegate.Equals compares method+target, so two conversions of the same
    // accessor method group compare equal across call sites, which is what makes the token work.
    private readonly (object, FieldRefFunc<T>, Delegate)? m_structFieldRef;
    private readonly (IArray, int)? m_arrayIndexRef;
    private readonly bool m_isNull;
    private T m_val;

    // PINNABLE storage for a standard heap box, replacing m_val whenever T can be held still — a
    // one-element array, allocated by the value constructors, that Value/ValueSlot resolve to.
    //
    // WHY THE VALUE CANNOT SIMPLY LIVE IN m_val. Go's `uintptr(unsafe.Pointer(&x))` names an address
    // that STAYS VALID while native code uses it (unsafe.Pointer rules 3 and 4) — Go's collector never
    // moves heap objects, so this is free there. The CLR's does move them, and m_val is a field of THIS
    // object, which is a class with reference fields and therefore cannot be pinned at all
    // (GCHandle refuses anything that contains pointers). A one-element array of an unmanaged T
    // contains none, so it can. The address handed to a syscall is then stable for as long as the box
    // lives (see EnsureStableAddress).
    //
    // WHY IT IS ALLOCATED EAGERLY AND NEVER MIGRATED. `heap<T>(out ж<T>)` hands the caller a ref
    // ALIAS to the slot before any address is taken (`ref var done = ref heap(new uint32(), out var
    // Ꮡdone)`), so moving the storage on first address-take would leave that alias pointing at the
    // abandoned copy — which is precisely the bug this field exists to fix, reintroduced one level
    // down: the kernel would write the real slot and the caller would read the stale one. The slot
    // must therefore BE the storage from construction.
    //
    // Null for a T that contains references (nothing pinnable to allocate — such a value's C# layout
    // is not a native layout either, so no syscall can meaningfully be handed its address) and for the
    // three non-standard box kinds, which keep their storage elsewhere. The
    // IsReferenceOrContainsReferences probe is a JIT-time constant, so the branch folds away in both
    // directions and neither the allocation nor the test costs a managed-T box anything.
    private readonly T[]? m_slot;

    // A NATIVE address this box aliases, rather than managed storage it owns (0 when this is an
    // ordinary managed box). This is the fourth reference kind, alongside the standard value, the
    // struct-field ref and the array-element ref above.
    //
    // It exists because a Win32 API can RETURN a pointer into native memory that the caller must
    // walk and then hand BACK to the OS — GetEnvironmentStringsW/FreeEnvironmentStringsW being the
    // canonical pair. Such an address must be ALIASED, never copied: reinterpreting it by copying the
    // pointed-at value into a managed box loses the address, so the walk scans the GC heap instead,
    // and returning the box's address to the OS asks it to free GC memory — which shows up as an
    // outright STATUS_HEAP_CORRUPTION (0xC0000374) process kill. A native-backed box aliases the real
    // address, so `.Value` reads the real memory and the uintptr/void* round-trip is EXACT. The
    // uintptr conversion operator below is the one that has to honor this.
    private readonly nuint m_nativeAddr;

    // A pin this box OWNS, kept alive for the box's lifetime and freed when the box is collected (the
    // PinnedBuffer finalizer releases the GCHandle). It serves the three — mutually exclusive — cases
    // in which a box's meaning is an ADDRESS of managed storage, and an address of managed storage is
    // meaningless unless something holds that storage still:
    //
    //   1. A box whose pointee is a Go fixed array handed to a native syscall as a buffer
    //      (`unsafe.Pointer(&arr)`): created LAZILY by pinnedArrayData below, on first conversion to
    //      uintptr/void*, so the syscall's write and the managed reads afterward see one address.
    //   2. A NATIVE-address box derived from managed storage by the reinterpret fallback
    //      (TryPinnedReinterpret below): created with the box, because the address it aliases is only
    //      valid while the storage behind it cannot move.
    //   3. EVERY OTHER conversion of a managed box to uintptr/void* (EnsureStableAddress below), on
    //      the same terms and for the same reason: whatever receives the address may still be using it
    //      after the statement that produced it has returned.
    //
    // Disjoint by construction — cases 1 and 3 run only when m_nativeAddr is 0 (the conversion
    // operators return early for a native box) and 3 is the fall-through of 1, and case 2 produces
    // nothing but native boxes.
    private PinnedBuffer? m_pin;

    /// <summary>
    /// Creates a new pointer to heap allocated instance of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">Source value for heap allocated reference.</param>
    public ж(in T value)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            // One object: this box. A managed T lives in m_val, a field of the box itself.
            m_val = value;
            AllocationCounter.Count();
        }
        else
        {
            // TWO objects for Go's ONE malloc — the box, plus the one-element pinnable slot whose
            // eager allocation the m_slot commentary above explains cannot be deferred. The count
            // says two because two is what the CLR heap receives; see AllocationCounter's remarks
            // on why the unit is the managed object and not the logical Go allocation.
            m_slot = [value];
            AllocationCounter.Count(2);
        }
    }

    // Create a new reference to a field in a heap allocated struct. fieldIdentity carries the
    // original accessor delegate when fieldRefFunc is a per-call closure wrapper (see the typed
    // `of(...)` overload) so pointer equality compares the FIELD, not the wrapper instance.
    internal ж(object source, FieldRefFunc<T> fieldRefFunc, Delegate? fieldIdentity = null)
    {
        m_structFieldRef = (source, fieldRefFunc, fieldIdentity ?? fieldRefFunc);
        m_val = default!;

        // The box only. The nullable ValueTuple is a struct stored INLINE in m_structFieldRef, so
        // it costs no object of its own; the accessor delegate is the caller's (typically a
        // compiler-cached static), and any per-call closure wrapper is charged where it is minted.
        AllocationCounter.Count();
    }

    // Create a new indexed reference into an existing heap allocated array
    internal ж(IArray array, int index)
    {
        m_arrayIndexRef = (array, index);
        m_val = default!;

        // The box only — the array is the caller's, already charged when it was created.
        AllocationCounter.Count();
    }

    // Create a pointer that ALIASES a native address (see m_nativeAddr). A zero address is the
    // nil pointer, matching Go's `(*T)(unsafe.Pointer(uintptr(0))) == nil`. An address INTO managed
    // storage carries the pin that holds that storage still (see m_pin); a genuinely native one
    // carries none, there being nothing the collector could move.
    internal ж(nuint nativeAddress, PinnedBuffer? pin = null)
    {
        m_nativeAddr = nativeAddress;
        m_val = default!;
        m_isNull = nativeAddress == 0;
        m_pin = pin;

        // The box only. The memory it aliases is native — never charged, because the CLR heap never
        // received it — and the pin, when there is one, is charged by whoever constructed it.
        AllocationCounter.Count();
    }

    /// <summary>
    /// Creates a new pointer from a nil value.
    /// </summary>
    /// <param name="_"></param>
    public ж(NilType _)
    {
        m_val = default!;
        m_isNull = true;

        // Counted, even though Go's nil pointer is a word and allocates nothing: this constructor
        // really does hand the CLR heap an object, and the counter's contract is what the heap
        // received. A nil box that cost nothing to report would be a model, not a measurement.
        AllocationCounter.Count();
    }

    // Creates a box that HOLDS value but is nonetheless the nil pointer. Needed by
    // unsafe.Pointer, whose value IS its address: `unsafe.Pointer(uintptr(0))` must compare equal
    // to nil (Go treats the zero address as the nil pointer), yet the wrapped uintptr still has to
    // round-trip back out as 0. Protected rather than public — an ordinary ж<T> is nil or it holds
    // a value, never both.
    protected ж(in T value, bool isNull)
    {
        // Same two shapes, and the same charge, as the public value constructor above.
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            m_val = value;
            AllocationCounter.Count();
        }
        else
        {
            m_slot = [value];
            AllocationCounter.Count(2);
        }

        m_isNull = isNull;
    }

    /// <summary>
    /// Creates a new nil pointer.
    /// </summary>
    public ж() : this(nil) { }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Cannot get reference to value, source is not a valid array or slice pointer.</exception>
    public unsafe ref T Value
    {
        get
        {
            // Alias native memory at the address this box holds (never managed storage). Reading it
            // as a T that carries a managed reference is refused rather than faulted — see
            // s_nativeReadFabricatesReference.
            if (m_nativeAddr != 0)
                return ref nativeValue();

            // Get reference to standard pointer value
            if (m_structFieldRef is null && m_arrayIndexRef is null)
            {
                if (IsNull)
                    throw RuntimeErrorPanic.NilPointerDereference();

                // The pinnable slot IS the storage when it exists (see m_slot) — never a mirror of
                // m_val, so there is only ever one authority to read or write.
                return ref m_slot is null ? ref m_val! : ref MemoryMarshal.GetArrayDataReference(m_slot);
            }

            // Get reference to struct field
            if (m_structFieldRef is not null)
            {
                (object source, FieldRefFunc<T> fieldRefFunc, Delegate _) = m_structFieldRef!.Value;
                return ref fieldRefFunc(source);
            }

            // Get reference to array or slice element
            (IArray array, int index) = m_arrayIndexRef!.Value;

            if (array is IArray<T> typedArray)
                return ref typedArray[index];

            throw new InvalidOperationException("Cannot get reference to value, source is not a valid struct field, array or slice reference.");
        }
    }

    /// <summary>
    /// Gets a reference to the value slot WITHOUT the nil-pointer-dereference check that <see cref="Value"/>
    /// performs — identical to <see cref="Value"/> except it never throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used only where this box is a real heap allocation (created via <c>Ꮡ</c> / <c>heap</c>, so it is
    /// structurally a non-nil pointer) AND its value is a <em>reference</em> type that may legitimately
    /// be null — a heap-boxed pointer, slice, map, interface, or func <em>local</em> captured by a
    /// closure (a <c>ж&lt;ж&lt;T&gt;&gt;</c>, etc.). There <c>.Value</c> is a <em>read of the held value</em>,
    /// not a dereference of this box, so it must not panic when the held value is null: in Go,
    /// <c>*(&amp;p)</c> where <c>p</c> is a nil <c>*T</c>/slice/map yields the nil value, no dereference
    /// happens. Unlike <see cref="PointerExtensions.DerefOrNil{T}"/> (which returns a throwaway slot for a
    /// nil box, so writes are lost), this returns the <em>real</em> slot — reads and writes both persist,
    /// which the captured local requires. A genuine nil-pointer dereference (<c>~Ꮡp</c> / <c>Ꮡp.Value</c>)
    /// still routes through the strict <see cref="Value"/> and panics, preserving Go semantics.
    /// </para>
    /// </remarks>
    public unsafe ref T ValueSlot
    {
        get
        {
            // Alias native memory at the address this box holds (see Value).
            if (m_nativeAddr != 0)
                return ref nativeValue();

            if (m_structFieldRef is null && m_arrayIndexRef is null)
                return ref m_slot is null ? ref m_val! : ref MemoryMarshal.GetArrayDataReference(m_slot);

            if (m_structFieldRef is not null)
            {
                (object source, FieldRefFunc<T> fieldRefFunc, Delegate _) = m_structFieldRef!.Value;
                return ref fieldRefFunc(source);
            }

            (IArray array, int index) = m_arrayIndexRef!.Value;

            if (array is IArray<T> typedArray)
                return ref typedArray[index];

            throw new InvalidOperationException("Cannot get reference to value, source is not a valid struct field, array or slice reference.");
        }
    }

    /// <inheritdoc/>
    // Peeking at the held value is legitimate for ONE kind of box and ONE question: a STANDARD heap
    // box, asked whether dereferencing it would read managed storage that is not there. Every other
    // kind keeps its storage ELSEWHERE and leaves m_val the unused default, which for a
    // reference-typed T reads as null and must NOT be mistaken for a nil pointer — a native-backed
    // box (its address is m_nativeAddr), a struct-field reference and an array-element reference are
    // all real addresses (`&s.next` is an address even while the field holds nil). Pointer IDENTITY
    // never asks this question at all; it asks IsNilPointer.
    public bool IsNull => m_isNull || (m_nativeAddr == 0 && m_structFieldRef is null && m_arrayIndexRef is null && s_valueCanBeNull && HeldValueIsNull);

    // The held value, asked whether it is null — read from whichever slot actually holds it. A box
    // whose T contains no references keeps its value in the pinnable m_slot and leaves m_val the
    // unused default (see the constructor), so peeking at m_val alone answered for the wrong storage:
    // every ж<Nullable<T>> reported IsNull whatever it held. Unreachable from converted code (Go has
    // no Nullable) but wrong, and the guard below is what makes reading the right slot free.
    private bool HeldValueIsNull => (m_slot is null ? m_val : m_slot[0]) is null;

    // Whether `m_val is null` is a question T can even answer — true for a reference type and for a
    // Nullable<>, false for every other value type. It guards the value-peeking term above, and
    // the guard is about ALLOCATION rather than about the answer: `is null` on an UNCONSTRAINED type
    // parameter compiles to `box !T; ldnull; ceq`, so evaluating a term that is constant-false for a
    // struct T still allocated — and memcpy'd — a full copy of the pointee, on EVERY dereference,
    // since Value routes its standard-box branch through IsNull. A pointer to a large struct paid its
    // own size per read: os.File.WriteString spent 4,760 of its 9,208 bytes here, eight boxed copies
    // of the 592-byte `os.file` record, one per link of the of() chains the write path walks.
    // Computed from the type instead of by boxing default(T), so type initialization allocates
    // nothing either.
    private static readonly bool s_valueCanBeNull = !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;

    // Whether reading a NATIVE address as T would FABRICATE a managed reference — true exactly when
    // T's layout contains one. A native-backed box means "the pointee lives at this address", and
    // `Unsafe.AsRef<T>` over it is correct for pure data; where T holds a reference field, the
    // reference is manufactured from whatever bytes happen to sit there. That is a CLR type-safety
    // break, and its first use is an AccessViolationException — a process kill with no diagnostic,
    // no recover(), and a stack that names the innocent consumer rather than the reinterpret that
    // built the box.
    //
    // The class this catches is the unrepresentable half of `(*U)(unsafe.Pointer(p))`. golib's
    // Reinterpret takes the ALIASING arm wherever the managed model can express it (see
    // ReinterpretAliasesStorage) and the raw-ADDRESS arm where it cannot; a pointer-to-ARRAY target
    // over differently-typed storage is always the latter, because `array<U>` is a backing-store
    // REFERENCE plus bounds and no `U[]` view over a `V[]` exists. Dereferencing such a box reads an
    // `array<U>` STRUCT out of the pointed-at DATA. sha3's `(*[200]byte)(unsafe.Pointer(&d.a))` over
    // its `[25]uint64` sponge state was the witness: `copy(b, ab[:])` faulted inside slice<byte>'s
    // constructor and killed the process, and every TLS 1.3 ClientHello reached it through
    // mlkem768 key generation. (That site is hand-owned now; the class is not closed — see
    // docs/phase4/BOARD-next-validation-candidates.md.)
    //
    // Reported as a PanicException so the failure is CONTAINED and named: a converted test host
    // records it against the running test, `recover()` sees it, and a stack trace points at the
    // dereference. Nothing correct is lost — there is no defensible program that reads a fabricated
    // managed reference, so every site this fires on was already broken, and it was broken in the
    // one way that cost the most to diagnose. Computed from the type, so the branch folds away at
    // JIT/ILC time for every T that carries no references.
    //
    // ⚠ The VALUE-TYPE term is load-bearing, and `IsReferenceOrContainsReferences` alone is the
    // WRONG predicate — it conflates two opposite situations. Where T is a value-type surrogate
    // (`array<U>`, `slice<U>`, `@string`, `map<K,V>` — all readonly structs holding a backing
    // reference) the read manufactures that reference out of DATA, which is the fatal case above.
    // Where T is a reference TYPE (`unsafe.Pointer`, `ж<U>`, any class) the address names a
    // reference SLOT and reading it yields a real, heap-valid object reference — which is precisely
    // the managed-referent model the corpus is built on ("hold the ж<T>/object DIRECTLY, never a
    // nuint round-trip"). `time.syncTimer` is the witness for the second, and it was measured, not
    // reasoned about: `return ~Ꮡc.Reinterpret<channel<Time>, unsafe.Pointer>()` lands on the address
    // route (Pointer is a class, so no arm of ReinterpretAliasesStorage can engage), and the wider
    // predicate refused it — taking down `time.NewTimer`, and with it every crypto/tls test that
    // opens a pipe. The reference it hands back is type-CONFUSED rather than fabricated: it is the
    // real channel object, and nothing ever calls a Pointer member on it. Refusing it buys nothing
    // and costs a hot path.
    private static readonly bool s_nativeReadFabricatesReference =
        typeof(T).IsValueType && RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    // Whether dereferencing THIS box would be refused — a native alias whose pointee carries a
    // managed reference. Exposed for the DISPLAY path, which must answer for every box a program can
    // hold, including one it can never legally read (see PointerExtensions.PrintPointer).
    internal bool NativeReadWouldFabricate => m_nativeAddr != 0 && s_nativeReadFabricatesReference;

    private ref T nativeValue()
    {
        if (s_nativeReadFabricatesReference)
            throw RuntimeErrorPanic.UnrepresentableNativeReinterpret(typeof(T));

        unsafe
        {
            return ref Unsafe.AsRef<T>((void*)m_nativeAddr);
        }
    }

    /// <summary>
    /// Gets a flag indicating whether this box IS the nil pointer — the STRUCTURAL predicate
    /// (the nil-constructed / canonical typed-nil form), as opposed to <see cref="IsNull"/>,
    /// which additionally reports a standard heap box whose held reference-typed value is null
    /// (a non-nil pointer HOLDING a nil value, e.g. a captured <c>ж&lt;ж&lt;T&gt;&gt;</c> local).
    /// Everything about pointer IDENTITY keys off this one — equality, the hash, the order token,
    /// the reflection bridge's <c>IsNil</c>/<c>Elem</c>, and the dereference guards on
    /// <c>operator ~</c>; a Go pointer's nilness is a property of the pointer, never of the value
    /// it points at.
    /// </summary>
    public bool IsNilPointer => m_isNull;

    /// <summary>
    /// Gets a flag indicating whether this pointer ALIASES a native address rather than managed
    /// storage — see the <c>m_nativeAddr</c> field.
    /// </summary>
    internal bool IsNative => m_nativeAddr != 0;

    /// <summary>
    /// Gets the native address this pointer aliases, or zero when it is an ordinary managed box.
    /// </summary>
    internal nuint NativeAddress => m_nativeAddr;

    /// <summary>
    /// Gets a flag indicating whether this is a nil <em>standard</em> pointer — a plain heap pointer
    /// that IS nil — as opposed to a struct-field or array-element reference (which resolve through
    /// <see cref="Value"/> without a null check). The nil-safe
    /// <see cref="PointerExtensions.DerefOrNil{T}"/> re-alias accessor uses it to hand back a
    /// throwaway slot rather than the (absent) storage of a pointer walked to a nil terminator.
    /// STRUCTURAL, like every other identity question: a real heap box whose reference-typed value
    /// is null still has a real slot, and DerefOrNil must return THAT slot so a write through the
    /// re-alias persists (a captured <c>ж&lt;ж&lt;T&gt;&gt;</c> local — the very shape the walk
    /// produces on its last step).
    /// </summary>
    internal bool IsNilStandardPointer => m_structFieldRef is null && m_arrayIndexRef is null && m_isNull;

    /// <summary>
    /// Gets a pinned pointer to the value of type <typeparamref name="T"/>.
    /// </summary>
    internal PinnedBuffer PinnedBuffer
    {
        get
        {
            // Get reference to standard pointer value. The value-peeking IsNull is deliberate here:
            // pinning requires an unmanaged layout, so a null-HOLDING reference-typed box has nothing
            // to pin either way (Marshal.SizeOf<T> would throw); for the value-typed T that actually
            // reaches a pin, IsNull and IsNilPointer coincide.
            if (m_structFieldRef is null && m_arrayIndexRef is null)
            {
                if (IsNull)
                    throw RuntimeErrorPanic.NilPointerDereference();

                return new PinnedBuffer(Value, Marshal.SizeOf<T>());
            }

            // Get reference to struct field
            if (m_structFieldRef is not null)
            {
                (object source, FieldRefFunc<T> _, Delegate _) = m_structFieldRef!.Value;
                return new PinnedBuffer(Value, Marshal.SizeOf(source));
            }

            // Get reference to array or slice element
            (IArray array, int _) = m_arrayIndexRef!.Value;

            if (array is IArray<T>)
                return new PinnedBuffer(Value, array.Length);

            throw new InvalidOperationException("Cannot get pinned buffer to value, source is not a valid struct field, array or slice reference.");
        }
    }

    internal (IArray, int)? ArrayRef => m_arrayIndexRef;

    /// <summary>
    /// Gets the managed array/slice storage this pointer's referent is an element of, together with
    /// the referent's ABSOLUTE index inside it.
    /// </summary>
    /// <param name="backing">Canonical backing store, on success.</param>
    /// <param name="index">Referent's index inside <paramref name="backing"/>, on success.</param>
    /// <returns><c>true</c> when the pointer addresses managed element storage; <c>false</c> for a
    /// heap box, a struct-field reference, a native address, or a reinterpreting pointer over
    /// differently-typed storage (no <c>T[]</c> view of which exists in the managed model).</returns>
    /// <remarks>
    /// Resolved through <see cref="CanonicalElement"/>, so a pointer taken through a re-sliced view
    /// — or through an <see cref="array{T}.Alias"/> window — names the same absolute element Go's
    /// would. The narrower <see cref="TryGetElementWindow"/> answers the same question for callers
    /// that need a fixed-length window and have their own fallback when one does not fit;
    /// <see cref="array{T}.AliasPointer"/> uses this one because Go's array length there is an upper
    /// bound rather than a real extent.
    /// </remarks>
    internal bool TryGetElementStorage([NotNullWhen(true)] out T[]? backing, out nint index)
    {
        backing = null;
        index = 0;

        if (m_arrayIndexRef is null)
            return false;

        (object storage, nint absoluteIndex) = CanonicalElement(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);

        if (storage is not T[] typed || absoluteIndex < 0 || absoluteIndex > typed.Length)
            return false;

        backing = typed;
        index = absoluteIndex;

        return true;
    }

    /// <summary>
    /// Gets the <paramref name="length"/>-element slice window this pointer's referent STARTS, when
    /// the referent is an element of managed array/slice storage and the window fits inside it.
    /// </summary>
    /// <param name="length">Element count the window must cover.</param>
    /// <param name="window">Aliasing slice over the referent's own storage, on success.</param>
    /// <returns><c>true</c> when an aliasing window exists; <c>false</c> to leave the caller its
    /// own fallback.</returns>
    /// <remarks>
    /// This is what makes <c>unsafe.Slice(&amp;s[i], n)</c> behave as Go's does — the result must
    /// ALIAS the original backing store, not snapshot it, or writes through the rebuilt slice are
    /// silently lost. crypto/subtle's <c>xorBytes</c> is the canonical case: <c>XORBytes</c> hands
    /// it <c>&amp;dst[0]</c>, it rebuilds <c>dst</c> with <c>unsafe.Slice</c>, and every XOR result
    /// is written through that slice — against a snapshot, <c>XORBytes</c> wrote nothing at all.
    /// The window is resolved through <see cref="CanonicalElement"/>, so a pointer taken through a
    /// re-sliced view addresses the same absolute element Go's would. Capacity is bounded at
    /// <paramref name="length"/>, matching Go's <c>len == cap == n</c> result.
    /// </remarks>
    internal bool TryGetElementWindow(int length, out slice<T> window)
    {
        window = default;

        if (length < 0 || m_arrayIndexRef is null)
            return false;

        (IArray array, int index) = m_arrayIndexRef.Value;
        (object storage, nint absoluteIndex) = CanonicalElement(array, index);

        // Only REAL managed element storage can be aliased. A reinterpreting pointer (a Go
        // `(*U)(unsafe.Pointer(&b[0]))` over a differently-typed array) is a struct-field ref, not
        // an array ref, and never reaches here — a T[] view over a differently-typed backing array
        // does not exist in the managed model, so those callers keep the snapshot.
        if (storage is not T[] backing || absoluteIndex < 0 || absoluteIndex + length > backing.Length)
            return false;

        window = new slice<T>(backing, absoluteIndex, absoluteIndex + length, absoluteIndex + length);

        return true;
    }

    /// <inheritdoc/>
    public ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc)
    {
        return new ж<TElem>(this, fieldRefFunc);
    }

    /// <inheritdoc/>
    public ж<TElem> of<TElem>(FieldRefFunc<T, TElem> fieldRefFunc)
    {
        // fieldRefFunc doubles as the equality-identity token: the untyped wrapper is derived from
        // it, and the accessor delegate compares equal across call sites (same method group).
        return new ж<TElem>(this, FieldRefWrappers<TElem>.For(fieldRefFunc), fieldRefFunc);
    }

    // Untyped `object source` wrappers around the typed field accessors, one per accessor rather
    // than one per call. The wrapper closes over nothing but the accessor, so it is a pure function
    // OF the accessor — and the accessor is a static method group, which the compiler caches, so a
    // whole program mints one wrapper per (T, TElem, field). Minting it per call instead cost a
    // display class plus a delegate on every `&x.field` in the corpus (88 bytes, ~11 times per
    // os.File.WriteString) for a value that was identical each time. The table's keys are WEAK, so
    // an accessor that is genuinely per-call — a lambda rather than a method group — leaves no
    // permanent entry behind.
    private static class FieldRefWrappers<TElem>
    {
        private static readonly ConditionalWeakTable<FieldRefFunc<T, TElem>, FieldRefFunc<TElem>> s_wrappers = new();

        public static FieldRefFunc<TElem> For(FieldRefFunc<T, TElem> fieldRefFunc)
        {
            return s_wrappers.GetValue(fieldRefFunc, Wrap);
        }

        private static FieldRefFunc<TElem> Wrap(FieldRefFunc<T, TElem> fieldRefFunc)
        {
            return getFieldRef;

            ref TElem getFieldRef(object structPtr)
            {
                ж<T> typedPtr = (ж<T>)structPtr;

                // Resolve the parent value through `Value`, not `m_val` — when this pointer is itself
                // a field reference (or array element) its real storage lives behind `Value` and
                // `m_val` is an empty default. This is the case for a nested `of()` chain, e.g.
                // `Ꮡb.of(Bool.Ꮡu).of(Uint8.Ꮡvalue)` where the intermediate `ж<Uint8>` is a field ref —
                // reading `m_val` would alias a throwaway copy and lose writes.
                return ref fieldRefFunc(ref typedPtr.Value);
            }
        }
    }

    /// <inheritdoc/>
    public ж<TElem> of<TElem>(FieldPtrFunc<T, TElem> fieldPtrFunc)
    {
        // No box is minted here: the accessor crossed an embedded POINTER and already rooted the
        // reference at the allocation that holds the field, which is where Go roots it. Building a
        // field ref over THIS box instead would name the outer allocation, and `&f.pfd` taken
        // through the outer would stop being the same pointer as `&f.file.pfd` taken through the
        // inner (see FieldPtrFunc's remarks).
        return fieldPtrFunc(ref Value);
    }

    // Materializes a value-type IArray implementer's lazy backing on the REAL STORAGE (see the
    // comment in at<Telem> below): the wrapper is boxed, its `Source` getter materializes the
    // backing on the boxed copy, and the whole wrapper — a struct over a SHARED backing
    // reference — is copied back, which lands that reference in the caller's storage. One box on
    // a rare shape; nothing for every other T (the guard is a JIT/ILC per-instantiation constant).
    //
    // ⚠ This was a reflection-built constrained delegate (GetMethod + MakeGenericMethod(typeof(T))
    // in the static initializer) until 2026-08-10, and that shape is FATAL under Native AOT: the
    // value-type generic instantiation is reachable only through reflection, ILC generates no
    // native code for it, and the FIRST ж<> type-init of any AOT-published program threw
    // NotSupportedException — all 13 perf-suite AOT binaries died at startup. No JIT-hosted gate
    // can see this class of defect; the perf suite's Verify phase is the corpus's only AOT
    // execution and is the gate that caught it.
    private static void ensureArrayBacking(ref T value)
    {
        if (typeof(T).IsValueType && value is IArray boxedView)
        {
            _ = boxedView.Source; // materializes the lazy backing on the boxed copy
            value = (T)(object)boxedView; // copy the wrapper (and its backing reference) back
        }
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Cannot get pointer element at index, type is not an array or slice.</exception>
    /// <exception cref="IndexOutOfRangeException">Index is out of range for array or slice.</exception>
    public ж<Telem> at<Telem>(nint index)
    {
        // An Array-class [GoType] wrapper's ZERO VALUE has a null lazy backing that its members
        // allocate on first touch. The `Value is IArray<Telem>` pattern below COPIES the wrapper —
        // touched on the copy, the backing allocates on the copy and the REAL storage stays
        // virgin, silently dropping every write through the returned element pointer (the
        // pallocBits lesson, resurfacing at the box-element seam). Materialize the backing on
        // the real storage FIRST (box, touch, copy the wrapper back — AOT-safe, see
        // ensureArrayBacking); the copies below then SHARE the materialized backing
        // (array<T> is a readonly struct over a shared T[]).
        ensureArrayBacking(ref Value);

        // Read through `Value`, not `m_val` — when this pointer is itself a field reference (from
        // `of(...)`) or an array-element reference, the real array storage lives behind `Value` and
        // `m_val` is an empty default. Reading `m_val` would miss the array entirely (spurious "not
        // an array" / null deref). This is the `Ꮡg.of(T.ᏑarrayField).at<E>(i)` form — the address of
        // an element of an array FIELD of a boxed struct (a boxed global, a pointer param/local).
        // `array<T>` is a readonly struct over a shared backing `T[]`, so the copy `Value` yields still
        // aliases the real elements (writes through the returned element pointer land).
        if (Value is not IArray<Telem> array)
            throw new InvalidOperationException("Cannot get pointer to element at index, type is not an array or slice.");
        
        if (!array.IndexIsValid(index))
            throw new IndexOutOfRangeException("Index is out of range for array or slice.");

        return new ж<Telem>(array, (int)index);
    }

    /// <summary>
    /// Gets a pointer to the element at <paramref name="index"/> of an <see cref="array{T}"/> or
    /// <see cref="slice{T}"/> FIELD of the pointed-to struct — the address of `x.field[index]`.
    /// </summary>
    /// <remarks>
    /// Combines <see cref="of{TElem}(FieldRefFunc{T,TElem})"/> and <see cref="at{Telem}(nint)"/> into a
    /// single call so the element type is INFERRED from the field accessor's return type, letting the
    /// converter emit `Ꮡx.at(T.Ꮡfield, i)` instead of the noisier `Ꮡx.of(T.Ꮡfield).at&lt;E&gt;(i)`.
    /// One overload per field-accessor shape (`ref T`/`object`) and collection kind (array/slice); each
    /// just forwards to the existing `of(...).at&lt;TElem&gt;(index)`.
    /// </remarks>
    public ж<TElem> at<TElem>(FieldRefFunc<T, array<TElem>> fieldRefFunc, nint index) => of(fieldRefFunc).at<TElem>(index);

    /// <inheritdoc cref="at{TElem}(FieldRefFunc{T,array{TElem}},nint)"/>
    public ж<TElem> at<TElem>(FieldRefFunc<array<TElem>> fieldRefFunc, nint index) => of(fieldRefFunc).at<TElem>(index);

    /// <inheritdoc cref="at{TElem}(FieldRefFunc{T,array{TElem}},nint)"/>
    public ж<TElem> at<TElem>(FieldRefFunc<T, slice<TElem>> fieldRefFunc, nint index) => of(fieldRefFunc).at<TElem>(index);

    /// <inheritdoc cref="at{TElem}(FieldRefFunc{T,array{TElem}},nint)"/>
    public ж<TElem> at<TElem>(FieldRefFunc<slice<TElem>> fieldRefFunc, nint index) => of(fieldRefFunc).at<TElem>(index);

    /// <inheritdoc cref="at{TElem}(FieldRefFunc{T,array{TElem}},nint)"/>
    /// <remarks>The pointer-crossing promotion shapes (see <see cref="FieldPtrFunc{T,TElem}"/>).</remarks>
    public ж<TElem> at<TElem>(FieldPtrFunc<T, array<TElem>> fieldPtrFunc, nint index) => of(fieldPtrFunc).at<TElem>(index);

    /// <inheritdoc cref="at{TElem}(FieldPtrFunc{T,array{TElem}},nint)"/>
    public ж<TElem> at<TElem>(FieldPtrFunc<T, slice<TElem>> fieldPtrFunc, nint index) => of(fieldPtrFunc).at<TElem>(index);

    /// <inheritdoc />
    public override string ToString()
    {
        // $"&{m_val?.ToString() ?? "nil"}";
        return this.PrintPointer();
    }

    /// <inheritdoc/>
    public bool Equals(ж<T>? other)
    {
        // A null REFERENCE and a structurally nil box are the same Go nil pointer — the two
        // representations coexist (`(ж<T>)default!` emits null; `ж<T> p = nil` and the canonical
        // typed-nil box are IsNilPointer boxes) and must compare equal, as Go's `(*T)(nil) == nil`.
        if (other is null)
            return m_isNull;

        if (ReferenceEquals(this, other))
            return true;

        // A nil pointer compares equal only to another nil pointer. STRUCTURAL nil only
        // (m_isNull): a real heap box whose held reference-typed value is null (a captured
        // `ж<ж<T>>` local holding a nil *T) is a NON-nil pointer holding a nil value — two such
        // distinct boxes are distinct addresses in Go (&p1 != &p2), and such a box never equals
        // nil. The value-peeking IsNull is for dereference guards, never pointer identity.
        if (m_isNull || other.m_isNull)
            return m_isNull && other.m_isNull;

        // Go pointer comparison is by identity — the same storage location — NOT by the pointed-to
        // value. A value-comparison fallback is both wrong (two distinct addresses that happen to
        // hold equal values are not equal pointers in Go) and unsound: it infinitely recurses for a
        // self-referential struct, one whose fields include a ж<T> back to its own type (e.g.
        // container/ring's `Ring.next *Ring`), since the struct's own Equals compares those fields.

        // Two boxes ALIASING the same native address are the same Go pointer — that address is the
        // whole of their identity (`(*T)(unsafe.Pointer(p)) == (*T)(unsafe.Pointer(p))` after a
        // uintptr round-trip, which produces a fresh box each time).
        if (m_nativeAddr != 0 || other.m_nativeAddr != 0)
            return m_nativeAddr == other.m_nativeAddr;

        // Pointer into a struct field (`Ꮡx.of(T.ᏑField)`): same source object and field accessor.
        // The comparison uses the field IDENTITY token (Item3) — the original accessor delegate —
        // not the stored ref function, which the typed `of(...)` overload wraps in a PER-CALL
        // closure: comparing wrappers made every distinct `&x.field` box unequal, so `&x.f == &x.f`
        // was false (Go pointer identity violated) and the address-keyed runtime semaphores in the
        // hand-owned sync/internal-poll implementations never paired release with acquire.
        if (m_structFieldRef is not null || other.m_structFieldRef is not null)
        {
            if (m_structFieldRef is null || other.m_structFieldRef is null)
                return false;

            (object source1, FieldRefFunc<T> _, Delegate fieldId1) = m_structFieldRef.Value;
            (object source2, FieldRefFunc<T> _, Delegate fieldId2) = other.m_structFieldRef.Value;

            return SameSource(source1, source2) && fieldId1.Equals(fieldId2);
        }

        // Pointer into an array/slice element: same backing STORAGE and same ABSOLUTE element index
        // (see CanonicalElement — the referent alone is a per-call VIEW or HEADER, never the storage).
        if (m_arrayIndexRef is not null || other.m_arrayIndexRef is not null)
        {
            if (m_arrayIndexRef is null || other.m_arrayIndexRef is null)
                return false;

            (IArray array1, int index1) = m_arrayIndexRef.Value;
            (IArray array2, int index2) = other.m_arrayIndexRef.Value;

            (object storage1, nint element1) = CanonicalElement(array1, index1);
            (object storage2, nint element2) = CanonicalElement(array2, index2);

            return ReferenceEquals(storage1, storage2) && element1 == element2;
        }

        // Two standard heap pointers, each a distinct allocation: distinct addresses, never equal
        // (the ReferenceEquals(this, other) check above already handled the same-box case, and a Go
        // variable whose address is taken is heap-boxed ONCE, so `&x == &x` is that same-box case).
        // A pointer's identity is its STORAGE and nothing else — never the value it holds. Deriving
        // identity from the held value for a reference-typed T makes two distinct boxes wrapping one
        // referent compare equal, and all three of these follow: `&c == &d` reports true for distinct
        // `*int` variables that happen to hold the same pointer; `map[**int]V{&c: …, &d: …}` collapses
        // to a single entry; and a box's hash MUTATES when its pointee is assigned, so a key inserted
        // while the pointee was nil can never be found again.
        return false;
    }

    // Resolves whether two field references were taken through the SAME Go pointer. A source is
    // compared by pointer IDENTITY, not by object reference, because an `of()` chain mints a FRESH
    // intermediate box on every access: `Ꮡo.of(Outer.Ꮡin).of(Inner.Ꮡv)` allocates a new ж<Inner>
    // each time it is evaluated, so a reference comparison made `&o.in.v == &o.in.v` FALSE at depth
    // two while it was correctly true at depth one. Everything that rests on pointer identity
    // followed: a `map[*T]V` grew a second entry for the same address, and the address-keyed runtime
    // semaphores in the hand-owned sync / internal-poll implementations paired a release with a
    // DIFFERENT bucket than the acquire — which is why closing an os.File whose Read was still in
    // flight blocked forever in runtime_Semacquire (&fd.csema is reached as file→pfd→csema, depth
    // two). ReferentObject already walked the chain for lifetime questions; equality, the hash and
    // the order token now agree with it.
    //
    // Only a POINTER source is resolved recursively. A source that is not a pointer box is storage
    // in its own right, and object identity is exactly the right question for it — deferring to
    // Equals there would compare boxed VALUES, which is never pointer identity.
    private static bool SameSource(object source1, object source2)
    {
        return ReferenceEquals(source1, source2) ||
               (source1 is INilPointer && source2 is INilPointer && source1.Equals(source2));
    }

    // The identity hash of a field reference's source, resolved through an `of()` chain on the same
    // terms as SameSource above so equal pointers always land in the same bucket.
    private static int SourceIdentityHash(object source)
    {
        return source is INilPointer parent ? parent.GetHashCode() : RuntimeHelpers.GetHashCode(source);
    }

    // Reduces an array-index referent to the identity of its ACTUAL storage plus the ABSOLUTE index
    // of the element within that storage. Used by Equals/GetHashCode above so two pointers to the
    // SAME element compare (and hash) as the same address no matter which view they were taken
    // through — Go's `&s[0] == &s[0]`, `&s[1:][0] == &s[1]`, `&a[:][i] == &a[i]`. The referent as
    // stored is never the storage itself:
    //
    //   - A PinnedBuffer is a per-access VIEW over its pinned storage (`@string.buffer` creates one
    //     per unsafe.StringData call), so it yields the object it pins — two pointers to the same
    //     string data are equal (Go compares addresses, never view instances). Falls back to the
    //     view when nothing is pinned.
    //   - A slice<T> is a HEADER (backing array + low + len + cap) over storage it SHARES. The
    //     `Ꮡ(target, index)` overload takes `in IArray<T>`, so every call BOXES the header struct
    //     afresh: comparing the boxes made `&s[0] == &s[0]` false (Go pointer identity violated) and
    //     put two aliasing element pointers in different map buckets. Yielding the backing array plus
    //     `Low + index` also makes a re-sliced window, a re-slice of a re-slice, and an in-capacity
    //     `append` result compare equal to the original — every Go alias of one element.
    //   - An array<T> (and a generated named-array wrapper) is likewise a struct over a shared T[],
    //     reached through the non-generic `IArray.Source`, which both expose RAW. (A Go by-value
    //     array COPY takes golib's `.Clone()`, giving it its own backing, so distinct Go arrays never
    //     canonicalize to the same storage. Only a slice HEADER's `Source` materializes a detached
    //     copy, and slices never reach that arm.)
    //
    // A foreign IArray implementer keeps the referent as its own storage — the pre-existing behavior.
    private static (object storage, nint index) CanonicalElement(IArray array, nint index)
    {
        switch (array)
        {
            case PinnedBuffer pinned:
                return (pinned.PinnedTarget ?? array, index);

            case slice<T> slice:
                return slice.m_array is null ? (array, index) : (slice.m_array, slice.Low + index);

            // A NAMED slice type wraps a slice<T> window it does not expose directly; its full-window
            // interface sub-slice hands back the shared header (the same unwrap slice<T>'s ISlice<T>
            // constructor performs — `Source` cannot serve, it copies).
            case ISlice<T> view when view.Slice((nint)0, view.Length) is slice<T> shared && shared.m_array is not null:
                return (shared.m_array, shared.Low + index);

            // An array<T> is a struct over a shared T[] — normally the WHOLE of it, but Go's
            // slice-to-array-POINTER conversion (`(*[N]T)(s)`, array<T>.Alias) makes it a WINDOW,
            // and an element pointer taken through that window must name the same absolute element
            // as one taken through the slice it aliases (Go: `&(*[4]byte)(s)[0] == &s[0]`).
            case array<T> arr when arr.Source is not null:
                return (arr.Source, arr.Low + index);

            default:
                return (array.Source ?? (object)array, index);
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ж<T> other && Equals(other);
    }

    /// <summary>
    /// Gets the stable address token for this pointer (see <see cref="INilPointer.PointerOrderToken"/>):
    /// nil → 0; a native alias → its real address; an array/slice-element reference → the canonical
    /// backing storage's identity in the high bits with the ABSOLUTE element index below, so
    /// same-storage element pointers order by index exactly like Go addresses; a struct-field
    /// reference → the source identity combined with the field identity token; a heap box → the
    /// box's own identity (the box IS the storage). Consistent with
    /// <see cref="Equals(ж{T})"/>: equal pointers always produce equal tokens (the converse only
    /// holds within one backing storage — tokens are order keys, never an identity substitute).
    /// virtual so <c>unsafe.Pointer</c> (whose VALUE is the address) can answer with the address itself.
    /// </summary>
    public virtual nuint PointerOrderToken
    {
        get
        {
            if (IsNilPointer)
                return 0;

            if (m_nativeAddr != 0)
                return m_nativeAddr;

            if (m_arrayIndexRef is not null)
            {
                (object storage, nint element) = CanonicalElement(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);
                return unchecked((nuint)(((ulong)(uint)RuntimeHelpers.GetHashCode(storage) << 32) | (uint)element));
            }

            if (m_structFieldRef is not null)
            {
                // The source identity is resolved through an `of()` chain (SameSource), so that the
                // documented invariant holds at every depth: equal pointers produce equal tokens.
                (object source, FieldRefFunc<T> _, Delegate fieldId) = m_structFieldRef.Value;
                return unchecked((nuint)(((ulong)(uint)SourceIdentityHash(source) << 32) | (uint)fieldId.GetHashCode()));
            }

            // A standard heap box IS the storage it addresses — its own identity, never the held
            // value's (see Equals: a token derived from the pointee changes when the pointee is
            // assigned, which is not something an address does).
            return (nuint)(uint)RuntimeHelpers.GetHashCode(this);
        }
    }

    /// <summary>
    /// Gets the object whose lifetime is the referenced Go allocation (see
    /// <see cref="INilPointer.ReferentObject"/>): an array/slice-element reference → the canonical
    /// backing storage (so <c>Ꮡ(buf, 0)</c>'s throwaway box resolves to <c>buf</c>'s own array); a
    /// struct-field reference → the containing allocation, resolved THROUGH any intermediate field
    /// boxes to the root; otherwise this box, which IS the allocation — a standard heap box, whatever
    /// its pointee's type, and a native alias, whose real storage is unmanaged and outside the GC's
    /// reach.
    /// </summary>
    /// <remarks>
    /// Deliberately consistent with <see cref="Equals(ж{T})"/>: two boxes that are the same Go
    /// pointer resolve to the SAME referent object, which is what makes a lifetime registration
    /// keyed on it behave like Go's (a second <c>SetFinalizer</c> through a different box for the
    /// same address is correctly seen as already-set) and what lets a copy detector compare the
    /// allocation it was initialized in against the one it is being used from. The one place the two
    /// part company is the native alias, which <see cref="Equals(ж{T})"/> identifies by the address
    /// it wraps while the referent is the box: an unmanaged address names no managed allocation, so
    /// there is nothing for a GC-keyed lifetime question to be asked about in the first place.
    /// </remarks>
    public virtual object ReferentObject
    {
        get
        {
            if (m_arrayIndexRef is not null)
                return CanonicalElement(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2).storage;

            // Recurse: the source of a nested field ref is a per-call intermediate box, never an
            // allocation of its own (`Ꮡouter.of(Ꮡinner).of(Ꮡfield)` mints a fresh ж<Inner> each
            // access). The chain is finite — each `of` wraps a strictly outer pointer.
            if (m_structFieldRef is not null)
            {
                return m_structFieldRef.Value.Item1 is INilPointer parent
                    ? parent.ReferentObject
                    : m_structFieldRef.Value.Item1;
            }

            // A standard heap box IS the allocation it addresses, so it is its own referent — the
            // same rule Equals and the hash follow, and for the same reason: the box's identity is
            // the address, never the identity of whatever value happens to be stored at it. That
            // holds for a reference-typed pointee too — `&m` for a map variable names the VARIABLE's
            // storage, a different allocation from the map header the variable happens to hold, and
            // it is the variable Go finalizes.
            return this;
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Identity-based hash, consistent with the identity Equals above. Hashing m_val directly
        // would recurse for a self-referential struct (its hash includes its ж<T> fields), so hash
        // by the referenced storage instead. STRUCTURAL nil, like Equals: the value-peeking IsNull
        // made the hash of a real box vary with its pointee (null → 0, assigned → the pointee's
        // hash), so a pointer key stored while its pointee was nil was unfindable afterward.
        if (IsNilPointer)
            return 0;

        if (m_structFieldRef is not null)
            return SourceIdentityHash(m_structFieldRef.Value.Item1);

        if (m_arrayIndexRef is not null)
        {
            (object storage, nint element) = CanonicalElement(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2);
            return System.HashCode.Combine(RuntimeHelpers.GetHashCode(storage), element);
        }

        // A native alias hashes by the address it aliases, so two boxes over one address (which
        // Equals reports as the same pointer) land in the same bucket.
        if (m_nativeAddr != 0)
            return m_nativeAddr.GetHashCode();

        // Standard heap pointer: the box IS the storage, so its own identity is the address.
        return RuntimeHelpers.GetHashCode(this);
    }

    // WISH: Would be super cool if this operator supported "ref" return, like:
    //     public static ref T operator ~(ж<T> value) => ref value.m_value;
    // or ideally C# supported an overloaded ref-based pointer operator, like:
    //     public static ref T operator *(ж<T> value) => ref value.m_value;
    // or maybe even an overall "ref" operator for a class, like:
    //     public static ref T operator ref(ж<T> value) => ref value.m_value;
    // Converted code like this:
    //     var v = 2; var vp = ptr(v);
    //     vp.Value = 999;
    // Could then become:
    //     var v = 2; var vp = ptr(v);
    //     ~vp = 999; // or
    //     *vp = 999; // or
    //     ref vp = 999;
    // As it stands, this operator just returns a copy of the structure value:

    /// <summary>
    /// Dereferences the heap allocated reference to the value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">Pointer value to dereference.</param>
    /// <returns>Dereferenced pointer value.</returns>
    public static T operator ~(ж<T> value)
    {
        // STRUCTURAL nil: `*p` panics when the POINTER is nil. Where p is a real address whose
        // pointee happens to be a nil interface / pointer / slice / map, Go yields that nil value —
        // `*(&i)` with `i == nil` is nil, not a panic — so the value-peeking IsNull cannot be the
        // guard (it reported every such address as nil, and every field-reference box over a
        // reference-typed T as well).
        if (value.IsNilPointer)
            throw RuntimeErrorPanic.NilPointerDereference();

        // Resolve through the slot, not the raw `m_val` field: for a struct-field reference (`Ꮡx.of(T.Ꮡf)`)
        // or an array-element reference, the real storage lives behind the slot and `m_val` is an empty
        // default — returning `m_val` would yield a zero-valued copy (so `(~(&x.field)).sub` read 0, e.g.
        // a `c := &b.w; c.a` field-chain read). ValueSlot rather than Value because the nil-pointer
        // question is already answered above, and Value would re-ask it the value-peeking way.
        return value.ValueSlot;
    }

    static T IPointer<T>.operator ~(IPointer<T> value)
    {
        // STRUCTURAL nil where the implementer can answer it — ж<T> and every generated named-pointer
        // wrapper implement INilPointer; a foreign implementer keeps the IsNull question it has.
        if (value is INilPointer nilable ? nilable.IsNilPointer : value.IsNull)
            throw RuntimeErrorPanic.NilPointerDereference();

        // Read the slot for a raw box (see the operator above); a wrapper resolves through its own
        // Value, which forwards to the inner box.
        return value is ж<T> box ? box.ValueSlot : value.Value;
    }

    // C# has no user-defined ref-returning operator, so `~p` hands back a COPY of the pointed-at
    // value rather than an alias to it. Callers that must WRITE through the pointer use the `Value`
    // ref-property instead. Suggestion filed upstream:
    // https://github.com/dotnet/roslyn/issues/45881

    // A `where T : unmanaged` box could return a real machine pointer instead —
    //     public static T* operator ~(ж<T> value) => value.m_value;
    // — but that fully unmanaged path cascades, which is why ж<T> stays a managed box:
    //   1. `unmanaged` excludes every T that contains a reference, i.e. most Go structs, so an
    //      unmanaged box can only ever be a SECOND, parallel pointer type — never a replacement.
    //   2. Holding the pointee at a stable address needs a pinning GCHandle, and a GCHandle needs a
    //      finalizer to release it or it leaks — so the box has to be a class, not a struct.
    //   3. A class is non-blittable, so any struct with a pointer FIELD becomes non-blittable too,
    //      and taking a pointer to THAT struct is then impossible — the case Go code hits constantly
    //      (`type node struct { next *node }`).
    // An earlier `ptr<T>`/`ptr2<T>` prototype under `golib/experimental/` worked this path to its
    // dead end and was removed once ж<T> settled; `git log --diff-filter=D -- src/core/golib/experimental`
    // recovers it.

    public static bool operator ==(ж<T>? value1, ж<T>? value2)
    {
        // A null reference is the nil pointer, equal to the other side's null reference OR
        // structurally nil box (the two nil representations — see Equals).
        return value1 is null ? value2 is null || value2.m_isNull : value1.Equals(value2);
    }

    public static bool operator !=(ж<T>? value1, ж<T>? value2)
    {
        return !(value1 == value2);
    }

    // Enable comparisons between nil and ж<T> instance. STRUCTURAL nil (see Equals): a heap box
    // holding a null reference-typed value is a non-nil pointer holding nil — `&p == nil` is
    // false in Go even when p itself is nil.
    public static bool operator ==(ж<T>? value, NilType _)
    {
        return value is null || value.m_isNull;
    }

    public static bool operator !=(ж<T>? value, NilType nil)
    {
        return !(value == nil);
    }

    public static bool operator ==(NilType nil, ж<T>? value)
    {
        return value == nil;
    }

    public static bool operator !=(NilType nil, ж<T>? value)
    {
        return value != nil;
    }

    /// <summary>
    /// Gets the CANONICAL typed nil box for this pointer type — the shared instance every
    /// nil→pointer conversion yields, so a typed nil boxed into an interface carries its Go type
    /// (<c>any((*T)(nil)) != nil</c>, <c>%T</c>, <c>reflect.TypeOf((*T)(nil)).Elem()</c>) and two
    /// typed nils of one type are reference-identical wherever the comparison is an untyped
    /// <c>object</c> reference compare. Write-protected structurally: the <see cref="Value"/>
    /// ref-getter throws <see cref="RuntimeErrorPanic.NilPointerDereference"/> on a nil box
    /// before any ref exists, so no store through the shared instance is possible.
    /// </summary>
    public static ж<T> NilBox { get; } = new(nil);

    public static implicit operator ж<T>(NilType _)
    {
        // The canonical instance, not a fresh box — see NilBox.
        return NilBox;
    }

    // The reinterpreting ref accessor for PointerExtensions.Reinterpret, as a static method rather
    // than a lambda so that two reinterprets of one box compare EQUAL: ж equality compares the source
    // object and the field identity delegate, and Delegate.Equals compares method + target — equal
    // across call sites for the same static method. Go requires
    // `(*U)(unsafe.Pointer(p)) == (*U)(unsafe.Pointer(p))`.
    internal static ref TDst ReinterpretRef<TDst>(object source)
    {
        return ref Unsafe.As<T, TDst>(ref ((ж<T>)source).ValueSlot);
    }

    // EXPLICIT by design: reinterpreting a raw address as a pointer is the runtime-unsafe reinterpret
    // seam — never something to happen silently. Converter-emitted reinterprets always use explicit
    // cast syntax ((ж<T>)(uintptr)(p)). As an implicit conversion it also made every uintptr argument
    // ambiguous between an unsafe.Pointer overload and a ж<T> overload (CS0121 — runtime's free
    // `add(p, x)` vs the `(*notInHeap).add` static companion).
    //
    // The result ALIASES the address (see m_nativeAddr) — it must never box a COPY of the pointed-at
    // value, because a copy silently discards the address: pointer arithmetic then walks the GC heap,
    // and handing the pointer back to a native API frees GC memory (STATUS_HEAP_CORRUPTION).
    // Aliasing keeps `uintptr(unsafe.Pointer(p))` an exact round-trip, as Go requires.
    public static unsafe explicit operator ж<T>(uintptr value)
    {
        // A pointer to MANAGED storage has no address to have been converted from: what a reflect
        // projection handed out was an order token (see ManagedPointerTokens). Recover the box that
        // token named, so `(*T)(v.Addr().UnsafePointer())` yields a pointer to the very storage the
        // reflect Value aliased — instead of a native box over a number that is not an address.
        if (ManagedPointerTokens.Resolve((nuint)value.Value) is ж<T> aliased)
            return aliased;

        return new ж<T>((nuint)value.Value);
    }

    public static unsafe implicit operator uintptr(ж<T> value)
    {
        // A native-backed pointer round-trips to the EXACT address it aliases — it is not managed
        // storage, so there is nothing to pin and no copy to take.
        if (value is not null && value.m_nativeAddr != 0)
            return (uintptr)value.m_nativeAddr;

        // A NIL pointer's address is 0, matching Go's `uintptr(unsafe.Pointer(nil)) == 0`. A nil box
        // has no storage to pin, so taking `&value.Value` would dereference it and throw — but the
        // syscall wrappers legitimately pass nil pointers whose numeric address is simply 0
        // (syscall.Write hands writeFile a nil `*Overlapped` for a synchronous write, then passes
        // `uintptr(unsafe.Pointer(overlapped))` to the trampoline). Return 0 instead of throwing.
        //
        // The value-peeking IsNull is KEPT here deliberately: this is the address model, and a
        // reference-typed pointee has no address to report — pinning the managed reference slot
        // would hand out a token valid only inside this statement. Whatever crosses into native
        // code has a value-typed pointee, and there IsNull and IsNilPointer coincide.
        if (value is null || value.IsNull)
            return default;

        // A pointer to a Go fixed array (`unsafe.Pointer(&arr)`): the native address must reference the
        // array's DATA (element 0), pinned so a syscall can fill it in and the managed reads afterward
        // observe the result — not the transient address of the `array<T>` struct wrapper. Slices keep
        // header semantics (`&s` is the slice header in Go), so they fall through to the value-slot path.
        if (value.Value is IArray arr && arr is not ISlice)
            return (uintptr)value.pinnedArrayData(arr);

        // Hold the storage still BEFORE reading its address (see EnsureStableAddress): `fixed` pins
        // only for its own statement, and the address outlives that statement by definition — it is
        // being converted to an integer precisely so it can be handed somewhere else.
        value.EnsureStableAddress();

        fixed (void* ptr = &value.Value)
            return (uintptr)ptr;
    }

    // Aliases the address rather than copying the pointed-at value — see the uintptr operator above.
    public static unsafe implicit operator ж<T>(void* value)
    {
        return new ж<T>((nuint)value);
    }

    public static unsafe implicit operator void*(ж<T> value)
    {
        // A native-backed pointer converts to the exact address it aliases (see the uintptr operator).
        if (value is not null && value.m_nativeAddr != 0)
            return (void*)value.m_nativeAddr;

        // A nil pointer converts to the null address (see the uintptr operator above); pinning a
        // nil box's absent storage would throw. The value-peeking IsNull is deliberate, for the same
        // reason as the uintptr operator: a reference-typed pointee has no reportable address.
        if (value is null || value.IsNull)
            return null;

        // A pointer to a Go fixed array resolves to the pinned address of the array data — see the
        // uintptr operator above for the full rationale.
        if (value.Value is IArray arr && arr is not ISlice)
            return value.pinnedArrayData(arr);

        // Hold the storage still before reading its address — see the uintptr operator above.
        value.EnsureStableAddress();

        fixed (T* ptr = &value.Value)
            return ptr;
    }

    /// <summary>
    /// Pins the managed storage this pointer addresses, for this box's lifetime, so the address taken
    /// from it stays valid after the statement that produced it has returned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE DEFECT THIS EXISTS TO CLOSE. Both address operators above end in a <c>fixed</c>, which pins
    /// for its own statement only, and both then RETURN the address as an integer — so every address
    /// go2cs handed to native code was, strictly, a former address. It survived by luck: the window
    /// between capture and the trampoline's <c>calli</c> is short and allocation-free. A BLOCKING
    /// syscall reopens that window for as long as the kernel takes. `os`'s <c>TestPipeEOF</c> parks in
    /// <c>ReadFile</c> on a pipe for 10 ms per read while the rest of a parallel suite allocates; a
    /// gen0 collection in that window moves both the caller's <c>*uint32</c> byte-count box and the
    /// read buffer, and the kernel then writes to neither. The count stays 0, <c>syscall.Read</c>
    /// returns <c>(0, nil)</c>, <c>FD.eofError</c> turns that into <c>io.EOF</c> — a premature EOF whose
    /// probability rises monotonically with parallelism, exactly as measured — and the buffer write
    /// lands in freed heap, which is the moving-site <c>ExecutionEngineException</c> recorded beside it.
    /// </para>
    /// <para>
    /// WHAT IT PINS. The ROOT managed storage behind this pointer, resolved per box kind: a standard
    /// heap box pins its own value slot; an array/slice-element reference pins the canonical backing
    /// array (so <c>&amp;buf[0]</c> holds <c>buf</c> itself still, not a per-access header box); a
    /// struct-field reference recurses to the allocation that contains the field. Pinning the root is
    /// what makes an INTERIOR address stable — the field or element cannot move without its container
    /// moving.
    /// </para>
    /// <para>
    /// WHEN IT DOES NOTHING. <see cref="GCHandle"/> pins only objects free of references, so storage
    /// whose type contains any is left exactly as it was — a transient address, the pre-change
    /// behavior. That is not a gap being tolerated: a converted Go value carrying managed references
    /// has no native layout either, so its address is not something a syscall can meaningfully be
    /// given. The change is additive — it makes stable the addresses that can be stable, and leaves
    /// the rest no worse.
    /// </para>
    /// <para>
    /// The pin is released when this box is collected (<see cref="PinnedBuffer"/>'s finalizer frees the
    /// handle), which bounds retention by the pointer's own lifetime; a Go pointer that stays reachable
    /// is one whose address Go also guarantees. Idempotent and unlocked, on the same terms as
    /// <c>pinnedArrayData</c>: a concurrent first touch can at worst allocate one extra handle that the
    /// finalizer frees.
    /// </para>
    /// </remarks>
    private void EnsureStableAddress()
    {
        if (m_pin is not null)
            return;

        if (PinnableStorage is { } storage)
            m_pin = PinnedBuffer.PinOnly(storage);
    }

    /// <inheritdoc/>
    public object? PinnableStorage
    {
        get
        {
            // An element reference's storage is the canonical backing array — the same resolution
            // equality and the order token use, so two pointers to one element pin one object.
            if (m_arrayIndexRef is not null)
                return CanonicalElement(m_arrayIndexRef.Value.Item1, m_arrayIndexRef.Value.Item2).storage;

            // A field reference's storage is its container's, recursively: `Ꮡo.of(Ꮡin).of(Ꮡv)` hangs
            // off a per-access intermediate box whose own storage is the outer allocation's.
            if (m_structFieldRef is not null)
            {
                return m_structFieldRef.Value.Item1 is INilPointer parent
                    ? parent.PinnableStorage
                    : m_structFieldRef.Value.Item1 as Array;
            }

            // A standard heap box: the pinnable value slot, when T admits one (see m_slot).
            return m_slot;
        }
    }

    // Returns a stable native pointer to the first element of this box's Go fixed-array data, pinning the
    // backing store for the box's lifetime (freed when the box is collected). Used when a fixed array is
    // handed to a native syscall as a read/write buffer via `unsafe.Pointer(&arr)`: the address must
    // reference the array DATA — not the `array<T>` struct wrapper — and the data must not move while
    // native code fills it in and while the value is read back afterward. Valid only for Go fixed arrays
    // (`array<T>`, an IArray that is not an ISlice); a slice's `&s` addresses its header, not its data.
    private unsafe void* pinnedArrayData(IArray arr)
    {
        // One stable pin per box: the syscall write and the subsequent managed reads must all see the
        // same address. (Not locked — a syscall buffer is a function-local array, never shared; a rare
        // concurrent first-touch would at worst leak one pinned handle to the finalizer.)
        m_pin ??= new PinnedBuffer(arr.Source, arr.Length);
        return m_pin.Pointer;
    }

    /// <summary>
    /// Derives a pointer that aliases this box's storage BY ADDRESS, with that storage pinned for the
    /// derived box's lifetime — or <c>null</c> when no such pin can be taken, leaving the caller its
    /// own fallback.
    /// </summary>
    /// <typeparam name="TDst">Pointee type of the derived pointer.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the safe form of the reinterpret fallback in
    /// <see cref="PointerExtensions.Reinterpret{T,TDst}"/>: where the derived pointer cannot ALIAS
    /// managed storage in the managed model, it has to name it by address, and an address of managed
    /// storage means nothing unless the storage is held still for as long as the address is held. The
    /// address-taking statement's own <c>fixed</c> pins for that statement and no longer, so a pointer
    /// that outlives the statement names memory the collector is free to move and then hand to another
    /// allocation — reads return whatever now occupies it, and WRITES land in whatever now owns it.
    /// The second is the dangerous one: it is not a wrong value, it is silent heap corruption whose
    /// crash surfaces later, somewhere else. `os`'s <c>createMountPoint</c> is the corpus's clearest
    /// instance of the shape — four <c>uint16</c> field stores through
    /// <c>(*windows.MountPointReparseBuffer)(unsafe.Pointer(&amp;b[0]))</c>. Guarded by
    /// <c>tests/Behavioral/ReinterpretPinLifetime</c>, which reproduces the loss deterministically —
    /// 5 of 5 runs before the pin, 8 of 8 matching Go after it.
    /// </para>
    /// <para>
    /// Only an ARRAY/SLICE-ELEMENT reference can be pinned, and that is not a shortcut — it is the only
    /// reference kind whose storage is an object the runtime can be asked to hold still. A native alias
    /// has nothing managed behind it; a nil box has no storage; and the storage of a standard heap box
    /// and of a struct-field reference alike is a field of a <see cref="ж{T}"/>, which holds delegates
    /// and a nullable tuple and so is never blittable — <c>GCHandle</c> refuses to pin it. Those kinds
    /// keep the pre-existing address route, exactly as before; the change is additive.
    /// </para>
    /// <para>
    /// The two spellings of the referent's address are CROSS-CHECKED before the pin is trusted. The pin
    /// is taken on the backing store <see cref="CanonicalElement"/> names, and it proves something only
    /// if the referent really does live inside that object — so the address reached through this box's
    /// own value slot must be the same byte as the address of that backing's element. A view whose
    /// storage is a detached copy fails that test and gets no pin.
    /// </para>
    /// </remarks>
    internal unsafe ж<TDst>? TryPinnedReinterpret<TDst>()
    {
        if (m_nativeAddr != 0 || m_isNull || m_arrayIndexRef is null)
            return null;

        (IArray array, int index) = m_arrayIndexRef.Value;
        (object storage, nint absoluteIndex) = CanonicalElement(array, index);

        if (storage is not T[] backing || absoluteIndex < 0 || absoluteIndex >= backing.Length)
            return null;

        PinnedBuffer? pin = PinnedBuffer.PinOnly(backing);

        if (pin is null)
            return null;

        // The backing cannot move from here on, so these addresses are the addresses it KEEPS —
        // which is what makes taking them without a `fixed` legitimate, and what the derived box
        // needs. They must name the same byte, or the object pinned is not the one the referent
        // lives inside and the pin guarantees nothing about it.
        void* referent = Unsafe.AsPointer(ref ValueSlot);

        if (referent != Unsafe.AsPointer(ref backing[absoluteIndex]))
        {
            pin.Dispose();
            return null;
        }

        return new ж<TDst>((nuint)referent, pin);
    }
}
