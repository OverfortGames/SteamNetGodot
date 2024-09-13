using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIOptionButtonMaxPlayer : OptionButton
    {
        public override void _Ready()
        {
            base._Ready();

            ItemSelected += UIOptionButtonMaxPlayer_ItemSelected;
        }

        private void UIOptionButtonMaxPlayer_ItemSelected(long index)
        {
            if (LobbyManager.Instance == null)
                return;

            if (int.TryParse(GetItemText(Selected), out var maxPlayers) == false)
            {
                UINotifications.Instance.PushNotification("Max Players Not Valid", UINotifications.NotificationType.Error);
                return;
            }

            LobbyManager.Instance.SetMaxPlayers(maxPlayers);
        }
    }
}