// GoStrAttribute.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System;

namespace go;

/// <summary>
/// Marks the member of an sstring TWIN that carries the Go body (docs/phase4/DESIGN-sstring-twin-pilot.md):
/// each twinned parameter is typed <c>sstring</c>. go2cs-gen's StrGenerator emits its companions:
/// the <c>@string</c> member that forwards to it under <c>[OverloadResolutionPriority(-1)]</c>, and for a
/// package-level function the canonical value delegate <c>&lt;Name&gt;ᶠ</c>.
/// </summary>
/// <remarks>
/// A generator trigger, like <see cref="GoRecvAttribute"/>: every call with an <c>@string</c>, a C# string
/// or a u8 literal binds the marked member, and every func value names the delegate. The package's
/// <c>package_info.cs</c> separately PUBLISHES each exported package-level twin to other packages as
/// <see cref="GoSStringTwinAttribute"/>, for the converter.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GoStrAttribute : Attribute;
