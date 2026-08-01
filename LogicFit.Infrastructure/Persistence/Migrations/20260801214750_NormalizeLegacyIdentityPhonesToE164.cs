using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(LogicFit.Infrastructure.Persistence.ApplicationDbContext))]
    [Migration("20260801214750_NormalizeLegacyIdentityPhonesToE164")]
    public partial class NormalizeLegacyIdentityPhonesToE164 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [IdentityAccounts] AS [legacy]
                    INNER JOIN [IdentityAccounts] AS [e164]
                        ON [e164].[Id] <> [legacy].[Id]
                        AND [e164].[NormalizedPhoneNumber] = N'+20' + SUBSTRING([legacy].[NormalizedPhoneNumber], 2, 10)
                    WHERE [legacy].[NormalizedPhoneNumber] LIKE N'01%'
                        AND LEN([legacy].[NormalizedPhoneNumber]) = 11
                        AND [legacy].[NormalizedPhoneNumber] NOT LIKE N'%[^0-9]%'
                )
                    THROW 51001, 'LEGACY_PHONE_E164_CONFLICT', 1;

                UPDATE [IdentityAccounts]
                SET [PhoneNumber] = N'+20' + SUBSTRING([NormalizedPhoneNumber], 2, 10),
                    [NormalizedPhoneNumber] = N'+20' + SUBSTRING([NormalizedPhoneNumber], 2, 10)
                WHERE [NormalizedPhoneNumber] LIKE N'01%'
                    AND LEN([NormalizedPhoneNumber]) = 11
                    AND [NormalizedPhoneNumber] NOT LIKE N'%[^0-9]%';

                UPDATE [user]
                SET [user].[PhoneNumber] = [identity].[NormalizedPhoneNumber]
                FROM [DomainUsers] AS [user]
                INNER JOIN [IdentityAccounts] AS [identity]
                    ON [identity].[Id] = [user].[IdentityAccountId]
                WHERE [identity].[NormalizedPhoneNumber] LIKE N'+20%'
                    AND LEN([identity].[NormalizedPhoneNumber]) = 13;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phone normalization is a data correction and must not be reversed automatically.
        }
    }
}
