using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Resources;
using RailWeldApp.Interfaces;

namespace RailWeldApp.Services;

public sealed class SoundService : ISoundService
{
    private const string _defaultSound = "Popup";
    private static readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();
    private readonly ConcurrentDictionary<string, Lazy<SoundPlayer?>> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lifecycleLock = new();
    private bool _disposed;

    public void Play(string soundName = _defaultSound)
    {
        lock (_lifecycleLock)
        {
            ThrowIfDisposed();

            // 找不到文件 / 解码失败时静默跳过：音效是装饰，不能因为它让界面崩掉
            GetPlayer(PickVariant(soundName))?.Play();
        }
    }

    // 支持 "a,b" 这种写法：随机挑一个，避免同一个音反复响
    private static string PickVariant(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName) || !soundName.Contains(','))
        {
            return soundName;
        }

        var names = soundName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return names.Length == 0 ? soundName : names[Random.Shared.Next(names.Length)];
    }

    // 预加载指定 WAV 音效
    public void Preload(string soundName = _defaultSound)
    {
        lock (_lifecycleLock)
        {
            ThrowIfDisposed();

            GetPlayer(PickVariant(soundName));
        }
    }

    // 停止已加载的指定音效
    public void Stop(string soundName = _defaultSound)
    {
        ValidateSoundName(soundName);

        lock (_lifecycleLock)
        {
            ThrowIfDisposed();

            var key = NormalizeSoundName(soundName);

            if (_players.TryGetValue(key, out Lazy<SoundPlayer?>? lazy) && lazy.IsValueCreated)
                lazy.Value?.Stop();
        }
    }

    // 释放所有播放器资源
    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (KeyValuePair<string, Lazy<SoundPlayer?>> entry in _players)
            {
                if (_players.TryRemove(entry.Key, out Lazy<SoundPlayer?>? lazy) && lazy.IsValueCreated)
                    lazy.Value?.Dispose();
            }
        }
    }

    private SoundPlayer? GetPlayer(string soundName)
    {
        ValidateSoundName(soundName);

        var key = NormalizeSoundName(soundName);

        return _players
            .GetOrAdd(key, name => new Lazy<SoundPlayer?>(() => TryCreate(name), LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }

    // 创建并加载 WAV 播放器；资源不存在或无法解码时返回 null
    private static SoundPlayer? TryCreate(string soundName)
    {
        var uri = new Uri($"pack://application:,,,/Assets/Sound/{soundName}", UriKind.Absolute);

        StreamResourceInfo? info = Application.GetResourceStream(uri);

        if (info is null)
        {
            // Assets/Sound 里没有这个文件：只记一条调试信息，不影响界面
            Debug.WriteLine($"找不到音频资源：{uri}");
            return null;
        }

        var player = new SoundPlayer(info.Stream);

        try
        {
            player.Load();

            return player;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"加载音频失败：{uri}（{ex.Message}）");

            player.Dispose();

            return null;
        }
    }

    // 规范化音效文件名
    private static string NormalizeSoundName(string soundName)
        => soundName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? soundName : $"{soundName}.wav";

    // 验证音效文件名
    private static void ValidateSoundName(string soundName)
    {
        if (string.IsNullOrWhiteSpace(soundName))
            throw new ArgumentException("声音文件名不能为空。", nameof(soundName));

        if (soundName.IndexOfAny(_invalidFileNameChars) >= 0 ||
            soundName.Contains('/') ||
            soundName.Contains('\\') ||
            soundName.Contains(':'))
        {
            throw new ArgumentException("声音文件名不能包含路径或非法字符。", nameof(soundName));
        }

        if (Path.GetExtension(soundName).Length > 0 && !soundName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("仅支持 WAV 音频文件。", nameof(soundName));
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
