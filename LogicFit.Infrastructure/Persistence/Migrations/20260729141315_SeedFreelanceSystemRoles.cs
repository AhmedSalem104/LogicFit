using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreelanceSystemRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @now datetime2 = SYSUTCDATETIME();
                DECLARE @freelanceOwnerRoleId uniqueidentifier = (
                    SELECT TOP (1) [Id]
                    FROM [Roles]
                    WHERE [TenantId] IS NULL AND [Name] = N'FreelanceOwner'
                    ORDER BY CASE WHEN [IsDeleted] = 0 THEN 0 ELSE 1 END, [CreatedAt]);
                DECLARE @freelanceCoachRoleId uniqueidentifier = (
                    SELECT TOP (1) [Id]
                    FROM [Roles]
                    WHERE [TenantId] IS NULL AND [Name] = N'FreelanceCoach'
                    ORDER BY CASE WHEN [IsDeleted] = 0 THEN 0 ELSE 1 END, [CreatedAt]);
                DECLARE @freelanceAssistantRoleId uniqueidentifier = (
                    SELECT TOP (1) [Id]
                    FROM [Roles]
                    WHERE [TenantId] IS NULL AND [Name] = N'FreelanceAssistant'
                    ORDER BY CASE WHEN [IsDeleted] = 0 THEN 0 ELSE 1 END, [CreatedAt]);

                IF @freelanceOwnerRoleId IS NULL
                BEGIN
                    SET @freelanceOwnerRoleId = '1a593cd7-5814-4a3d-93d0-6110d57f91a3';
                    INSERT INTO [Roles] ([Id], [TenantId], [Name], [NameAr], [NormalizedName], [Description], [IsSystemRole], [IsDeleted], [DeletedAt], [DeletedBy], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                    VALUES (@freelanceOwnerRoleId, NULL, N'FreelanceOwner', N'مالك المدرب الحر', N'FREELANCEOWNER', N'System role: FreelanceOwner', 1, 0, NULL, NULL, @now, N'SeedFreelanceSystemRoles', NULL, NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Roles]
                    SET [NameAr] = N'مالك المدرب الحر', [NormalizedName] = N'FREELANCEOWNER', [Description] = N'System role: FreelanceOwner', [IsSystemRole] = 1,
                        [IsDeleted] = 0, [DeletedAt] = NULL, [DeletedBy] = NULL, [UpdatedAt] = @now, [UpdatedBy] = N'SeedFreelanceSystemRoles'
                    WHERE [Id] = @freelanceOwnerRoleId;
                END;

                IF @freelanceCoachRoleId IS NULL
                BEGIN
                    SET @freelanceCoachRoleId = 'cfabfd1c-7d86-4b0d-82ef-40e6e08b18c6';
                    INSERT INTO [Roles] ([Id], [TenantId], [Name], [NameAr], [NormalizedName], [Description], [IsSystemRole], [IsDeleted], [DeletedAt], [DeletedBy], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                    VALUES (@freelanceCoachRoleId, NULL, N'FreelanceCoach', N'مدرب حر', N'FREELANCECOACH', N'System role: FreelanceCoach', 1, 0, NULL, NULL, @now, N'SeedFreelanceSystemRoles', NULL, NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Roles]
                    SET [NameAr] = N'مدرب حر', [NormalizedName] = N'FREELANCECOACH', [Description] = N'System role: FreelanceCoach', [IsSystemRole] = 1,
                        [IsDeleted] = 0, [DeletedAt] = NULL, [DeletedBy] = NULL, [UpdatedAt] = @now, [UpdatedBy] = N'SeedFreelanceSystemRoles'
                    WHERE [Id] = @freelanceCoachRoleId;
                END;

                IF @freelanceAssistantRoleId IS NULL
                BEGIN
                    SET @freelanceAssistantRoleId = 'dbb62718-0ab7-442d-820d-8bdc7a320f4d';
                    INSERT INTO [Roles] ([Id], [TenantId], [Name], [NameAr], [NormalizedName], [Description], [IsSystemRole], [IsDeleted], [DeletedAt], [DeletedBy], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                    VALUES (@freelanceAssistantRoleId, NULL, N'FreelanceAssistant', N'مساعد مدرب', N'FREELANCEASSISTANT', N'System role: FreelanceAssistant', 1, 0, NULL, NULL, @now, N'SeedFreelanceSystemRoles', NULL, NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Roles]
                    SET [NameAr] = N'مساعد مدرب', [NormalizedName] = N'FREELANCEASSISTANT', [Description] = N'System role: FreelanceAssistant', [IsSystemRole] = 1,
                        [IsDeleted] = 0, [DeletedAt] = NULL, [DeletedBy] = NULL, [UpdatedAt] = @now, [UpdatedBy] = N'SeedFreelanceSystemRoles'
                    WHERE [Id] = @freelanceAssistantRoleId;
                END;

                DECLARE @rolePermissions TABLE ([RoleId] uniqueidentifier NOT NULL, [PermissionCode] nvarchar(100) NOT NULL);

                INSERT INTO @rolePermissions ([RoleId], [PermissionCode]) VALUES
                    (@freelanceOwnerRoleId, N'ManageMembers'), (@freelanceOwnerRoleId, N'ViewMembers'), (@freelanceOwnerRoleId, N'CreateMembers'),
                    (@freelanceOwnerRoleId, N'UpdateMembers'), (@freelanceOwnerRoleId, N'DeleteMembers'), (@freelanceOwnerRoleId, N'ManageCoaches'),
                    (@freelanceOwnerRoleId, N'ManageAttendance'), (@freelanceOwnerRoleId, N'ManageClientSubscriptions'), (@freelanceOwnerRoleId, N'ManagePOS'),
                    (@freelanceOwnerRoleId, N'ManageInventory'), (@freelanceOwnerRoleId, N'ManageEmployees'), (@freelanceOwnerRoleId, N'ManageBranches'),
                    (@freelanceOwnerRoleId, N'ManageFinance'), (@freelanceOwnerRoleId, N'ViewReports'), (@freelanceOwnerRoleId, N'ManageReports'),
                    (@freelanceOwnerRoleId, N'ManageSettings'), (@freelanceOwnerRoleId, N'ManageTenantBilling'),
                    (@freelanceCoachRoleId, N'ViewMembers'), (@freelanceCoachRoleId, N'CreateMembers'), (@freelanceCoachRoleId, N'UpdateMembers'),
                    (@freelanceCoachRoleId, N'ManageCoaches'), (@freelanceCoachRoleId, N'ManageAttendance'), (@freelanceCoachRoleId, N'ManageClientSubscriptions'),
                    (@freelanceCoachRoleId, N'ViewReports'),
                    (@freelanceAssistantRoleId, N'ViewMembers'), (@freelanceAssistantRoleId, N'CreateMembers'), (@freelanceAssistantRoleId, N'UpdateMembers'),
                    (@freelanceAssistantRoleId, N'ManageAttendance'), (@freelanceAssistantRoleId, N'ManageClientSubscriptions');

                IF EXISTS (
                    SELECT 1
                    FROM @rolePermissions AS [mapping]
                    LEFT JOIN [Permissions] AS [permission] ON [permission].[Code] = [mapping].[PermissionCode]
                    WHERE [permission].[Id] IS NULL)
                BEGIN
                    THROW 51000, 'A required freelance system-role permission is missing.', 1;
                END;

                INSERT INTO [RolePermissions] ([Id], [RoleId], [PermissionId])
                SELECT NEWID(), [mapping].[RoleId], [permission].[Id]
                FROM @rolePermissions AS [mapping]
                INNER JOIN [Permissions] AS [permission] ON [permission].[Code] = [mapping].[PermissionCode]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [RolePermissions] AS [existing]
                    WHERE [existing].[RoleId] = [mapping].[RoleId]
                      AND [existing].[PermissionId] = [permission].[Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // System roles may already be assigned to users. Preserve role and permission history on rollback.
        }
    }
}
