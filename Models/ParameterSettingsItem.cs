using CommunityToolkit.Mvvm.ComponentModel;
using RailWeldApp.Enums;

namespace RailWeldApp.ViewModels;

/// <summary>
///  参数设定
/// </summary>
public partial class ParameterSettingsItem : ViewModelBase
{
    // 位移传感器参数设定

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private bool _isNewItem;

    [ObservableProperty]
    private string _parameterName = string.Empty;

    [ObservableProperty]
    private string _yWeldSeamZero = string.Empty;

    [ObservableProperty]
    private string _yCompensationZero = string.Empty;

    [ObservableProperty]
    private bool _isCompensationEnabled;

    [ObservableProperty]
    private string _weldSeamHeight = string.Empty;

    [ObservableProperty]
    private string _zCompensationZero = string.Empty;

    [ObservableProperty]
    private bool _isSeamSearchEnabled;

    // 伺服轴位置设定

    [ObservableProperty]
    private string _xStartPosition = string.Empty;

    [ObservableProperty]
    private string _xStartSpeed = string.Empty;

    [ObservableProperty]
    private string _xWeldLimitPosition = string.Empty;

    [ObservableProperty]
    private string _xWeldLimitSpeed = string.Empty;

    [ObservableProperty]
    private string _xHeatDistance = string.Empty;

    [ObservableProperty]
    private string _xHeatSpeed = string.Empty;

    [ObservableProperty]
    private string _xAirDistance = string.Empty;

    [ObservableProperty]
    private string _xAirSpeed = string.Empty;

    [ObservableProperty]
    private string _yStartPosition = string.Empty;

    [ObservableProperty]
    private string _yStartSpeed = string.Empty;

    [ObservableProperty]
    private string _yWeldLimitPosition = string.Empty;

    [ObservableProperty]
    private string _yWeldLimitSpeed = string.Empty;

    [ObservableProperty]
    private string _yHeatDistance = string.Empty;

    [ObservableProperty]
    private string _yHeatSpeed = string.Empty;

    [ObservableProperty]
    private string _yAirDistance = string.Empty;

    [ObservableProperty]
    private string _yAirSpeed = string.Empty;

    [ObservableProperty]
    private string _zStartPosition = string.Empty;

    [ObservableProperty]
    private string _zStartSpeed = string.Empty;

    [ObservableProperty]
    private string _zWeldLimitPosition = string.Empty;

    [ObservableProperty]
    private string _zWeldLimitSpeed = string.Empty;

    [ObservableProperty]
    private string _zHeatDistance = string.Empty;

    [ObservableProperty]
    private string _zHeatSpeed = string.Empty;

    [ObservableProperty]
    private string _zAirDistance = string.Empty;

    [ObservableProperty]
    private string _zAirSpeed = string.Empty;

    // 工艺参数
    [ObservableProperty]
    private string _startTemp = string.Empty;

    [ObservableProperty]
    private string _startPower = string.Empty;

    [ObservableProperty]
    private CoolingModes _startCoolingMode;

    [ObservableProperty]
    private string _startCoolingParam = string.Empty;

    [ObservableProperty]
    private string _transitionTemp = string.Empty;

    [ObservableProperty]
    private string _transitionPower = string.Empty;

    [ObservableProperty]
    private CoolingModes _transitionCoolingMode;

    [ObservableProperty]
    private string _transitionCoolingParam = string.Empty;

    [ObservableProperty]
    private string _finalTemp = string.Empty;

    [ObservableProperty]
    private string _finalPower = string.Empty;

    [ObservableProperty]
    private CoolingModes _finalCoolingMode;

    [ObservableProperty]
    private string _finalCoolingParam = string.Empty;

    // 风压等...

    [ObservableProperty]
    private string _airPressureMax = string.Empty;

    [ObservableProperty]
    private string _airPressureMin = string.Empty;

    [ObservableProperty]
    private string _flowMax = string.Empty;

    [ObservableProperty]
    private string _flowMin = string.Empty;

    [ObservableProperty]
    private string _energyMax = string.Empty;

    [ObservableProperty]
    private string _energyMin = string.Empty;
}
