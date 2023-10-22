using System.Reflection.Metadata;

namespace Dgmjr.DtoGenerator;

#pragma warning disable CA1822
internal readonly record struct FilenameAndTimestampTuple(string Filename)
{
    public string Timestamp => DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzzZ");
    public string ToolName => Constants.AssemblyName;
    public string ToolVersion => Constants.AssemblyVersion;
    public string CompilerGeneratedAttributes => Constants.CompilerGeneratedAttributes;
}
#pragma warning restore
