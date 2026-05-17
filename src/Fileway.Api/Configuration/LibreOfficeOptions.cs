namespace Fileway.Api.Configuration;

public sealed class LibreOfficeOptions
{
    public const string SectionName = "LibreOffice";

    public string ExecutablePath { get; init; } = "soffice";
    public int MaxConcurrent { get; init; } = 2;
    public string TempBasePath { get; init; } = "/tmp/fileway/";
}
