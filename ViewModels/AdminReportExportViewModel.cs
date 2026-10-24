using RailWeldApp.Enums;

namespace RailWeldApp.ViewModels
{
    /// <summary>
    /// 报告导出：左侧选焊缝，右侧质量报告预览，界面在 AdminReportExportView。
    /// 预览纸张（x:Name="ReportPaper"）就是打印源，后端直接打印/打印到 PDF 即可。
    /// 字段契约见视图顶部注释（Report / ProcessRow / MetricRow）。
    /// </summary>
    public partial class AdminReportExportViewModel() : PageViewModel(PageName.AdminReportExport)
    {
    }
}
