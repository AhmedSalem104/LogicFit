using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFreelanceWorkspaceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkspaceType",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityAccountId",
                table: "DomainUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FreelanceWorkspaceProfiles",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SpecialtiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialLinksJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WelcomeMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BookingSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelanceWorkspaceProfiles", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_FreelanceWorkspaceProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NormalizedPhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SponsoredByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_WorkspaceMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMemberships_DomainUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceMemberships_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceMemberships_WorkspaceMemberships_SponsoredByMembershipId",
                        column: x => x.SponsoredByMembershipId,
                        principalTable: "WorkspaceMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetScopeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReservedWorkspaceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedRole = table.Column<int>(type: "int", nullable: true),
                    SponsoredByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResubmissionNumber = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InformationRequest = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRequests_IdentityAccounts_IdentityAccountId",
                        column: x => x.IdentityAccountId,
                        principalTable: "IdentityAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationRequests_Tenants_TargetWorkspaceId",
                        column: x => x.TargetWorkspaceId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationRequests_WorkspaceMemberships_SponsoredByMembershipId",
                        column: x => x.SponsoredByMembershipId,
                        principalTable: "WorkspaceMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRequestRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRequestRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRequestRevisions_ApplicationRequests_ApplicationRequestId",
                        column: x => x.ApplicationRequestId,
                        principalTable: "ApplicationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationTrackingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTrackingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationTrackingSessions_ApplicationRequests_ApplicationRequestId",
                        column: x => x.ApplicationRequestId,
                        principalTable: "ApplicationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_IdentityAccountId",
                table: "DomainUsers",
                column: "IdentityAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequestRevisions_ApplicationRequestId_RevisionNumber",
                table: "ApplicationRequestRevisions",
                columns: new[] { "ApplicationRequestId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetScopeKey_ApplicationType",
                table: "ApplicationRequests",
                columns: new[] { "IdentityAccountId", "TargetScopeKey", "ApplicationType" },
                unique: true,
                filter: "[Status] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_IdentityAccountId_TargetWorkspaceId_ApplicationType_Status",
                table: "ApplicationRequests",
                columns: new[] { "IdentityAccountId", "TargetWorkspaceId", "ApplicationType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_ReservedWorkspaceIdentifier",
                table: "ApplicationRequests",
                column: "ReservedWorkspaceIdentifier",
                unique: true,
                filter: "[ReservedWorkspaceIdentifier] IS NOT NULL AND [Status] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_SponsoredByMembershipId",
                table: "ApplicationRequests",
                column: "SponsoredByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_TargetWorkspaceId",
                table: "ApplicationRequests",
                column: "TargetWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTrackingSessions_ApplicationRequestId_ExpiresAt",
                table: "ApplicationTrackingSessions",
                columns: new[] { "ApplicationRequestId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTrackingSessions_TokenHash",
                table: "ApplicationTrackingSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAccounts_NormalizedEmail",
                table: "IdentityAccounts",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAccounts_NormalizedPhoneNumber",
                table: "IdentityAccounts",
                column: "NormalizedPhoneNumber",
                unique: true,
                filter: "[NormalizedPhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_IdentityAccountId_TenantId",
                table: "WorkspaceMemberships",
                columns: new[] { "IdentityAccountId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_SponsoredByMembershipId",
                table: "WorkspaceMemberships",
                column: "SponsoredByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_TenantId_Status",
                table: "WorkspaceMemberships",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMemberships_UserId",
                table: "WorkspaceMemberships",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DomainUsers_IdentityAccounts_IdentityAccountId",
                table: "DomainUsers",
                column: "IdentityAccountId",
                principalTable: "IdentityAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DomainUsers_IdentityAccounts_IdentityAccountId",
                table: "DomainUsers");

            migrationBuilder.DropTable(
                name: "ApplicationRequestRevisions");

            migrationBuilder.DropTable(
                name: "ApplicationTrackingSessions");

            migrationBuilder.DropTable(
                name: "FreelanceWorkspaceProfiles");

            migrationBuilder.DropTable(
                name: "ApplicationRequests");

            migrationBuilder.DropTable(
                name: "WorkspaceMemberships");

            migrationBuilder.DropTable(
                name: "IdentityAccounts");

            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_IdentityAccountId",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "WorkspaceType",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IdentityAccountId",
                table: "DomainUsers");
        }
    }
}
