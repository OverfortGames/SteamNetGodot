using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonQuit : UIButton
    {
        [Export]
        private string question = "Are you sure you want to quit?";

        [Export]
        private string option1 = "No 8)";

        [Export]
        private string option2 = "Yes";

        public override void _Ready()
        {
            base._Ready();

            Pressed += OnPressed;
        }

        private void OnPressed()
        {
            var confirmationPrompt = SceneLoader.Instance.AddScene(ResourceId.ConfirmationPrompt) as UIConfirmationPrompt;

            var option1Action = () =>
            {
                SceneLoader.Instance.RemoveSceneIfLoaded(ResourceId.ConfirmationPrompt);
            };

            var option2Action = () =>
            {
                GetTree().Quit();
            };

            confirmationPrompt.Setup(question, option1, option2, option1Action, option2Action);
        }
    }
}