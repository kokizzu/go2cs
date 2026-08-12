// ImplementGenerator.cs - Gbtc
// Copyright © 2026 The go2cs Authors. All rights reserved.
//
// Use of this source code is governed by an MIT-style license
// that can be found in the LICENSE file.

//#define DEBUG_GENERATOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using go2cs.Templates.InterfaceImpl;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static go2cs.Common;
using static go2cs.Symbols;

#if DEBUG_GENERATOR
using System.Diagnostics;
#endif

namespace go2cs;

[Generator]
public class ImplementGenerator : ISourceGenerator
{
    private const string Namespace = "go";
    private const string AttributeName = "GoImplement";
    private const string FullAttributeName = $"{Namespace}.{AttributeName}Attribute<TStruct, TInterface>";

    // Renders a namespace for a `using` directive: no `global::`, keyword segments escaped
    // (`go.crypto.@internal`, not the invalid `go.crypto.internal`).
    private static readonly SymbolDisplayFormat s_namespaceUsingFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public void Initialize(GeneratorInitializationContext context)
    {
    #if DEBUG_GENERATOR
        if (!Debugger.IsAttached)
            Debugger.Launch();
    #endif

        // Register to find "GoImplementAttribute" on assembly attribute declarations
        context.RegisterForSyntaxNotifications(() => new AssemblyAttributeFinder(FullAttributeName));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not AssemblyAttributeFinder { HasAttributes: true } attributeFinder)
            return;

        // Roslyn hintNames are case-insensitive; Go type names differing only by case are legal
        // and common, so disambiguate like the other generators (a collision throws and suppresses
        // ALL interface implementations for the package).
        HashSet<string> emittedHintNames = new(StringComparer.OrdinalIgnoreCase);

        // A (struct, interface) pair can carry Pointer and Promoted on SEPARATE attribute
        // instances (the converter records the ж-form and the embed-promotion independently);
        // the pointer-adapter emission needs to know the pair is promoted, so pre-index.
        HashSet<string> promotedPairs = new(StringComparer.Ordinal);

        // Emit ONE value-form implementation per (struct, interface) pair: the converter can
        // record the same pair both plain AND Promoted (archive/tar's lifted anon struct got
        // a promotion record from its interface embed and a plain record from the conversion
        // site) — the second partial re-emitted the comparison operators (CS0111 ×8). The
        // promotedPairs pre-index already folds the Promoted flag into the first emission.
        HashSet<string> emittedValuePairs = new(StringComparer.Ordinal);

        // Emit each explicit interface MEMBER once per struct across its partial-struct impls.
        // A type implementing both an interface AND a super-interface that EMBEDS it (File :
        // io.Closer, mime/multipart's sectionReadCloser) would emit the inherited member's
        // explicit impl (io_package.Closer.Close()) in BOTH the io.Closer partial and the File
        // partial → CS0111/CS8646. The earlier partial's explicit impl already satisfies the
        // member for the whole struct, so later partials skip it. Keyed by (struct, member) and
        // scoped to the partial-struct path — adapter classes (structNameжInterfaceName) are
        // distinct per interface and must keep their full method set, so they do not consult it.
        HashSet<string> emittedPartialMembers = new(StringComparer.Ordinal);

        // A GENERIC struct is recorded once per INSTANTIATION (nistCurve<ж<P224Point>>,
        // nistCurve<ж<P384Point>>, …) but generates ONE generic adapter over the struct's OPEN
        // form (nistCurveжCurve<Point>). All those closed records collapse to the same open
        // (struct, interface) pair here, so emit the class only for the first — a second would
        // redeclare it (CS0102). Keyed by the open definition + interface.
        HashSet<string> emittedGenericPointerAdapters = new(StringComparer.Ordinal);
        HashSet<string> emittedInterfaceAdapters = new(StringComparer.Ordinal);

        foreach ((AttributeSyntax attributeSyntax, GeneratorSyntaxContext syntaxContext, _, _) in attributeFinder.TargetAttributes)
        {
            (string name, string value)[] arguments = attributeSyntax.GetArgumentValues();

            if (!bool.Parse(arguments.FirstOrDefault(arg => arg.name.Equals("Promoted")).value?.Trim() ?? "false"))
                continue;

            (ITypeSymbol? structType, ITypeSymbol? interfaceType) = attributeSyntax.Get2GenericTypeArguments(syntaxContext);

            if (structType is not null && interfaceType is not null)
                promotedPairs.Add($"{structType.ToDisplayString()}|{interfaceType.ToDisplayString()}");
        }

        // A pointer adapter is named "[<pkg>_]<structSimple>ж<ifaceSimple>". The STRUCT side is
        // package-qualified when foreign, but the INTERFACE side composes from its bare simple
        // name — ambiguous as soon as one struct adapts to two interfaces sharing a simple name.
        // compress/flate does: its own `Reader` and `io.Reader`, both reached from *bufio.Reader
        // and *bytes.Reader in its tests, composed one class TWICE (CS0102 + CS0111 + CS8646).
        // Collect the names more than one interface maps to, so the composition below can qualify
        // just those. COLLISION-CONDITIONAL by design: unconditional qualification would rename
        // 644 adapters across 3,688 construction sites, and the production corpus has no
        // collisions at all. Keep the rule in sync with the converter's adapterNameCollisions.go,
        // which resolves the matching cast-site references from these same records.
        Dictionary<string, HashSet<string>> adapterNameGroups = new(StringComparer.Ordinal);

        foreach ((AttributeSyntax attributeSyntax, GeneratorSyntaxContext syntaxContext, CompilationUnitSyntax compilationUnit, _) in attributeFinder.TargetAttributes)
        {
            (string name, string value)[] arguments = attributeSyntax.GetArgumentValues();

            if (!bool.Parse(arguments.FirstOrDefault(arg => arg.name.Equals("Pointer")).value?.Trim() ?? "false"))
                continue;

            (ITypeSymbol? structType, ITypeSymbol? interfaceType) = attributeSyntax.Get2GenericTypeArguments(syntaxContext);

            if (structType is null || interfaceType is null)
                continue;

            string packageClass = GetFirstClassName(compilationUnit) ?? string.Empty;
            string unqualified = $"{AdapterStructKey(structType, packageClass)}{PointerPrefix}{GetUnsanitizedIdentifier(GetSimpleName(interfaceType.ToDisplayString()))}";

            if (!adapterNameGroups.TryGetValue(unqualified, out HashSet<string>? interfaces))
                adapterNameGroups[unqualified] = interfaces = new HashSet<string>(StringComparer.Ordinal);

            interfaces.Add(interfaceType.ToDisplayString());
        }

        HashSet<string> collidingAdapterNames = new(adapterNameGroups.Where(entry => entry.Value.Count > 1).Select(entry => entry.Key), StringComparer.Ordinal);

        foreach ((AttributeSyntax attributeSyntax, GeneratorSyntaxContext syntaxContext, CompilationUnitSyntax compilationUnit, FileScopedNamespaceDeclarationSyntax? namespaceSyntax) in attributeFinder.TargetAttributes)
        {
            SyntaxTree syntaxTree = attributeSyntax.SyntaxTree;
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

            string packageNamespace = GetNamespace(namespaceSyntax) ?? Namespace;
            string packageClassName = GetFirstClassName(compilationUnit) ?? throw new MissingMemberException($"No package class found in same file as [assembly: {AttributeName}]");
            string packageName = packageClassName.EndsWith(PackageSuffix) ? packageClassName[..^PackageSuffix.Length] : packageClassName;
            
            string[] usingStatements = GetFullyQualifiedUsingStatements(syntaxTree, semanticModel);

            // Extract generic type arguments from "GoImplementAttribute"
            (ITypeSymbol? structType, ITypeSymbol? interfaceType) = attributeSyntax.Get2GenericTypeArguments(syntaxContext);
            
            if (structType is null || interfaceType is null)
                throw new InvalidOperationException($"Invalid usage of [assembly: {AttributeName}] attribute, must specify two generic type arguments.");

            if (interfaceType.TypeKind != TypeKind.Interface)
                throw new InvalidOperationException($"Invalid usage of [assembly: {AttributeName}] attribute, second generic type argument must be an interface.");

            string structName = structType.GetFullTypeName();
            string interfaceName = GlobalQualify(interfaceType.GetFullTypeName(true));

            // Get the attribute's Promoted / Pointer / ConstraintProxy argument values, if defined
            (string name, string value)[] arguments = attributeSyntax.GetArgumentValues();
            bool promoted = bool.Parse(arguments.FirstOrDefault(arg => arg.name.Equals("Promoted")).value?.Trim() ?? "false");
            bool pointer = bool.Parse(arguments.FirstOrDefault(arg => arg.name.Equals("Pointer")).value?.Trim() ?? "false");
            bool constraintProxy = bool.Parse(arguments.FirstOrDefault(arg => arg.name.Equals("ConstraintProxy")).value?.Trim() ?? "false");

            if (structType.TypeKind == TypeKind.Interface)
            {
                List<MethodInfo> interfaceAdapterMethods = ((INamedTypeSymbol)interfaceType).GetAllBaseInterfaces(context.Compilation)
                    .Concat([(INamedTypeSymbol)interfaceType])
                    .SelectMany(iface => iface.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(method => method.MethodKind == MethodKind.Ordinary)
                        .Where(method => !method.IsStatic)
                        .Select(method => (name: iface.ToDisplayString(), method)))
                    .Select(info => new MethodInfo
                    {
                        Name = $"{GlobalQualify(info.name)}.{EscapeCsKeyword(info.method.Name)}",
                        ReturnType = GlobalQualify(info.method.ReturnType.ToDisplayString()),
                        Parameters = info.method.Parameters.ToParameterInfos(withRefKind: true),
                        GenericTypes = string.Join(", ", info.method.TypeParameters.Select(type => type.ToDisplayString())),
                        TypeConstraints = info.method.TypeParameters.ToDictionary(type => type.Name, type => type.ConstraintTypes.Select(constraint => constraint.ToDisplayString()).ToArray()),
                        IsInaccessibleMarker = GetScope(info.method.Name) == "internal" &&
                            !SymbolEqualityComparer.Default.Equals(info.method.ContainingAssembly, context.Compilation.Assembly)
                    })
                    .Distinct()
                    .ToList();

                bool interfaceAdapterImplementsFormattable = interfaceAdapterMethods.RemoveAll(m => m.Name.StartsWith("System.IFormattable.")) > 0;
                bool foreignSourceInterface = !SymbolEqualityComparer.Default.Equals(structType.ContainingAssembly, syntaxContext.SemanticModel.Compilation.Assembly);
                string adapterScope = (interfaceType.DeclaredAccessibility == Accessibility.Public || GetScope(GetSimpleName(interfaceName)) == "public") &&
                                      (structType.DeclaredAccessibility == Accessibility.Public || GetScope(GetSimpleName(structName)) == "public") ? "public" : "internal";

                // Compose the class name from UNESCAPED simple names: a keyword-named side arrives
                // "@"-escaped from its display string, and "@" is only legal at the START of an
                // identifier token — an interior marker (`lockᴠ@lock`) lexes as two tokens. The
                // composed name (always containing the infix) is never a keyword, so no marker is
                // needed. Keep in sync with the converter's valueAdapterTypeRef composition.
                string adapterName = $"{(foreignSourceInterface ? ForeignPackagePrefix(structType) : "")}{GetUnsanitizedIdentifier(GetSimpleName(structName))}{ValueAdapterInfix}{GetUnsanitizedIdentifier(GetSimpleName(interfaceName))}";

                if (!emittedInterfaceAdapters.Add($"{adapterName}|{interfaceName}"))
                    continue;

                string interfaceAdapterSource = new InterfaceAdapterImplTemplate
                {
                    PackageNamespace = packageNamespace,
                    PackageName = packageName,
                    SourceInterfaceName = GlobalQualify(structType.GetFullTypeName(true)),
                    InterfaceName = interfaceName,
                    AdapterName = adapterName,
                    AdapterScope = adapterScope,
                    ImplementsFormattable = interfaceAdapterImplementsFormattable,
                    Methods = interfaceAdapterMethods,
                    UsingStatements = usingStatements
                }
                .Generate();

                context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{structName}-{interfaceName}-iface.g.cs")), interfaceAdapterSource);
                continue;
            }

            // A NAMED FUNC type with methods (flag's `type funcValue func(string) error`
            // implementing Value) arrives as a DELEGATE — it cannot be a partial struct, so
            // it routes to the VALUE adapter below. Anything else non-struct is a converter
            // bug worth failing loudly on — but NOT by throwing, which kills the entire
            // generator run for the package (flag lost all 11 of its adapters, CS0246 ×19).
            if (structType.TypeKind != TypeKind.Struct && structType.TypeKind != TypeKind.Delegate)
                continue;
                
            // A SELF-REFERENTIAL constraint proxy (crypto/elliptic's *P224Point satisfying
            // nistPoint[Point] structurally so nistCurve[*P224Point] can instantiate): emit the
            // proxy class that wraps ж<element> and implements the interface over ITSELF, then move
            // on — none of the normal adapter method-binding below applies.
            if (constraintProxy)
            {
                EmitConstraintProxy(context, structType, (INamedTypeSymbol)interfaceType, packageNamespace, packageName, packageClassName, usingStatements, emittedHintNames, emittedGenericPointerAdapters);
                continue;
            }

            // Get all extension methods for the struct, any directly defined receivers
            // take precedence over promoted interface methods that have the same name
            (StructDeclarationSyntax? structDecl, Compilation? compilation) = context.GetStructDeclaration(structType.ToDisplayString());
            IEnumerable<MethodInfo>? structMethods = structDecl is null ? [] : structDecl.GetExtensionMethods(compilation!);
            HashSet<string> overrides = new(structMethods?.Select(method => method.Name) ?? [], StringComparer.Ordinal);

            // Every method this struct DECLARES in the current compilation, in either receiver form —
            // the value/ref extensions above plus the direct-ж primaries. This is the evidence that the
            // adapter has something real to forward to, and it is what keeps the package-sealing stub
            // below from swallowing a genuine implementation (see IsInaccessibleMarker).
            HashSet<string> localImplNames = new(overrides, StringComparer.Ordinal);

            if (structDecl is not null)
                localImplNames.UnionWith(structDecl.GetBoxReceiverMethodNames(compilation!));

            // GetAllBaseInterfaces (not AllInterfaces) recovers a base declared in ANOTHER package
            // class, which is still PRIVATE until this generator emits its access modifier and would
            // otherwise bind to an empty error symbol — see Common.GetAllBaseInterfaces.
            List<MethodInfo> methods = ((INamedTypeSymbol)interfaceType).GetAllBaseInterfaces(context.Compilation)
                .Concat([(INamedTypeSymbol)interfaceType]) // Include the original interface
                .SelectMany(iface => iface.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(method => method.MethodKind == MethodKind.Ordinary)
                    .Where(method => !method.IsStatic)
                    .Select(method => (name: iface.ToDisplayString(), method)))
                .Select(info => new MethodInfo
                {
                    // A keyword method name (gob's `string()`) read from the interface symbol is
                    // UNescaped; escape it so both the explicit-interface signature and the
                    // forwarding call emit `@string` (bare `string` is a parse error, CS1525/0539).
                    Name = promoted && !pointer && !overrides.Contains(GetSimpleName(EscapeCsKeyword(info.method.Name))) ? EscapeCsKeyword(info.method.Name) : $"{GlobalQualify(info.name)}.{EscapeCsKeyword(info.method.Name)}",
                    ReturnType = GlobalQualify(info.method.ReturnType.ToDisplayString()),
                    // Carry the parameter REF KIND: an interface member declared with an `in`
                    // param (the hand-finished io stub's Reader.Read(in slice<byte>)) is a
                    // distinct signature - an explicit impl without it is CS0539. Parameter NAMES
                    // are `@`-escaped for the same reason the method name above is — see
                    // ToParameterInfos (sync.Map's `CompareAndSwap(key, old, new any)`).
                    Parameters = info.method.Parameters.ToParameterInfos(withRefKind: true),
                    GenericTypes = string.Join(", ", info.method.TypeParameters.Select(type => type.ToDisplayString())),
                    TypeConstraints = info.method.TypeParameters.ToDictionary(type => type.Name, type => type.ConstraintTypes.Select(constraint => constraint.ToDisplayString()).ToArray()),
                    // A Go-UNEXPORTED interface method (lowercase name → its C# extension impl is
                    // INTERNAL) declared in a DIFFERENT assembly than the one this adapter is generated
                    // into is a package-sealing MARKER (ast.Expr's exprNode(), parse.Node's tree()/writeTo()):
                    // its extension is invisible here, so forwarding is CS1061. Go bars calling it from
                    // outside its package, so the adapter stubs the (still-required, public) member.
                    //
                    // The assembly comparison alone is a PROXY for "there is nothing to forward to", and
                    // it answers wrongly for the one shape where a Go package spans two assemblies: an
                    // INTERNAL (white-box) test package. `package profile`'s proto_test.go declares
                    // packedInts and its `encode`/`decoder` methods for profile's own unexported
                    // `message` interface — same Go package, different C# assembly, and reachable
                    // because the test model mints an InternalsVisibleTo grant. Stubbing there is worse
                    // than a compile error: the adapter COMPILES and silently does nothing, so
                    // marshal(source) returned an empty buffer with no diagnostic anywhere. Require the
                    // absence of a local implementation as well, which leaves the genuine markers (a
                    // FOREIGN struct never declares the sealing method) stubbed exactly as before.
                    IsInaccessibleMarker = GetScope(info.method.Name) == "internal" &&
                        !SymbolEqualityComparer.Default.Equals(info.method.ContainingAssembly, context.Compilation.Assembly) &&
                        !localImplNames.Contains(GetSimpleName(EscapeCsKeyword(info.method.Name)))
                })
                .Distinct()
                .ToList();

            // The interface may inherit System.IFormattable (the hand-finished io stub's
            // Reader does, for the dyn machinery): its ToString(format, provider) cannot
            // forward through the box or an uncast struct (CS1501/CS0030) — the templates
            // emit a canned explicit impl instead when flagged.
            bool implementsFormattable = methods.RemoveAll(m => m.Name.StartsWith("System.IFormattable.")) > 0;

            // An interface member with NO direct struct method may be satisfied by Go method
            // promotion through an embedded POINTER field (`type rtype struct { *abi.Type }`).
            // That promotion is syntax-resolved at Go call sites (the converter emits the hop
            // `t.Type.Value.M()`), so the explicit interface implementation must forward through
            // the same hop — `this.M()` has nothing to bind (CS1929). A SINGLE hop takes every
            // unbound member unconditionally: that member's promotion is what type-checked the
            // cast, so there is nothing to decide. SEVERAL embeds is the case that must be
            // decided per member — see multiEmbedHopPaths below.
            List<(string Name, string TypeName)> embedHops = structDecl?.GetEmbeddedPointerHopNames() ?? [];
            string? embedHop = embedHops.Count == 1 ? embedHops[0].Name : null;

            // With SEVERAL embedded pointers no single hop can take every member, so both forms of the
            // single-hop forwarding below stayed silent and every promoted member fell back to the bare
            // `m_box.M(…)` / `this.M(…)` receiver — which binds nothing on the struct and lets overload
            // resolution reach an unrelated same-named extension elsewhere in scope. net/rpc/jsonrpc's `type pipe
            // struct { *io.PipeReader; *io.PipeWriter }` is the shape: Read and Write come only by
            // promotion, and the adapter's `m_box.Read(p)` bound `io_package.Read(ref LimitedReader, …)`
            // (CS1929 — an error naming LimitedReader from a jsonrpc test is the signature). Index the
            // hop path PER MEMBER instead, routing each to the UNIQUE embed declaring it exactly as Go's
            // depth-1 promotion does; a name two embeds declare is promoted from neither (Go rejects the
            // tie), and the struct's own method, already bound above, wins over both.
            Dictionary<string, string> multiEmbedHopPaths = embedHops.Count > 1
                ? GetMultiEmbedHopPaths(context, syntaxContext.SemanticModel.Compilation, structType, embedHops)
                : [];

            // Hop-target methods that are direct-ж primaries bind on the box FIELD itself
            // (`this.File.Read(p)`) — deref'ing first strands the extension receiver (CS1929).
            HashSet<string> embedHopBoxMethods = embedHops.Count == 1
                ? StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNames(embedHops[0].TypeName, syntaxContext.SemanticModel.Compilation)
                : [];

            // A FOREIGN hop type (net/http's http2timeTimer embeds *time.Timer) declares its
            // ptr-receiver methods as ж-extensions visible only in METADATA — direct-ж primaries
            // and the public RecvGenerator twins — so the syntax scan above finds none and every
            // promoted forwarder deref'd to the value (`this.Timer.Value.Reset(d)`, CS1929).
            // Resolve the hop element's SYMBOL from the struct and union its metadata box methods:
            // those bind on the embedded box field exactly as a local direct-ж primary does.
            if (embedHops.Count == 1)
            {
                INamedTypeSymbol? hopElement = StructDeclarationSyntaxExtensions.GetPointerEmbeds(structType)
                    .FirstOrDefault(embed => embed.Name == embedHops[0].Name).Type;

                if (hopElement is not null &&
                    !SymbolEqualityComparer.Default.Equals(hopElement.ContainingAssembly, syntaxContext.SemanticModel.Compilation.Assembly))
                {
                    embedHopBoxMethods.UnionWith(StructDeclarationSyntaxExtensions.GetForeignBoxReceiverMethodNames(hopElement));
                }
            }

            // A hop-type method may be declared DEEPER - on a VALUE-embedded field of the hop
            // type with a POINTER receiver (net's tcpConnWithoutWriteTo embeds *TCPConn; TCPConn
            // embeds conn by VALUE; Read/Write live on zh<conn>). `this.TCPConn.Value.Read(p)`
            // strands the extension receiver (CS1929 x2); project the field's box through the
            // generated ref accessor instead: `this.TCPConn.of(TCPConn.Rconn).Read(p)`.
            Dictionary<string, string> embedHopDeepPaths = new(StringComparer.Ordinal);

            if (embedHops.Count == 1)
            {
                (StructDeclarationSyntax? hopDecl, Compilation? hopCompilation) = context.GetStructDeclaration(embedHops[0].TypeName);

                if (hopDecl is not null && hopCompilation is not null)
                {
                    string hopSimpleName = GetSimpleName(embedHops[0].TypeName);

                    foreach ((string fieldName, string fieldTypeName) in hopDecl.GetEmbeddedValueHopNames())
                    {
                        foreach (string deepMethod in StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNames(fieldTypeName, hopCompilation))
                        {
                            if (!embedHopBoxMethods.Contains(deepMethod) && !embedHopDeepPaths.ContainsKey(deepMethod))
                                embedHopDeepPaths[deepMethod] = $".of({packageClassName}.{hopSimpleName}.Ꮡ{fieldName})";
                        }
                    }
                }
            }

            if (pointer)
            {
                // A POINTER-sourced interface cast (`var s Iface = &t`): the interface value must
                // alias the receiver box (Go's interface holds the *T), so emit the IжAdapter
                // wrapper instead of the value-boxing partial struct — which copies, and cannot
                // bind direct-ж receiver methods from a struct's `this` at all (CS1929).
                Dictionary<string, string> forwardReceivers = new(StringComparer.Ordinal);
                Dictionary<string, string> forwardStaticCalls = new(StringComparer.Ordinal);

                foreach (MethodInfo structMethod in structMethods ?? [])
                {
                    // A [GoRecv] ref extension has a RecvGenerator ж-twin that binds on the box;
                    // a plain value-receiver method needs the deref'd value (Go copies at the call).
                    forwardReceivers[structMethod.Name] = structMethod.IsRefRecv ? "m_box" : "m_box.Value";
                }

                foreach (string boxReceiverName in structDecl?.GetBoxReceiverMethodNames(compilation!) ?? [])
                    forwardReceivers[boxReceiverName] = "m_box"; // direct-ж primary form binds the box itself

                // A referenced production struct can gain pointer-receiver methods from the current
                // test compilation's friend bridge. Those extensions are syntax-local even though the
                // struct declaration is metadata-only — which is exactly when the discovery above
                // hands back (null, null), so this scan must use the CURRENT compilation, and must
                // key on the SIMPLE name: the bridge spells its box parameter through whatever
                // qualification its own file needs (`this ж<Replacer>` via an imported alias, but
                // `this ж<global::go.sync_package.poolChain>` once a `go/*` package in the closure
                // shadows the root namespace), never one fixed display form.
                HashSet<string> bridgeBoxMethods = [];

                if (structDecl is null)
                {
                    bridgeBoxMethods = StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNamesBySimpleName(structType.Name, syntaxContext.SemanticModel.Compilation);

                    foreach (string boxReceiverName in bridgeBoxMethods)
                        forwardReceivers[boxReceiverName] = "m_box";
                }

                // A FOREIGN struct (no local declaration — the pair is recorded locally because
                // the defining package never converts it: os never casts *File to io.Reader, so
                // fmt's Fscan(os.Stdin, …) has no exported adapter to reference — CS1503). Bind
                // forwarding from METADATA: the compiled assembly exposes every converter and
                // sibling-generator form as real symbols, so an extension on ж<T> binds the box
                // and everything else binds the deref'd value (ref extensions bind through the
                // ref-returning .Value).
                bool foreignStruct = structDecl is null && !SymbolEqualityComparer.Default.Equals(structType.ContainingAssembly, syntaxContext.SemanticModel.Compilation.Assembly);

                if (foreignStruct && structType.ContainingType is INamedTypeSymbol packageClass)
                {
                    HashSet<string> boxBound = new(StringComparer.Ordinal);
                    HashSet<string> refBound = new(StringComparer.Ordinal);

                    foreach (IMethodSymbol method in packageClass.GetMembers().OfType<IMethodSymbol>())
                    {
                        if (!method.IsStatic || method.Parameters.Length == 0)
                            continue;

                        // Only a PUBLIC zh-extension binds cross-assembly — the RecvGenerator
                        // twins of unexported methods are internal, visible to this METADATA
                        // scan but not to the consuming compilation (elf's zstd_ReaderzhReader
                        // forwarded m_box.Read to zstd's internal twin, CS1929).
                        if (method.DeclaredAccessibility == Accessibility.Public &&
                            method.Parameters[0].Type is INamedTypeSymbol recvType &&
                            recvType.Name == "ж" &&
                            recvType.TypeArguments.Length == 1 &&
                            SymbolEqualityComparer.Default.Equals(recvType.TypeArguments[0], structType))
                        {
                            boxBound.Add(method.Name);
                        }

                        // A [GoRecv] ref extension called STATICALLY needs the ref keyword.
                        if (method.DeclaredAccessibility == Accessibility.Public &&
                            method.Parameters[0].RefKind == RefKind.Ref &&
                            SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, structType))
                        {
                            refBound.Add(method.Name);
                        }
                    }

                    // A foreign package class in ANOTHER namespace segment (zstd_package lives
                    // in go.@internal) is not imported by the generated file, so its extensions
                    // are invisible to extension-method lookup (CS1929, elf's zstd_ReaderzhReader)
                    // — forward via the package-class STATIC call instead.
                    string emittingNamespace = packageNamespace;
                    string foreignNamespace = packageClass.ContainingNamespace?.ToDisplayString() ?? "";
                    string? staticClass = null;

                    if (!string.Equals(foreignNamespace, emittingNamespace, StringComparison.Ordinal))
                        staticClass = packageClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    foreach (MethodInfo method in methods)
                    {
                        string simpleName = GetSimpleName(method.Name);

                        // A method the FRIEND BRIDGE contributes is not in the referenced assembly's
                        // metadata at all, so the METADATA scan above cannot see it and would fall
                        // this member back to `m_box.Value` — stranding the direct-ж extension
                        // receiver (CS1929). The bridge scan already bound it to the box; keep that.
                        // sync's export_test.go declares PushHead/PopTail on the production
                        // `*poolDequeue`/`*poolChain`, which is exactly this shape.
                        if (bridgeBoxMethods.Contains(simpleName))
                            continue;

                        bool viaBox = boxBound.Contains(simpleName);

                        // Only forward through a package-class STATIC when one actually binds this
                        // struct's box/ref. A PROMOTED interface method — debug/buildinfo's *xcoff.Section
                        // → io.ReaderAt, where Section EMBEDS the io.ReaderAt interface so ReadAt is
                        // promoted, not declared — has no such static, so `xcoff_package.ReadAt(m_box,…)`
                        // targets a nonexistent overload (CS1501). Fall to the box VALUE, invoking the
                        // struct's own PUBLIC promoted method (`m_box.Value.ReadAt(…)`).
                        if (staticClass is not null && (viaBox || refBound.Contains(simpleName)))
                        {
                            string recvArg = viaBox ? "m_box" : "ref m_box.Value";
                            forwardStaticCalls[simpleName] = $"{staticClass}.{simpleName}";
                            forwardReceivers[simpleName] = recvArg;
                        }
                        else
                        {
                            forwardReceivers[simpleName] = viaBox ? "m_box" : "m_box.Value";
                        }
                    }

                    // An interface member the foreign struct PROMOTES through a VALUE-embedded field
                    // (parse's `RangeNode` embeds `BranchNode`; the exported `String` lives on BranchNode,
                    // not RangeNode) has no extension on the struct's OWN package class, so the fallback
                    // `m_box.Value.String()` is CS1929. Forward through the embed's package-class STATIC
                    // (`parse_package.String(ref m_box.Value.BranchNode)`) — the embed's namespace is not
                    // imported here (only `using go;`), so the instance form cannot resolve the extension,
                    // exactly as the staticClass arm above handles the struct's own foreign extensions.
                    Dictionary<string, RefKind> structOwnMethods = StructDeclarationSyntaxExtensions.GetForeignValueReceiverMethods((INamedTypeSymbol)structType);

                    foreach ((string embedName, INamedTypeSymbol embedType) in StructDeclarationSyntaxExtensions.GetForeignValueEmbeds((INamedTypeSymbol)structType))
                    {
                        string embedStaticClass = embedType.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "";

                        if (embedStaticClass.Length == 0)
                            continue;

                        Dictionary<string, RefKind> embedMethods = StructDeclarationSyntaxExtensions.GetForeignValueReceiverMethods(embedType);

                        foreach (MethodInfo method in methods)
                        {
                            string simpleName = GetSimpleName(method.Name);

                            // Only reroute a genuinely PROMOTED member still on the plain m_box.Value
                            // fallback: the struct binds it neither directly (box/ref/value) nor via an
                            // already-resolved hop, and the embed publicly declares it.
                            if (!embedMethods.TryGetValue(simpleName, out RefKind embedRefKind) ||
                                boxBound.Contains(simpleName) ||
                                structOwnMethods.ContainsKey(simpleName) ||
                                !forwardReceivers.TryGetValue(simpleName, out string? currentReceiver) ||
                                currentReceiver != "m_box.Value")
                            {
                                continue;
                            }

                            string receiverPrefix = embedRefKind switch
                            {
                                RefKind.Ref => "ref ",
                                RefKind.In => "in ",
                                _ => ""
                            };

                            forwardStaticCalls[simpleName] = $"{embedStaticClass}.{simpleName}";
                            forwardReceivers[simpleName] = $"{receiverPrefix}m_box.Value.{embedName}";
                        }
                    }

                    // An interface member the foreign struct PROMOTES through a POINTER embed —
                    // net/http/internal's FlushAfterChunkWriter embeds *bufio.Writer; bufio.ReadWriter
                    // embeds BOTH *Reader and *Writer (recorded downstream by net/http and httputil):
                    // the member lives on the EMBED's ж-extensions (direct-ж primaries or public
                    // RecvGenerator twins), so the plain m_box.Value fallback binds nothing (CS1061).
                    // Forward through the embedded box FIELD (`m_box.Value.Writer.Write(p)`) — Go
                    // promotes the embed's full pointer method set into *T's. With SEVERAL pointer
                    // embeds each member routes to the UNIQUE embed declaring it (Go's promotion
                    // ambiguity rules reject the rest at depth one). An embed package class outside
                    // this file's extension-lookup reach (not the emitting namespace, the shared root
                    // namespace, or an enclosing segment) forwards via its package-class STATIC with
                    // the box as the receiver argument, mirroring the struct's own foreign arm above.
                    List<(string Name, INamedTypeSymbol Type)> foreignPointerEmbeds = StructDeclarationSyntaxExtensions.GetPointerEmbeds(structType);

                    if (foreignPointerEmbeds.Count > 0)
                    {
                        List<(string Name, INamedTypeSymbol Type, HashSet<string> BoxMethods)> embedBoxMethods = foreignPointerEmbeds
                            .Select(embed => (embed.Name, embed.Type, StructDeclarationSyntaxExtensions.GetForeignBoxReceiverMethodNames(embed.Type)))
                            .ToList();

                        foreach (MethodInfo method in methods)
                        {
                            string simpleName = GetSimpleName(method.Name);

                            // Only reroute a member still on the plain m_box.Value fallback
                            // (mirrors the value-embed arm's gating above).
                            if (boxBound.Contains(simpleName) ||
                                structOwnMethods.ContainsKey(simpleName) ||
                                !forwardReceivers.TryGetValue(simpleName, out string? pointerEmbedReceiver) ||
                                pointerEmbedReceiver != "m_box.Value")
                            {
                                continue;
                            }

                            List<(string Name, INamedTypeSymbol Type, HashSet<string> BoxMethods)> declaringEmbeds = embedBoxMethods
                                .Where(embed => embed.BoxMethods.Contains(simpleName))
                                .ToList();

                            if (declaringEmbeds.Count != 1)
                                continue;

                            (string pointerEmbedName, INamedTypeSymbol pointerEmbedType, _) = declaringEmbeds[0];
                            string pointerEmbedNamespace = pointerEmbedType.ContainingType?.ContainingNamespace?.ToDisplayString() ?? "";

                            if (pointerEmbedNamespace != packageNamespace && pointerEmbedNamespace != Namespace &&
                                !packageNamespace.StartsWith($"{pointerEmbedNamespace}.", StringComparison.Ordinal))
                            {
                                forwardStaticCalls[simpleName] = $"{pointerEmbedType.ContainingType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{simpleName}";
                            }

                            forwardReceivers[simpleName] = $"m_box.Value.{pointerEmbedName}";
                        }
                    }
                }

                // Interface members with NO struct method forward through a single embedded-pointer
                // hop, mirroring the value-form template (Go method promotion through `*abi.Type`).
                if (embedHops.Count == 1)
                {
                    foreach (MethodInfo method in methods)
                    {
                        string simpleName = GetSimpleName(method.Name);

                        if (!forwardReceivers.ContainsKey(simpleName))
                        {
                            if (embedHopDeepPaths.TryGetValue(simpleName, out string? deepPath))
                                forwardReceivers[simpleName] = $"m_box.Value.{embedHop}{deepPath}";
                            else
                                forwardReceivers[simpleName] = embedHopBoxMethods.Contains(simpleName) ? $"m_box.Value.{embedHop}" : $"m_box.Value.{embedHop}.Value";
                        }
                    }
                }
                else if (multiEmbedHopPaths.Count > 0)
                {
                    // SEVERAL embedded pointers: only a member the per-embed index resolved forwards
                    // (see GetMultiEmbedHopPaths). One it cannot place stays unbound and falls to the
                    // template's `m_box` default exactly as before — a loud CS1929 naming the member,
                    // never a silent wrong receiver.
                    foreach (MethodInfo method in methods)
                    {
                        string simpleName = GetSimpleName(method.Name);

                        if (!forwardReceivers.ContainsKey(simpleName) && multiEmbedHopPaths.TryGetValue(simpleName, out string? hopPath))
                            forwardReceivers[simpleName] = $"m_box.Value.{hopPath}";
                    }
                }

                // DEPTH-1 promotion through a marker-backed VALUE embed is resolved FIRST, ahead of
                // the embedded-INTERFACE-field arm below. That arm's field test is a NAME heuristic
                // and it cannot, on its own, tell a Go embedded interface from an ordinary named
                // field whose name happens to equal its type's simple name: `type PtrType struct {
                // CommonType; Type Type }` and slogtest's genuine `wrapper` embed of `slog.Handler`
                // BOTH emit a plain `public ΔType Type;` / `public ΔHandler Handler;` field.
                //
                // Where a depth-1 value embed the converter MARKED (`public partial ref CommonType
                // CommonType { get; }`) provides the member, the heuristic's answer is the wrong one
                // by construction: legal Go cannot promote one member from two depth-1 embeds — that
                // is an ambiguity the compiler rejects — so seeing both means the interface "embed"
                // is really a plain field. Yield to the hard marker. Deeper embed levels stay BELOW
                // the interface arm, matching Go's shallower-embed-wins rule.
                //
                // What this cost while the order was the other way round: `dwarf.PtrType.Common()`
                // and `TypedefType.Common()` forwarded through the `Type` FIELD, so they returned
                // the REFERENCED type's CommonType instead of the receiver's own — a silent wrong
                // answer whenever `Type` was non-nil, and a null dereference when it was not.
                bindThroughValueEmbeds(maxDepth: 1);

                // Interface members may also promote through embedded INTERFACE field(s) —
                // zip's `type nopCloser struct { io.Writer }`: Write lives on the FIELD's
                // interface value (Go promotes its method set), Close on the struct. Forward
                // still-unbound members through the field whose interface declares them:
                // `m_box.Value.Writer.Write(…)`. Semantic detection (field name equals its
                // interface type's simple name — the converter names the field after the Go
                // embed, so a Δ-renamed interface TYPE keeps a markerless FIELD: slogtest's
                // `wrapper` embeds slog.ΔHandler as `Handler`) — the converter emits embeds
                // as real fields, so the symbol sees them. With SEVERAL embedded interface
                // fields (httputil's `dumpConn` embeds io.Writer AND io.Reader, adapted to
                // net.Conn) each member routes to the UNIQUE field declaring it — Go's
                // promotion ambiguity rules reject a member two fields declare unless the
                // struct overrides it, which forwardReceivers already resolved above.
                List<(string FieldName, HashSet<string> Members)> embeddedIfaceFieldMembers = structType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(field => !field.IsStatic && field.Type.TypeKind == TypeKind.Interface &&
                                    field.Type is INamedTypeSymbol &&
                                    (field.Name == field.Type.Name || ShadowVarMarker + field.Name == field.Type.Name))
                    .Select(field => (field.Name, new HashSet<string>(((INamedTypeSymbol)field.Type).AllInterfaces
                        .Concat([(INamedTypeSymbol)field.Type])
                        .SelectMany(iface => iface.GetMembers().OfType<IMethodSymbol>())
                        .Select(method => method.Name), StringComparer.Ordinal)))
                    .ToList();

                if (embeddedIfaceFieldMembers.Count > 0)
                {
                    foreach (MethodInfo method in methods)
                    {
                        string simpleName = GetSimpleName(method.Name);

                        if (forwardReceivers.ContainsKey(simpleName))
                            continue;

                        List<(string FieldName, HashSet<string> Members)> declaringFields = embeddedIfaceFieldMembers
                            .Where(field => field.Members.Contains(simpleName))
                            .ToList();

                        if (declaringFields.Count == 1)
                            forwardReceivers[simpleName] = $"m_box.Value.{declaringFields[0].FieldName}";
                    }
                }

                // Interface members may instead promote through embedded VALUE struct(s) whose
                // methods are pointer-receiver extensions — dwarf's `type VoidType struct
                // { CommonType }` with `func (c *CommonType) Common()`, including CHAINED embeds
                // (`UintType → BasicType → CommonType`) — CS1929 ×18. The TypeGenerator heap-boxes
                // each embedded field with a public static ref accessor, so the adapter projects
                // the receiver box hop by hop onto the field's box:
                // `m_box.of(UintType.ᏑBasicType).of(BasicType.ᏑCommonType).Common()`. Direct-ж
                // methods bind on the projected box; everything else binds through its deref'd
                // .Value (ref extensions bind on the ref-returning Value, matching the pointer-hop
                // dichotomy above). Each level follows a SINGLE value embed (Go's promotion
                // ambiguity rules make multi-embed satisfaction rare), bounded to 4 hops.
                //
                // Run in TWO passes (see the depth-1 call sited above the interface-field arm): the
                // marker-backed depth-1 level outranks that arm's name heuristic, everything deeper
                // stays below it. A member already bound is skipped, so the second pass only picks up
                // what the first two left.
                bindThroughValueEmbeds(maxDepth: 4);

                void bindThroughValueEmbeds(int maxDepth)
                {
                    foreach (MethodInfo method in methods)
                    {
                        string methodName = GetSimpleName(method.Name);

                        if (forwardReceivers.ContainsKey(methodName))
                            continue;

                        string receiver = "m_box";
                        StructDeclarationSyntax? currentDecl = structDecl;
                        string currentTypeName = structName;
                        INamedTypeSymbol? currentTypeSymbol = structType as INamedTypeSymbol;

                        for (int depth = 0; depth < maxDepth && currentDecl is not null; depth++)
                        {
                            List<(string Name, string TypeName)> valueEmbedHops = currentDecl.GetEmbeddedValueHopNames();

                            if (valueEmbedHops.Count != 1)
                                break;

                            (string embedName, string embedTypeName) = valueEmbedHops[0];

                            // Resolve the embedded field's TYPE SYMBOL from the current struct symbol —
                            // the converter emits the embed as a `partial ref {Type} {Name}` property (and
                            // the TypeGenerator a boxed backing field), so the member named `embedName`
                            // carries the embed type. Needed to probe a FOREIGN embed's methods in
                            // METADATA when it has no local syntax declaration (see below).
                            INamedTypeSymbol? embedTypeSymbol = currentTypeSymbol?
                                .GetMembers(embedName)
                                .Select(member => member switch
                                {
                                    IPropertySymbol property => property.Type,
                                    IFieldSymbol field => field.Type,
                                    _ => null
                                })
                                .OfType<INamedTypeSymbol>()
                                .FirstOrDefault();

                            // The projecting class qualifier is a real identifier position — escape a
                            // keyword-named type (`@fixed.Ꮡn`, not the parse-breaking `fixed.Ꮡn`).
                            receiver = $"{receiver}.of({EscapeCsKeyword(currentTypeName)}.{AddressPrefix}{GetUnsanitizedIdentifier(embedName)})";
                            (currentDecl, Compilation? embedCompilation) = context.GetStructDeclaration(embedTypeName);
                            currentTypeName = embedTypeName;
                            currentTypeSymbol = embedTypeSymbol;

                            if (StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNames(embedTypeName, syntaxContext.SemanticModel.Compilation).Contains(methodName))
                            {
                                forwardReceivers[methodName] = receiver;
                                break;
                            }

                            // The embed method may be a FOREIGN direct-ж extension visible only in
                            // METADATA — a syntax-tree scan cannot see it (database/sql's driverConn
                            // value-embeds sync.Mutex, whose Lock/Unlock are `this ж<Mutex>` extensions in
                            // the compiled sync assembly). Bind the box hop exactly as a local direct-ж
                            // primary does — `m_box.of(driverConn.ᏑMutex).Lock()`, matching the converter's
                            // own call-site form — instead of the bare `m_box.Lock()` fallback (CS1929).
                            if (embedTypeSymbol is not null &&
                                StructDeclarationSyntaxExtensions.GetForeignBoxReceiverMethodNames(embedTypeSymbol).Contains(methodName))
                            {
                                forwardReceivers[methodName] = receiver;
                                break;
                            }

                            if (currentDecl is not null && embedCompilation is not null &&
                                currentDecl.GetExtensionMethods(embedCompilation).Any(m => GetSimpleName(m.Name) == methodName))
                            {
                                forwardReceivers[methodName] = $"{receiver}.Value";
                                break;
                            }
                        }
                    }
                }

                // PROMOTED members (an embedded INTERFACE field — sort's `type reverse struct
                // { Interface }`) forward through the interface field itself, mirroring the
                // value-form template's promoted arm (`m_box.Len()` has nothing to bind — CS1929).
                // The Promoted flag may live on the pair's SIBLING attribute instance. The FIELD
                // carries the Go embed name — the Δ-stripped simple name when the interface TYPE
                // was collision-renamed (`m_box.Value.ΔHandler` binds nothing, CS1061 — slogtest).
                if (promoted || promotedPairs.Contains($"{structType.ToDisplayString()}|{interfaceType.ToDisplayString()}"))
                {
                    string interfaceFieldName = GetSimpleName(interfaceName, dropCollisionPrefix: true);

                    foreach (MethodInfo method in methods)
                    {
                        string simpleName = GetSimpleName(method.Name);

                        if (!forwardReceivers.ContainsKey(simpleName))
                            forwardReceivers[simpleName] = $"m_box.Value.{interfaceFieldName}";
                    }
                }

                // STRUCT scope by name-exportedness OR declared syntax modifier (the TypeGenerator's
                // `public partial` is a SIBLING generator's output - invisible to this one's symbol,
                // the single-pass limitation); INTERFACE scope by symbol:
                // the golib `error` interface is lowercase yet PUBLIC (its symbol comes from
                // METADATA, so DeclaredAccessibility is reliable), and the name heuristic
                // made io/fs's PathErrorжerror internal - unreachable from os (CS0122 x40).
                // Interface side is symbol-OR-name for the same reason: a SAME-assembly interface
                // (CrossPkgLib.Reporter) gets its public modifier from a sibling generator too.
                string adapterScope = (GetScope(structName) == "public" || structType.DeclaredAccessibility == Accessibility.Public) && (interfaceType.DeclaredAccessibility == Accessibility.Public || GetScope(GetSimpleName(interfaceName)) == "public") ? "public" : "internal";

                // A GENERIC struct (crypto/elliptic's nistCurve[Point nistPoint[Point]]) adapts
                // through ONE generic adapter class over its OPEN type parameters —
                // `nistCurveжCurve<Point> : Curve where Point : nistPoint<Point>` wrapping
                // `ж<nistCurve<Point>>` — that the converter instantiates as
                // `new nistCurveжCurve<ж<P224Point>>(box)`. structName is already the OPEN form
                // (GetFullTypeName spells `nistCurve<Point>`, the type-PARAMETER name), so the
                // class NAME drops to the bare simple name (`nistCurve`) and the `<Point>` list
                // plus the struct's own constraint ride SEPARATELY. The per-instantiation records
                // all collapse to the same open pair here — emit the class once (a second is
                // CS0102). Foreign generic adapters are out of scope (kept on the non-generic path).
                string adapterBaseName = structName;
                string adapterTypeParameters = "";
                string adapterConstraintClause = "";

                if (!foreignStruct && structType is INamedTypeSymbol { IsGenericType: true } genericStructType)
                {
                    if (!emittedGenericPointerAdapters.Add($"{genericStructType.OriginalDefinition.ToDisplayString()}|{interfaceName}"))
                        continue;

                    adapterBaseName = genericStructType.Name;
                    adapterTypeParameters = $"<{string.Join(", ", genericStructType.TypeParameters.Select(typeParameter => typeParameter.Name))}>";
                    adapterConstraintClause = GetGenericConstraintClause(genericStructType.TypeParameters);
                }

                string adapterSource = new AdapterImplTemplate
                {
                    PackageNamespace = packageNamespace,
                    PackageName = packageName,
                    // FULLY-qualified for a foreign struct (no local using guarantees resolution;
                    // mirrors the value adapter's same-named-type shadow rationale). A GENERIC
                    // struct uses its OPEN form (`nistCurve<Point>`) for the wrapped field — the
                    // GoImplement record may carry a CLOSED instantiation (`nistCurve<P224Pointж…>`)
                    // once the proxy makes it constraint-satisfiable, but the adapter class is
                    // generic, so `adapterBaseName + adapterTypeParameters` reconstructs the open form.
                    // A LOCAL name is a bare SYMBOL name — UNescaped, unlike display strings — so a
                    // keyword-named struct must be "@"-escaped here or `ж<fixed>` breaks the parse
                    // (the CS0708 'main_package.' cascade). No-op for every other name.
                    StructName = foreignStruct ? GlobalQualify(structType.GetFullTypeName(true)) : $"{EscapeCsKeyword(adapterBaseName)}{adapterTypeParameters}",
                    InterfaceName = interfaceName,
                    // Adapter class name composes with the shared pointer glyph (CatжAnimal) - always
                    // via Symbols.PointerPrefix so a future symbol change follows automatically.
                    // A FOREIGN struct's name is PACKAGE-QUALIFIED (os_FileжReader): two
                    // same-named foreign structs adapting to one interface otherwise compose
                    // a single colliding class (math/big records both bytes.Reader and
                    // strings.Reader against io.ByteScanner - CS0102/CS0111/CS8646). The
                    // package name comes from the containing package class (bytes_package). A
                    // GENERIC struct uses the bare simple name (`nistCurve`) with the `<Point>`
                    // list supplied via TypeParameters, so the args do not bake into the name.
                    // The interface side composes UNESCAPED: a keyword-named interface arrives
                    // "@"-escaped from its display string, and an interior marker (`fixedж@lock`)
                    // lexes as two tokens; the composed name is never a keyword. Keep in sync
                    // with the converter's adapterTypeRef composition.
                    // The interface side takes a package qualifier ONLY when this name is one the
                    // pre-pass found more than one interface composing (see adapterNameGroups) —
                    // flate's own `Reader` vs `io.Reader`, both reached from *bufio.Reader.
                    AdapterName = $"{(foreignStruct ? $"{ForeignPackagePrefix(structType)}{GetSimpleName(structName)}" : adapterBaseName)}{PointerPrefix}{(collidingAdapterNames.Contains($"{AdapterStructKey(structType, packageClassName)}{PointerPrefix}{GetUnsanitizedIdentifier(GetSimpleName(interfaceName))}") ? AdapterInterfacePrefix(interfaceType, packageClassName) : "")}{GetUnsanitizedIdentifier(GetSimpleName(interfaceName))}",
                    TypeParameters = adapterTypeParameters,
                    ConstraintClause = adapterConstraintClause,
                    AdapterScope = adapterScope,
                    Methods = methods,
                    ForwardReceivers = forwardReceivers,
                    ForwardStaticCalls = forwardStaticCalls,
                    ImplementsFormattable = implementsFormattable,
                    UsingStatements = usingStatements
                }
                .Generate();

                context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{structName}-{interfaceName}-ptr.g.cs")), adapterSource);
                continue;
            }

            // A FOREIGN struct (no local declaration to partial - it lives in another
            // assembly) with a value conversion takes the VALUE adapter: a class wrapping a
            // COPY (Go value semantics), forwarding through the foreign package's extension
            // methods via the file's usings (os's Signal interface over syscall.Signal -
            // neither assembly can partial the other, exec_posix CS1503). A local NAMED FUNC
            // type (a DELEGATE - flag's funcValue) takes the same route: a delegate cannot
            // be a partial struct, and its Go methods are package extension methods that
            // bind on the wrapped copy.
            if (structType.TypeKind == TypeKind.Delegate ||
                (structDecl is null && !SymbolEqualityComparer.Default.Equals(structType.ContainingAssembly, syntaxContext.SemanticModel.Compilation.Assembly)))
            {
                // Symbol-OR-name on BOTH sides (mirrors the pointer arm): a public adapter
                // whose ctor takes an INTERNAL wrapped type is CS0051 (flag's internal
                // funcValue delegate under the public Value interface).
                string valueAdapterScope = (interfaceType.DeclaredAccessibility == Accessibility.Public || GetScope(GetSimpleName(interfaceName)) == "public") &&
                                           (structType.DeclaredAccessibility == Accessibility.Public || GetScope(GetSimpleName(structName)) == "public") ? "public" : "internal";

                string valueAdapterSource = new ValueAdapterImplTemplate
                {
                    PackageNamespace = packageNamespace,
                    PackageName = packageName,
                    // FULLY-qualified: the bare name resolves to the LOCAL same-named type
                    // inside this package class (os's ΔSignal interface shadowed syscall's
                    // ΔSignal struct - the adapter field/ctor typed the wrong side).
                    StructName = GlobalQualify(structType.GetFullTypeName(true)),
                    InterfaceName = interfaceName,
                    // Composes with Symbols.ValueAdapterInfix - the value sibling of the
                    // PointerPrefix-composed pointer adapters. A FOREIGN struct's name is
                    // PACKAGE-QUALIFIED (syscall_ΔSignalᴠΔSignal), mirroring the pointer arm's
                    // same-simple-name collision guard; a LOCAL delegate stays bare. Both simple
                    // names compose UNESCAPED (an interior "@" marker lexes as two tokens); keep
                    // in sync with the converter's valueAdapterTypeRef composition.
                    AdapterName = $"{(structDecl is null && !SymbolEqualityComparer.Default.Equals(structType.ContainingAssembly, syntaxContext.SemanticModel.Compilation.Assembly) ? ForeignPackagePrefix(structType) : "")}{GetUnsanitizedIdentifier(GetSimpleName(structName))}{ValueAdapterInfix}{GetUnsanitizedIdentifier(GetSimpleName(interfaceName))}",
                    AdapterScope = valueAdapterScope,
                    ImplementsFormattable = implementsFormattable,
                    Methods = methods,
                    UsingStatements = usingStatements
                }
                .Generate();

                context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{structName}-{interfaceName}-val.g.cs")), valueAdapterSource);
                continue;
            }

            // One value-form impl per pair — a plain + Promoted duplicate would re-emit the
            // comparison operators (CS0111); the promotedPairs pre-index folds the flag in.
            if (!emittedValuePairs.Add($"{structType.ToDisplayString()}|{interfaceType.ToDisplayString()}"))
                continue;

            // Drop members already emitted in an earlier partial of the SAME struct (a member
            // inherited from an embedded interface shared by two implemented interfaces — see
            // emittedPartialMembers). The earlier partial's impl satisfies it for the whole struct;
            // re-declaring it here is CS0111/CS8646. An empty resulting partial is valid.
            List<MethodInfo> partialMethods = methods
                .Where(method => emittedPartialMembers.Add($"{structType.ToDisplayString()}|{method.Name}"))
                .ToList();

            string generatedSource = new InterfaceImplTemplate
            {
                PackageNamespace = packageNamespace,
                PackageName = packageName,
                // A bare SYMBOL name arrives UNescaped (unlike display strings) — a keyword-named
                // struct must be "@"-escaped or `partial struct fixed` parses as a fixed-size
                // buffer (the reported CS0708 'main_package.' cascade). No-op otherwise.
                StructName = EscapeCsKeyword(structName),
                InterfaceName = interfaceName,
                Promoted = promoted || promotedPairs.Contains($"{structType.ToDisplayString()}|{interfaceType.ToDisplayString()}"),
                Overrides = overrides,
                Methods = partialMethods,
                EmbedHop = embedHop,
                EmbedHopBoxMethods = embedHopBoxMethods,
                EmbedHopDeepPaths = embedHopDeepPaths,
                // The value form promotes through embedded pointers exactly as the adapter does — a
                // pointer embed's method set is in the STRUCT's method set too, so `var rw ReadWriter =
                // p` (no `&`) records this pair — and it reached the same bare `this.Read(p)` fallback
                // when several embeds left no single hop to name.
                MultiEmbedHopPaths = multiEmbedHopPaths,
                // An interface member with NO direct struct method and a SINGLE VALUE embed
                // must promote through the embedded field (Go's promotion is what type-checked
                // it) - net's `addrPortUDPAddr struct { netip.AddrPort }`: the bare
                // `this.String()` bound an unrelated same-package extension by NAME (CS1929);
                // `this.AddrPort.String()` binds the embed's value-receiver method.
                ValueEmbedHop = embedHop is null && (structDecl?.GetEmbeddedValueHopNames() is [var singleValueHop]) ? singleValueHop.Name : null,
                // A FOREIGN value embed's extensions live in another namespace segment
                // (netip_package sits in go.net; the source file only ALIASES it, which does
                // not import extensions) - call the package-class static directly:
                // `global::go.net.netip_package.String(this.AddrPort)`.
                ValueEmbedHopStaticClass = embedHop is null && (structDecl?.GetEmbeddedValueHopNames() is [var svh]) && svh.TypeName.Contains('.')
                    ? "global::go." + svh.TypeName.Substring(0, svh.TypeName.LastIndexOf('.'))
                    : null,
                UsingStatements = usingStatements
            }
            .Generate();

            // Add the source code to the compilation
            context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{structName}-{interfaceName}.g.cs")), generatedSource);
        }
    }

    /// <summary>
    /// Indexes, for a struct with SEVERAL embedded POINTER fields, the hop path each promoted method
    /// name forwards through — the embed's ж field itself (<c>PipeReader</c>) for a direct-ж primary,
    /// its deref'd value (<c>Reader.Value</c>) for a value/ref-receiver method. A name TWO embeds
    /// declare is dropped: Go's depth-1 promotion rejects the tie, so nothing is promoted and only a
    /// method the struct declares itself can satisfy the member (jsonrpc's <c>*pipe.Close</c>, over
    /// the <c>Close</c> both <c>*io.PipeReader</c> and <c>*io.PipeWriter</c> declare).
    /// </summary>
    /// <remarks>
    /// The single-embed case does not come here — its one hop takes every unbound member
    /// unconditionally, since that member's promotion is what type-checked the cast, and no per-member
    /// evidence is needed. It is only with several embeds that the receiver must be decided per member,
    /// which is also what the FOREIGN-struct arm does from metadata for a struct declared elsewhere.
    /// Each embed's method set is read from local SYNTAX where its type is declared in this compilation
    /// and from METADATA where it is not — a referenced assembly exposes both the converter's direct-ж
    /// primaries and the public <c>RecvGenerator</c> ж-twins as ordinary symbols, which is the whole
    /// jsonrpc case (<c>*io.PipeReader</c>/<c>*io.PipeWriter</c> live in the compiled io assembly).
    /// A member neither resolution places is left out, so it keeps the caller's existing fallback.
    /// </remarks>
    private static Dictionary<string, string> GetMultiEmbedHopPaths(GeneratorExecutionContext context, Compilation compilation, ITypeSymbol structType, List<(string Name, string TypeName)> embedHops)
    {
        List<(string Name, INamedTypeSymbol Type)> pointerEmbeds = StructDeclarationSyntaxExtensions.GetPointerEmbeds(structType);
        Dictionary<string, string> hopPaths = new(StringComparer.Ordinal);
        HashSet<string> ambiguous = new(StringComparer.Ordinal);

        foreach ((string hopName, string hopTypeName) in embedHops)
        {
            HashSet<string> boxMethods = StructDeclarationSyntaxExtensions.GetBoxReceiverMethodNames(hopTypeName, compilation);
            HashSet<string> valueMethods = new(StringComparer.Ordinal);

            (StructDeclarationSyntax? hopDecl, Compilation? hopCompilation) = context.GetStructDeclaration(hopTypeName);

            if (hopDecl is not null && hopCompilation is not null)
                valueMethods.UnionWith(hopDecl.GetExtensionMethods(hopCompilation).Select(method => GetSimpleName(method.Name)));

            INamedTypeSymbol? hopElement = pointerEmbeds.FirstOrDefault(embed => embed.Name == hopName).Type;

            if (hopElement is not null && !SymbolEqualityComparer.Default.Equals(hopElement.ContainingAssembly, compilation.Assembly))
            {
                boxMethods.UnionWith(StructDeclarationSyntaxExtensions.GetForeignBoxReceiverMethodNames(hopElement));
                valueMethods.UnionWith(StructDeclarationSyntaxExtensions.GetForeignValueReceiverMethods(hopElement).Keys);
            }

            foreach (string methodName in boxMethods.Union(valueMethods))
            {
                // Seen on an EARLIER embed: ambiguous at depth 1, so Go promotes it from neither.
                if (!hopPaths.ContainsKey(methodName))
                    hopPaths[methodName] = boxMethods.Contains(methodName) ? hopName : $"{hopName}.Value";
                else
                    ambiguous.Add(methodName);
            }
        }

        foreach (string methodName in ambiguous)
            hopPaths.Remove(methodName);

        return hopPaths;
    }

    /// <summary>
    /// Gets the disambiguating package prefix ("bytes_") for a FOREIGN struct's local adapter
    /// class name, derived from its containing package class ("bytes_package") — matching the
    /// converter's <c>getSanitizedIdentifier(pkg.Name()) + "_"</c> composition at the cast site.
    /// </summary>
    /// <summary>
    /// Gets the adapter-name key for the STRUCT side: the package-prefixed simple name for a
    /// foreign struct ("bufio_Reader"), the bare simple name for one declared by this package.
    /// Locality is decided by the containing package class, matching the converter's rule that a
    /// LOCAL type reference is written bare in the GoImplement record while a foreign one is
    /// qualified — the two must agree or the collision groups diverge.
    /// </summary>
    private static string AdapterStructKey(ITypeSymbol structType, string packageClassName)
    {
        string simpleName = GetUnsanitizedIdentifier(GetSimpleName(structType.ToDisplayString()));
        string? container = structType.ContainingType?.Name;

        if (container is null || !container.EndsWith(PackageSuffix) || container == packageClassName)
            return simpleName;

        return $"{ForeignPackagePrefix(structType)}{simpleName}";
    }

    /// <summary>
    /// Gets the disambiguating package prefix for the INTERFACE side of a COLLIDING adapter name
    /// ("io_" in "bufio_Readerжio_Reader"). Empty for an interface this package declares: at most
    /// one member of a colliding group can be local, so leaving it bare stays unambiguous and
    /// keeps the Go-like short form for the package's own interface.
    /// </summary>
    private static string AdapterInterfacePrefix(ITypeSymbol interfaceType, string packageClassName)
    {
        string? container = interfaceType.ContainingType?.Name;

        if (container is null || !container.EndsWith(PackageSuffix) || container == packageClassName)
            return string.Empty;

        return $"{container.Substring(0, container.Length - PackageSuffix.Length)}_";
    }

    private static string ForeignPackagePrefix(ITypeSymbol structType)
    {
        string? packageClassName = structType.ContainingType?.Name;

        if (packageClassName is null || !packageClassName.EndsWith(PackageSuffix))
            return string.Empty;

        return $"{packageClassName.Substring(0, packageClassName.Length - PackageSuffix.Length)}_";
    }

    // Renders the C# `where T : …` clauses for a GENERIC struct's adapter from the struct's own
    // type parameters. The adapter wraps `ж<nistCurve<Point>>`, so it must repeat every constraint
    // nistCurve itself declares (`where Point : nistPoint<Point>`) or the wrapped field is CS0314.
    // Constraint ORDER follows C#'s required sequence: the class/struct/unmanaged/notnull primary
    // constraint first, then base + interface types, then new() last. Interface constraints (the
    // Go case) are global::-qualified like every other generated type reference.
    private static string GetGenericConstraintClause(IEnumerable<ITypeParameterSymbol> typeParameters)
    {
        List<string> clauses = new();

        foreach (ITypeParameterSymbol typeParameter in typeParameters)
        {
            List<string> constraints = new();

            if (typeParameter.HasReferenceTypeConstraint)
                constraints.Add("class");
            else if (typeParameter.HasValueTypeConstraint)
                constraints.Add("struct");
            else if (typeParameter.HasUnmanagedTypeConstraint)
                constraints.Add("unmanaged");
            else if (typeParameter.HasNotNullConstraint)
                constraints.Add("notnull");

            foreach (ITypeSymbol constraintType in typeParameter.ConstraintTypes)
                constraints.Add(GlobalQualify(constraintType.ToDisplayString()));

            if (typeParameter.HasConstructorConstraint)
                constraints.Add("new()");

            if (constraints.Count > 0)
                clauses.Add($"where {typeParameter.Name} : {string.Join(", ", constraints)}");
        }

        return clauses.Count > 0 ? $" {string.Join(" ", clauses)}" : string.Empty;
    }

    // Emits a SELF-REFERENTIAL constraint proxy for a GoImplement(ConstraintProxy = true) record:
    // `elementжinterface : interface<itself>` wrapping ж<element>, with implicit ж<element>↔proxy
    // conversions and one forwarder per interface method (the body forwards to the box's like-named
    // extension; the implicit conversions marshal every self-typed T argument/result). See
    // ConstraintProxyImplTemplate for the rationale.
    private static void EmitConstraintProxy(GeneratorExecutionContext context, ITypeSymbol elementType, INamedTypeSymbol interfaceType, string packageNamespace, string packageName, string packageClassName, string[] usingStatements, HashSet<string> emittedHintNames, HashSet<string> emittedProxies)
    {
        INamedTypeSymbol interfaceDef = interfaceType.OriginalDefinition;

        if (interfaceDef.TypeParameters.Length != 1)
            return;

        // One proxy per (element, open-interface) pair — the converter records it at every
        // constrained instantiation site (nistCurve[*P224Point] appears several times).
        if (!emittedProxies.Add($"proxy|{elementType.OriginalDefinition.ToDisplayString()}|{interfaceDef.ToDisplayString()}"))
            return;

        // Proxy name element-simple + ж + interface-simple — MUST match the converter's
        // type-argument rendering at the constrained instantiation.
        string proxyName = $"{elementType.Name}{PointerPrefix}{interfaceDef.Name}";

        // The boxed pointee, fully qualified so a FOREIGN element resolves (nistec.P224Point used
        // from crypto/elliptic).
        string elementName = GlobalQualify(elementType.GetFullTypeName(true));

        // The interface closed over the proxy ITSELF: `nistPoint<P224PointжnistPoint>`.
        string interfaceRef = $"{StripGenericTypeArguments(GlobalQualify(interfaceDef.ToDisplayString()))}<{proxyName}>";

        ITypeParameterSymbol selfParameter = interfaceDef.TypeParameters[0];
        StringBuilder methods = new();

        foreach (IMethodSymbol method in interfaceDef.GetMembers().OfType<IMethodSymbol>().Where(member => member.MethodKind == MethodKind.Ordinary && !member.IsStatic))
        {
            string methodName = EscapeCsKeyword(method.Name);
            string returnType = RenderWithProxy(method.ReturnType, selfParameter, proxyName);
            string parameters = string.Join(", ", method.Parameters.Select((parameter, index) => $"{RenderWithProxy(parameter.Type, selfParameter, proxyName)} {SafeParameterName(parameter.Name, index)}"));
            string arguments = string.Join(", ", method.Parameters.Select((parameter, index) => SafeParameterName(parameter.Name, index)));

            if (methods.Length > 0)
                methods.Append("\r\n\r\n        ");

            methods.Append($"{returnType} {interfaceRef}.{methodName}({parameters}) => m_box.{methodName}({arguments});");
        }

        // Each forwarder calls the boxed element's box extension methods (`m_box.Bytes()`), declared
        // in the element type's PACKAGE class; bring that class's NAMESPACE into scope so a FOREIGN
        // element's extensions resolve. The [GoImplement] attribute sits in package_info.cs, whose
        // usings never cover the element (nistec's P224Point used from crypto/elliptic — without this
        // `m_box.Bytes()`/`.SetBytes()` bind nothing, CS1929/CS1501).
        string[] proxyUsings = usingStatements;

        if (elementType.ContainingNamespace is { IsGlobalNamespace: false } elementNamespace)
            proxyUsings = [.. usingStatements, $"using {elementNamespace.ToDisplayString(s_namespaceUsingFormat)};"];

        string proxySource = new ConstraintProxyImplTemplate
        {
            PackageNamespace = packageNamespace,
            PackageName = packageName,
            ProxyName = proxyName,
            InterfaceRef = interfaceRef,
            ElementName = elementName,
            AdapterScope = "internal",
            MethodsImplementation = methods.ToString(),
            UsingStatements = proxyUsings
        }
        .Generate();

        context.AddSource(GetUniqueHintName(emittedHintNames, GetValidFileName($"{packageNamespace}.{packageClassName}.{proxyName}-proxy.g.cs")), proxySource);
    }

    private static string SafeParameterName(string name, int index) => string.IsNullOrEmpty(name) ? $"arg{index}" : EscapeCsKeyword(name);

    // Reports whether a type mentions the interface's self-type parameter anywhere in its shape.
    private static bool ContainsTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParameter) => type switch
    {
        _ when SymbolEqualityComparer.Default.Equals(type, typeParameter) => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType, typeParameter),
        INamedTypeSymbol named => named.TypeArguments.Any(argument => ContainsTypeParameter(argument, typeParameter)),
        _ => false
    };

    // Renders a type for a proxy method signature with the interface's self-type parameter T
    // rewritten to the proxy itself — `T`→proxy, `(T, error)`→`(proxy, error)`, `[]T`→`proxy[]`,
    // `Foo<T>`→`Foo<proxy>`. A type with no T renders exactly as the interface declares it.
    private static string RenderWithProxy(ITypeSymbol type, ITypeParameterSymbol selfParameter, string proxyName)
    {
        if (SymbolEqualityComparer.Default.Equals(type, selfParameter))
            return proxyName;

        if (!ContainsTypeParameter(type, selfParameter))
            return GlobalQualify(type.ToDisplayString());

        switch (type)
        {
            case IArrayTypeSymbol array:
                return $"{RenderWithProxy(array.ElementType, selfParameter, proxyName)}[]";
            case INamedTypeSymbol { IsTupleType: true } tuple:
                return $"({string.Join(", ", tuple.TupleElements.Select(element => RenderWithProxy(element.Type, selfParameter, proxyName)))})";
            case INamedTypeSymbol named:
                return $"{StripGenericTypeArguments(GlobalQualify(named.ConstructedFrom.ToDisplayString()))}<{string.Join(", ", named.TypeArguments.Select(argument => RenderWithProxy(argument, selfParameter, proxyName)))}>";
            default:
                return GlobalQualify(type.ToDisplayString());
        }
    }

    private static string? GetNamespace(FileScopedNamespaceDeclarationSyntax? namespaceSyntax)
    {
        return namespaceSyntax?.Name.ToString();
    }

    private static string? GetFirstClassName(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;
    }
}
