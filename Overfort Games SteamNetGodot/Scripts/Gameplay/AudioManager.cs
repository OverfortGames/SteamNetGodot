using Godot;
using Steamworks.Data;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public partial class AudioManager : Node
    {
        public static AudioManager Instance { get; set; }

        private List<AudioStreamPlayer3D> players3D = new List<AudioStreamPlayer3D>();
        private List<AudioStreamPlayer2D> players2D = new List<AudioStreamPlayer2D>();

        private AudioStreamPlayer2D soundtrack;

        private int playersBufferSize = 20;

        public static Vector3 POSITION2D = new Vector3(-4000, -4000, -4000);

        private const string BUS_MUSIC = "Music";
        private const string BUS_UI = "UI";
        private const string BUS_MASTER = "Master";

        private Dictionary<int, double> audioDictionary = new Dictionary<int, double>();

        public override void _EnterTree()
        {
            if (Instance != null)
            {
                GD.PrintErr($"{GetType().Name} already instanced. This instance will be destroyed");
                QueueFree();
                return;
            }

            Instance = this;

            for (int i = 0; i < playersBufferSize; i++)
            {
                var audioStreamPlayer = new AudioStreamPlayer3D();
                audioStreamPlayer.Name = "Player 3D - " + i;
                AddChild(audioStreamPlayer);

                players3D.Add(audioStreamPlayer);
            }

            for (int i = 0; i < playersBufferSize; i++)
            {
                var audioStreamPlayer = new AudioStreamPlayer2D();
                audioStreamPlayer.Name = "Player 2D - " + i;
                AddChild(audioStreamPlayer);

                players2D.Add(audioStreamPlayer);
            }

            soundtrack = new AudioStreamPlayer2D();
            soundtrack.Name = "Soundtrack";
            soundtrack.Bus = BUS_MUSIC;
            AddChild(soundtrack);

            NetworkManager.OnStartup += NetworkManager_OnStartup;
        }

        public override void _ExitTree()
        {
            NetworkManager.OnStartup -= NetworkManager_OnStartup;
        }

        public override void _Ready()
        {
            if (SimpleSaveSystem.Load(SimpleSaveSystem.AUDIO_PATH, out var audioDictionaryAsVariant))
            {
                var audioDictionaryGodot = audioDictionaryAsVariant.AsGodotDictionary<int, double>();

                audioDictionary = GodotCollectionsUtilities.GetDictionary(audioDictionaryGodot);
            }
            else
            {
                for (int i = 0; i < AudioServer.BusCount; i++)
                {
                    audioDictionary.Add(i, AudioServer.GetBusVolumeDb(i));
                }
                Save();
            }

            for (int i = 0; i < AudioServer.BusCount; i++)
            {
                AudioServer.SetBusVolumeDb(i, (float)audioDictionary[i]);
            }
        }

        private void NetworkManager_OnStartup()
        {
            NetworkManager.Instance.Client_SubscribeRPC<PlayOneShotPacket>(PlayOneShotPacketReceived, () => this.IsValid() == false);
        }

        public void Save(int busIndex, double value)
        {
            audioDictionary[busIndex] = Mathf.LinearToDb(value);

            Save();
        }

        private void Save()
        {
            SimpleSaveSystem.Save(GodotCollectionsUtilities.GetGodotDictionary(audioDictionary), SimpleSaveSystem.AUDIO_PATH);
        }

        public void PlayOneShotUI(string audioClipResourceId, float volume)
        {
            PlayOneShot(audioClipResourceId, POSITION2D, volume, BUS_UI);
        }

        public void PlayOneShot(string audioClipResourceId, Vector3 position, float volume, string bus = "")
        {
            AudioStream audioClipAsAudioStream = SimpleResourceLoader.Instance.GetResource(audioClipResourceId) as AudioStream;

            if (audioClipAsAudioStream == null)
            {
                GD.PrintErr($"audioClip: {audioClipResourceId} is not {typeof(AudioStream)}");
                return;
            }

            if (position != POSITION2D)
            {
                // Create a new AudioStreamPlayer3D node
                var audioStreamPlayer = GetFirstAvailablePlayer3D();

                // Set the audio stream
                audioStreamPlayer.Stream = audioClipAsAudioStream;

                // Set the volume
                audioStreamPlayer.VolumeDb = Mathf.LinearToDb(volume);

                // Set the position
                audioStreamPlayer.GlobalPosition = position;

                if (string.IsNullOrEmpty(bus))
                {
                    audioStreamPlayer.Bus = BUS_MASTER;
                }
                else
                {
                    audioStreamPlayer.Bus = bus;
                }

                // Play the audio stream
                audioStreamPlayer.Play();
            }
            else
            {
                // Create a new AudioStreamPlayer3D node
                var audioStreamPlayer = GetFirstAvailablePlayer2D();

                // Set the audio stream
                audioStreamPlayer.Stream = audioClipAsAudioStream;

                // Set the volume
                audioStreamPlayer.VolumeDb = Mathf.LinearToDb(volume);

                if (string.IsNullOrEmpty(bus))
                {
                    audioStreamPlayer.Bus = BUS_MASTER;
                }
                else
                {
                    audioStreamPlayer.Bus = bus;
                }

                // Play the audio stream
                audioStreamPlayer.Play();
            }
        }

        public void PlaySoundtrack(string audioClipResourceId, float volume)
        {
            StopSoundtrack();
            soundtrack.Stream = SimpleResourceLoader.Instance.GetResource(audioClipResourceId) as AudioStream;
            soundtrack.VolumeDb = Mathf.LinearToDb(volume);
            soundtrack.Play();
        }

        public void StopSoundtrack()
        {
            soundtrack.Playing = false;
        }

        public AudioStreamPlayer3D GetFirstAvailablePlayer3D()
        {
            foreach (var player in players3D)
            {
                if (player.Playing == false)
                    return player;
            }

            return null;
        }

        public AudioStreamPlayer2D GetFirstAvailablePlayer2D()
        {
            foreach (var player in players2D)
            {
                if (player.Playing == false)
                    return player;
            }

            return null;
        }

        #region SERVER

        public void PlayOneShotBroadcast(SendType sendType, string audioClipResourceId, Vector3 position, float volume, string bus = "", Connection except = default)
        {
            if (NetworkManager.Instance == null)
                return;

            var playOneShotPacket = new PlayOneShotPacket();
            playOneShotPacket.position = position;
            playOneShotPacket.volume = volume;
            playOneShotPacket.audioClipResourceId = audioClipResourceId;
            playOneShotPacket.bus = bus;

            NetworkManager.Instance.Server.Broadcast(playOneShotPacket, sendType, except);
        }

        #endregion

        #region CLIENT

        private void PlayOneShotPacketReceived(PlayOneShotPacket playOneShotPacket)
        {
            PlayOneShot(playOneShotPacket.audioClipResourceId, playOneShotPacket.position, playOneShotPacket.volume, playOneShotPacket.bus);
        }

        #endregion
    }

}