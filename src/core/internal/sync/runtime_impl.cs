// runtime_impl.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: BSD-3-Clause
// Use of this source code is governed by a BSD-style license
// that can be found in the LICENSE file.

// Hand-written bodies for internal/sync's two runtime fatal bridges. Go 1.24 moved Mutex into this
// new package and its `throw` and `fatal` are pushed from the runtime; the push does not arrive in
// this corpus, so PartialStubGenerator fills both with a throwing NotImplementedException.
//
// REACH, measured at this tree rather than assumed: `fatal` has ONE caller, mutex.cs:190 in
// unlockSlow — an unlock of an unlocked Mutex, which is a program bug Go reports and exits on.
// `@throw` has NONE in this package today. That is not a reason to leave it throwing: an
// unreached stub is precisely the member that reaches a user as a dead host rather than as a
// failing gate, and the first caller to arrive would meet the wrong failure with no warning.

// WHY A COMPANION AND NOT A //go:linkname REGISTRY ROW. The converter CAN forward a push into the
// consumer package, and five of RED 7's members are being wired that way (crypto/internal/fips140's
// three, crypto/internal/sysrand's fatal and crypto/internal/fips140hash's sha3Unwrap — G's seat).
// A row is admissible only where the consumer's project ALREADY references the pushing package.
// internal.sync.csproj carries NO reference to runtime, and adding one would be the W1 project-graph
// cycle class that check-solution-integrity's per-GOOS assertion exists to catch — runtime sits above
// internal/sync. So these two take the companion, not the row.
// COORD's split at 1fa7940a0, on G's reading of the project files at 5bb307d57e.
//
// WHY golib HOLDS THE PRIMITIVE, and not `runtime`. golib is the only assembly BELOW every converted
// package. FatalReport.cs argues this for itself and names this very set of consumers — it was
// written for the fatal shims already known, and these are the members the 1.24 hop added to the
// same class. One primitive there, a one-line forward here.
//
// WHAT THE FORWARD PRESERVES. Go's fatal is UNRECOVERABLE from its first instruction: no recover(),
// no deferred function, no catch. FatalReport.Fatal writes `fatal error: <text>`, a blank line and a
// Go-spelled traceback, then calls Environment.Exit(2), so the unrecoverability is structural rather
// than a property of the exception type — which is exactly what a throwing stub does NOT give: a
// NotImplementedException is an ordinary managed exception any frame above can catch.
//
// userFault is Go's throwType axis, read from Go's OWN push at the pin rather than inferred:
// runtime/panic.go:1061 pushes internal_sync_throw, whose body is `throw(s)` — throwTypeRuntime,
// userFault FALSE. :1066 pushes internal_sync_fatal, whose body is `fatal(s)` — throwTypeUser,
// userFault TRUE. The two differ, which is why this is two forwards and not one with a flag.

// Aliased rather than imported wholesale: this file needs exactly one golib type, and a blanket
// `using go.golib` would also pull that namespace's extension methods into a hand-owned file sitting
// beside converted code.
using FatalReport = go.golib.FatalReport;

// Hand-owned (no *_impl.go exists, so a reconvert never regenerates it); marked so the marker-based
// readers see it as well as the suffix-based ones.
[module: go.GoManualConversion]

namespace go.@internal;

partial class sync_package
{
    internal static partial void @throw(@string s) => FatalReport.Fatal(s, userFault: false);

    internal static partial void fatal(@string s) => FatalReport.Fatal(s, userFault: true);
}
