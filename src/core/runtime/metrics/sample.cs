// Copyright 2020 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Hand-finished conversion (runtime/metrics.Read's crossing into the runtime).
//
// Go's Read hands the runtime the RAW ADDRESS of the caller's []Sample backing store
// (runtime_readMetrics, a //go:linkname push), and runtime.readMetricsLocked reconstructs a
// []metricSample over that address — an address-reinterpret the managed pointer model cannot
// alias, so the reconstructed slice read garbage names. The managed form carries the same data
// as plain managed values through runtime.readMetricsManaged (managed_impl.cs): names in,
// computed (kind, scalar, pointer) out, index-aligned, with readMetricsLocked's batch semantics
// — one lock hold, one defensive agg clear, per-sample ensure+compute in order — preserved on
// the runtime side. Sample itself and everything Read populates it from (initMetrics' table,
// the compute closures) stay auto-converted.
//
// The bodyless runtime_readMetrics declaration is deliberately GONE: its pushed body cannot be
// honored (linknamePushTargets records why), and this file no longer needs it.
//
// Hand-owned: the [module: GoManualConversion] marker keeps a -stdlib reconvert from
// regenerating this file (a <name>.cs.auto review sibling is dropped beside it instead).

[module: go.GoManualConversion]

namespace go.runtime;

using @unsafe = unsafe_package;

partial class metrics_package {

// Go runs a blank-imported package's `init` before this package's own; .NET would never
// load an assembly nothing references, so the side effects the import exists for are forced.
[GoInit] internal static void initᴛᴛblankImportꓸruntime() {
    builtin.initPackage(typeof(runtime_package));
}

// Sample captures a single metric sample. The `public` is carried HERE because this file is
// hand-owned: sample.go left the convert set, so the regenerated package_info.cs no longer
// anchors Sample's accessibility in its <TypeAccessibility> block (the runtime2_impl pattern —
// a hand-own declares its own types' accessibility).
[GoType] public partial struct Sample {
    // Name is the name of the metric sampled.
    //
    // It must correspond to a name in one of the metric descriptions
    // returned by All.
    public @string Name;
    // Value is the value of the metric sample.
    public Value Value;
}

// Read populates each [Value] field in the given slice of metric samples.
//
// Desired metrics should be present in the slice with the appropriate name.
// The user of this API is encouraged to re-use the same slice between calls for
// efficiency, but is not required to do so.
//
// Note that re-use has some caveats. Notably, Values should not be read or
// manipulated while a Read with that value is outstanding; that is a data race.
// This property includes pointer-typed Values (for example, [Float64Histogram])
// whose underlying storage will be reused by Read when possible. To safely use
// such values in a concurrent setting, all data must be deep-copied.
//
// It is safe to execute multiple Read calls concurrently, but their arguments
// must share no underlying memory. When in doubt, create a new []Sample from
// scratch, which is always safe, though may be inefficient.
//
// Sample values with names not appearing in [All] will have their Value populated
// as KindBad to indicate that the name is unknown.
public static void Read(slice<Sample> m) {
    var names = new slice<@string>(len(m));
    var kinds = new slice<nint>(len(m));
    var scalars = new slice<uint64>(len(m));
    var pointers = new slice<@unsafe.Pointer>(len(m));

    for (nint i = 0; i < len(m); i++) {
        names[i] = m[i].Name;
    }

    global::go.runtime_package.readMetricsManaged(names, kinds, scalars, pointers);

    for (nint i = 0; i < len(m); i++) {
        m[i].Value.kind = (ValueKind)kinds[i];
        m[i].Value.scalar = scalars[i];
        m[i].Value.pointer = pointers[i];
    }
}

} // end metrics_package
