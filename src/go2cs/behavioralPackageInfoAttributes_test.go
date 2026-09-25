// behavioralPackageInfoAttributes_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// SPDX-License-Identifier: AGPL-3.0-only
// Use of this source code is governed by the GNU Affero General Public License
// version 3 only, which can be found in the LICENSE file.
// Additional permission for emitted output: see LICENSE-EXCEPTION (AGPL section 7).

package main

import (
	"os"
	"path/filepath"
	"sort"
	"strings"
	"testing"
)

// TestBehavioralPackageInfoCarriesNoDuplicateAttribute guards the class of a committed behavioral
// package_info.cs that carries one attribute line twice. [GoTestMatchingConsoleOutput] is not
// AllowMultiple, so a repeat is CS0579 in go2cs.slnx, which nothing routinely builds. The converter
// merges an existing package_info.cs, so re-emitting a project KEEPS a hand-added attribute, and adding
// it again by hand duplicates it: LocalStringConstHoist at claude/c2-arm-c 2d068bca59, found by R's
// TRAIN B rehearsal (ledger 12833683e1). An IDENTICAL attribute line never has a reason to repeat,
// whatever its arguments.
func TestBehavioralPackageInfoCarriesNoDuplicateAttribute(t *testing.T) {
	files, err := filepath.Glob(filepath.Join("..", "tests", "Behavioral", "*", "package_info.cs"))

	if err != nil {
		t.Fatal(err)
	}

	if len(files) == 0 {
		t.Fatal("no behavioral package_info.cs found: the guard would pass vacuously")
	}

	sort.Strings(files)

	for _, file := range files {
		contents, err := os.ReadFile(file)

		if err != nil {
			t.Fatal(err)
		}

		seen := map[string]int{}

		for i, line := range strings.Split(strings.ReplaceAll(string(contents), "\r\n", "\n"), "\n") {
			trimmed := strings.TrimSpace(line)

			if !strings.HasPrefix(trimmed, "[") {
				continue
			}

			if first, repeated := seen[trimmed]; repeated {
				t.Errorf("%s:%d repeats the attribute line %q first seen at line %d (CS0579 when the attribute is not AllowMultiple)", file, i+1, trimmed, first)
				continue
			}

			seen[trimmed] = i + 1
		}
	}
}
