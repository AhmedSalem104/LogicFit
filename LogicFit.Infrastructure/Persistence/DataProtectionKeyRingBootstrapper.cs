using System.Xml.Linq;
using LogicFit.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Imports legacy file-system keys into the central Platform key store and mirrors central keys
/// back to the durable application directory. This runs before any protected database resource is
/// read, so moving from the old file ring to the database ring does not silently invalidate keys.
/// </summary>
public sealed class DataProtectionKeyRingBootstrapper(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ILoggerFactory loggerFactory,
    ILogger<DataProtectionKeyRingBootstrapper> logger)
{
    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var directory = DataProtectionKeyDirectory.Resolve(configuration);
        Directory.CreateDirectory(directory);

        var fileRepository = new FileSystemXmlRepository(new DirectoryInfo(directory), loggerFactory);
        var fileElements = fileRepository.GetAllElements().ToArray();
        var databaseKeys = await dbContext.DataProtectionKeys
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var databaseXml = databaseKeys
            .Select(key => NormalizeXml(key.Xml))
            .ToHashSet(StringComparer.Ordinal);

        var imported = 0;
        foreach (var element in fileElements)
        {
            var xml = NormalizeXml(element.ToString(SaveOptions.DisableFormatting));
            if (!databaseXml.Add(xml))
                continue;

            dbContext.DataProtectionKeys.Add(new DataProtectionKey
            {
                FriendlyName = GetFriendlyName(element),
                Xml = xml
            });
            imported++;
        }

        if (imported > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported {ImportedKeyCount} Data Protection key(s) into the central Platform key store.",
                imported);
        }

        var mirrored = 0;
        var fileXml = fileElements
            .Select(element => NormalizeXml(element.ToString(SaveOptions.DisableFormatting)))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var key in databaseKeys.Concat(
                     imported == 0
                         ? Array.Empty<DataProtectionKey>()
                         : await dbContext.DataProtectionKeys.AsNoTracking().ToListAsync(cancellationToken)))
        {
            var xml = NormalizeXml(key.Xml);
            if (!fileXml.Add(xml))
                continue;

            try
            {
                fileRepository.StoreElement(XElement.Parse(xml, LoadOptions.PreserveWhitespace),
                    string.IsNullOrWhiteSpace(key.FriendlyName) ? GetFriendlyName(XElement.Parse(xml)) : key.FriendlyName);
                mirrored++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The database is now authoritative. A read-only application directory must not
                // make a healthy central key store unusable, but it is still operationally visible.
                logger.LogWarning(
                    exception,
                    "Could not mirror a Data Protection key to {KeyDirectory}; the central key store remains authoritative.",
                    directory);
            }
        }

        if (mirrored > 0)
        {
            logger.LogInformation(
                "Mirrored {MirroredKeyCount} central Data Protection key(s) to {KeyDirectory}.",
                mirrored,
                directory);
        }
    }

    private static string NormalizeXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new InvalidOperationException("The central Data Protection key store contains an empty XML key.");

        return XElement.Parse(xml, LoadOptions.PreserveWhitespace).ToString(SaveOptions.DisableFormatting);
    }

    private static string GetFriendlyName(XElement element)
        => $"key-{element.Attribute("id")?.Value ?? Guid.NewGuid().ToString("D")}";
}
