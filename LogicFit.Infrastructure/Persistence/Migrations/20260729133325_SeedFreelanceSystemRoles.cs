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
            migrationBuilder.Sql("""
                DECLARE @now datetime2 = SYSUTCDATETIME();

                INSERT INTO dbo.Roles
                    (Id, TenantId, Name, NormalizedName, Description, NameAr, IsSystemRole,
                     IsDeleted, DeletedAt, DeletedBy, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                SELECT NEWID(), NULL, source.Name, UPPER(source.Name), source.Description, source.NameAr, 1,
                       0, NULL, NULL, @now, 'migration:SeedFreelanceSystemRoles', NULL, NULL
                FROM (VALUES
                    (N'FreelanceOwner', N'System role: FreelanceOwner', N'مالك المدرب الحر'),
                    (N'FreelanceCoach', N'System role: FreelanceCoach', N'مدرب حر'),
                    (N'FreelanceAssistant', N'System role: FreelanceAssistant', N'مساعد مدرب')
                ) AS source(Name, Description, NameAr)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.Roles roleRecord
                    WHERE roleRecord.TenantId IS NULL AND roleRecord.Name = source.Name
                );

                UPDATE roleRecord
                SET IsDeleted = 0,
                    DeletedAt = NULL,
                    DeletedBy = NULL,
                    IsSystemRole = 1,
                    NormalizedName = UPPER(source.Name),
                    Description = source.Description,
                    NameAr = source.NameAr,
                    UpdatedAt = @now,
                    UpdatedBy = 'migration:SeedFreelanceSystemRoles'
                FROM dbo.Roles roleRecord
                INNER JOIN (VALUES
                    (N'FreelanceOwner', N'System role: FreelanceOwner', N'مالك المدرب الحر'),
                    (N'FreelanceCoach', N'System role: FreelanceCoach', N'مدرب حر'),
                    (N'FreelanceAssistant', N'System role: FreelanceAssistant', N'مساعد مدرب')
                ) AS source(Name, Description, NameAr) ON source.Name = roleRecord.Name
                WHERE roleRecord.TenantId IS NULL;
            """);

            migrationBuilder.Sql("""
                INSERT INTO dbo.RolePermissions (Id, RoleId, PermissionId)
                SELECT NEWID(), roleRecord.Id, permissionRecord.Id
                FROM dbo.Roles roleRecord
                CROSS JOIN dbo.Permissions permissionRecord
                WHERE roleRecord.TenantId IS NULL
                  AND roleRecord.IsDeleted = 0
                  AND roleRecord.Name = N'FreelanceOwner'
                  AND permissionRecord.IsPlatformPermission = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.RolePermissions currentMapping
                      WHERE currentMapping.RoleId = roleRecord.Id
                        AND currentMapping.PermissionId = permissionRecord.Id
                  );
            """);

            migrationBuilder.Sql("""
                INSERT INTO dbo.RolePermissions (Id, RoleId, PermissionId)
                SELECT NEWID(), roleRecord.Id, permissionRecord.Id
                FROM dbo.Roles roleRecord
                INNER JOIN dbo.Permissions permissionRecord ON permissionRecord.Code IN (
                    N'ViewMembers', N'CreateMembers', N'UpdateMembers', N'ManageCoaches',
                    N'ManageAttendance', N'ManageClientSubscriptions', N'ViewReports'
                )
                WHERE roleRecord.TenantId IS NULL
                  AND roleRecord.IsDeleted = 0
                  AND roleRecord.Name = N'FreelanceCoach'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.RolePermissions currentMapping
                      WHERE currentMapping.RoleId = roleRecord.Id
                        AND currentMapping.PermissionId = permissionRecord.Id
                  );
            """);

            migrationBuilder.Sql("""
                INSERT INTO dbo.RolePermissions (Id, RoleId, PermissionId)
                SELECT NEWID(), roleRecord.Id, permissionRecord.Id
                FROM dbo.Roles roleRecord
                INNER JOIN dbo.Permissions permissionRecord ON permissionRecord.Code IN (
                    N'ViewMembers', N'CreateMembers', N'UpdateMembers', N'ManageAttendance',
                    N'ManageClientSubscriptions'
                )
                WHERE roleRecord.TenantId IS NULL
                  AND roleRecord.IsDeleted = 0
                  AND roleRecord.Name = N'FreelanceAssistant'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.RolePermissions currentMapping
                      WHERE currentMapping.RoleId = roleRecord.Id
                        AND currentMapping.PermissionId = permissionRecord.Id
                  );
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // System roles are shared reference data. Removing them could invalidate active
            // memberships, so this corrective data migration deliberately has no destructive down step.
        }
    }
}
