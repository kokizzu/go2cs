// InterfaceTypeTemplate.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

using System.Linq;
using System.Text;
using static go2cs.Common;
using static go2cs.Symbols;

namespace go2cs.Templates.InterfaceType;

internal class InterfaceTypeTemplate : TemplateBase
{
    // Template Parameters
    public required string InterfaceName;
    public required string[] OperatorConstraints;
    public required MethodInfo[] Methods;
    public required bool Dynamic;

    // Set for a NAMED, non-generic, non-constraint, non-empty interface: the two runtime
    // duck-typing shells are emitted beside it and discovered through a [GoInterfaceShell] stamp
    // (see InterfaceShellEmitter). A dyn interface keeps today's ᴛAs machinery until it is migrated
    // onto the same shape.
    public bool EmitShells;

    private InterfaceShellEmitter? m_shells;
    private string? m_nonGenericInterfaceName;
    private string? m_conversionTypeName;
    private string? m_nonGenericConversionTypeName;
    private string? m_implementedInterfaceName;

    private const string Indent = "        ";

    // Define a type T variables that will not conflict with any method generic types
    private const string TypeT = $"{ShadowVarMarker}T";
    private const string TypeTTarget = $"{TypeT}Target";

    public override string Generate()
    {
        string[] RequiredUsings = ["using System.Diagnostics;", "using System.Reflection;", "using go.golib;"];

        UsingStatements = UsingStatements is null ?
            RequiredUsings :
            UsingStatements.Concat(RequiredUsings).ToArray();

        return base.Generate();
    }

    public override string TemplateBody =>
        $$"""
              {{ShellAttribute}}[{{GeneratedCodeAttribute}}]
              {{Scope}} partial interface {{InterfaceName}}{{AppliedOperatorConstraints}}
              {{{RuntimeInterfaceConversionMethods}}
              }{{RuntimeInterfaceConversionType}}{{RuntimeInterfaceShells}}
          """;

    private InterfaceShellEmitter? Shells => EmitShells ? m_shells ??= new InterfaceShellEmitter(NonGenericInterfaceName, Scope, Methods) : null;

    private string ShellAttribute => Shells?.Attribute ?? "";

    private string RuntimeInterfaceShells => Shells?.Shells ?? "";

    private string AppliedOperatorConstraints
    {
        get
        {
            if (OperatorConstraints.Length == 0)
                return string.Empty;

            StringBuilder implementation = new();
            string constraints = string.Join(",\r\n", OperatorConstraints.SelectMany(GetConstraintName));

            implementation.AppendLine(" :");
            implementation.AppendLine(constraints);
            implementation.AppendLine($"{Indent}where {TypeT} :");
            implementation.Append(constraints);

            return implementation.ToString();
        }
    }

    private static string[] GetConstraintName(string name)
    {
        return name switch
        {
            "Sum" => [$"{Indent}IAdditionOperators<{TypeT}, {TypeT}, {TypeT}>"],
            // Increment/decrement live in the numeric-only Arithmetic set (never the
            // string-including Sum set) — mirrors the converter's lifted constraint list in
            // constraintOperations.go getLiftedConstraints; keep the two in sync.
            "Arithmetic" => [$"{Indent}ISubtractionOperators<{TypeT}, {TypeT}, {TypeT}>", $"{Indent}IMultiplyOperators<{TypeT}, {TypeT}, {TypeT}>", $"{Indent}IDivisionOperators<{TypeT}, {TypeT}, {TypeT}>", $"{Indent}IIncrementOperators<{TypeT}>", $"{Indent}IDecrementOperators<{TypeT}>", $"{Indent}IUnaryNegationOperators<{TypeT}, {TypeT}>"],
            // Shift-count parameter is int, matching the BCL IShiftOperators<TSelf, int, TSelf>
            // shape (see the converter's lifted Integer constraint set in constraintOperations.go).
            "Integer" => [$"{Indent}IModulusOperators<{TypeT}, {TypeT}, {TypeT}>", $"{Indent}IBitwiseOperators<{TypeT}, {TypeT}, {TypeT}>", $"{Indent}IShiftOperators<{TypeT}, int, {TypeT}>"],
            "Comparable" => [$"{Indent}IEqualityOperators<{TypeT}, {TypeT}, bool>"],
            "Ordered" => [$"{Indent}IComparisonOperators<{TypeT}, {TypeT}, bool>"],
            _ => [$"{Indent}{name}"]
        };
    }

    private string ConversionTypeName => m_conversionTypeName ??= $"{NonGenericConversionTypeName}<{TypeTTarget}>";

    // The Δ-prefixed wrapper name must compose on the BARE interface name: an interface whose Go name
    // is a C# reserved keyword is declared escaped (database/sql's `decimal` → `@decimal`), and `@` is
    // legal only at the START of a token, so `Δ@decimal` is invalid. The prefix also means the composed
    // name is never itself a keyword, so it needs no escape of its own. NonGenericInterfaceName keeps
    // the escaped form — it is used as a TYPE REFERENCE (ImplementedInterfaceName).
    private string NonGenericConversionTypeName => m_nonGenericConversionTypeName ??= $"{ShadowVarMarker}{GetUnsanitizedIdentifier(NonGenericInterfaceName)}";

    private string NonGenericInterfaceName => m_nonGenericInterfaceName ??= GetNonGenericInterfaceName();

    private string GetNonGenericInterfaceName()
    {
        int startIndex = InterfaceName.IndexOf('<');
        return startIndex < 0 ? InterfaceName : InterfaceName[..startIndex];
    }

    private string ImplementedInterfaceName => m_implementedInterfaceName ??= GetImplementedInterfaceName();

    private string GetImplementedInterfaceName()
    {
        int startIndex = InterfaceName.IndexOf('<');
        return startIndex < 0 ? InterfaceName : $"{NonGenericInterfaceName}<{ConversionTypeName}>";
    }

    private string RuntimeInterfaceConversionMethods
    {
        get
        {
            return !Dynamic ? "" : 
                $"""
                
                        // Runtime interface conversion methods
                        public static {ImplementedInterfaceName} {TempVarMarker}As<{TypeTTarget}>(in {TypeTTarget} target) =>
                            ({ConversionTypeName})target!;
                
                        public static {ImplementedInterfaceName} {TempVarMarker}As<{TypeTTarget}>({PointerPrefix}<{TypeTTarget}> target_ptr) =>
                            ({ConversionTypeName})target_ptr;
                
                """ +
                (InterfaceName == NonGenericInterfaceName ? 
                $"""
                
                        public static {InterfaceName}? {TempVarMarker}As(object target) =>
                            typeof({NonGenericConversionTypeName}<>).CreateInterfaceHandler<{InterfaceName}>(target);            
                """ : 
                $"""
                
                        public static {InterfaceName}? {TempVarMarker}As(object target) =>
                            ({InterfaceName}?)typeof({NonGenericConversionTypeName}<>).CreateInterfaceHandler<object>(target);             
                """);
        }
    }

    private string RuntimeInterfaceConversionType
    {
        get
        {
            return !Dynamic ? "" :
                $$"""
                
                
                    // Defines a runtime type for duck-typed interface implementations based on existing
                    // extension methods that satisfy interface. This class is only used as fallback for
                    // when the interface was not able to be implemented at transpile time, e.g., with
                    // dynamically declared anonymous interfaces used with type assertions.
                    [{{GeneratedCodeAttribute}}]
                    {{Scope}} class {{ConversionTypeName}} : {{ImplementedInterfaceName}}, IInterfaceAdapter
                    {
                        private {{TypeTTarget}} m_target = default!;
                        private readonly {{PointerPrefix}}<{{TypeTTarget}}>? m_target_ptr;
                        private readonly bool m_target_is_ptr;

                        public ref {{TypeTTarget}} Target
                        {
                            get
                            {
                                if (m_target_is_ptr && m_target_ptr is not null)
                                    return ref m_target_ptr.Value;

                                return ref m_target;
                            }
                        }

                        // The Go dynamic value this duck-typing wrapper stands in for — the receiver box
                        // (the *T) when pointer-backed, the wrapped struct value otherwise. Joining the
                        // IInterfaceAdapter unwrap protocol makes the wrapper transparent to interface
                        // equality (AreEqual), type() switches, GetGoTypeName, and re-asserts — so a
                        // reflection-driven interface store (reflect.Value.Set) compares equal to the
                        // original interface value it wrapped, and Go's value-vs-pointer method-set rule
                        // is preserved on re-assert (a value-backed wrapper unwraps to the STRUCT,
                        // exposing only its value method set). Explicit implementation: the wrapped
                        // interface may itself declare a member named Value.
                        object? IInterfaceAdapter.Value => m_target_is_ptr ? m_target_ptr : (object?)m_target;

                        public {{NonGenericConversionTypeName}}(in {{TypeTTarget}} target)
                        {
                            m_target = target;
                        }

                        public {{NonGenericConversionTypeName}}({{PointerPrefix}}<{{TypeTTarget}}> target_ptr)
                        {
                            m_target_ptr = target_ptr;
                            m_target_is_ptr = true;
                        }{{ReceiverMethodImplementations}}{{ReceiverMethodInitializations}}
                    
                        public static explicit operator {{ConversionTypeName}}(in {{PointerPrefix}}<{{TypeTTarget}}> target_ptr) => new(target_ptr);
                    
                        public static explicit operator {{ConversionTypeName}}(in {{TypeTTarget}} target) => new(target);
                
                        public override int GetHashCode() => Target?.GetHashCode() ?? 0;
                
                        public static bool operator ==({{ConversionTypeName}}? left, {{ConversionTypeName}}? right) => left?.Equals(right) ?? right is null;
                        
                        public static bool operator !=({{ConversionTypeName}}? left, {{ConversionTypeName}}? right) => !(left == right);
                
                        #region [ Operator Constraint Implementations ]
                
                        // These operator constraints exist to satisfy possible constraints defined on source interface,
                        // however, the instance of this class is only used to implement the interface methods, so these
                        // operators are only placeholders and not actually functional.
                
                        public static bool operator <({{ConversionTypeName}} left, {{ConversionTypeName}} right) => false;
                        
                        public static bool operator <=({{ConversionTypeName}} left, {{ConversionTypeName}} right) => false;
                        
                        public static bool operator >({{ConversionTypeName}} left, {{ConversionTypeName}} right) => false;
                        
                        public static bool operator >=({{ConversionTypeName}} left, {{ConversionTypeName}} right) => false;
                        
                        public static {{ConversionTypeName}} operator +({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator -({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator -({{ConversionTypeName}} value) => default!;
                        
                        public static {{ConversionTypeName}} operator *({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator /({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator %({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                
                        public static {{ConversionTypeName}} operator &({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator |({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator ^({{ConversionTypeName}} left, {{ConversionTypeName}} right) => default!;
                        
                        public static {{ConversionTypeName}} operator ~({{ConversionTypeName}} value) => default!;
                        
                        public static {{ConversionTypeName}} operator <<({{ConversionTypeName}} value, int shiftAmount) => default!;

                        public static {{ConversionTypeName}} operator >>({{ConversionTypeName}} value, int shiftAmount) => default!;

                        public static {{ConversionTypeName}} operator >>>({{ConversionTypeName}} value, int shiftAmount) => default!;
                        
                        #endregion
                    
                        // Enable comparisons between nil and {{ConversionTypeName}} interface instance
                        public static bool operator ==({{ConversionTypeName}} value, NilType nil) => global::System.Activator.CreateInstance<{{ConversionTypeName}}>().Equals(value);
                    
                        public static bool operator !=({{ConversionTypeName}} value, NilType nil) => !(value == nil);
                    
                        public static bool operator ==(NilType nil, {{ConversionTypeName}} value) => value == nil;
                    
                        public static bool operator !=(NilType nil, {{ConversionTypeName}} value) => value != nil;
                    }
                """;
        }
    }

    private string ReceiverMethodImplementations
    {
        get
        {
            if (Methods.Length == 0)
                return "";

            StringBuilder results = new();

            foreach (MethodInfo method in Methods)
                results.Append(GetReceiverMethodImplementation(method));

            return results.ToString();
        }
    }

    private string GetReceiverMethodImplementation(MethodInfo method)
    {
        // Compound member names (the ByPtr/ByVal delegate types and their s_ backing fields) must
        // compose on the BARE identifier: C#'s `@` verbatim prefix is legal only at the START of a
        // token, so composing on the escaped name of a keyword-named Go method (testing.TB's
        // @private) emitted `s_@privateByPtr`, which Roslyn tokenizes as `s_` + `@privateByPtr` —
        // corrupting the class body (CS0102 on a phantom `s_` member). The DECLARATION keeps the
        // escaped form (method.GetSignature escapes it).
        string bareName = GetUnsanitizedIdentifier(method.Name);
        // The forwarding method's receiver LOCAL takes the same marker-suffixed name the ByPtr/ByVal
        // delegates already give their receiver parameter. The plain name `target` collided with an
        // interface-method parameter of the same name — net/http.Pusher's `Push(target string, opts
        // *PushOptions) error` — which is CS0136, and had it compiled the forwarded call would have
        // handed the RECEIVER to the parameter's slot (`s_PushByVal!(target, target)`, CS1503). One
        // name for the receiver throughout the wrapper removes the collision class; a Go parameter
        // cannot reach the marker glyph through the converter's identifier rendering.
        string receiverLocal = $"target{CapturedVarMarker}";

        return $$"""

                     
                         // Implementation for '{{NonGenericInterfaceName}}.{{method.Name}}' receiver method 
                         private delegate {{method.ReturnType}} {{bareName}}ByPtr{{method.GetGenericSignature()}}({{PointerPrefix}}<{{TypeTTarget}}> {{receiverLocal}}{{getCommaPrefixedTypedParameters()}}){{method.GetWhereConstraints()}};
                         private delegate {{method.ReturnType}} {{bareName}}ByVal{{method.GetGenericSignature()}}({{TypeTTarget}} {{receiverLocal}}{{getCommaPrefixedTypedParameters()}}){{method.GetWhereConstraints()}};
                         
                         private static readonly {{bareName}}ByPtr? s_{{bareName}}ByPtr;
                         private static readonly {{bareName}}ByVal? s_{{bareName}}ByVal;
                         
                         [global::System.Diagnostics.DebuggerNonUserCode]
                         public {{method.ReturnType}} {{method.GetSignature(false)}}
                         {
                             {{TypeTTarget}} {{receiverLocal}} = m_target;
                         
                             if (m_target_is_ptr && m_target_ptr is not null)
                                 {{receiverLocal}} = m_target_ptr.Value;
                         
                             if (s_{{bareName}}ByPtr is null || !m_target_is_ptr)
                                 {{getReturnStatement()}}s_{{bareName}}ByVal!({{receiverLocal}}{{getCommaPrefixedCallParameters()}});
                             else
                                 {{getReturnStatement()}}s_{{bareName}}ByPtr!(m_target_ptr!{{getCommaPrefixedCallParameters()}});
                         }
                 """;

        string getCommaPrefixedTypedParameters()
        {
            string typeParameters = method.TypedParameters;

            if (typeParameters.Length > 0)
                typeParameters = $", {typeParameters}";

            return typeParameters;
        }

        string getCommaPrefixedCallParameters()
        {
            string callParameters = method.GetCallParameters(false);

            if (callParameters.Length > 0)
                callParameters = $", {callParameters}";

            return callParameters;
        }

        string getReturnStatement()
        {
            return method.ReturnType == "void" ? "" : "return ";
        }
    }

    private string ReceiverMethodInitializations
    {
        get
        {
            if (Methods.Length == 0)
                return "";

            StringBuilder results = new();

            results.Append(
                $$"""
                  
                  
                          static {{NonGenericConversionTypeName}}()
                          {
                              global::System.Type targetType = typeof({{TypeTTarget}});
                              global::System.Type targetTypeByPtr = typeof({{PointerPrefix}}<{{TypeTTarget}}>);
                              global::System.Reflection.MethodInfo? extensionMethod;
                  """);

            foreach (MethodInfo method in Methods)
                results.Append(GetReceiverMethodInitialization(method));

            results.AppendLine();
            results.Append("        }");

            return results.ToString();
        }
    }

    private string GetReceiverMethodInitialization(MethodInfo method)
    {
        // `nameof(...)` takes the method name as a bare identifier, so a Go sealing method whose
        // name is a C# reserved keyword (testing.TB.private()) must be `@`-escaped here — Roslyn
        // hands back the UNescaped name for an inherited (AllInterfaces) member. The compound
        // delegate/field names (`{Name}ByPtr`, `s_{Name}ByPtr`) must instead compose on the BARE
        // name: keyword + suffix is never itself a keyword, and `@` cannot appear mid-token, so a
        // SYNTAX-sourced (already-escaped) name — a keyword-named method declared DIRECTLY on the
        // dyn interface — composed the invalid `s_@privateByPtr`. Must stay in lockstep with
        // GetReceiverMethodImplementation, which declares the very members named here.
        string escapedName = EscapeCsKeyword(method.Name);
        string bareName = GetUnsanitizedIdentifier(method.Name);

        return $$"""


                             // Initialization of '{{NonGenericInterfaceName}}.{{method.Name}}' receiver method implementation
                             extensionMethod = targetTypeByPtr.GetExtensionMethod(nameof({{escapedName}}));

                             if (extensionMethod is not null)
                                 s_{{bareName}}ByPtr = extensionMethod.CreateStaticDelegate(typeof({{bareName}}ByPtr)) as {{bareName}}ByPtr;

                             extensionMethod = targetType.GetExtensionMethod(nameof({{escapedName}}));

                             if (extensionMethod is not null)
                                 s_{{bareName}}ByVal = extensionMethod.CreateStaticDelegate(typeof({{bareName}}ByVal)) as {{bareName}}ByVal;

                             if (s_{{bareName}}ByPtr is null && s_{{bareName}}ByVal is null)
                                 throw new global::System.NotImplementedException($"{targetType.FullName} does not implement '{{NonGenericInterfaceName}}.{nameof({{escapedName}})}' method");
                 """;
    }
}
