// os_windows_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The runtime's system-directory snapshot, taken the way a managed program can take it - the
// Windows sibling of goenvs_impl.cs and goargs_impl.cs in the parent folder, for the same reason
// and in the same shape.
//
// Go fills `runtime.sysDirectory` in initSysDirectory(), which osinit() calls before any user code:
// `stdcall2(_GetSystemDirectoryA, &sysDirectory[0], len(sysDirectory)-1)`, then it appends a
// backslash and records the length. Neither half survives conversion - osinit is the runtime
// bootstrap go2cs emits already marked not-run, and stdcall bottoms out in asmstdcall, a throwing
// stub - so the buffer stayed all-zero and sysDirectoryLen stayed 0.
//
// Zero is not a harmless absence here. `runtime.windows_GetSystemDirectory` is pushed onto
// internal/syscall/windows.GetSystemDirectory, and net reads it in a package-level var initializer:
// `hostsFilePath = windows.GetSystemDirectory() + "/Drivers/etc/hosts"`. Over an empty buffer that
// is the string "/Drivers/etc/hosts" - not an error, just a plausible-looking wrong answer, the
// failure mode this project has ruled against. The difference, as with argslice, is that this one
// CAN be honored: the CLR knows the system directory exactly.
//
// TRAILING BACKSLASH IS GO'S, NOT AN EMBELLISHMENT. initSysDirectory writes `sysDirectory[l] = '\\'`
// and sets `sysDirectoryLen = l + 1`, so Go's answer ends with a separator and net's concatenation
// really does produce `C:\Windows\System32\/Drivers/etc/hosts`. Environment.GetFolderPath returns no
// trailing separator, so the backslash is appended here to reproduce Go's string exactly rather than
// a tidier one.
//
// ENCODING. Go calls the ANSI entry point and treats the result as a Go string, so its bytes are the
// system code page's. UTF-8 is used here because a converted @string IS UTF-8 and because the two
// agree over the only content this path can produce - a system directory is ASCII on any real
// install. Where they could disagree, this is the answer Go would want rather than the bytes it
// would get.
//
// The Go-side failure is reproduced too: initSysDirectory throws "Unable to determine system
// directory" when the call returns nothing or overruns the buffer, and so does this - announcing,
// never falling back to a short answer that would read as real.
//
// A module initializer is the faithful stand-in for osinit's slot, for the reasons goenvs_impl.cs
// states at length: it runs when the runtime assembly is first touched, before any converted Go code
// in it, exactly once. The snapshot semantics are Go's own - the system directory is fixed for the
// life of the process.
//
// This file has no `<name>.go` counterpart, so a -stdlib reconvert never emits over it; the module
// marker states the ownership explicitly and matches the other hand-owned runtime files. It sits in
// the windows/ folder because its principal os_windows.cs does (layout L3).

using System;
using System.Runtime.CompilerServices;
using System.Text;

[module: go.GoManualConversion]

namespace go;

partial class runtime_package
{
    [ModuleInitializer]
    internal static void ᴛInitSysDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        byte[] directory = Encoding.UTF8.GetBytes(Environment.GetFolderPath(Environment.SpecialFolder.System));

        // initSysDirectory's own bounds, kept verbatim: the call must return something, and it must
        // leave room for the separator inside the _MAX_PATH+1 buffer.
        if (directory.Length == 0 || directory.Length > len(sysDirectory) - 1)
        {
            @throw(unableToDetermineSystemˢ);
        }

        for (nint i = 0; i < directory.Length; i++)
        {
            sysDirectory[i] = directory[i];
        }

        sysDirectory[directory.Length] = (byte)'\\';
        sysDirectoryLen = (uintptr)(directory.Length + 1);
    }
}