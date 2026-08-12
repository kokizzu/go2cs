// TypeGenerator.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

//#define DEBUG_GENERATOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using go2cs.Templates.InheritedType;
using go2cs.Templates.InterfaceType;
using go2cs.Templates.StructType;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static go2cs.Common;
using static go2cs.Symbols;

#if DEBUG_GENERATOR
using System.Diagnostics;
#endif

namespace go2cs;

[Generator]
public class TypeGenerator : ISourceGenerator
{
    private const string Namespace = "go";
    private const string AttributeName = "GoType";
    private const string FullAttributeName = $"{Namespace}.{AttributeName}Attribute";
    private const string ValueCloneAttributeName = "GoValueClone";

    public void Initialize(GeneratorInitializationContext context)
    {
    #if DEBUG_GENERATOR
        if (!Debugger.IsAttached)
            Debugger.Launch();
    #endif

        // Register to find "GoTypeAttribute" on type declarations
        context.RegisterForSyntaxNotifications(() => new AttributeFinder<BaseTypeDeclarationSyntax>(FullAttributeName));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not AttributeFinder<BaseTypeDeclarationSyntax> { HasAttributes: true } attributeFinder)
            return;

        HashSet<string> emittedHintNames = new(StringComparer.OrdinalIgnoreCase);

        foreach ((BaseTypeDeclarationSyntax targetSyntax, List<AttributeSyntax> attributes) in attributeFinder.TargetAttributes)
        {
            SyntaxTree syntaxTree = targetSyntax.SyntaxTree;
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

            string packageNamespace = targetSyntax.GetNamespaceName();
            string packageClassName = targetSyntax.GetParentClassName();
            string packageName = packageClassName.EndsWith(PackageSuffix) ? packageClassName[..^PackageSuffix.Length] : packageClassName;
            string identifier = targetSyntax.Identifier.Text;
            bool hasEqualityOperators = true;

            // Add generic type parameters to the identifier
            if (targetSyntax is TypeDeclarationSyntax { TypeParameterList.Parameters.Count: > 0 } typeDecl)
            {
                IEnumerable<string> typeParamNames = typeDecl.TypeParameterList.Parameters.Select(p => p.Identifier.Text);
                identifier += $"<{string.Join(", ", typeParamNames)}>";
                hasEqualityOperators = typeDecl.AllGenericTypesHaveConstraint(semanticModel, "System.Numerics.IEqualityOperators`3");
            }

            string fullyQualifiedIdentifier = semanticModel.GetDeclaredSymbol(targetSyntax)?.ToDisplayString() ?? $"{packageNamespace}.{packageClassName}.{identifier}";
            
            // Since many types are referenced by assembly attributes outside namespace,
            // "internal" scope is used so types can be referenced instead of "private".
            // An explicit modifier on the converter's partial declaration wins (e.g. an
            // unexported type publicized because it is an exported field's type — CS0051/CS0052).
            string scope = GetExplicitAccessModifier(targetSyntax) ?? GetScope(identifier);

            string[] usingStatements = GetFullyQualifiedUsingStatements(syntaxTree, semanticModel);

            // Fields the converter marked as needing a DEEP copy on a Go by-value struct copy
            // (see GoValueCloneAttribute / StructTypeTemplate.ValueCloneImplementation).
            string[] valueCloneFields = GetValueCloneFields(targetSyntax, semanticModel);

            foreach (AttributeSyntax attribute in attributes)
            {
                // Get the attribute's argument values
                (string _, string value)[] arguments = attribute.GetArgumentValues();

                // Get the attribute's first constructor argument value, the type definition
                string typeDefinition = string.Empty;

                if (arguments.Length > 0)
                {
                    string value = arguments[0].value;
                    
                    if (!string.IsNullOrWhiteSpace(value) && value.Length > 2)
                        typeDefinition = value[1..^1].Trim();
                }

                string generatedSource, typeName;

                switch (targetSyntax)
                {
                    case StructDeclarationSyntax structDeclaration when string.IsNullOrWhiteSpace(typeDefinition) || typeDefinition.Equals("dyn"):
                        generatedSource = new StructTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            Scope = scope,
                            Context = context,
                            StructName = identifier,
                            FullyQualifiedStructType = fullyQualifiedIdentifier,
                            StructMembers = structDeclaration.GetStructMembers(context.Compilation, true),
                            HasEqualityOperators = hasEqualityOperators,
                            // A generic struct that failed the whole-struct constraint gate still
                            // gets a real memberwise Equals: each member whose type supports ==
                            // independent of the unconstrained type parameters compares with ==,
                            // and only the rest fall back to golib's AreEqual (never a blanket
                            // `false`, which broke ==-independent structs like unique.Handle<T>).
                            EqualityFallbackMembers = hasEqualityOperators ? null : structDeclaration.GetEqualityFallbackMembers(context.Compilation),
                            ValueCloneFields = valueCloneFields,
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;
                    
                    case StructDeclarationSyntax when typeDefinition.StartsWith("[]"): // slice
                        typeName = QualifySourceAliasReferences(typeDefinition[2..].Trim(), syntaxTree, semanticModel);

                        // m_value stays MUTABLE for a named-slice wrapper: a Go pointer-reinterpret to
                        // the underlying slice — `(*[][]byte)(buf)` with `buf *Buffers`, net
                        // fd_windows.go — projects a ж<slice<T>> VIEW over the wrapper's own field
                        // (`Ꮡbuf.of(Buffers.Ꮡm_value)`), so header writes through the view (poll
                        // FD.Writev's consume reslicing) land on the original (a readonly field would
                        // force a defensive copy and lose them).
                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            Scope = scope,
                            TypeName = $"slice<{typeName}>",
                            TargetTypeName = typeName,
                            TypeClass = "Slice",
                            ReadOnlyValue = false
                        }
                        .Generate();

                        break;

                    case StructDeclarationSyntax when typeDefinition.StartsWith("map["):
                        (string keyTypeName, string valueTypeName) = SplitMapTypes(typeDefinition);
                        keyTypeName = QualifySourceAliasReferences(keyTypeName, syntaxTree, semanticModel);
                        valueTypeName = QualifySourceAliasReferences(valueTypeName, syntaxTree, semanticModel);

                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            Scope = scope,
                            ObjectName = identifier,
                            TypeName = $"map<{keyTypeName}, {valueTypeName}>",
                            TargetTypeName = keyTypeName,
                            TargetValueTypeName = valueTypeName,
                            TypeClass = "Map",
                            UsingStatements = usingStatements

                        }
                        .Generate();

                        break;

                    case StructDeclarationSyntax when typeDefinition.StartsWith("chan "):
                        typeName = QualifySourceAliasReferences(typeDefinition[5..].Trim(), syntaxTree, semanticModel);

                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            Scope = scope,
                            TypeName = $"channel<{typeName}>",
                            TargetTypeName = typeName,
                            TypeClass = "Channel",
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;
                    
                    case StructDeclarationSyntax when typeDefinition.StartsWith("["): // array
                        int sizeStart = typeDefinition.IndexOf('[') + 1;
                        int sizeEnd = typeDefinition.IndexOf(']');
                        string arraySize = typeDefinition[sizeStart..sizeEnd].Trim();
                        typeName = QualifySourceAliasReferences(typeDefinition[(sizeEnd + 1)..].Trim(), syntaxTree, semanticModel);

                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            ReadOnlyValue = false,
                            Scope = scope,
                            TypeName = $"array<{typeName}>",
                            TargetTypeName = typeName,
                            TargetTypeSize = arraySize,
                            TypeClass = "Array",
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;

                    case StructDeclarationSyntax when typeDefinition.StartsWith("num:"): // numeric
                        typeName = typeDefinition[4..].Trim();

                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            Scope = $"{scope} readonly",
                            TypeName = typeName,
                            TargetTypeName = identifier,
                            TypeClass = "Numeric",
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;

                    case StructDeclarationSyntax when !string.IsNullOrWhiteSpace(typeDefinition):
                        typeName = typeDefinition;

                        // A defined type whose underlying is a STRUCT (`type winlibcall libcall`) exposes
                        // the underlying struct's fields in Go (`w.fn`). Resolve the underlying struct
                        // (same-package or a source-referenced package) and forward its members as get/set
                        // properties over a MUTABLE m_value, so `box.Value.fn = x` (a write through a
                        // ж<T>.Value ref) persists. Non-struct underlyings (a named type over an interface or
                        // another named type) resolve to null and keep the plain wrapper (no churn).
                        List<(string typeName, string memberName, bool isReferenceType, bool isProperty)>? forwardedMembers = null;
                        string? underlyingArrayElem = null;
                        bool mutableValue = false;

                        (StructDeclarationSyntax? underlyingStruct, Compilation? underlyingCompilation) = context.GetStructDeclaration(typeDefinition);

                        if (underlyingStruct is not null && underlyingCompilation is not null)
                        {
                            List<(string typeName, string memberName, bool isReferenceType, bool isProperty)> members = underlyingStruct.GetStructMembers(underlyingCompilation, false);

                            // Only forward + go mutable when the underlying actually contributes fields.
                            // An empty result (e.g. a named type over an array-typed named struct whose
                            // members are generated, not declared) keeps the plain readonly wrapper — no ripple.
                            if (members.Count > 0)
                            {
                                forwardedMembers = members;
                                mutableValue = true;
                            }
                            else
                            {
                                // A defined type over an ARRAY-backed [GoType] wrapper — `type pallocBits
                                // pageBits` where `type pageBits [8]uint64` — is len()'d / indexed directly
                                // in Go, which needs IArray on THIS wrapper (golib `len(IArray)`, CS1503
                                // otherwise; runtime mpallocbits.go). Detect it from the underlying's own
                                // [GoType] definition (`[N]elem`, not a `[]` slice) and implement
                                // IArray<elem> as a view over m_value (IArrayViewTypeTemplate).
                                string? underlyingDefinition = GetGoTypeDefinition(underlyingStruct);

                                if (underlyingDefinition is not null && underlyingDefinition.StartsWith("[") && !underlyingDefinition.StartsWith("[]"))
                                {
                                    int closeBracket = underlyingDefinition.IndexOf(']');

                                    if (closeBracket > 0 && closeBracket < underlyingDefinition.Length - 1)
                                    {
                                        underlyingArrayElem = underlyingDefinition[(closeBracket + 1)..].Trim();

                                        // The view's ref accessor must ensure the underlying's lazily-
                                        // allocated backing lands on THIS wrapper's own m_value (a
                                        // readonly field would force a defensive copy and lose writes).
                                        mutableValue = true;
                                    }
                                }
                            }
                        }
                        else if (context.FindUnderlyingStructSymbol(typeDefinition) is { } underlyingSymbol)
                        {
                            // The underlying struct's SOURCE is not in this compilation — the normal
                            // shape in a real MSBuild build, where a <ProjectReference> arrives as
                            // compiled METADATA and the referenced-compilations walk above finds
                            // nothing. Resolve it by SYMBOL instead: a defined type over a FOREIGN
                            // struct exposes that struct's fields in Go exactly as a same-package one
                            // does (`type index Index` in a white-box _test.go reading `x.sa`;
                            // `type P otherpkg.Point` reading `p.X`), so it needs the same forwarding.
                            List<(string typeName, string memberName, bool isReferenceType, bool isProperty)> members =
                                StructDeclarationSyntaxExtensions.GetForeignStructMembers(underlyingSymbol, context.Compilation);

                            if (members.Count > 0)
                            {
                                forwardedMembers = members;
                                mutableValue = true;
                            }
                        }

                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            Scope = scope,
                            ReadOnlyValue = !mutableValue,
                            TypeName = typeName,
                            TargetTypeName = typeName,
                            TypeClass = typeDefinition,
                            ForwardedStructMembers = forwardedMembers,
                            UnderlyingArrayElementType = underlyingArrayElem,
                            ValueClone = valueCloneFields.Length > 0,
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;

                    case StructDeclarationSyntax:
                        throw new NotSupportedException($"Unsupported [{AttributeName}] definition \"{typeDefinition}\" on struct \"{identifier}\".");

                    case InterfaceDeclarationSyntax interfaceDeclaration:
                        string[]? operatorConstraints = null;

                        if (!string.IsNullOrWhiteSpace(typeDefinition))
                        {
                            string[] keys = typeDefinition.Split([';'], StringSplitOptions.RemoveEmptyEntries);

                            foreach (string key in keys)
                            {
                                string[] parts = key.Split(["="], StringSplitOptions.RemoveEmptyEntries);

                                if (parts.Length > 1 && parts[0].Trim().Equals("operators", StringComparison.OrdinalIgnoreCase))
                                    operatorConstraints = parts[1].Split([','], StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).ToArray();
                            }
                        }

                        usingStatements = usingStatements.Append("using System.Numerics;").ToArray();

                        // A CONSTRAINT interface (operators=…) exists to carry C# operator constraints
                        // and a GENERIC one has no single runtime instantiation to bind against, so
                        // neither has a Go method set a runtime shell could satisfy — and an interface
                        // with no methods is satisfied by every value nominally already.
                        //
                        // The "dyn" key is NOT read here: an anonymous interface takes exactly the same
                        // shells a named one does. The key once selected a second renderer — the ᴛAs
                        // conversion methods and their Δ wrapper — which is retired now that dyn rides
                        // on the shells, so the ONLY remaining reader of "dyn" is the runtime's
                        // Type.IsDynamicType (struct-to-struct dynamic conversion), and it reads the
                        // [GoType] attribute directly rather than going through here.
                        bool shellEligible = operatorConstraints is null &&
                            interfaceDeclaration.TypeParameterList is null or { Parameters.Count: 0 };

                        MethodInfo[] interfaceMethods = shellEligible ?
                            interfaceDeclaration.GetInterfaceMethods(context) :
                            [];

                        generatedSource = new InterfaceTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            Scope = scope,
                            InterfaceName = identifier,
                            OperatorConstraints = operatorConstraints ?? [],
                            Methods = interfaceMethods,
                            // A member declared with a ref-kind modifier cannot be re-declared
                            // faithfully from the recorded parameter types, so an interface carrying
                            // one gets no shell rather than one that fails to implement it (CS0535).
                            EmitShells = shellEligible && interfaceMethods.Length > 0 &&
                                interfaceMethods.All(method => method.IsSignatureRenderable),
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;

                    case ClassDeclarationSyntax when typeDefinition.StartsWith($"{PointerPrefix}<"): // pointer
                        typeName = typeDefinition[2..^1];
                        
                        generatedSource = new InheritedTypeTemplate
                        {
                            PackageNamespace = packageNamespace,
                            PackageName = packageName,
                            ObjectName = identifier,
                            ObjectKind = "class",
                            Scope = scope,
                            TypeName = $"{PointerPrefix}<{typeName}>",
                            TargetTypeName = typeName,
                            TypeClass = "Pointer",
                            UsingStatements = usingStatements
                        }
                        .Generate();

                        break;

                    default:
                        throw new NotSupportedException($"Unsupported [{AttributeName}] on {targetSyntax.GetType().Name} type \"{identifier}\".");
                }

                // Add the source code to the compilation
                context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{identifier}.g.cs")), generatedSource);
            }
        }
    }

    // Reads the field names out of the converter's [GoValueClone("f1", "f2")] stamp — the fields a
    // Go by-value copy of this struct must DEEP-copy (see GoValueCloneAttribute). Matched by syntax
    // name, like every other attribute this generator reads; the converter emits it unqualified.
    //
    // Scanned over EVERY partial declaration of the type, not just the [GoType] one this generator
    // was handed: the converter writes the stamp on the package_info.cs accessibility record so the
    // mainline declaration reads like the Go original, and a hand-owned conversion — which gets no
    // such record — keeps it inline. C# unions a partial type's attributes, so both are the same
    // stamp on the same type.
    private static string[] GetValueCloneFields(BaseTypeDeclarationSyntax targetSyntax, SemanticModel semanticModel)
    {
        foreach (BaseTypeDeclarationSyntax declaration in GetPartialDeclarations(targetSyntax, semanticModel))
        {
            foreach (AttributeListSyntax attributeList in declaration.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    string attributeName = GetSimpleName(attribute.Name.ToString());

                    if (attributeName != ValueCloneAttributeName && attributeName != $"{ValueCloneAttributeName}Attribute")
                        continue;

                    return attribute.GetArgumentValues()
                        .Select(argument => argument.value.Trim())
                        .Where(value => value.Length > 2 && value[0] == '"' && value[value.Length - 1] == '"')
                        .Select(value => value[1..^1])
                        .ToArray();
                }
            }
        }

        return [];
    }

    // Every declaration that makes up the (partial) type, starting with the one the syntax receiver
    // matched. A type whose symbol cannot be resolved yields just that declaration, which is what
    // this generator read before the accessibility record existed to carry attributes.
    private static IEnumerable<BaseTypeDeclarationSyntax> GetPartialDeclarations(BaseTypeDeclarationSyntax targetSyntax, SemanticModel semanticModel)
    {
        yield return targetSyntax;

        if (semanticModel.GetDeclaredSymbol(targetSyntax) is not INamedTypeSymbol typeSymbol)
            yield break;

        foreach (SyntaxReference reference in typeSymbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not BaseTypeDeclarationSyntax declaration)
                continue;

            // Skip the one already yielded. Identity is tree + span: a SyntaxReference materializes
            // its node on demand, so reference equality is not guaranteed to hold against the node
            // the receiver matched.
            if (declaration.SyntaxTree == targetSyntax.SyntaxTree && declaration.Span == targetSyntax.Span)
                continue;

            yield return declaration;
        }
    }

    private static (string keyTypeName, string valueTypeName) SplitMapTypes(string typeDefinition)
    {
        string mapTypes = typeDefinition[4..^1];
        int depth = 0;

        for (int i = 0; i < mapTypes.Length; i++)
        {
            char ch = mapTypes[i];

            switch (ch)
            {
                case '<':
                case '[':
                case '(':
                    depth++;
                    break;
                case '>':
                case ']':
                case ')':
                    if (depth > 0)
                        depth--;
                    break;
                case ',' when depth == 0:
                    return (mapTypes[..i].Trim(), mapTypes[(i + 1)..].Trim());
            }
        }

        return (mapTypes.Trim(), string.Empty);
    }

    private static string QualifySourceAliasReferences(string typeName, SyntaxTree syntaxTree, SemanticModel semanticModel)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        string result = typeName;

        foreach (UsingDirectiveSyntax directive in syntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive is not { Alias: not null, Name: not null } || !directive.GlobalKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None))
                continue;

            string alias = directive.Alias.Name.Identifier.Text;

            if (result.IndexOf($"{alias}.", StringComparison.Ordinal) < 0)
                continue;

            ISymbol? symbol = semanticModel.GetSymbolInfo(directive.Name).Symbol;
            string target = symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? directive.Name.ToString();

            // Two descriptor conventions coexist: the SOURCE-ALIAS form ("CrossPkgLib.Ticks",
            // the map key/value emitter) that this substitution resolves, and the NAMESPACE-
            // QUALIFIED form ("io.fs_package.FileInfo", the slice/array element and defined-
            // over-selector emitters) that must pass through untouched. They are told apart by
            // the segment AFTER the leading identifier: a real alias maps to a package CLASS,
            // so the next segment is a TYPE name — a "_package"-suffixed next segment means the
            // leading identifier is a namespace segment that merely COLLIDES with a file alias
            // (net/http's fs.go aliases `io` while `[]io.fs_package.FileInfo` roots io/fs), and
            // substituting it mangles the reference (CS0426 ×48). The negative lookahead skips
            // exactly those occurrences.
            result = Regex.Replace(result, $@"(^|[<,\s\(\[]){Regex.Escape(alias)}\.(?![^.<>,\s\(\)\[\]]*{Regex.Escape(PackageSuffix)}\.)", $"$1{target}.");
        }

        return GlobalQualify(result);
    }
    // Reads a struct declaration's own [GoType("…")] definition string (first constructor argument,
    // quotes stripped) — used to inspect the UNDERLYING type of a defined-over-defined chain
    // (`type pallocBits pageBits`: pageBits' definition is "[8]uint64"). Null when the struct has no
    // GoType attribute or no argument.
    private static string? GetGoTypeDefinition(StructDeclarationSyntax structDeclaration)
    {
        foreach (AttributeListSyntax attributeList in structDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                string name = attribute.Name.ToString();

                if (name != AttributeName && name != $"{AttributeName}Attribute")
                    continue;

                (string _, string value)[] arguments = attribute.GetArgumentValues();

                if (arguments.Length > 0)
                {
                    string value = arguments[0].value;

                    if (!string.IsNullOrWhiteSpace(value) && value.Length > 2)
                        return value[1..^1].Trim();
                }
            }
        }

        return null;
    }
}
