// TypeExtensions.GoMethodSets.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

#pragma warning disable IL2075
#pragma warning disable IL2067
#pragma warning disable IL2055
#pragma warning disable IL2060
#pragma warning disable IL2080
#pragma warning disable IL2070
#pragma warning disable IL2026

namespace go.golib;

// ---------------------------------------------------------------------------------------------
// GO METHOD SETS — Go's duck typing, decided at run time.
//
// WHAT LIVES HERE
//   The answer to "does this VALUE satisfy this interface?" when nothing declared that it does:
//   Go's value-vs-pointer receiver rule, the structural (name AND signature) satisfaction probe
//   built on it, and the interface-side and struct-side name projections it needs.
//
// WHY IT EXISTS
//   Go interfaces are satisfied STRUCTURALLY — a type implements an interface by having the
//   methods, never by saying so. C# interfaces are nominal, so a converted type can only declare
//   the interfaces the converter could SEE. Anything it could not — an anonymous interface in a
//   type assertion (`x.(interface{ Temporary() bool })`), an interface declared in a package that
//   does not import the type's package — has to be decided here, at the moment of the assert.
//
//   Live consumers: `AdapterBinder` (the duck-typing shell binder), `GoReflect.GoImplements` (so
//   reflection and direct asserts can never disagree about a method set), and `builtin`'s per-
//   interface assert cache.
//
// THE RULE THIS FILE EXISTS TO GET RIGHT
//   In Go, the method set of `*T` contains T's value-receiver AND pointer-receiver methods; the
//   method set of `T` contains only the value-receiver ones. GetGoMethodSetCandidates is the ONE
//   place that rule is applied at run time, and both the probe and the shell binder resolve
//   through it deliberately — if they each applied it themselves they would be free to disagree,
//   and a value-sourced shell that picked up a pointer-receiver method would make an assertion Go
//   REJECTS succeed.
//
//   A pointer receiver reaches this file in TWO emitted shapes — the RecvGenerator's `ж<X>`
//   overload and the original `[GoRecv] this ref X` extension — and the byref strip that
//   normalizes receiver types erases the second one's pointer-ness. That is why the filter also
//   asks the `[GoRecv]` marker directly.
//
// COUNTEREXAMPLES — WHAT A CHEAPER TEST GETS WRONG
//   Both are the reason StructurallyImplements is shaped the way it is, and neither is
//   hypothetical:
//     * Matching by NAME alone conflates methods whose signatures differ. `Unwrap() error` and
//       `Unwrap() []error` are different Go methods; a value implementing one satisfied the
//       interface for the other, and the generated adapter then failed to bind a delegate
//       (NotImplementedException at the call, far from the bad match).
//     * Asking GetExtensionMethodNames about a pointer value admits EVERY type's pointer-receiver
//       methods, because that lookup collapses a closed `ж<X>` to the open `ж<>` definition. That
//       collapse is correct for the registry's precedence-ranked single dispatch and wrong for a
//       membership test — same data, different question.
//   The one place a name-only match survives is an open-generic receiver method, whose signature
//   carries type parameters that cannot be compared against the interface's concrete one. Both
//   defects above were on closed types, so the narrow exemption keeps them fixed.
//
// THE BindingFlags ARE LOAD-BEARING, NOT DECORATION
//   GetInterfaceMethods passes `BindingFlags.Public | BindingFlags.Instance` explicitly, and the
//   base-interface loop MUST pass the same. Reflection's DEFAULT flags include STATICS, and
//   golib's hand-written core interfaces expose static duck-typing helpers (`error.As<T>`,
//   `fmt.Stringer.As<T>`) that are not part of any Go method set. Collect those and the probe
//   demands a static `As` from the value's Go method set — which no Go type can ever satisfy — so
//   every interface that EMBEDS such a base answers FALSE for everything. Measured when it
//   happened: an anonymous `interface{ error; Temporary() bool }` (net.Error's shape) asserted
//   against a `*tempErr` that plainly has both methods answered MISS.
//
// CACHES ARE CLEARED FROM THE SIBLING FILE
//   s_interfaceMethodNames and s_goMethodSetCandidates are emptied by `ClearTypeCaches` in
//   TypeExtensions.ExtensionMethodRegistry.cs on every assembly load, because their contents are
//   derived from the extension-method scan and a late-loading package assembly changes the answer.
//   s_structFieldNames is NOT cleared, and correctly so — a type's own fields are fixed when the
//   type is loaded. A new cache here belongs on that clear list only if the scan feeds it.
// ---------------------------------------------------------------------------------------------
public static partial class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, ImmutableHashSet<string>> s_interfaceMethodNames = [];
    private static readonly ConcurrentDictionary<(Type element, bool isPointer), List<MethodInfo>> s_goMethodSetCandidates = [];
    private static readonly ConcurrentDictionary<Type, ImmutableHashSet<string>> s_structFieldNames = [];

    /// <summary>
    /// Determines, by Go DUCK-TYPING (structural) rules, whether the method set of
    /// <paramref name="valueType"/> satisfies every method of <paramref name="interfaceType"/> —
    /// matching each interface method by NAME <em>and</em> full SIGNATURE (parameter and return types).
    /// </summary>
    /// <param name="valueType">
    /// Concrete runtime type of the value being asserted. A pointer value is the box <c>ж&lt;X&gt;</c>.
    /// </param>
    /// <param name="interfaceType">Anonymous or named interface the value is asserted against.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="valueType"/> structurally implements <paramref name="interfaceType"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is the runtime gate for duck-typed type assertions (<c>x.(interface{ … })</c>). It replaces a
    /// prior NAME-ONLY test that had two defects:
    /// <list type="number">
    /// <item>It conflated same-named methods whose signatures differ — <c>Unwrap() error</c> versus
    /// <c>Unwrap() []error</c> — so a value implementing one matched the interface for the other and the
    /// generated duck-typed adapter then failed to bind a delegate (a <see cref="NotImplementedException"/>).</item>
    /// <item>For a pointer value <c>ж&lt;X&gt;</c> it admitted the pointer-receiver methods of <em>every</em>
    /// type, because <see cref="GetExtensionMethodNames"/> collapses a closed <c>ж&lt;X&gt;</c> to the open
    /// <c>ж&lt;&gt;</c> generic definition (correct for MinBy-precedence single dispatch, wrong for a
    /// name-set membership test).</item>
    /// </list>
    /// Both are corrected here: candidates are restricted to the extension methods whose receiver actually
    /// belongs to <paramref name="valueType"/>'s Go method set (a pointer box <c>ж&lt;X&gt;</c> exposes the
    /// value- and pointer-receiver methods of <c>X</c>; a plain value <c>X</c> exposes only its
    /// value-receiver methods), and each candidate is compared by full signature. Open-generic receiver
    /// methods keep the prior name-only match, since their signatures carry type parameters that cannot be
    /// compared structurally against the interface's concrete signature (the defects being fixed are all on
    /// closed types).
    /// </remarks>
    public static bool StructurallyImplements(this Type valueType, Type interfaceType)
    {
        MethodInfo[] interfaceMethods = [.. interfaceType.GetInterfaceMethods()];

        // All types implement an empty interface.
        if (interfaceMethods.Length == 0)
            return true;

        // Resolve the value's Go method-set receiver element: a pointer box ж<X> exposes BOTH the value-
        // and pointer-receiver methods of X; a plain value X exposes only its value-receiver methods.
        ResolveReceiverElement(valueType, out Type valueElement, out bool valueIsPointer);

        // Collect the extension methods whose receiver belongs to this value's method set.
        List<MethodInfo> candidates = GetGoMethodSetCandidates(valueElement, valueIsPointer);

        // Every interface method must be satisfied by a candidate with the same name AND signature.
        foreach (MethodInfo interfaceMethod in interfaceMethods)
        {
            bool satisfied = false;

            foreach (MethodInfo candidate in candidates)
            {
                if (candidate.Name != interfaceMethod.Name)
                    continue;

                // An open-generic receiver method (a method generic over its receiver's type parameter)
                // carries type parameters in its signature that cannot be compared structurally against the
                // interface's concrete signature — keep the prior name-only match for those.
                if (candidate.IsGenericMethodDefinition || candidate.GetParameters()[0].ParameterType.ContainsGenericParameters)
                {
                    satisfied = true;
                    break;
                }

                if (SignaturesMatch(interfaceMethod, candidate))
                {
                    satisfied = true;
                    break;
                }
            }

            if (!satisfied)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Collects the extension methods whose receiver belongs to the Go METHOD SET of element type
    /// <paramref name="valueElement"/>, viewed as a pointer (<c>*X</c>) or as a value (<c>X</c>).
    /// </summary>
    /// <param name="valueElement">Go element type, i.e. the pointee of a receiver box or the value's own type.</param>
    /// <param name="valueIsPointer"><c>true</c> for the <c>*X</c> method set (value AND pointer receivers); <c>false</c> for <c>X</c>'s (value receivers only).</param>
    /// <returns>Candidate receiver methods, in no particular order.</returns>
    /// <remarks>
    /// This is the ONE place Go's value-vs-pointer receiver rule is applied at run time. Both the
    /// structural probe (<see cref="StructurallyImplements"/>) and the duck-typing shell binder
    /// (<see cref="AdapterBinder"/>) resolve through it, so a shell can never bind a method the probe
    /// would not have counted — the two would otherwise be free to disagree about a method set, and a
    /// value-sourced shell that picked up a pointer-receiver method would make an assertion Go REJECTS
    /// succeed. Deliberately keyed on the element + pointer-ness rather than on the concrete value type
    /// so no caller has to construct a <c>ж&lt;X&gt;</c> <see cref="Type"/> just to ask the question.
    /// </remarks>
    internal static List<MethodInfo> GetGoMethodSetCandidates(Type valueElement, bool valueIsPointer)
    {
        return s_goMethodSetCandidates.GetOrAdd((valueElement, valueIsPointer), static key =>
        {
            (Type valueElement, bool valueIsPointer) = key;
            (MethodInfo method, Type receiver)[] allExtensionMethods = GetExtensionMethods();
            List<MethodInfo> candidates = [];

            foreach ((MethodInfo method, Type receiver) in allExtensionMethods)
            {
                Type receiverType = receiver.IsByRef ? receiver.GetElementType()! : receiver;
                ResolveReceiverElement(receiverType, out Type receiverElement, out bool receiverIsPointer);

                // Pointer-receiver methods are NOT part of a plain value's method set (Go semantics).
                // A pointer receiver appears in TWO emitted shapes: the RecvGenerator's ж<X> overload
                // (receiverIsPointer above) and the original [GoRecv] `this ref X` extension — the
                // byref strip on the line above erases the latter's pointer-ness, so ask the marker
                // attribute directly (a `this ref` receiver without [GoRecv] does not exist in
                // emitted code; value receivers are always by-value `this X`).
                if (!valueIsPointer && (receiverIsPointer || method.GetCustomAttribute<GoRecvAttribute>() is not null))
                    continue;

                if (ReceiverElementMatches(receiverElement, valueElement))
                    candidates.Add(method);
            }

            return candidates;
        });
    }

    // Splits a receiver type into its concrete element type and whether it is a pointer box ж<X>.
    internal static void ResolveReceiverElement(Type type, out Type element, out bool isPointer)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ж<>))
        {
            element = type.GetGenericArguments()[0];
            isPointer = true;
        }
        else
        {
            element = type;
            isPointer = false;
        }
    }

    // Determines whether an extension method's receiver element belongs to the asserted value's method set.
    private static bool ReceiverElementMatches(Type receiverElement, Type valueElement)
    {
        if (receiverElement == valueElement)
            return true;

        // An open-generic receiver (a method on a Go generic type, receiver e.g. List<T>) matches a
        // constructed value of the same generic definition (List<int>).
        if (receiverElement.ContainsGenericParameters && receiverElement.IsGenericType &&
            valueElement.IsGenericType &&
            receiverElement.GetGenericTypeDefinition() == valueElement.GetGenericTypeDefinition())
            return true;

        // Safety net for promotion/embedding and base relationships — mirrors the assignability rule the
        // legacy value-type extension lookup used.
        return receiverElement.IsAssignableFrom(valueElement);
    }

    // Compares a duck-typed interface method against a candidate extension method by full signature.
    // The candidate's first parameter is the extension receiver; the interface method has none.
    private static bool SignaturesMatch(MethodInfo interfaceMethod, MethodInfo candidate)
    {
        if (!TypesEquivalent(interfaceMethod.ReturnType, candidate.ReturnType))
            return false;

        ParameterInfo[] interfaceParameters = interfaceMethod.GetParameters();
        ParameterInfo[] candidateParameters = candidate.GetParameters();

        if (interfaceParameters.Length != candidateParameters.Length - 1)
            return false;

        for (int i = 0; i < interfaceParameters.Length; i++)
        {
            if (!TypesEquivalent(interfaceParameters[i].ParameterType, candidateParameters[i + 1].ParameterType))
                return false;
        }

        return true;
    }

    // Type identity for signature comparison, disregarding by-ref decoration (ref/in/out parameters).
    private static bool TypesEquivalent(Type left, Type right)
    {
        if (left.IsByRef)
            left = left.GetElementType()!;

        if (right.IsByRef)
            right = right.GetElementType()!;

        return left == right;
    }

    /// <summary>
    /// Returns all method names defined in an interface type, including those inherited from other interfaces.
    /// </summary>
    /// <param name="interfaceType">The interface type to examine. Must be an interface.</param>
    /// <returns>A collection of method names defined in the interface and its base interfaces.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided type is not an interface.</exception>
    public static ImmutableHashSet<string> GetInterfaceMethodNames(this Type interfaceType)
    {
        return s_interfaceMethodNames.GetOrAdd(interfaceType, _ => [..interfaceType.GetInterfaceMethods().Select(info => info.Name)]);
    }

    /// <summary>
    /// Returns detailed information about methods defined in an interface type, including those inherited from other interfaces.
    /// </summary>
    /// <param name="interfaceType">The interface type to examine. Must be an interface.</param>
    /// <returns>A collection of MethodInfo objects for methods defined in the interface and its base interfaces.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided type is not an interface.</exception>
    public static IEnumerable<MethodInfo> GetInterfaceMethods(this Type interfaceType)
    {
        // Verify the type is an interface
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"The type '{interfaceType.FullName}' is not an interface.", nameof(interfaceType));

        // Get all methods directly defined on this interface
        MethodInfo[] methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Get all base interfaces
        Type[] baseInterfaces = interfaceType.GetInterfaces();

        // If there are no base interfaces, return just the direct methods
        if (baseInterfaces.Length == 0)
            return methods;

        // Otherwise, combine the direct methods with inherited methods
        HashSet<MethodInfo> allMethods = [..methods];

        // Add methods from all base interfaces. The explicit BindingFlags MUST match the direct-member
        // call above, and the reason is that reflection's DEFAULT flags include STATICS: golib's
        // hand-written core interfaces expose static duck-typing conversion helpers (`error.As<T>`,
        // `fmt.Stringer.As<T>`), which are not part of any Go method set. Collect those and
        // StructurallyImplements demands a static `As` from the dynamic value's Go METHOD SET — which
        // no Go type can ever satisfy — so every interface that EMBEDS such a base probes FALSE.
        // Measured when that happened: an anonymous `interface{ error; Temporary() bool }` (net.Error's
        // shape) asserted against a *tempErr that plainly has both methods answered MISS.
        foreach (Type baseInterface in baseInterfaces)
        {
            MethodInfo[] baseMethods = baseInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo method in baseMethods)
                allMethods.Add(method);
        }

        return allMethods;
    }

    /// <summary>
    /// Gets the names of all fields in a struct type.
    /// </summary>
    /// <param name="valueType">Struct type to search.</param>
    /// <returns>Names of all fields in the struct type.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided type is not a value type.</exception>
    public static ImmutableHashSet<string> GetStructFieldNames(this Type valueType)
    {
        return s_structFieldNames.GetOrAdd(valueType, _ =>
        {
            if (!valueType.IsValueType)
                throw new ArgumentException($"Type '{valueType.FullName}' is not a value type.", nameof(valueType));

            FieldInfo[] fields = valueType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            return [..fields.Select(field => field.Name)];
        });
    }
}
