using System.Security;

namespace Dgmjr.DtoGenerator;

public static partial class Constants
{
    private const string _g = ".g";
    private const string _cs = ".cs";
    public const string _g_cs = _g + _cs;
    private const string scriban = nameof(scriban);
    private const string _scriban = "." + scriban;
    public const string Dto = nameof(Dto);
    public const string Dtos = nameof(Dtos);
    public const string GenerateDtoAttribute = nameof(GenerateDtoAttribute);
    public const string DtoIgnoreAttribute = nameof(DtoIgnoreAttribute);
    private const string DtoType = nameof(DtoType);
    private const string DataStructureType = nameof(DataStructureType);
    public const string GenerateDtoAttributeFilename = GenerateDtoAttribute + _g_cs;
    public const string AssemblyName = ThisAssembly.Project.AssemblyName;
    public const string AssemblyVersion = ThisAssembly.Info.Version;
    public const string CompilerGeneratedAttributes =
        $"[System.Runtime.CompilerServices.CompilerGenerated, System.CodeDom.Compiler.GeneratedCode(\"{AssemblyName}\", \"{AssemblyVersion}\")]";

    private static readonly string Header = typeof(Constants).Assembly.ReadAssemblyResourceAllText(
        nameof(Header) + _scriban
    );

    private static readonly Template HeaderTemplate = Parse(Header);

    public static string RenderHeader(string filename) =>
        HeaderTemplate.Render(new FilenameAndTimestampTuple(filename));

    private static readonly Template GenerateDtoAttributeTemplate = Parse(
        typeof(Constants).Assembly.ReadAssemblyResourceAllText(GenerateDtoAttribute + _scriban)
    );

    public static readonly string RenderedGenerateDtoAttributeDeclaration =
        RenderHeader(GenerateDtoAttributeFilename)
        + GenerateDtoAttributeTemplate.Render(
            new FilenameAndTimestampTuple(GenerateDtoAttributeFilename)
        );

    private static readonly Template DtoTypeTemplate = Parse(
        typeof(Constants).Assembly.ReadAssemblyResourceAllText(DtoType + _scriban)
    );

    public static readonly string RenderedDtoTypeDeclaration =
        RenderHeader(GenerateDtoAttributeFilename)
        + DtoTypeTemplate.Render(new FilenameAndTimestampTuple(DtoType + _g_cs));

    private static readonly Template DataStructureTypeTemplate = Parse(
        typeof(Constants).Assembly.ReadAssemblyResourceAllText(DataStructureType + _scriban)
    );

    public static readonly string RenderedDataStructureTypeDeclaration =
        RenderHeader(GenerateDtoAttributeFilename)
        + DataStructureTypeTemplate.Render(
            new FilenameAndTimestampTuple(DataStructureType + _g_cs)
        );
}
