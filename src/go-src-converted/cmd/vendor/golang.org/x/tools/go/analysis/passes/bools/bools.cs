// Copyright 2014 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// Package bools defines an Analyzer that detects common mistakes
// involving boolean operators.

// package bools -- go2cs converted at 2022 March 13 06:41:45 UTC
// import "cmd/vendor/golang.org/x/tools/go/analysis/passes/bools" ==> using bools = go.cmd.vendor.golang.org.x.tools.go.analysis.passes.bools_package
// Original source: C:\Program Files\Go\src\cmd\vendor\golang.org\x\tools\go\analysis\passes\bools\bools.go
namespace go.cmd.vendor.golang.org.x.tools.go.analysis.passes;

using ast = go.ast_package;
using token = go.token_package;
using types = go.types_package;

using analysis = golang.org.x.tools.go.analysis_package;
using inspect = golang.org.x.tools.go.analysis.passes.inspect_package;
using analysisutil = golang.org.x.tools.go.analysis.passes.@internal.analysisutil_package;
using inspector = golang.org.x.tools.go.ast.inspector_package;
using System;

public static partial class bools_package {

public static readonly @string Doc = "check for common mistakes involving boolean operators";



public static ptr<analysis.Analyzer> Analyzer = addr(new analysis.Analyzer(Name:"bools",Doc:Doc,Requires:[]*analysis.Analyzer{inspect.Analyzer},Run:run,));

private static (object, error) run(ptr<analysis.Pass> _addr_pass) {
    object _p0 = default;
    error _p0 = default!;
    ref analysis.Pass pass = ref _addr_pass.val;

    ptr<inspector.Inspector> inspect = pass.ResultOf[inspect.Analyzer]._<ptr<inspector.Inspector>>();

    ast.Node nodeFilter = new slice<ast.Node>(new ast.Node[] { (*ast.BinaryExpr)(nil) });
    var seen = make_map<ptr<ast.BinaryExpr>, bool>();
    inspect.Preorder(nodeFilter, n => {
        ptr<ast.BinaryExpr> e = n._<ptr<ast.BinaryExpr>>();
        if (seen[e]) { 
            // Already processed as a subexpression of an earlier node.
            return ;
        }
        boolOp op = default;

        if (e.Op == token.LOR) 
            op = or;
        else if (e.Op == token.LAND) 
            op = and;
        else 
            return ;
                var comm = op.commutativeSets(pass.TypesInfo, e, seen);
        foreach (var (_, exprs) in comm) {
            op.checkRedundant(pass, exprs);
            op.checkSuspect(pass, exprs);
        }
    });
    return (null, error.As(null!)!);
}

private partial struct boolOp {
    public @string name;
    public token.Token tok; // token corresponding to this operator
    public token.Token badEq; // token corresponding to the equality test that should not be used with this operator
}

private static boolOp or = new boolOp("or",token.LOR,token.NEQ);private static boolOp and = new boolOp("and",token.LAND,token.EQL);

// commutativeSets returns all side effect free sets of
// expressions in e that are connected by op.
// For example, given 'a || b || f() || c || d' with the or op,
// commutativeSets returns {{b, a}, {d, c}}.
// commutativeSets adds any expanded BinaryExprs to seen.
private static slice<slice<ast.Expr>> commutativeSets(this boolOp op, ptr<types.Info> _addr_info, ptr<ast.BinaryExpr> _addr_e, map<ptr<ast.BinaryExpr>, bool> seen) {
    ref types.Info info = ref _addr_info.val;
    ref ast.BinaryExpr e = ref _addr_e.val;

    var exprs = op.split(e, seen); 

    // Partition the slice of expressions into commutative sets.
    nint i = 0;
    slice<slice<ast.Expr>> sets = default;
    for (nint j = 0; j <= len(exprs); j++) {
        if (j == len(exprs) || hasSideEffects(_addr_info, exprs[j])) {
            if (i < j) {
                sets = append(sets, exprs[(int)i..(int)j]);
            }
            i = j + 1;
        }
    }

    return sets;
}

// checkRedundant checks for expressions of the form
//   e && e
//   e || e
// Exprs must contain only side effect free expressions.
private static void checkRedundant(this boolOp op, ptr<analysis.Pass> _addr_pass, slice<ast.Expr> exprs) {
    ref analysis.Pass pass = ref _addr_pass.val;

    var seen = make_map<@string, bool>();
    foreach (var (_, e) in exprs) {
        var efmt = analysisutil.Format(pass.Fset, e);
        if (seen[efmt]) {
            pass.ReportRangef(e, "redundant %s: %s %s %s", op.name, efmt, op.tok, efmt);
        }
        else
 {
            seen[efmt] = true;
        }
    }
}

// checkSuspect checks for expressions of the form
//   x != c1 || x != c2
//   x == c1 && x == c2
// where c1 and c2 are constant expressions.
// If c1 and c2 are the same then it's redundant;
// if c1 and c2 are different then it's always true or always false.
// Exprs must contain only side effect free expressions.
private static void checkSuspect(this boolOp op, ptr<analysis.Pass> _addr_pass, slice<ast.Expr> exprs) {
    ref analysis.Pass pass = ref _addr_pass.val;
 
    // seen maps from expressions 'x' to equality expressions 'x != c'.
    var seen = make_map<@string, @string>();

    foreach (var (_, e) in exprs) {
        ptr<ast.BinaryExpr> (bin, ok) = e._<ptr<ast.BinaryExpr>>();
        if (!ok || bin.Op != op.badEq) {
            continue;
        }
        ast.Expr x = default;

        if (pass.TypesInfo.Types[bin.Y].Value != null) 
            x = bin.X;
        else if (pass.TypesInfo.Types[bin.X].Value != null) 
            x = bin.Y;
        else 
            continue;
        // e is of the form 'x != c' or 'x == c'.
        var xfmt = analysisutil.Format(pass.Fset, x);
        var efmt = analysisutil.Format(pass.Fset, e);
        {
            var (prev, found) = seen[xfmt];

            if (found) { 
                // checkRedundant handles the case in which efmt == prev.
                if (efmt != prev) {
                    pass.ReportRangef(e, "suspect %s: %s %s %s", op.name, efmt, op.tok, prev);
                }
            }
            else
 {
                seen[xfmt] = efmt;
            }

        }
    }
}

// hasSideEffects reports whether evaluation of e has side effects.
private static bool hasSideEffects(ptr<types.Info> _addr_info, ast.Expr e) {
    ref types.Info info = ref _addr_info.val;

    var safe = true;
    ast.Inspect(e, node => {
        switch (node.type()) {
            case ptr<ast.CallExpr> n:
                var typVal = info.Types[n.Fun];

                if (typVal.IsType())                 else if (typVal.IsBuiltin()) 
                    // Builtin func, conservatively assumed to not
                    // be safe for now.
                    safe = false;
                    return false;
                else 
                    // A non-builtin func or method call.
                    // Conservatively assume that all of them have
                    // side effects for now.
                    safe = false;
                    return false;
                                break;
            case ptr<ast.UnaryExpr> n:
                if (n.Op == token.ARROW) {
                    safe = false;
                    return false;
                }
                break;
        }
        return true;
    });
    return !safe;
}

// split returns a slice of all subexpressions in e that are connected by op.
// For example, given 'a || (b || c) || d' with the or op,
// split returns []{d, c, b, a}.
// seen[e] is already true; any newly processed exprs are added to seen.
private static slice<ast.Expr> split(this boolOp op, ast.Expr e, map<ptr<ast.BinaryExpr>, bool> seen) {
    slice<ast.Expr> exprs = default;

    while (true) {
        e = unparen(e);
        {
            ptr<ast.BinaryExpr> (b, ok) = e._<ptr<ast.BinaryExpr>>();

            if (ok && b.Op == op.tok) {
                seen[b] = true;
                exprs = append(exprs, op.split(b.Y, seen));
                e = b.X;
            }
            else
 {
                exprs = append(exprs, e);
                break;
            }

        }
    }
    return ;
}

// unparen returns e with any enclosing parentheses stripped.
private static ast.Expr unparen(ast.Expr e) {
    while (true) {
        ptr<ast.ParenExpr> (p, ok) = e._<ptr<ast.ParenExpr>>();
        if (!ok) {
            return e;
        }
        e = p.X;
    }
}

} // end bools_package
