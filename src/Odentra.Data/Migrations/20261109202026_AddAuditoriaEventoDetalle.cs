using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odentra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaEventoDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgenteUsuario",
                table: "RegistrosAuditoria",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionIp",
                table: "RegistrosAuditoria",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaHora",
                table: "RegistrosAuditoria",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Modulo_Accion",
                table: "RegistrosAuditoria",
                columns: new[] { "Modulo", "Accion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_FechaHora",
                table: "RegistrosAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_Modulo_Accion",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "AgenteUsuario",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "DireccionIp",
                table: "RegistrosAuditoria");
        }
    }
}
