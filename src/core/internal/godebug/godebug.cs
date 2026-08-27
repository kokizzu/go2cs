// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package godebug makes the settings in the $GODEBUG environment variable
// available to other packages. These settings are often used for compatibility
// tweaks, when we need to change a default behavior but want to let users
// opt back in to the original. For example GODEBUG=http2server=0 disables
// HTTP/2 support in the net/http server.
//
// In typical usage, code should declare a Setting as a global
// and then call Value each time the current setting value is needed:
//
//	var http2server = godebug.New("http2server")
//
//	func ServeConn(c net.Conn) {
//		if http2server.Value() == "0" {
//			disallow HTTP/2
//			...
//		}
//		...
//	}

// go2cs NATIVE IMPLEMENTATION (hand-owned; replaces the converted godebug.go output). The Go
// implementation maintains a cache of per-setting atomic pointers kept current by runtime update
// hooks (re-notified when os.Setenv changes $GODEBUG), reached through a *setting EMBEDDED in
// Setting and resolved under a sync.Once. The converted runtime has no Setenv notification, and
// the literal conversion of the embedded-pointer machinery faults at runtime (the generated
// promoted-field box treats its held nil *setting as a nil POINTER dereference, so even the
// `s.setting = lookup(...)` assignment panics). This hand-owned implementation parses $GODEBUG
// (comma-separated key=value, later entries overriding earlier ones, matching Go's backward parse)
// and serves every lookup from that snapshot, REPARSING whenever the variable's text changes —
// which is what Go's update hooks accomplish, and what `Value`'s own contract below promises. An
// unset or unlisted key yields "", Go's unset default. Not carried over: bisect stack-pattern
// values (the `value#pattern` suffix is stripped, enabling the setting unconditionally) and the
// runtime/metrics non-default counters (IncNonDefault is inert — there is no converted metrics
// consumer).

using System;
using System.Collections.Generic;

using godebugs = go.@internal.godebugs_package;

// Hand-owned native replacement of the converted godebug.go output — the converter skips
// regenerating a file that carries this marker, so a -stdlib reconvert preserves it (see
// containsManualConversionMarker).
[module: go.GoManualConversion]

namespace go.@internal;

partial class godebug_package {

// A Setting is a single setting in the $GODEBUG environment variable.
[GoType] partial struct Setting {
    internal @string name;
}

// New returns a new Setting for the $GODEBUG setting with the given name.
//
// GODEBUGs meant for use by end users must be listed in ../godebugs/table.go,
// which is used for generating and checking various documentation.
// If the name is not listed in that table, New will succeed but calling Value
// on the returned Setting will panic.
// To disable that panic for access to an undocumented setting,
// prefix the name with a #, as in godebug.New("#gofsystrace").
// The # is a signal to New but not part of the key used in $GODEBUG.
public static ж<Setting> New(@string name) {
    return Ꮡ(new Setting(name: name));
}

// Name returns the name of the setting.
[GoRecv] public static @string Name(this ref Setting s) {
    if (s.name != ""u8 && s.name[0] == (rune)'#') {
        return s.name[1..];
    }
    return s.name;
}

// Undocumented reports whether this is an undocumented setting.
[GoRecv] public static bool Undocumented(this ref Setting s) {
    return s.name != ""u8 && s.name[0] == (rune)'#';
}

// String returns a printable form for the setting: name=value.
public static @string String(this ж<Setting> Ꮡs) {
    ref var s = ref Ꮡs.Value;

    return s.Name() + "="u8 + Ꮡs.Value();
}

// IncNonDefault increments the non-default behavior counter
// associated with the given setting.
// This counter is exposed in the runtime/metrics value
// /godebug/non-default-behavior/<name>:events.
//
// go2cs: runtime/metrics plumbing with no converted consumer — inert.
public static void IncNonDefault(this ж<Setting> Ꮡs) {
}

// Value returns the current value for the GODEBUG setting s.
//
// Value maintains an internal cache that is synchronized
// with changes to the $GODEBUG environment variable,
// making Value efficient to call as frequently as needed.
// Clients should therefore typically not attempt their own
// caching of Value's result.
public static @string Value(this ж<Setting> Ꮡs) {
    ref var s = ref Ꮡs.Value;

    string name = s.Name();

    if (godebugs.Lookup(name) == nil && !s.Undocumented()) {
        throw panic("godebug: Value of name not listed in godebugs.All: " + s.name);
    }
    return settings().TryGetValue(name, out string? value) ? value : "";
}

// The current $GODEBUG snapshot, paired with the raw text it was parsed from. Go keeps its cache
// current through runtime update hooks that fire when os.Setenv rewrites $GODEBUG; with no such
// hook here, the raw text IS the invalidation token — cheap to fetch, and a change to it is
// exactly the event the hooks signal. Reading the variable and comparing is far cheaper than
// re-parsing it, so a process whose $GODEBUG never changes (the overwhelming majority) parses
// once, as the previous one-shot form did.
//
// A one-shot snapshot instead made `Value` unable to honor the contract stated above it, and
// silently: `t.Setenv("GODEBUG", …)` — how Go's own suites exercise a setting, and the only way a
// converted test can reach one — could not be observed once ANY setting had been read, which in a
// test binary is almost always before the test that sets it runs.
private static volatile Tuple<string, Dictionary<string, string>> s_settings =
    new("", new Dictionary<string, string>(StringComparer.Ordinal));

private static Dictionary<string, string> settings() {
    string raw = Environment.GetEnvironmentVariable("GODEBUG") ?? "";
    Tuple<string, Dictionary<string, string>> current = s_settings;

    if (string.Equals(current.Item1, raw, StringComparison.Ordinal))
        return current.Item2;

    Tuple<string, Dictionary<string, string>> parsed = new(raw, parseGodebugEnv(raw));

    // Last writer wins, and every writer parsed the same text it observed — so whichever snapshot
    // is published is self-consistent (the field is volatile, so the dictionary's contents are
    // visible to whoever reads the reference). A concurrent Setenv can leave a stale one behind,
    // exactly as it can race Go's update hook; the next lookup notices and reparses.
    s_settings = parsed;

    return parsed.Item2;
}

// Parses comma-separated key=value pairs, later entries overriding earlier ones (Go parses
// backward and pins the first hit — same winner). A `value#pattern` bisect suffix is stripped,
// keeping just the value.
private static Dictionary<string, string> parseGodebugEnv(string raw) {
    Dictionary<string, string> settings = new(StringComparer.Ordinal);

    foreach (string pair in raw.Split(',')) {
        int eq = pair.IndexOf('=');

        if (eq < 0)
            continue;

        string value = pair[(eq + 1)..];
        int hash = value.IndexOf('#');

        settings[pair[..eq]] = hash < 0 ? value : value[..hash];
    }

    return settings;
}

// go2cs: retained from the converted output because the package's GoImplement registration
// (package_info.cs) binds runtimeStderr to bisect.Writer; writes go to standard error directly
// (Go routes through the runtime's fd-2 write since it cannot import os).
[GoType] partial struct runtimeStderr {
}

internal static ж<runtimeStderr> Ꮡstderr = new StandardBox<runtimeStderr>(default(runtimeStderr));
internal static ref runtimeStderr stderr => ref Ꮡstderr.Value;

[GoRecv] internal static (nint, error) Write(this ref runtimeStderr _, slice<byte> b) {
    if (len(b) > 0) {
        System.Console.OpenStandardError().Write(b.ToSpan());
    }
    return (len(b), default!);
}

} // end godebug_package
