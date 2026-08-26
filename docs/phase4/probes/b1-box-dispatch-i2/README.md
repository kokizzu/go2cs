# b1-box-dispatch-i2 — the increment-2 amendments' bench (point-in-time record)

The measurement half of `DESIGN-zh-box-b1.md` increment 2 — REVIEW amendments 1A (per-cell
dispersion, isolated processes), 2A (the Pointer-typed virtual-Value site), 3 (the
PointerOrderToken/Equals/GetHashCode surface), 4 (the parent-mandated union-slot V2, built and
eliminated on time/bytes/counts), and 5 (the element-ref kind in its real canonical shape,
managed and foreign arms).

**A record, not a gate.** Numbers frozen in the design note. The banked increment-1 probe sits
beside this one at `../b1-box-dispatch/` and is not superseded — it owns the Value/ValueSlot
P-F2 discharge; this one owns everything the review found unmeasured.

Run: `dotnet run -c Release` ×4 (JIT arm), then publish `-p:PublishAot=true` and run the exe ×4
(AOT arm; needs MSVC link.exe — prepend the VS Installer dir to PATH). Outputs of record:
`output-jit-4proc.txt` / `output-aot-4proc.txt` (GRETCHEN-LAPTOP, CoreCLR/NativeAOT 10.0.11,
2026-08-26; JIT processes 1–2 briefly overlapped a killed AOT warmup — verified equal to the
solo processes 3–4 within ±1.2 % on the V1 baseline row).
