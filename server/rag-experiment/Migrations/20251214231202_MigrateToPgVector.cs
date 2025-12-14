using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace rag_experiment.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToPgVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgvector extension
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            // Clear all embeddings first (they need to be regenerated in the new format)
            migrationBuilder.Sql(@"DELETE FROM ""Embeddings"";");

            // Drop the old bytea column and recreate as vector type
            // (can't cast bytea to vector automatically)
            migrationBuilder.DropColumn(
                name: "EmbeddingData",
                table: "Embeddings");

            migrationBuilder.AddColumn<Vector>(
                name: "EmbeddingData",
                table: "Embeddings",
                type: "vector(1536)",
                nullable: false);

            // Create HNSW index for fast approximate nearest neighbor search using cosine distance
            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_EmbeddingData",
                table: "Embeddings",
                column: "EmbeddingData")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Embeddings_EmbeddingData",
                table: "Embeddings");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<byte[]>(
                name: "EmbeddingData",
                table: "Embeddings",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(1536)");
        }
    }
}
