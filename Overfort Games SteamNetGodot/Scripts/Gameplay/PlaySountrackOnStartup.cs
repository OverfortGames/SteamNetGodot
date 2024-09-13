using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class PlaySountrackOnStartup : Node
    {
        [Export]
        public string soundtrackResourceId;

        [Export]
        public float volume = 1;

        public override void _Ready()
        {
            AudioManager.Instance.PlaySoundtrack(soundtrackResourceId, volume);
        }
    }
}