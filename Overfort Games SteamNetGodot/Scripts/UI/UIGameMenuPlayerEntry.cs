using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIGameMenuPlayerEntry : Control
    {
        [Export]
        public Label playerNameLabel;

        [Export]
        public Label pingLabel;

        [Export]
        public Button muteButton;

        [Export]
        public Button muteGlobalButton;

        [Export]
        public Button kickButton;

        [Export]
        public Button banButton;

        private PlayerData playerData;

        public override void _ExitTree()
        {
            Reset();
        }

        public void Init(PlayerData _playerData)
        {
            Reset();

            playerData = _playerData;

            playerNameLabel.Text = $"{playerData.name} ({playerData.steamId})";
            pingLabel.Text = $"{playerData.ping} RTT";
            kickButton.Pressed += KickButton_Pressed;
            banButton.Pressed += BanButton_Pressed;
            muteButton.Toggled += MuteButton_Toggled;
            muteGlobalButton.Toggled += MuteGlobalButton_Toggled;

            muteButton.SetPressedNoSignal(VOIPSettings.IsMuted(playerData.steamId));
            muteGlobalButton.SetPressedNoSignal(VOIPSettings.IsMutedGlobal(playerData.steamId));
        }

        public void Reset()
        {
            playerData = default;
            playerNameLabel.Text = "Uninitialized Name";
            UIUtilities.ResetButtonToggledCallbacks(muteButton);
            UIUtilities.ResetButtonToggledCallbacks(muteGlobalButton);
            UIUtilities.ResetButtonPressedCallbacks(kickButton);
            UIUtilities.ResetButtonPressedCallbacks(banButton);
        }

        private void MuteGlobalButton_Toggled(bool toggledOn)
        {
            if (NetworkManager.Instance == null)
            {
                return;
            }

            if (NetworkManager.Instance.IsServer() == false)
            {
                UINotifications.Instance.PushNotification("You are not an admin", UINotifications.NotificationType.Error);
                return;
            }

            if (toggledOn)
            {
                VOIPSettings.MuteGlobal(playerData.steamId);
            }
            else
            {
                VOIPSettings.UnmuteGlobal(playerData.steamId);
            }
        }

        private void MuteButton_Toggled(bool toggledOn)
        {
            if (toggledOn)
            {
                VOIPSettings.Mute(playerData.steamId);
            }
            else
            {
                VOIPSettings.Unmute(playerData.steamId);
            }
        }

        private void BanButton_Pressed()
        {
            if (NetworkManager.Instance == null)
            {
                return;
            }

            if (NetworkManager.Instance.IsServer() == false)
            {
                UINotifications.Instance.PushNotification("You are not an admin", UINotifications.NotificationType.Error);
                return;
            }

            NetworkManager.Instance.Server.Ban(playerData.steamId);
        }

        private void KickButton_Pressed()
        {
            if (NetworkManager.Instance == null)
            {
                return;
            }

            if (NetworkManager.Instance.IsServer() == false)
            {
                UINotifications.Instance.PushNotification("You are not an admin", UINotifications.NotificationType.Error);
                return;
            }

            NetworkManager.Instance.Server_Kick(playerData.steamId);
        }
    }


}