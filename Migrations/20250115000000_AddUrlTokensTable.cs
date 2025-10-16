using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrlTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlTokens_AspNetUsers_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ExpiresAt",
                table: "UrlTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_IsUsed",
                table: "UrlTokens",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ResourceId",
                table: "UrlTokens",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ResourceType",
                table: "UrlTokens",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_Token",
                table: "UrlTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrlTokens");
        }
    }
}
