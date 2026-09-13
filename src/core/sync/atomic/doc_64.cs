// Copyright 2023 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
//go:build !(386 || arm || mips || mipsle)
namespace go.sync;

partial class atomic_package {

// SwapInt64 atomically stores new into *addr and returns the previous *addr value.
// Consider using the more ergonomic and less error-prone [Int64.Swap] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial int64 /*old*/ SwapInt64(ж<int64> addr, int64 @new);

// SwapUint64 atomically stores new into *addr and returns the previous *addr value.
// Consider using the more ergonomic and less error-prone [Uint64.Swap] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial uint64 /*old*/ SwapUint64(ж<uint64> addr, uint64 @new);

// CompareAndSwapInt64 executes the compare-and-swap operation for an int64 value.
// Consider using the more ergonomic and less error-prone [Int64.CompareAndSwap] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial bool /*swapped*/ CompareAndSwapInt64(ж<int64> addr, int64 old, int64 @new);

// CompareAndSwapUint64 executes the compare-and-swap operation for a uint64 value.
// Consider using the more ergonomic and less error-prone [Uint64.CompareAndSwap] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial bool /*swapped*/ CompareAndSwapUint64(ж<uint64> addr, uint64 old, uint64 @new);

// AddInt64 atomically adds delta to *addr and returns the new value.
// Consider using the more ergonomic and less error-prone [Int64.Add] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial int64 /*new*/ AddInt64(ж<int64> addr, int64 delta);

// AddUint64 atomically adds delta to *addr and returns the new value.
// To subtract a signed positive constant value c from x, do AddUint64(&x, ^uint64(c-1)).
// In particular, to decrement x, do AddUint64(&x, ^uint64(0)).
// Consider using the more ergonomic and less error-prone [Uint64.Add] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial uint64 /*new*/ AddUint64(ж<uint64> addr, uint64 delta);

// AndInt64 atomically performs a bitwise AND operation on *addr using the bitmask provided as mask
// and returns the old value.
// Consider using the more ergonomic and less error-prone [Int64.And] instead.
//
//go:noescape
public static partial int64 /*old*/ AndInt64(ж<int64> addr, int64 mask);

// AndUint64 atomically performs a bitwise AND operation on *addr using the bitmask provided as mask
// and returns the old.
// Consider using the more ergonomic and less error-prone [Uint64.And] instead.
//
//go:noescape
public static partial uint64 /*old*/ AndUint64(ж<uint64> addr, uint64 mask);

// OrInt64 atomically performs a bitwise OR operation on *addr using the bitmask provided as mask
// and returns the old value.
// Consider using the more ergonomic and less error-prone [Int64.Or] instead.
//
//go:noescape
public static partial int64 /*old*/ OrInt64(ж<int64> addr, int64 mask);

// OrUint64 atomically performs a bitwise OR operation on *addr using the bitmask provided as mask
// and returns the old value.
// Consider using the more ergonomic and less error-prone [Uint64.Or] instead.
//
//go:noescape
public static partial uint64 /*old*/ OrUint64(ж<uint64> addr, uint64 mask);

// LoadInt64 atomically loads *addr.
// Consider using the more ergonomic and less error-prone [Int64.Load] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial int64 /*val*/ LoadInt64(ж<int64> addr);

// LoadUint64 atomically loads *addr.
// Consider using the more ergonomic and less error-prone [Uint64.Load] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial uint64 /*val*/ LoadUint64(ж<uint64> addr);

// StoreInt64 atomically stores val into *addr.
// Consider using the more ergonomic and less error-prone [Int64.Store] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial void StoreInt64(ж<int64> addr, int64 val);

// StoreUint64 atomically stores val into *addr.
// Consider using the more ergonomic and less error-prone [Uint64.Store] instead
// (particularly if you target 32-bit platforms; see the bugs section).
//
//go:noescape
public static partial void StoreUint64(ж<uint64> addr, uint64 val);

} // end atomic_package
