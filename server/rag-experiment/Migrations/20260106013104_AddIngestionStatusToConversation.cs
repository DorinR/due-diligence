using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rag_experiment.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionStatusToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IngestionStatus",
                table: "Conversations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IngestionStatus",
                table: "Conversations");
        }
    }
}
