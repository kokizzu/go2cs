// fieldlib is the FOREIGN package for ReflectFieldMetadata's PkgPath row: a defined type in the
// parent over Outer must report this package for the unexported field it did not declare.
package fieldlib

type Outer struct {
	Exported   int
	unexported int
}

// Keep the unexported field referenced so the package has a use for it.
func (o Outer) Sum() int { return o.Exported + o.unexported }
