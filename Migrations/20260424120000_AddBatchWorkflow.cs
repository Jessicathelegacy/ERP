using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payroll.Data;

#nullable disable

namespace Payroll.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260424120000_AddBatchWorkflow")]
    public partial class AddBatchWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PayrollBatches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "PayrollBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByAdminUserId",
                table: "PayrollBatches",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollBatches_SubmittedByAdminUserId",
                table: "PayrollBatches",
                column: "SubmittedByAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollBatches_AdminUsers_SubmittedByAdminUserId",
                table: "PayrollBatches",
                column: "SubmittedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollBatches_AdminUsers_SubmittedByAdminUserId",
                table: "PayrollBatches");

            migrationBuilder.DropIndex(
                name: "IX_PayrollBatches_SubmittedByAdminUserId",
                table: "PayrollBatches");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PayrollBatches");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "PayrollBatches");

            migrationBuilder.DropColumn(
                name: "SubmittedByAdminUserId",
                table: "PayrollBatches");
        }
    }
}
