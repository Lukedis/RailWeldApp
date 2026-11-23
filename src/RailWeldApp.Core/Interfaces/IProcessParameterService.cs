using RailWeldApp.DataModels;

namespace RailWeldApp.Interfaces;

/// <summary>
/// 把配方写入 PLC 工艺参数区。
/// </summary>
public interface IProcessParameterService
{
    Task LoadAsync(ParameterSettingRecord recipe, CancellationToken cancellationToken = default);
}
