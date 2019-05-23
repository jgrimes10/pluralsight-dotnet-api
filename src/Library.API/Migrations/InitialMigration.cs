using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Library.API.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Authors",
                table => new
                {
                    Id = table.Column<Guid>(),
                    DateOfBirth = table.Column<DateTimeOffset>(),
                    FirstName = table.Column<string>(maxLength: 50),
                    Genre = table.Column<string>(maxLength: 50),
                    LastName = table.Column<string>(maxLength: 50)
                },
                constraints: table => { table.PrimaryKey("PK_Authors", x => x.Id); });

            migrationBuilder.CreateTable(
                "Books",
                table => new
                {
                    Id = table.Column<Guid>(),
                    AuthorId = table.Column<Guid>(),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    Title = table.Column<string>(maxLength: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        "FK_Books_Authors_AuthorId",
                        x => x.AuthorId,
                        "Authors",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Books_AuthorId",
                "Books",
                "AuthorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Books");

            migrationBuilder.DropTable(
                "Authors");
        }
    }
}