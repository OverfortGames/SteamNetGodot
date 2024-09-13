using Steamworks;
using System;
using Godot;
using System.IO;
using Steamworks.Data;
using System.Collections.Generic;
using LiteNetLib.Layers;

namespace OverfortGames.SteamNetGodot
{
    public partial class VOIP : Node
    {
        [Export]
        private Pawn pawn;

        [Export]
        public bool isLocalRecording;

        [Export]
        public AudioStreamPlayer3D player3D;

        [Export]
        private float outputVolumeMultiplier = 1.5f;

        private AudioStreamGeneratorPlayback playback;
        private AudioStreamGenerator generator;
        private readonly MemoryStream _compressedVoiceStream = new();
        private readonly MemoryStream _decompressedVoiceStream = new();
        private readonly Queue<float> _streamingReadQueue = new();

        private List<short> pcmSamples = new List<short>();

        private bool speakerInitialized;

        private ulong internalVoiceTick;
        private ulong lastReceivedInternalVoiceTick;

        private static List<VOIP> activeVOIPs = new List<VOIP>();

        private bool voiceToggleInputActive = false;

        public bool IsRecording { get; private set;}
        public float CurrentRMS { get; private set; }

        public override void _Ready()
        {
            if (isLocalRecording)
            {
                generator = new AudioStreamGenerator();
                generator.BufferLength = 0.25f;

                var player = new AudioStreamPlayer();
                player.VolumeDb = 1;
                player.Stream = generator;
                AddChild(player);

                player.Play();

                playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            }
            else
            {
                generator = new AudioStreamGenerator();
                generator.BufferLength = 1;

                if (player3D == null)
                {
                    player3D = new AudioStreamPlayer3D();
                    player3D.MaxDistance = 20;
                    player3D.VolumeDb = 1.333f;
                    AddChild(player3D);
                }

                player3D.Stream = generator;
                player3D.Play();

                playback = player3D.GetStreamPlayback() as AudioStreamGeneratorPlayback;
            }

            if (pawn.IsValid())
            {
                NetworkManager.Instance.Client_SubscribeRPC<VoicePacket, Connection>(Client_OnVoicePacketReceived, () => this.IsValid() == false);
                NetworkManager.Instance.Server_SubscribeRPC<VoicePacket, Connection>(Server_OnVoicePacketReceived, () => this.IsValid() == false);

                NetworkManager.Instance.OnTick += OnTick;
            }

            if(activeVOIPs.Contains(this) == false)
                activeVOIPs.Add(this);

            // Mute yourself
            VOIPSettings.Mute(SteamClient.SteamId);
        }

        public override void _ExitTree()
        {
            EndRecording();

            if (activeVOIPs.Contains(this))
                activeVOIPs.Remove(this);

            if (pawn.IsValid())
            {
                NetworkManager.Instance.OnTick -= OnTick;
            }
        }

        public override void _Process(double delta)
        {
            if (SteamClient.IsValid == false)
                return;

            HandleInput();

            ProcessLocalRecording();
        }

        private void OnTick(ulong tick)
        {
            if (SteamClient.IsValid == false)
                return;

            if ((tick % 10) == 0)
            { 
                ProcessSteamVoiceData();
            }
        }

        private void HandleInput()
        {
            if (NetworkManager.Instance == null)
                return;

            if (NetworkManager.Instance.IsClientConnected() == false)
                return;

            if (pawn.IsValid() == false || pawn.HasOwnership() == false)
                return;

            if (Input.IsActionJustPressed(InputSettings.VOICE_ACTION))
            {
                if (IsRecording == false)
                    BeginRecording();
            }

            if (Input.IsActionJustReleased(InputSettings.VOICE_ACTION))
            {
                if (IsRecording && voiceToggleInputActive == false)
                    EndRecording();
            }

            if (Input.IsActionJustPressed(InputSettings.VOICE_TOGGLE_ACTION))
            {
                voiceToggleInputActive = !voiceToggleInputActive;

                if (voiceToggleInputActive)
                    BeginRecording();
                else
                    EndRecording();
            }
        }

        private void ProcessSteamVoiceData()
        {
            if (isLocalRecording == false && (pawn.IsValid() == false || pawn.HasOwnership() == false))
                return;

            foreach (var activeVOIP in activeVOIPs)
            {
                if (activeVOIP.IsValid() == false)
                    continue;

                if (activeVOIP.isLocalRecording && activeVOIP != this)
                    return;
            }

            if (SteamUser.HasVoiceData)
            {
                _compressedVoiceStream.Position = 0;

                int numBytesWritten = SteamUser.ReadVoiceData(_compressedVoiceStream);

                if (isLocalRecording)
                {
                    DecompressAndListenVoiceData(new ArraySegment<byte>(_compressedVoiceStream.GetBuffer(), 0, numBytesWritten));
                }
                else
                {
                    Client_SendVoice(new ArraySegment<byte>(_compressedVoiceStream.GetBuffer(), 0, numBytesWritten));
                }
            }
        }

        private void ProcessLocalRecording()
        {
            if(IsRecording == false)
                return;

            if (isLocalRecording == false)
                return;

            foreach (var activeVOIP in activeVOIPs)
            {
                if (activeVOIP.IsValid() == false)
                    continue;

                if (activeVOIP.isLocalRecording && activeVOIP != this)
                    return;
            }

            if (SteamUser.HasVoiceData)
            {
                _compressedVoiceStream.Position = 0;

                int numBytesWritten = SteamUser.ReadVoiceData(_compressedVoiceStream);

                DecompressAndListenVoiceData(new ArraySegment<byte>(_compressedVoiceStream.GetBuffer(), 0, numBytesWritten));
            }
        }

        private void InitializeSpeaker()
        {
            if (!speakerInitialized)
            {
                speakerInitialized = true;
                generator.MixRate = SteamUser.OptimalSampleRate;
            }
        }

        public void BeginRecording()
        {
            if (SteamClient.IsValid == false)
                return;

            if (isLocalRecording == false && (pawn.IsValid() == false || pawn.HasOwnership() == false))
                return;

            IsRecording = true;

            GD.Print("VOIP - Begin Recording | isLocalRecording " + isLocalRecording);
            SteamUser.VoiceRecord = true;
        }

        public void EndRecording()
        {
            if (SteamClient.IsValid == false)
                return;

            IsRecording = false;

            if (pawn.IsValid())
            {
                if (pawn.HasOwnership())
                    SteamUser.VoiceRecord = false;
            }
            else
            { 
                SteamUser.VoiceRecord = false;
            }

            GD.Print("VOIP - End Recording");
        }

        private void DecompressAndListenVoiceData(ArraySegment<byte> voiceData)
        {
            InitializeSpeaker();

            pcmSamples.Clear();
            _streamingReadQueue.Clear();

            _compressedVoiceStream.Position = 0;
            _compressedVoiceStream.Write(voiceData);

            _compressedVoiceStream.Position = 0;
            _decompressedVoiceStream.Position = 0;

            int numBytesWritten = SteamUser.DecompressVoice(_compressedVoiceStream, voiceData.Count, _decompressedVoiceStream);

            _decompressedVoiceStream.Position = 0;
            while (_decompressedVoiceStream.Position < numBytesWritten)
            {
                byte byte1 = (byte)_decompressedVoiceStream.ReadByte();
                byte byte2 = (byte)_decompressedVoiceStream.ReadByte();

                short pcmShort = (short)((byte2 << 8) | (byte1 << 0));
                float pcmFloat = Convert.ToSingle(pcmShort) / short.MaxValue;

                pcmSamples.Add(pcmShort);

                _streamingReadQueue.Enqueue(pcmFloat);
            }

            CurrentRMS = CalculateRMS(pcmSamples);
            
            int framesAvailable = playback.GetFramesAvailable();

            while (framesAvailable > 0 && _streamingReadQueue.Count > 0)
            {
                Vector2 data = new Vector2(0, 0);
                if (_streamingReadQueue.TryDequeue(out float sample))
                {
                    data = new Vector2(sample * outputVolumeMultiplier, sample * outputVolumeMultiplier);
                }
                playback.PushFrame(data);

                framesAvailable--; // Decrement available frames
            }
        }

        private float CalculateRMS(List<short> audioSamples)
        {
            if (audioSamples == null || audioSamples.Count == 0)
            {
                return 0f;
            }

            double sum = 0;
            foreach (short sample in audioSamples)
            {
                sum += sample * sample;
            }

            double rms = Math.Sqrt(sum / audioSamples.Count);
            return (float)rms / short.MaxValue; // Normalize RMS to a value between 0 and 1
        }


        #region SERVER

        private void Server_OnVoicePacketReceived(VoicePacket packet, Connection from)
        {
            if (pawn.IsValid() == false)
                return;

            if (pawn.networkId != packet.networkId)
                return;

            ulong fromSteamId = (ulong)from.UserData;

            if (VOIPSettings.IsMutedGlobal(fromSteamId))
                return;

            packet.fromUser = fromSteamId;

            NetworkManager.Instance.Server.Broadcast(packet, SendType.Reliable, from);
        }

        #endregion

        #region CLIENT

        private void Client_OnVoicePacketReceived(VoicePacket packet, Connection from)
        {
            if (pawn.IsValid() == false)
                return;

            if (pawn.networkId != packet.networkId)
                return;

            if (packet.internalVoiceTick != lastReceivedInternalVoiceTick + 1)
            {
                GD.Print("voice packet loss");
            }

            lastReceivedInternalVoiceTick = packet.internalVoiceTick;

            ulong fromSteamId = packet.fromUser;

            if (VOIPSettings.IsMuted(fromSteamId))
                return;

            ArraySegment<byte> compressedVoiceData = packet.compressedVoiceData;

            DecompressAndListenVoiceData(compressedVoiceData);
        }

        public void Client_SendVoice(ArraySegment<byte> compressedVoiceData)
        {
            if (NetworkManager.Instance == null || NetworkManager.Instance.Client == null)
                return;

            if (pawn.IsValid() == false)
                return;

            internalVoiceTick += 1;

            VoicePacket voicePacket = new VoicePacket();
            voicePacket.compressedVoiceData = compressedVoiceData;
            voicePacket.size = compressedVoiceData.Count;
            voicePacket.internalVoiceTick = internalVoiceTick;
            voicePacket.networkId = pawn.networkId;
            NetworkManager.Instance.Client.Send<VoicePacket>(voicePacket, SendType.Reliable);
        }

        #endregion
    }
}
