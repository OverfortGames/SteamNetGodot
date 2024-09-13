using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonToggleTestVOIP : UIButton
    {
        [Export]
        private VOIP voip;

        public override void _Ready()
        {
            base._Ready();

            Toggled += Button_Toggled;

            SteamConnect.OnCheckSteamConnectionStatus += NetworkManager_OnCheckSteamConnectionStatus;
        }

        public override void _ExitTree()
        {
            SteamConnect.OnCheckSteamConnectionStatus -= NetworkManager_OnCheckSteamConnectionStatus;
        }

        private void NetworkManager_OnCheckSteamConnectionStatus(SteamConnect.SteamConnectionStatus status)
        {
            if (status != SteamConnect.SteamConnectionStatus.Ok && ButtonPressed == true)
                ButtonPressed = false;
        }

        private void Button_Toggled(bool toggledOn)
        {
            if (toggledOn)
            {
                if (SteamConnect.CurrentSteamConnectionStatus != SteamConnect.SteamConnectionStatus.Ok)
                {
                    UINotifications.Instance.PushNotification("Steam connection invalid", UINotifications.NotificationType.Error);

                    ButtonPressed = false;
                    return;
                }

                voip.BeginRecording();
            }
            else
            {
                voip.EndRecording();
            }
        }
    }
}