using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILabelGameVersion : Label
    {
        public override void _Ready()
        {
            Text = GameVersion.VERSION;
        }
    }
}