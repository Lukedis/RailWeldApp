using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Options;

namespace RailWeldApp.Services;

/// <summary>
/// 将 ParameterSettingRecord 按 ProcessParametersAddress 写入 PLC。
///
/// 每个字段独立写入：某个字段为空或格式错误时只跳过该字段；
/// DBX 写 bool，DBD 写 float；PLC 写入失败会抛异常，页面负责提示。
/// </summary>
public sealed class ProcessParameterService(
    IPlcService plc,
    IOptions<ProcessParametersConfigurationOptions> options,
    ILogger<ProcessParameterService> logger)
    : IProcessParameterService
{
    private readonly ProcessParametersConfigurationOptions _addresses = options.Value;

    public async Task LoadAsync(ParameterSettingRecord recipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        if (!plc.IsConnected)
            throw new InvalidOperationException("PLC 未连接，无法加载工艺参数。");

        int written = 0;

        // 位移传感器零位 / 焊缝高度。
        written += await WriteFloatAsync(_addresses.ActWeldingHome, recipe.YWeldSeamZero, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActWeldingHeight, recipe.WeldSeamHeight, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActYCompensationHome, recipe.YCompensationZero, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActZCompensationHome, recipe.ZCompensationZero, cancellationToken);

        // 补偿和找焊缝使能。
        written += await WriteBoolAsync(_addresses.YAutoCompensationEnabled, recipe.IsCompensationEnabled, cancellationToken);
        written += await WriteBoolAsync(_addresses.ZAutoCompensationEnabled, recipe.IsCompensationEnabled, cancellationToken);
        written += await WriteBoolAsync(_addresses.AutoWeldingEnabled, recipe.IsSeamSearchEnabled, cancellationToken);

        // X 轴各阶段位置、速度。
        written += await WriteFloatAsync(_addresses.ActHomePos, recipe.XStartPosition, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActHomeSpeed, recipe.XStartSpeed, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActMaxPos, recipe.XWeldLimitPosition, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActMaxSpeed, recipe.XWeldLimitSpeed, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActHeatPos, recipe.XHeatDistance, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActHeatSpeed, recipe.XHeatSpeed, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActWindPos, recipe.XAirDistance, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActWindSpeed, recipe.XAirSpeed, cancellationToken);

        // 三段工艺温度、功率。
        written += await WriteFloatAsync(_addresses.ActHaetTempSet1, recipe.StartTemp, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActHaetTempSet2, recipe.TransitionTemp, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActHaetTempSet3, recipe.FinalTemp, cancellationToken);

        written += await WriteFloatAsync(_addresses.ActPowerSet1, recipe.StartPower, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActPowerSet2, recipe.TransitionPower, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActPowerSet3, recipe.FinalPower, cancellationToken);

        bool temperatureMode = recipe.StartCoolingMode == CoolingModes.Temperature
            || recipe.TransitionCoolingMode == CoolingModes.Temperature
            || recipe.FinalCoolingMode == CoolingModes.Temperature;

        bool timeMode = recipe.StartCoolingMode == CoolingModes.Time
            || recipe.TransitionCoolingMode == CoolingModes.Time
            || recipe.FinalCoolingMode == CoolingModes.Time;

        // 当前只有一个喷风模式位，按三段中是否出现温度/时间模式写入。
        written += await WriteBoolAsync(_addresses.TempMode, temperatureMode, cancellationToken);
        written += await WriteBoolAsync(_addresses.TimeMode, timeMode, cancellationToken);

        // 冷却参数字段只有一个，按最终段写入；现场点表明确后可改成不同字段。
        written += await WriteFloatAsync(_addresses.ActWindTempSet, recipe.FinalCoolingParam, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActWindTimeSet, recipe.FinalCoolingParam, cancellationToken);

        // 风压、流量、能量上下限。
        written += await WriteFloatAsync(_addresses.ActPressureMax, recipe.AirPressureMax, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActPressureMin, recipe.AirPressureMin, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActFlowMax, recipe.FlowMax, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActFlowMin, recipe.FlowMin, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActPowerKWSMax, recipe.EnergyMax, cancellationToken);
        written += await WriteFloatAsync(_addresses.ActPowerKWSMin, recipe.EnergyMin, cancellationToken);

        logger.LogInformation("工艺参数已写入 PLC，共 {Count} 个地址。", written);
    }

    private async Task<int> WriteFloatAsync(string address, string value, CancellationToken cancellationToken)
    {
        // 地址未配置就跳过，方便现场只启用部分参数。
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        {
            logger.LogWarning("工艺参数值无法解析：{Address} = {Value}", address, value);
            return 0;
        }

        await plc.WriteAsync(address, (float)number, cancellationToken);

        return 1;
    }

    private async Task<int> WriteBoolAsync(string address, bool value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
            return 0;

        await plc.WriteAsync(address, value, cancellationToken);

        return 1;
    }
}