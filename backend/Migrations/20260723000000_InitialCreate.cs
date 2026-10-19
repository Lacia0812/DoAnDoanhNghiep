using GiaPhuWaterDelivery.Api.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaPhuWaterDelivery.Api.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CustomerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ProductCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ProductCategories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                Capacity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                StockQuantity = table.Column<int>(type: "int", nullable: false),
                MinimumStock = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.CheckConstraint("CK_Products_MinimumStock_NonNegative", "[MinimumStock] >= 0");
                table.CheckConstraint("CK_Products_Price_Positive", "[Price] > 0");
                table.CheckConstraint("CK_Products_Stock_NonNegative", "[StockQuantity] >= 0");
                table.ForeignKey(
                    name: "FK_Products_ProductCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "ProductCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                DeliveryUserId = table.Column<int>(type: "int", nullable: true),
                OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                OrderStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.CheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
                table.ForeignKey(
                    name: "FK_Orders_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Orders_Users_DeliveryUserId",
                    column: x => x.DeliveryUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Deliveries",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                DeliveryUserId = table.Column<int>(type: "int", nullable: false),
                AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Deliveries", x => x.Id);
                table.ForeignKey(
                    name: "FK_Deliveries_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Deliveries_Users_DeliveryUserId",
                    column: x => x.DeliveryUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OrderDetails",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderDetails", x => x.Id);
                table.CheckConstraint("CK_OrderDetails_Quantity_Positive", "[Quantity] > 0");
                table.CheckConstraint("CK_OrderDetails_TotalPrice_NonNegative", "[TotalPrice] >= 0");
                table.CheckConstraint("CK_OrderDetails_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                table.ForeignKey(
                    name: "FK_OrderDetails_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderDetails_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InventoryTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                OrderId = table.Column<int>(type: "int", nullable: true),
                TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                QuantityBefore = table.Column<int>(type: "int", nullable: false),
                QuantityAfter = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                table.CheckConstraint("CK_InventoryTransactions_After_NonNegative", "[QuantityAfter] >= 0");
                table.CheckConstraint("CK_InventoryTransactions_Before_NonNegative", "[QuantityBefore] >= 0");
                table.CheckConstraint("CK_InventoryTransactions_Quantity_Positive", "[Quantity] > 0");
                table.ForeignKey(
                    name: "FK_InventoryTransactions_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InventoryTransactions_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InventoryTransactions_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_CustomerCode",
            table: "Customers",
            column: "CustomerCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Deliveries_DeliveryUserId",
            table: "Deliveries",
            column: "DeliveryUserId");
        migrationBuilder.CreateIndex(
            name: "IX_Deliveries_OrderId",
            table: "Deliveries",
            column: "OrderId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_CreatedByUserId",
            table: "InventoryTransactions",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_OrderId",
            table: "InventoryTransactions",
            column: "OrderId");
        migrationBuilder.CreateIndex(
            name: "IX_InventoryTransactions_ProductId",
            table: "InventoryTransactions",
            column: "ProductId");
        migrationBuilder.CreateIndex(
            name: "IX_OrderDetails_OrderId_ProductId",
            table: "OrderDetails",
            columns: new[] { "OrderId", "ProductId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_OrderDetails_ProductId",
            table: "OrderDetails",
            column: "ProductId");
        migrationBuilder.CreateIndex(
            name: "IX_Orders_CreatedByUserId",
            table: "Orders",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_Orders_CustomerId",
            table: "Orders",
            column: "CustomerId");
        migrationBuilder.CreateIndex(
            name: "IX_Orders_DeliveryUserId",
            table: "Orders",
            column: "DeliveryUserId");
        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderCode",
            table: "Orders",
            column: "OrderCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_ProductCategories_Name",
            table: "ProductCategories",
            column: "Name",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");
        migrationBuilder.CreateIndex(
            name: "IX_Products_ProductCode",
            table: "Products",
            column: "ProductCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Deliveries");
        migrationBuilder.DropTable(name: "InventoryTransactions");
        migrationBuilder.DropTable(name: "OrderDetails");
        migrationBuilder.DropTable(name: "Orders");
        migrationBuilder.DropTable(name: "Products");
        migrationBuilder.DropTable(name: "Customers");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "ProductCategories");
    }
}
