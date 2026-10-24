using RailWeldApp.Enums;

namespace RailWeldApp.ViewModels
{
    /// <summary>
    /// 历史查询：左侧当天焊缝列表，右侧选中焊缝的指标统计与采样明细，界面在 AdminHistoryQueryView。
    /// 页面没有定时器：只在「选日期 / 输编号 / 选焊缝」时各取一次数据。
    /// 行类型见视图顶部注释（WeldSummary / MetricStat / SampleRow）。
    /// </summary>
    public partial class AdminHistoryQueryViewModel() : PageViewModel(PageName.AdminHistoryQuery)
    {
    }
}
