using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace rag_experiment.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExperimentResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperimentResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExperimentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExperimentName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EmbeddingModelName = table.Column<string>(type: "text", nullable: false),
                    EmbeddingDimension = table.Column<int>(type: "integer", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "integer", nullable: false),
                    StopwordRemoval = table.Column<bool>(type: "boolean", nullable: false),
                    Stemming = table.Column<bool>(type: "boolean", nullable: false),
                    Lemmatization = table.Column<bool>(type: "boolean", nullable: false),
                    QueryExpansion = table.Column<bool>(type: "boolean", nullable: false),
                    TopK = table.Column<int>(type: "integer", nullable: false),
                    AveragePrecision = table.Column<double>(type: "double precision", nullable: false),
                    AverageRecall = table.Column<double>(type: "double precision", nullable: false),
                    AverageF1Score = table.Column<double>(type: "double precision", nullable: false),
                    DetailedResults = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentResults", x => x.Id);
                });
        }
    }
}
