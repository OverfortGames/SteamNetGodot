namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonCloseGameMenu : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            NodeUtilities.GetNodeOfType<ToggleGameMenu>(GetTree().CurrentScene).Toggle(false);
        }
    }
}