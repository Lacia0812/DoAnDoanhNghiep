using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaPhuWaterDelivery.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportsProofContainersAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContainerTransactions_OrderId_ProductId",
                table: "ContainerTransactions");

            migrationBuilder.AddColumn<string>(
                name: "ProofImagePath",
                table: "Deliveries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProofUploadedAt",
                table: "Deliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Deliveries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "ContainerTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "QuantityChange",
                table: "ContainerTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "ContainerTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Delivery");

            migrationBuilder.Sql(
                "UPDATE [ContainerTransactions] SET [TransactionType] = 'Delivery', " +
                "[QuantityChange] = [DeliveredQuantity] - [ReturnedQuantity]");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_OrderId_ProductId",
                table: "ContainerTransactions",
                columns: new[] { "OrderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_ContainerTransactions_OrderId_ProductId",
                table: "ContainerTransactions");

            migrationBuilder.DropColumn(
                name: "ProofImagePath",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "ProofUploadedAt",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "QuantityChange",
                table: "ContainerTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "ContainerTransactions");

            migrationBuilder.Sql("DELETE FROM [ContainerTransactions] WHERE [OrderId] IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "ContainerTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTransactions_OrderId_ProductId",
                table: "ContainerTransactions",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);
        }
    }
}
