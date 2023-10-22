namespace Dgmjr.DtoGenerator;

public static class AttributeDataExtensions
{
    public static T? GetConstructorArgument<T>(this AttributeData attributeData, int index)
    {
        var args = attributeData.ConstructorArguments;
        if (args.Length > index)
        {
            return (T?)args[index].Value;
        }

        return default;
    }
}
