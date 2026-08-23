// InterfaceImplTemplate.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Collections.Generic;
using System.Text;
using static go2cs.Common;
using static go2cs.Symbols;

namespace go2cs.Templates.InterfaceImpl;

internal class InterfaceImplTemplate : TemplateBase
{
    // Template Parameters
    public required string StructName;
    public required string InterfaceName;
    public required bool Promoted;
    public required HashSet<string> Overrides;
    public required List<MethodInfo> Methods;

    // Single embedded-pointer hop property (`Type` for rtype's `*abi.Type`): an interface member
    // with no direct struct method forwards through it (`this.Type.Value.M()`), matching the
    // converter's syntax-resolved promotion at Go call sites. Null when no (single) hop exists.
    public string? EmbedHop;

    // Hop-target methods that are direct-ж primaries (extensions on ж<X> with no ref twin) bind
    // the box field itself — `this.File.Read(p)`; deref'ing first strands the receiver (CS1929).
    public HashSet<string> EmbedHopBoxMethods = [];

    // Methods declared on a VALUE-embedded field of the hop type with a POINTER receiver bind a
    // projected field box — `this.TCPConn.of(TCPConn.Ꮡconn).Read(p)` (net CS1929 x2); the map
    // carries the `.of(…)` suffix per method name.
    public Dictionary<string, string>? EmbedHopDeepPaths;

    // With SEVERAL embedded pointers there is no single hop to name, so the receiver is decided per
    // member: each routes to the UNIQUE embed declaring it (`this.PipeReader.Read(p)`), Go's depth-1
    // promotion rule. Empty for one embed or none — that case keeps EmbedHop above.
    public Dictionary<string, string>? MultiEmbedHopPaths;

    // Single VALUE-embedded field (`addrPortUDPAddr struct { netip.AddrPort }`): an interface
    // member with no direct struct method promotes through it (`this.AddrPort.String()`).
    public string? ValueEmbedHop;

    // Non-null when the value embed's type is FOREIGN (dotted) - the extension lives in another
    // namespace segment the file only aliases, so the forwarding calls the package-class static
    // directly: `global::go.net.netip_package.String(this.AddrPort)`.
    public string? ValueEmbedHopStaticClass;

    // The struct's own accessibility, so a promoted method's EXTENSION twin below is declared
    // exactly as the converter declares that type's own Go methods.
    public string StructAccessibility = "internal";

    public override string TemplateBody =>
        $$"""
             partial struct {{StructName}} : {{InterfaceName}}
             {
                 {{MethodsImplementation}}{{Comparisions}}
             }{{PromotedExtensionMethods}}
         """;

    // A PROMOTED interface method is a Go method of the struct, and it is the ONE kind of Go method
    // that never became an extension method — the member above satisfies the C# interface, but
    // go2cs's runtime method set is built from EXTENSION methods alone
    // (TypeExtensions.GetGoMethodSetCandidates), so a promoted method was invisible to every
    // structural question asked about the type.
    //
    // That is not a cosmetic gap. `builtin.Implements<T>` answers a direct assert with C# `is T`,
    // which succeeds for the embedded interface itself; ANY OTHER interface falls to the structural
    // probe, which counted only the directly-declared methods. A type embedding `net.Conn` and
    // adding ReadFrom/WriteTo therefore failed `c.(net.PacketConn)` — Go takes the UDP arm, the
    // conversion took TCP framing (F2). The twin below closes it by making the promoted method an
    // ordinary Go method: the probe finds it, AdapterBinder binds it through the same candidate
    // source, and reflect's NumMethod counts it, which is what Go does too.
    //
    // Emitting a member AND an extension of one name is legal and deliberate: C# prefers the member
    // at every direct call site, so behavior is unchanged there, while the extension exists for the
    // reflection-sourced registry. Accessibility follows the STRUCT, matching the converter's own
    // rule for that type's methods; the registry admits non-public extensions
    // (BindingFlags.NonPublic in ExtensionMethodRegistry), so an unexported type's twin is still
    // discovered by a foreign assembly's assert — which is precisely where F2 bites.
    //
    // Shadowing is Go's rule and is already decided: `Overrides` holds the struct's own declared
    // methods, and a member it declares itself needs no twin (it has one — the converter emitted
    // it). Only the !methodOverriden arm — the one that actually promotes — gets a twin.
    private string PromotedExtensionMethods
    {
        get
        {
            if (!Promoted)
                return "";

            StringBuilder result = new();

            foreach (MethodInfo method in Methods)
            {
                string simpleMethodName = GetSimpleName(method.Name);

                if (Overrides.Contains(method.ForwardMemberName(simpleMethodName)))
                    continue;

                string receiver = $"recv{TempVarMarker}";
                string typedParameters = method.GetTypedParameters(false);
                string parameterList = string.IsNullOrEmpty(typedParameters) ?
                    $"this {StructName} {receiver}" :
                    $"this {StructName} {receiver}, {typedParameters}";

                string callParameters = method.GetCallParameters(false);
                string embedField = GetSimpleName(InterfaceName, dropCollisionPrefix: true);

                result.Append($"\r\n\r\n    // Go method set entry for the promoted '{GetSimpleName(InterfaceName)}.{simpleMethodName}()':\r\n");
                result.Append($"    {StructAccessibility} static {method.ReturnType} {EscapeCsKeyword(simpleMethodName)}{method.GetGenericSignature()}({parameterList}){method.GetWhereConstraints()} => ");
                result.Append($"{receiver}.{embedField}.{simpleMethodName}{method.GetGenericSignature()}({callParameters});");
            }

            return result.ToString();
        }
    }

    private string MethodsImplementation
    {
        get
        {
            StringBuilder result = new();

            foreach (MethodInfo method in Methods)
            {
                string simpleInterfaceName = GetSimpleName(InterfaceName);
                string simpleMethodName = GetSimpleName(method.Name);

                // Implemented under the interface's name, forwarded under the EMITTED one when the
                // collision pass renamed the implementation — see MethodInfo.ForwardName. `Overrides`
                // is keyed by the DECLARED names, so the resolved name is what decides whether this
                // member has a direct struct method at all; a Go-name miss read a renamed declaration
                // as absent and sent the member down the promotion path instead. Non-null only when
                // the struct itself declares the Δ name, which is exactly when `methodOverriden`
                // becomes true and every embed hop below is skipped — so the hop lookups keep reading
                // the interface member's own name, which is what the EMBEDDED type declares it under.
                string forwardName = method.ForwardMemberName(simpleMethodName);
                bool methodOverriden = Overrides.Contains(forwardName);

                if (result.Length > 0)
                    result.Append("\r\n\r\n        ");

                if (Promoted && !methodOverriden)
                {
                    // The forwarding receiver is the embedded interface FIELD, which carries the
                    // Go embed name — the Δ-stripped simple name when the interface TYPE was
                    // collision-renamed (bare `ΔHandler.Enabled(…)` binds nothing, CS0103 —
                    // slogtest's `wrapper` embeds slog.ΔHandler as field `Handler`).
                    result.Append($"// '{simpleInterfaceName}.{simpleMethodName}()' implicit implementation mapped to promoted interface receiver method:\r\n        ");
                    result.Append($"public {method.ReturnType} {method.GetSignature()} => {GetSimpleName(InterfaceName, dropCollisionPrefix: true)}.{simpleMethodName}{method.GetGenericSignature()}({method.CallParameters});");
                }
                else
                {
                    if (Promoted && methodOverriden)
                    {
                        result.Append($"// '{simpleInterfaceName}.{simpleMethodName}()' explicit implementation mapped to direct struct receiver method,\r\n        ");
                        result.Append($"// this overrides promoted interface method '{GetSimpleName(InterfaceName)}.{simpleMethodName}':\r\n        ");
                    }
                    else
                    {
                        result.Append($"// '{simpleInterfaceName}.{simpleMethodName}()' explicit implementation mapped to direct struct receiver method:\r\n        ");
                    }

                    string receiver = "this";

                    if (!methodOverriden && EmbedHop is not null)
                    {
                        if (EmbedHopDeepPaths is not null && EmbedHopDeepPaths.TryGetValue(simpleMethodName, out string? deepPath))
                            receiver = $"this.{EmbedHop}{deepPath}";
                        else
                            receiver = EmbedHopBoxMethods.Contains(simpleMethodName) ? $"this.{EmbedHop}" : $"this.{EmbedHop}.Value";
                    }
                    else if (!methodOverriden && MultiEmbedHopPaths is not null && MultiEmbedHopPaths.TryGetValue(simpleMethodName, out string? multiHopPath))
                    {
                        receiver = $"this.{multiHopPath}";
                    }
                    else if (!methodOverriden && ValueEmbedHop is not null)
                    {
                        if (ValueEmbedHopStaticClass is not null)
                        {
                            string staticArgs = string.IsNullOrEmpty(method.CallParameters) ? $"this.{ValueEmbedHop}" : $"this.{ValueEmbedHop}, {method.CallParameters}";
                            result.Append($"{method.ReturnType} {method.GetSignature()} => {ValueEmbedHopStaticClass}.{simpleMethodName}{method.GetGenericSignature()}({staticArgs});");
                            continue;
                        }

                        receiver = $"this.{ValueEmbedHop}";
                    }

                    result.Append($"{method.ReturnType} {method.GetSignature()} => {receiver}.{forwardName}{method.GetGenericSignature()}({method.CallParameters});");
                }
            }

            return result.ToString();
        }
    }

    private string Comparisions
    {
        get
        {
            // Operators can only be public
            return OperatorScope != "public" ? 
                string.Empty : 
                $"""
                
                
                        // Handle comparisons between struct '{StructName}' and interface '{GetSimpleName(InterfaceName)}'
                        public static bool operator ==({StructName} src, {InterfaceName} iface) => iface is {StructName} val && val == src;
                        
                        public static bool operator !=({StructName} src, {InterfaceName} iface) => !(src == iface);
                        
                        public static bool operator ==({InterfaceName} iface, {StructName} src) => iface is {StructName} val && val == src;
                        
                        public static bool operator !=({InterfaceName} iface, {StructName} src) => !(iface == src);
                """;
        }
    }

    private string OperatorScope
    {
        get
        {
            string structNameScope = GetScope(StructName);
            string interfaceNameScope = GetScope(GetSimpleName(InterfaceName));
            return structNameScope == interfaceNameScope ? structNameScope : "internal";
        }
    }
}
