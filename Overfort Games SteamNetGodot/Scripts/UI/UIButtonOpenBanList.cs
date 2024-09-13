using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonOpenBanList : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            SimpleSaveSystem.Load(SimpleSaveSystem.BAN_LIST_PATH, out var _);
            var path = ProjectSettings.GlobalizePath(SimpleSaveSystem.BAN_LIST_PATH);
            OS.ShellOpen(path);
        }
    }
}