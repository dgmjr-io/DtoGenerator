using System.Reflection.Metadata.Ecma335;
using System.Net.Security;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Dgmjr.DtoGenerator;

[Generator]
public partial class DtoGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context => context.AddSource(GenerateDtoAttributeFilename, GenerateDtoAttributeDeclaration));

        var validDtoDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(GenerateDtoAttributeName,
            IsEligibleType,
            (context, _) => context).Collect();
        var contextAndValidDtos = context.CompilationProvider.Combine(validDtoDeclarations);

        context.RegisterSourceOutput(contextAndValidDtos, GenerateDtos);
    }

    private static bool IsPartialType(SyntaxNode node)
    {
        return (node is ClassDeclarationSyntax cls && cls.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword)))
            || (node is StructDeclarationSyntax @struct && @struct.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword)))
            || (node is InterfaceDeclarationSyntax @interface && @interface.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static bool IsEligibleType(SyntaxNode node, CancellationToken _ = default)
    {
        return (node is ClassDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax) && /*IsPartialType(node) && */node.GetAttribute(GenerateDtoAttributeName) != null;
    }

    private static void GenerateDtos(SourceProductionContext context, (Compilation compilation, ImmutableArray<GeneratorAttributeSyntaxContext> generateDtoTypes) values)
    {
        (Compilation compilation, ImmutableArray<GeneratorAttributeSyntaxContext> generateDtoTypes) = values;
        // Generate code for each type
        foreach (var generateDtoType in generateDtoTypes)
        {
            var dtoAttribute = generateDtoType.TargetSymbol.GetAttributes().Single(attr => attr.AttributeClass.MetadataName.Equals(GenerateDtoAttributeName));
            var dtoType = dtoAttribute.NamedArguments.SingleOrDefault(arg => arg.Key == "dtoType").Value.Value as DtoType? ?? DtoType.Dto;
            var dataStructureType = dtoAttribute.NamedArguments.SingleOrDefault(arg => arg.Key == "dataStructureType").Value.Value as DataStructureType? ?? DataStructureType.RecordStruct;
            var dtoTypeName = dtoAttribute.NamedArguments.SingleOrDefault(arg => arg.Key == "dtoTypeName").Value.Value as string ?? $"{generateDtoType.TargetSymbol.Name}{dtoType}{(dtoType is not DtoType.Dto ? "Dto" : "")}";
            var @namespace = dtoAttribute.NamedArguments.SingleOrDefault(arg => arg.Key == "@namespace").Value.Value as string ?? $"{generateDtoType.TargetSymbol.ContainingNamespace}.Dtos";

            if ((generateDtoType.TargetSymbol as INamedTypeSymbol)?.TypeKind is /*TypeKind.Interface or*/ TypeKind.Enum)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("DTO001", "Invalid type", "GenerateDtoAttribute can only decorate classes and structs", "Code", DiagnosticSeverity.Error, true), Location.Create(generateDtoType.TargetNode.SyntaxTree, generateDtoType.TargetNode.Span)));
                continue;
            }

            var modelTypeSymbol = generateDtoType.TargetSymbol as INamedTypeSymbol;

            var dtoProperties = modelTypeSymbol.GetMembers().OfType<IPropertySymbol>()
                .Where(prop => prop.DeclaredAccessibility == Accessibility.Public && !prop.IsReadOnly && !prop.GetAttributes().Any(attr => attr.AttributeClass.Name == "IgnoreDtoPropertyAttribute" || attr.AttributeClass.Name == "KeyAttribute"))
                .Select(prop =>
                {
                    var isKey = prop.GetAttributes().Any(attr => attr.AttributeClass.Name == "KeyAttribute");
                    var databaseGeneratedAttribute = prop.GetAttributes().SingleOrDefault(attr => attr.AttributeClass.Name == "DatabaseGeneratedAttribute");
                    var hasDatabaseGeneratedAttribute = databaseGeneratedAttribute?.ConstructorArguments.Length == 1 && (databaseGeneratedAttribute.ConstructorArguments[0].Value as DatabaseGeneratedOption? ?? DatabaseGeneratedOption.None) != DatabaseGeneratedOption.None;
                    var isReadOnly = prop.IsReadOnly;
                    var isRequired = prop.IsRequired;
                    var ignore = (dtoType is DtoType.Insert or DtoType.Edit) && (isKey || hasDatabaseGeneratedAttribute);
                    var attributes = new[] { isRequired ? SyntaxGenerator.ParseTypeName("RequiredAttribute") : nulll }.WhereNotNull();
                    return (prop, ignore);
                })
                .Where(tuple => !tuple.ignore)
                .Select(tuple => tuple.prop)
                .ToList();

            var dtoDeclaration = SyntaxFactory.TypeDeclaration(ToSyntaxKind(dataStructureType), dtoTypeName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Serializable"))))))
                .WithTypeParameterList(modelTypeSymbol.TypeParameters.ToTypeParameterListSyntax())
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(dtoProperties.Select(prop =>
                {
                    var propType = prop.Type;

                    var dtoProp = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propType.ToDisplayString()), prop.Name)
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        })));

                    return dtoProp;
                })));

            var dtoNamespace = SyntaxFactory.ParseName(@namespace);
            var dtoNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(dtoNamespace)
                .WithUsings(SyntaxFactory.List(new[]
                {
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("AutoMapper")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(modelTypeSymbol.ContainingNamespace.ToDisplayString()))
                }))
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
                {
                    dtoDeclaration,
                    GenerateAutoMapperProfile(modelTypeSymbol, dtoNamespace, dataStructureType, dtoTypeName)
                }));

            var dtoCompilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(new[]
                {
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq"))
                }))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(dtoNamespaceDeclaration));

            var sourceText = SourceText.From(dtoCompilationUnit.NormalizeWhitespace().ToFullString(), UTF8);
            context.AddSource($"{dtoTypeName}.cs", sourceText);
        }
    }

    private static SyntaxKind ToSyntaxKind(DataStructureType dataStructureType)
    {
        return dataStructureType switch
        {
            DataStructureType.Class => SyntaxKind.ClassDeclaration,
            DataStructureType.Struct => SyntaxKind.StructDeclaration,
            DataStructureType.RecordClass => SyntaxKind.RecordDeclaration,
            DataStructureType.RecordStruct => SyntaxKind.RecordStructDeclaration,
            _ => throw new InvalidOperationException($"Invalid data structure type {dataStructureType}")
        };
    }
    private static ClassDeclarationSyntax GenerateAutoMapperProfile(INamedTypeSymbol sourceType, NameSyntax dtoNamespace, DataStructureType dataStructureType, string dtoTypeName)
    {
        var profileClass = SyntaxFactory.ClassDeclaration($"{dtoTypeName}Profile")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Profile")))))
            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[]
            {
                GenerateAutoMapperMappings(sourceType, dtoNamespace, dataStructureType, dtoTypeName),
                GenerateAutoMapperReverseMappings(sourceType, dtoNamespace, dataStructureType, dtoTypeName)
            }));

        return profileClass;
    }

    private static MethodDeclarationSyntax GenerateAutoMapperMappings(INamedTypeSymbol sourceType, NameSyntax dtoNamespace, DataStructureType dataStructureType, string dtoTypeName)
    {
        var sourceTypeSyntax = SyntaxFactory.ParseTypeName(sourceType.ToDisplayString());
        var dtoTypeSyntax = SyntaxFactory.ParseTypeName($"{dtoNamespace}.{dtoTypeName}");
        var mappingMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "ConfigureMappings")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(SyntaxFactory.Identifier("configuration"))
                .WithType(SyntaxFactory.ParseTypeName("IProfileExpression")))))
            .WithBody(SyntaxFactory.Block(
            SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("configuration"),
                    SyntaxFactory.IdentifierName("CreateMap")))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                {
                    SyntaxFactory.Argument(sourceTypeSyntax),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    SyntaxFactory.Argument(dtoTypeSyntax)
                }))))
            ));

        return mappingMethod;
    }

    private static MethodDeclarationSyntax GenerateAutoMapperReverseMappings(INamedTypeSymbol sourceType, NameSyntax dtoNamespace, DataStructureType dataStructureType, string dtoTypeName)
    {
        var sourceTypeSyntax = SyntaxFactory.ParseTypeName(sourceType.ToDisplayString());
        var dtoTypeSyntax = SyntaxFactory.ParseTypeName($"{dtoNamespace}.{dtoTypeName}, {dtoNamespace}");
        var mappingMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "ConfigureReverseMappings")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(SyntaxFactory.Identifier("configuration"))
                .WithType(SyntaxFactory.ParseTypeName("IProfileExpression")))))
            .WithBody(SyntaxFactory.Block(
            SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("configuration"),
                    SyntaxFactory.IdentifierName("CreateMap")))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                {
                    SyntaxFactory.Argument(dtoTypeSyntax),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    SyntaxFactory.Argument(sourceTypeSyntax)
                }))))
            ));

        return mappingMethod;
    }
}

internal static class SyntaxNodeExtensions
{
    public static AttributeSyntax? GetAttribute(this SyntaxNode syntaxNode, string attributeMetadataName)
    {
        SyntaxList<AttributeListSyntax> attributeLists;
        if (syntaxNode is BaseTypeDeclarationSyntax tds)
        {
            attributeLists = tds.AttributeLists;
        }
        else if (syntaxNode is BaseFieldDeclarationSyntax fds)
        {
            attributeLists = fds.AttributeLists;
        }
        else if (syntaxNode is BaseMethodDeclarationSyntax mds)
        {
            attributeLists = mds.AttributeLists;
        }
        else if (syntaxNode is BasePropertyDeclarationSyntax pds)
        {
            attributeLists = pds.AttributeLists;
        }
        else
        {
            attributeLists = SyntaxFactory.List<AttributeListSyntax>();
        }

        return attributeLists.SelectMany(x => x.Attributes).FirstOrDefault(x => x.Name.ToString() == attributeMetadataName);
    }
}
