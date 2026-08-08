// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
namespace go.runtime;

using errors = errors_package;

partial class pprof_package {

// readMapping adds a mapping entry for the text region of the running process.
// It uses the mach_vm_region region system call to add mapping entries for the
// text region of the running process. Note that currently no attempt is
// made to obtain the buildID information.
internal static void readMapping(this ж<profileBuilder> Ꮡb) {
    ref var b = ref Ꮡb.DerefOrNull();

    if (!machVMInfo(Ꮡb.addMapping)) {
        b.addMappingEntry(0, 0, 0, ""u8, ""u8, true);
    }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string machVMInfoFailedˢ = "machVMInfo failed"u8;

internal static (uint64 start, uint64 end, @string exe, @string buildID, error err) readMainModuleMapping() {
    uint64 start = default!;
    uint64 end = default!;
    @string exe = default!;
    @string buildID = default!;

    var first = true;
    var ok = machVMInfo((uint64 lo, uint64 hi, uint64 off, @string @file, @string build) => {
        if (first) {
            (start, end) = (lo, hi);
            (exe, buildID) = (@file, build);
        }
        // May see multiple text segments if rosetta is used for running
        // the go toolchain itself.
        first = false;
    });
    if (!ok) {
        return (0, 0, "", "", errors.New(machVMInfoFailedˢ));
    }
    return (start, end, exe, buildID, default!);
}

} // end pprof_package
