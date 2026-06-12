using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Procedures.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialProceduresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "procedure_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    VehicleQueryMode = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "procedure_type_actors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleLabel = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_type_actors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procedure_type_actors_procedure_types_ProcedureTypeId",
                        column: x => x.ProcedureTypeId,
                        principalTable: "procedure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_type_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_type_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procedure_type_documents_procedure_types_ProcedureTypeId",
                        column: x => x.ProcedureTypeId,
                        principalTable: "procedure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_type_actors_ProcedureTypeId_SortOrder",
                table: "procedure_type_actors",
                columns: new[] { "ProcedureTypeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_type_documents_ProcedureTypeId_SortOrder",
                table: "procedure_type_documents",
                columns: new[] { "ProcedureTypeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_types_Code",
                table: "procedure_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procedure_types_IsActive",
                table: "procedure_types",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_types_Name",
                table: "procedure_types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procedure_types_UpdatedAt",
                table: "procedure_types",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procedure_type_actors");

            migrationBuilder.DropTable(
                name: "procedure_type_documents");

            migrationBuilder.DropTable(
                name: "procedure_types");
        }
    }
}
