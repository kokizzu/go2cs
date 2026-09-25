// GoTwinForwarderAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Marks a converter-emitted FORWARDER of an sstring twin (docs/phase4/DESIGN-sstring-twin-pilot.md):
/// the <c>@string</c> member that forwards to the prioritized <c>sstring</c> member, and the lambda of
/// the function's canonical value delegate (<c>Sprintfᶠ</c>). A forwarder is go2cs machinery rather
/// than Go code, so a traceback skips its frame exactly as it skips a go2cs-gen forwarder, and a call
/// through it shows the same Go frames as a direct call.
/// </summary>
/// <remarks>
/// <see cref="GoName"/> is set on the canonical delegate's lambda. The C# compiler emits that lambda
/// as a generated method (<c>&lt;.cctor&gt;b__X_Y</c>), so the Go name of the function the value stands for
/// is recorded here. <c>GoSyntheticPC.GoNameOf</c> and the runtime's frame naming read it, which is
/// what makes <c>runtime.FuncForPC(reflect.ValueOf(fmt.Sprintf).Pointer()).Name()</c> answer
/// <c>fmt.Sprintf</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GoTwinForwarderAttribute(string? goName = null) : Attribute
{
    /// <summary>The Go name of the function the forwarder stands for, or <c>null</c> when the method's own name is it.</summary>
    public string? GoName { get; } = goName;
}
