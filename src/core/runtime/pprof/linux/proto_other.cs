// Copyright 2022 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !windows && !darwin
namespace go.runtime;

using errors = errors_package;
using os = os_package;

partial class pprof_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string procSelfMapsˢ = "/proc/self/maps"u8;

// readMapping reads /proc/self/maps and writes mappings to b.pb.
// It saves the address ranges of the mappings in b.mem for use
// when emitting locations.
internal static void readMapping(this ж<profileBuilder> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    var (data, _) = os.ReadFile(procSelfMapsˢ);
    parseProcSelfMaps(data, Ꮡb.addMapping);
    if (len(b.mem) == 0) {
        // pprof expects a map entry, so fake one.
        b.addMappingEntry(0, 0, 0, ""u8, ""u8, true);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string notImplementedˢ = "not implemented"u8;

// TODO(hyangah): make addMapping return *memMap or
// take a memMap struct, and get rid of addMappingEntry
// that takes a bunch of positional arguments.
internal static (uint64 start, uint64 end, @string exe, @string buildID, error err) readMainModuleMapping() {
    return (0, 0, "", "", errors.New(notImplementedˢ));
}

} // end pprof_package
