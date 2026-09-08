// §4.2 COST BENCH for DESIGN-token-value-tag-refusal.md -- the trampoline door, A vs B vs no door.
//
// THE PREDICTION IS ALREADY ON RECORD (mailbox 583d49cd64, record §D), written before this file:
//   "B will not bench worse, because its test is not two tests: (arg & 0x8000800000000000) ==
//    0x8000000000000000 is ONE AND and ONE COMPARE -- the same shape and instruction count as A's
//    (arg >> 48) == 0x8000 -- and its mint side costs one extra shift-and-or on a path measured at
//    4,532 events against 629,240,995 constructor calls."
//   FALSIFIER: a measured per-call regression for B over A at the trampoline, or a mint-side
//   regression large enough to show against 4,532 events.
//
// THREE TRAMPOLINE ARMS, because A-vs-B is COORD's criterion but §4.2 owes the absolute cost too:
//   none  the trampoline as it is today, no door at all
//   A     (arg >> 48) == 0x8000
//   B     (arg & 0x8000_8000_0000_0000) == 0x8000_0000_0000_0000
//
// POSITIVE CONTROL, and it is the arm that makes a null result mean anything: a deliberately
// expensive door (a dictionary probe) MUST bench measurably worse than all three. Without it,
// "no difference between A and B" is indistinguishable from "this harness cannot resolve a
// difference at this scale", which is the same vacuous-green shape as an instrument that never ran.
//
// The argument mix is realistic rather than uniform: real-looking addresses, small handles, lengths,
// -1 (INVALID_HANDLE_VALUE, the value that must NOT be refused), and a sprinkling of tagged tokens
// so the branch is not perfectly predicted and the refused count is provably nonzero.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

const int N = 10_000_000;
const int Reps = 7;
const ulong BitB  = 0x8000_0000_0000_0000UL;

ulong[] probeArgs = new ulong[N];
var rng = new Random(12345);
int tagged = 0;
for (int i = 0; i < N; i++)
{
    int kind = rng.Next(100);
    if (kind < 60)      probeArgs[i] = 0x0000_7FF0_0000_0000UL | (ulong)(uint)rng.Next() ; // canonical address
    else if (kind < 75) probeArgs[i] = (ulong)(uint)rng.Next(1, 4096);                     // handle
    else if (kind < 90) probeArgs[i] = (ulong)(uint)rng.Next(0, 1 << 20);                  // length / flags
    else if (kind < 95) probeArgs[i] = unchecked((ulong)(long)(-1));                       // INVALID_HANDLE_VALUE
    else { probeArgs[i] = BitB | ((ulong)(uint)rng.Next() << 16); tagged++; }              // a tagged token
}

var dict = new Dictionary<ulong, int>();
for (int i = 0; i < 4096; i++) dict[(ulong)i * 7919UL] = i;

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmNone(ulong[] a)
{ long acc = 0; for (int i = 0; i < a.Length; i++) acc += (long)(a[i] & 1); return acc; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmA(ulong[] a)
{ long r = 0; for (int i = 0; i < a.Length; i++) if ((a[i] >> 48) == 0x8000UL) r++; return r; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmA2(ulong[] a)
{ long r = 0; for (int i = 0; i < a.Length; i++) if ((a[i] >> 48) == 0x8000UL) r++; return r; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmB(ulong[] a)
{ long r = 0; for (int i = 0; i < a.Length; i++) if ((a[i] & 0x8000_8000_0000_0000UL) == 0x8000_0000_0000_0000UL) r++; return r; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmB2(ulong[] a)
{ long r = 0; for (int i = 0; i < a.Length; i++) { ulong x = a[i]; if ((long)x < 0 && ((x >> 47) & 1UL) == 0) r++; } return r; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmDict(ulong[] a, Dictionary<ulong,int> d)
{ long r = 0; for (int i = 0; i < a.Length; i++) if (d.ContainsKey(a[i])) r++; return r; }

[MethodImpl(MethodImplOptions.NoInlining)] static long ArmPinvoke(int n)
{ long r = 0; for (int i = 0; i < n; i++) r += Native.TrivialCall(); return r; }

static (double best, double median, long sink) Bench(Func<long> f, int reps)
{
    var ms = new List<double>(); long sink = 0;
    f();                                            // warm
    for (int r = 0; r < reps; r++)
    { var sw = Stopwatch.StartNew(); sink += f(); sw.Stop(); ms.Add(sw.Elapsed.TotalMilliseconds); }
    ms.Sort();
    return (ms[0], ms[ms.Count / 2], sink);
}

Console.WriteLine($"TAGCOST n={N} reps={Reps} tagged_args={tagged} tiered={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "(unset)"}");

bool reverse = args.Length > 0 && args[0] == "--reverse";
(double best, double median, long sink) rNone, rA, rA2, rB, rB2, rCtl;
if (reverse)
{
    rCtl  = Bench(() => ArmDict(probeArgs, dict), Reps);
    rB2   = Bench(() => ArmB2(probeArgs), Reps);
    rB    = Bench(() => ArmB(probeArgs), Reps);
    rA2   = Bench(() => ArmA2(probeArgs), Reps);
    rA    = Bench(() => ArmA(probeArgs), Reps);
    rNone = Bench(() => ArmNone(probeArgs), Reps);
}
else
{
    rNone = Bench(() => ArmNone(probeArgs), Reps);
    rA    = Bench(() => ArmA(probeArgs), Reps);
    rA2   = Bench(() => ArmA2(probeArgs), Reps);
    rB    = Bench(() => ArmB(probeArgs), Reps);
    rB2   = Bench(() => ArmB2(probeArgs), Reps);
    rCtl  = Bench(() => ArmDict(probeArgs, dict), Reps);
}

static void Row(string name, (double best, double median, long sink) r, int n)
    => Console.WriteLine($"TAGCOST {name,-6} best={r.best,8:F2} ms  median={r.median,8:F2} ms  " +
                         $"ns/call best={r.best * 1e6 / n,6:F3}  sink={r.sink}");

Row("none", rNone, N); Row("A", rA, N); Row("A2", rA2, N); Row("B", rB, N); Row("B2", rB2, N); Row("CONTROL", rCtl, N);

double a = rA.best, a2 = rA2.best, b = rB.best, b2 = rB2.best, none = rNone.best, ctl = rCtl.best;
const int PinvokeN = 1_000_000;
var rPinv = Bench(() => ArmPinvoke(PinvokeN), Reps);
double pinvNs = rPinv.best * 1e6 / PinvokeN;
Console.WriteLine($"TAGCOST ANCHOR  pinvoke best={rPinv.best,8:F2} ms over {PinvokeN} calls  ns/call={pinvNs:F3}  " +
                  $"({(OperatingSystem.IsWindows() ? "kernel32!GetCurrentProcessId" : "libc getppid")}) " +
                  "-- the managed->native transition the trampoline pays anyway; a LOWER BOUND on the guarded call, NOT the syscall cost");
Console.WriteLine($"TAGCOST delta B2-A = {(b2 - a) * 1e6 / N:F4} ns/call   B2 door cost over none = {(b2 - none) * 1e6 / N:F4} ns/call");
Console.WriteLine(rB2.sink == rB.sink
    ? "TAGCOST B2 agrees with B on the refusal set -- same predicate, different spelling"
    : $"TAGCOST B2 DISAGREES with B (B2={rB2.sink} B={rB.sink}) -- the spellings are NOT the same predicate, which voids the comparison");
Console.WriteLine($"TAGCOST delta B-A = {(b - a) * 1e6 / N:F4} ns/call   " +
                  $"door cost over none: A={(a - none) * 1e6 / N:F4}  B={(b - none) * 1e6 / N:F4} ns/call");
Console.WriteLine(rA.sink == rB.sink
    ? $"TAGCOST arms AGREE on the refusal count ({rA.sink / Reps} per pass, {tagged} tagged args) -- both doors refuse the same set"
    : $"TAGCOST ARMS DISAGREE A={rA.sink} B={rB.sink} -- the two doors do NOT refuse the same set, which is a FINDING not a timing result");
double floorNs = Math.Abs(a2 - a) * 1e6 / N, deltaNs = (b - a) * 1e6 / N;
Console.WriteLine($"TAGCOST-FLOOR |A2-A| = {floorNs:F4} ns/call (identical code, two methods) vs |B-A| = {Math.Abs(deltaNs):F4} ns/call -- " +
    (Math.Abs(deltaNs) > 3 * floorNs
        ? "B-A is ABOVE the harness noise floor, so the delta is a reading"
        : "B-A is WITHIN the noise floor, so this harness CANNOT resolve it and the delta is NOT a reading"));
Console.WriteLine($"TAGCOST order={(reverse ? "reverse" : "forward")}");
Console.WriteLine($"TAGCOST MATERIALITY door A = {(a-none)*1e6/N:F4} ns/call, B = {(b-none)*1e6/N:F4} ns/call, " +
                  $"|B-A| = {Math.Abs(deltaNs):F4} ns/call, against a guarded call whose LOWER BOUND on this host is {pinvNs:F3} ns/call " +
                  $"=> B's excess over A is {Math.Abs(deltaNs)/pinvNs*100:F3}% of that lower bound (a real syscall is strictly larger, so this OVERSTATES the share)");
// PER-CALL, not per-test: the door sits on SyscallN(uintptr trap, params ...uintptr args) and runs
// ONCE PER ARGUMENT. Quoting the per-test number as a per-call cost understates it by the arity.
Console.WriteLine("TAGCOST PER-CALL (door runs once per argument; Syscall/Syscall6/Syscall18 exist)");
foreach (int nargs in new[] { 1, 5, 18 })
    Console.WriteLine($"TAGCOST   nargs={nargs,2}: A={(a-none)*1e6/N*nargs,7:F3} ns  B={(b-none)*1e6/N*nargs,7:F3} ns  " +
                      $"B-A={deltaNs*nargs,7:F3} ns  = {Math.Abs(deltaNs)*nargs/pinvNs*100,6:F3}% of the {pinvNs:F1} ns lower bound on the guarded call");
Console.WriteLine(ctl > a * 1.5 && ctl > b * 1.5
    ? $"TAGCOST-CONTROL OK: the expensive door benches {ctl / Math.Max(a, b):F1}x the cheap ones, so this harness CAN resolve a difference"
    : "TAGCOST-CONTROL FAILED: the expensive door did not separate, so a null A-vs-B result here means NOTHING");

static class Native
{
    [DllImport("libc", EntryPoint = "getppid")] private static extern int PosixGetPpid();
    [DllImport("kernel32", EntryPoint = "GetCurrentProcessId")] private static extern uint WinGetCurrentProcessId();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long TrivialCall() => OperatingSystem.IsWindows() ? (long)WinGetCurrentProcessId() : PosixGetPpid();
}
