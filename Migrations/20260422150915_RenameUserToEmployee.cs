using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_Users_UserId",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_PaymentSchemes_PaymentSchemeId",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Employees");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Payrolls",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Payrolls_UserId",
                table: "Payrolls",
                newName: "IX_Payrolls_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Payrolls_PayrollBatchId_UserId",
                table: "Payrolls",
                newName: "IX_Payrolls_PayrollBatchId_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Employees",
                newName: "IX_Employees_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Users_PaymentSchemeId",
                table: "Employees",
                newName: "IX_Employees_PaymentSchemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_PaymentSchemes_PaymentSchemeId",
                table: "Employees",
                column: "PaymentSchemeId",
                principalTable: "PaymentSchemes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_Employees_EmployeeId",
                table: "Payrolls",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_Employees_EmployeeId",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_PaymentSchemes_PaymentSchemeId",
                table: "Employees");

            migrationBuilder.RenameTable(
                name: "Employees",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Payrolls",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Payrolls_PayrollBatchId_EmployeeId",
                table: "Payrolls",
                newName: "IX_Payrolls_PayrollBatchId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Payrolls_EmployeeId",
                table: "Payrolls",
                newName: "IX_Payrolls_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_PaymentSchemeId",
                table: "Users",
                newName: "IX_Users_PaymentSchemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PaymentSchemes_PaymentSchemeId",
                table: "Users",
                column: "PaymentSchemeId",
                principalTable: "PaymentSchemes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_Users_UserId",
                table: "Payrolls",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
