using System.IO.Compression;
using System.Text;
using LogicFit.Infrastructure.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class BacpacPackageInspectorTests
{
    [Fact]
    public async Task Inspects_structure_without_returning_archive_contents()
    {
        await using var stream = CreatePackage();

        var result = await BacpacPackageInspector.InspectAsync(stream);

        Assert.True(result.IsValid);
        Assert.True(result.HasModel);
        Assert.True(result.HasOrigin);
        Assert.Equal(2, result.TableCount);
        Assert.Equal(1, result.DataEntryCount);
        Assert.DoesNotContain("Members", result.ErrorCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_non_bacpac_input_with_safe_error_code()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not-a-bacpac"));

        var result = await BacpacPackageInspector.InspectAsync(stream);

        Assert.False(result.IsValid);
        Assert.Equal("BACPAC_INVALID_ARCHIVE", result.ErrorCode);
    }

    private static MemoryStream CreatePackage()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "origin.xml", "<Origin />");
            WriteEntry(archive, "model.xml", "<Model><Element Type=\"SqlTable\" Name=\"Members\" /><Element Type=\"SqlTable\" Name=\"Payments\" /></Model>");
            WriteEntry(archive, "Data/schema/table.bcp", "payload");
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
