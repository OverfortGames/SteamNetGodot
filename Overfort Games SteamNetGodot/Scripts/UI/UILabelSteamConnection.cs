using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILabelSteamConnection : Label
    {
        [Export]
        public string defaultText = "Steam connection: Checking... ";

        [Export]
        public string prefix = "Steam connection: ";

        public override void _Ready()
        {
            Text = defaultText;
            SteamConnect.OnCheckSteamConnectionStatus += OnCheckSteamConnectionStatus;
        }

        public override void _ExitTree()
        {
            SteamConnect.OnCheckSteamConnectionStatus -= OnCheckSteamConnectionStatus;
        }

        private void OnCheckSteamConnectionStatus(SteamConnect.SteamConnectionStatus status)
        {
            switch (status)
            {
                case SteamConnect.SteamConnectionStatus.Ok:
                    Text = prefix + "OK";
                    Modulate = new Color(0, 1, 0);
                    break;
                case SteamConnect.SteamConnectionStatus.Closed:
                    Text = prefix + "Client closed";
                    Modulate = new Color(1, 0, 0);
                    break;
                case SteamConnect.SteamConnectionStatus.NotConnected:
                    Text = prefix + "Not connected";
                    Modulate = new Color(1, 0, 0);
                    break;
                default:
                    break;
            }
        }
    }
}