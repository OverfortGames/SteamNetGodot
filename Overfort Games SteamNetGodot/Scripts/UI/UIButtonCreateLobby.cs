namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonCreateLobby : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            if (SteamConnect.CurrentSteamConnectionStatus != SteamConnect.SteamConnectionStatus.Ok)
            {
                UINotifications.Instance.PushNotification("Steam connection invalid", UINotifications.NotificationType.Error);
                return;
            }

            SceneLoader.Instance.LoadSceneAsync(ResourceId.CreateServer);
        }
    }
}