using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayrollBatchId",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    TotalGrossSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_PayrollBatchId_UserId",
                table: "Payrolls",
                columns: new[] { "PayrollBatchId", "UserId" },
                unique: true,
                filter: "[PayrollBatchId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_PayrollBatches_PayrollBatchId",
                table: "Payrolls",
                column: "PayrollBatchId",
                principalTable: "PayrollBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_PayrollBatches_PayrollBatchId",
                table: "Payrolls");

            migrationBuilder.DropTable(
                name: "PayrollBatches");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_PayrollBatchId_UserId",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "PayrollBatchId",
                table: "Payrolls");
        }
    }
}
