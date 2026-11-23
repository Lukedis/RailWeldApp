using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.DataModels;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;

namespace RailWeldApp.ViewModels;

// 系统设置：Modbus / PLC 连接控制，连接错误持久化到 CommunicationError。
public partial class AdminSettingsViewModel : PageViewModel, INavigationAware
{
    private const int _maxErrors = 200;
    private readonly IPlcService _plc;
    private readonly IModbusRtuService _modbus;
    private readonly IPlcPollingBackgroundService _polling;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<AdminSettingsViewModel> _logger;

    private bool _subscribed;

    public AdminSettingsViewModel(
        IPlcService plc,
        IModbusRtuService modbus,
        IPlcPollingBackgroundService polling,
        IDatabaseService databaseService,
        ILogger<AdminSettingsViewModel> logger)
        : base(PageName.AdminSettings)
    {
        _plc = plc;
        _modbus = modbus;
        _polling = polling;
        _databaseService = databaseService;
        _logger = logger;
    }

    public ObservableCollection<CommunicationErrorItem> ConnectionErrors { get; } = [];

    public bool IsModbusConnected => _modbus.IsConnected;
    public bool IsModbusDisconnected => !IsModbusConnected;
    public string ModbusConnectionStatus => IsModbusConnected ? "已连接" : "未连接";
    public string ModbusLastError => _modbus.LastError ?? "暂无连接异常";

    public bool IsPlcConnected => _plc.IsConnected;
    public bool IsPlcDisconnected => !IsPlcConnected;
    public string PlcConnectionStatus => IsPlcConnected ? "已连接" : "未连接";
    public string PlcLastError => _plc.LastError ?? "暂无连接异常";

    public bool IsPolling => _polling.IsRunning;
    public bool CanTogglePolling => IsPolling || IsPlcConnected;
    public string PollingButtonText => IsPolling ? "停止采集" : "开始采集";

    public void OnNavigatedTo()
    {
        _ = LoadConnectionErrorsAsync();

        if (_subscribed)
        {
            return;
        }

        // 监听事件
        _modbus.ConnectionStatusChanged += OnModbusConnectionChanged;
        _plc.ConnectionStateChanged += OnPlcConnectionChanged;
        _subscribed = true;

        RefreshStatus();
    }

    public void OnNavigatedFrom()
    {
        if (!_subscribed)
        {
            return;
        }

        _modbus.ConnectionStatusChanged -= OnModbusConnectionChanged;
        _plc.ConnectionStateChanged -= OnPlcConnectionChanged;
        _subscribed = false;
    }

    private void OnModbusConnectionChanged(object? sender, bool connected)
        => RefreshFromAnyThread();

    private void OnPlcConnectionChanged(object? sender, bool connected)
        => RefreshFromAnyThread();

    private void RefreshFromAnyThread()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(RefreshStatus));
            return;
        }

        RefreshStatus();
    }

    /// <summary>
    ///   OnPropertyChanged(string.Empty) 通知所有属性发生改变
    /// </summary>
    private void RefreshStatus() => OnPropertyChanged(string.Empty);

    private void AddError(string type, string message)
    {
        string normalized = string.IsNullOrWhiteSpace(message) ? "连接失败" : message;

        ConnectionErrors.Insert(0, new CommunicationErrorItem
        {
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            CommunicationType = type,
            Message = normalized,
        });

        while (ConnectionErrors.Count > _maxErrors)
        {
            ConnectionErrors.RemoveAt(ConnectionErrors.Count - 1);
        }

        _ = SaveErrorAsync(type, normalized);
    }

    private async Task SaveErrorAsync(string type, string message)
    {
        try
        {
            await _databaseService.AddCommunicationErrorAsync(new CommunicationErrorRecord
            {
                OccurredAt = DateTime.Now,
                CommunicationType = type,
                Message = message,
                Detail = message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "通信错误写入数据库失败。");
        }
    }

    private async Task LoadConnectionErrorsAsync()
    {
        try
        {
            List<CommunicationErrorRecord> records = await _databaseService
                .GetCommunicationErrorsAsync(_maxErrors);

            if (records.Count == 0)
                return;

            ConnectionErrors.Clear();

            foreach (CommunicationErrorRecord record in records)
            {
                ConnectionErrors.Add(new CommunicationErrorItem
                {
                    Time = record.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    CommunicationType = record.CommunicationType,
                    Message = record.Message,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取通信错误记录失败。");
        }
    }

    [RelayCommand]
    private async Task ConnectModbusAsync()
    {
        try
        {
            await _modbus.ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modbus RTU 连接失败。");
            AddError("Modbus RTU", ex.Message);
        }
        finally
        {
            RefreshStatus();
        }
    }

    [RelayCommand]
    private async Task DisconnectModbusAsync()
    {
        try
        {
            await _modbus.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modbus RTU 断开失败。");
            AddError("Modbus RTU", ex.Message);
        }
        finally
        {
            RefreshStatus();
        }
    }

    [RelayCommand]
    private async Task ConnectPlcAsync()
    {
        try
        {
            var connected = await _plc.ConnectAsync();

            if (!connected)
            {
                AddError("Siemens PLC", _plc.LastError ?? "连接失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC 连接失败。");
            AddError("Siemens PLC", ex.Message);
        }
        finally
        {
            RefreshStatus();
        }
    }

    [RelayCommand]
    private async Task DisconnectPlcAsync()
    {
        try
        {
            if (IsPolling)
            {
                await _polling.StopAsync();
            }

            await _plc.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC 断开失败。");
            AddError("Siemens PLC", ex.Message);
        }
        finally
        {
            RefreshStatus();
        }
    }

    [RelayCommand]
    private async Task TogglePollingAsync()
    {
        try
        {
            if (IsPolling)
            {
                await _polling.StopAsync();
            }
            else if (IsPlcConnected)
            {
                await _polling.StartAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PLC 采集启动/停止失败。");
            AddError("PLC 采集", ex.Message);
        }
        finally
        {
            RefreshStatus();
        }
    }
}