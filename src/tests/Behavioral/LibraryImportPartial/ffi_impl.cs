// ffi_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

// The hand-owned half of this behavioral test — see main.go for what it guards and why the guard is
// mostly a COMPILE assertion. This file has no `.go` counterpart, so a transpile never regenerates
// it; it is compiled by the project's `<Compile Include="*.cs" />` exactly as a corpus `*_impl.cs`
// companion is.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The declaration under test. The converter scans this package's output directory for it and unions
// the result into the <AllowUnsafeBlocks> of the .csproj it regenerates on every transpile — which
// is the only way the property can survive a reconvert. Remove this line and the project stops
// compiling (SYSLIB1062), which is the negative control the unit guards in
// src/go2cs/requiresUnsafeMarker_test.go assert directly.
[module: go.GoRequiresUnsafe]

namespace go;

internal static partial class LibraryImportProbe
{
    // GetCurrentProcessId is chosen for being total: no arguments, no marshalling, no failure mode,
    // and a return the caller can check without knowing anything about the machine. What is under
    // test is that the SOURCE GENERATOR — not the runtime marshaller — produced a stub that reaches
    // the kernel at all.
    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentProcessId")]
    private static partial uint GetCurrentProcessId();

    // A module initializer runs before Main, so a failure here suppresses the program's only line of
    // output and the behavioral suite's output comparison reports it. Guarded on Windows because
    // kernel32 is: the suite is a Windows harness today, and this keeps that an assumption the file
    // states rather than one it depends on.
    [ModuleInitializer]
    internal static void Probe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (GetCurrentProcessId() == 0)
        {
            throw new InvalidOperationException("source-generated GetCurrentProcessId returned 0");
        }
    }
}
