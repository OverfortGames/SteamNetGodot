using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UILoadingScreen : Control
    {
        [Export]
        public Label resourceLabel;

        [Export]
        public Label resourcePercentage;

        [Export]
        public TextureProgressBar progressBar;

        public override void _Ready()
        {
            SimpleResourceLoader.OnLoadResourceBegin += OnLoadResourceBegin;
            SimpleResourceLoader.OnLoadResourceProgress += OnLoadResourceProgress;
            SimpleResourceLoader.OnLoadResourceEnd += OnLoadResourceEnd;

            Visible = false;
        }

        private void OnLoadResourceEnd(string path)
        {
            progressBar.Value = 0;
            Visible = false;
        }

        private void OnLoadResourceProgress(string path, float progress)
        {
            resourcePercentage.Text = (progress * 100).ToString("0.0") + "%";
            progressBar.Value = progress * 100;
        }

        public override void _ExitTree()
        {
            SimpleResourceLoader.OnLoadResourceBegin -= OnLoadResourceBegin;
        }

        private void OnLoadResourceBegin(string path)
        {
            resourceLabel.Text = path;

            progressBar.Value = 0;
            Visible = true;
        }
    }

}