using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UIButton : Button
    {
        public string audioOnClick = ResourceId.AudioUIClick;
        private float audioOnClickVolume = .5f;

        public string audioOnHover = ResourceId.AudioUIClick;
        private float audioOnHoverVolume = .5f;

        public override void _Ready()
        {
            Pressed += PlayOnClick;

            MouseEntered += PlayOnHover;
        }

        private void PlayOnHover()
        {
            AudioManager.Instance.PlayOneShotUI(audioOnHover, audioOnHoverVolume);
        }

        private void PlayOnClick()
        {
            AudioManager.Instance.PlayOneShotUI(audioOnClick, audioOnHoverVolume);
        }
    }

}