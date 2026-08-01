using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

public partial class AddLocalizedPermissionsAndMemberCrud : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The production rollout may have added these columns manually before the
        // migration runner was enabled. Use idempotent SQL so the migration can finish
        // in either state and record itself in __EFMigrationsHistory.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Permissions', N'DisplayNameAr') IS NULL
            BEGIN
                ALTER TABLE dbo.Permissions ADD DisplayNameAr nvarchar(150) NOT NULL
                    CONSTRAINT DF_Permissions_DisplayNameAr DEFAULT N'';
            END;
        """);
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Roles', N'NameAr') IS NULL
            BEGIN
                ALTER TABLE dbo.Roles ADD NameAr nvarchar(150) NOT NULL
                    CONSTRAINT DF_Roles_NameAr DEFAULT N'';
            END;
        """);

        migrationBuilder.Sql("""
            INSERT INTO Permissions (Id, Code, DisplayName, DisplayNameAr, Category, IsPlatformPermission, CreatedAt)
            SELECT NEWID(), v.Code, v.Code, v.Label, 'Tenant', 0, SYSUTCDATETIME()
            FROM (VALUES
              ('CreateMembers', N'إضافة العملاء'),
              ('UpdateMembers', N'تعديل العملاء'),
              ('DeleteMembers', N'حذف العملاء')
            ) v(Code, Label)
            WHERE NOT EXISTS (SELECT 1 FROM Permissions p WHERE p.Code = v.Code);
        """);

        migrationBuilder.Sql("""
            UPDATE p SET DisplayNameAr = CASE p.Code
              WHEN 'ViewMembers' THEN N'عرض العملاء' WHEN 'ManageMembers' THEN N'إدارة العملاء'
              WHEN 'CreateMembers' THEN N'إضافة العملاء' WHEN 'UpdateMembers' THEN N'تعديل العملاء' WHEN 'DeleteMembers' THEN N'حذف العملاء'
              WHEN 'ManageCoaches' THEN N'إدارة المدربين' WHEN 'ManageAttendance' THEN N'إدارة الحضور'
              WHEN 'ManageClientSubscriptions' THEN N'إدارة اشتراكات العملاء' WHEN 'ManageFinance' THEN N'إدارة المالية'
              WHEN 'ViewReports' THEN N'عرض التقارير' WHEN 'ManageReports' THEN N'إدارة التقارير' WHEN 'ManageSettings' THEN N'إدارة الإعدادات'
              ELSE p.DisplayNameAr END
            FROM Permissions p;
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Permissions', N'DisplayNameAr') IS NOT NULL
                ALTER TABLE dbo.Permissions DROP CONSTRAINT IF EXISTS DF_Permissions_DisplayNameAr;
            IF COL_LENGTH(N'dbo.Permissions', N'DisplayNameAr') IS NOT NULL
                ALTER TABLE dbo.Permissions DROP COLUMN DisplayNameAr;
            IF COL_LENGTH(N'dbo.Roles', N'NameAr') IS NOT NULL
                ALTER TABLE dbo.Roles DROP CONSTRAINT IF EXISTS DF_Roles_NameAr;
            IF COL_LENGTH(N'dbo.Roles', N'NameAr') IS NOT NULL
                ALTER TABLE dbo.Roles DROP COLUMN NameAr;
        """);
    }
}
