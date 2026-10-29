using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PCL.Core.App;
using PCL.Core.IO.Net;
using PCL.Core.Link.EasyTier;
using PCL.Core.Logging;

namespace PCL;

/// <summary>
///     PCL 和姚修改版：自建联机（基于 EasyTier + 公共/自定义中继节点）。
///     房间号只用于推导 EasyTier 的网络名与密钥，不依赖任何中心服务器。
/// </summary>
public static class ModSelfLink
{
    public const string VirtualHostIp = "10.114.51.41";
    public const int DefaultGamePort = 25565;
    public const string DefaultRelays =
        "tcp://public.easytier.top:11010;udp://public.easytier.top:11010;tcp://public.easytier.cn:11010";

    private static Process? _etProcess;
    private static int _rpcPort;

    public static bool IsRunning => _etProcess is { HasExited: false };
    public static bool IsHost { get; private set; }
    public static string RoomCode { get; private set; } = "";
    public static int GamePort { get; private set; } = DefaultGamePort;
    public static int LocalPort { get; private set; } = DefaultGamePort;
    public static string? LastError { get; private set; }

    /// <summary>加入方在 Minecraft 里应该连接的地址。</summary>
    public static string JoinAddress => $"127.0.0.1:{LocalPort}";

    public static string? EasyTierDir
    {
        get
        {
            var candidates = new[]
            {
                Path.Combine(ModBase.exePath, "EasyTier", "easytier-windows-x86_64"),
                Path.Combine(ModBase.exePath, "EasyTier"),
                ETInfoProvider.ETPath
            };
            return candidates.FirstOrDefault(dir => File.Exists(Path.Combine(dir, "easytier-core.exe")));
        }
    }

    public static bool IsReady => EasyTierDir is not null;

    /// <summary>把房间号映射成 EasyTier 网络名与密钥。</summary>
    public static (string Name, string Secret) DeriveNetwork(string roomCode)
    {
        var code = roomCode.Trim().ToUpperInvariant();
        var name = "PCLHEYAO" + code;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("PCL-HEYAO-SELFHOST:" + code));
        var secret = Convert.ToHexString(hash)[..32].ToLowerInvariant();
        return (name, secret);
    }

    public static bool Start(string roomCode, bool isHost, int gamePort, string relays, string username)
    {
        Stop();

        LastError = null;
        var dir = EasyTierDir;
        if (dir is null)
        {
            LastError = "未找到 EasyTier 运行文件";
            return false;
        }

        var (name, secret) = DeriveNetwork(roomCode);
        _rpcPort = NetworkHelper.NewTcpPort();
        IsHost = isHost;
        RoomCode = roomCode.Trim().ToUpperInvariant();
        GamePort = gamePort;
        LocalPort = gamePort;

        var args = new List<string>
        {
            "--no-tun",
            "--network-name", name,
            "--network-secret", secret,
            "--relay-network-whitelist", name,
            "--private-mode", "true",
            "-l", "11020",
            "--rpc-portal", $"127.0.0.1:{_rpcPort}",
            "--enable-quic-proxy",
            "--enable-kcp-proxy",
            "--use-smoltcp",
            "--encryption-algorithm", "chacha20",
            "--compression", "zstd",
            "--multi-thread",
            "--hostname", (isHost ? "H|" : "J|") + (string.IsNullOrWhiteSpace(username) ? "Player" : username)
        };

        if (isHost)
        {
            args.AddRange(["-i", VirtualHostIp]);
            args.AddRange(["--tcp-whitelist", gamePort.ToString()]);
            args.AddRange(["--udp-whitelist", gamePort.ToString()]);
        }
        else
        {
            args.Add("-d");
            args.AddRange(["--port-forward", $"tcp://127.0.0.1:{LocalPort}/{VirtualHostIp}:{gamePort}"]);
            args.AddRange(["--port-forward", $"udp://127.0.0.1:{LocalPort}/{VirtualHostIp}:{gamePort}"]);
        }

        foreach (var relay in (relays ?? DefaultRelays).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var url = relay.Trim();
            if (url.Contains("://", StringComparison.Ordinal))
                args.AddRange(["-p", url]);
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(dir, "easytier-core.exe"),
                WorkingDirectory = dir,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            foreach (var arg in args) startInfo.ArgumentList.Add(arg);

            _etProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _etProcess.Exited += (_, _) => LogWrapper.Warn("SelfLink", "EasyTier 进程已退出");
            _etProcess.Start();
            LogWrapper.Info("SelfLink", $"已启动 EasyTier（{(isHost ? "房主" : "加入")}），房间 {RoomCode}，参数: {startInfo.Arguments}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            LogWrapper.Error(ex, "SelfLink", "启动 EasyTier 失败");
            _etProcess = null;
            return false;
        }
    }

    public static void Stop()
    {
        try
        {
            if (_etProcess is { HasExited: false })
            {
                _etProcess.Kill(true);
                _etProcess.WaitForExit(500);
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "SelfLink", "关闭 EasyTier 时出错");
        }
        finally
        {
            _etProcess = null;
            RoomCode = "";
            LastError = null;
        }
    }

    public static bool IsEasyTierProcessAlive() => IsRunning;

    /// <summary>读取 EasyTier 网络中的成员。</summary>
    public static List<SelfLinkPeer> GetPeers()
    {
        var result = new List<SelfLinkPeer>();
        var dir = EasyTierDir;
        if (dir is null || !IsRunning) return result;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(dir, "easytier-cli.exe"),
                WorkingDirectory = dir,
                Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} -o json peer",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using var process = Process.Start(psi);
            if (process is null) return result;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(1500);

            using var doc = JsonDocument.Parse(output);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var hostname = item.TryGetProperty("hostname", out var h) ? h.GetString() ?? "" : "";
                if (hostname.StartsWith("PublicServer", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = hostname.Split('|');
                result.Add(new SelfLinkPeer
                {
                    IsHost = hostname.StartsWith("H|", StringComparison.Ordinal),
                    Name = parts.Length >= 2 ? parts[1] : hostname,
                    Ipv4 = item.TryGetProperty("ipv4", out var ip) ? ip.GetString() ?? "" : "",
                    Cost = item.TryGetProperty("cost", out var cost) ? cost.GetString() ?? "" : "",
                    Ping = item.TryGetProperty("lat_ms", out var lat) ? lat.GetString() ?? "" : ""
                });
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "SelfLink", "读取 EasyTier 成员列表失败");
        }

        return result.OrderByDescending(p => p.IsHost).ToList();
    }
}

public class SelfLinkPeer
{
    public bool IsHost { get; init; }
    public string Name { get; init; } = "";
    public string Ipv4 { get; init; } = "";
    public string Cost { get; init; } = "";
    public string Ping { get; init; } = "";
}
