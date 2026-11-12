using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IsExternalInit
    {
    }
}

namespace Sigurn.Serialize.Generator
{
    // A type of the compilation implementing ISerializable: registered when the generated code can make it, else reported where AOT is on.
    readonly record struct SerializableTypeInfo(string Name, string? Problem, string FilePath, TextSpan Span, LinePositionSpan LineSpan);

    readonly record struct TypePropertyInfo(string Name, string Type, int OrderId);

    readonly record struct TargetTypeInfo(string TypeNamespace, string TypeName, string SerializerNamespace, string SerializerName, bool UseGlobally, EquatableArray<TypePropertyInfo> Properties, EquatableArray<string> Registrations);

    /// <summary>
    /// Serializer generator.
    /// </summary>
    [Generator]
    public class SerializationGenerator : IIncrementalGenerator
    {
        private const string _generateSerializerAttributeName = "Sigurn.Serialize.GenerateSerializerAttribute";
        private const string _serializationIgnoreAttributeName = "Sigurn.Serialize.SerializeIgnoreAttribute";
        private const string _serializationOrderIdAttributeName = "Sigurn.Serialize.SerializeOrderAttribute";
        private const string _serializableInterfaceName = "Sigurn.ISerializable";

        // Set by the SDK when the project publishes with Native AOT or is marked IsAotCompatible, and visible to the compiler there.
        private const string _enableAotAnalyzerProperty = "build_property.EnableAotAnalyzer";

        internal static readonly DiagnosticDescriptor SerializableNotConstructible = new DiagnosticDescriptor(
            id: "SGS001",
            title: "ISerializable type cannot be serialized without reflection",
            messageFormat: "Type '{0}' implements ISerializable but {1}, so it cannot be read under Native AOT; add a public or internal parameterless constructor, or register a serializer for it with Serializer.RegisterSerializer",
            category: "Sigurn.Serialize",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private const string _serializerTypeName = "Sigurn.Serialize.Serializer";

        // Generic types the library serializes, by metadata name, and the Serializer helper that makes each serializable without reflection.
        private static readonly Dictionary<string, string> _registrationHelpers = new Dictionary<string, string>
        {
            ["System.Collections.Generic.List`1"] = "RegisterList",
            ["System.Collections.Generic.IList`1"] = "RegisterList",
            ["System.Collections.Generic.ICollection`1"] = "RegisterList",
            ["System.Collections.Generic.IReadOnlyList`1"] = "RegisterList",
            ["System.Collections.Generic.IReadOnlyCollection`1"] = "RegisterList",
            ["System.Collections.Generic.HashSet`1"] = "RegisterHashSet",
            ["System.Collections.Generic.ISet`1"] = "RegisterHashSet",
            ["System.Collections.Generic.IReadOnlySet`1"] = "RegisterHashSet",
            ["System.Collections.Generic.Stack`1"] = "RegisterStack",
            ["System.Collections.Generic.Queue`1"] = "RegisterQueue",
            ["System.Collections.Generic.SortedSet`1"] = "RegisterSortedSet",
            ["System.Collections.Generic.LinkedList`1"] = "RegisterLinkedList",
            ["System.Collections.Immutable.ImmutableArray`1"] = "RegisterImmutableArray",
            ["System.Collections.Immutable.ImmutableList`1"] = "RegisterImmutableList",
            ["System.Collections.Immutable.IImmutableList`1"] = "RegisterImmutableList",
            ["System.Collections.Immutable.ImmutableSortedSet`1"] = "RegisterImmutableSortedSet",
            ["System.Collections.Generic.Dictionary`2"] = "RegisterDictionary",
            ["System.Collections.Generic.IReadOnlyDictionary`2"] = "RegisterDictionary",
            ["System.Collections.Generic.SortedDictionary`2"] = "RegisterSortedDictionary",
            ["System.Collections.Generic.SortedList`2"] = "RegisterSortedList",
            ["System.Collections.Immutable.ImmutableSortedDictionary`2"] = "RegisterImmutableSortedDictionary",
            ["System.Collections.Generic.KeyValuePair`2"] = "RegisterKeyValuePair",
        };

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TargetTypeInfo> typesToGenerateSerializer = 
            context.SyntaxProvider.ForAttributeWithMetadataName
            (
                _generateSerializerAttributeName,
                predicate: (s, _) => s is TypeDeclarationSyntax,
                transform: (ctx, _) => GetTargetTypeInfo(ctx.SemanticModel, (TypeDeclarationSyntax)ctx.TargetNode)
            );
            
            context.RegisterSourceOutput(typesToGenerateSerializer, (spc, source) => Execute(source, spc));

            // Every type of the compilation that implements ISerializable is registered too, so it is serializable without reflection
            // (Native AOT) wherever it is used, not only as the member of a type marked with GenerateSerializer.
            IncrementalValueProvider<ImmutableArray<SerializableTypeInfo>> serializableInfos = context.SyntaxProvider
                .CreateSyntaxProvider
                (
                    predicate: (s, _) => s is TypeDeclarationSyntax { BaseList: not null },
                    transform: (ctx, _) => GetSerializableTypeInfo(ctx.SemanticModel, (TypeDeclarationSyntax)ctx.Node)
                )
                .Where(info => info is not null)
                .Select((info, _) => info!.Value)
                .Collect();

            IncrementalValueProvider<EquatableArray<string>> serializableTypes = serializableInfos
                .Select((infos, _) => new EquatableArray<string>(infos.Where(i => i.Problem is null).Select(i => i.Name)
                    .Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray()));

            // Only a project built for Native AOT hears about a type it cannot register: elsewhere reflection makes it, as it always did.
            IncrementalValueProvider<bool> aot = context.AnalyzerConfigOptionsProvider
                .Select((options, _) => options.GlobalOptions.TryGetValue(_enableAotAnalyzerProperty, out var value) &&
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

            // The compilation is joined only to place a diagnostic in its source, where #pragma and .editorconfig can silence it.
            context.RegisterSourceOutput(serializableInfos.Combine(aot).Combine(context.CompilationProvider), (spc, source) =>
            {
                if (!source.Left.Right)
                    return;

                // A partial type is declared more than once, and reported once.
                foreach (var info in source.Left.Left.Where(i => i.Problem is not null).GroupBy(i => i.Name).Select(g => g.First()))
                    spc.ReportDiagnostic(Diagnostic.Create(SerializableNotConstructible,
                        ToLocation(source.Right, info.FilePath, info.Span, info.LineSpan), info.Name.Replace("global::", string.Empty), info.Problem));
            });

            context.RegisterSourceOutput(serializableTypes, (spc, names) => ExecuteSerializableRegistrations(names, spc));

            // Every type the code hands to Serializer.ToStreamAsync or FromStreamAsync is registered too, so a collection serialized only by
            // hand (inside ISerializable, say) needs no registration of its own. A type not known at build time (an open T, a Type held in a
            // variable) is left to the registrations of the code that knows it; the strict mode (SigurnSerializeReflection=false) catches one missed.
            IncrementalValueProvider<EquatableArray<string>> callSiteRegistrations = context.SyntaxProvider
                .CreateSyntaxProvider
                (
                    predicate: (s, _) => s is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ToStreamAsync" or "FromStreamAsync" } },
                    transform: (ctx, _) => GetCallSiteRegistrations(ctx.SemanticModel, (InvocationExpressionSyntax)ctx.Node)
                )
                .Where(registrations => registrations.Count != 0)
                .Collect()
                .Select((all, _) => new EquatableArray<string>(all.SelectMany(r => r)
                    .Distinct().OrderBy(r => r, StringComparer.Ordinal).ToArray()));

            context.RegisterSourceOutput(callSiteRegistrations, (spc, registrations) => ExecuteCallSiteRegistrations(registrations, spc));
        }

        private static Location ToLocation(Compilation compilation, string filePath, TextSpan span, LinePositionSpan lineSpan)
        {
            var tree = compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == filePath);
            return tree is not null ? Location.Create(tree, span) : Location.Create(filePath, span, lineSpan);
        }

        private static EquatableArray<string> GetCallSiteRegistrations(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
                method.ContainingType is null || MetadataName(method.ContainingType) != _serializerTypeName)
                return default;

            ITypeSymbol? type = null;
            if (method.IsGenericMethod)
            {
                type = method.TypeArguments[0];
            }
            else
            {
                // The Type overloads: only a typeof(...) is known at build time.
                var parameter = method.Parameters.FirstOrDefault(p => p.Name == "type");
                if (parameter is null)
                    return default;

                var argument = invocation.ArgumentList.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == "type")
                    ?? (parameter.Ordinal < invocation.ArgumentList.Arguments.Count ? invocation.ArgumentList.Arguments[parameter.Ordinal] : null);
                if (argument?.Expression is TypeOfExpressionSyntax typeOf)
                    type = semanticModel.GetTypeInfo(typeOf.Type).Type;
            }

            if (type is null || ContainsTypeParameter(type))
                return default;

            var registrations = new List<string>();
            CollectRegistrations(type, registrations, new HashSet<string>());
            return new EquatableArray<string>(registrations.ToArray());
        }

        private static bool ContainsTypeParameter(ITypeSymbol type) => type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
            _ => false,
        };

        private static void ExecuteCallSiteRegistrations(EquatableArray<string> registrations, SourceProductionContext context)
        {
            if (registrations.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("using System.Runtime.CompilerServices;\n");
            sb.Append("\n");
            sb.Append("using Sigurn.Serialize;\n");
            sb.Append("\n");
            sb.Append("namespace Sigurn.Serialize.Generated;\n");
            sb.Append("\n");
            sb.Append("internal static class SerializerCallSiteRegistrations\n");
            sb.Append("{\n");
            sb.Append("    [ModuleInitializer]\n");
            sb.Append("    internal static void Initializer()\n");
            sb.Append("    {\n");
            foreach (var registration in registrations)
                sb.Append($"        {registration}\n");
            sb.Append("    }\n");
            sb.Append("}\n");

            context.AddSource("SerializerCallSiteRegistrations.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private static void ExecuteSerializableRegistrations(EquatableArray<string> names, SourceProductionContext context)
        {
            if (names.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("using System.Runtime.CompilerServices;\n");
            sb.Append("\n");
            sb.Append("using Sigurn.Serialize;\n");
            sb.Append("\n");
            sb.Append("namespace Sigurn.Serialize.Generated;\n");
            sb.Append("\n");
            sb.Append("internal static class SerializableTypesRegistration\n");
            sb.Append("{\n");
            sb.Append("    [ModuleInitializer]\n");
            sb.Append("    internal static void Initializer()\n");
            sb.Append("    {\n");
            foreach (var name in names)
                sb.Append($"        Serializer.RegisterSerializable<{name}>();\n");
            sb.Append("    }\n");
            sb.Append("}\n");

            context.AddSource("SerializableTypesRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private void Execute(TargetTypeInfo tti, SourceProductionContext context)
        {
            StringBuilder toStreamStringBuilder = new StringBuilder();
            StringBuilder fromStreamStringBuilder = new StringBuilder();
            if (tti.Properties.Count != 0)
                toStreamStringBuilder.Append($"    public async ValueTask ToStreamAsync(Stream stream, {tti.TypeNamespace}.{tti.TypeName} value, SerializationContext context, CancellationToken cancellationToken)\n");
            else
                toStreamStringBuilder.Append($"    public ValueTask ToStreamAsync(Stream stream, {tti.TypeNamespace}.{tti.TypeName} value, SerializationContext context, CancellationToken cancellationToken)\n");
            toStreamStringBuilder.Append("    {\n");
            toStreamStringBuilder.Append("        ArgumentNullException.ThrowIfNull(stream);\n");
            toStreamStringBuilder.Append("        ArgumentNullException.ThrowIfNull(context);\n");
            toStreamStringBuilder.Append("\n");

            if (tti.Properties.Count != 0)
                fromStreamStringBuilder.Append($"    public async ValueTask<{tti.TypeNamespace}.{tti.TypeName}> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)\n");
            else
                fromStreamStringBuilder.Append($"    public ValueTask<{tti.TypeNamespace}.{tti.TypeName}> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)\n");
            fromStreamStringBuilder.Append("    {\n");
            fromStreamStringBuilder.Append("        ArgumentNullException.ThrowIfNull(stream);\n");
            fromStreamStringBuilder.Append("        ArgumentNullException.ThrowIfNull(context);\n");
            fromStreamStringBuilder.Append("\n");
            if (tti.Properties.Count != 0)
                fromStreamStringBuilder.Append($"        return new {tti.TypeNamespace}.{tti.TypeName}()\n");
            else
                fromStreamStringBuilder.Append($"        return ValueTask.FromResult(new {tti.TypeNamespace}.{tti.TypeName}()\n");
            fromStreamStringBuilder.Append("        {\n");

            foreach (var p in tti.Properties.OrderBy(x => x.OrderId))
            {
                toStreamStringBuilder.Append($"        await Serializer.ToStreamAsync<{p.Type}>(stream, value.{p.Name}, context, cancellationToken);\n");
                fromStreamStringBuilder.Append($"            {p.Name} = await Serializer.FromStreamAsync<{p.Type}>(stream, context, cancellationToken),\n");
            }

            if (tti.Properties.Count == 0)
                toStreamStringBuilder.Append($"        return ValueTask.CompletedTask;\n");

            toStreamStringBuilder.Append("    }\n");

            if (tti.Properties.Count == 0)
                fromStreamStringBuilder.Append("        });\n");
            else
                fromStreamStringBuilder.Append("        };\n");
            fromStreamStringBuilder.Append("    }\n");

            StringBuilder sb = new StringBuilder();
            var useGlobally = tti.UseGlobally ? "true" : "false";

            sb.Append($"using System;\n");
            sb.Append($"using System.IO;\n");
            sb.Append($"using System.Threading;\n");
            sb.Append($"using System.Runtime.CompilerServices;\n");
            sb.Append("\n");
            sb.Append($"using Sigurn.Serialize;\n");
            sb.Append("\n");
            sb.Append($"namespace {tti.SerializerNamespace};\n");
            sb.Append("\n");
            sb.Append($"internal sealed class {tti.SerializerName} : ITypeSerializer<{tti.TypeNamespace}.{tti.TypeName}>\n");
            sb.Append("{\n");

            sb.Append($"    [ModuleInitializer]\n");
            sb.Append($"    internal static void Initializer()\n");
            sb.Append("    {\n");
            foreach (var registration in tti.Registrations)
                sb.Append($"        {registration}\n");
            sb.Append($"        Serializer.RegisterSerializer<{tti.TypeNamespace}.{tti.TypeName}>(() => new {tti.SerializerName}(), {useGlobally});\n");
            sb.Append("    }\n");
            sb.Append("\n");

            sb.Append($"    private {tti.SerializerName}()\n");
            sb.Append("    {\n");
            sb.Append("    }\n");
            sb.Append("\n");

            sb.Append(toStreamStringBuilder);
            sb.Append("\n");
            sb.Append(fromStreamStringBuilder);

            sb.Append("}\n");

            context.AddSource($"{tti.SerializerName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private TargetTypeInfo GetTargetTypeInfo(SemanticModel semanticModel, TypeDeclarationSyntax syntaxNode)
        {            
            var typeName = syntaxNode.Identifier.Text;

            var ns = syntaxNode.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            var typeNamespace = GetFullNamespace(ns);
            var serializerNamespace = $"{typeNamespace}.Serializers";
            var serializerName = $"{typeName}Serializer";
            var useGlobally = true;

            var generateAttr = GetAttribute(syntaxNode, semanticModel, _generateSerializerAttributeName);
            if (generateAttr != null && generateAttr.ArgumentList?.Arguments.Count != 0)
            {
                var attrArg = generateAttr.ArgumentList?.Arguments[0];
                if (attrArg is not null)
                {
                    var constantValue = semanticModel.GetConstantValue(attrArg.Expression);
                    if (constantValue.HasValue && constantValue.Value is bool b)
                        useGlobally = b;
                }
            } 


            var publicProps = syntaxNode.Members.OfType<PropertyDeclarationSyntax>()
                .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
                .Where(x => !HasAttribute(x, semanticModel, _serializationIgnoreAttributeName))
                .Where(x =>
                {
                    var getAccessor = x.AccessorList?.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).FirstOrDefault();                        
                    if (getAccessor is null) return false;
                    if (getAccessor.Modifiers.Count != 0 && 
                        !getAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                        !getAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                    var setAccessor = x.AccessorList?.Accessors.Where(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)).FirstOrDefault();
                    var initAccessor = x.AccessorList?.Accessors.Where(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)).FirstOrDefault();
                    if (setAccessor is null && initAccessor is null) return false;
                    if (setAccessor != null && setAccessor.Modifiers.Count != 0 && 
                        !setAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                        !setAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                    if (initAccessor != null && initAccessor.Modifiers.Count != 0 && 
                        !initAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                        !initAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                    return true;
                });

            var publicPropList = publicProps.ToList();

            var registrations = new List<string>();
            var seen = new HashSet<string>();
            foreach (var p in publicPropList)
            {
                var propType = semanticModel.GetTypeInfo(p.Type).Type;
                if (propType is not null)
                    CollectRegistrations(propType, registrations, seen);
            }

            var props = new EquatableArray<TypePropertyInfo>(publicPropList.Select(p =>
            {
                var name = p.Identifier.Text;
                var type = semanticModel.GetTypeInfo(p.Type).Type?.ToString() ?? string.Empty;
                int orderId = 0;

                var orderAttr = GetAttribute(p, semanticModel, _serializationOrderIdAttributeName);
                if (orderAttr != null && orderAttr.ArgumentList?.Arguments.Count != 0)
                {
                    var attrArg = orderAttr.ArgumentList?.Arguments[0];
                    if (attrArg is not null)
                    {
                        var constantValue = semanticModel.GetConstantValue(attrArg.Expression);
                        if (constantValue.HasValue && constantValue.Value is int n)
                            orderId = n;
                    }
                } 

                return new TypePropertyInfo(name, type, orderId);
            }).ToArray());

            return new TargetTypeInfo(typeNamespace, typeName, serializerNamespace, serializerName, useGlobally, props,
                new EquatableArray<string>(registrations.ToArray()));
        }

        // The Serializer.Register* calls that make the type of a member, and every type it is made of, serializable without reflection, so
        // the generated serializer works where code cannot be made at run time (Native AOT). Each type is registered once per serializer.
        private static void CollectRegistrations(ITypeSymbol type, List<string> registrations, HashSet<string> seen)
        {
            if (type is IArrayTypeSymbol array)
            {
                if (array.Rank != 1)
                    return;

                CollectRegistrations(array.ElementType, registrations, seen);
                Add($"Serializer.RegisterArray<{Name(array.ElementType)}>();");
                return;
            }

            if (type is not INamedTypeSymbol named || named.TypeKind == TypeKind.Error)
                return;

            if (named.IsGenericType)
            {
                if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    CollectRegistrations(named.TypeArguments[0], registrations, seen);
                    return;
                }

                if (!_registrationHelpers.TryGetValue(MetadataName(named.OriginalDefinition), out var helper))
                    return;

                if (named.TypeArguments.Any(a => a is ITypeParameterSymbol))
                    return;

                foreach (var argument in named.TypeArguments)
                    CollectRegistrations(argument, registrations, seen);

                Add($"Serializer.{helper}<{string.Join(", ", named.TypeArguments.Select(Name))}>();");
                return;
            }

            if (IsSerializable(named))
                Add($"Serializer.RegisterSerializable<{Name(named)}>();");

            void Add(string registration)
            {
                if (seen.Add(registration))
                    registrations.Add(registration);
            }
        }

        private static SerializableTypeInfo? GetSerializableTypeInfo(SemanticModel semanticModel, TypeDeclarationSyntax syntax)
        {
            if (semanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol type)
                return null;

            if (type.IsAbstract || type.IsStatic || type.IsGenericType ||
                (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) ||
                !type.AllInterfaces.Any(i => MetadataName(i) == _serializableInterfaceName))
                return null;

            string? problem = null;
            if (!IsSerializable(type))
            {
                bool accessible = true;
                for (INamedTypeSymbol? t = type; t is not null; t = t.ContainingType)
                    accessible &= IsVisible(t.DeclaredAccessibility);

                problem = accessible ? "has no public or internal parameterless constructor" : "is not accessible to generated code";
            }

            var location = syntax.Identifier.GetLocation();
            return new SerializableTypeInfo(Name(type), problem, location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan,
                location.GetLineSpan().Span);
        }

        // A type implementing ISerializable that can be made without reflection: a struct, or a class that is not abstract and has a
        // parameterless constructor the generated code can call.
        private static bool IsSerializable(INamedTypeSymbol type)
        {
            if (type.IsAbstract || type.IsStatic || type.IsGenericType)
                return false;

            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
                return false;

            if (!type.AllInterfaces.Any(i => MetadataName(i) == _serializableInterfaceName))
                return false;

            for (INamedTypeSymbol? t = type; t is not null; t = t.ContainingType)
                if (!IsVisible(t.DeclaredAccessibility))
                    return false;

            return type.TypeKind == TypeKind.Struct ||
                type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && IsVisible(c.DeclaredAccessibility));
        }

        private static bool IsVisible(Accessibility accessibility) =>
            accessibility == Accessibility.Public || accessibility == Accessibility.Internal || accessibility == Accessibility.ProtectedOrInternal;

        private static string MetadataName(INamedTypeSymbol type) =>
            type.ContainingNamespace is { IsGlobalNamespace: false } ns ? $"{ns.ToDisplayString()}.{type.MetadataName}" : type.MetadataName;

        private static string Name(ITypeSymbol type) =>
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private bool HasAttribute(MemberDeclarationSyntax memberDeclarartion, SemanticModel model, string fullAttrName)
        {
            foreach (AttributeListSyntax attributeListSyntax in memberDeclarartion.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    var si = model.GetSymbolInfo(attributeSyntax);
                    var attributeSymbol = si.Symbol;
                    if (attributeSymbol == null)
                        continue;

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == fullAttrName)
                        return true;
                }
            }

            return false;
        }

        private AttributeSyntax? GetAttribute(MemberDeclarationSyntax memberDeclarartion, SemanticModel model, string fullAttrName)
        {
            foreach (AttributeListSyntax attributeListSyntax in memberDeclarartion.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    var si = model.GetSymbolInfo(attributeSyntax);
                    var attributeSymbol = si.Symbol;
                    if (attributeSymbol == null)
                        continue;

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == fullAttrName)
                        return attributeSyntax;
                }
            }

            return null;
        }

        private static string GetFullTypeName(TypeDeclarationSyntax typeDeclaration)
        {
            var typeName = typeDeclaration.Identifier.Text;

            var namespaceNode = typeDeclaration
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            var namespaceName = namespaceNode != null ? GetFullNamespace(namespaceNode) : string.Empty;

            var enclosingTypes = typeDeclaration
                .Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .Select(c => c.Identifier.Text)
                .Reverse();

            var fullNameParts = new List<string>();
            if (!string.IsNullOrEmpty(namespaceName))
                fullNameParts.Add(namespaceName);

            fullNameParts.AddRange(enclosingTypes);
            fullNameParts.Add(typeName);

            return string.Join(".", fullNameParts);
        }

        private static string GetFullNamespace(BaseNamespaceDeclarationSyntax? namespaceNode)
        {
            var names = new List<string>();

            while (!(namespaceNode is null))
            {
                names.Insert(0, namespaceNode.Name.ToString());
                namespaceNode = namespaceNode.Parent as BaseNamespaceDeclarationSyntax ;
            }

            return string.Join(".", names);
        }
    }
}


