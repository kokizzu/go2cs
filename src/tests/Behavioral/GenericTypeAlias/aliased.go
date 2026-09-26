package main

import (
	"fmt"

	ga "GenericTypeAliasLib"
)

// The same aliases through an ALIASED import: the unaliased target takes this file's import alias.
func aliasedImport() {
	var a ga.Alias[string] = ga.NewBox("g")
	var l ga.List[ga.Alias[int]] = ga.List[ga.Alias[int]]{{V: 1}, {V: 2}}
	var s ga.Set[int] = ga.Set[int]{3: {}}
	fmt.Println("aliased:", a.Get(), len(l), l[1].Get(), len(s))
}
