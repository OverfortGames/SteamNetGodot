using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButtonDisconnect : UIButton
    {
        [Export]
        private string question = "Are you sure you want to disconnect?";

        [Export]
        private string questionServer = "Are you sure you want to stop hosting?";

        [Export]
        private string option1 = "No 8)";

        [Export]
        private string option2 = "Yes";

        [Export]
        private string option2Server = "Stop Host";

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
                if (NetworkManager.Instance.IsServer())
                {
                    NetworkManager.Instance.StopHost();
                }
                else
                {
                    NetworkManager.Instance.ClientDisconnect();
                }
            };


            bool isServer = NetworkManager.Instance.IsServer();
            confirmationPrompt.Setup(isServer ? questionServer : question, option1, isServer ? option2Server : option2, option1Action, option2Action);
        }
    }
}