# DESIGN -- the string byte-window: candidate C of the `os` want-zero residue (segment 1)

> Drafted 2026-09-05 by lane G, proposed to the coordinator at mailbox `15a668bf2` and cut on that
> proposal. It is the THIRD step of the `os` row's retirement plan (B, then E, then C) and the plan
> the `TestUTF16Alloc` entry needs before it can be written as a legal `deferred` entry at all --
> which is why it is cut now rather than after E. Every price that depends on a landed seat is marked
> **re-read at the landing**. Nothing is cut by this record.

## 0. The site

Go's `os.(*File).WriteString` is two lines (`os/file.go:300`, go1.23.12):

```go
b := unsafe.Slice(unsafe.StringData(s), len(s))
return f.Write(b)
```

The idiom is Go's canonical zero-copy string-to-bytes VIEW: `StringData` takes the interior pointer
`&s[0]`, `unsafe.Slice` rebuilds a slice header over it, and no byte is copied. It is the last object
on the `os` want-zero row -- segment 1 of the residue decomposition.

The converted emission is faithful, construct by construct, and that is exactly why it allocates:

```csharp
var b = @unsafe.Slice(@unsafe.StringData(s), len(s));
```

`@unsafe.StringData(s)` ends in `Ꮡ(str.Slice(0, str.Length), 0)` -- and each half of that is already
correct on its own. `@string.Slice` returns `new slice<byte>(m_value, m_offset + start, ...)`: a
**zero-copy window over the string's own backing array**, a struct, allocating nothing. `@unsafe.Slice`
over a managed element box returns that window back, also without copying. What allocates is the
**element box in the middle** -- the `Ꮡ(window, 0)` that exists only to be immediately consumed by the
call that rebuilds the window it came from.

## 1. The population -- exactly two production sites, censused at the pin

`unsafe.Slice(unsafe.StringData(` over `go1.23.12`'s `src`, production files:

| site | package |
|:--|:--|
| `os/file.go:300` | `os` -- the row's own site |
| `hash/maphash/maphash_runtime.go:37` | `hash/maphash` |

**Zero** in `_test.go` files. The bare `unsafe.StringData(` census is wider (runtime 6, then single
sites in `syscall`, `os`, `log/slog`, `hash/maphash`, `go/types`, `crypto/x509/internal/macos`), but
those are not this idiom: they take the pointer for their own purposes rather than immediately
rebuilding a window over it, and the rule below does not touch them. This confirms E's §3 sizing
("population two in std") as measured rather than estimated.

## 2. The mechanism -- recognize the composite, emit the window that already exists

The rule is a converter-side pattern over ONE expression shape: `unsafe.Slice(unsafe.StringData(x),
len(x))`, where the two `x` are the SAME string expression and the length argument is `len` of that
same expression. Emit the window directly:

```csharp
var b = s.Slice(0, len(s));
```

Three properties make this the cheap member of the residue rather than another box-reduction arc.
**The target form already exists and is already correct** -- `@string.Slice` is the zero-copy view
over the string's own backing array, which is precisely what Go's idiom denotes, so there is no golib
change and no new API. **Nothing is minted**: no element box, no interface temp, no reconstruction.
And **the aliasing contract is preserved exactly** -- both forms produce a window over the string's
own storage, so a reader observing the string's bytes through `b` sees the same bytes at the same
addresses; Go forbids writing through it, and nothing about this changes what a write would do.

Refusals, each because the shapes are not the same expression: a differing length argument
(`unsafe.Slice(unsafe.StringData(s), n)`), two different string expressions, a `StringData` whose
result is stored, returned, compared or passed anywhere other than that one `unsafe.Slice` call, and
any use of `StringData` that is not immediately consumed by it. The census's wider `StringData`
population is exactly that refused class, and it stays on today's emission.

## 3. Prediction, on record

- **The `os` row (Release, tiering off, the same-tree A/B), AFTER B and E: 64.25 B / 1 obj → 0.25 B /
  0 obj** -- the bank condition, and the assertion `TestWriteStringAlloc` wants. Segment 1 is the
  last object. Falsifiers: any count other than 0; bytes above 0.25 with the count at 0 (a temp the
  read did not name); and any movement in the row's earlier segments, which this rule cannot reach.
- **`hash/maphash`** takes the same rule at its own site and is the second measurable consumer; its
  row is banked, so its verdict count must not move -- a changed count there is a falsifier, not a
  bonus.
- Corpus footprint: the two-seeded three-target diff confined to `os/<goos>/file.cs` and
  `hash/maphash/maphash_runtime.cs`, and nowhere else, because the refused class is everything else.
  **Re-read at the landing** -- the figures above assume B and E have landed; before them the same
  cut removes the same one object from a larger row.

## 4. Gates (if ruled)

The converter suite with the predicate's guard (the composite recognized; each refusal shape left on
today's emission, including the same-string-different-length case); the two-seeded three-target
`-stdlib` diff applied by hunk with its path set predicted first; CNR; `go2cs.slnx`; the `os` row's
own sweep and `hash/maphash`'s banked row at its banked count; the os-row A/B on this box against the
table above. No golib change, so the golib gate list is not owed -- which is what makes this the
cheapest of the three and the reason it is nonetheless LAST: it can only be measured once B and E
have taken the objects above it.

## 5. Why this record exists before its increment

`TestUTF16Alloc`'s `deferred` entry needs a plan that EXISTS (the owner's strengthening: an entry with
no executable plan is refused), and its string-materialization component is this family. The entry
references this record; the record's own increment runs in phase 4D behind B and E.

## 6. Dated amendment, 2026-09-23 (C1, REC-D of the H10 relabel ruling) -- the MIRROR arm

Ruled at ledger 2026-09-23 03:37 (X(2) REC-D, O4, O5). G's acknowledgement is non-gating. Nothing
above this block is rewritten; read at `bb54ff0920`. **Owner: C1. Full design: phase-4D kickoff.**

**The shape.** The mirror of section 2's composite: `unsafe.String(unsafe.SliceData(b), len(b))`,
where the two `b` are the SAME `[]byte` expression. Go denotes a string over `b`'s own storage and
allocates nothing. The emission mints an element reference for `SliceData` (`Ꮡ(slice, 0)`,
unsafe.cs:985, one counted `ElemRefBox`) and then rebuilds the window from it.

**The population: exactly two production sites in the converted stdlib** at go1.24.13 (GOROOT has a
third, cmd/go/internal/modindex/read.go:973, outside the corpus) --
- `strings.(*Builder).String`, strings/builder.go:41, emitted at src/core/strings/builder.cs:44;
- `syscall.UTF16ToString`, syscall/syscall_windows.go:84, emitted at
  src/core/syscall/windows/syscall_windows.cs:89.

**The stage that REMOVES the counted allocation.** Recognise the composite and emit the `@string`
window over `b`'s backing directly -- the same window `unsafe.String` already builds on its
element-window arm, without the element reference in between. *Removes:* one `ElemRefBox` per call.
*Preconditions:* the window is the persistent `@string` form, not `tmpstring`, because both results
escape (they are returned); and the aliasing is Go's own (a Builder only appends past `len`, and
`UTF16ToString`'s buffer is dead after the call).

**Refusals:** a differing length argument; two different slice expressions; a `SliceData` result that
is stored, returned or passed anywhere but that one `unsafe.String` call; a native-backed pointer
(the window must be over a managed backing).

**Predictions (counted objects per run):** os TestUTF16Alloc 2 -> 1 (want exactly 1; the pass then
reads Go's one allocation). strings TestBuilderAllocs 2 -> 1 and bufio TestReadStringAllocs 2 -> 1,
leaving the `ж<Builder>` box -- and, because the buffer `bytealg.MakeNoZero` allocates is uncounted
today (bytealg_impl.cs:24), that 1 is the box, not Go's buffer: a pass there rests on the counter's
coverage boundary, and when REC-F (iii) counts the buffer both read 2 again. strings TestBuilderGrow
one fewer on every leg; strings TestBuilderGrowSizeclasses 3 -> 2.

**Gates.** The recognition is controlled both ways before any member is read: a composite with a
DIFFERING length argument and one whose `SliceData` result is stored must both keep today's emission.
Then the four members' rows -- os (TestUTF16Alloc), strings (TestBuilderAllocs, TestBuilderGrow,
TestBuilderGrowSizeclasses), bufio (TestReadStringAllocs) -- read before and after at Release with
tiering off.

*Fixed up 2026-09-23 (COORD's ACCEPT-WITH-FIXES, ledger 3942e083ad, item 10 and the §6 note): the
owner line, the gates paragraph and "in the converted stdlib" added.*

## 7. Dated amendment, 2026-09-23 (C1, from the H10 relabel) -- the TRANSIENT-conversion arm

Written so the string-conversion family's `deferred` entries cite a record, as the relabel ruling's
plan bar requires (ledger 2026-09-23 03:37, X(1)). Read at `bb54ff0920`. Owner: C1. Full design:
phase-4D kickoff. Nothing above this block is rewritten.

**The class.** A conversion between `string` and `[]byte` (or a rune) whose RESULT is not retained --
consumed by a comparison, a map probe, a range, or a callee that reads it and returns -- costs Go no
allocation (the compiler's `slicebytetostringtmp` and `stringtoslicebytetmp`, and `intstring`'s 4-byte
stack buffer). golib already has both non-allocating forms and the converter already emits them for
PART of the class: `builtin.tmpstring` for a map-read key (Reference, `m[string(b)]`) and the
`sstring` view for a provably read-only, non-escaping `string([]byte)` (Reference, *Strings (`@string`
and `sstring`)*). Everything else in the class copies through `new @string(...)` or `slice<byte>(s)`,
one counted object per evaluation.

**The stages that REMOVE the counted allocation**, each an extension of an emission that exists:

1. **Type-parameter operands.** In a generic function over `T string | []byte`, `string(x) ==
   string(y)` renders through `ByteSeqExtensions.ToGoString` and copies both sides (bytealg.cs:82, :90,
   :110 in `IndexRabinKarp`/`LastIndexRabinKarp`), where the same comparison on a concrete `[]byte`
   takes `sstring` (bytes.Equal, bytes.cs:25). Extend the `sstring` elision to the type-parameter
   operand. *Removes:* two objects per match check.
2. **A non-retaining callee.** `f(string(b))` where `f` reads its parameter and does not retain it
   (strconv's `Atoi(string(bytes.Number))`; mime `TypeByExtension`'s `(@string)lower` probe at
   mime/type.cs:140): emit `tmpstring(b)`, whose contract is exactly "not retained". *Precondition:* a
   callee classification, cached per function, that the parameter is never stored, returned, captured
   or sent. **Go parity holds only up to 32 bytes:** for a non-escaping `string(b)` argument Go uses the
   32-byte stack `tmpBuf` (`slicebytetostring`) and still allocates above it, so beyond 32 bytes this
   stage allocates less than Go does. Every member's string is shorter.
5. **A comparison operand the concrete `sstring` elision does not reach.** strings TestBuilderGrow's
   `b.String() != (@string)p` (strings/builder_test.cs:146) is a comparison operand, not a call argument,
   so stage 2 does not cover it; the existing elision declines it because `p` is closure-captured.
   Extend the elision's predicate to an operand consumed entirely inside one comparison expression.
   *Removes:* one object per leg on growLen>0.
3. **`range []byte(s)`.** Go walks the string's bytes without a copy; the emission is
   `slice<byte>(s)` (strconv/atoi.cs:267). Range over the string's own byte span.
4. **The rune arm.** `string(r)` handed to a non-retaining callee (bytes and strings `IndexRune`'s
   `Index(s, string(r))`): a transient one-rune `@string` over a 4-byte frame buffer, the managed
   counterpart of `intstring`'s stack buffer.

**Refusals:** a result that is stored, returned, captured, sent, or passed to a callee not classified
non-retaining; a source buffer written between the conversion and the last use of its result.

**Predictions (counted objects per run):** bytes TestIndex 2 -> 0 and TestLastIndex 2 -> 0 (stage 1);
strconv TestAllocationsFromBytes/Atoi 3 -> 1, /ParseInt 4 -> 2 and /ParseUint 3 -> 1 (stages 2 and 3;
their per-call function-local consts -- `fnAtoi`, `fnParseInt`, `fnParseUint` -- are
DESIGN-string-literal-allocation.md §8's const arm, which takes each to 0); /ParseBool, /ParseFloat,
/ParseComplex, /CanBackquote, /AppendQuote, /AppendQuoteToASCII and /AppendQuoteToGraphic 1 -> 0 (stage
2); bytes and strings TestIndexRune to 0 (stage 4); mime TestLookupMallocs one fewer (stage 2); strings
TestBuilderGrow one fewer on each growLen>0 leg (stage 5).

*Fixed up 2026-09-23 (COORD's ACCEPT-WITH-FIXES, ledger 3942e083ad, the §7 note and item 8): stage 2's
32-byte parity, stage 5 for the comparison operand, and every strconv leg named.*

**Gate:** each stage is controlled against a retained result (it must keep the copy), then its members'
rows read before and after at Release with tiering off.

## 8. Dated amendment, 2026-09-23 (C1, COORD's ACCEPT-WITH-FIXES item 3) -- a STORED StringData whose every reader rebuilds the string

Written because log/slog's F7 had no stage: §2 names slog's `StringData` as outside the idiom, §7
refuses a stored result, and REC-E refuses a stored pointer. Read at `bb54ff0920`. **Owner: C1. Full
design: phase-4D kickoff.** Nothing above this block is rewritten.

**The site.** `slog.StringValue` stores a pointer to the string's first byte and its length:
`new Value(num: (uint64)len(value), any: new stringptr(@unsafe.StringData(value)))`
(log/slog/value.cs:118). `StringData` mints a counted `ElemRefBox` (`Ꮡ(…, 0)`). Go stores the pointer
for free. Every READ of a `stringptr` in the package rebuilds the string and nothing else:
`@unsafe.String(sp, v.num)` (value.cs:348 and :356); the only other use is the type switch in
`Kind()` (value.cs:90), which reads the dynamic type, not the pointer.

**The stage that REMOVES the counted allocation.** Classify a named pointer type whose EVERY read in
its package is `unsafe.String(p, n)` (or a type test), and carry the `@string` window itself -- or its
backing plus an offset -- in place of the `ElemRefBox`; the reader then returns the window directly.
*Removes:* one counted object per string `Value`. *Preconditions:* the classification covers every read
of the type in the package (it is unexported, so the package is the whole population); the pointer is
never dereferenced, compared, converted to `unsafe.Pointer`/`uintptr` or exposed outside the package.

**Refusals:** an exported pointer type; any dereference, comparison or conversion of the pointer; a
reader that uses a length other than the one stored beside it.

**Members:** log/slog TestAlloc/2_pairs, /9_kvs, /attrs3, /attrs3_disabled, /attrs6, /attrs9,
TestAttrNoAlloc, TestValueNoAlloc.

**Predictions (counted objects per run, UNMEASURED):** 2_pairs 10 -> 9; 9_kvs 27 -> 24; attrs3 12 -> 11;
attrs3_disabled 9 -> 8; attrs6 21 -> 19; attrs9 28 -> 25; TestAttrNoAlloc 14 -> 13; TestValueNoAlloc
15 -> 14.

**Gate:** the classification is controlled both ways (a type with one dereferencing reader must be
refused), then log/slog's row reads before and after at Release with tiering off; slog's
TestValueString and TestValueEqual are the behaviour gates.
