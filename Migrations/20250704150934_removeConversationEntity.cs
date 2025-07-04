using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace websocket.Migrations
{
    /// <inheritdoc />
    public partial class removeConversationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.AddColumn<bool>(
                name: "RecieverDelete",
                schema: "dbo",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SenderDelete",
                schema: "dbo",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecieverDelete",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SenderDelete",
                schema: "dbo",
                table: "Messages");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                schema: "dbo",
                table: "Messages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                schema: "dbo",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Conversations_ConversationId",
                schema: "dbo",
                table: "Messages",
                column: "ConversationId",
                principalSchema: "dbo",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
