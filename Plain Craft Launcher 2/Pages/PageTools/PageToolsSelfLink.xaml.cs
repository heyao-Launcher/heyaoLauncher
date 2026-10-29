using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

public partial class PageToolsSelfLink
{
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private DispatcherTimer? _timer;

    public PageToolsSelfLink()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshUi();
        Unloaded += (_, _) => StopTimer();
        if (string.IsNullOrWhiteSpace(TextRoomCode.Text)) TextRoomCode.Text = GenerateCode();
        TextRelays.Text = ModSelfLink.DefaultRelays;
        TextGamePort.Text = ModSelfLink.DefaultGamePort.ToString();
    }

    private static string GenerateCode()
    {
        var random = new Random();
        return new string(Enumerable.Range(0, 6).Select(_ => CodeChars[random.Next(CodeChars.Length)]).ToArray());
    }

    private void BtnRandomCode_Click(object sender, MouseButtonEventArgs e)
    {
        TextRoomCode.Text = GenerateCode();
    }

    private void BtnHost_Click(object sender, MouseButtonEventArgs e)
    {
        StartRoom(true);
    }

    private void BtnJoin_Click(object sender, MouseButtonEventArgs e)
    {
        StartRoom(false);
    }

    private void BtnStop_Click(object sender, MouseButtonEventArgs e)
    {
        ModSelfLink.Stop();
        StopTimer();
        RefreshUi();
        HintService.Hint(Lang.Text("Tools.SelfLink.Stopped"));
    }

    private void StartRoom(bool isHost)
    {
        var code = (TextRoomCode.Text ?? "").Trim().ToUpperInvariant();
        if (code.Length is < 4 or > 10 || !code.All(char.IsAsciiLetterOrDigit))
        {
            HintService.Hint(Lang.Text("Tools.SelfLink.Error.Code"), HintType.Error);
            return;
        }

        if (!int.TryParse(TextGamePort.Text, out var port) || port is < 1 or > 65535)
        {
            HintService.Hint(Lang.Text("Tools.SelfLink.Error.Port"), HintType.Error);
            return;
        }

        if (!ModSelfLink.IsReady)
        {
            HintService.Hint(Lang.Text("Tools.SelfLink.Error.NoEasyTier"), HintType.Error);
            return;
        }

        var username = string.IsNullOrWhiteSpace(Config.Link.Username) ? "Player" : Config.Link.Username;
        if (!ModSelfLink.Start(code, isHost, port, TextRelays.Text, username))
        {
            HintService.Hint(ModSelfLink.LastError ?? Lang.Text("Tools.SelfLink.Error.Start"), HintType.Error);
            return;
        }

        StartTimer();
        RefreshUi();
        HintService.Hint(Lang.Text("Tools.SelfLink.Started"));
    }

    private void StartTimer()
    {
        StopTimer();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => RefreshPeers();
        _timer.Start();
        RefreshPeers();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void RefreshUi()
    {
        var running = ModSelfLink.IsRunning;
        if (!running) StopTimer();

        if (BtnHost is not null)
        {
            BtnHost.IsEnabled = !running;
            BtnJoin.IsEnabled = !running;
            BtnStop.IsEnabled = running;
            TextRoomCode.IsEnabled = !running;
            TextGamePort.IsEnabled = !running;
            TextRelays.IsEnabled = !running;

            LabStatus.Text = running
                ? (ModSelfLink.IsHost
                    ? string.Format(Lang.Text("Tools.SelfLink.Status.Hosting"), ModSelfLink.RoomCode, ModSelfLink.GamePort)
                    : string.Format(Lang.Text("Tools.SelfLink.Status.Joined"), ModSelfLink.RoomCode, ModSelfLink.JoinAddress))
                : Lang.Text("Tools.SelfLink.Status.Idle");
        }

        if (!running) RefreshPeers();
    }

    private void RefreshPeers()
    {
        if (PanPeers is null) return;

        if (!ModSelfLink.IsRunning)
        {
            if (_timer is not null)
            {
                StopTimer();
                RefreshUi();
            }
            return;
        }

        var peers = ModSelfLink.GetPeers();
        PanPeers.Children.Clear();
        if (peers.Count == 0)
        {
            PanPeers.Children.Add(new TextBlock
            {
                Text = Lang.Text("Tools.SelfLink.NoMembers"),
                Opacity = 0.6
            });
            return;
        }

        foreach (var peer in peers)
        {
            var line = string.Format(Lang.Text("Tools.SelfLink.PeerLine"),
                peer.IsHost ? Lang.Text("Tools.SelfLink.HostTag") : Lang.Text("Tools.SelfLink.PlayerTag"),
                peer.Name, peer.Ipv4, peer.Ping, peer.Cost);
            PanPeers.Children.Add(new TextBlock { Text = line, Margin = new Thickness(0, 2, 0, 2) });
        }
    }
}
