// Copyright 2018 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package objabi -- go2cs converted at 2022 March 13 05:43:21 UTC
// import "cmd/internal/objabi" ==> using objabi = go.cmd.@internal.objabi_package
// Original source: C:\Program Files\Go\src\cmd\internal\objabi\funcid.go
namespace go.cmd.@internal;

using strings = strings_package;

public static partial class objabi_package {

// A FuncFlag records bits about a function, passed to the runtime.
public partial struct FuncFlag { // : byte
}

// Note: This list must match the list in runtime/symtab.go.
public static readonly nint FuncFlag_TOPFRAME = 1 << (int)(iota);
public static readonly var FuncFlag_SPWRITE = 0;

// A FuncID identifies particular functions that need to be treated
// specially by the runtime.
// Note that in some situations involving plugins, there may be multiple
// copies of a particular special runtime function.
public partial struct FuncID { // : byte
}

// Note: this list must match the list in runtime/symtab.go.
public static readonly FuncID FuncID_normal = iota; // not a special function
public static readonly var FuncID_abort = 0;
public static readonly var FuncID_asmcgocall = 1;
public static readonly var FuncID_asyncPreempt = 2;
public static readonly var FuncID_cgocallback = 3;
public static readonly var FuncID_debugCallV2 = 4;
public static readonly var FuncID_gcBgMarkWorker = 5;
public static readonly var FuncID_goexit = 6;
public static readonly var FuncID_gogo = 7;
public static readonly var FuncID_gopanic = 8;
public static readonly var FuncID_handleAsyncEvent = 9;
public static readonly var FuncID_jmpdefer = 10;
public static readonly var FuncID_mcall = 11;
public static readonly var FuncID_morestack = 12;
public static readonly var FuncID_mstart = 13;
public static readonly var FuncID_panicwrap = 14;
public static readonly var FuncID_rt0_go = 15;
public static readonly var FuncID_runfinq = 16;
public static readonly var FuncID_runtime_main = 17;
public static readonly var FuncID_sigpanic = 18;
public static readonly var FuncID_systemstack = 19;
public static readonly var FuncID_systemstack_switch = 20;
public static readonly var FuncID_wrapper = 21; // any autogenerated code (hash/eq algorithms, method wrappers, etc.)

private static map funcIDs = /* TODO: Fix this in ScannerBase_Expression::ExitCompositeLit */ new map<@string, FuncID>{"abort":FuncID_abort,"asmcgocall":FuncID_asmcgocall,"asyncPreempt":FuncID_asyncPreempt,"cgocallback":FuncID_cgocallback,"debugCallV2":FuncID_debugCallV2,"gcBgMarkWorker":FuncID_gcBgMarkWorker,"go":FuncID_rt0_go,"goexit":FuncID_goexit,"gogo":FuncID_gogo,"gopanic":FuncID_gopanic,"handleAsyncEvent":FuncID_handleAsyncEvent,"jmpdefer":FuncID_jmpdefer,"main":FuncID_runtime_main,"mcall":FuncID_mcall,"morestack":FuncID_morestack,"mstart":FuncID_mstart,"panicwrap":FuncID_panicwrap,"runfinq":FuncID_runfinq,"sigpanic":FuncID_sigpanic,"switch":FuncID_systemstack_switch,"systemstack":FuncID_systemstack,"deferreturn":FuncID_wrapper,"runOpenDeferFrame":FuncID_wrapper,"reflectcallSave":FuncID_wrapper,"deferCallSave":FuncID_wrapper,};

// Get the function ID for the named function in the named file.
// The function should be package-qualified.
public static FuncID GetFuncID(@string name, bool isWrapper) {
    if (isWrapper) {
        return FuncID_wrapper;
    }
    if (strings.HasPrefix(name, "runtime.")) {
        {
            var (id, ok) = funcIDs[name[(int)len("runtime.")..]];

            if (ok) {
                return id;
            }

        }
    }
    return FuncID_normal;
}

} // end objabi_package
