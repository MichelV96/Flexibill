using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVSoftware.Flexibill.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalFlowSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiresSequentialApproval = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlowSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    AccountingPackage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsAccountingConnected = table.Column<bool>(type: "bit", nullable: false),
                    AccountingConnectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAccountingSyncAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalAmountExclVat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountExclVatCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVatAmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresSequentialApproval = table.Column<bool>(type: "bit", nullable: false),
                    ExternalBookingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChamberOfCommerceNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActiveModules = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChamberOfCommerceNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address_Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultGeneralLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultCostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Roles = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    BranchIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalFlowLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RequiredApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinimumAmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ApprovalFlowSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalFlowLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalFlowLevels_ApprovalFlowSettings_ApprovalFlowSettingId",
                        column: x => x.ApprovalFlowSettingId,
                        principalTable: "ApprovalFlowSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    RequiredApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceApprovalSteps_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    GeneralLedgerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VatCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OcrConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierBranchLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierBranchLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierBranchLinks_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierIbans",
                columns: table => new
                {
                    Iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierIbans", x => new { x.SupplierId, x.Iban });
                    table.ForeignKey(
                        name: "FK_SupplierIbans_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlowLevels_ApprovalFlowSettingId",
                table: "ApprovalFlowLevels",
                column: "ApprovalFlowSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityType_EntityId",
                table: "AuditLog",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalSteps_InvoiceId",
                table: "InvoiceApprovalSteps",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBranchLinks_SupplierId",
                table: "SupplierBranchLinks",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_Email",
                table: "Users",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalFlowLevels");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "InvoiceApprovalSteps");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "SupplierBranchLinks");

            migrationBuilder.DropTable(
                name: "SupplierIbans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ApprovalFlowSettings");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
