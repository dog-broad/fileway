# Error Model

---

## Propagation Pipeline

```
Processor throws ProcessorDomainException(ErrorCode)
  → JobDispatcher catches → writes Failed SSE event with FailedPayload
    → SseClient.cs deserialises FailedPayload
      → ToolStateService updates to Failed state
        → ErrorPanel.razor renders userMessage + suggestedAction
```

All error copy lives in one place: `Fileway.Client/Components/Errors/ErrorMessages.cs`  
All error code constants in one place: `Fileway.Shared/Errors/ErrorCodes.cs`

No layer invents its own error copy. No layer invents its own error code strings.

---

## ErrorPanel Rendering Rules

| Condition | UI behaviour |
|---|---|
| `retryable: true` | Show "Try again" button — re-submits identical job |
| `retryable: false` | Show "What else can I do?" + RelatedSlugs as suggestion chips |
| `errorCode` not in ErrorMessages.cs | Fall back to API's `userMessage` + `suggestedAction` verbatim |
| Never | Show blank panel, raw exception, or stack trace |

---

## Complete Error Copy

| errorCode | userMessage | suggestedAction | retryable |
|---|---|---|---|
| CorruptedFile | This file appears to be corrupted or incomplete. | Try re-exporting from the original application. | false |
| EncryptedFile | This file is password-protected. | Remove the password in the original app, then try again. | false |
| FormatMismatch | This file doesn't match the expected format. | Check the file extension matches its actual contents. | false |
| EmptyFile | This file appears to be empty. | Upload a file with content. | false |
| ZipBombDetected | This file was rejected for security reasons. | Upload a standard archive file. | false |
| FileTooLarge | This file exceeds the size limit for this tool. | Try compressing the file first, or split it into smaller parts. | false |
| TooManyFiles | Too many files selected for this operation. | Reduce the number of files and try again. | false |
| InvalidPageRange | The selected page range is not valid for this document. | Check the page numbers and try again. | false |
| InvalidPageOrder | The page order contains invalid or duplicate page numbers. | Check the order and try again. | false |
| UnsupportedEncoding | This file uses an unsupported text encoding. | Convert the file to UTF-8 encoding and try again. | false |
| MalformedJson | This doesn't appear to be valid JSON. | Check for missing brackets, commas, or quotes. | false |
| MalformedYaml | This doesn't appear to be valid YAML. | Check for incorrect indentation or special characters. | false |
| InvalidCsv | This CSV file has inconsistent columns. | Ensure every row has the same number of columns. | false |
| JsonNotCsvCompatible | JSON-to-CSV requires a flat array of objects. | Wrap your records in a JSON array: [{"field": "value"}, ...]. Nested objects and arrays are not supported. | false |
| JobTimeout | This conversion took too long and was stopped. | Try with a smaller file, or split it into parts. | true |
| RateLimitExceeded | Too many conversions in a short time. | Wait a moment and try again. | true |
| QueueFull | The server is very busy right now. | Wait a moment and try again. | true |
| ConversionFailed | The conversion could not be completed. | Try a different file or check if the file is valid. | true |
| ProcessorUnexpectedError | Something went wrong on our end. | This has been logged. Please try again in a moment. | true |

---

## Rate Limit UX — Special Case

When `errorCode` is `RateLimitExceeded`, the ErrorPanel reads the `Retry-After` header from the response and shows a live countdown timer: **"Try again in 3s"**. The upload button re-enables automatically when the countdown reaches zero. This requires `ApiJobClient.cs` to expose the `Retry-After` header value alongside the error response.

---

## Exception Classes (Fileway.Shared/Errors/)

### ProcessorValidationException
Fields: `ErrorCode` (string), `UserMessage` (string), `SuggestedAction` (string)  
Thrown by: `ValidateOptions()`  
Caught by: JobDispatcher before job creation → 422 ProblemDetails

### ProcessorDomainException  
Fields: `ErrorCode` (string), `UserMessage` (string), `SuggestedAction` (string), `Retryable` (bool)  
Thrown by: `ExecuteAsync()` for known, expected failure modes  
Caught by: JobDispatcher → Failed SSE event

### ProcessorUnexpectedException
Fields: `InnerException` (logged only — never sent to client)  
Thrown by: `ExecuteAsync()` wrapping any unanticipated exception  
Caught by: JobDispatcher → Failed SSE event with ProcessorUnexpectedError code
