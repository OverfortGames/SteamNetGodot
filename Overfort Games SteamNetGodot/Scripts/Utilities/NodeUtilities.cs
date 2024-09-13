using Godot;
using System.Collections.Generic;

namespace OverfortGames.SteamNetGodot
{
    public static class NodeUtilities
    {
        public static T GetNodeOfType<T>(Node root) where T : Node
        {
            List<Node> unsolved = new List<Node>();
            unsolved.Add(root);
            while (unsolved.Count > 0)
            {
                if (unsolved[0].GetType() == typeof(T)) return unsolved[0] as T;
                if (unsolved[0].GetType().IsSubclassOf(typeof(T))) return unsolved[0] as T;
                if (unsolved[0].GetChildCount() > 0)
                {
                    foreach (Node n in unsolved[0].GetChildren(false))
                    {
                        unsolved.Add(n);
                    }
                }
                unsolved.RemoveAt(0);
            }
            return null;
        }


        public static T[] GetNodesOfType<T>(Node root) where T : Node
        {
            List<T> result = new List<T>();
            List<Node> unsolved = new List<Node>();
            unsolved.Add(root);

            while (unsolved.Count > 0)
            {
                Node currentNode = unsolved[0];

                // Check if the current node is of the desired type or a subclass of it
                if (currentNode is T currentTypedNode)
                {
                    result.Add(currentTypedNode);
                }

                // Add the children of the current node to the unsolved list
                if (currentNode.GetChildCount() > 0)
                {
                    foreach (Node child in currentNode.GetChildren(false))
                    {
                        unsolved.Add(child);
                    }
                }

                // Remove the processed node from the list
                unsolved.RemoveAt(0);
            }

            // Return the array of found nodes
            return result.ToArray();
        }

        public static bool IsValid<T>(this T node) where T : GodotObject
        {
            return node != null
                && GodotObject.IsInstanceValid(node)
                && !node.IsQueuedForDeletion();
        }
    }
}