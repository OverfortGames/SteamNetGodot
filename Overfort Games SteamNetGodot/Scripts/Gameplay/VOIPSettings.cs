using Steamworks;
using Godot;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public static class VOIPSettings
    {
        private static List<ulong> muteList = new List<ulong>();
        private static List<ulong> muteGlobalList = new List<ulong>();

        static VOIPSettings()
        {

            if (SimpleSaveSystem.Load(SimpleSaveSystem.MUTE_LIST_PATH, out var muteListAsVariant))
            {
                muteList = GodotCollectionsUtilities.GetList(muteListAsVariant.AsGodotArray<ulong>());
            }

            if (SimpleSaveSystem.Load(SimpleSaveSystem.MUTE_GLOBAL_LIST_PATH, out var muteGlobalListAsVariant))
            {
                muteGlobalList = GodotCollectionsUtilities.GetList(muteGlobalListAsVariant.AsGodotArray<ulong>());
            }
        }

        public static void Mute(SteamId steamId)
        {
            if (muteList.Contains(steamId))
                return;

            muteList.Add(steamId.Value);

            SimpleSaveSystem.Save(GodotCollectionsUtilities.GetGodotArray(muteList), SimpleSaveSystem.MUTE_LIST_PATH);
        }

        public static void Unmute(SteamId steamId)
        {
            if (muteList.Contains(steamId) == false)
                return;

            muteList.Remove(steamId.Value);
            SimpleSaveSystem.Save(GodotCollectionsUtilities.GetGodotArray(muteList), SimpleSaveSystem.MUTE_LIST_PATH);
        }

        public static void MuteGlobal(SteamId steamId)
        {
            if (muteGlobalList.Contains(steamId))
                return;

            muteGlobalList.Add(steamId.Value);
            SimpleSaveSystem.Save(GodotCollectionsUtilities.GetGodotArray(muteGlobalList), SimpleSaveSystem.MUTE_GLOBAL_LIST_PATH);
        }

        public static void UnmuteGlobal(SteamId steamId)
        {
            if (muteGlobalList.Contains(steamId) == false)
                return;

            muteGlobalList.Remove(steamId.Value);
            SimpleSaveSystem.Save(GodotCollectionsUtilities.GetGodotArray(muteGlobalList), SimpleSaveSystem.MUTE_GLOBAL_LIST_PATH);
        }

        public static bool IsMuted(SteamId steamId)
        {
            return muteList.Contains(steamId);
        }

        public static bool IsMutedGlobal(SteamId steamId)
        {
            return muteGlobalList.Contains(steamId);
        }
    }
}