// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package ssa -- go2cs converted at 2022 March 13 06:22:05 UTC
// import "cmd/compile/internal/ssa" ==> using ssa = go.cmd.compile.@internal.ssa_package
// Original source: C:\Program Files\Go\src\cmd\compile\internal\ssa\trim.go
namespace go.cmd.compile.@internal;

using src = cmd.@internal.src_package;

public static partial class ssa_package {

// trim removes blocks with no code in them.
// These blocks were inserted to remove critical edges.
private static void trim(ptr<Func> _addr_f) {
    ref Func f = ref _addr_f.val;

    nint n = 0;
    foreach (var (_, b) in f.Blocks) {
        if (!trimmableBlock(_addr_b)) {
            f.Blocks[n] = b;
            n++;
            continue;
        }
        var bPos = b.Pos;
        var bIsStmt = bPos.IsStmt() == src.PosIsStmt; 

        // Splice b out of the graph. NOTE: `mergePhi` depends on the
        // order, in which the predecessors edges are merged here.
        var p = b.Preds[0].b;
        var i = b.Preds[0].i;
        var s = b.Succs[0].b;
        var j = b.Succs[0].i;
        var ns = len(s.Preds);
        p.Succs[i] = new Edge(s,j);
        s.Preds[j] = new Edge(p,i);

        foreach (var (_, e) in b.Preds[(int)1..]) {
            p = e.b;
            i = e.i;
            p.Succs[i] = new Edge(s,len(s.Preds));
            s.Preds = append(s.Preds, new Edge(p,i));
        }        if (bIsStmt) {
            var sawStmt = false;
            {
                var v__prev2 = v;

                foreach (var (_, __v) in s.Values) {
                    v = __v;
                    if (isPoorStatementOp(v.Op)) {
                        continue;
                    }
                    if (v.Pos.SameFileAndLine(bPos)) {
                        v.Pos = v.Pos.WithIsStmt();
                    }
                    sawStmt = true;
                    break;
                }
                v = v__prev2;
            }

            if (!sawStmt && s.Pos.SameFileAndLine(bPos)) {
                s.Pos = s.Pos.WithIsStmt();
            }
        }
        if (ns > 1) {
            {
                var v__prev2 = v;

                foreach (var (_, __v) in s.Values) {
                    v = __v;
                    if (v.Op == OpPhi) {
                        mergePhi(_addr_v, j, _addr_b);
                    }
                }
                v = v__prev2;
            }

            nint k = 0;
            {
                var v__prev2 = v;

                foreach (var (_, __v) in b.Values) {
                    v = __v;
                    if (v.Op == OpPhi) {
                        if (v.Uses == 0) {
                            v.resetArgs();
                            continue;
                        }
                        var args = make_slice<ptr<Value>>(len(v.Args));
                        copy(args, v.Args);
                        v.resetArgs();
                        {
                            nint x__prev3 = x;

                            for (nint x = 0; x < j; x++) {
                                v.AddArg(v);
                            }

                            x = x__prev3;
                        }
                        v.AddArg(args[0]);
                        {
                            nint x__prev3 = x;

                            for (x = j + 1; x < ns; x++) {
                                v.AddArg(v);
                            }

                            x = x__prev3;
                        }
                        foreach (var (_, a) in args[(int)1..]) {
                            v.AddArg(a);
                        }
                    }
                    b.Values[k] = v;
                    k++;
                }
                v = v__prev2;
            }

            b.Values = b.Values[..(int)k];
        }
        {
            var v__prev2 = v;

            foreach (var (_, __v) in b.Values) {
                v = __v;
                v.Block = s;
            }
            v = v__prev2;
        }

        k = len(b.Values);
        var m = len(s.Values);
        {
            var i__prev2 = i;

            for (i = 0; i < k; i++) {
                s.Values = append(s.Values, null);
            }

            i = i__prev2;
        }
        copy(s.Values[(int)k..], s.Values[..(int)m]);
        copy(s.Values, b.Values);
    }    if (n < len(f.Blocks)) {
        f.invalidateCFG();
        var tail = f.Blocks[(int)n..];
        {
            var i__prev1 = i;

            foreach (var (__i) in tail) {
                i = __i;
                tail[i] = null;
            }
            i = i__prev1;
        }

        f.Blocks = f.Blocks[..(int)n];
    }
}

// emptyBlock reports whether the block does not contain actual
// instructions
private static bool emptyBlock(ptr<Block> _addr_b) {
    ref Block b = ref _addr_b.val;

    foreach (var (_, v) in b.Values) {
        if (v.Op != OpPhi) {
            return false;
        }
    }    return true;
}

// trimmableBlock reports whether the block can be trimmed from the CFG,
// subject to the following criteria:
//  - it should not be the first block
//  - it should be BlockPlain
//  - it should not loop back to itself
//  - it either is the single predecessor of the successor block or
//    contains no actual instructions
private static bool trimmableBlock(ptr<Block> _addr_b) {
    ref Block b = ref _addr_b.val;

    if (b.Kind != BlockPlain || b == b.Func.Entry) {
        return false;
    }
    var s = b.Succs[0].b;
    return s != b && (len(s.Preds) == 1 || emptyBlock(_addr_b));
}

// mergePhi adjusts the number of `v`s arguments to account for merge
// of `b`, which was `i`th predecessor of the `v`s block.
private static void mergePhi(ptr<Value> _addr_v, nint i, ptr<Block> _addr_b) {
    ref Value v = ref _addr_v.val;
    ref Block b = ref _addr_b.val;

    var u = v.Args[i];
    if (u.Block == b) {
        if (u.Op != OpPhi) {
            b.Func.Fatalf("value %s is not a phi operation", u.LongString());
        }
        v.SetArg(i, u.Args[0]);
        v.AddArgs(u.Args[(int)1..]);
    }
    else
 { 
        // If the original block contained u = φ(u0, u1, ..., un) and
        // the current phi is
        //    v = φ(v0, v1, ...,  vi, ..., vk)
        // i.e. it does not use a value from the predecessor block,
        // then the merged phi is
        //    v = φ(v0, v1, ..., vk, vi, vi, ...)
        for (nint j = 1; j < len(b.Preds); j++) {
            v.AddArg(v.Args[i]);
        }
    }
}

} // end ssa_package
