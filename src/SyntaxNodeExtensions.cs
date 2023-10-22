using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dgmjr.DtoGenerator;

internal static class SyntaxNodeExtensions
{
    public static AttributeSyntax? GetAttribute(
        this SyntaxNode syntaxNode,
        string attributeMetadataName
    )
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

        return attributeLists
            .SelectMany(x => x.Attributes)
            .FirstOrDefault(x => x.Name.ToString() == attributeMetadataName);
    }
}
