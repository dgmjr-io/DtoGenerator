namespace Dgmjr.DtoGenerator;

public static class ISymbolExtensions
{
    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName)
    {
        return symbol
            .GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.MetadataName == attributeName);
    }
}
