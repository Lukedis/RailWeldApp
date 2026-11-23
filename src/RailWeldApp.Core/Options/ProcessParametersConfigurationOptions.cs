namespace RailWeldApp.Options
{
    /// <summary>
    ///  工艺参数配置选项类
    /// </summary>
    public sealed class ProcessParametersConfigurationOptions
    {
        public string ActWeldingHome { get; set; } = string.Empty;
        public string ActWeldingHeight { get; set; } = string.Empty;
        public string ActYCompensationHome { get; set; } = string.Empty;
        public string ActZCompensationHome { get; set; } = string.Empty;
        public string YAutoCompensationEnabled { get; set; } = string.Empty;
        public string ZAutoCompensationEnabled { get; set; } = string.Empty;
        public string AutoWeldingEnabled { get; set; } = string.Empty;
        public string ActHomePos { get; set; } = string.Empty;
        public string ActHomeSpeed { get; set; } = string.Empty;
        public string ActMaxPos { get; set; } = string.Empty;
        public string ActMaxSpeed { get; set; } = string.Empty;
        public string ActHeatPos { get; set; } = string.Empty;
        public string ActHeatSpeed { get; set; } = string.Empty;
        public string ActWindPos { get; set; } = string.Empty;
        public string ActWindSpeed { get; set; } = string.Empty;
        public string ActHaetTempSet1 { get; set; } = string.Empty;
        public string ActHaetTempSet2 { get; set; } = string.Empty;
        public string ActHaetTempSet3 { get; set; } = string.Empty;
        public string ActPowerSet1 { get; set; } = string.Empty;
        public string ActPowerSet2 { get; set; } = string.Empty;
        public string ActPowerSet3 { get; set; } = string.Empty;
        public string TempMode { get; set; } = string.Empty;
        public string TimeMode { get; set; } = string.Empty;
        public string ActWindTempSet { get; set; } = string.Empty;
        public string ActWindTimeSet { get; set; } = string.Empty;
        public string ActPressureMax { get; set; } = string.Empty;
        public string ActPressureMin { get; set; } = string.Empty;
        public string ActFlowMax { get; set; } = string.Empty;
        public string ActFlowMin { get; set; } = string.Empty;
        public string ActPowerKWSMax { get; set; } = string.Empty;
        public string ActPowerKWSMin { get; set; } = string.Empty;
    }
}