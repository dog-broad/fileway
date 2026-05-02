namespace Fileway.Shared.Errors;

public static class ErrorCodes
{
    // Validation (4xx)
    public const string InvalidSessionToken = "InvalidSessionToken";
    public const string MalformedOptions = "MalformedOptions";
    public const string UnknownToolSlug = "UnknownToolSlug";
    public const string InvalidOutputFormat = "InvalidOutputFormat";
    public const string MissingFilePart = "MissingFilePart";
    public const string TooManyFiles = "TooManyFiles";
    public const string JobNotFound = "JobNotFound";
    public const string JobNotOwned = "JobNotOwned";
    public const string FileTooLarge = "FileTooLarge";
    public const string UnsupportedMediaType = "UnsupportedMediaType";
    public const string RateLimitExceeded = "RateLimitExceeded";
    public const string QueueFull = "QueueFull";
    public const string ConcurrentJobLimit = "ConcurrentJobLimit";

    // File / Format (422)
    public const string FormatMismatch = "FormatMismatch";
    public const string CorruptedFile = "CorruptedFile";
    public const string EncryptedFile = "EncryptedFile";
    public const string EmptyFile = "EmptyFile";
    public const string ZipBombDetected = "ZipBombDetected";
    public const string PolyglotDetected = "PolyglotDetected";
    public const string UnsupportedEncoding = "UnsupportedEncoding";
    public const string InvalidPageRange = "InvalidPageRange";
    public const string TooManyPages = "TooManyPages";
    public const string InvalidPageOrder = "InvalidPageOrder";
    public const string ImageTooLarge = "ImageTooLarge";
    public const string InvalidDimensions = "InvalidDimensions";
    public const string MalformedJson = "MalformedJson";
    public const string MalformedYaml = "MalformedYaml";
    public const string InvalidCsv = "InvalidCsv";
    public const string MalformedToml = "MalformedToml";
    public const string JsonNotCsvCompatible = "JsonNotCsvCompatible";

    // Processing (5xx)
    public const string JobTimeout = "JobTimeout";
    public const string ProcessorUnexpectedError = "ProcessorUnexpectedError";
    public const string StorageWriteFailed = "StorageWriteFailed";
    public const string LibreOfficeUnavailable = "LibreOfficeUnavailable";
    public const string LibreOfficeTimeout = "LibreOfficeTimeout";
    public const string OnnxModelUnavailable = "OnnxModelUnavailable";
    public const string OutputValidationFailed = "OutputValidationFailed";
    public const string ConversionFailed = "ConversionFailed";
}
