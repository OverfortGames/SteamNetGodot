using Godot;
using System;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIConfirmationPrompt : Node
    {
        [Export]
        public Label questionLabel;

        [Export]
        public Button option1;

        [Export]
        public Button option2;

        public override void _EnterTree()
        {
            SetDefault();
        }

        public override void _ExitTree()
        {
            SetDefault();
        }

        public void Setup(string questionText, string option1Text, string option2Text, Action option1Callback, Action option2Callback)
        {
            questionLabel.Text = questionText;
            option1.Text = option1Text;
            option2.Text = option2Text;
            option1.Pressed += option1Callback;
            option2.Pressed += option2Callback;
        }

        private void SetDefault()
        {
            questionLabel.Text = "Game Breaking Question";
            option1.Text = "O1";
            option1.Text = "O2";
            UIUtilities.ResetButtonPressedCallbacks(option1);
            UIUtilities.ResetButtonPressedCallbacks(option2);
        }
    }
}