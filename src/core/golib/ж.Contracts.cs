// ж.Contracts.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using go.golib;

namespace go;

// ---------------------------------------------------------------------------------------------
// POINTER CONTRACTS — what every Go pointer representation must be able to answer.
//
// WHAT LIVES HERE
//   The four declarations that describe a Go pointer without being one: the two field-reference
//   delegates, the IL-emitting FieldRef<T> factory that builds them, INilPointer (the NON-generic
//   nilness and identity surface) and IPointer<T> (the generic pointer surface). ж.cs holds ж<T>,
//   which is one implementation of these — the standard one, not the only one.
//
// WHY THAT DISTINCTION IS REAL AND NOT BOOKKEEPING
//   A Go named pointer type (`type P *T`) is emitted as a GENERATED WRAPPER CLASS, not as a ж<T>,
//   and it implements these same interfaces. So every rule stated here has at least two
//   implementers, and a rule that is really about the box belongs on the box. That is the test for
//   adding to this file: does EVERY pointer representation owe this answer? If yes it belongs on
//   an interface, with a default implementation covering the ordinary case and ж<T> overriding
//   where it can do better. If it needs ж<T>'s private storage, it is not a contract.
//
//   Both interfaces already carry defaults on exactly those terms — `PointerOrderToken` and
//   `ReferentObject` default to per-instance identity, which is correct for a plain heap box, and
//   ж<T> overrides both for element and field referents. A generated wrapper keeps the defaults;
//   that is a RECORDED fidelity residual (no consumer orders wrapped pointers), not an oversight.
//
// WHY INilPointer IS NON-GENERIC
//   Because the machinery that needs it does not know T. Equality tails, the reflection bridge's
//   IsNil/Elem probes and the type-assert paths all hold a pointer as `object`. Without a
//   non-generic surface they would have to ask the nil question by REFLECTION — per value, on
//   paths that run per element — and reflection is exactly what this runtime exists to avoid on
//   hot paths.
//
// THE ONE DISTINCTION TO GET RIGHT — STRUCTURAL NIL vs VALUE-PEEKING NIL
//   This is the recurring defect in this corner of the runtime, so it is stated here as well as on
//   the members:
//     * `IsNilPointer` is STRUCTURAL — is this box THE nil pointer. Everything about pointer
//       IDENTITY keys off it: equality, the hash, the order token, the reflection bridge's
//       IsNil/Elem, and the dereference guards.
//     * `IsNull` additionally reports a real heap box whose HELD reference-typed value is null.
//       That is a non-nil pointer holding a nil value, and it is a perfectly ordinary Go thing —
//       a captured `ж<ж<T>>` local holding a nil `*T`, or `&s.next` where the field is nil. An
//       address is an address even when what is stored there is nil.
//   Use the value-peeking one for a dereference guard and the structural one for everything else.
//   Getting it backwards is not a compile error and not a crash; it silently makes `&c == &d` true
//   for distinct pointers, collapses `map[**int]V` to one entry, and gives a box a hash that
//   MUTATES when its pointee is assigned — so a key inserted while the pointee was nil can never
//   be found again.
//
// FieldRef<T> IS THE SIMPLE CASE, AND IT KNOWS IT
//   Its emitted IL is `((ж<T>)obj).m_val.field` — the box's own value field, hardcoded. That is
//   right for the direct case and cannot reach a nested parent, because a box that is ITSELF a
//   field or element reference keeps its storage elsewhere and leaves `m_val` an unused default.
//   The reflection bridge needs the nested case and therefore emits its own accessor through
//   `ValueSlot` (GoReflect.FieldAccess.cs). Two IL emitters, deliberately, for two different
//   reaches — do not fold one into the other without carrying the ValueSlot walk into it.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Delegate that returns a <c>ref</c> to a field value within a struct <see cref="ж{T}"/> reference.
/// </summary>
/// <typeparam name="TElem">Field type.</typeparam>
/// <param name="structPtr"><see cref="ж{T}"/> heap reference of struct.</param>
/// <returns>A <c>ref</c> to the field value within the struct <see cref="ж{T}"/> reference.</returns>
public delegate ref TElem FieldRefFunc<TElem>(object structPtr);

/// <summary>
/// Delegate that returns a <c>ref</c> to a field value within a struct <see cref="ж{T}"/> reference.
/// </summary>
/// <typeparam name="T">Struct type.</typeparam>
/// <typeparam name="TElem">Field type.</typeparam>
/// <param name="structRef">Reference to struct.</param>
/// <returns>A <c>ref</c> to the field value within the struct <see cref="ж{T}"/> reference.</returns>
public delegate ref TElem FieldRefFunc<T, TElem>(ref T structRef);

/// <summary>
/// Delegate that returns a <see cref="ж{T}"/> POINTER to a field whose storage lives in a DIFFERENT
/// allocation than the struct it is selected from.
/// </summary>
/// <typeparam name="T">Struct type.</typeparam>
/// <typeparam name="TElem">Field type.</typeparam>
/// <param name="structRef">Reference to struct.</param>
/// <returns>Pointer to the field, rooted at the allocation that actually contains it.</returns>
/// <remarks>
/// <para>
/// This is the contract for a field promoted through an EMBEDDED POINTER — Go's
/// <c>type File struct { *file }</c>, where <c>f.pfd</c> is by definition <c>f.file.pfd</c> and so
/// names storage inside the <c>file</c> allocation, not inside <c>File</c>. A plain
/// <see cref="FieldRefFunc{T,TElem}"/> can REACH that storage (its <c>ref</c> is correct, so reads
/// and writes land in the right place) but it cannot say WHERE the storage lives, and a pointer's
/// identity is exactly that: <see cref="ж{T}"/> encodes a field reference as (containing allocation,
/// field token), so an accessor that silently hops to another allocation makes <c>&amp;f.pfd</c> and
/// <c>&amp;f.file.pfd</c> — ONE address in Go — compare unequal. Everything keyed on pointer identity
/// then splits in two: a <c>map[*T]V</c> grows a second entry for the same address, and the
/// address-keyed runtime semaphores paired a release with a different bucket than the acquire, which
/// is what hung <c>internal/poll</c>'s <c>Close</c> on a file whose <c>Read</c> was still in flight.
/// </para>
/// <para>
/// go2cs-gen emits the promoted accessor in THIS form whenever the promotion crosses a pointer, so
/// the hop is taken BEFORE the field reference is built and the result is rooted where Go roots it.
/// Call sites are unchanged: <c>Ꮡf.of(File.Ꮡpfd)</c> binds to the matching <c>of</c> overload by the
/// accessor's return type.
/// </para>
/// </remarks>
public delegate ж<TElem> FieldPtrFunc<T, TElem>(ref T structRef);

/// <summary>
/// Helper class for creating a <see cref="FieldRefFunc{TElem}"/> delegate for a struct field.
/// </summary>
/// <typeparam name="T">Type of struct.</typeparam>
public static class FieldRef<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] T> where T : struct
{
    /// <summary>
    /// Creates a <see cref="FieldRefFunc{TElem}"/> delegate for a struct field.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <returns> A <see cref="FieldRefFunc{TElem}"/> delegate for a struct field. </returns>
    /// <typeparam name="TElem">Type of field.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Field <paramref name="fieldName"/> not found in type <typeparamref name="T"/>.
    /// </exception>
    public static FieldRefFunc<TElem> Create<TElem>(string fieldName)
    {
        // Get the FieldInfo for fieldName in struct T referenced by ж<T>
        FieldInfo structField = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                             ?? throw new InvalidOperationException($"Field '{fieldName}' not found in type {typeof(T).FullName}");

        // Create a dynamic method that matches the delegate signature
        DynamicMethod method = new(
            name: $"ref_{fieldName}",
            returnType: typeof(TElem).MakeByRefType(),
            parameterTypes: [typeof(object)],
            m: typeof(FieldRef<>).Module, // Use the module where this code is running
            skipVisibility: true);

        ILGenerator il = method.GetILGenerator();

        // Emit IL code to load the field address: ((ж<T>)obj).m_val.fieldName
        il.Emit(OpCodes.Ldarg_0);               // Load the object argument
        il.Emit(OpCodes.Castclass, s_ptrType);  // Cast to ж<T>
        il.Emit(OpCodes.Ldflda, s_ptrValField); // Load address of ж<T>.m_val struct, type &T
        il.Emit(OpCodes.Ldflda, structField);   // Load address of m_val struct field, type &TElem
        il.Emit(OpCodes.Ret);                   // Return

        // Create the delegate
        return (FieldRefFunc<TElem>)method.CreateDelegate(typeof(FieldRefFunc<TElem>));
    }

    // Type of ж<T>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
    private static readonly Type s_ptrType = typeof(ж<T>);

    // FieldInfo for m_val in ж<T>
    private static readonly FieldInfo s_ptrValField = s_ptrType.GetField("m_val", BindingFlags.Instance | BindingFlags.NonPublic)!;
}

/// <summary>
/// Non-generic pointer-nilness surface: implemented by <see cref="ж{T}"/> and the generated
/// named-pointer wrapper classes, so runtime machinery holding a pointer only as <see cref="object"/>
/// (equality tails, the reflection bridge's <c>IsNil</c>/<c>Elem</c> probes) can ask the STRUCTURAL
/// nil question without reflection. See <see cref="ж{T}.IsNilPointer"/> for the structural-vs-
/// value-peeking distinction.
/// </summary>
public interface INilPointer
{
    /// <summary>
    /// Gets a flag indicating whether this box IS the nil pointer (structural — the
    /// nil-constructed / canonical typed-nil form).
    /// </summary>
    bool IsNilPointer { get; }

    /// <summary>
    /// Gets a stable, order-consistent address token for the pointer — the value the reflection
    /// bridge reports from <c>reflect.Value.Pointer()</c>/<c>UnsafePointer()</c>. Equal Go
    /// pointers yield equal tokens, and pointers into the SAME backing storage order by element
    /// position (Go programs order same-object addresses arithmetically — internal/fmtsort's
    /// map-key ordering of <c>*T</c>/<c>unsafe.Pointer</c> keys). The default is per-instance
    /// identity; <see cref="ж{T}"/> supplies the canonical referent-based form (a generated
    /// named-pointer wrapper keeps the default — a recorded fidelity residual, no consumer
    /// orders wrapped pointers).
    /// </summary>
    nuint PointerOrderToken => (nuint)(uint)RuntimeHelpers.GetHashCode(this);

    /// <summary>
    /// Gets the managed object whose LIFETIME is the Go allocation this pointer references — the
    /// referent, not the pointer box. Go attaches a finalizer to the *object* a pointer points at
    /// (<c>runtime.SetFinalizer(&amp;buf[0], f)</c> finalizes <c>buf</c>'s allocation), so anything
    /// keyed on a Go pointer's lifetime must key on this, never on the <see cref="ж{T}"/> instance:
    /// a pointer box is routinely a per-expression TEMPORARY (<c>Ꮡ(target, index)</c> allocates a
    /// fresh box per call), whose lifetime has nothing to do with the storage it names. The default
    /// is the box itself — correct for a standard heap box, which IS its own allocation;
    /// <see cref="ж{T}"/> supplies the canonical form for element and field referents (a generated
    /// named-pointer wrapper keeps the default, the same recorded residual noted for
    /// <see cref="PointerOrderToken"/>).
    /// </summary>
    /// <remarks>
    /// It resolves to the ROOT allocation, not the immediate parent: a nested field reference
    /// (<c>Ꮡouter.of(Outer.Ꮡinner).of(Inner.Ꮡfield)</c>) hangs off a per-call intermediate box, so
    /// stopping at the immediate source would hand back a different object on every access. The
    /// root is what Go allocates and what Go's identity questions — "is this the same object?",
    /// "when does this object die?" — are asked about.
    /// </remarks>
    object ReferentObject => this;
}

/// <summary>
/// Defines an interface that represents a pointer <see cref="ж{T}"/> type.
/// </summary>
/// <typeparam name="T">Type for heap based reference.</typeparam>
public interface IPointer<T>
{
    /// <summary>
    /// Gets a reference to the value of type <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="PanicException">runtime error: invalid memory address or nil pointer dereference</exception>
    ref T Value { get; }

    /// <summary>
    /// Gets flag indicating if the pointer is null.
    /// </summary>
    bool IsNull { get; }

    /// <summary>
    /// Gets a pointer to the field of a struct.
    /// </summary>
    /// <typeparam name="TElem">Type of field.</typeparam>
    /// <param name="fieldRefFunc">Struct field reference delegate.</param>
    /// <returns>Pointer to field of struct.</returns>
    ж<TElem> of<TElem>(FieldRefFunc<TElem> fieldRefFunc);

    /// <summary>
    /// Gets a pointer to the field of a struct.
    /// </summary>
    /// <typeparam name="TElem">Type of field.</typeparam>
    /// <param name="fieldRefFunc">Struct field reference delegate.</param>
    /// <returns>Pointer to field of struct.</returns>
    ж<TElem> of<TElem>(FieldRefFunc<T, TElem> fieldRefFunc);

    /// <summary>
    /// Gets a pointer to a field of a struct whose storage lives in another allocation — a field
    /// promoted through an EMBEDDED POINTER (see <see cref="FieldPtrFunc{T,TElem}"/>).
    /// </summary>
    /// <typeparam name="TElem">Type of field.</typeparam>
    /// <param name="fieldPtrFunc">Struct field pointer delegate.</param>
    /// <returns>Pointer to field of struct, rooted at the allocation that contains it.</returns>
    /// <remarks>
    /// The accessor has already taken the hop, so this is a plain forward: whatever pointer it
    /// returns IS the answer, and its identity is the pointed-to allocation's, exactly as Go's.
    /// </remarks>
    ж<TElem> of<TElem>(FieldPtrFunc<T, TElem> fieldPtrFunc) => fieldPtrFunc(ref Value);

    /// <summary>
    /// Gets a pointer to element at the specified index for <see cref="array{T}"/> or <see cref="slice{T}"/> types.
    /// </summary>
    /// <typeparam name="TElem">Element type of array or slice.</typeparam>
    /// <param name="index">Index of element to get pointer for.</param>
    /// <returns>Pointer to element at specified index.</returns>
    ж<TElem> at<TElem>(nint index);

    /// <summary>
    /// Dereferences the heap allocated reference to the value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">Pointer value to dereference.</param>
    /// <returns>Dereferenced pointer value.</returns>
    static abstract T operator ~(IPointer<T> value);
}
