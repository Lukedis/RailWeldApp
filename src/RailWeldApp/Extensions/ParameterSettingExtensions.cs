using System.Collections.ObjectModel;
using RailWeldApp.DataModels;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Extensions;

public static class ParameterSettingExtensions
{
    public static ParameterSettingsItem ToViewModel(this ParameterSettingRecord dataModel)
    {
        return new ParameterSettingsItem
        {
            Id = dataModel.Id,
            ParameterName = dataModel.ParameterName,
            YWeldSeamZero = dataModel.YWeldSeamZero,
            YCompensationZero = dataModel.YCompensationZero,
            IsCompensationEnabled = dataModel.IsCompensationEnabled,
            WeldSeamHeight = dataModel.WeldSeamHeight,
            ZCompensationZero = dataModel.ZCompensationZero,
            IsSeamSearchEnabled = dataModel.IsSeamSearchEnabled,
            XStartPosition = dataModel.XStartPosition,
            XStartSpeed = dataModel.XStartSpeed,
            XWeldLimitPosition = dataModel.XWeldLimitPosition,
            XWeldLimitSpeed = dataModel.XWeldLimitSpeed,
            XHeatDistance = dataModel.XHeatDistance,
            XHeatSpeed = dataModel.XHeatSpeed,
            XAirDistance = dataModel.XAirDistance,
            XAirSpeed = dataModel.XAirSpeed,
            YStartPosition = dataModel.YStartPosition,
            YStartSpeed = dataModel.YStartSpeed,
            YWeldLimitPosition = dataModel.YWeldLimitPosition,
            YWeldLimitSpeed = dataModel.YWeldLimitSpeed,
            YHeatDistance = dataModel.YHeatDistance,
            YHeatSpeed = dataModel.YHeatSpeed,
            YAirDistance = dataModel.YAirDistance,
            YAirSpeed = dataModel.YAirSpeed,
            ZStartPosition = dataModel.ZStartPosition,
            ZStartSpeed = dataModel.ZStartSpeed,
            ZWeldLimitPosition = dataModel.ZWeldLimitPosition,
            ZWeldLimitSpeed = dataModel.ZWeldLimitSpeed,
            ZHeatDistance = dataModel.ZHeatDistance,
            ZHeatSpeed = dataModel.ZHeatSpeed,
            ZAirDistance = dataModel.ZAirDistance,
            ZAirSpeed = dataModel.ZAirSpeed,
            StartTemp = dataModel.StartTemp,
            StartPower = dataModel.StartPower,
            StartCoolingMode = dataModel.StartCoolingMode,
            StartCoolingParam = dataModel.StartCoolingParam,
            TransitionTemp = dataModel.TransitionTemp,
            TransitionPower = dataModel.TransitionPower,
            TransitionCoolingMode = dataModel.TransitionCoolingMode,
            TransitionCoolingParam = dataModel.TransitionCoolingParam,
            FinalTemp = dataModel.FinalTemp,
            FinalPower = dataModel.FinalPower,
            FinalCoolingMode = dataModel.FinalCoolingMode,
            FinalCoolingParam = dataModel.FinalCoolingParam,
            AirPressureMax = dataModel.AirPressureMax,
            AirPressureMin = dataModel.AirPressureMin,
            FlowMax = dataModel.FlowMax,
            FlowMin = dataModel.FlowMin,
            EnergyMax = dataModel.EnergyMax,
            EnergyMin = dataModel.EnergyMin
        };
    }

    public static List<ParameterSettingRecord> ToDataModels(this ObservableCollection<ParameterSettingsItem> viewModels)
        => [.. viewModels.Select(x => x.ToDataModel())];

    public static ObservableCollection<ParameterSettingsItem> ToViewModels(this List<ParameterSettingRecord> dataModels)
        => [.. dataModels.Select(x => x.ToViewModel())];

    public static ParameterSettingRecord ToDataModel(this ParameterSettingsItem viewModel)
    {
        return new ParameterSettingRecord
        {
            Id = viewModel.Id,
            ParameterName = viewModel.ParameterName,
            YWeldSeamZero = viewModel.YWeldSeamZero,
            YCompensationZero = viewModel.YCompensationZero,
            IsCompensationEnabled = viewModel.IsCompensationEnabled,
            WeldSeamHeight = viewModel.WeldSeamHeight,
            ZCompensationZero = viewModel.ZCompensationZero,
            IsSeamSearchEnabled = viewModel.IsSeamSearchEnabled,
            XStartPosition = viewModel.XStartPosition,
            XStartSpeed = viewModel.XStartSpeed,
            XWeldLimitPosition = viewModel.XWeldLimitPosition,
            XWeldLimitSpeed = viewModel.XWeldLimitSpeed,
            XHeatDistance = viewModel.XHeatDistance,
            XHeatSpeed = viewModel.XHeatSpeed,
            XAirDistance = viewModel.XAirDistance,
            XAirSpeed = viewModel.XAirSpeed,
            YStartPosition = viewModel.YStartPosition,
            YStartSpeed = viewModel.YStartSpeed,
            YWeldLimitPosition = viewModel.YWeldLimitPosition,
            YWeldLimitSpeed = viewModel.YWeldLimitSpeed,
            YHeatDistance = viewModel.YHeatDistance,
            YHeatSpeed = viewModel.YHeatSpeed,
            YAirDistance = viewModel.YAirDistance,
            YAirSpeed = viewModel.YAirSpeed,
            ZStartPosition = viewModel.ZStartPosition,
            ZStartSpeed = viewModel.ZStartSpeed,
            ZWeldLimitPosition = viewModel.ZWeldLimitPosition,
            ZWeldLimitSpeed = viewModel.ZWeldLimitSpeed,
            ZHeatDistance = viewModel.ZHeatDistance,
            ZHeatSpeed = viewModel.ZHeatSpeed,
            ZAirDistance = viewModel.ZAirDistance,
            ZAirSpeed = viewModel.ZAirSpeed,
            StartTemp = viewModel.StartTemp,
            StartPower = viewModel.StartPower,
            StartCoolingMode = viewModel.StartCoolingMode,
            StartCoolingParam = viewModel.StartCoolingParam,
            TransitionTemp = viewModel.TransitionTemp,
            TransitionPower = viewModel.TransitionPower,
            TransitionCoolingMode = viewModel.TransitionCoolingMode,
            TransitionCoolingParam = viewModel.TransitionCoolingParam,
            FinalTemp = viewModel.FinalTemp,
            FinalPower = viewModel.FinalPower,
            FinalCoolingMode = viewModel.FinalCoolingMode,
            FinalCoolingParam = viewModel.FinalCoolingParam,
            AirPressureMax = viewModel.AirPressureMax,
            AirPressureMin = viewModel.AirPressureMin,
            FlowMax = viewModel.FlowMax,
            FlowMin = viewModel.FlowMin,
            EnergyMax = viewModel.EnergyMax,
            EnergyMin = viewModel.EnergyMin
        };
    }
}
