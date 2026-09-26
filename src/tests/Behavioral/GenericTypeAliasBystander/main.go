package main

import (
	"fmt"

	"GenericTypeAliasLib"
)

// A consumer that imports the package declaring generic aliases and never names one: nothing the
// package publishes about them may break it.
func main() {
	b := GenericTypeAliasLib.NewBox(41)
	b.Set(b.Get() + 1)
	fmt.Println(b.Get(), GenericTypeAliasLib.Label("bystander"))
}
