// Copyright 2021 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.
[assembly: global::go.GoPositionMap("go/types/signature.go", "signature.cs", "ACRO4gACEgAIAoKCgpSCgIK2goKClJSCgpSUAAIS4KaQppCmkKaQppCkgKKAAAkMAAsCgoKCgoS4gqaCggAHEIKCgpSqosqAgsiCgoKCuP6CuoK4ggAJFoKCgoKCgoKCgpaIsra2grSkqKSCgoLKmKKClIKClIKmkua4pKLGxMqCggAJCvKClpKCgoCCgoKUyKaUgoKmgoKUpoKCgqiC3oKCgpY=")]

namespace go.go;

using fmt = fmt_package;
using ast = global::go.go.ast_package;
using token = global::go.go.token_package;
using static global::go.@internal.types.errors_package;
using constant = global::go.go.constant_package;
using errors = global::go.@internal.types.errors_package;
using global::go.go;

partial class types_package {

// ----------------------------------------------------------------------------
// API

// A Signature represents a (non-builtin) function or method type.
// The receiver is ignored when comparing signatures for identity.
[GoType] partial struct ΔSignature {
    // We need to keep the scope in Signature (rather than passing it around
    // and store it in the Func Object) because when type-checking a function
    // literal we call the general type checker which returns a general Type.
    // We then unpack the *Signature and use the scope for the literal body.
    internal ж<TypeParamList> rparams; // receiver type parameters from left to right, or nil
    internal ж<TypeParamList> tparams; // type parameters from left to right, or nil
    internal ж<ΔScope> scope;    // function scope for package-local and non-instantiated signatures; nil otherwise
    internal ж<Var> recv;        // nil if not a method
    internal ж<Tuple> @params;      // (incoming) parameters from left to right; or nil
    internal ж<Tuple> results;      // (outgoing) results from left to right; or nil
    internal bool variadic;           // true if the last parameter's type is of the form ...T (or string, for append built-in only)
}

// NewSignature returns a new function type for the given receiver, parameters,
// and results, either of which may be nil. If variadic is set, the function
// is variadic, it must have at least one parameter, and the last parameter
// must be of unnamed slice type.
//
// Deprecated: Use [NewSignatureType] instead which allows for type parameters.
public static ж<ΔSignature> NewSignature(ж<Var> Ꮡrecv, ж<Tuple> Ꮡparams, ж<Tuple> Ꮡresults, bool variadic) {
    return NewSignatureType(Ꮡrecv, default!, default!, Ꮡparams, Ꮡresults, variadic);
}

// NewSignatureType creates a new function type for the given receiver,
// receiver type parameters, type parameters, parameters, and results. If
// variadic is set, params must hold at least one parameter and the last
// parameter's core type must be of unnamed slice or bytestring type.
// If recv is non-nil, typeParams must be empty. If recvTypeParams is
// non-empty, recv must be non-nil.
public static ж<ΔSignature> NewSignatureType(ж<Var> Ꮡrecv, slice<ж<TypeParam>> recvTypeParams, slice<ж<TypeParam>> typeParams, ж<Tuple> Ꮡparams, ж<Tuple> Ꮡresults, bool variadic) {
    ref var @params = ref Ꮡparams.DerefOrNull();

    if (variadic) {
        nint n = Ꮡparams.Len();
        if (n == 0) {
            throw panic("variadic function must have at least one parameter");
        }
        var core = coreString((~@params.At(n - 1)).typ);
        {
            var (_, ok) = core._<ж<Slice>>(ᐧ); if (!ok && !isString(core)) {
                throw panic(fmt.Sprintf("got %s, want variadic parameter with unnamed slice type or string as core type"u8, core.String()));
            }
        }
    }
    var sig = Ꮡ(new ΔSignature(recv: Ꮡrecv, @params: Ꮡparams, results: Ꮡresults, variadic: variadic));
    if (len(recvTypeParams) != 0) {
        if (Ꮡrecv == nil) {
            throw panic("function with receiver type parameters must have a receiver");
        }
        sig.Value.rparams = bindTParams(recvTypeParams);
    }
    if (len(typeParams) != 0) {
        if (Ꮡrecv != nil) {
            throw panic("function with type parameters cannot have a receiver");
        }
        sig.Value.tparams = bindTParams(typeParams);
    }
    return sig;
}

// Recv returns the receiver of signature s (if a method), or nil if a
// function. It is ignored when comparing signatures for identity.
//
// For an abstract method, Recv returns the enclosing interface either
// as a *[Named] or an *[Interface]. Due to embedding, an interface may
// contain methods whose receiver type is a different interface.
[GoRecv] public static ж<Var> Recv(this ref ΔSignature s) {
    return s.recv;
}

// TypeParams returns the type parameters of signature s, or nil.
[GoRecv] public static ж<TypeParamList> TypeParams(this ref ΔSignature s) {
    return s.tparams;
}

// RecvTypeParams returns the receiver type parameters of signature s, or nil.
[GoRecv] public static ж<TypeParamList> RecvTypeParams(this ref ΔSignature s) {
    return s.rparams;
}

// Params returns the parameters of signature s, or nil.
[GoRecv] public static ж<Tuple> Params(this ref ΔSignature s) {
    return s.@params;
}

// Results returns the results of signature s, or nil.
[GoRecv] public static ж<Tuple> Results(this ref ΔSignature s) {
    return s.results;
}

// Variadic reports whether the signature s is variadic.
[GoRecv] public static bool Variadic(this ref ΔSignature s) {
    return s.variadic;
}

public static ΔType Underlying(this ж<ΔSignature> Ꮡt) {
    return new ΔSignatureжΔType(Ꮡt);
}

public static @string String(this ж<ΔSignature> Ꮡt) {
    return TypeString(new ΔSignatureжΔType(Ꮡt), default!);
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string typeParameterˢ = "type parameter"u8;
internal static readonly @string functionBodyTempScopeˢ = "function body (temp. scope)"u8;
internal static readonly @string methodHasMultipleˢ = "method has multiple receivers"u8;
internal static readonly @string unsafePointerˢ = "unsafe.Pointer"u8;
internal static readonly @string pointerOrInterfaceTypeˢ = "pointer or interface type"u8;

// ----------------------------------------------------------------------------
// Implementation

// funcType type-checks a function or method type.
internal static void funcType(this ж<Checker> Ꮡcheck, ж<ΔSignature> Ꮡsig, ж<ast.FieldList> ᏑrecvPar, ж<ast.FuncType> Ꮡftyp) {
    GoFrame ᒐ = default;
    try {
        ref var check = ref Ꮡcheck.DerefOrNull();
        ref var sig = ref Ꮡsig.DerefOrNull();
        ref var recvPar = ref ᏑrecvPar.DerefOrNull();
        ref var ftyp = ref Ꮡftyp.DerefOrNull();

        check.openScope(new ast.FuncTypeжNode(Ꮡftyp), functionˢ);
        check.scope.Value.isFunc = true;
        check.recordScope(new ast.FuncTypeжNode(Ꮡftyp), check.scope);
        sig.scope = check.scope;
        defer(Ꮡcheck.closeScope, ref ᒐ);
        if (ᏑrecvPar != nil && len(recvPar.List) > 0) {
            // collect generic receiver type parameters, if any
            // - a receiver type parameter is like any other type parameter, except that it is declared implicitly
            // - the receiver specification acts as local declaration for its type parameters, which may be blank
            var (_, rname, rparams) = Ꮡcheck.unpackRecv((~recvPar.List[0]).Type, true);
            if (len(rparams) > 0) {
                // The scope of the type parameter T in "func (r T[T]) f()"
                // starts after f, not at "r"; see #52038.
                tokenꓸPos scopePosΔ1 = ftyp.Params.Pos();
                var tparams = Ꮡcheck.declareTypeParams(default!, rparams, scopePosΔ1);
                sig.rparams = bindTParams(tparams);
                // Blank identifiers don't get declared, so naive type-checking of the
                // receiver type expression would fail in Checker.collectParams below,
                // when Checker.ident cannot resolve the _ to a type.
                //
                // Checker.recvTParamMap maps these blank identifiers to their type parameter
                // types, so that they may be resolved in Checker.ident when they fail
                // lookup in the scope.
                foreach (var (i, p) in rparams) {
                    if ((~p).Name == "_"u8) {
                        if (check.recvTParamMap == default!) {
                            check.recvTParamMap = new map<ж<ast.Ident>, ж<TypeParam>>();
                        }
                        check.recvTParamMap[p] = tparams[i];
                    }
                }
                // determine receiver type to get its type parameters
                // and the respective type parameter bounds
                slice<ж<TypeParam>> recvTParams = default!;
                if (rname != nil) {
                    // recv should be a Named type (otherwise an error is reported elsewhere)
                    // Also: Don't report an error via genericType since it will be reported
                    //       again when we type-check the signature.
                    // TODO(gri) maybe the receiver should be marked as invalid instead?
                    {
                        var recv = asNamed(Ꮡcheck.genericType(new ast.IdentжExpr(rname), nil)); if (recv != nil) {
                            recvTParams = recv.TypeParams().list();
                        }
                    }
                }
                // provide type parameter bounds
                if (len(tparams) == len(recvTParams)){
                    var smap = makeRenameMap(recvTParams, tparams);
                    foreach (var (i, tpar) in tparams) {
                        var recvTPar = recvTParams[i];
                        check.mono.recordCanon(tpar, recvTPar);
                        // recvTPar.bound is (possibly) parameterized in the context of the
                        // receiver type declaration. Substitute parameters for the current
                        // context.
                        tpar.Value.bound = Ꮡcheck.subst((~(~tpar).obj).pos, (~recvTPar).bound, smap, nil, check.context());
                    }
                } else 
                if (len(tparams) < len(recvTParams)) {
                    // Reporting an error here is a stop-gap measure to avoid crashes in the
                    // compiler when a type parameter/argument cannot be inferred later. It
                    // may lead to follow-on errors (see issues go.dev/issue/51339, go.dev/issue/51343).
                    // TODO(gri) find a better solution
                    @string got = measure(len(tparams), typeParameterˢ);
                    Ꮡcheck.errorf(new ast_FieldListжpositioner(ᏑrecvPar), BadRecv, "got %s, but receiver base type declares %d"u8, got, len(recvTParams));
                }
            }
        }
        if (ftyp.TypeParams != nil) {
            Ꮡcheck.collectTypeParams(Ꮡsig.of(types_package.ΔSignature.Ꮡtparams), ftyp.TypeParams);
            // Always type-check method type parameters but complain that they are not allowed.
            // (A separate check is needed when type-checking interface method signatures because
            // they don't have a receiver specification.)
            if (ᏑrecvPar != nil) {
                Ꮡcheck.error(new ast_FieldListжpositioner(ftyp.TypeParams), InvalidMethodTypeParams, methodsCannotHaveTypeˢ);
            }
        }
        // Use a temporary scope for all parameter declarations and then
        // squash that scope into the parent scope (and report any
        // redeclarations at that time).
        //
        // TODO(adonovan): now that each declaration has the correct
        // scopePos, there should be no need for scope squashing.
        // Audit to ensure all lookups honor scopePos and simplify.
        var scope = NewScope(check.scope, nopos, nopos, functionBodyTempScopeˢ);
        tokenꓸPos scopePos = ftyp.End(); // all parameters' scopes start after the signature
        var (recvList, _) = Ꮡcheck.collectParams(scope, ᏑrecvPar, false, scopePos);
        var (@params, variadic) = Ꮡcheck.collectParams(scope, ftyp.Params, true, scopePos);
        var (results, _) = Ꮡcheck.collectParams(scope, ftyp.Results, false, scopePos);
        scope.squash((Object obj, Object alt) => {
            var err = Ꮡcheck.newError(DuplicateDecl);
            err.addf(new Objectᴠpositioner(obj), "%s redeclared in this block"u8, obj.Name());
            err.addAltDecl(alt);
            err.report();
        });
        if (ᏑrecvPar != nil) {
            // recv parameter list present (may be empty)
            // spec: "The receiver is specified via an extra parameter section preceding the
            // method name. That parameter section must declare a single parameter, the receiver."
            ж<Var> recv = default!;
            var exprᴛ1 = len(recvList);
            var matchᴛ1 = false;
            var matchᴛ2 = exprᴛ1 is 0 || exprᴛ1 is 1;
            if (exprᴛ1 is 0) {
                recv = NewParam(nopos, // error reported by resolver
 nil, ""u8, new BasicжΔType(Typ[Invalid])); // ignore recv below
            }
            else if (!matchᴛ2) { /* default: */
                Ꮡcheck.error(new Varжpositioner(recvList[len(recvList) - 1]), // more than one receiver
 InvalidRecv, methodHasMultipleˢ); // continue with first receiver
                fallthrough = true;
            }
            if (fallthrough || !matchᴛ1 && exprᴛ1 is 1) { matchᴛ1 = true;
                recv = recvList[0];
            }

            sig.recv = recv;
            // Delay validation of receiver type as it may cause premature expansion
            // of types the receiver type is dependent on (see issues go.dev/issue/51232, go.dev/issue/51233).
            var recvʗ1 = recv;
            check.later(() => {
                // spec: "The receiver type must be of the form T or *T where T is a type name."
                var (rtyp, _) = deref((~recvʗ1).typ);
                var atyp = Unalias(rtyp);
                if (!isValid(atyp)) {
                    return; // error was reported before
                }
                // spec: "The type denoted by T is called the receiver base type; it must not
                // be a pointer or interface type and it must be declared in the same package
                // as the method."
                switch (atyp.type()) {
                case ж<Named> T: {
                    if (T.TypeArgs() != nil && Ꮡsig.Value.RecvTypeParams() == nil) {
                        // The receiver type may be an instantiated type referred to
                        // by an alias (which cannot have receiver parameters for now).
                        Ꮡcheck.errorf(new Varжpositioner(recvʗ1), InvalidRecv, "cannot define new methods on instantiated type %s"u8, rtyp);
                        break;
                    }
                    if ((~(~T).obj).pkg != Ꮡcheck.Value.pkg) {
                        Ꮡcheck.errorf(new Varжpositioner(recvʗ1), InvalidRecv, "cannot define new methods on non-local type %s"u8, rtyp);
                        break;
                    }
                    @string cause = default!;
                    var switchᴛ13 = T.under();
                    switch (switchᴛ13.type()) {
                    case ж<Basic> u: {
                        if ((~u).kind == UnsafePointer) {
                            // unsafe.Pointer is treated like a regular pointer
                            cause = unsafePointerˢ;
                        }
                        break;
                    }
                    case ж<Pointer> _:
                    case ж<Interface> _: {
                        var u = switchᴛ13;
                        cause = pointerOrInterfaceTypeˢ;
                        break;
                    }
                    case ж<TypeParam> u: {
                        throw panic("unreachable");
                        break;
                    }}
                    if (cause != ""u8) {
                        // The underlying type of a receiver base type cannot be a
                        // type parameter: "type T[P any] P" is not a valid declaration.
                        Ꮡcheck.errorf(new Varжpositioner(recvʗ1), InvalidRecv, "invalid receiver type %s (%s)"u8, rtyp, cause);
                    }
                    break;
                }
                case ж<Basic> T: {
                    Ꮡcheck.errorf(new Varжpositioner(recvʗ1), InvalidRecv, "cannot define new methods on non-local type %s"u8, rtyp);
                    break;
                }
                default: {
                    var T = atyp;
                    Ꮡcheck.errorf(new Varжpositioner(recvʗ1), InvalidRecv, "invalid receiver type %s"u8, (~recvʗ1).typ);
                    break;
                }}
            }).describef(new Varжpositioner(recv), "validate receiver %s"u8, recv.OrTypedNil());
        }
        sig.@params = NewTuple(@params.ꓸꓸꓸ);
        sig.results = NewTuple(results.ꓸꓸꓸ);
        sig.variadic = variadic;
    }
    catch (Exception ᒐex) when (GoFrame.IsPanic(ᒐex, out PanicException? ᒐp)) { GoFrame.Capture(ᒐp); }
    finally { ᒐ.Run(); }
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
internal static readonly @string anonymousParameterˢ = "anonymous parameter"u8;
internal static readonly @string listContainsBothNamedAndˢ = "list contains both named and anonymous parameters"u8;

// collectParams declares the parameters of list in scope and returns the corresponding
// variable list.
internal static (slice<ж<Var>> @params, bool variadic) collectParams(this ж<Checker> Ꮡcheck, ж<ΔScope> Ꮡscope, ж<ast.FieldList> Ꮡlist, bool variadicOk, tokenꓸPos scopePos) {
    slice<ж<Var>> @params = default!;
    bool variadic = default!;

    ref var check = ref Ꮡcheck.DerefOrNull();
    ref var list = ref Ꮡlist.DerefOrNull();
    if (Ꮡlist == nil) {
        return (@params, variadic);
    }
    bool named = default!;
    bool anonymous = default!;
    foreach (var (i, field) in list.List) {
        var ftype = field.Value.Type;
        {
            var (t, _) = ftype._<ж<ast.Ellipsis>>(ᐧ); if (t != nil) {
                ftype = t.Value.Elt;
                if (variadicOk && i == len(list.List) - 1 && len((~field).Names) <= 1){
                    variadic = true;
                } else {
                    Ꮡcheck.softErrorf(new ast_Ellipsisжpositioner(t), MisplacedDotDotDot, "can only use ... with final parameter in list"u8);
                }
            }
        }
        // ignore ... and continue
        var typ = Ꮡcheck.varType(ftype);
        // The parser ensures that f.Tag is nil and we don't
        // care if a constructed AST contains a non-nil tag.
        if (len((~field).Names) > 0){
            // named parameter
            foreach (var (_, name) in (~field).Names) {
                if ((~name).Name == ""u8) {
                    Ꮡcheck.error(new ast_Identжpositioner(name), InvalidSyntaxTree, anonymousParameterˢ);
                }
                // ok to continue
                var par = NewParam(name.Pos(), check.pkg, (~name).Name, typ);
                Ꮡcheck.declare(Ꮡscope, name, new VarжObject(par), scopePos);
                @params = append(@params, par);
            }
            named = true;
        } else {
            // anonymous parameter
            var par = NewParam(ftype.Pos(), check.pkg, ""u8, typ);
            check.recordImplicit(new ast.FieldжNode(field), new VarжObject(par));
            @params = append(@params, par);
            anonymous = true;
        }
    }
    if (named && anonymous) {
        Ꮡcheck.error(new ast_FieldListжpositioner(Ꮡlist), InvalidSyntaxTree, listContainsBothNamedAndˢ);
    }
    // ok to continue
    // For a variadic function, change the last parameter's type from T to []T.
    // Since we type-checked T rather than ...T, we also need to retro-actively
    // record the type for ...T.
    if (variadic) {
        var last = @params[len(@params) - 1];
        last.Value.typ = new SliceжΔType(Ꮡ(new Slice(elem: (~last).typ)));
        check.recordTypeAndValue((~list.List[len(list.List) - 1]).Type, typexpr, (~last).typ, default!);
    }
    return (@params, variadic);
}

} // end types_package
