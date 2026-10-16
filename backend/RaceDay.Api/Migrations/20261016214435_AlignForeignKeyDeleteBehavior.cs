using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignForeignKeyDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventCategory_Event_EventId",
                table: "EventCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_EventEnrolment_Event_EventId",
                table: "EventEnrolment");

            migrationBuilder.DropForeignKey(
                name: "FK_Result_EventEnrolment_EnrolmentId",
                table: "Result");

            migrationBuilder.DropForeignKey(
                name: "FK_Route_Event_EventId",
                table: "Route");

            migrationBuilder.AddForeignKey(
                name: "FK_EventCategory_Event_EventId",
                table: "EventCategory",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventEnrolment_Event_EventId",
                table: "EventEnrolment",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Result_EventEnrolment_EnrolmentId",
                table: "Result",
                column: "EnrolmentId",
                principalTable: "EventEnrolment",
                principalColumn: "EnrolmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Route_Event_EventId",
                table: "Route",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventCategory_Event_EventId",
                table: "EventCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_EventEnrolment_Event_EventId",
                table: "EventEnrolment");

            migrationBuilder.DropForeignKey(
                name: "FK_Result_EventEnrolment_EnrolmentId",
                table: "Result");

            migrationBuilder.DropForeignKey(
                name: "FK_Route_Event_EventId",
                table: "Route");

            migrationBuilder.AddForeignKey(
                name: "FK_EventCategory_Event_EventId",
                table: "EventCategory",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EventEnrolment_Event_EventId",
                table: "EventEnrolment",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Result_EventEnrolment_EnrolmentId",
                table: "Result",
                column: "EnrolmentId",
                principalTable: "EventEnrolment",
                principalColumn: "EnrolmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Route_Event_EventId",
                table: "Route",
                column: "EventId",
                principalTable: "Event",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
