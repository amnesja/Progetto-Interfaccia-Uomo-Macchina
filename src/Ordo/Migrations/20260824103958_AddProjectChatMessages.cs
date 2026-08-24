using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordo.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Testo = table.Column<string>(type: "TEXT", nullable: true),
                    DataCreazione = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectChatMessages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectChatMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_ProjectId_DataCreazione",
                table: "ProjectChatMessages",
                columns: new[] { "ProjectId", "DataCreazione" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_UserId",
                table: "ProjectChatMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectChatMessages");
        }
    }
}
