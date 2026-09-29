# SIZING -- P1's runtime fixable rows, the two LFStack rows and the six held rows (2026-09-29)

> Measured on LINUX, Go 1.24.13, .NET 10 Release, tiered compilation off, cgo off. The three fixable rows
> are cut on `claude/p1-runtime-fixable` (branched from master `2ff42f7a16`); every other reading is the
> disclosure seat's full runtime row at `7c27acb6c5` (`claude/p1-runtime-disclosures`). Author: P1 (Sonnet
> 5.5, high). Nothing here is measured on Windows or macOS.

## 1. The three fixable rows

| Row | Cause read | Cut | Filtered reading (linux) |
|---|---|---|---|
| TestStringW | `rawstring` calls `mallocgc`, which the managed host does not run; it died in `mallocgcTiny` (nil dereference), so `gostringw` and every caller of `rawstring` died with it. A 1 MiB request took the whole test host down. | `rawstring` registered in `manualConversionFuncs["runtime"]`; hand-owned in `managed_impl.cs` over one `byte[]` that backs both the `@string` (which wraps without copying) and the `slice<byte>`. RED `6e79b83e81`, GREEN `2249374fb1`, regen `8207999660`. | pass (Go pass) |
| TestGroupSizeZero | the synthesized `SwissMapType` left `Group` nil, so `(~mt).Group.Size()` dereferenced nil | `synthesizeGroup` in `internal/abi/type_impl.cs` derives Go's group layout (`struct { ctrl uint64; slots [8]struct { key; elem } }`, key or elem over 128 bytes held by pointer, a pad byte after a trailing zero-size field) from the key and elem descriptors' stamped size, alignment and pointer prefix, and answers no group where a size is not stamped. `GroupSize` is `Group.Size_`; `SlotSize`, `ElemOff` and `Flags` stay zero. Expected sizes (16, 136, 72, 200, 136, 136) are `unsafe.Sizeof` of Go's struct. RED `89d327ff57`, GREEN `054a28869b`. Whole-file hand-owned, so no emission. | pass (Go pass) |
| TestSmhasherAvalanche | `efaceHash` reads an eface through a pointer to the interface variable, which is a managed reference with no address (arm-2a refusal `*eface over 0x...`) | `efaceHash` registered and hand-owned in `managed_impl.cs`: nil hashes to its seed, an unhashable dynamic type panics naming it, and the value is hashed as `c1 * typehash(t, value, seed ^ c0)` for regular-memory kinds (over their own bytes, memhash32/64/memhash split) and strings; every other kind is refused by name. RED `2a5913115f`, GREEN `e537088567`, regen `6e110052ce`. | **still fail**: EfaceKey now passes; the panic moves to `*iface over 0x...` at IfaceKey |

Emission footprint (two-seeded, three targets, windows/linux/darwin): rawstring 4 files (string.cs -8/+1 and
three `package_info.cs` 1/1 each); efaceHash 4 files (alg.cs -9/+3, ifaceHash's temp renumbered `ᴛ5`->`ᴛ4`
because the temp counter is file-wide, and three `package_info.cs` 1/1 each). Every before-arm equals the
committed file. GolibTests at the tip, `GoTargetOS=linux`: 1290 passed, 0 failed, 16 skipped.

### 1a. What blocks TestSmhasherAvalanche, and what it needs

`IfaceHash` in export_test.go is typed through `ifaceHash`'s parameter, an anonymous `interface{ F() }`. The
converter lifts it as `ifaceHash_i` and publishes a `GoDynamicTypeLift` record from the converted declaration
only. Registering `ifaceHash` as a manual function displaces that declaration, the record leaves
`package_info.cs`, and the `-tests` conversion fails with "1 unresolved dynamic type(s) ... export_test.cs(273):
interface{F()}" (measured; declaring `ifaceHash_i` by hand in `managed_impl.cs` does not help, because the
resolution reads the published record). So `ifaceHash` stays converted and the row stays red until the
converter lifts a manual function's dynamic-typed signature (emit the lifted interface beside the placeholder,
or publish the record). That is a converter seat: proposed for C2, footprint every manual function whose
signature carries an anonymous interface or struct (unmeasured; the runtime row is the first to need it).
After it, P1 registers `ifaceHash` with the same body shape as `efaceHash`.

## 2. TestLFStack and TestLFStackStress (behind `spanOf`)

Both now get past `spanOf` and fail on real behaviour ("no lifo", "Wrong sum ..."). Cause, measured: the
lock-free stack itself is right (a probe over three `persistentalloc`'d `lfnode`s pushes and pops in LIFO
order with correct packing). The test's node is `MyNode { LFNode; data int }`, allocated in raw native memory
and reinterpreted between `MyNode` and `LFNode`. The CLR lays `MyNode` out as `data, ʗLFNode` (read from the
`runtime.tests.dll` metadata: `SequentialLayout`, fields `data,ʗLFNode`), because the user's part declares
`data` and the source generator adds the embedded field's storage in a later partial part. Go's layout is
`LFNode` at 0 and `data` at 16, so `node.next = old` (offset 0 of the `LFNode` alias) overwrites `data`, and
the popped node's `data` reads back a packed pointer.

**Proposed class: structural** (a reinterpretation over Go's byte layout that a partial-part CLR layout does not
honour). A plan candidate exists but is large and unmeasured: emit an embedded field's storage in declaration
order (converter and generator), whose footprint is every struct that embeds a type and declares another
field. Recommendation: disclose structural now, name the layout-order plan in the entry's reading, and let a
converter seat decide whether to take it.

## 3. The six held rows, with a proposed class each

| Row | Reading (linux, disclosure seat's full row) | Proposed class | Plan or reason |
|---|---|---|---|
| TestIntStringAllocs | fail: 2 objects, 64 B per run (want 0); `s1 := string(r)`, `s2 := string(r+1)`, compared with `==` | deferred | DESIGN-string-byte-window.md §7 stage 4 (the rune arm), whose predicate covers a rune conversion handed to a non-retaining callee; this site is consumed only by a comparison, so the predicate needs widening. C1 owns the record; COORD to rule the widening. |
| TestConcatTempString | fail: 2 objects, 88 B per run; `"prefix " + string(b) + " suffix" != "prefix bytes suffix"` | deferred | No record covers it. Cause: the three-operand concat allocates an intermediate `@string` and the result (two objects); Go's result is a 32-byte stack `tmpBuf`. Proposed: a new stage in §7 for an N-ary concat consumed entirely by a comparison, over a transient buffer. Feasibility unmeasured. C1 owns the record; COORD to rule. |
| TestArrayHash | fail: 326 objects per run (977,680 B over 10 runs), threshold 6; 256 `var k [8]string` locals and 70 map-key copies | deferred | Two records: REC-B (`DESIGN-nonescaping-locals.md`, local fixed arrays) for the 256 locals and REC-A (`DESIGN-array-value-storage.md`, by-value copy face) for the key copies. Both are needed to reach the threshold. |
| TestNonEscapingMap | fail: 1 object, 232 B per run in all four shapes (map literal, no hint, small hint, variable hint), each followed by `m[0] = 0`; want 0 | structural | golib's `map<K,V>` is a CLR object graph and the test inserts an entry, so the storage is real; there is no frame-local map form. If COORD prefers a plan, the only candidate is a non-escaping-map shape in REC-B with a map variant that keeps its first group inline; feasibility unmeasured and likely large. |
| TestNewOSProc0 | infrastructure-error: `NotImplementedException` in `clone` (assembly stub) called from `newosproc0` | runtime-capability (linux) | A raw `clone` syscall running a function pointer on a new OS thread has no meaning where goroutines are CLR threads. The fail arm cannot disclose an infrastructure-error, so the cut is: `clone` refuses by name in `linux/os_linux_impl.cs`, then the entry. |
| TestSignalM | infrastructure-error: `NotImplementedException` in `getpid` (assembly stub) from `SendSigusr1` | runtime-capability (linux) | Per-M signal delivery needs a kernel thread id per M and Go's SIGUSR1 handler, neither of which exist here. `getpid` itself is implementable (`Environment.ProcessId`); the seat reads where the test first dies past it and refuses by name at that point, then the entry. |
