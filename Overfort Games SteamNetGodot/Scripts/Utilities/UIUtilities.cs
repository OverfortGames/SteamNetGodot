using Godot;

namespace OverfortGames.SteamNetGodot
{
    public static class UIUtilities
    {
        public static void ResetButtonPressedCallbacks(Button button)
        {
            // Get a list of all the methods connected to the "pressed" signal
            var connectedMethods = button.GetSignalConnectionList(Button.SignalName.Pressed);

            foreach (var method in connectedMethods)
            {
                Callable callable = (Callable)method["callable"];
                // Disconnect each method
                button.Disconnect(Button.SignalName.Pressed, callable);
            }
        }

        public static void ResetButtonToggledCallbacks(Button button)
        {
            // Get a list of all the methods connected to the "pressed" signal
            var connectedMethods = button.GetSignalConnectionList(Button.SignalName.Toggled);

            foreach (var method in connectedMethods)
            {
                Callable callable = (Callable)method["callable"];
                // Disconnect each method
                button.Disconnect(Button.SignalName.Toggled, callable);
            }
        }
    }
}