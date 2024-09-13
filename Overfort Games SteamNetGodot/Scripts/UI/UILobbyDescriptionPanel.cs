using Godot;
using Steamworks.Data;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILobbyDescriptionPanel : Node
    {
        [Export]
        private Label serverNameLabel;

        [Export]
        private Label ownerLabel;

        [Export]
        private Label serverDescriptionLabel;

        [Export]
        private Label playersCountLabel;

        [Export]
        private Label maxPlayersLabel;

        [Export]
        private Label gameVersionLabel;

        [Export]
        private Button joinServerButton;

        public override void _Ready()
        {
            ShowDefault();
        }

        public void Update(Lobby lobby)
        {
            serverNameLabel.Text = lobby.GetData(LobbyManager.NAME_LOBBYKEY);
            ownerLabel.Text = lobby.GetData(LobbyManager.OWNER_LOBBYKEY);
            serverDescriptionLabel.Text = lobby.GetData(LobbyManager.DESCRIPTION_LOBBYKEY);
            playersCountLabel.Text = lobby.GetData(LobbyManager.PLAYERSCOUNT_LOBBYKEY);
            maxPlayersLabel.Text = lobby.GetData(LobbyManager.MAXPLAYERS_LOBBYKEY);
            gameVersionLabel.Text = lobby.GetData(LobbyManager.GAMEVERSION_LOBBYKEY);

            UIUtilities.ResetButtonPressedCallbacks(joinServerButton);
            joinServerButton.Disabled = false;
            joinServerButton.Pressed += () => LobbyManager.Instance.Join(lobby);
        }

        public void ShowDefault()
        {
            serverNameLabel.Text = string.Empty;
            ownerLabel.Text = string.Empty;
            serverDescriptionLabel.Text = string.Empty;
            playersCountLabel.Text = string.Empty;
            maxPlayersLabel.Text = string.Empty;
            gameVersionLabel.Text = string.Empty;
            joinServerButton.Disabled = true;
            UIUtilities.ResetButtonPressedCallbacks(joinServerButton);
        }
    }

}

