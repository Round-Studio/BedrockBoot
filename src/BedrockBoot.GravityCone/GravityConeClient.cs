using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BedrockBoot.GravityCone.Entry;
using BedrockBoot.GravityCone.Entry.Result;

namespace BedrockBoot.GravityCone;

public class GravityConeClient : IDisposable
{
    public Process _process;
    private StreamWriter _stdinWriter;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<CliResponse>> _pendingRequests = new();
    private int _nextId = 1;
    private bool _disposed = false;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    public event EventHandler<CliEvent>? OnEvent;
    public event EventHandler<CliResponse>? OnResponse;
    public event EventHandler<Exception>? OnError;
    public event EventHandler? OnReady;

    public bool IsRunning => _process != null && !_process.HasExited;

    public async Task StartAsync(string cliPath, List<string>? peers = null,
        string? vendor = null, string? motd = null, string? workingDirectory = null)
    {
        if (IsRunning)
            throw new InvalidOperationException("CLI 已经在运行中");

        var args = new List<string>();
        if (peers != null)
            foreach (var peer in peers)
            {
                args.Add("-p");
                args.Add(peer);
            }

        if (!string.IsNullOrEmpty(vendor))
        {
            args.Add("-v");
            args.Add(vendor);
        }

        if (!string.IsNullOrEmpty(motd))
        {
            args.Add("-m");
            args.Add(motd);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(cliPath) ?? "."
        };

        _process = new Process { StartInfo = startInfo };

        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"启动 CLI 失败: {ex.Message}", ex);
        }

        _stdinWriter = _process.StandardInput;

        _ = Task.Run(() => ReadOutputLoopAsync(_cts.Token));
        _ = Task.Run(() => ReadErrorLoopAsync(_cts.Token));

        var readyTcs = new TaskCompletionSource<bool>();
        EventHandler<CliEvent>? handler = null;
        handler = (s, e) =>
        {
            if (e.Event == "system.ready")
            {
                readyTcs.TrySetResult(true);
                OnReady?.Invoke(this, EventArgs.Empty);
                OnEvent -= handler;
            }
        };
        OnEvent += handler;

        var timeout = Task.Delay(10000);
        var completed = await Task.WhenAny(readyTcs.Task, timeout);
        if (completed == timeout)
        {
            OnEvent -= handler;
            throw new TimeoutException("等待 CLI 就绪超时");
        }
    }

    private async Task ReadOutputLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _process != null && !_process.HasExited)
            {
                var line = await _process.StandardOutput.ReadLineAsync();
                if (line == null) break;
                
                Console.WriteLine($"[RAW] {line}");

                try
                {
                    var jsonDoc = JsonDocument.Parse(line);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("event", out var eventElement))
                    {
                        var evt = new CliEvent
                        {
                            Event = eventElement.GetString() ?? string.Empty,
                            Data = root.TryGetProperty("data", out var dataElement)
                                ? dataElement.Clone()
                                : JsonDocument.Parse("{}").RootElement
                        };
                        OnEvent?.Invoke(this, evt);
                    }
                    else if (root.TryGetProperty("id", out var idElement))
                    {
                        var id = idElement.GetInt32();
                        var response = JsonSerializer.Deserialize<CliResponse>(line);
                        if (response != null && _pendingRequests.TryRemove(id, out var tcs)) tcs.TrySetResult(response);
                        OnResponse?.Invoke(this, response!);
                    }
                }
                catch (JsonException ex)
                {
                    OnError?.Invoke(this, new Exception($"解析 JSON 失败: {line}", ex));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private async Task ReadErrorLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _process != null && !_process.HasExited)
            {
                var line = await _process.StandardError.ReadLineAsync();
                if (line == null) break;
                OnError?.Invoke(this, new Exception($"CLI 错误: {line}"));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnError?.Invoke(this, ex);
        }
    }

    public async Task<CliResponse> RequestAsync(string method, object? @params = null, int timeoutMs = 30000)
    {
        if (!IsRunning)
            throw new InvalidOperationException("CLI 未运行");

        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<CliResponse>();
        _pendingRequests.TryAdd(id, tcs);

        var request = new CliRequest
        {
            Id = id,
            Method = method,
            Params = @params ?? new { }
        };

        try
        {
            var json = JsonSerializer.Serialize(request);

            lock (_lock)
            {
                _stdinWriter.WriteLine(json);
                Console.WriteLine($"[SEND] {json}");
                _stdinWriter.Flush();
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            using var registration = cts.Token.Register(() =>
                tcs.TrySetException(new TimeoutException($"请求 {method} (ID: {id}) 超时")));

            return await tcs.Task;
        }
        catch(Exception exception)
        {
            _pendingRequests.TryRemove(id, out _);
            OnError?.Invoke(this, exception);
            return null;
        }
    }

    public async Task ShutdownAsync()
    {
        await RequestAsync("system.shutdown");
    }

    public async Task<StunResult> StunProbeAsync()
    {
        return JsonSerializer.Deserialize<StunResult>((await RequestAsync("stun.probe")).Data.ToString())!;
    }

    public async Task<RoomCreateResult> CreatePaperConnectRoomAsync(string playerName)
    {
        var @params = new { player_name = playerName, protocol = "paperconnect" };
        var response = await RequestAsync("room.create", @params);
        return JsonSerializer.Deserialize<RoomCreateResult>(response.Data.ToString())!;
    }

    public async Task<RoomJoinResult> JoinRoomAsync(string code, string playerName)
    {
        var @params = new { code, player_name = playerName };
        var response = await RequestAsync("room.join", @params, 60000);
        return JsonSerializer.Deserialize<RoomJoinResult>(response.Data.ToString())!;
    }

    public async Task StopRoomAsync()
    {
        await RequestAsync("room.stop");
    }

    public async Task LeaveRoomAsync()
    {
        await RequestAsync("room.leave");
    }

    public async Task<CliResponse> GetRoomStatusAsync()
    {
        return await RequestAsync("room.status");
    }

    public async Task AddPeersAsync(params string[] peers)
    {
        await RequestAsync("system.add_peers", new { peers });
    }

    public async Task StartLanDiscoveryAsync()
    {
        await RequestAsync("lan.start_discovery");
    }

    public async Task StopLanDiscoveryAsync()
    {
        await RequestAsync("lan.stop_discovery");
    }

    public async Task<LanServer[]> ListLanServersAsync()
    {
        var response = await RequestAsync("lan.list_servers");
        return JsonSerializer.Deserialize<LanServer[]>(response.Data.GetProperty("servers").ToString())!;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();

        try
        {
            if (IsRunning)
            {
                _stdinWriter.WriteLine(@"{""id"":114514,""method"":""system.shutdown"",""params"":{}}");
                _stdinWriter.Flush();
                _process.WaitForExit(3000);
            }
        }
        catch
        {
        }

        try
        {
            _process?.Kill();
        }
        catch
        {
        }

        try
        {
            _process?.Dispose();
        }
        catch
        {
        }

        try
        {
            _stdinWriter?.Dispose();
        }
        catch
        {
        }

        _cts.Dispose();

        foreach (var pair in _pendingRequests)
            pair.Value.TrySetException(new ObjectDisposedException("GravityConeClient"));
        _pendingRequests.Clear();
    }
}