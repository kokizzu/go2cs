// Copyright 2009 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !mips && !mipsle && !mips64 && !mips64le && linux
namespace go;

partial class runtime_package {

/* 0 */
/* 1 */
/* 2 */
/* 3 */
/* 4 */
/* 5 */
/* 6 */
/* 7 */
/* 8 */
/* 9 */
/* 10 */
/* 11 */
/* 12 */
/* 13 */
/* 14 */
/* 15 */
/* 16 */
/* 17 */
/* 18 */
/* 19 */
/* 20 */
/* 21 */
/* 22 */
/* 23 */
/* 24 */
/* 25 */
/* 26 */
/* 27 */
/* 28 */
/* 29 */
/* 30 */
/* 31 */
/* 32 */
/* SIGCANCEL; see issue 6997 */
/* 33 */
/* SIGSETXID; see issues 3871, 9400, 12498 */
/* 34 */
/* musl SIGSYNCCALL; see issue 39343 */
/* 35 */
/* 36 */
/* 37 */
/* 38 */
/* 39 */
/* 40 */
/* 41 */
/* 42 */
/* 43 */
/* 44 */
/* 45 */
/* 46 */
/* 47 */
/* 48 */
/* 49 */
/* 50 */
/* 51 */
/* 52 */
/* 53 */
/* 54 */
/* 55 */
/* 56 */
/* 57 */
/* 58 */
/* 59 */
/* 60 */
/* 61 */
/* 62 */
/* 63 */
/* 64 */
internal static ж<array<sigTabT>> Ꮡsigtable = new StandardBox<array<sigTabT>>(new sigTabT[]{
    new(0, "SIGNONE: no trap"u8),
    new(_SigNotify + _SigKill, "SIGHUP: terminal line hangup"u8),
    new(_SigNotify + _SigKill, "SIGINT: interrupt"u8),
    new(_SigNotify + _SigThrow, "SIGQUIT: quit"u8),
    new(_SigThrow + _SigUnblock, "SIGILL: illegal instruction"u8),
    new(_SigThrow + _SigUnblock, "SIGTRAP: trace trap"u8),
    new(_SigNotify + _SigThrow, "SIGABRT: abort"u8),
    new(_SigPanic + _SigUnblock, "SIGBUS: bus error"u8),
    new(_SigPanic + _SigUnblock, "SIGFPE: floating-point exception"u8),
    new(0, "SIGKILL: kill"u8),
    new(_SigNotify, "SIGUSR1: user-defined signal 1"u8),
    new(_SigPanic + _SigUnblock, "SIGSEGV: segmentation violation"u8),
    new(_SigNotify, "SIGUSR2: user-defined signal 2"u8),
    new(_SigNotify, "SIGPIPE: write to broken pipe"u8),
    new(_SigNotify, "SIGALRM: alarm clock"u8),
    new(_SigNotify + _SigKill, "SIGTERM: termination"u8),
    new(_SigThrow + _SigUnblock, "SIGSTKFLT: stack fault"u8),
    new(_SigNotify + _SigUnblock + _SigIgn, "SIGCHLD: child status has changed"u8),
    new(_SigNotify + _SigDefault + _SigIgn, "SIGCONT: continue"u8),
    new(0, "SIGSTOP: stop, unblockable"u8),
    new(_SigNotify + _SigDefault + _SigIgn, "SIGTSTP: keyboard stop"u8),
    new(_SigNotify + _SigDefault + _SigIgn, "SIGTTIN: background read from tty"u8),
    new(_SigNotify + _SigDefault + _SigIgn, "SIGTTOU: background write to tty"u8),
    new(_SigNotify + _SigIgn, "SIGURG: urgent condition on socket"u8),
    new(_SigNotify, "SIGXCPU: cpu limit exceeded"u8),
    new(_SigNotify, "SIGXFSZ: file size limit exceeded"u8),
    new(_SigNotify, "SIGVTALRM: virtual alarm clock"u8),
    new(_SigNotify + _SigUnblock, "SIGPROF: profiling alarm clock"u8),
    new(_SigNotify + _SigIgn, "SIGWINCH: window size change"u8),
    new(_SigNotify, "SIGIO: i/o now possible"u8),
    new(_SigNotify, "SIGPWR: power failure restart"u8),
    new(_SigThrow, "SIGSYS: bad system call"u8),
    new(_SigSetStack + _SigUnblock, "signal 32"u8),
    new(_SigSetStack + _SigUnblock, "signal 33"u8),
    new(_SigSetStack + _SigUnblock, "signal 34"u8),
    new(_SigNotify, "signal 35"u8),
    new(_SigNotify, "signal 36"u8),
    new(_SigNotify, "signal 37"u8),
    new(_SigNotify, "signal 38"u8),
    new(_SigNotify, "signal 39"u8),
    new(_SigNotify, "signal 40"u8),
    new(_SigNotify, "signal 41"u8),
    new(_SigNotify, "signal 42"u8),
    new(_SigNotify, "signal 43"u8),
    new(_SigNotify, "signal 44"u8),
    new(_SigNotify, "signal 45"u8),
    new(_SigNotify, "signal 46"u8),
    new(_SigNotify, "signal 47"u8),
    new(_SigNotify, "signal 48"u8),
    new(_SigNotify, "signal 49"u8),
    new(_SigNotify, "signal 50"u8),
    new(_SigNotify, "signal 51"u8),
    new(_SigNotify, "signal 52"u8),
    new(_SigNotify, "signal 53"u8),
    new(_SigNotify, "signal 54"u8),
    new(_SigNotify, "signal 55"u8),
    new(_SigNotify, "signal 56"u8),
    new(_SigNotify, "signal 57"u8),
    new(_SigNotify, "signal 58"u8),
    new(_SigNotify, "signal 59"u8),
    new(_SigNotify, "signal 60"u8),
    new(_SigNotify, "signal 61"u8),
    new(_SigNotify, "signal 62"u8),
    new(_SigNotify, "signal 63"u8),
    new(_SigNotify, "signal 64"u8)
}.array());
internal static ref array<sigTabT> sigtable => ref Ꮡsigtable.Value;

} // end runtime_package
