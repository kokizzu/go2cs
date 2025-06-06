// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package bisect can be used by compilers and other programs
// to serve as a target for the bisect debugging tool.
// See [golang.org/x/tools/cmd/bisect] for details about using the tool.
//
// To be a bisect target, allowing bisect to help determine which of a set of independent
// changes provokes a failure, a program needs to:
//
//  1. Define a way to accept a change pattern on its command line or in its environment.
//     The most common mechanism is a command-line flag.
//     The pattern can be passed to [New] to create a [Matcher], the compiled form of a pattern.
//
//  2. Assign each change a unique ID. One possibility is to use a sequence number,
//     but the most common mechanism is to hash some kind of identifying information
//     like the file and line number where the change might be applied.
//     [Hash] hashes its arguments to compute an ID.
//
//  3. Enable each change that the pattern says should be enabled.
//     The [Matcher.ShouldEnable] method answers this question for a given change ID.
//
//  4. Print a report identifying each change that the pattern says should be printed.
//     The [Matcher.ShouldPrint] method answers this question for a given change ID.
//     The report consists of one more lines on standard error or standard output
//     that contain a “match marker”. [Marker] returns the match marker for a given ID.
//     When bisect reports a change as causing the failure, it identifies the change
//     by printing the report lines with the match marker removed.
//
// # Example Usage
//
// A program starts by defining how it receives the pattern. In this example, we will assume a flag.
// The next step is to compile the pattern:
//
//	m, err := bisect.New(patternFlag)
//	if err != nil {
//		log.Fatal(err)
//	}
//
// Then, each time a potential change is considered, the program computes
// a change ID by hashing identifying information (source file and line, in this case)
// and then calls m.ShouldPrint and m.ShouldEnable to decide whether to
// print and enable the change, respectively. The two can return different values
// depending on whether bisect is trying to find a minimal set of changes to
// disable or to enable to provoke the failure.
//
// It is usually helpful to write a helper function that accepts the identifying information
// and then takes care of hashing, printing, and reporting whether the identified change
// should be enabled. For example, a helper for changes identified by a file and line number
// would be:
//
//	func ShouldEnable(file string, line int) {
//		h := bisect.Hash(file, line)
//		if m.ShouldPrint(h) {
//			fmt.Fprintf(os.Stderr, "%v %s:%d\n", bisect.Marker(h), file, line)
//		}
//		return m.ShouldEnable(h)
//	}
//
// Finally, note that New returns a nil Matcher when there is no pattern,
// meaning that the target is not running under bisect at all,
// so all changes should be enabled and none should be printed.
// In that common case, the computation of the hash can be avoided entirely
// by checking for m == nil first:
//
//	func ShouldEnable(file string, line int) bool {
//		if m == nil {
//			return true
//		}
//		h := bisect.Hash(file, line)
//		if m.ShouldPrint(h) {
//			fmt.Fprintf(os.Stderr, "%v %s:%d\n", bisect.Marker(h), file, line)
//		}
//		return m.ShouldEnable(h)
//	}
//
// When the identifying information is expensive to format, this code can call
// [Matcher.MarkerOnly] to find out whether short report lines containing only the
// marker are permitted for a given run. (Bisect permits such lines when it is
// still exploring the space of possible changes and will not be showing the
// output to the user.) If so, the client can choose to print only the marker:
//
//	func ShouldEnable(file string, line int) bool {
//		if m == nil {
//			return true
//		}
//		h := bisect.Hash(file, line)
//		if m.ShouldPrint(h) {
//			if m.MarkerOnly() {
//				bisect.PrintMarker(os.Stderr, h)
//			} else {
//				fmt.Fprintf(os.Stderr, "%v %s:%d\n", bisect.Marker(h), file, line)
//			}
//		}
//		return m.ShouldEnable(h)
//	}
//
// This specific helper – deciding whether to enable a change identified by
// file and line number and printing about the change when necessary – is
// provided by the [Matcher.FileLine] method.
//
// Another common usage is deciding whether to make a change in a function
// based on the caller's stack, to identify the specific calling contexts that the
// change breaks. The [Matcher.Stack] method takes care of obtaining the stack,
// printing it when necessary, and reporting whether to enable the change
// based on that stack.
//
// # Pattern Syntax
//
// Patterns are generated by the bisect tool and interpreted by [New].
// Users should not have to understand the patterns except when
// debugging a target's bisect support or debugging the bisect tool itself.
//
// The pattern syntax selecting a change is a sequence of bit strings
// separated by + and - operators. Each bit string denotes the set of
// changes with IDs ending in those bits, + is set addition, - is set subtraction,
// and the expression is evaluated in the usual left-to-right order.
// The special binary number “y” denotes the set of all changes,
// standing in for the empty bit string.
// In the expression, all the + operators must appear before all the - operators.
// A leading + adds to an empty set. A leading - subtracts from the set of all
// possible suffixes.
//
// For example:
//
//   - “01+10” and “+01+10” both denote the set of changes
//     with IDs ending with the bits 01 or 10.
//
//   - “01+10-1001” denotes the set of changes with IDs
//     ending with the bits 01 or 10, but excluding those ending in 1001.
//
//   - “-01-1000” and “y-01-1000 both denote the set of all changes
//     with IDs not ending in 01 nor 1000.
//
//   - “0+1-01+001” is not a valid pattern, because all the + operators do not
//     appear before all the - operators.
//
// In the syntaxes described so far, the pattern specifies the changes to
// enable and report. If a pattern is prefixed by a “!”, the meaning
// changes: the pattern specifies the changes to DISABLE and report. This
// mode of operation is needed when a program passes with all changes
// enabled but fails with no changes enabled. In this case, bisect
// searches for minimal sets of changes to disable.
// Put another way, the leading “!” inverts the result from [Matcher.ShouldEnable]
// but does not invert the result from [Matcher.ShouldPrint].
//
// As a convenience for manual debugging, “n” is an alias for “!y”,
// meaning to disable and report all changes.
//
// Finally, a leading “v” in the pattern indicates that the reports will be shown
// to the user of bisect to describe the changes involved in a failure.
// At the API level, the leading “v” causes [Matcher.Visible] to return true.
// See the next section for details.
//
// # Match Reports
//
// The target program must enable only those changed matched
// by the pattern, and it must print a match report for each such change.
// A match report consists of one or more lines of text that will be
// printed by the bisect tool to describe a change implicated in causing
// a failure. Each line in the report for a given change must contain a
// match marker with that change ID, as returned by [Marker].
// The markers are elided when displaying the lines to the user.
//
// A match marker has the form “[bisect-match 0x1234]” where
// 0x1234 is the change ID in hexadecimal.
// An alternate form is “[bisect-match 010101]”, giving the change ID in binary.
//
// When [Matcher.Visible] returns false, the match reports are only
// being processed by bisect to learn the set of enabled changes,
// not shown to the user, meaning that each report can be a match
// marker on a line by itself, eliding the usual textual description.
// When the textual description is expensive to compute,
// checking [Matcher.Visible] can help the avoid that expense
// in most runs.
namespace go.@internal;

using runtime = runtime_package;
using sync = sync_package;
using atomic = sync.atomic_package;
using sync;
using ꓸꓸꓸany = Span<any>;

partial class bisect_package {

// New creates and returns a new Matcher implementing the given pattern.
// The pattern syntax is defined in the package doc comment.
//
// In addition to the pattern syntax syntax, New("") returns nil, nil.
// The nil *Matcher is valid for use: it returns true from ShouldEnable
// and false from ShouldPrint for all changes. Callers can avoid calling
// [Hash], [Matcher.ShouldEnable], and [Matcher.ShouldPrint] entirely
// when they recognize the nil Matcher.
public static (ж<Matcher>, error) New(@string pattern) {
    if (pattern == ""u8) {
        return (default!, default!);
    }
    var m = @new<Matcher>();
    @string p = pattern;
    // Special case for leading 'q' so that 'qn' quietly disables, e.g. fmahash=qn to disable fma
    // Any instance of 'v' disables 'q'.
    if (len(p) > 0 && p[0] == (rune)'q') {
        m.val.quiet = true;
        p = p[1..];
        if (p == ""u8) {
            return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
        }
    }
    // Allow multiple v, so that “bisect cmd vPATTERN” can force verbose all the time.
    while (len(p) > 0 && p[0] == (rune)'v') {
        m.val.verbose = true;
        m.val.quiet = false;
        p = p[1..];
        if (p == ""u8) {
            return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
        }
    }
    // Allow multiple !, each negating the last, so that “bisect cmd !PATTERN” works
    // even when bisect chooses to add its own !.
    m.val.enable = true;
    while (len(p) > 0 && p[0] == (rune)'!') {
        m.val.enable = !(~m).enable;
        p = p[1..];
        if (p == ""u8) {
            return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
        }
    }
    if (p == "n"u8) {
        // n is an alias for !y.
        m.val.enable = !(~m).enable;
        p = "y"u8;
    }
    // Parse actual pattern syntax.
    var result = true;
    var bits = ((uint64)0);
    nint start = 0;
    nint wid = 1;
    // 1-bit (binary); sometimes 4-bit (hex)
    for (nint i = 0; i <= len(p); i++) {
        // Imagine a trailing - at the end of the pattern to flush final suffix
        var c = ((byte)(rune)'-');
        if (i < len(p)) {
            c = p[i];
        }
        if (i == start && wid == 1 && c == (rune)'x') {
            // leading x for hex
            start = i + 1;
            wid = 4;
            continue;
        }
        var exprᴛ1 = c;
        var matchᴛ1 = false;
        { /* default: */
            return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
        }
        if (exprᴛ1 is (rune)'2' or (rune)'3' or (rune)'4' or (rune)'5' or (rune)'6' or (rune)'7' or (rune)'8' or (rune)'9') { matchᴛ1 = true;
            if (wid != 4) {
                return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
            }
            fallthrough = true;
        }
        if (fallthrough || !matchᴛ1 && (exprᴛ1 is (rune)'0' or (rune)'1')) { matchᴛ1 = true;
            bits <<= (nint)(wid);
            bits |= (uint64)(((uint64)(c - (rune)'0')));
        }
        else if (exprᴛ1 is (rune)'a' or (rune)'b' or (rune)'c' or (rune)'d' or (rune)'e' or (rune)'f' or (rune)'A' or (rune)'B' or (rune)'C' or (rune)'D' or (rune)'E' or (rune)'F') { matchᴛ1 = true;
            if (wid != 4) {
                return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
            }
            bits <<= (UntypedInt)(4);
            bits |= (uint64)(((uint64)((byte)(c & ~32) - (rune)'A' + 10)));
        }
        else if (exprᴛ1 is (rune)'y') {
            if (i + 1 < len(p) && (p[i + 1] == (rune)'0' || p[i + 1] == (rune)'1')) {
                return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
            }
            bits = 0;
        }
        else if (exprᴛ1 is (rune)'+' or (rune)'-') { matchᴛ1 = true;
            if (c == (rune)'+' && result == false) {
                // Have already seen a -. Should be - from here on.
                return (default!, new parseError("invalid pattern syntax (+ after -): "u8 + pattern));
            }
            if (i > 0){
                nint n = (i - start) * wid;
                if (n > 64) {
                    return (default!, new parseError("pattern bits too long: "u8 + pattern));
                }
                if (n <= 0) {
                    return (default!, new parseError("invalid pattern syntax: "u8 + pattern));
                }
                if (p[start] == (rune)'y') {
                    n = 0;
                }
                var mask = ((uint64)1) << (int)(n) - 1;
                m.val.list = append((~m).list, new cond(mask, bits, result));
            } else 
            if (c == (rune)'-') {
                // leading - subtracts from complete set
                m.val.list = append((~m).list, new cond(0, 0, true));
            }
            bits = 0;
            result = c == (rune)'+';
            start = i + 1;
            wid = 1;
        }

    }
    return (m, default!);
}

// A Matcher is the parsed, compiled form of a PATTERN string.
// The nil *Matcher is valid: it has all changes enabled but none reported.
[GoType] partial struct Matcher {
    internal bool verbose;   // annotate reporting with human-helpful information
    internal bool quiet;   // disables all reporting.  reset if verbose is true. use case is -d=fmahash=qn
    internal bool enable;   // when true, list is for “enable and report” (when false, “disable and report”)
    internal slice<cond> list; // conditions; later ones win over earlier ones
    internal sync.atomic_package.Pointer dedup;
}

// A cond is a single condition in the matcher.
// Given an input id, if id&mask == bits, return the result.
[GoType] partial struct cond {
    internal uint64 mask;
    internal uint64 bits;
    internal bool result;
}

// MarkerOnly reports whether it is okay to print only the marker for
// a given change, omitting the identifying information.
// MarkerOnly returns true when bisect is using the printed reports
// only for an intermediate search step, not for showing to users.
[GoRecv] public static bool MarkerOnly(this ref Matcher m) {
    return !m.verbose;
}

// ShouldEnable reports whether the change with the given id should be enabled.
[GoRecv] public static bool ShouldEnable(this ref Matcher m, uint64 id) {
    if (m == nil) {
        return true;
    }
    return m.matchResult(id) == m.enable;
}

// ShouldPrint reports whether to print identifying information about the change with the given id.
[GoRecv] public static bool ShouldPrint(this ref Matcher m, uint64 id) {
    if (m == nil || m.quiet) {
        return false;
    }
    return m.matchResult(id);
}

// matchResult returns the result from the first condition that matches id.
[GoRecv] internal static bool matchResult(this ref Matcher m, uint64 id) {
    for (nint i = len(m.list) - 1; i >= 0; i--) {
        var c = Ꮡ(m.list[i]);
        if ((uint64)(id & (~c).mask) == (~c).bits) {
            return (~c).result;
        }
    }
    return false;
}

// FileLine reports whether the change identified by file and line should be enabled.
// If the change should be printed, FileLine prints a one-line report to w.
[GoRecv] public static bool FileLine(this ref Matcher m, Writer w, @string file, nint line) {
    if (m == nil) {
        return true;
    }
    return m.fileLine(w, file, line);
}

// fileLine does the real work for FileLine.
// This lets FileLine's body handle m == nil and potentially be inlined.
[GoRecv] internal static bool fileLine(this ref Matcher m, Writer w, @string file, nint line) {
    var h = Hash(file, line);
    if (m.ShouldPrint(h)) {
        if (m.MarkerOnly()){
            PrintMarker(w, h);
        } else {
            printFileLine(w, h, file, line);
        }
    }
    return m.ShouldEnable(h);
}

// printFileLine prints a non-marker-only report for file:line to w.
internal static error printFileLine(Writer w, uint64 h, @string file, nint line) {
    static readonly UntypedInt markerLen = 40; // overestimate
    var b = new slice<byte>(0, markerLen + len(file) + 24);
    b = AppendMarker(b, h);
    b = appendFileLine(b, file, line);
    b = append(b, (rune)'\n');
    var (_, err) = w.Write(b);
    return err;
}

// appendFileLine appends file:line to dst, returning the extended slice.
internal static slice<byte> appendFileLine(slice<byte> dst, @string file, nint line) {
    dst = append(dst, file.ꓸꓸꓸ);
    dst = append(dst, (rune)':');
    nuint u = ((nuint)line);
    if (line < 0) {
        dst = append(dst, (rune)'-');
        u = -u;
    }
    array<byte> buf = new(24);
    nint i = len(buf);
    while (i == len(buf) || u > 0) {
        i--;
        buf[i] = (rune)'0' + ((byte)(u % 10));
        u /= 10;
    }
    dst = append(dst, buf[(int)(i)..].ꓸꓸꓸ);
    return dst;
}

// MatchStack assigns the current call stack a change ID.
// If the stack should be printed, MatchStack prints it.
// Then MatchStack reports whether a change at the current call stack should be enabled.
[GoRecv] public static bool Stack(this ref Matcher m, Writer w) {
    if (m == nil) {
        return true;
    }
    return m.stack(w);
}

// stack does the real work for Stack.
// This lets stack's body handle m == nil and potentially be inlined.
[GoRecv] internal static bool stack(this ref Matcher m, Writer w) {
    static readonly UntypedInt maxStack = 16;
    array<uintptr> stk = new(16); /* maxStack */
    nint n = runtime.Callers(2, stk[..]);
    // caller #2 is not for printing; need it to normalize PCs if ASLR.
    if (n <= 1) {
        return false;
    }
    var @base = stk[0];
    // normalize PCs
    foreach (var (i, _) in stk[..(int)(n)]) {
        stk[i] -= @base;
    }
    var h = Hash(stk[..(int)(n)]);
    if (m.ShouldPrint(h)) {
        ж<dedup> d = default!;
        while (ᐧ) {
            d = m.dedup.Load();
            if (d != nil) {
                break;
            }
            d = @new<dedup>();
            if (m.dedup.CompareAndSwap(nil, d)) {
                break;
            }
        }
        if (m.MarkerOnly()){
            if (!d.seenLossy(h)) {
                PrintMarker(w, h);
            }
        } else {
            if (!d.seen(h)) {
                // Restore PCs in stack for printing
                foreach (var (i, _) in stk[..(int)(n)]) {
                    stk[i] += @base;
                }
                printStack(w, h, stk[1..(int)(n)]);
            }
        }
    }
    return m.ShouldEnable(h);
}

// Writer is the same interface as io.Writer.
// It is duplicated here to avoid importing io.
[GoType] partial interface Writer {
    (nint, error) Write(slice<byte> _);
}

// PrintMarker prints to w a one-line report containing only the marker for h.
// It is appropriate to use when [Matcher.ShouldPrint] and [Matcher.MarkerOnly] both return true.
public static error PrintMarker(Writer w, uint64 h) {
    array<byte> buf = new(50);
    var b = AppendMarker(buf[..0], h);
    b = append(b, (rune)'\n');
    var (_, err) = w.Write(b);
    return err;
}

// printStack prints to w a multi-line report containing a formatting of the call stack stk,
// with each line preceded by the marker for h.
internal static error printStack(Writer w, uint64 h, slice<uintptr> stk) {
    var buf = new slice<byte>(0, 2048);
    array<byte> prefixBuf = new(100);
    var prefix = AppendMarker(prefixBuf[..0], h);
    var frames = runtime.CallersFrames(stk);
    while (ᐧ) {
        var (f, more) = frames.Next();
        buf = append(buf, prefix.ꓸꓸꓸ);
        buf = append(buf, f.Function.ꓸꓸꓸ);
        buf = append(buf, "()\n"u8.ꓸꓸꓸ);
        buf = append(buf, prefix.ꓸꓸꓸ);
        buf = append(buf, (rune)'\t');
        buf = appendFileLine(buf, f.File, f.Line);
        buf = append(buf, (rune)'\n');
        if (!more) {
            break;
        }
    }
    buf = append(buf, prefix.ꓸꓸꓸ);
    buf = append(buf, (rune)'\n');
    var (_, err) = w.Write(buf);
    return err;
}

// Marker returns the match marker text to use on any line reporting details
// about a match of the given ID.
// It always returns the hexadecimal format.
public static @string Marker(uint64 id) {
    return ((@string)AppendMarker(default!, id));
}

// AppendMarker is like [Marker] but appends the marker to dst.
public static slice<byte> AppendMarker(slice<byte> dst, uint64 id) {
    @string prefix = "[bisect-match 0x"u8;
    array<byte> buf = new(33); /* len(prefix) + 16 + 1 */
    copy(buf[..], prefix);
    for (nint i = 0; i < 16; i++) {
        buf[len(prefix) + i] = "0123456789abcdef"u8[id >> (int)(60)];
        id <<= (UntypedInt)(4);
    }
    buf[len(prefix) + 16] = (rune)']';
    return append(dst, buf[..].ꓸꓸꓸ);
}

// CutMarker finds the first match marker in line and removes it,
// returning the shortened line (with the marker removed),
// the ID from the match marker,
// and whether a marker was found at all.
// If there is no marker, CutMarker returns line, 0, false.
public static (@string @short, uint64 id, bool ok) CutMarker(@string line) {
    @string @short = default!;
    uint64 id = default!;
    bool ok = default!;

    // Find first instance of prefix.
    @string prefix = "[bisect-match "u8;
    nint i = 0;
    for (; ᐧ ; i++) {
        if (i >= len(line) - len(prefix)) {
            return (line, 0, false);
        }
        if (line[i] == (rune)'[' && line[(int)(i)..(int)(i + len(prefix))] == prefix) {
            break;
        }
    }
    // Scan to ].
    nint j = i + len(prefix);
    while (j < len(line) && line[j] != (rune)']') {
        j++;
    }
    if (j >= len(line)) {
        return (line, 0, false);
    }
    // Parse id.
    @string idstr = line[(int)(i + len(prefix))..(int)(j)];
    if (len(idstr) >= 3 && idstr[..2] == "0x"){
        // parse hex
        if (len(idstr) > 2 + 16) {
            // max 0x + 16 digits
            return (line, 0, false);
        }
        for (nint iΔ1 = 2; iΔ1 < len(idstr); iΔ1++) {
            id <<= (UntypedInt)(4);
            {
                var c = idstr[i];
                switch (ᐧ) {
                case {} when (rune)'0' <= c && c <= (rune)'9': {
                    id |= (uint64)(((uint64)(c - (rune)'0')));
                    break;
                }
                case {} when (rune)'a' <= c && c <= (rune)'f': {
                    id |= (uint64)(((uint64)(c - (rune)'a' + 10)));
                    break;
                }
                case {} when (rune)'A' <= c && c <= (rune)'F': {
                    id |= (uint64)(((uint64)(c - (rune)'A' + 10)));
                    break;
                }}
            }

        }
    } else {
        if (idstr == ""u8 || len(idstr) > 64) {
            // min 1 digit, max 64 digits
            return (line, 0, false);
        }
        // parse binary
        for (nint iΔ2 = 0; iΔ2 < len(idstr); iΔ2++) {
            id <<= (UntypedInt)(1);
            {
                var c = idstr[i];
                switch (c) {
                default: {
                    return (line, 0, false);
                }
                case (rune)'0' or (rune)'1': {
                    id |= (uint64)(((uint64)(c - (rune)'0')));
                    break;
                }}
            }

        }
    }
    // Construct shortened line.
    // Remove at most one space from around the marker,
    // so that "foo [marker] bar" shortens to "foo bar".
    j++;
    // skip ]
    if (i > 0 && line[i - 1] == (rune)' '){
        i--;
    } else 
    if (j < len(line) && line[j] == (rune)' ') {
        j++;
    }
    @short = line[..(int)(i)] + line[(int)(j)..];
    return (@short, id, true);
}

// Hash computes a hash of the data arguments,
// each of which must be of type string, byte, int, uint, int32, uint32, int64, uint64, uintptr, or a slice of one of those types.
public static uint64 Hash(params ꓸꓸꓸany dataʗp) {
    var data = dataʗp.slice();

    var h = offset64;
    foreach (var (_, v) in data) {
        switch (v.type()) {
        default: {
            var v = v.type();
            throw panic("bisect.Hash: unexpected argument type");
            break;
        }
        case @string v: {
            h = fnvString(h, // Note: Not printing the type, because reflect.ValueOf(v)
 // would make the interfaces prepared by the caller escape
 // and therefore allocate. This way, Hash(file, line) runs
 // without any allocation. It should be clear from the
 // source code calling Hash what the bad argument was.
 v);
            break;
        }
        case byte v: {
            h = fnv(h, v);
            break;
        }
        case nint v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case int32 v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case nuint v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case uint32 v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case int32 v: {
            h = fnvUint32(h, ((uint32)v));
            break;
        }
        case uint32 v: {
            h = fnvUint32(h, v);
            break;
        }
        case int64 v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case uint64 v: {
            h = fnvUint64(h, v);
            break;
        }
        case uintptr v: {
            h = fnvUint64(h, ((uint64)v));
            break;
        }
        case slice<@string> v: {
            foreach (var (_, x) in v) {
                h = fnvString(h, x);
            }
            break;
        }
        case slice<byte> v: {
            foreach (var (_, x) in v) {
                h = fnv(h, x);
            }
            break;
        }
        case slice<nint> v: {
            foreach (var (_, x) in v) {
                h = fnvUint64(h, ((uint64)x));
            }
            break;
        }
        case slice<nuint> v: {
            foreach (var (_, x) in v) {
                h = fnvUint64(h, ((uint64)x));
            }
            break;
        }
        case slice<int32> v: {
            foreach (var (_, x) in v) {
                h = fnvUint32(h, ((uint32)x));
            }
            break;
        }
        case slice<uint32> v: {
            foreach (var (_, x) in v) {
                h = fnvUint32(h, x);
            }
            break;
        }
        case slice<int64> v: {
            foreach (var (_, x) in v) {
                h = fnvUint64(h, ((uint64)x));
            }
            break;
        }
        case slice<uint64> v: {
            foreach (var (_, x) in v) {
                h = fnvUint64(h, x);
            }
            break;
        }
        case slice<uintptr> v: {
            foreach (var (_, x) in v) {
                h = fnvUint64(h, ((uint64)x));
            }
            break;
        }}
    }
    return h;
}

// Trivial error implementation, here to avoid importing errors.

// parseError is a trivial error implementation,
// defined here to avoid importing errors.
[GoType] partial struct parseError {
    internal @string text;
}

[GoRecv] internal static @string Error(this ref parseError e) {
    return e.text;
}

// FNV-1a implementation. See Go's hash/fnv/fnv.go.
// Copied here for simplicity (can handle integers more directly)
// and to avoid importing hash/fnv.
internal const uint64 offset64 = 14695981039346656037;
internal const uint64 prime64 = 1099511628211;

internal static uint64 fnv(uint64 h, byte x) {
    h ^= (uint64)(((uint64)x));
    h *= prime64;
    return h;
}

internal static uint64 fnvString(uint64 h, @string x) {
    for (nint i = 0; i < len(x); i++) {
        h ^= (uint64)(((uint64)x[i]));
        h *= prime64;
    }
    return h;
}

internal static uint64 fnvUint64(uint64 h, uint64 x) {
    for (nint i = 0; i < 8; i++) {
        h ^= (uint64)((uint64)(x & 255));
        x >>= (UntypedInt)(8);
        h *= prime64;
    }
    return h;
}

internal static uint64 fnvUint32(uint64 h, uint32 x) {
    for (nint i = 0; i < 4; i++) {
        h ^= (uint64)(((uint64)((uint32)(x & 255))));
        x >>= (UntypedInt)(8);
        h *= prime64;
    }
    return h;
}

// A dedup is a deduplicator for call stacks, so that we only print
// a report for new call stacks, not for call stacks we've already
// reported.
//
// It has two modes: an approximate but lock-free mode that
// may still emit some duplicates, and a precise mode that uses
// a lock and never emits duplicates.
[GoType] partial struct dedup {
    // 128-entry 4-way, lossy cache for seenLossy
    internal array<array<uint64>> recent = new(128);
    // complete history for seen
    internal sync_package.Mutex mu;
    internal map<uint64, bool> m;
}

// seen records that h has now been seen and reports whether it was seen before.
// When seen returns false, the caller is expected to print a report for h.
[GoRecv] internal static bool seen(this ref dedup d, uint64 h) {
    d.mu.Lock();
    if (d.m == default!) {
        d.m = new map<uint64, bool>();
    }
    var seen = d.m[h];
    d.m[h] = true;
    d.mu.Unlock();
    return seen;
}

// seenLossy is a variant of seen that avoids a lock by using a cache of recently seen hashes.
// Each cache entry is N-way set-associative: h can appear in any of the slots.
// If h does not appear in any of them, then it is inserted into a random slot,
// overwriting whatever was there before.
[GoRecv] internal static bool seenLossy(this ref dedup d, uint64 h) {
    var cache = Ꮡ(d.recent[((nuint)h) % ((nuint)len(d.recent))]);
    ref var i = ref heap<nint>(out var Ꮡi);
    for (i = 0; i < len(cache); i++) {
        if (atomic.LoadUint64(Ꮡ(cache.val[i])) == h) {
            return true;
        }
    }
    // Compute index in set to evict as hash of current set.
    ref var ch = ref heap<uint64>(out var Ꮡch);
    ch = offset64;
    foreach (var (_, x) in cache.val) {
        ch = fnvUint64(ch, x);
    }
    atomic.StoreUint64(Ꮡ(cache.val[((nuint)ch) % ((nuint)len(cache))]), h);
    return false;
}

} // end bisect_package
