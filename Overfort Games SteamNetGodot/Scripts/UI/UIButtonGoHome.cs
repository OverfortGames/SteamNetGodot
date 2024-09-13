namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonGoHome : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            SceneLoader.Instance.LoadSceneAsync(ResourceId.Home);
        }
    }
}