namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonCloseOptions : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            SceneLoader.Instance.RemoveSceneIfLoaded(ResourceId.Options);
        }
    }
}