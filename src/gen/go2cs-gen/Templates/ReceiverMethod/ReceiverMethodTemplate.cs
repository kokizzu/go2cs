// ReceiverMethodTemplate.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Linq;
using static go2cs.Common;
using static go2cs.Symbols;

namespace go2cs.Templates.ReceiverMethod;

internal class ReceiverMethodTemplate : TemplateBase
{
    // Template Parameters
    public required MethodInfo Method;

    private string? m_receiverParamName;
    private string ReceiverParamName => m_receiverParamName ??= Method.Parameters.First().name;

    // The heap-box parameter/local name (`Ꮡx`). Built from the UNescaped receiver name: a C#-keyword
    // receiver is escaped as `@enum`, but `Ꮡ@enum` is invalid ('@' is only valid as a leading prefix).
    // The `Ꮡ` prefix already yields a distinct, valid identifier, so strip the '@' → `Ꮡenum`.
    private string? m_receiverBoxName;
    private string ReceiverBoxName => m_receiverBoxName ??= $"{AddressPrefix}{GetUnsanitizedIdentifier(ReceiverParamName)}";

    private string? m_receiverParamType;
    private string ReceiverParamType => m_receiverParamType ??= $"{PointerPrefix}<{Method.Parameters.First().type}>";

    // The receiver alias is nil-DEFERRING, matching the converter's own entry preamble for a
    // direct-ж receiver. This bridge is how a `ref T`-receiver method is reached through a box, so
    // `.Value` deref'd at the BRIDGE — a nil receiver panicked before the method it forwards to
    // could run at all, which is one call frame earlier than Go, where the method RUNS and only the
    // body's own dereference panics. DerefOrNull binds a null ref instead: legal to hold and to pass
    // on as `ref T`, and the callee's first field read/write raises the nil-pointer panic with Go's
    // message, at Go's point — after any side effect the callee performed first. A non-nil box is
    // unaffected (the same real slot).
    // A B′-S0 arm-(a) primary returns `ref T` — the receiver itself (the R3 ruling, 2026-09-02:
    // Go's fluent `return v` returns the receiver POINTER, which the primary cannot mint). The
    // twin restores the ж surface for every existing consumer: it delegates (discarding the ref —
    // the mutation already landed in the box's own storage through DerefOrNull) and returns ITS
    // OWN box, which IS Go's receiver pointer — `p := Ꮡv.M(…); p == Ꮡv` holds by construction
    // (the identity guard row in ZhBoxSelectionProbeTests' fluent class).
    private bool IsRefReturnPrimary =>
        Method.ReturnType.StartsWith("ref ", StringComparison.Ordinal);

    public override string TemplateBody => IsRefReturnPrimary
        ? $$"""
            [{{GeneratedCodeAttribute}}]
            {{TargetScope}} static {{ReceiverParamType}} {{Method.Name}}{{Method.GetGenericSignature()}}({{DeclParams}}){{Method.GetWhereConstraints()}}
            {
                ref var {{ReceiverParamName}} = ref {{ReceiverBoxName}}.{{NilDeferringDerefAccessor}};
                {{ReceiverParamName}}.{{Method.Name}}({{CallParams}});
                return {{ReceiverBoxName}};
            }
        """
        : $$"""
            [{{GeneratedCodeAttribute}}]
            {{TargetScope}} static {{Method.ReturnType}} {{Method.Name}}{{Method.GetGenericSignature()}}({{DeclParams}}){{Method.GetWhereConstraints()}}
            {
                ref var {{ReceiverParamName}} = ref {{ReceiverBoxName}}.{{NilDeferringDerefAccessor}};
                {{ReturnStatement}}{{ReceiverParamName}}.{{Method.Name}}({{CallParams}});
            }
        """;

    private string DeclParams
    {
        get
        {
            List<string> result = [];
            bool first = true;

            foreach ((string type, string name) in Method.Parameters)
            {
                if (first)
                {
                    result.Add($"this {PointerPrefix}<{type}> {ReceiverBoxName}");
                    first = false;
                }
                else
                {
                    result.Add($"{type} {name}");
                }
            }

            return string.Join(", ", result);
        }
    }

    private string ReturnStatement =>
        Method.ReturnType == "void" ? "" : "return ";

    private string CallParams => 
        string.Join(", ", Method.Parameters.Skip(1).Select(item => item.name));

    private string TargetScope
    {
        get
        {
            string receiverScope = GetScope(GetSimpleName(Method.Parameters[0].type));
            return Scope == receiverScope ? Scope : "internal";
        }
    }
}
