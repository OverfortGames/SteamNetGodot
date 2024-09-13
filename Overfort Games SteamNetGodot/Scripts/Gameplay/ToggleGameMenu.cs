using Godot;
using System;

namespace OverfortGames.SteamNetGodot
{
    public partial class ToggleGameMenu : Node
    {
        [Export]
        public Key key = Key.Escape;

        private bool toggled = false;

        public static event Action<bool> OnToggleGameMenu = delegate { };

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey eventKey && eventKey.Pressed)
            {
                // Check if the key pressed is the space bar
                if (eventKey.Keycode == key)
                {
                    Toggle(true);
                }
            }
        }

        public void Toggle(bool value)
        {
            if (toggled == value)
                return;

            toggled = value;

            if (toggled)
            {
                SceneLoader.Instance.AddScene(ResourceId.GameMenu);
            }
            else
            {
                SceneLoader.Instance.RemoveSceneIfLoaded(ResourceId.GameMenu);
            }

            OnToggleGameMenu(toggled);
        }
    }
}