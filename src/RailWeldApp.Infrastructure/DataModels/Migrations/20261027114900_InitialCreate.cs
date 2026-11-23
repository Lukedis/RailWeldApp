using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailWeldApp.DataModels.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationError",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CommunicationType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IsRecovered = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecoveredAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationError", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParameterSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ParameterName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    YWeldSeamZero = table.Column<string>(type: "TEXT", nullable: false),
                    YCompensationZero = table.Column<string>(type: "TEXT", nullable: false),
                    IsCompensationEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WeldSeamHeight = table.Column<string>(type: "TEXT", nullable: false),
                    ZCompensationZero = table.Column<string>(type: "TEXT", nullable: false),
                    IsSeamSearchEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    XStartPosition = table.Column<string>(type: "TEXT", nullable: false),
                    XStartSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    XWeldLimitPosition = table.Column<string>(type: "TEXT", nullable: false),
                    XWeldLimitSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    XHeatDistance = table.Column<string>(type: "TEXT", nullable: false),
                    XHeatSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    XAirDistance = table.Column<string>(type: "TEXT", nullable: false),
                    XAirSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    YStartPosition = table.Column<string>(type: "TEXT", nullable: false),
                    YStartSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    YWeldLimitPosition = table.Column<string>(type: "TEXT", nullable: false),
                    YWeldLimitSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    YHeatDistance = table.Column<string>(type: "TEXT", nullable: false),
                    YHeatSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    YAirDistance = table.Column<string>(type: "TEXT", nullable: false),
                    YAirSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    ZStartPosition = table.Column<string>(type: "TEXT", nullable: false),
                    ZStartSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    ZWeldLimitPosition = table.Column<string>(type: "TEXT", nullable: false),
                    ZWeldLimitSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    ZHeatDistance = table.Column<string>(type: "TEXT", nullable: false),
                    ZHeatSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    ZAirDistance = table.Column<string>(type: "TEXT", nullable: false),
                    ZAirSpeed = table.Column<string>(type: "TEXT", nullable: false),
                    StartTemp = table.Column<string>(type: "TEXT", nullable: false),
                    StartPower = table.Column<string>(type: "TEXT", nullable: false),
                    StartCoolingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    StartCoolingParam = table.Column<string>(type: "TEXT", nullable: false),
                    TransitionTemp = table.Column<string>(type: "TEXT", nullable: false),
                    TransitionPower = table.Column<string>(type: "TEXT", nullable: false),
                    TransitionCoolingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    TransitionCoolingParam = table.Column<string>(type: "TEXT", nullable: false),
                    FinalTemp = table.Column<string>(type: "TEXT", nullable: false),
                    FinalPower = table.Column<string>(type: "TEXT", nullable: false),
                    FinalCoolingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    FinalCoolingParam = table.Column<string>(type: "TEXT", nullable: false),
                    AirPressureMax = table.Column<string>(type: "TEXT", nullable: false),
                    AirPressureMin = table.Column<string>(type: "TEXT", nullable: false),
                    FlowMax = table.Column<string>(type: "TEXT", nullable: false),
                    FlowMin = table.Column<string>(type: "TEXT", nullable: false),
                    EnergyMax = table.Column<string>(type: "TEXT", nullable: false),
                    EnergyMin = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Account = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Account = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginRecord_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RecipeId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RecipeName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    JobNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OperatorName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ShiftName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EquipmentName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ReportNo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultDetail = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkRecord_ParameterSetting_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "ParameterSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkRecord_UserAccount_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alarm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkRecordId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecoveredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcknowledgedByAccount = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AcknowledgedByUserId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarm_WorkRecord_WorkRecordId",
                        column: x => x.WorkRecordId,
                        principalTable: "WorkRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkMetric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkRecordId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StartValue = table.Column<double>(type: "REAL", nullable: false),
                    PeakValue = table.Column<double>(type: "REAL", nullable: false),
                    EndValue = table.Column<double>(type: "REAL", nullable: false),
                    AverageValue = table.Column<double>(type: "REAL", nullable: false),
                    MinValue = table.Column<double>(type: "REAL", nullable: false),
                    MaxValue = table.Column<double>(type: "REAL", nullable: false),
                    OverCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PeakRatio = table.Column<double>(type: "REAL", nullable: false),
                    Judgement = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkMetric", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkMetric_WorkRecord_WorkRecordId",
                        column: x => x.WorkRecordId,
                        principalTable: "WorkRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkSample",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkRecordId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ElapsedMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature1 = table.Column<double>(type: "REAL", nullable: false),
                    Temperature2 = table.Column<double>(type: "REAL", nullable: false),
                    Temperature3 = table.Column<double>(type: "REAL", nullable: false),
                    Temperature4 = table.Column<double>(type: "REAL", nullable: false),
                    Pressure = table.Column<double>(type: "REAL", nullable: false),
                    Flow1 = table.Column<double>(type: "REAL", nullable: false),
                    Flow2 = table.Column<double>(type: "REAL", nullable: false),
                    Flow3 = table.Column<double>(type: "REAL", nullable: false),
                    Voltage = table.Column<double>(type: "REAL", nullable: false),
                    Current = table.Column<double>(type: "REAL", nullable: false),
                    Power = table.Column<double>(type: "REAL", nullable: false),
                    Energy = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSample", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkSample_WorkRecord_WorkRecordId",
                        column: x => x.WorkRecordId,
                        principalTable: "WorkRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkStage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkRecordId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<double>(type: "REAL", nullable: false),
                    Power = table.Column<double>(type: "REAL", nullable: false),
                    CoolingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    CoolingParameter = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkStage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkStage_WorkRecord_WorkRecordId",
                        column: x => x.WorkRecordId,
                        principalTable: "WorkRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarm_Code",
                table: "Alarm",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Alarm_IsActive",
                table: "Alarm",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alarm_OccurredAt",
                table: "Alarm",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alarm_WorkRecordId",
                table: "Alarm",
                column: "WorkRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationError_OccurredAt",
                table: "CommunicationError",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginRecord_Account",
                table: "LoginRecord",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_LoginRecord_OccurredAt",
                table: "LoginRecord",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginRecord_UserId",
                table: "LoginRecord",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Account",
                table: "UserAccount",
                column: "Account",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkMetric_WorkRecordId_SortOrder",
                table: "WorkMetric",
                columns: new[] { "WorkRecordId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecord_AccountId",
                table: "WorkRecord",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecord_RecipeId",
                table: "WorkRecord",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecord_ReportNo",
                table: "WorkRecord",
                column: "ReportNo");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecord_StartedAt",
                table: "WorkRecord",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSample_WorkRecordId_Timestamp",
                table: "WorkSample",
                columns: new[] { "WorkRecordId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkStage_WorkRecordId_SortOrder",
                table: "WorkStage",
                columns: new[] { "WorkRecordId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarm");

            migrationBuilder.DropTable(
                name: "CommunicationError");

            migrationBuilder.DropTable(
                name: "LoginRecord");

            migrationBuilder.DropTable(
                name: "WorkMetric");

            migrationBuilder.DropTable(
                name: "WorkSample");

            migrationBuilder.DropTable(
                name: "WorkStage");

            migrationBuilder.DropTable(
                name: "WorkRecord");

            migrationBuilder.DropTable(
                name: "ParameterSetting");

            migrationBuilder.DropTable(
                name: "UserAccount");
        }
    }
}
