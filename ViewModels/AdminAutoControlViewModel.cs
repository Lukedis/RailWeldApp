using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Extensions;
using RailWeldApp.Models;
using RailWeldApp.Services;

namespace RailWeldApp.ViewModels
{
    public partial class AdminAutoControlViewModel : PageViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly PlcDataCache _plcDataCache;
        private readonly ILogger<AdminAutoControlViewModel> _logger;

        public AdminAutoControlViewModel(DatabaseService databaseService, PlcDataCache plcDataCache, ILogger<AdminAutoControlViewModel> logger)
            : base(PageName.AdminAutoControl)
        {
            _databaseService = databaseService;
            _plcDataCache = plcDataCache;
            _logger = logger;

            SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
                _plcDataCache,
                nameof(INotifyPropertyChanged.PropertyChanged),
                OnPlcDataChanged);

            FetchParameterSettings();
        }

        private PlcSnapshot? Snapshot => _plcDataCache.LastSnapshot;

        private void OnPlcDataChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PlcDataCache.LastSnapshot) or nameof(PlcDataCache.IsConnected))
            {
                OnPropertyChanged(string.Empty);
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasParameterSettingsItem))]
        private ObservableCollection<ParameterSettingsItem> _parameterSettings = [];

        public bool HasParameterSettingsItem => ParameterSettings.Count > 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedParameterSettingsItem))]
        private string _selectedParameterSettingsId = string.Empty;

        public ParameterSettingsItem? SelectedParameterSettingsItem
            => ParameterSettings.FirstOrDefault(x => x.Id == SelectedParameterSettingsId);

        // 集合内容变化（Add/Remove/Clear）不会触发 ParameterSettings 属性本身的变更通知，
        // 所以 HasParameterSettingsItem 和 SelectedParameterSettingsItem 要手动通知。
        private void OnParameterSettingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasParameterSettingsItem));
            OnPropertyChanged(nameof(SelectedParameterSettingsItem));
        }

        /// <summary>
        ///  设置集合，并监听集合发生改变
        /// </summary>
        private void SetParameterSettings(ObservableCollection<ParameterSettingsItem> items)
        {
            ParameterSettings = items;
            ParameterSettings.CollectionChanged += OnParameterSettingsCollectionChanged;
        }

        private void FetchParameterSettings()
        {
            try
            {
                List<ParameterSettingRecord> parameterSettings = _databaseService.GetParameterSettingRecords();

                SetParameterSettings([.. parameterSettings.ToViewModels()]);

                SelectedParameterSettingsId = ParameterSettings.Count > 0
                    ? ParameterSettings[0].Id
                    : string.Empty;
            }
            catch (Exception ex)
            {
                // 数据库还没建表时不要让整个页面构造失败。
                _logger.LogError(ex, "读取参数配方失败。");

                SetParameterSettings([]);

                SelectedParameterSettingsId = string.Empty;
            }
        }

        [RelayCommand]
        private void AddNewParameterSettingsItem()
        {
            var newItem = new ParameterSettingsItem
            {
                Id = Guid.NewGuid().ToString("N"),
                ParameterName = "新建参数",
                StartCoolingMode = Enums.CoolingModes.Temperature,
                TransitionCoolingMode = Enums.CoolingModes.Temperature,
                FinalCoolingMode = Enums.CoolingModes.Temperature,
                IsNewItem = true,
            };

            ParameterSettings.Add(newItem);

            SelectedParameterSettingsId = newItem.Id;
        }

        [RelayCommand]
        private void RemoveParameterSettingsItem(string id)
        {
            ParameterSettingsItem? item = ParameterSettings.FirstOrDefault(x => x.Id == id);

            if (item is null)
            {
                return;
            }

            // TODO: 确认用户是否删除

            // 新建但还没入库的记录，只从 UI 删除，不要碰数据库。
            if (!item.IsNewItem)
            {
                _databaseService.DeleteParameterSettingRecord(item.Id);
            }

            var index = ParameterSettings.IndexOf(item);

            ParameterSettings.RemoveAt(index);

            if (ParameterSettings.Count == 0)
            {
                SelectedParameterSettingsId = string.Empty;

                return;
            }

            // 删除后优先选中后一条；删的是最后一条就选中前一条。
            var nextIndex = Math.Min(index, ParameterSettings.Count - 1);

            SelectedParameterSettingsId = ParameterSettings[nextIndex].Id;
        }

        [RelayCommand]
        private void SaveParameterSettingsItem()
        {
            ParameterSettingsItem? item = SelectedParameterSettingsItem;

            if (item is null)
            {
                return;
            }

            if (item.IsNewItem)
            {
                _databaseService.AddParameterSettingRecord(item.ToDataModel());

                item.IsNewItem = false;
            }
            else
            {
                _databaseService.UpdateParameterSettingRecord(item.ToDataModel());
            }
        }

        [RelayCommand]
        private void LoadToPlc()
        {
            // TODO: 把当前配方写入 PLC
        }

        // 页面顶部的实时反馈
        public double XActualPosition => Snapshot?.XActPos ?? 0;

        public double YActualPosition => Snapshot?.YActPos ?? 0;
        public double ZActualPosition => Snapshot?.ZActPos ?? 0;
        public double YSensorValue => Snapshot?.YActPos ?? 0;
        public double ZSensorValue => Snapshot?.ZActPos ?? 0;

        // 阈值卡片的实际值
        public double AirPressureActual => Snapshot?.ActPressure ?? 0;

        public double FlowActual => Snapshot?.ActFlow1 ?? 0;
        public double EnergyActual => Snapshot?.Energy ?? 0;

        public IReadOnlyList<CoolingModes> CoolingModes { get; } = Enum.GetValues<CoolingModes>();

        // 页头/页脚
        public string CurrentWeldNumber => "—";

        public string CurrentWeldMeta => string.Empty;
        public bool IsPlcConnected => _plcDataCache.IsConnected;
        public string AppVersion => "v1.0.0";
        public bool IsCommunicationOk => _plcDataCache.IsConnected;
        public int CommunicationDelayMs => 0;
        public string ClockText => DateTime.Now.ToString("HH:mm:ss");
    }
}
