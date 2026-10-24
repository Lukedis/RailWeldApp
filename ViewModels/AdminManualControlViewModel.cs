using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RailWeldApp.Enums;
using RailWeldApp.Interfaces;
using RailWeldApp.Models;

namespace RailWeldApp.ViewModels;

// 手动控制页：
// - 轴反馈来自 PlcDataCache（PlcPollingBackgroundService 写入）；
// - 16 个按钮都是“点动”：写 true，短延时后写 false，由 PLC 侧自锁/动作；
// - PowerSetpoint / TimeSetpoint 修改后防抖写入 DB7；
// - 未连接 PLC 时按钮禁用，所有写操作直接返回。
public partial class AdminManualControlViewModel : PageViewModel, INavigationAware
{
    // DB7 手动控制点表（来自旧系统说明 4.3）
    private const string _powerOnAddress = "DB7.DBX66.0";

    private const string _powerOffAddress = "DB7.DBX66.1";
    private const string _airBlowStartAddress = "DB7.DBX72.0";
    private const string _airBlowStopAddress = "DB7.DBX72.1";
    private const string _yAxisLeftAddress = "DB7.DBX74.0";
    private const string _yAxisRightAddress = "DB7.DBX74.1";
    private const string _zAxisUpAddress = "DB7.DBX74.2";
    private const string _zAxisDownAddress = "DB7.DBX74.3";
    private const string _jack1UpAddress = "DB7.DBX76.0";
    private const string _jack1DownAddress = "DB7.DBX76.1";
    private const string _jack2UpAddress = "DB7.DBX76.2";
    private const string _jack2DownAddress = "DB7.DBX76.3";
    private const string _jack3UpAddress = "DB7.DBX76.4";
    private const string _jack3DownAddress = "DB7.DBX76.5";
    private const string _jack4UpAddress = "DB7.DBX76.6";
    private const string _jack4DownAddress = "DB7.DBX76.7";

    private const string _powerSetpointAddress = "DB7.DBD80";
    private const string _timeSetpointAddress = "DB7.DBD68";

    private static readonly TimeSpan _pulseWidth = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan _setpointWriteDelay = TimeSpan.FromMilliseconds(300);

    private readonly PlcDataCache _plcDataCache;
    private readonly IPlcService _plcService;
    private readonly ILogger<AdminManualControlViewModel> _logger;

    private readonly SemaphoreSlim _pulseLock = new(1, 1);
    private CancellationTokenSource? _powerSetpointCts;
    private CancellationTokenSource? _timeSetpointCts;

    public AdminManualControlViewModel(PlcDataCache plcDataCache, IPlcService plcService, ILogger<AdminManualControlViewModel> logger)
        : base(PageName.AdminManualControl)
    {
        _plcDataCache = plcDataCache;
        _plcService = plcService;
        _logger = logger;

        SubscribeWeak<INotifyPropertyChanged, PropertyChangedEventArgs>(
            _plcDataCache,
            nameof(INotifyPropertyChanged.PropertyChanged),
            OnPlcDataChanged);
    }

    private PlcSnapshot? Snapshot => _plcDataCache.LastSnapshot;

    private bool CanOperate() => _plcService.IsConnected;

    private void OnPlcDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlcDataCache.IsConnected))
        {
            RefreshCommandStates();
        }

        if (e.PropertyName is nameof(PlcDataCache.LastSnapshot)
            or nameof(PlcDataCache.IsConnected)
            or nameof(PlcDataCache.LastError))
        {
            OnPropertyChanged(string.Empty);
        }
    }

    /// <summary>
    ///  刷新命令是否可以执行
    /// </summary>
    private void RefreshCommandStates()
    {
        PowerOnCommand.NotifyCanExecuteChanged();
        PowerOffCommand.NotifyCanExecuteChanged();
        AirBlowStartCommand.NotifyCanExecuteChanged();
        AirBlowStopCommand.NotifyCanExecuteChanged();
        YAxisLeftCommand.NotifyCanExecuteChanged();
        YAxisRightCommand.NotifyCanExecuteChanged();
        ZAxisUpCommand.NotifyCanExecuteChanged();
        ZAxisDownCommand.NotifyCanExecuteChanged();
        Jack1UpCommand.NotifyCanExecuteChanged();
        Jack1DownCommand.NotifyCanExecuteChanged();
        Jack2UpCommand.NotifyCanExecuteChanged();
        Jack2DownCommand.NotifyCanExecuteChanged();
        Jack3UpCommand.NotifyCanExecuteChanged();
        Jack3DownCommand.NotifyCanExecuteChanged();
        Jack4UpCommand.NotifyCanExecuteChanged();
        Jack4DownCommand.NotifyCanExecuteChanged();
    }

    // 点动：true -> 80ms -> false。PLC 侧负责自锁/互锁。
    private async Task PulseAsync(string address)
    {
        if (!CanOperate())
        {
            return;
        }

        await _pulseLock.WaitAsync();

        try
        {
            await _plcService.WriteAsync(address, true);

            await Task.Delay(_pulseWidth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动点动写入失败：{Address}", address);
        }
        finally
        {
            try
            {
                await _plcService.WriteAsync(address, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动点动复位失败：{Address}", address);
            }

            _pulseLock.Release();
        }
    }

    private void ScheduleSetpointWrite(ref CancellationTokenSource? cts, string address, float value)
    {
        if (!CanOperate())
        {
            return;
        }

        cts?.Cancel();
        cts?.Dispose();

        var source = new CancellationTokenSource();

        cts = source;

        _ = WriteSetpointAsync(address, value, source.Token);
    }

    private async Task WriteSetpointAsync(string address, float value, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_setpointWriteDelay, cancellationToken);

            if (cancellationToken.IsCancellationRequested || !CanOperate())
            {
                return;
            }

            await _plcService.WriteAsync(address, value, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 用户连续输入，旧值被新值取消
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动设定值写入失败：{Address} = {Value}", address, value);
        }
    }

    /// <summary>
    ///  当页面进入时
    /// </summary>
    public void OnNavigatedTo()
    {
        RefreshCommandStates();
    }

    /// <summary>
    ///  当页面离开时
    /// </summary>
    public void OnNavigatedFrom()
    {
        _powerSetpointCts?.Cancel();
        _powerSetpointCts?.Dispose();
        _powerSetpointCts = null;

        _timeSetpointCts?.Cancel();
        _timeSetpointCts?.Dispose();
        _timeSetpointCts = null;
    }

    // 轴反馈
    public double XAxisActualPosition => Snapshot?.XActPos ?? 0;

    public double XAxisActualSpeed => Snapshot?.XActSpeed ?? 0;
    public double XAxisTargetPosition => Snapshot?.XSetPos ?? 0;
    public double XAxisTargetSpeed => Snapshot?.XSetSpeed ?? 0;
    public double XAxisCurrent => Snapshot?.XActCurrent ?? 0;

    public double YAxisActualPosition => Snapshot?.YActPos ?? 0;
    public double YAxisActualSpeed => Snapshot?.YActSpeed ?? 0;
    public double YAxisTargetPosition => Snapshot?.YSetPos ?? 0;
    public double YAxisTargetSpeed => Snapshot?.YSetSpeed ?? 0;
    public double YAxisCurrent => Snapshot?.YActCurrent ?? 0;

    public double ZAxisActualPosition => Snapshot?.ZActPos ?? 0;
    public double ZAxisActualSpeed => Snapshot?.ZActSpeed ?? 0;
    public double ZAxisTargetPosition => Snapshot?.ZSetPos ?? 0;
    public double ZAxisTargetSpeed => Snapshot?.ZSetSpeed ?? 0;
    public double ZAxisCurrent => Snapshot?.ZActCurrent ?? 0;

    // 手动设定值
    [ObservableProperty]
    private double _powerSetpoint;

    [ObservableProperty]
    private double _timeSetpoint;

    partial void OnPowerSetpointChanged(double value)
        => ScheduleSetpointWrite(ref _powerSetpointCts, _powerSetpointAddress, (float)value);

    partial void OnTimeSetpointChanged(double value)
        => ScheduleSetpointWrite(ref _timeSetpointCts, _timeSetpointAddress, (float)value);

    // 16 个点动命令
    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task PowerOnAsync() => PulseAsync(_powerOnAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task PowerOffAsync() => PulseAsync(_powerOffAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task AirBlowStartAsync() => PulseAsync(_airBlowStartAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task AirBlowStopAsync() => PulseAsync(_airBlowStopAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task YAxisLeftAsync() => PulseAsync(_yAxisLeftAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task YAxisRightAsync() => PulseAsync(_yAxisRightAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task ZAxisUpAsync() => PulseAsync(_zAxisUpAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task ZAxisDownAsync() => PulseAsync(_zAxisDownAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack1UpAsync() => PulseAsync(_jack1UpAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack1DownAsync() => PulseAsync(_jack1DownAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack2UpAsync() => PulseAsync(_jack2UpAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack2DownAsync() => PulseAsync(_jack2DownAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack3UpAsync() => PulseAsync(_jack3UpAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack3DownAsync() => PulseAsync(_jack3DownAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack4UpAsync() => PulseAsync(_jack4UpAddress);

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private Task Jack4DownAsync() => PulseAsync(_jack4DownAddress);

    // 页脚
    public string AppVersion => "v1.0.0";

    public bool IsCommunicationOk => _plcDataCache.IsConnected;
    public int CommunicationDelayMs => 0;
    public string ClockText => DateTime.Now.ToString("HH:mm:ss");
}
