using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiChainAPI.Migrations
{
    /// <inheritdoc />
    public partial class Phase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestStudents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    regno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fullname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    mname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    enrollmentno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C_Mobile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C_Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    C_Pincode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdhaarNo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestStudents", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestStudents");
        }
    }
}
