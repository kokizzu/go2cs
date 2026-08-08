// typeAccessibilityInterface_test.go - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

package main

import (
	"go/types"
	"testing"
)

// publicizedTypeNames renders the publicize pass's result as a lookup of simple type names, so a
// test can assert membership without depending on object identity.
func publicizedTypeNames() map[string]bool {
	names := map[string]bool{}

	for obj := range packagePublicizedTypes {
		names[obj.Name()] = true
	}

	return names
}

// A C# interface member carries NO access modifier (visitInterfaceType emits the bare signature)
// and is therefore implicitly PUBLIC — Go's case convention does not survive into the emitted
// surface. So an unexported named type in an unexported interface method's signature is still
// exposed by a public interface, and must be publicized or the member is more accessible than its
// own result type (CS0050) / parameter type (CS0051).
//
// syscall's `Sockaddr` is the archetype: `sockaddr() (unsafe.Pointer, _Socklen, error)` is
// deliberately unexported so only the package can implement the interface, yet the emitted member
// returns the unexported `_Socklen`. Windows spells the same method with `int32`, which is why the
// corpus only ever saw this on the unix flavors.
//
// The NEGATIVE control is the other half of the rule and is what keeps the fix from being a blanket
// "publicize every signature": a CONCRETE method's emitted accessibility DOES track Go exportedness
// (an unexported method emits `internal static … peek(this ж<Conn>)`), so it exposes nothing public
// and its signature types must stay internal. Dropping the exportedness gate outright — rather than
// dropping it only where the emitted member is unconditionally public — would publicize `hidden`
// too, and that assertion fails.
func TestInterfaceMemberSignatureTypesArePublicized(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/access\n\ngo 1.23\n",
		"access.go": `package access

type _Socklen uint32

type sockLen2 uint32

type hidden struct{}

type alsoHidden struct{}

// Sockaddr's members emit as public C# interface members whatever the Go case.
type Sockaddr interface {
	sockaddr() (_Socklen, error)
	setLen(l sockLen2)
}

// Conn's unexported method emits internal, so its signature types stay internal.
type Conn struct{}

func (c Conn) peek() hidden { return hidden{} }

func (c Conn) poke(h alsoHidden) {}
`,
	})

	production := loadProductionForDir(t, dir)

	resetPackageState(production)
	packagePublicizedTypes = nil
	packagePublicizedLiftedTypes = nil

	collectPublicizedTypes(production.Types)

	publicized := publicizedTypeNames()

	// The RESULT type of an unexported interface method — the syscall `_Socklen` shape.
	if !publicized["_Socklen"] {
		t.Fatalf("an unexported interface method's RESULT type must be publicized (a C# interface member is implicitly public); publicized: %v", publicized)
	}

	// …and its PARAMETER type, the CS0051 half of the same rule.
	if !publicized["sockLen2"] {
		t.Fatalf("an unexported interface method's PARAMETER type must be publicized; publicized: %v", publicized)
	}

	// Negative control: a concrete unexported method emits `internal` and exposes nothing public.
	if publicized["hidden"] {
		t.Fatalf("a concrete UNEXPORTED method's result type must NOT be publicized — its emitted member is internal; publicized: %v", publicized)
	}

	if publicized["alsoHidden"] {
		t.Fatalf("a concrete UNEXPORTED method's parameter type must NOT be publicized; publicized: %v", publicized)
	}
}

// An INTERNAL interface's members are implicitly public too, but their effective accessibility is
// bounded by the containing type, so an internal signature type is legal there and nothing needs
// lifting. The pass reaches interface methods only through packagePublicizedTypes and the exported
// seed loop, so an unexported, never-publicized interface must leave its signature types alone —
// this pins that the fix widened the rule for PUBLIC surfaces only.
func TestUnexportedInterfaceDoesNotPublicizeSignatureTypes(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/internalonly\n\ngo 1.23\n",
		"internalonly.go": `package internalonly

type quiet uint32

type sealed interface {
	sockaddr() (quiet, error)
}

var _ sealed = nil
`,
	})

	production := loadProductionForDir(t, dir)

	resetPackageState(production)
	packagePublicizedTypes = nil
	packagePublicizedLiftedTypes = nil

	collectPublicizedTypes(production.Types)

	if publicizedTypeNames()["quiet"] {
		t.Fatalf("an unexported interface's signature types must stay internal; publicized: %v", publicizedTypeNames())
	}
}

// The cascade must carry the rule through: an exported type whose exported method returns an
// unexported INTERFACE publicizes that interface, and the now-public interface's own unexported
// members then expose their signature types. This is the fixpoint half — `collectSignatureTypes`
// seeds the interface, `cascadePublicizedMethodTypes` re-walks it, and only then does the widened
// interface gate see it.
func TestPublicizedInterfaceCascadesToItsMemberSignatureTypes(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/cascade\n\ngo 1.23\n",
		"cascade.go": `package cascade

type deepLen uint32

type sealed interface {
	sockaddr() (deepLen, error)
}

type Holder struct{}

// Exported method returning an unexported interface: sealed is publicized, and
// the cascade must then publicize deepLen through its implicitly-public member.
func (h Holder) Addr() sealed { return nil }
`,
	})

	production := loadProductionForDir(t, dir)

	resetPackageState(production)
	packagePublicizedTypes = nil
	packagePublicizedLiftedTypes = nil

	collectPublicizedTypes(production.Types)

	publicized := publicizedTypeNames()

	if !publicized["sealed"] {
		t.Fatalf("an exported method's unexported interface result must be publicized; publicized: %v", publicized)
	}

	if !publicized["deepLen"] {
		t.Fatalf("the cascade must publicize a publicized interface's member signature types; publicized: %v", publicized)
	}
}

// Guards the assumption the fix rests on: interface methods are reached through the UNDERLYING
// *types.Interface, and the method set there includes EMBEDDED members. An embedded unexported
// method is exactly as public in the emitted C# as a directly declared one.
func TestEmbeddedInterfaceMemberSignatureTypesArePublicized(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/embedded\n\ngo 1.23\n",
		"embedded.go": `package embedded

type embLen uint32

type inner interface {
	sockaddr() (embLen, error)
}

type Outer interface {
	inner
}
`,
	})

	production := loadProductionForDir(t, dir)

	resetPackageState(production)
	packagePublicizedTypes = nil
	packagePublicizedLiftedTypes = nil

	collectPublicizedTypes(production.Types)

	if !publicizedTypeNames()["embLen"] {
		t.Fatalf("an EMBEDDED unexported interface method's signature types must be publicized; publicized: %v", publicizedTypeNames())
	}
}

// Sanity: the helper reads the pass's own map, so a pass that publicized nothing at all would make
// every negative assertion above vacuously true. Assert the instrument works by requiring the
// classic exported-method case to still register.
func TestPublicizeInstrumentIsNotVacuous(t *testing.T) {
	dir := t.TempDir()

	writeModuleFiles(t, dir, map[string]string{
		"go.mod": "module example/instrument\n\ngo 1.23\n",
		"instrument.go": `package instrument

type choice uint32

type Nat struct{}

func (n Nat) Equal(o Nat) choice { return 0 }
`,
	})

	production := loadProductionForDir(t, dir)

	resetPackageState(production)
	packagePublicizedTypes = nil
	packagePublicizedLiftedTypes = nil

	collectPublicizedTypes(production.Types)

	if !publicizedTypeNames()["choice"] {
		t.Fatalf("an EXPORTED method's unexported result type must be publicized (bigmod's Nat.Equal); publicized: %v", publicizedTypeNames())
	}

	// The pass must be walking real go/types objects, not names — a smoke check that the
	// publicized set holds TypeName objects from this package.
	for obj := range packagePublicizedTypes {
		if _, ok := obj.(*types.TypeName); !ok {
			t.Fatalf("publicized set must hold *types.TypeName objects, got %T", obj)
		}
	}
}
