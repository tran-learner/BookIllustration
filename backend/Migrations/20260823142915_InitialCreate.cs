using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookIllustration_Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectTitle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    BookTextPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Style = table.Column<string>(type: "TEXT", nullable: true),
                    UserEmail = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Users_UserEmail",
                        column: x => x.UserEmail,
                        principalTable: "Users",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChapterTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ChapterDescription = table.Column<string>(type: "TEXT", nullable: false),
                    ChapterIllustrationPath = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CharacterDescription = table.Column<string>(type: "TEXT", nullable: false),
                    CharacterIllustrationPath = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_Characters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PipelineSteps",
                columns: table => new
                {
                    PipelineStepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepName = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StepData = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineSteps", x => x.PipelineStepId);
                    table.ForeignKey(
                        name: "FK_PipelineSteps_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChapterCharacters",
                columns: table => new
                {
                    ChaptersChapterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharactersCharacterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterCharacters", x => new { x.ChaptersChapterId, x.CharactersCharacterId });
                    table.ForeignKey(
                        name: "FK_ChapterCharacters_Chapters_ChaptersChapterId",
                        column: x => x.ChaptersChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterCharacters_Characters_CharactersCharacterId",
                        column: x => x.CharactersCharacterId,
                        principalTable: "Characters",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterCharacters_CharactersCharacterId",
                table: "ChapterCharacters",
                column: "CharactersCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_ProjectId",
                table: "Chapters",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ProjectId",
                table: "Characters",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineSteps_ProjectId_StepName",
                table: "PipelineSteps",
                columns: new[] { "ProjectId", "StepName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserEmail",
                table: "Projects",
                column: "UserEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterCharacters");

            migrationBuilder.DropTable(
                name: "PipelineSteps");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
