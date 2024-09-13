using Godot;
using Steamworks;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILabelToggleSpeech : Label
    {
        public override void _Process(double delta)
        {
            base._Process(delta);

            if (NetworkManager.Instance.IsClientConnected() == false)
            {
                Modulate = new Color(1, 1, 1, 0.65f);
                return;
            }

            if (SteamUser.VoiceRecord)
            {
                Modulate = new Color(0, 1, 0);
            }
            else
            {
                Modulate = new Color(1, 0, 0);
            }
        }

    }
}