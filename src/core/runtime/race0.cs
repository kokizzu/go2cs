// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !race
// Dummy race detection API, used when not built with -race.
namespace go;

using @unsafe = unsafe_package;

partial class runtime_package {

internal const bool raceenabled = false;

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string raceˢ = "race"u8;

// Because raceenabled is false, none of these functions should be called.
internal static void raceReadObjectPC(ж<_type> Ꮡt, @unsafe.Pointer addr, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static void raceWriteObjectPC(ж<_type> Ꮡt, @unsafe.Pointer addr, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static (uintptr, uintptr) raceinit() {
    @throw(raceˢ);
    return (0, 0);
}

internal static void racefini() {
    @throw(raceˢ);
}

internal static uintptr raceproccreate() {
    @throw(raceˢ);
    return 0;
}

internal static void raceprocdestroy(uintptr ctx) {
    @throw(raceˢ);
}

internal static void racemapshadow(@unsafe.Pointer addr, uintptr size) {
    @throw(raceˢ);
}

internal static void racewritepc(@unsafe.Pointer addr, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static void racereadpc(@unsafe.Pointer addr, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static void racereadrangepc(@unsafe.Pointer addr, uintptr sz, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static void racewriterangepc(@unsafe.Pointer addr, uintptr sz, uintptr callerpc, uintptr pc) {
    @throw(raceˢ);
}

internal static void raceacquire(@unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void raceacquireg(ж<g> Ꮡgp, @unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void raceacquirectx(uintptr racectx, @unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racerelease(@unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racereleaseg(ж<g> Ꮡgp, @unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racereleaseacquire(@unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racereleaseacquireg(ж<g> Ꮡgp, @unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racereleasemerge(@unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racereleasemergeg(ж<g> Ꮡgp, @unsafe.Pointer addr) {
    @throw(raceˢ);
}

internal static void racefingo() {
    @throw(raceˢ);
}

internal static void racemalloc(@unsafe.Pointer Δp, uintptr sz) {
    @throw(raceˢ);
}

internal static void racefree(@unsafe.Pointer Δp, uintptr sz) {
    @throw(raceˢ);
}

internal static uintptr racegostart(uintptr pc) {
    @throw(raceˢ);
    return 0;
}

internal static void racegoend() {
    @throw(raceˢ);
}

internal static void racectxend(uintptr racectx) {
    @throw(raceˢ);
}

} // end runtime_package
