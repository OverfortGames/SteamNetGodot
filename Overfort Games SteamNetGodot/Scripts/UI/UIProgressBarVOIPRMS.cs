using Godot;
using Steamworks;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIProgressBarVOIPRMS : ProgressBar
    {
        [Export]
        private VOIP voip;

        public override void _Process(double delta)
        {
            if (SteamConnect.CurrentSteamConnectionStatus != SteamConnect.SteamConnectionStatus.Ok || SteamUser.VoiceRecord == false)
            {
                Value = 0;
                return;
            }

            Value = voip.CurrentRMS;
        }
    }
}