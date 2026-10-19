using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaPhuWaterDelivery.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerDebtAndInvoiceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ContainerDeposit",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresContainer",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ContainerDepositAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReturnedContainerQuantity",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContainerBalance",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE [Orders] SET [ProductAmount] = [TotalAmount], " +
                "[PaidAmount] = CASE WHEN [PaymentStatus] = 'Paid' THEN [TotalAmount] ELSE 0 END");

            migrationBuilder.CreateTable(
                name: "ContainerTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DeliveredQuantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerTransactions", x => x.Id);
                    table.CheckConstraint("CK_ContainerTransactions_Balance_NonNegative", "[BalanceBefore] >= 0 AND [BalanceAfter] >= 0");
                    table.CheckConstraint("CK_ContainerTransactions_Delivered_NonNegative", "[DeliveredQuantity] >= 0");
                    table.CheckConstraint("CK_ContainerTransactions_Returned_NonNegative", "[ReturnedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_ContainerTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerTransactions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_ContainerDeposit_NonNegative",
                table: "Products",
                sql: "[ContainerDeposit] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_DepositAmount_NonNegative",
                table: "Orders",
                sql: "[DepositAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_PaidAmount_Valid",
                table: "Orders",
                sql: "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_ProductAmount_NonNegative",
                table: "Orders",
                sql: "[ProductAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderDetails_Deposit_NonNegative",
                table: "OrderDetails",
                sql: "[ContainerDepositAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderDetails_ReturnedContainers_NonNegative",
                table: "OrderDetails",
                sql: "[ReturnedContainerQuantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_CreatedByUserId",
                table: "ContainerTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_CustomerId",
                table: "ContainerTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_OrderId_ProductId",
                table: "ContainerTransactions",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_ProductId",
                table: "ContainerTransactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId_PaidAt",
                table: "Payments",
                columns: new[] { "OrderId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RecordedByUserId",
                table: "Payments",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerTransactions");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_ContainerDeposit_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_DepositAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_PaidAmount_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_ProductAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderDetails_Deposit_NonNegative",
                table: "OrderDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderDetails_ReturnedContainers_NonNegative",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ContainerDeposit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresContainer",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ContainerDepositAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnedContainerQuantity",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ContainerBalance",
                table: "Customers");
        }
    }
}
