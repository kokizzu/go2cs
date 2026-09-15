// rootGoTypeDescriptor_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.

package main

import "testing"

// TestRootGoTypeDescriptor pins the [GoType] descriptor rooting rule (H7 red 1,
// crypto/internal/fips140deps/godebug): every package-qualified reference is rooted at global::go., in every
// descriptor shape a writer emits, and nothing else moves. Each case is also re-applied to its own output, so
// the rule is proven idempotent on the already-rooted form the -tests bridge writes.
func TestRootGoTypeDescriptor(t *testing.T) {
	cases := []struct{ in, want string }{
		// the red: a first segment that an enclosing namespace's sibling captures
		{"@internal.godebug_package.Setting", "global::go.@internal.godebug_package.Setting"},
		// a root-level package, and a go/* package whose leading `go.` is a PATH segment, not the root
		{"time_package.Duration", "global::go.time_package.Duration"},
		{"go.token_package.ΔPos", "global::go.go.token_package.ΔPos"},
		// every descriptor shape that can carry a reference
		{"[]io.fs_package.FileInfo", "[]global::go.io.fs_package.FileInfo"},
		{"[1021]sync.atomic_package.Pointer<cacheEntry<K, V>>", "[1021]global::go.sync.atomic_package.Pointer<cacheEntry<K, V>>"},
		{"map[time_package.Time, sync.atomic_package.Uint32]", "map[global::go.time_package.Time, global::go.sync.atomic_package.Uint32]"},
		{"chan time_package.Time", "chan global::go.time_package.Time"},
		{"ж<time_package.Time>", "ж<global::go.time_package.Time>"},
		{"num:time_package.Duration", "num:global::go.time_package.Duration"},
		{"Pointer<time_package.Time, sync_package.Mutex>", "Pointer<global::go.time_package.Time, global::go.sync_package.Mutex>"},
		// nothing package-qualified: unchanged
		{"pageBits", "pageBits"},
		{"num:nint", "num:nint"},
		{"dyn", "dyn"},
		{"@string", "@string"},
		{"[]ж<ΔError>", "[]ж<ΔError>"},
		{"cacheEntry<K, V>", "cacheEntry<K, V>"},
		// already rooted: unchanged
		{"global::go.net.http_package.ΔHeader", "global::go.net.http_package.ΔHeader"},
	}

	for _, c := range cases {
		got := rootGoTypeDescriptor(c.in)

		if got != c.want {
			t.Errorf("rootGoTypeDescriptor(%q) = %q, want %q", c.in, got, c.want)
		}

		if again := rootGoTypeDescriptor(got); again != got {
			t.Errorf("rootGoTypeDescriptor is not idempotent on %q: %q", got, again)
		}
	}
}
