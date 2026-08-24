using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Tenant.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TopGymMemberFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    NoteType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientFeedback_DomainUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientFeedback_DomainUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFeedback_ClientId",
                table: "ClientFeedback",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientFeedback_ReviewedById",
                table: "ClientFeedback",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClientFeedback_TenantId_ClientId",
                table: "ClientFeedback",
                columns: new[] { "TenantId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFeedback_TenantId_Status",
                table: "ClientFeedback",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFeedback_TenantId_SubmittedAt",
                table: "ClientFeedback",
                columns: new[] { "TenantId", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientFeedback");
        }
    }
}
