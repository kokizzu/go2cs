using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using go;
using alias = go.crypto.@internal.alias_package;
using valias = go.vendor.golang.org.x.crypto.@internal.alias_package;
using aes = go.crypto.aes_package;
using cipher = go.crypto.cipher_package;

namespace GolibTests;

// The crypto/internal/alias address-ordering RACE — its mechanism as a live test, the structural
// contract that replaces it, and two stress guards over the REAL converted code.
//
// Go writes AnyOverlap (and slices.overlaps) by ORDERING element addresses, and the converter emitted
// that literally as four `(uintptr)Ꮡ(…)` takes. Each take pins its backing through a FINALIZABLE holder
// on a box that is garbage the instant the take returns, so the pin is released by the finalizer, not by
// the next take; a collection landing between two takes relocates an operand whose earlier pin has
// already been finalized, and the ordering then compares two heap layouts. Measured 2026-09-03 on a
// 4-core host, Release, tiering off, 16 threads + 2 allocation-churn threads (the probe is the C2 lane's
// `c2-overlap-probe`; the numbers are in the commit that landed this file):
//
//     the mirrored four-take predicate     RED at 17 s / 913,047 calls — FIVE threads torn on ONE collection,
//                                          every quadruple four real heap addresses, exactly one array's pair
//                                          inconsistent (x relocated between take 1 and take 4 by 3.6–43.9 MB,
//                                          or y between take 2 and take 3), every re-take 15/15 and FALSE
//     the converted alias.AnyOverlap       RED at 9 s / 280,239 calls (TRUE for two distinct fresh arrays)
//     the converted GCM Open, TLS shape    PANIC `crypto/aes: invalid buffer overlap` at 27 s / 1,405 records,
//                                          through crypto/aes Encrypt ← counterCrypt ← Open — the chain that
//                                          killed the banked net/http row on two host classes
//
// ⚠ THE CONFIGURATION IS PART OF THE MEASUREMENT. The SAME probe read six million clean calls in 120 s at
// Debug and went red in nine seconds at Release with tiering off: a non-optimizing frame roots its
// temporaries for the method's life, so all four pins hold until the predicate returns and the takes are
// atomic by accident. A pin-lifetime or GC-liveness probe runs at Release with tiering off, or it is not
// a measurement — and which way the configuration cuts is decided per case by running it (the
// internal/poll finalizer hypothesis was measured FALSE the same way, and both readings stand). The two
// stress guards below therefore report INCONCLUSIVE, never green, when the assembly under test carries
// a JIT-optimizer-disabled DebuggableAttribute; run `dotnet test -c Release` for the live guard.
//
// The remedy is structural: golib slice<T>.Overlaps answers by canonical backing identity + absolute
// index-range intersection (native-address and zero-size arms), and AnyOverlap / slices.overlaps are
// displaced onto it through manualConversionFuncs (crypto/internal/alias/alias_impl.cs,
// slices/slices_impl.cs). AliasOverlapTests.cs keeps the seven contract assertions; this file adds the
// mechanism, the structural contract, and the stress guards that are RED on the address form.
[TestClass]
public class AliasOverlapRaceTests
{
    private static int StressSeconds =>
        int.TryParse(Environment.GetEnvironmentVariable("GO2CS_OVERLAP_STRESS_SECONDS"), out int s) && s > 0 ? s : 20;

    private static int StressThreads => Math.Max(8, Environment.ProcessorCount * 4);

    private static bool JitOptimizerDisabled(Assembly assembly) =>
        assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;

    // The takes live in their own NON-INLINED frames so no slot of the calling frame can keep a box
    // alive — the first cut of this probe took the address inline and read NOT MOVED at Debug for that
    // reason alone.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint Take(slice<byte> x, int i) => (nuint)(uintptr)Ꮡ(x, i);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe nuint Raw(byte[] array) { fixed (byte* p = array) return (nuint)p; }

    [TestMethod]
    public void TakenElementAddressIsNotStableOnceItsPinIsFinalized()
    {
        // THE MECHANISM, kept live: an address taken through the uintptr operator is the array's address
        // only while the take's own pin holds — and that pin is released by the holder's FINALIZER. A
        // never-pinned control array shows the construction relocates at all; the taken array stays put
        // through the first compacting collection (pinned by an unfinalized holder) and moves once the
        // pin is gone. Measured 3/3 fresh processes at Release; the same shape at Debug with non-inlined
        // takes.
        byte[][] below = new byte[40000][];
        for (int i = 0; i < below.Length; i++) below[i] = new byte[16];
        byte[] control = new byte[16];
        byte[] backing = new byte[16];
        byte[][] above = new byte[40000][];
        for (int i = 0; i < above.Length; i++) above[i] = new byte[16];
        slice<byte> x = backing.slice();

        nuint control0 = Raw(control);
        nuint take1 = Take(x, 0);
        below = null!; above = null!;                   // the gaps a compacting collection closes

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        nuint control1 = Raw(control);
        nuint takeWhilePinned = Take(x, 0);
        GC.WaitForPendingFinalizers();                  // take 1's holder finalizes: its pin is freed
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();                  // the second take's holder finalizes
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        nuint control2 = Raw(control);
        nuint takeAfterPinsFreed = Take(x, 0);

        GC.KeepAlive(backing);
        GC.KeepAlive(control);

        if (control2 == control0 && control1 == control0)
        {
            // Not a silent pass: a runtime that did not relocate even the unpinned control cannot stage
            // the demonstration (no gap closed), and the assertion below would be vacuous.
            Assert.Inconclusive(
                $"the never-pinned control array did not relocate (0x{control0:X}) — this runtime closed no " +
                "gap below it, so the pin-lifetime demonstration cannot be staged here");
            return;
        }

        Assert.AreEqual(take1, takeWhilePinned,
            "through the first compacting collection the array is still pinned by take 1's unfinalized " +
            "holder, so a second take reads the same address");
        Assert.AreNotEqual(take1, takeAfterPinsFreed,
            "once every holder over the array has been finalized, a compacting collection relocates it: the " +
            "address from take 1 is not the array's address any more — two takes are only comparable while " +
            "the first one's pin holds, which is exactly what the four-take predicate assumed and never had");
    }

    [TestMethod]
    public void SliceOverlapsAnswersByStorageAndIndexRange()
    {
        // The structural contract AnyOverlap and slices.overlaps are displaced onto: no address is read,
        // so nothing can tear. Every arm the address form used to answer by ordering.
        byte[] a = new byte[32];
        byte[] b = new byte[32];

        Assert.IsFalse(a.slice().Overlaps(b.slice()), "two distinct backing arrays share no memory");
        Assert.IsFalse(a.slice(0, 16).Overlaps(a.slice(16, 32)), "adjacent-but-disjoint windows of one array touch but share no element");
        Assert.IsTrue(a.slice(0, 20).Overlaps(a.slice(10, 32)), "windows [0,20) and [10,32) of one array share elements 10..19");
        Assert.IsTrue(a.slice(10, 32).Overlaps(a.slice(0, 20)), "Overlaps is symmetric");
        Assert.IsTrue(a.slice(4, 20).Overlaps(a.slice(4, 20)), "a window overlaps itself");
        Assert.IsTrue(a.slice().Overlaps(a.slice(0, 8)), "the whole window overlaps its own prefix");
        Assert.IsTrue(a.slice(8, 9).Overlaps(a.slice(0, 32)), "a one-element window inside the whole overlaps it");
        Assert.IsFalse(a.slice(4, 4).Overlaps(a.slice()), "a zero-length window names no memory");
        Assert.IsFalse(a.slice().Overlaps(a.slice(4, 4)), "…in either argument position");
        Assert.IsFalse(default(slice<byte>).Overlaps(a.slice()), "the nil slice names no memory");
        Assert.IsFalse(a.slice().Overlaps(default(slice<byte>)), "…in either argument position");

        // Every zero-size slice shares ONE static backing in golib, so an index-range answer alone would
        // report any two of them as overlapping; Go's `elemSize == 0` early-out is load-bearing here.
        slice<EmptyStruct> z1 = new slice<EmptyStruct>(8);
        slice<EmptyStruct> z2 = new slice<EmptyStruct>(8);
        Assert.IsFalse(z1.Overlaps(z2), "zero-size elements never overlap (Go: elemSize == 0)");
        Assert.IsFalse(z1.Overlaps(z1), "…not even a zero-size window with itself");

        GC.KeepAlive(a);
        GC.KeepAlive(b);
    }

    [TestMethod]
    public unsafe void SliceOverlapsAnswersNativeWindowsByAddressRange()
    {
        // The native arm: a slice over unmanaged memory has no backing array, so overlap is the exact
        // address-range intersection — and a managed window never overlaps a native one (two spaces).
        // OverNativeMemory is golib's single creation door for native-backed slices (internal, visible to
        // this project).
        nint block = System.Runtime.InteropServices.Marshal.AllocHGlobal(128);
        nint other = System.Runtime.InteropServices.Marshal.AllocHGlobal(64);

        try
        {
            slice<byte> whole = go.slice<byte>.OverNativeMemory((nuint)block, 64);
            slice<byte> shifted = go.slice<byte>.OverNativeMemory((nuint)(block + 16), 64);   // a different base over the same bytes
            slice<byte> elsewhere = go.slice<byte>.OverNativeMemory((nuint)other, 64);
            byte[] managed = new byte[64];

            Assert.IsFalse(whole[..16].Overlaps(whole[16..32]), "adjacent native windows share no byte");
            Assert.IsTrue(whole[..20].Overlaps(whole[10..32]), "native windows [0,20) and [10,32) share bytes 10..19");
            Assert.IsTrue(whole.Overlaps(shifted), "two bases over the same unmanaged bytes overlap by address");
            Assert.IsTrue(shifted.Overlaps(whole), "…symmetrically");
            Assert.IsFalse(whole[..16].Overlaps(shifted[16..]), "block[0,16) and block[32,80) do not overlap even across bases");
            Assert.IsFalse(whole.Overlaps(elsewhere), "two unrelated unmanaged blocks share no byte");
            Assert.IsFalse(whole.Overlaps(managed.slice()), "a native window and a managed one live in different spaces");
            Assert.IsFalse(managed.slice().Overlaps(whole), "…in either argument position");
            Assert.IsFalse(whole[..0].Overlaps(whole), "a zero-length native window names no memory");

            GC.KeepAlive(managed);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(block);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(other);
        }
    }

    [TestMethod]
    public void ConvertedAnyOverlapNeverReportsDistinctArraysUnderStress()
    {
        // The REAL converted predicate, under the stress that turned its address form red in nine
        // seconds on a 4-core host: oversubscribed threads (preemption INSIDE the four-take window is the
        // ingredient), two allocation-churn threads, a fresh pair of arrays per "record" and 1,024 calls per
        // pair (one 16 KB record of block encryptions). RED on the address-ordering form, GREEN on the
        // structural one; INCONCLUSIVE at Debug, where a non-optimizing frame masks the race.
        if (JitOptimizerDisabled(typeof(alias).Assembly))
        {
            Assert.Inconclusive("crypto/internal/alias is a JIT-optimizer-disabled (Debug) build: the four-take " +
                "tear is masked by frame liveness there, so this guard measures nothing — run GolibTests -c Release");
            return;
        }

        int failures = RunStress(() =>
        {
            byte[] mask = new byte[16];
            byte[] counter = new byte[16];
            slice<byte> x = mask.slice(), y = counter.slice();

            for (int k = 0; k < 1024; k++)
            {
                if (alias.InexactOverlap(x, y) || alias.AnyOverlap(x, y))
                    return false;
            }

            GC.KeepAlive(mask);
            GC.KeepAlive(counter);
            return true;
        });

        Assert.AreEqual(0, failures,
            "alias.AnyOverlap/InexactOverlap reported two DISTINCT fresh arrays as overlapping — the " +
            "address-ordering tear is back (or a new one): overlap must be answered by storage identity and " +
            "index range, never by comparing addresses taken one pin at a time");
    }

    [TestMethod]
    public void ConvertedGcmOpenNeverPanicsWithOverlapUnderStress()
    {
        // The shape that killed net/http: cipher.NewGCM(aes.NewCipher(key)).Open(payload[:0], nonce,
        // payload, ad) over 16 KB records — crypto/tls conn.decrypt's exact call — with a fresh cipher +
        // GCM every 64 records (a new connection). counterCrypt calls Encrypt(mask[:], counter[:]) once per
        // block, and crypto/aes guards that with alias.InexactOverlap. PANIC on the address form at 27 s
        // here; nothing on the structural one. INCONCLUSIVE at Debug for the reason in the header.
        if (JitOptimizerDisabled(typeof(alias).Assembly))
        {
            Assert.Inconclusive("crypto/internal/alias is a JIT-optimizer-disabled (Debug) build: the four-take " +
                "tear is masked by frame liveness there, so this guard measures nothing — run GolibTests -c Release");
            return;
        }

        byte[] key = new byte[16];
        for (int i = 0; i < key.Length; i++) key[i] = (byte)(i * 7 + 3);
        byte[] nonce = new byte[12];
        byte[] additionalData = new byte[13];
        byte[] plaintext = new byte[16384];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        string? firstPanic = null;

        int failures = RunStress(() =>
        {
            var (block, err) = aes.NewCipher(key.slice());
            Assert.IsNull(err, "aes.NewCipher");
            var (aead, err2) = cipher.NewGCM(block);
            Assert.IsNull(err2, "cipher.NewGCM");
            slice<byte> sealedRecord = aead.Seal(default, nonce.slice(), plaintext.slice(), additionalData.slice());

            for (int record = 0; record < 64; record++)
            {
                slice<byte> payload = sealedRecord.Source.slice();   // a fresh read buffer per record

                try
                {
                    var (plain, oerr) = aead.Open(payload[..0], nonce.slice(), payload, additionalData.slice());

                    if (oerr is not null || len(plain) != plaintext.Length)
                    {
                        Interlocked.CompareExchange(ref firstPanic, "Open returned " + (oerr is null ? "a wrong length" : oerr.Error()), null);
                        return false;
                    }
                }
                catch (PanicException ex)
                {
                    Interlocked.CompareExchange(ref firstPanic, ex.Message, null);
                    return false;
                }
            }

            return true;
        });

        Assert.AreEqual(0, failures,
            $"the converted GCM Open raised '{firstPanic}' on records whose mask and counter are two distinct " +
            "fresh arrays — crypto/aes's alias.InexactOverlap guard tore between its address takes");
    }

    [TestMethod]
    public void VendoredAnyOverlapAnswersByStorageAndIndexRange()
    {
        // The vendored purego twin — vendor/golang.org/x/crypto/internal/alias, the guard chacha20 and
        // chacha20poly1305 call through InexactOverlap on every XORKeyStream / Seal / Open — carries the
        // crypto/internal twin's contract: storage identity and index range decide, no address is read.
        byte[] a = new byte[32];
        byte[] b = new byte[32];
        Assert.IsFalse(valias.AnyOverlap(a.slice(), b.slice()), "two distinct backing arrays share no memory");
        Assert.IsFalse(valias.AnyOverlap(a.slice(0, 16), a.slice(16, 32)), "adjacent-but-disjoint windows of one array share no element");
        Assert.IsTrue(valias.AnyOverlap(a.slice(0, 20), a.slice(10, 32)), "windows [0,20) and [10,32) of one array share elements 10..19");
        Assert.IsTrue(valias.AnyOverlap(a.slice(10, 32), a.slice(0, 20)), "AnyOverlap is symmetric");
        Assert.IsTrue(valias.AnyOverlap(a.slice(), a.slice(0, 8)), "the whole window overlaps its own prefix");
        Assert.IsFalse(valias.AnyOverlap(a.slice(4, 4), a.slice()), "a zero-length window names no memory");
        Assert.IsFalse(valias.AnyOverlap(default(slice<byte>), a.slice()), "the nil slice names no memory");
        Assert.IsFalse(valias.InexactOverlap(a.slice(), a.slice()), "exact aliasing is not INEXACT overlap — chacha20's in-place XORKeyStream(dst, dst)");
        Assert.IsTrue(valias.InexactOverlap(a.slice(0, 20), a.slice(1, 21)), "a one-element shift is inexact overlap");
        Assert.IsFalse(valias.InexactOverlap(a.slice(), b.slice()), "distinct arrays: no inexact overlap");
    }

    [TestMethod]
    public void VendoredAnyOverlapDoesNotConfuseArraysWithCollidingIdentityHashes()
    {
        // The vendored purego twin orders `reflect.ValueOf(Ꮡ(x, 0)).Pointer()` values, and the reflect bridge
        // answers Pointer() for an element reference with ElemRefBox.PointerOrderToken — the backing's
        // RuntimeHelpers.GetHashCode shifted 32 bits plus the element index. That token is GC-stable, so the
        // four-take TEAR the crypto/internal twin measured cannot happen here (the 16-thread stress guard read
        // GREEN against this body for 30 s, measured 2026-09-03), but an identity hash is not an identity: two
        // DISTINCT live arrays can share one, and then their token ranges coincide and the order form reads
        // them as overlapping. chacha20poly1305's Seal/Open and chacha20's XORKeyStream guard with
        // InexactOverlap, so such a pair panics `invalid buffer overlap` on buffers that share no byte. The
        // search below allocates until two arrays collide (a 32-bit identity hash makes that a birthday
        // problem, not a rarity), keeping every candidate alive so no hash is recycled. RED on the token form,
        // GREEN on alias_purego_impl.cs over slice<T>.Overlaps, INCONCLUSIVE only if no collision surfaces
        // within the allocation budget.
        var keepAlive = new List<byte[]>(1 << 16);
        var byHash = new Dictionary<int, byte[]>(1 << 16);
        byte[]? first = null, second = null;
        int allocations = 0;

        while (first is null && allocations < (1 << 23))
        {
            byte[] candidate = new byte[64];
            keepAlive.Add(candidate);
            allocations++;
            int hash = RuntimeHelpers.GetHashCode(candidate);

            if (byHash.TryGetValue(hash, out byte[]? earlier))
            {
                first = earlier;
                second = candidate;
            }
            else
            {
                byHash[hash] = candidate;
            }
        }

        if (first is null || second is null)
        {
            Assert.Inconclusive($"no two of {allocations:N0} live arrays shared an identity hash within the budget; the collision arm is unmeasured here");
            return;
        }

        Assert.AreNotSame(first, second, "the search returned one array twice");
        Assert.AreEqual(RuntimeHelpers.GetHashCode(first), RuntimeHelpers.GetHashCode(second), "the pair does not actually collide");

        slice<byte> x = first.slice(), y = second.slice();

        Assert.IsFalse(valias.AnyOverlap(x, y),
            $"the vendored alias.AnyOverlap reported two DISTINCT arrays as overlapping because they share identity hash " +
            $"{RuntimeHelpers.GetHashCode(first):X} (found after {allocations:N0} allocations) — a hash-derived order token is not " +
            "storage identity; overlap must be answered by storage identity and index range");
        Assert.IsFalse(valias.InexactOverlap(x, y), "…and InexactOverlap, chacha20poly1305's Seal/Open guard, must agree");
        Assert.IsFalse(valias.AnyOverlap(y, x), "…in either argument position");

        GC.KeepAlive(keepAlive);
    }
    // Runs `body` on StressThreads oversubscribed workers plus two allocation-churn threads for
    // StressSeconds, stopping at the first false; returns the number of workers that reported false.
    private static int RunStress(Func<bool> body)
    {
        int seconds = StressSeconds;
        int workers = StressThreads;
        bool stop = false;
        int failures = 0;
        var threads = new Thread[workers + 2];

        for (int i = 0; i < workers; i++)
        {
            threads[i] = new Thread(() =>
            {
                while (!Volatile.Read(ref stop))
                {
                    if (!body())
                    {
                        Interlocked.Increment(ref failures);
                        Volatile.Write(ref stop, true);
                        return;
                    }
                }
            }) { IsBackground = true };
        }

        for (int i = workers; i < workers + 2; i++)
        {
            threads[i] = new Thread(() =>
            {
                long n = 0;
                while (!Volatile.Read(ref stop))
                {
                    byte[] garbage = new byte[256];
                    garbage[0] = (byte)n;
                    if ((++n & 0x3FFF) == 0)
                        Thread.Yield();
                }
            }) { IsBackground = true };
        }

        foreach (Thread t in threads)
            t.Start();

        var sw = Stopwatch.StartNew();

        while (!Volatile.Read(ref stop) && sw.Elapsed.TotalSeconds < seconds)
            Thread.Sleep(100);

        Volatile.Write(ref stop, true);

        foreach (Thread t in threads)
            t.Join(5000);

        return failures;
    }
}
