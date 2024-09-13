using Godot;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public static class GodotCollectionsUtilities
    {
        public static Dictionary<TKey, TValue> GetDictionary<[MustBeVariant] TKey, [MustBeVariant] TValue>(Godot.Collections.Dictionary<TKey, TValue> from)
        {
            var result = new Dictionary<TKey, TValue>();

            foreach (var pair in from)
            {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static Godot.Collections.Dictionary<TKey, TValue> GetGodotDictionary<[MustBeVariant] TKey, [MustBeVariant] TValue>(Dictionary<TKey, TValue> from)
        {
            var result = new Godot.Collections.Dictionary<TKey, TValue>();

            foreach (var pair in from)
            {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static Godot.Collections.Array<T> GetGodotArray<[MustBeVariant] T>(List<T> from)
        {
            var result = new Godot.Collections.Array<T>();

            foreach (var value in from)
            {
                result.Add(value);
            }

            return result;
        }

        public static List<T> GetList<[MustBeVariant] T>(Godot.Collections.Array<T> from)
        {
            var result = new List<T>();

            foreach (var value in from)
            {
                result.Add(value);
            }

            return result;
        }
    }
}