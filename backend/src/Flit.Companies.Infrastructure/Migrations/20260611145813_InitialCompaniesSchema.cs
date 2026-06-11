using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flit.Companies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCompaniesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nit = table.Column<string>(type: "text", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_companies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "runt_provider_catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runt_provider_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_user_exceptions",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_user_exceptions", x => new { x.TenantId, x.UserId });
                    table.ForeignKey(
                        name: "FK_tenant_user_exceptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tenant_user_exceptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "traffic_authorities",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traffic_authorities", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "company_matricula_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowNewVehicleFiling = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMiscProcedures = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_matricula_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_matricula_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_notification_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    NotificationTarget = table.Column<int>(type: "integer", nullable: false),
                    ClientApiSettings = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_notification_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_notification_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_payment_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowFlitGateway = table.Column<bool>(type: "boolean", nullable: false),
                    AllowOt = table.Column<bool>(type: "boolean", nullable: false),
                    AllowOther = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_payment_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_payment_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_runt_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryProvider = table.Column<int>(type: "integer", nullable: false),
                    SecondaryProvider = table.Column<int>(type: "integer", nullable: false),
                    FailoverTimeoutMs = table.Column<int>(type: "integer", nullable: false),
                    ProviderCredentials = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_runt_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_runt_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_signature_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerSignatureType = table.Column<int>(type: "integer", nullable: false),
                    BuyerSignatureType = table.Column<int>(type: "integer", nullable: false),
                    VaultEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    VaultSettings = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_signature_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_signature_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_traspaso_config",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnlyOwnVehicles = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_traspaso_config", x => x.CompanyId);
                    table.ForeignKey(
                        name: "FK_company_traspaso_config_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_traffic_authority_matrix",
                columns: table => new
                {
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorityCode = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_traffic_authority_matrix", x => new { x.CompanyId, x.AuthorityCode });
                    table.ForeignKey(
                        name: "FK_company_traffic_authority_matrix_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_traffic_authority_matrix_traffic_authorities_Author~",
                        column: x => x.AuthorityCode,
                        principalTable: "traffic_authorities",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_LegalName",
                table: "companies",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_companies_Nit",
                table: "companies",
                column: "Nit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_TenantId",
                table: "companies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_UpdatedAt",
                table: "companies",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_company_traffic_authority_matrix_AuthorityCode",
                table: "company_traffic_authority_matrix",
                column: "AuthorityCode");

            migrationBuilder.CreateIndex(
                name: "IX_company_traffic_authority_matrix_CompanyId_IsEnabled",
                table: "company_traffic_authority_matrix",
                columns: new[] { "CompanyId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_exceptions_TenantId",
                table: "tenant_user_exceptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_user_exceptions_UserId",
                table: "tenant_user_exceptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_matricula_config");

            migrationBuilder.DropTable(
                name: "company_notification_config");

            migrationBuilder.DropTable(
                name: "company_payment_config");

            migrationBuilder.DropTable(
                name: "company_runt_config");

            migrationBuilder.DropTable(
                name: "company_signature_config");

            migrationBuilder.DropTable(
                name: "company_traffic_authority_matrix");

            migrationBuilder.DropTable(
                name: "company_traspaso_config");

            migrationBuilder.DropTable(
                name: "runt_provider_catalog");

            migrationBuilder.DropTable(
                name: "tenant_user_exceptions");

            migrationBuilder.DropTable(
                name: "traffic_authorities");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
