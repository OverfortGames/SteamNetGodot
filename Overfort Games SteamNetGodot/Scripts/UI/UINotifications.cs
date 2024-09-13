using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class UINotifications : Node
    {
        public enum NotificationType
        {
            Error,
            Normal
        }

        public static UINotifications Instance { get; private set; }

        private List<Button> notificationsQueue = new List<Button>();

        private Vector2? firstPosition;
        private float popCooldown = 2;

        public override void _Ready()
        {
            Instance = this;
        }

        public void PushNotification(string content, NotificationType type = NotificationType.Normal, float duration = 3)
        {
            Button notificationPopup = SimpleResourceLoader.Instance.LoadResourceAndInstantiate(ResourceId.NotificationPopup, this) as Button;
            notificationPopup.Text = content;
            notificationPopup.Pressed += NotificationPopup_Pressed;

            switch (type)
            {
                case NotificationType.Error:
                    ChangeButtonTextColor(notificationPopup, new Color(1, 0, 0));
                    break;
                case NotificationType.Normal:
                    ChangeButtonTextColor(notificationPopup, new Color(1, 1, 0));
                    break;
                default:
                    break;
            }

            if (notificationsQueue.Count > 0)
            {
                var lastNotication = notificationsQueue[notificationsQueue.Count - 1];
                notificationPopup.Position = lastNotication.Position + Vector2.Down * lastNotication.Size.Y + Vector2.Down * 15;
            }

            notificationsQueue.Add(notificationPopup);

            if (firstPosition == null)
                firstPosition = notificationPopup.Position;

            Pop(notificationPopup, duration);
        }

        private void NotificationPopup_Pressed()
        {
            Pop();
        }

        private void Pop()
        {
            if (notificationsQueue.Count == 0)
            {
                return;
            }

            notificationsQueue[0].QueueFree();
            notificationsQueue.RemoveAt(0);

            ReorderPositions();
        }

        private async void Pop(Button button, float cooldown)
        {
            await Task.Delay((int)(cooldown * 1000));

            if (button == null || notificationsQueue.Contains(button) == false)
            {
                return;
            }

            int index = notificationsQueue.IndexOf(button);
            notificationsQueue[index].QueueFree();
            notificationsQueue.RemoveAt(index);

            ReorderPositions();
        }

        private void ReorderPositions()
        {
            for (int i = 0; i < notificationsQueue.Count; i++)
            {
                var element = notificationsQueue[i];
                if (i > 0)
                {
                    var lastNotication = notificationsQueue[i - 1];
                    element.Position = lastNotication.Position + Vector2.Down * lastNotication.Size.Y + Vector2.Down * 15;
                }
                else
                {
                    element.Position = firstPosition.Value;
                }
            }
        }

        private void ChangeButtonTextColor(Button button, Color color)
        {
            button.AddThemeColorOverride("font_color", color);
        }
    }
}