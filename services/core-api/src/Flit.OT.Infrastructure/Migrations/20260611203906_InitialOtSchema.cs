using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.OT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOtSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_type_catalog",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DefaultSortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_type_catalog", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ot_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DivipolCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IntegrationMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ot_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ot_profiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_type_catalog",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_type_catalog", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ot_document_order_items",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureTypeCode = table.Column<string>(type: "text", nullable: false),
                    DocumentTypeCode = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    IsIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    OtProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ot_document_order_items", x => new { x.TenantId, x.ProcedureTypeCode, x.DocumentTypeCode });
                    table.ForeignKey(
                        name: "FK_ot_document_order_items_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ot_document_order_items_document_type_catalog_DocumentTypeC~",
                        column: x => x.DocumentTypeCode,
                        principalTable: "document_type_catalog",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ot_document_order_items_ot_profiles_OtProfileId",
                        column: x => x.OtProfileId,
                        principalTable: "ot_profiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ot_document_order_items_procedure_type_catalog_ProcedureTyp~",
                        column: x => x.ProcedureTypeCode,
                        principalTable: "procedure_type_catalog",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_document_defaults",
                columns: table => new
                {
                    ProcedureTypeCode = table.Column<string>(type: "text", nullable: false),
                    DocumentTypeCode = table.Column<string>(type: "text", nullable: false),
                    DefaultPosition = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_document_defaults", x => new { x.ProcedureTypeCode, x.DocumentTypeCode });
                    table.ForeignKey(
                        name: "FK_procedure_document_defaults_document_type_catalog_DocumentT~",
                        column: x => x.DocumentTypeCode,
                        principalTable: "document_type_catalog",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_procedure_document_defaults_procedure_type_catalog_Procedur~",
                        column: x => x.ProcedureTypeCode,
                        principalTable: "procedure_type_catalog",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ot_document_order_items_DocumentTypeCode",
                table: "ot_document_order_items",
                column: "DocumentTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_ot_document_order_items_OtProfileId",
                table: "ot_document_order_items",
                column: "OtProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ot_document_order_items_ProcedureTypeCode",
                table: "ot_document_order_items",
                column: "ProcedureTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_ot_document_order_items_TenantId_ProcedureTypeCode_Position",
                table: "ot_document_order_items",
                columns: new[] { "TenantId", "ProcedureTypeCode", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ot_profiles_DivipolCode",
                table: "ot_profiles",
                column: "DivipolCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ot_profiles_TenantId",
                table: "ot_profiles",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ot_profiles_UpdatedAt",
                table: "ot_profiles",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_document_defaults_DocumentTypeCode",
                table: "procedure_document_defaults",
                column: "DocumentTypeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ot_document_order_items");

            migrationBuilder.DropTable(
                name: "procedure_document_defaults");

            migrationBuilder.DropTable(
                name: "ot_profiles");

            migrationBuilder.DropTable(
                name: "document_type_catalog");

            migrationBuilder.DropTable(
                name: "procedure_type_catalog");
        }
    }
}
