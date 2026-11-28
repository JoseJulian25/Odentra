using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odentra.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarEstadosTratamientoRealizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Se elimina el estado Pendiente: los tratamientos que lo tenían pasan a En proceso.
            migrationBuilder.Sql(
                "UPDATE TratamientosRealizados SET Estado = 'EnProceso' WHERE Estado = 'Pendiente';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se puede recuperar qué registros estaban en Pendiente antes de la migración.
        }
    }
}
