using Godot;

namespace OverfortGames.SteamNetGodot
{
    public static class SimpleSaveSystem
    {
        public const string BAN_LIST_PATH = "user://ban_list.save";
        public const string MUTE_LIST_PATH = "user://mute_list.save";
        public const string MUTE_GLOBAL_LIST_PATH = "user://mute_global_list.save";
        public const string AUDIO_PATH = "user://audio.save";

        static SimpleSaveSystem()
        {
            Load(BAN_LIST_PATH, out var _);
            Load(MUTE_LIST_PATH, out var _);
            Load(MUTE_GLOBAL_LIST_PATH, out var _);
            Load(AUDIO_PATH, out var _);
        }

        public static void Save(Variant variant, string filePath)
        {
            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.Print($"Save {filePath}: {FileAccess.GetOpenError()}");
                return;
            }
            // Json provides a static method to serialized JSON string.
            var jsonString = Json.Stringify(variant);
            GD.Print($"Save JSON: {jsonString}");

            // Store the save dictionary as a new line in the save file.
            file.StoreString(jsonString);
            file.Close();
        }

        public static bool Load(string filePath, out Variant result)
        {
            var json = new Json();

            var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.Print($"Load {filePath}: {FileAccess.GetOpenError()}");

                if (FileAccess.GetOpenError() == Error.FileNotFound)
                {
                    Save("", filePath);
                }

                result = default;
                return false;
            }

            if (file.GetLength() == 0)
            {
                GD.Print($"Trying to load empty file at path {filePath}");
                result = default;
                file.StoreString("");
                file.Close();
                return true;
            }

            var jsonString = file.GetAsText();

            var parseResult = json.Parse(jsonString);

            if (parseResult != Error.Ok)
            {
                GD.PrintErr($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
                result = default;
                return false;
            }

            result = json.Data;

            file.Close();
            return true;
        }
    }
}