using Xunit;

namespace LogicFit.Tests;

public sealed class PrivatePaymentProofStorageContractTests
{
    [Fact]
    public void Public_static_file_pipeline_blocks_uploaded_documents_before_static_files()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LogicFit.API", "Program.cs"));
        var guard = source.IndexOf("/uploads/documents", StringComparison.Ordinal);
        var staticFiles = source.IndexOf("app.UseStaticFiles();", StringComparison.Ordinal);

        Assert.True(guard >= 0);
        Assert.True(staticFiles > guard);
        Assert.Contains("StatusCodes.Status404NotFound", source, StringComparison.Ordinal);
    }
}
