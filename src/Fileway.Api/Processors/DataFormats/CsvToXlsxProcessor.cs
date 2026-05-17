using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;

namespace Fileway.Api.Processors.DataFormats;

public sealed class CsvToXlsxProcessor : IApiProcessor
{
    public void ValidateOptions(JsonElement toolOptions)
    {
        // No tool options defined for csv-to-xlsx
    }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var input = context.InputFiles[0];
        var csvText = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        byte[] xlsxBytes;
        try
        {
            xlsxBytes = ConvertCsvToXlsx(csvText);
        }
        catch (CsvHelperException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.InvalidCsv,
                "Could not parse the CSV content.", ex);
        }

        var outputFilename = BuildOutputFilename(input.OriginalFilename);

        return new ProcessorResult
        {
            OutputContent = xlsxBytes,
            OutputFormat = FileFormats.Xlsx,
            OutputFilename = outputFilename
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
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Write header row
        if (!csv.Read() || !csv.ReadHeader())
        {
            worksheet.Cell(1, 1).Value = "(empty)";
        }
        else
        {
            var headers = csv.HeaderRecord!;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
            }

            // Write data rows
            int row = 2;
            while (csv.Read())
            {
                for (int col = 0; col < headers.Length; col++)
                {
                    var rawValue = csv.GetField(col) ?? string.Empty;
                    var cell = worksheet.Cell(row, col + 1);

                    if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                        cell.Value = number;
                    else if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        cell.Value = date;
                    else
                        cell.Value = rawValue;
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string BuildOutputFilename(string? originalFilename)
    {
        if (string.IsNullOrWhiteSpace(originalFilename))
            return "output.xlsx";

        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFilename);
        // Sanitise: strip path separators and other unsafe chars
        var safe = string.Concat(nameWithoutExt
            .Where(c => c != '/' && c != '\\' && c != ':' && c != '*' && c != '?' && c != '"' && c != '<' && c != '>' && c != '|'));
        return string.IsNullOrWhiteSpace(safe) ? "output.xlsx" : $"{safe}.xlsx";
    }
}
