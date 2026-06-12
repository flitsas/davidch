using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Procedures.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "procedure_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureTypeCode = table.Column<string>(type: "text", nullable: false),
                    OtTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtDivipolCode = table.Column<string>(type: "text", nullable: false),
                    VehicleQueryValue = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procedure_instances_procedure_types_ProcedureTypeId",
                        column: x => x.ProcedureTypeId,
                        principalTable: "procedure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procedure_instance_actors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleLabel = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PersonKind = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "text", nullable: false),
                    IsLegalRepresentative = table.Column<bool>(type: "boolean", nullable: false),
                    ParentActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalDataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_instance_actors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procedure_instance_actors_procedure_instance_actors_ParentA~",
                        column: x => x.ParentActorId,
                        principalTable: "procedure_instance_actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_procedure_instance_actors_procedure_instances_ProcedureInst~",
                        column: x => x.ProcedureInstanceId,
                        principalTable: "procedure_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "procedure_instance_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procedure_instance_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procedure_instance_documents_procedure_instances_ProcedureI~",
                        column: x => x.ProcedureInstanceId,
                        principalTable: "procedure_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_instance_actors_ParentActorId",
                table: "procedure_instance_actors",
                column: "ParentActorId");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_instance_actors_ProcedureInstanceId_SortOrder",
                table: "procedure_instance_actors",
                columns: new[] { "ProcedureInstanceId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_procedure_instance_documents_ProcedureInstanceId_Label",
                table: "procedure_instance_documents",
                columns: new[] { "ProcedureInstanceId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_procedure_instances_ProcedureTypeId",
                table: "procedure_instances",
                column: "ProcedureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_procedure_instances_TenantId_CreatedAt",
                table: "procedure_instances",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procedure_instance_actors");

            migrationBuilder.DropTable(
                name: "procedure_instance_documents");

            migrationBuilder.DropTable(
                name: "procedure_instances");
        }
    }
}
