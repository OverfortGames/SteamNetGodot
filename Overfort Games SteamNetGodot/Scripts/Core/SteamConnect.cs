using Steamworks;
using System;
using Godot;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class SteamConnect : Node
    {
        public enum SteamConnectionStatus
        {
            Ok,
            Closed,
            NotConnected
        }

        public static event Action<SteamConnectionStatus> OnCheckSteamConnectionStatus = delegate { };
        public static SteamConnectionStatus CurrentSteamConnectionStatus { get; private set; } = SteamConnectionStatus.Closed;

        public override void _Ready()
        {
            TryConnectToSteam(1000);

            CheckConnection(1000);
        }

        private async void CheckConnection(int checkIntervalMilliseconds)
        {
            while (true)
            {
                await Task.Delay(checkIntervalMilliseconds);

                if (SteamClient.IsValid == false)
                {
                    CurrentSteamConnectionStatus = SteamConnectionStatus.Closed;
                }
                else
                    if (SteamClient.IsLoggedOn == false)
                {
                    CurrentSteamConnectionStatus = SteamConnectionStatus.NotConnected;
                }
                else
                    CurrentSteamConnectionStatus = SteamConnectionStatus.Ok;

                OnCheckSteamConnectionStatus(CurrentSteamConnectionStatus);
            }
        }

        public async void TryConnectToSteam(int retryIntervalMilliseconds)
        {
            while (SteamClient.IsValid == false)
            {
                try
                {
                    Steamworks.SteamClient.Init(480);
                    GD.Print("Steam Client Successfully Initialized " + Steamworks.SteamClient.SteamId);
                }
                catch (System.Exception e)
                {
                    GD.Print(e.Message);
                }

                await Task.Delay(retryIntervalMilliseconds);
            }

        }
    }
}