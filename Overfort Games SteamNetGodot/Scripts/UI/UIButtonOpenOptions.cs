using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonOpenOptions : UIButton
    {
        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            SceneLoader.Instance.AddSceneAsync(ResourceId.Options);
        }
    }
}