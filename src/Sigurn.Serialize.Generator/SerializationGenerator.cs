using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sigurn.Serialize.Generator
{
    /// <summary>
    /// Serializer generator.
    /// </summary>
    [Generator]
    public class SerializationGenerator : IIncrementalGenerator
    {
        private const string _generateSerializerAttributeName = "Sigurn.Serialize.GenerateSerializerAttribute";
        private const string _serializationIgnoreAttributeName = "Sigurn.Serialize.SerializationIgnoreAttribute";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = 
            context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (s, _) => IsSyntaxTargetForGeneration(s), 
                transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(m => !(m is null));

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndClasses 
                = context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => 
            {
                if (!source.Item2.IsDefaultOrEmpty)
                    Execute(source.Item1, source.Item2, spc);
            });
        }

        private void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            if (types.IsDefaultOrEmpty)
                return;

            IEnumerable<TypeDeclarationSyntax> distinctClasses = types.Distinct();

            foreach(var t in types)
            {
                var model = compilation.GetSemanticModel(t.SyntaxTree);

                var ns = t.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                var fullTypeName = GetFullTypeName(t);

                var publicProps = t.Members.OfType<PropertyDeclarationSyntax>()
                    .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
                    .Where(x => !HasAttribute(x, model, _serializationIgnoreAttributeName))
                    .Where(x =>
                    {
                        var getAccessor = x.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)).FirstOrDefault();                        
                        if (getAccessor is null) return false;
                        if (getAccessor.Modifiers.Count != 0 && 
                            !getAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                            !getAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                        var setAccessor = x.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)).FirstOrDefault();
                        var initAccessor = x.AccessorList.Accessors.Where(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)).FirstOrDefault();
                        if (setAccessor is null && initAccessor is null) return false;
                        if (setAccessor != null && setAccessor.Modifiers.Count != 0 && 
                            !setAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                            !setAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                        if (initAccessor != null && initAccessor.Modifiers.Count != 0 && 
                            !initAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                            !initAccessor.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword))) return false;

                        return true;
                    });

                StringBuilder toStreamStringBuilder = new StringBuilder();
                StringBuilder fromStreamStringBuilder = new StringBuilder();

                toStreamStringBuilder.Append($"    public async Task ToStreamAsync(Stream stream, {fullTypeName} value, SerializationContext context, CancellationToken cancellationToken)\n");
                toStreamStringBuilder.Append( "    {\n");
                toStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(stream);\n");
                toStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(context);\n");
                toStreamStringBuilder.Append( "\n");

                fromStreamStringBuilder.Append($"    public async Task<{fullTypeName}> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)\n");
                fromStreamStringBuilder.Append( "    {\n");
                fromStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(stream);\n");
                fromStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(context);\n");
                fromStreamStringBuilder.Append( "\n");
                fromStreamStringBuilder.Append($"        return new {fullTypeName}()\n");
                fromStreamStringBuilder.Append( "        {\n");

                foreach(var p in publicProps)
                {
                    var typeSymbol = model.GetTypeInfo(p.Type).Type;
                    toStreamStringBuilder.Append($"        await Serializer.ToStreamAsync<{typeSymbol}>(stream, value.{p.Identifier.Text}, context, cancellationToken);\n");
                    fromStreamStringBuilder.Append($"            {p.Identifier.Text} = await Serializer.FromStreamAsync<{typeSymbol}>(stream, context, cancellationToken),\n");
                }
            
                toStreamStringBuilder.Append( "    }\n");

                fromStreamStringBuilder.Append( "        };\n");
                fromStreamStringBuilder.Append( "    }\n");

                StringBuilder sb = new StringBuilder();
                var serializerClassName = $"{t.Identifier.Text}Serializer";

                sb.Append($"using System;\n");
                sb.Append($"using System.IO;\n");
                sb.Append($"using System.Threading;\n");
                sb.Append($"using System.Runtime.CompilerServices;\n");
                sb.Append("\n");
                sb.Append($"using Sigurn.Serialize;\n");
                sb.Append("\n");
                sb.Append($"namespace {ns.Name}.Serializers;\n");
                sb.Append("\n");
                sb.Append($"internal sealed class {serializerClassName} : ITypeSerializer<{fullTypeName}>\n");
                sb.Append("{\n");

                sb.Append($"    [ModuleInitializer]\n");
                sb.Append($"    internal static void Initializer()\n");
                sb.Append( "    {\n");
                sb.Append($"        Serializer.RegisterSerializer<{fullTypeName}>(() => new {serializerClassName}());\n");
                sb.Append( "    }\n");
                sb.Append("\n");

                sb.Append($"    private {serializerClassName}()\n");
                sb.Append( "    {\n");
                sb.Append( "    }\n");
                sb.Append("\n");

                sb.Append(toStreamStringBuilder);
                sb.Append("\n");
                sb.Append(fromStreamStringBuilder);

                sb.Append("}\n");

                context.AddSource($"{t.Identifier.Text}Serializer.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            return node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0;
        }

        private TypeDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {            
            var tds = (TypeDeclarationSyntax)context.Node;

            if (HasAttribute(tds, context.SemanticModel, _generateSerializerAttributeName))
                return tds;

            return null;
        }

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

        private static string GetFullTypeName(TypeDeclarationSyntax typeDeclaration)
        {
            var typeName = typeDeclaration.Identifier.Text;

            var namespaceNode = typeDeclaration
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            var namespaceName = namespaceNode != null ? GetFullNamespace(namespaceNode) : null;

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

        private static string GetFullNamespace(BaseNamespaceDeclarationSyntax namespaceNode)
        {
            var names = new List<string>();

            while (!(namespaceNode is null))
            {
                names.Insert(0, namespaceNode.Name.ToString());
                namespaceNode = namespaceNode.Parent as BaseNamespaceDeclarationSyntax;
            }

            return string.Join(".", names);
        }
    }
}


