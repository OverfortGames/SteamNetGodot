using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonCreateLobbyAndEnterGame : UIButton
    {
        [Export]
        public TextEdit lobbyName;

        [Export]
        public TextEdit lobbyDescription;

        [Export]
        public OptionButton lobbyMaxPlayers;

        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private async void OnPressed()
        {
            if (SteamConnect.CurrentSteamConnectionStatus != SteamConnect.SteamConnectionStatus.Ok)
            {
                UINotifications.Instance.PushNotification("Steam connection invalid", UINotifications.NotificationType.Error);
                return;
            }

            if (int.TryParse(lobbyMaxPlayers.GetItemText(lobbyMaxPlayers.Selected), out var maxPlayers) == false)
            {
                UINotifications.Instance.PushNotification("Max Players Not Valid", UINotifications.NotificationType.Error);
                return;
            }

            LobbyManager.Instance.SetMaxPlayers(maxPlayers);
            bool result = await LobbyManager.Instance.CreateLobby(lobbyName.Text, lobbyDescription.Text);
            if (result)
            {
                NetworkManager.startupCommand = NetworkManager.StartupCommand.Localhost;
                SceneLoader.Instance.LoadSceneAsync(ResourceId.Game, useFakeLoading: true, isGameplayScene: true);
            }
        }
    }
}