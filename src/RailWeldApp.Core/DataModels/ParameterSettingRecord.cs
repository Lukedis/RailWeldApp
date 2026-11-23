using RailWeldApp.Enums;

namespace RailWeldApp.DataModels;

public sealed class ParameterSettingRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ParameterName { get; set; } = string.Empty;

    public string YWeldSeamZero { get; set; } = string.Empty;

    public string YCompensationZero { get; set; } = string.Empty;

    public bool IsCompensationEnabled { get; set; }

    public string WeldSeamHeight { get; set; } = string.Empty;

    public string ZCompensationZero { get; set; } = string.Empty;

    public bool IsSeamSearchEnabled { get; set; }

    public string XStartPosition { get; set; } = string.Empty;

    public string XStartSpeed { get; set; } = string.Empty;

    public string XWeldLimitPosition { get; set; } = string.Empty;

    public string XWeldLimitSpeed { get; set; } = string.Empty;

    public string XHeatDistance { get; set; } = string.Empty;

    public string XHeatSpeed { get; set; } = string.Empty;

    public string XAirDistance { get; set; } = string.Empty;

    public string XAirSpeed { get; set; } = string.Empty;

    public string YStartPosition { get; set; } = string.Empty;

    public string YStartSpeed { get; set; } = string.Empty;

    public string YWeldLimitPosition { get; set; } = string.Empty;

    public string YWeldLimitSpeed { get; set; } = string.Empty;

    public string YHeatDistance { get; set; } = string.Empty;

    public string YHeatSpeed { get; set; } = string.Empty;

    public string YAirDistance { get; set; } = string.Empty;

    public string YAirSpeed { get; set; } = string.Empty;

    public string ZStartPosition { get; set; } = string.Empty;

    public string ZStartSpeed { get; set; } = string.Empty;

    public string ZWeldLimitPosition { get; set; } = string.Empty;

    public string ZWeldLimitSpeed { get; set; } = string.Empty;

    public string ZHeatDistance { get; set; } = string.Empty;

    public string ZHeatSpeed { get; set; } = string.Empty;

    public string ZAirDistance { get; set; } = string.Empty;

    public string ZAirSpeed { get; set; } = string.Empty;

    public string StartTemp { get; set; } = string.Empty;

    public string StartPower { get; set; } = string.Empty;

    public CoolingModes StartCoolingMode { get; set; }

    public string StartCoolingParam { get; set; } = string.Empty;

    public string TransitionTemp { get; set; } = string.Empty;

    public string TransitionPower { get; set; } = string.Empty;

    public CoolingModes TransitionCoolingMode { get; set; }

    public string TransitionCoolingParam { get; set; } = string.Empty;

    public string FinalTemp { get; set; } = string.Empty;

    public string FinalPower { get; set; } = string.Empty;

    public CoolingModes FinalCoolingMode { get; set; }

    public string FinalCoolingParam { get; set; } = string.Empty;
    public string AirPressureMax { get; set; } = string.Empty;

    public string AirPressureMin { get; set; } = string.Empty;

    public string FlowMax { get; set; } = string.Empty;

    public string FlowMin { get; set; } = string.Empty;
    public string EnergyMax { get; set; } = string.Empty;
    public string EnergyMin { get; set; } = string.Empty;
}