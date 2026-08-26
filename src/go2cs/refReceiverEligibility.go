package main

import (
	"go/ast"
	"go/types"
)

// B′ §4.1 receiver eligibility — which pointer-receiver methods may dual-emit.
//
// This is the ANALYSIS half of stage S0 and it emits nothing: it records, per method, whether the
// declaration is eligible to carry a `[GoRecv] this ref T` primary beside the ж twin RecvGenerator
// already mints. Nothing reads the verdict at S0a; the census prints it, exactly as A1's parameter
// classification landed before A2 consumed it.
//
// Why this is a separate, smaller thing than Phase A's classifier, and why that is not a shortcut:
// §4.1's rule is DECLARATION-LOCAL by construction — "everything else dual-emits unconditionally at
// the declaration, because selection pressure lives at call sites, not declarations". There is no
// fixed point to resolve and no whole-program question to answer, so eligibility needs none of the
// two-sided strip machinery `refLoweringAnalysisOperations.go` runs for package-level functions.
// That is also why §8 OQ-2's receiver-only ruling for S0 is load-bearing rather than cosmetic: with
// parameters left boxed, S0 never needs the method-scope parameter fixed point at all. That
// apparatus — which does not exist today, since collectFunc returns early for every method — is
// S1's parameter half.
const (
	// XM-1: declared in a hand-owned file, or supplied by an *_impl.cs companion. The converter does
	// not re-emit these, and a hand-own already chose its own form — the same reasoning (and the
	// same mechanical per-file marker probe) as Phase A's X5-hand-owned arm, reused verbatim.
	refRecvVetoHandOwned = "XM-1-hand-owned"
	// XM-2: value receiver. Not in scope rather than refused — a value receiver already emits the
	// `this T` / `this ref T` shapes without a box, so there is no box for B′ to remove. Recorded so
	// the census denominator stays honest about what it looked at.
	refRecvVetoValueReceiver = "XM-2-value-receiver"
	// XM-3: the receiver's base is an interface or a type parameter — no struct storage to `ref`.
	refRecvVetoNoStorage = "XM-3-no-storage"
	// XM-4: a //go:linkname participant. The registries publicize the symbol across the assembly
	// boundary in the boxed convention, invisibly to a per-package scan — Phase A's X5-linkname arm,
	// reused for the same reason it exists there.
	refRecvVetoLinkname = "XM-4-linkname"
	// XM-5: the receiver type's C# representation IS the box (ж<T> subclasses, reflect-bridge
	// special types), so a `ref T` receiver would name a type that does not exist in that form.
	// Curated rather than inferred, exactly like the linkname and hand-own-caller registries and for
	// the same reason: the frozen C# these describe is invisible to a Go-source scan. The set is
	// deliberately empty today — every instance the corpus actually carries is declared in a
	// hand-owned file and is therefore already caught by XM-1 — and the census will say so rather
	// than leaving the arm's emptiness to be assumed.
	refRecvVetoBoxRepresented = "XM-5-box-represented"
)

// refRecvBoxRepresentedTypes is XM-5's curated set, keyed `<canonical-pkg-path>.<TypeName>`. Empty
// by construction today (see refRecvVetoBoxRepresented); adding an entry is a deliberate act with a
// cited C# declaration, never a guess from Go source.
var refRecvBoxRepresentedTypes = map[string]bool{}

// refMethodVerdict records one method's §4.1 eligibility. A method with no vetoes is eligible to
// dual-emit; the vetoes are kept as a list rather than a bool so the census can report WHY, which
// is what makes an unexpectedly small eligible set diagnosable instead of merely disappointing.
type refMethodVerdict struct {
	PkgPath  string   `json:"pkg"`
	Recv     string   `json:"recv"`
	Name     string   `json:"name"`
	Exported bool     `json:"exported"`
	Vetoes   []string `json:"vetoes,omitempty"`
}

// Eligible reports whether this method may carry the ref-receiver primary.
func (verdict *refMethodVerdict) Eligible() bool {
	return len(verdict.Vetoes) == 0
}

// classifyMethodReceiver applies §4.1 to one method declaration and returns its verdict. It is
// called from collectFunc's method branch, which previously did nothing but count pointer-param
// positions for B′'s pricing context.
//
// fileIsHandOwned carries the declaring file's manual-conversion status, exactly as the
// package-level path receives it.
func (a *refLoweringAnalysis) classifyMethodReceiver(funcDecl *ast.FuncDecl, obj *types.Func, signature *types.Signature, fileIsHandOwned bool) *refMethodVerdict {
	recv := signature.Recv()

	if recv == nil {
		return nil
	}

	verdict := &refMethodVerdict{
		PkgPath:  a.pkg.Path(),
		Recv:     refReceiverBaseName(recv.Type()),
		Name:     obj.Name(),
		Exported: obj.Exported(),
	}

	recvType := types.Unalias(recv.Type())
	pointer, isPointer := recvType.(*types.Pointer)

	if !isPointer {
		// XM-2 — and return immediately: the remaining arms all reason about a pointer receiver's
		// base, and reporting them alongside "this was never a pointer" would inflate every count.
		verdict.Vetoes = append(verdict.Vetoes, refRecvVetoValueReceiver)
		return verdict
	}

	if !refReceiverHasRefStorage(pointer.Elem()) {
		verdict.Vetoes = append(verdict.Vetoes, refRecvVetoNoStorage)
	}

	if fileIsHandOwned || funcDecl.Body == nil {
		// A bodiless method is the assembly/cgo partial-stub shape: its emitted partial declaration
		// pairs with a hand-written *_impl.cs, so its form is frozen for the same reason a
		// hand-owned file's is. Folded into XM-1 rather than given its own arm, because the remedy
		// and the reasoning are identical.
		verdict.Vetoes = append(verdict.Vetoes, refRecvVetoHandOwned)
	}

	if a.isLinknameExposed(obj.Name()) || a.isLinknameExposed(verdict.Recv+"."+obj.Name()) {
		// Both spellings are probed because the registries key methods inconsistently across the
		// three of them (handle sets carry the bare name; the forward/push maps carry the qualified
		// form). Probing one and trusting it is how a linkname participant slips through.
		verdict.Vetoes = append(verdict.Vetoes, refRecvVetoLinkname)
	}

	if refRecvBoxRepresentedTypes[refCanonicalPkgPath(a.pkg.Path())+"."+verdict.Recv] {
		verdict.Vetoes = append(verdict.Vetoes, refRecvVetoBoxRepresented)
	}

	return verdict
}

// refReceiverHasRefStorage reports whether a pointer receiver's base type has struct storage a
// `ref` can bind — XM-3's test. An interface has no storage of its own, and a type parameter names
// no single layout, so neither can carry the primary.
func refReceiverHasRefStorage(base types.Type) bool {
	base = types.Unalias(base)

	if _, isTypeParam := base.(*types.TypeParam); isTypeParam {
		return false
	}

	if _, isInterface := base.Underlying().(*types.Interface); isInterface {
		return false
	}

	return true
}

// refReceiverBaseName renders a receiver type's base name (`*Element` → `Element`) for census
// keying. Unnamed bases render empty rather than synthesized: a method cannot be declared on one in
// Go, so an empty name is a signal the caller should never see, not a case to paper over.
func refReceiverBaseName(recvType types.Type) string {
	t := types.Unalias(recvType)

	if pointer, isPointer := t.(*types.Pointer); isPointer {
		t = types.Unalias(pointer.Elem())
	}

	switch named := t.(type) {
	case *types.Named:
		return named.Obj().Name()
	case *types.TypeParam:
		return named.Obj().Name()
	}

	return ""
}
