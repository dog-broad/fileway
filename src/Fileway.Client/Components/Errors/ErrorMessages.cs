using Fileway.Shared.Errors;

namespace Fileway.Client.Components.Errors;

public static class ErrorMessages
{
    public sealed record ErrorEntry(string UserMessage, string SuggestedAction, bool Retryable);

    private static readonly Dictionary<string, ErrorEntry> _map = new(StringComparer.Ordinal)
    {
        [ErrorCodes.CorruptedFile]           = new("This file appears to be corrupted or incomplete.", "Try re-exporting from the original application.", false),
        [ErrorCodes.EncryptedFile]           = new("This file is password-protected.", "Remove the password in the original app, then try again.", false),
        [ErrorCodes.FormatMismatch]          = new("This file doesn't match the expected format.", "Check the file extension matches its actual contents.", false),
        [ErrorCodes.EmptyFile]               = new("This file appears to be empty.", "Upload a file with content.", false),
        [ErrorCodes.ZipBombDetected]         = new("This file was rejected for security reasons.", "Upload a standard archive file.", false),
        [ErrorCodes.FileTooLarge]            = new("This file exceeds the size limit for this tool.", "Try compressing the file first, or split it into smaller parts.", false),
        [ErrorCodes.TooManyFiles]            = new("Too many files selected for this operation.", "Reduce the number of files and try again.", false),
        [ErrorCodes.InvalidPageRange]        = new("The selected page range is not valid for this document.", "Check the page numbers and try again.", false),
        [ErrorCodes.InvalidPageOrder]        = new("The page order contains invalid or duplicate page numbers.", "Check the order and try again.", false),
        [ErrorCodes.UnsupportedEncoding]     = new("This file uses an unsupported text encoding.", "Convert the file to UTF-8 encoding and try again.", false),
        [ErrorCodes.MalformedJson]           = new("This doesn't appear to be valid JSON.", "Check for missing brackets, commas, or quotes.", false),
        [ErrorCodes.MalformedYaml]           = new("This doesn't appear to be valid YAML.", "Check for incorrect indentation or special characters.", false),
        [ErrorCodes.InvalidCsv]             = new("This CSV file has inconsistent columns.", "Ensure every row has the same number of columns.", false),
        [ErrorCodes.MalformedToml]           = new("This doesn't appear to be valid TOML.", "Check for syntax errors in key-value pairs or table headers.", false),
        [ErrorCodes.JsonNotCsvCompatible]    = new("JSON-to-CSV requires a flat array of objects.", "Wrap your records in a JSON array: [{\"field\": \"value\"}, ...]. Nested objects and arrays are not supported.", false),
        [ErrorCodes.JobTimeout]              = new("This conversion took too long and was stopped.", "Try with a smaller file, or split it into parts.", true),
        [ErrorCodes.RateLimitExceeded]       = new("Too many conversions in a short time.", "Wait a moment and try again.", true),
        [ErrorCodes.QueueFull]               = new("The server is very busy right now.", "Wait a moment and try again.", true),
        [ErrorCodes.ConversionFailed]        = new("The conversion could not be completed.", "Try a different file or check if the file is valid.", true),
        [ErrorCodes.ProcessorUnexpectedError]= new("Something went wrong on our end.", "This has been logged. Please try again in a moment.", true),
    };

    public static ErrorEntry? Get(string? errorCode)
    {
        if (errorCode is null) return null;
        return _map.TryGetValue(errorCode, out var entry) ? entry : null;
    }
}
