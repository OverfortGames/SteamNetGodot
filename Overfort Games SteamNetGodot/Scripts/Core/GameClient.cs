using Steamworks;
using Steamworks.Data;

namespace OverfortGames.SteamNetGodot
{
    public class GameClient : BaseClient
    {
        public GameState gameState;

        public override void OnConnectionChanged(ConnectionInfo info)
        {
            base.OnConnectionChanged(info);

            if (info.State == ConnectionState.Connected)
            {
                gameState = new GameState(NetworkManager.Instance.IsServer());
            }

            if(info.State == ConnectionState.None || info.State == ConnectionState.ClosedByPeer || info.State == ConnectionState.Dead || info.State == ConnectionState.ProblemDetectedLocally)
            {
                gameState = null;

                if(info.State == ConnectionState.None)
                    UINotifications.Instance.PushNotification("Disconnected", UINotifications.NotificationType.Error);

                if (info.State == ConnectionState.ClosedByPeer)
                    UINotifications.Instance.PushNotification("You have been kicked", UINotifications.NotificationType.Error);
                
                if (info.State == ConnectionState.Dead)
                    UINotifications.Instance.PushNotification("Server died", UINotifications.NotificationType.Error);

                if (info.State == ConnectionState.ProblemDetectedLocally)
                    UINotifications.Instance.PushNotification("ProblemDetectedLocally", UINotifications.NotificationType.Error);


                NetworkManager.Instance.ClientDisconnect();
            }
        }
    }
}
