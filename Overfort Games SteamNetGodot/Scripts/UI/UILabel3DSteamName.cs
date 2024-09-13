using Godot;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILabel3DSteamName : Label3D
    {
        [Export]
        private Pawn pawn;

        public override void _Ready()
        {
            if (pawn.HasOwnership())
                Visible = false;

            UpdateLabelText();
        }

        private async void UpdateLabelText()
        {
            while (this.IsValid())
            {
                var playersData = NetworkManager.Instance.GetGameState().GetPlayersData();

                foreach (var data in playersData)
                {
                    if (data.steamId == pawn.networkOwner)
                        Text = data.name;
                }
                await Task.Delay(2000);
            }
        }
    }
}