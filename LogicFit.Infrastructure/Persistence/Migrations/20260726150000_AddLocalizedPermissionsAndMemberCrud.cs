using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations;

public partial class AddLocalizedPermissionsAndMemberCrud : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DisplayNameAr", "Permissions", type: "nvarchar(150)", maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>("NameAr", "Roles", type: "nvarchar(150)", maxLength: 150, nullable: false, defaultValue: "");

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
        migrationBuilder.DropColumn("DisplayNameAr", "Permissions");
        migrationBuilder.DropColumn("NameAr", "Roles");
    }
}
