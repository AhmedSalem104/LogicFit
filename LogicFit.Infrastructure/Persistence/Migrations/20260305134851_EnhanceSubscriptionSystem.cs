using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceSubscriptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "SubscriptionPlans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InBodyIncluded",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFreezeCount",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxFreezeDays",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PrivateCoach",
                table: "SubscriptionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SessionsPerWeek",
                table: "SubscriptionPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SubscriptionFreezes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "ClientSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "ClientSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ClientSubscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "ClientSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromId",
                table: "ClientSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "ClientSubscriptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_ClientSubscriptions_RenewedFromId",
                table: "ClientSubscriptions",
                column: "RenewedFromId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientSubscriptions_ClientSubscriptions_RenewedFromId",
                table: "ClientSubscriptions",
                column: "RenewedFromId",
                principalTable: "ClientSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientSubscriptions_ClientSubscriptions_RenewedFromId",
                table: "ClientSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ClientSubscriptions_RenewedFromId",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "InBodyIncluded",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxFreezeCount",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxFreezeDays",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "PrivateCoach",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "SessionsPerWeek",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SubscriptionFreezes");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "RenewedFromId",
                table: "ClientSubscriptions");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "ClientSubscriptions");
        }
    }
}
