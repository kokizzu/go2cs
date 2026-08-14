// ж.PointerTokens.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace go;

// ---------------------------------------------------------------------------------------------
// POINTER TOKEN RECOVERY — making `unsafe.Pointer` round-trip for a MANAGED pointer.
// ---------------------------------------------------------------------------------------------
//
// A Go pointer to managed storage has no machine address to report, so every projection of one to
// a scalar — `uintptr(unsafe.Pointer(p))`, `reflect.Value.Pointer`, `reflect.Value.UnsafePointer` —
// answers with a stable ORDER TOKEN instead (see INilPointer.PointerOrderToken, whose own remarks
// say plainly that tokens "are order keys, never an identity substitute").
//
// That contract is sufficient for every consumer that only ORDERS or NIL-TESTS the result — fmt's
// `%p`, internal/fmtsort's map-key ordering — and it was written for exactly those. It is NOT
// sufficient for the other direction Go permits: converting the scalar back to a pointer and
// dereferencing it.
//
//     ps := (*bool)(v.FieldByName(name).Addr().UnsafePointer())   // go/types check_test.go:345
//     *ps = true
//
// Emitted, that is `(ж<bool>)(uintptr)(…)` followed by a `.Value` store — and the uintptr operator
// builds a NATIVE-address box over whatever number it was handed. Storing a bool at the numeric
// value of an order token writes to an arbitrary address: an access violation when the page is not
// mapped, and silent heap corruption when it is. It killed `go/types`' converted test host outright
// at the first test that reaches the idiom (TestCheck/blank.go), 542 verdicts behind it.
//
// The information needed to do better is never actually lost — `reflect.Value.Addr` surfaces the
// real aliasing ж box, and only the projection to a scalar discards it. So this table remembers the
// association the projection drops: the token that was handed out, and the box it named. The uintptr
// operator consults it FIRST, and a token that came from here recovers its own box and aliases the
// original storage exactly as Go's pointer would. Anything else keeps the pre-existing native-address
// route, unchanged.
//
// WHY THE TOKEN VALUE IS NOT CHANGED. The obvious alternative — mint handles from a reserved numeric
// range so a token is self-identifying — would also change what `%p` prints and what order pointer-
// keyed maps print in, because those read the very same token. Keeping the value byte-identical and
// carrying the association out of band costs one dictionary probe on a conversion that was already
// building an object, and leaves every existing observable untouched.
//
// COLLISION. A real machine address that numerically equals a live token would resolve to that
// token's box instead of the address. The window is remote in practice — the table only ever holds
// tokens that a reflect projection actually handed out, which is a handful — and the resolution is
// verified against the box's own current token before it is used, so a stale or reused entry can
// never answer. Where it did collide, the effect is a managed access in place of a wild one, i.e. it
// fails safe relative to the behavior this replaces.
//
// LIFETIME. Entries are weak: this table must never be the reason a box stays alive, or every
// pointer fmt ever printed would leak. Dead entries are swept opportunistically as the table grows.
//
// PUBLIC because the only party that mints these tokens is the hand-owned reflection bridge in the
// separate `reflect` assembly (reflect/value_impl.cs), the same way GoReflect is public for it. It is
// a runtime seam, not part of any Go surface.
public static class ManagedPointerTokens
{
    // token → the box that token named. WeakReference so a remembered pointer is still collectable.
    //
    // CONCURRENT, and read WITHOUT a lock, because Resolve sits on the `uintptr → ж<T>` conversion
    // operator — 875 emitted call sites across the corpus, 54 of them in the syscall wrappers. A
    // global lock there would serialize goroutines through a conversion that is otherwise free.
    private static readonly ConcurrentDictionary<nuint, WeakReference<object>> s_table = new();

    // The overwhelmingly common case is a program that never asks reflect for a pointer's scalar
    // form at all, and it must not pay even a hash: an empty table answers from this one load.
    // Volatile because the writer is a different thread than the reader in the general case.
    //
    // The fast path is exact for the sequence that matters. A round trip PROJECTS a pointer and then
    // CONVERTS the scalar back on ONE thread — `(*bool)(v.Addr().UnsafePointer())` is a single
    // expression — so the registration always happens-before the resolution that depends on it. A
    // reader on some OTHER thread racing the very first registration may still see zero and take the
    // native-address route, which is exactly what it would have done before this table existed: the
    // fast path can lose a race it was never in a position to win.
    private static volatile int s_count;

    // Guards Sweep alone — registration and resolution never take it. Sweeping is rare and its
    // cost is proportional to the table, so one sweeper at a time is the point, not a bottleneck.
    private static readonly object s_sweepLock = new();

    // Sweep dead entries when the table has grown by this much since the last sweep. The table is
    // expected to hold a handful of live entries; the threshold exists so a program that projects
    // many short-lived pointers cannot grow it without bound.
    private const int SweepThreshold = 256;

    private static int s_sweepAt = SweepThreshold;

    /// <summary>
    /// Remembers that <paramref name="token"/> was handed out as the scalar form of
    /// <paramref name="box"/>, so a conversion back to a pointer can recover it.
    /// </summary>
    /// <remarks>
    /// A zero token is the reserved nil form and is never registered. Re-registering a token simply
    /// refreshes it: two projections of one pointer produce one token, and a token whose box has
    /// been collected is free to be reused by whatever the runtime hands the same identity to next.
    /// </remarks>
    public static void Register(nuint token, object box)
    {
        if (token == 0 || box is null)
            return;

        // Already remembered — return without allocating or writing. This is the steady state, not
        // an edge case: `fmt` projects a pointer through this on every `%p` and on every nil-test of
        // a pointer, map, func or channel it prints, so printing one value in a loop would otherwise
        // allocate a WeakReference and take a bucket lock per iteration to store what is already
        // there.
        if (s_table.TryGetValue(token, out WeakReference<object>? existing) &&
            existing.TryGetTarget(out object? remembered) &&
            ReferenceEquals(remembered, box))
        {
            return;
        }

        s_table[token] = new WeakReference<object>(box);
        s_count = s_table.Count;

        if (s_count >= s_sweepAt)
            Sweep();
    }

    /// <summary>
    /// Recovers the box <paramref name="token"/> was handed out for, or <c>null</c> when the token
    /// did not come from a reflect pointer projection, or its box has since been collected.
    /// </summary>
    public static object? Resolve(nuint token)
    {
        // The fast path every non-reflect program takes: nothing was ever registered, so no token
        // can resolve and the conversion goes straight to its native-address route.
        if (token == 0 || s_count == 0)
            return null;

        if (!s_table.TryGetValue(token, out WeakReference<object>? weak))
            return null;

        if (!weak.TryGetTarget(out object? box))
        {
            if (s_table.TryRemove(token, out _))
                s_count = s_table.Count;

            return null;
        }

        // Verify the box still answers to this token before handing it back: an entry whose box
        // has been re-identified is stale, and a numeric collision with a real address must not
        // be allowed to resolve to something that never carried that token.
        return CurrentToken(box) == token ? box : null;
    }

    // The token the box would report today — the same projection reflect used to mint the entry.
    private static nuint CurrentToken(object box)
    {
        return box switch
        {
            INilPointer p => p.PointerOrderToken,
            IChannel c => c.PointerOrderToken,
            _ => (nuint)(uint)RuntimeHelpers.GetHashCode(box)
        };
    }

    // Drops entries whose box has been collected. One sweeper at a time; concurrent registrations
    // and resolutions continue against the table throughout.
    private static void Sweep()
    {
        if (!Monitor.TryEnter(s_sweepLock))
            return;

        try
        {
            // Re-check under the gate: a thread that queued behind a sweep has nothing left to do.
            if (s_table.Count < s_sweepAt)
                return;

            foreach ((nuint token, WeakReference<object> weak) in s_table)
            {
                if (!weak.TryGetTarget(out _))
                    s_table.TryRemove(token, out _);
            }

            // Re-arm above the surviving population so a table that is legitimately large does not
            // sweep on every registration.
            s_count = s_table.Count;
            s_sweepAt = s_count + SweepThreshold;
        }
        finally
        {
            Monitor.Exit(s_sweepLock);
        }
    }
}
