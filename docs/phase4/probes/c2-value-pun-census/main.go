// c2-value-pun-census: every *(*U)(unsafe.Pointer(&x)) in the standard library, classified the way the
// converter's value-pun recognition classifies it (src/go2cs/valuePunOperations.go), plus the call sites
// of math's four Float*bits functions. The Float*bits seat's record (C2, 2026-09-24). Not a gate.
//
//	go build -o puncensus . && GOOS=<os> ./puncensus > census-<os>.txt 2> prod-sites-<os>.tsv
package main

import (
	"fmt"
	"go/ast"
	"go/token"
	"go/types"
	"os"
	"sort"
	"strings"

	"golang.org/x/tools/go/packages"
)

func main() {
	cfg := &packages.Config{Mode: packages.NeedName | packages.NeedFiles | packages.NeedSyntax | packages.NeedTypes | packages.NeedTypesInfo | packages.NeedTypesSizes, Tests: true}
	pkgs, err := packages.Load(cfg, "std")
	if err != nil {
		panic(err)
	}
	counts := map[string]int{}
	seen := map[string]bool{}
	var sites []string
	for _, p := range pkgs {
		info := p.TypesInfo
		if info == nil {
			continue
		}
		sizes := p.TypesSizes
		for _, f := range p.Syntax {
			name := p.Fset.Position(f.Pos()).Filename
			if seen[name] {
				continue
			}
			seen[name] = true
			scope := "prod"
			if strings.HasSuffix(name, "_test.go") {
				scope = "test"
			}
			// which objects have their address taken anywhere, and how often inside a pun
			addrAll := map[types.Object]int{}
			addrPun := map[types.Object]int{}
			var stack []ast.Node
			type pun struct {
				star   *ast.StarExpr
				x      types.Object
				lvalue bool
				kind   string
			}
			var puns []pun
			ast.Inspect(f, func(n ast.Node) bool {
				if n == nil {
					stack = stack[:len(stack)-1]
					return true
				}
				var parent ast.Node
				if len(stack) > 0 {
					parent = stack[len(stack)-1]
				}
				stack = append(stack, n)
				if u, ok := n.(*ast.UnaryExpr); ok && u.Op == token.AND {
					if id, ok := ast.Unparen(u.X).(*ast.Ident); ok {
						if obj, ok := info.Uses[id].(*types.Var); ok {
							addrAll[obj]++
						}
					}
				}
				st, ok := n.(*ast.StarExpr)
				if !ok {
					return true
				}
				conv, ok := ast.Unparen(st.X).(*ast.CallExpr)
				if !ok || len(conv.Args) != 1 {
					return true
				}
				tv, ok := info.Types[conv.Fun]
				if !ok || !tv.IsType() {
					return true
				}
				ptrU, ok := tv.Type.Underlying().(*types.Pointer)
				if !ok {
					return true
				}
				up, ok := ast.Unparen(conv.Args[0]).(*ast.CallExpr)
				if !ok || len(up.Args) != 1 {
					return true
				}
				if b, ok := info.TypeOf(up).(*types.Basic); !ok || b.Kind() != types.UnsafePointer {
					return true
				}
				amp, ok := ast.Unparen(up.Args[0]).(*ast.UnaryExpr)
				if !ok || amp.Op != token.AND {
					return true
				}
				id, ok := ast.Unparen(amp.X).(*ast.Ident)
				if !ok {
					counts[scope+" pun of a non-identifier (&expr)"]++
					return true
				}
				obj, ok := info.Uses[id].(*types.Var)
				if !ok {
					return true
				}
				tx, tu := obj.Type(), ptrU.Elem()
				bx, okx := tx.Underlying().(*types.Basic)
				bu, oku := tu.Underlying().(*types.Basic)
				kind := "other types"
				if okx && oku && bx.Info()&types.IsNumeric != 0 && bu.Info()&types.IsNumeric != 0 && bx.Kind() != types.UnsafePointer && bu.Kind() != types.UnsafePointer {
					if sizes.Sizeof(tx) == sizes.Sizeof(tu) {
						kind = "numeric, equal size"
					} else {
						kind = "numeric, UNEQUAL size"
					}
				}
				lv := false
				if as, ok := parent.(*ast.AssignStmt); ok {
					for _, l := range as.Lhs {
						if l == n {
							lv = true
						}
					}
				}
				if _, ok := parent.(*ast.IncDecStmt); ok {
					lv = true
				}
				if u, ok := parent.(*ast.UnaryExpr); ok && u.Op == token.AND {
					lv = true // &*(*U)(...) keeps the pointer
				}
				addrPun[obj]++
				puns = append(puns, pun{st, obj, lv, kind})
				return true
			})
			for _, pu := range puns {
				rw := "read"
				if pu.lvalue {
					rw = "WRITE"
				}
				only := "x's address taken elsewhere too"
				if addrAll[pu.x] == addrPun[pu.x] {
					only = "x's address taken ONLY by puns"
				}
				k := fmt.Sprintf("%s pun %s, %s, %s", scope, rw, pu.kind, only)
				counts[k]++
				if scope == "prod" {
					pp := p.Fset.Position(pu.star.Pos())
					sites = append(sites, fmt.Sprintf("%s:%d\t%s\t%s\t%s", pp.Filename[strings.Index(pp.Filename, "/src/")+5:], pp.Line, rw, pu.kind, only))
				}
			}
		}
	}
	// callers of the four math functions
	for _, p := range pkgs {
		if p.TypesInfo == nil {
			continue
		}
		for id, obj := range p.TypesInfo.Uses {
			fn, ok := obj.(*types.Func)
			if !ok || fn.Pkg() == nil || fn.Pkg().Path() != "math" {
				continue
			}
			switch fn.Name() {
			case "Float64bits", "Float32bits", "Float64frombits", "Float32frombits":
				name := p.Fset.Position(id.Pos()).Filename
				key := fmt.Sprintf("%s:%d", name, id.Pos())
				if seen["call:"+key] {
					continue
				}
				seen["call:"+key] = true
				scope := "prod"
				if strings.HasSuffix(name, "_test.go") {
					scope = "test"
				}
				counts[scope+" call math."+fn.Name()]++
			}
		}
	}
	var keys []string
	for k := range counts {
		keys = append(keys, k)
	}
	sort.Strings(keys)
	for _, k := range keys {
		fmt.Printf("%-90s %d\n", k, counts[k])
	}
	sort.Strings(sites)
	fmt.Fprintln(os.Stderr, strings.Join(sites, "\n"))
}
