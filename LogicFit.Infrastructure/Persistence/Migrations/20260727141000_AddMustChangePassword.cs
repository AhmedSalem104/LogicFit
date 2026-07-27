using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some production databases never had ProgramRoutines.NameAr. Make the
            // cleanup idempotent so startup migrations work for both schemas.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ProgramRoutines', N'NameAr') IS NOT NULL
                BEGIN
                    DECLARE @constraint sysname;
                    SELECT @constraint = d.name
                    FROM sys.default_constraints d
                    INNER JOIN sys.columns c ON c.default_object_id = d.object_id
                    WHERE d.parent_object_id = OBJECT_ID(N'dbo.ProgramRoutines')
                      AND c.name = N'NameAr';
                    IF @constraint IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.ProgramRoutines DROP CONSTRAINT [' + @constraint + ']');
                    ALTER TABLE dbo.ProgramRoutines DROP COLUMN NameAr;
                END;
            """);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Roles",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "DomainUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "DomainUsers");

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ProgramRoutines', N'NameAr') IS NULL
                    ALTER TABLE dbo.ProgramRoutines ADD NameAr nvarchar(150) NOT NULL DEFAULT N'';
            """);
        }
    }
}
