using System.IO;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Services;

/// <summary>
/// 用 QuestPDF 生成 A4 报告。界面预览使用 ReportPaper，PDF 生成独立排版。
/// </summary>
public sealed class ReportPdfService
{
    static ReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;

        // QuestPDF 2026 默认不加载系统字体；显式注册中文系统字体。
        string chineseFont = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "msyh.ttc");

        if (File.Exists(chineseFont))
        {
            FontManager.RegisterFontFromFile(chineseFont);
        }

        string tempPath = Path.Combine(AppContext.BaseDirectory, "Temp", "QuestPDF");
        Directory.CreateDirectory(tempPath);
        QuestPDF.Settings.TemporaryStoragePath = tempPath;
    }

    public void Export(Report report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Microsoft YaHei").FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text("焊接接头热处理质量报告").FontSize(18).SemiBold();
                    header.Item().PaddingTop(4).Text($"报告编号：{report.ReportNo}").FontColor(Colors.Grey.Darken2);
                    header.Item().PaddingTop(2).Text($"生成时间：{report.GeneratedAt}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        AddInfo(table, "账户", report.AccountName);
                        AddInfo(table, "操作员", report.OperatorName);
                        AddInfo(table, "工号", report.JobNumber);
                        AddInfo(table, "生产日期", report.ProducedAt);
                        AddInfo(table, "设备编号", report.EquipmentName);
                        AddInfo(table, "工艺配方", report.RecipeName);
                        AddInfo(table, "生产线 / 班次", report.ShiftName);
                        AddInfo(table, "判定", report.ResultText);
                    });

                    column.Item().Text("工艺参数").FontSize(11).SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.5f);
                        });

                        HeaderCell(table, "程序段");
                        HeaderCell(table, "温度 ℃");
                        HeaderCell(table, "功率 %");
                        HeaderCell(table, "冷却方式");
                        HeaderCell(table, "冷却参数");

                        foreach (Report.ProcessRow row in report.ProcessRows)
                        {
                            BodyCell(table, row.Section);
                            BodyCell(table, row.TempText);
                            BodyCell(table, row.PowerText);
                            BodyCell(table, row.CoolingMode);
                            BodyCell(table, row.CoolingParam);
                        }
                    });

                    column.Item().Text("实测统计").FontSize(11).SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.3f);
                        });

                        HeaderCell(table, "指标");
                        HeaderCell(table, "起始");
                        HeaderCell(table, "峰值");
                        HeaderCell(table, "末值");
                        HeaderCell(table, "均值");
                        HeaderCell(table, "上下限");
                        HeaderCell(table, "超限");
                        HeaderCell(table, "判定");

                        foreach (Report.MetricRow row in report.MetricRows)
                        {
                            BodyCell(table, row.Name);
                            BodyCell(table, row.Start);
                            BodyCell(table, row.Peak);
                            BodyCell(table, row.End);
                            BodyCell(table, row.Average);
                            BodyCell(table, $"{row.MinText} / {row.MaxText}");
                            BodyCell(table, row.OverCount.ToString());
                            BodyCell(table, row.Judgement);
                        }
                    });

                    column.Item().PaddingTop(4).Text($"判定说明：{report.ResultDetail}");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("RailWeld · 第 ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                    text.Span(" 页");
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void AddInfo(TableDescriptor table, string label, string value)
    {
        table.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(label).SemiBold();
        table.Cell().Padding(4).Text(value);
    }

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold();
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
    }
}
