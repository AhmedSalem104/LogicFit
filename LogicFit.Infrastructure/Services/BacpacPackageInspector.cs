using System.IO.Compression;
using System.Xml;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Reads only the structural metadata of a server-created BACPAC. It never returns
/// table names, row data, connection material, or archive contents.
/// </summary>
public sealed record BacpacInspectionResult(
    bool IsValid,
    int EntryCount,
    int DataEntryCount,
    int TableCount,
    bool HasModel,
    bool HasOrigin,
    string? ErrorCode);

public static class BacpacPackageInspector
{
    private const long MaxModelCharacters = 64 * 1024 * 1024;

    public static async Task<BacpacInspectionResult> InspectAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            var model = FindEntry(archive, "model.xml");
            var origin = FindEntry(archive, "origin.xml");
            var dataEntryCount = archive.Entries.Count(entry =>
                entry.FullName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.StartsWith("Data\\", StringComparison.OrdinalIgnoreCase));

            if (model is null || origin is null)
            {
                return new BacpacInspectionResult(
                    false,
                    archive.Entries.Count,
                    dataEntryCount,
                    0,
                    model is not null,
                    origin is not null,
                    "BACPAC_METADATA_MISSING");
            }

            var tableCount = await CountTablesAsync(model, cancellationToken);
            return new BacpacInspectionResult(
                true,
                archive.Entries.Count,
                dataEntryCount,
                tableCount,
                true,
                true,
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            return Invalid("BACPAC_INVALID_ARCHIVE");
        }
        catch (XmlException)
        {
            return Invalid("BACPAC_INVALID_MODEL");
        }
        catch (IOException)
        {
            return Invalid("BACPAC_READ_FAILED");
        }
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive archive, string fileName)
        => archive.Entries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileName(entry.FullName), fileName, StringComparison.OrdinalIgnoreCase));

    private static async Task<int> CountTablesAsync(
        ZipArchiveEntry model,
        CancellationToken cancellationToken)
    {
        await using var modelStream = model.Open();
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxModelCharacters
        };
        using var reader = XmlReader.Create(modelStream, settings);
        var tableCount = 0;
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element &&
                string.Equals(reader.GetAttribute("Type"), "SqlTable", StringComparison.OrdinalIgnoreCase))
            {
                tableCount++;
            }
        }

        return tableCount;
    }

    private static BacpacInspectionResult Invalid(string errorCode)
        => new(false, 0, 0, 0, false, false, errorCode);
}
