// convBasicLit_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import "testing"

// TestStringLiteralNeedsByteArray locks the raw-byte-escape diversion: a Go interpreted string
// literal carrying an escape that denotes a byte >= 0x80 — `\xHH` OR `\NNN` octal — must take the
// byte-array-backed @string path, because a C# string/u8 literal would UTF-8-re-encode that byte
// into a multi-byte sequence and break @string byte indexing / len. Sub-0x80 escapes are ASCII-safe
// and keep the readable string form; the `\x` greedy-extension case has no octal counterpart.
func TestStringLiteralNeedsByteArray(t *testing.T) {
	cases := map[string]bool{
		// Octal escapes — Go's `\NNN` is EXACTLY three digits denoting ONE raw byte.
		`"\377"`:     true,  // 0xFF: as a char, U+00FF UTF-8-encodes to TWO bytes (0xC3 0xBF)
		`"\200"`:     true,  // 0x80: the low boundary of the diverted range
		`"\377\200"`: true,  // a run of high octal bytes
		`"a\377b"`:   true,  // high octal byte embedded in ASCII text
		`"\303\277"`: true,  // the UTF-8 bytes of ÿ spelled octally — still raw bytes, not one char
		`"\177"`:     false, // 0x7F: ASCII-safe, `` round-trips
		`"\101"`:     false, // 'A': ASCII-safe
		`"\000"`:     false, // NUL: ASCII-safe
		`"\101\102"`: false, // "AB": all sub-0x80
		`"\400"`:     false, // > 255: not a valid Go escape, Unquote fails (caller falls back)
		`"\\377"`:    false, // ESCAPED backslash then "377" — not an octal escape (parity)
		`"\37"`:      false, // only two digits: not a Go octal escape
		// Hex escapes — pre-existing behavior, unchanged.
		`"\xff"`:             true,  // high byte
		`"\x80"`:             true,  // low boundary
		`"\xdb50"`:           true,  // C#-greedy: folds to the lone surrogate U+DB50 (CS9026)
		`"\x00\x10\x01\x11"`: false, // image/jpeg's all-ASCII table, no greedy extension
		`"\x50\x4b"`:         false, // "PK" zip magic, ASCII
		`"\\xff"`:            false, // ESCAPED backslash then "xff" (parity)
		// No escapes at all — real UTF-8 characters round-trip through `u8` exactly.
		`"plain text"`: false,
		`"Michał"`:     false,
		`"白鵬翔"`:        false,
		`""`:           false,
	}

	for token, want := range cases {
		if got := stringLiteralNeedsByteArray(token); got != want {
			t.Errorf("stringLiteralNeedsByteArray(%s) = %v, want %v", token, got, want)
		}
	}
}
