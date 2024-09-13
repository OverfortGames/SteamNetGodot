namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonOpenGameMenuPlayers : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            SceneLoader.Instance.AddSceneAsync(ResourceId.GameMenuPlayers);
        }
    }
}