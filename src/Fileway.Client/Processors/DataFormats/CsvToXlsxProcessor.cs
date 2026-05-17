using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;

namespace Fileway.Client.Processors.DataFormats;

public sealed class CsvToXlsxProcessor : IWasmProcessor
{
    private const long WasmThresholdBytes = 5 * 1024 * 1024;

    public bool CanHandleSize(long fileSizeBytes) => fileSizeBytes <= WasmThresholdBytes;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];
        var csvText = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        byte[] xlsxBytes;
        try { xlsxBytes = ConvertCsvToXlsx(csvText); }
        catch (CsvHelperException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.InvalidCsv, "Could not parse the CSV content.", ex);
        }

        return new ProcessorResult
        {
            OutputContent = xlsxBytes,
            OutputFormat = FileFormats.Xlsx,
            OutputFilename = BuildFilename(input.OriginalFilename)
        };
    }

    private static byte[] ConvertCsvToXlsx(string csvText)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        };

        using var reader = new StringReader(csvText);
        using var csv = new CsvReader(reader, config);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Sheet1");

        if (!csv.Read() || !csv.ReadHeader())
        {
            sheet.Cell(1, 1).Value = "(empty)";
        }
        else
        {
            var headers = csv.HeaderRecord!;
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
            }

            int row = 2;
            while (csv.Read())
            {
                for (int c = 0; c < headers.Length; c++)
                {
                    var raw = csv.GetField(c) ?? string.Empty;
                    var cell = sheet.Cell(row, c + 1);
                    if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                        cell.Value = num;
                    else if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        cell.Value = dt;
                    else
                        cell.Value = raw;
                }
                row++;
            }

            sheet.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string BuildFilename(string? original)
    {
        if (string.IsNullOrWhiteSpace(original)) return "output.xlsx";
        var safe = string.Concat(Path.GetFileNameWithoutExtension(original)
            .Where(c => c != '/' && c != '\\' && c != ':' && c != '*'
                     && c != '?' && c != '"' && c != '<' && c != '>' && c != '|'));
        return string.IsNullOrWhiteSpace(safe) ? "output.xlsx" : $"{safe}.xlsx";
    }
}
