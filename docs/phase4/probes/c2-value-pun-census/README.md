# c2-value-pun-census -- the value-pun (math.Float*bits) seat's census and members (point-in-time record)

R's encoding/binary finding (ledger b408431d4a): `math.Float32bits`/`Float64bits` cost 3 counted objects
per call where Go has 0. The fix is recorded in `docs/ConversionStrategies-Reference.md`, "A numeric value
pun READ is a bitcast". C2, 2026-09-24, on `claude/c2-float-bits` off `fa18863b94`. Not a gate.

## 1. Static census (`main.go`)

It covers every `*(*U)(unsafe.Pointer(&x))` in std plus its tests, classified the way the converter
classifies it, and the call sites of the four math functions.

```
go build -o puncensus . && GOOS=<os> ./puncensus > census-<os>.txt 2> prod-sites-<os>.tsv
```

**Production, windows:**
- **12 pun READS between equal-size numerics**, each `x` addressed only by puns. These are the population
  the converter now bitcasts: math ×4, runtime float.go ×2, runtime histogram.go ×2, reflect
  float32reg_generic.go ×2, internal/runtime/atomic types.go ×2. The set is identical on linux and
  darwin.
- **Kept on the aliasing reinterpret:**
  - 4 numeric WRITES (runtime/minmax.go);
  - 1 unequal-size read;
  - 32 reads of other types (pointer words, slices, strings, funcs);
  - 9 writes of other types;
  - 20 puns of a non-identifier.
- **Calls into the four math functions:** 118 production (Float64bits 46, Float64frombits 46,
  Float32frombits 14, Float32bits 12) and 137 in tests.

## 2. Dynamic census: which AllocsPerRun rows call a pun, and how often per run

This uses the same method as `../c2-mnz-census/`. Go's own tests of the 46 std packages that call
`AllocsPerRun` ran on a go1.24.13 GOROOT whose math, runtime float and reflect float32 pun functions
count their calls while an `AllocsPerRun` window is open. The positive control, two calls per run, read
200 over 100 runs. Fractional per-run counts are Go's own background runtime work (the GC pacer's float
math). For example, strconv's `AppendQuoteToASCII(nil, oneMB)` leg reads 6.58 without touching a float.
They are excluded.

| row (Go's count) | pun calls per run | go2cs today (i7 reading run, COUNT) | predicted after, UNMEASURED |
|:--|--:|--:|--:|
| encoding/binary TestAppendAllocs (0) | 6 | 75 | ≤ 57 |
| database/sql TestRawBytesAllocs (0) | 2 | 28 | ≤ 22 |
| log/slog TestAttrNoAlloc (0) | 2 | 14 | ≤ 8 |
| log/slog TestValueNoAlloc (0) | 2 | 15 | ≤ 9 |
| fmt TestCountMallocs, `Sprintf("%g")` leg (1) | 1 | 7 (the disclosure's reading) | ≤ 4 |
| encoding/gob TestCountEncodeMallocs (0) | 1 | not in the reading set | 3 fewer |
| encoding/gob TestCountDecodeMallocs, both calls (0, 5) | 1 each | not in the reading set | 3 fewer each |
| strconv TestCountMallocs: `AppendFloat` ×2, `ParseFloat("123.456789123456789")` (0 each) | 1 each | not in the reading set | 3 fewer each |
| strconv TestCountMallocs: the two long `ParseFloat` legs (0 each) | 3 each | not in the reading set | 9 fewer each |

**Why "3" per call:** GolibTests `ValuePunBitcastTests.TheReinterpretItReplacesCostsThreeCountedObjectsPerCall`
runs the old emission's body verbatim and reads exactly 3 counted objects: the heap box, its pinnable
slot, and the reinterpreting reference. The bitcast reads 0.

**Why the predictions are "≤":**
- Other mechanisms on the same rows move independently. log/slog's two rows also carry REC-F (iv)'s
  predictions (C1's, 14 → 7 and 15 → 7), and the two do not add.
- A row whose LAST counted objects are these, and which still allocates uncounted bytes, crosses
  `AllocsPerRun`'s COUNT/BYTES seam (testing.cs:750) and reports bytes instead.

**Not executed:** 7 sites that do not run on linux or skip themselves, the same set `../c2-mnz-census/`
lists. None of them calls a float pun.
