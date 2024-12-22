﻿using System;
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
        private const string _serializationIgnoreAttributeName = "Sigurn.Serialize.SerializeIgnoreAttribute";
        private const string _serializationOrderIdAttributeName = "Sigurn.Serialize.SerializeOrderAttribute";

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TargetTypeInfo> typesToGenerateSerializer = 
            context.SyntaxProvider.ForAttributeWithMetadataName
            (
                _generateSerializerAttributeName,
                predicate: (s, _) => true,
                transform: (ctx, _) => GetTargetTypeInfo(ctx.SemanticModel, ctx.TargetNode)
            )
            .Where(m => !(m is null));
            
            IncrementalValueProvider<(Compilation, ImmutableArray<TargetTypeInfo>)> compilationAndClasses 
                = context.CompilationProvider.Combine(typesToGenerateSerializer.Collect());

            context.RegisterSourceOutput(typesToGenerateSerializer,
                (spc, source) => Execute(source, spc));
        }

        private void Execute(TargetTypeInfo tti, SourceProductionContext context)
        {
            StringBuilder toStreamStringBuilder = new StringBuilder();
            StringBuilder fromStreamStringBuilder = new StringBuilder();

            toStreamStringBuilder.Append($"    public async Task ToStreamAsync(Stream stream, {tti.TypeNamespace}.{tti.TypeName} value, SerializationContext context, CancellationToken cancellationToken)\n");
            toStreamStringBuilder.Append( "    {\n");
            toStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(stream);\n");
            toStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(context);\n");
            toStreamStringBuilder.Append( "\n");

            fromStreamStringBuilder.Append($"    public async Task<{tti.TypeNamespace}.{tti.TypeName}> FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken)\n");
            fromStreamStringBuilder.Append( "    {\n");
            fromStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(stream);\n");
            fromStreamStringBuilder.Append( "        ArgumentNullException.ThrowIfNull(context);\n");
            fromStreamStringBuilder.Append( "\n");
            fromStreamStringBuilder.Append($"        return new {tti.TypeNamespace}.{tti.TypeName}()\n");
            fromStreamStringBuilder.Append( "        {\n");

            foreach(var p in tti.Properties.OrderBy(x => x.OrderId))
            {
                toStreamStringBuilder.Append($"        await Serializer.ToStreamAsync<{p.Type}>(stream, value.{p.Name}, context, cancellationToken);\n");
                fromStreamStringBuilder.Append($"            {p.Name} = await Serializer.FromStreamAsync<{p.Type}>(stream, context, cancellationToken),\n");
            }
        
            toStreamStringBuilder.Append( "    }\n");

            fromStreamStringBuilder.Append( "        };\n");
            fromStreamStringBuilder.Append( "    }\n");

            StringBuilder sb = new StringBuilder();

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
            sb.Append( "    {\n");
            sb.Append($"        Serializer.RegisterSerializer<{tti.TypeNamespace}.{tti.TypeName}>(() => new {tti.SerializerName}());\n");
            sb.Append( "    }\n");
            sb.Append("\n");

            sb.Append($"    private {tti.SerializerName}()\n");
            sb.Append( "    {\n");
            sb.Append( "    }\n");
            sb.Append("\n");

            sb.Append(toStreamStringBuilder);
            sb.Append("\n");
            sb.Append(fromStreamStringBuilder);

            sb.Append("}\n");

            context.AddSource($"{tti.SerializerName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        private TargetTypeInfo GetTargetTypeInfo(SemanticModel semanticModel, SyntaxNode syntaxNode)
        {            
            if (!(syntaxNode is TypeDeclarationSyntax tds)) return null;

            var tti = new TargetTypeInfo();
            tti.TypeName = tds.Identifier.Text;

            var ns = tds.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault();

            tti.TypeNamespace = GetFullNamespace(ns);

            tti.SerializerNamespace = $"{tti.TypeNamespace}.Serializers";
            tti.SerializerName = $"{tti.TypeName}Serializer";

            var publicProps = tds.Members.OfType<PropertyDeclarationSyntax>()
                .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
                .Where(x => !HasAttribute(x, semanticModel, _serializationIgnoreAttributeName))
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

            foreach(var p in publicProps)
            {
                var typeSymbol = semanticModel.GetTypeInfo(p.Type).Type;
                var pi = new TypePropertyInfo();
                pi.Name = p.Identifier.Text;
                pi.Type = typeSymbol.ToString();

                var orderAttr = GetAttribute(p, semanticModel, _serializationOrderIdAttributeName);
                if (orderAttr != null && orderAttr.ArgumentList.Arguments.Count != 0)
                {
                    var attrArg = orderAttr.ArgumentList.Arguments[0];

                    var constantValue = semanticModel.GetConstantValue(attrArg.Expression);
                    if (constantValue.HasValue)
                        pi.OrderId = (int)constantValue.Value;
                } 

                tti.Properties.Add(pi);
            }

            return tti;
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

        private AttributeSyntax GetAttribute(MemberDeclarationSyntax memberDeclarartion, SemanticModel model, string fullAttrName)
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


