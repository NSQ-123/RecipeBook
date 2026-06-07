using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game
{
    public class RecipeBookCompactDemo : MonoBehaviour
    {
        
        public int ItemId = 3004;
        [ContextMenu("Run Compact Demo")]
        public void RunCompactDemo()
        {
            RecipeBookCompact.TryRecipeResolver = RecipeBookTestProvider.TryResolve;

            var owned = new Dictionary<int, int>
            {
                { 1001, 1 },
                { 1002, 1 },
                { 1003, 1 },
                { 3003, 1 },
                { 3002, 1 },
                { 2002, 1 }
            };

            var root = RecipeBookCompact.BuildTree(ItemId);
            RecipeBookCompact.MarkOwned(root, owned);

            var needed = RecipeBookCompact.CollectNeeded(root);
            var baseNeeded = RecipeBookCompact.CollectNeededBase(root);

            Debug.Log(RecipeBookCompact.PrintTree(root));
            Debug.Log(FormatNeeded("Compact Needed", needed));
            Debug.Log(FormatNeeded("Compact Base Needed", baseNeeded));

            //RunCompactRegression();
        }

        [ContextMenu("Run Compact Regression")]
        public void RunCompactRegression()
        {
            RecipeBookCompact.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
            RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;

            var ownedA = new Dictionary<int, int>
            {
                { 1001, 1 },
                { 1002, 1 },
                { 1003, 1 }
            };

            var compactRootA = RecipeBookCompact.BuildTree(1005);
            RecipeBookCompact.MarkOwned(compactRootA, ownedA);

            Dictionary<int, int> compactNeededA = RecipeBookCompact.CollectNeeded(compactRootA);
            Dictionary<int, int> compactBaseA = RecipeBookCompact.CollectNeededBase(compactRootA);

            bool passNeededA = compactNeededA.Count == 2 &&
                               compactNeededA.ContainsKey(1004) && compactNeededA[1004] == 1 &&
                               compactNeededA.ContainsKey(1001) && compactNeededA[1001] == 1;

            bool passBaseA = compactBaseA.Count == 1 && compactBaseA.ContainsKey(1001) && compactBaseA[1001] == 9;

            Debug.Log("Compact Regression 1005 Needed: " + (passNeededA ? "PASS" : "FAIL") + " -> " + FormatNeeded("Needed", compactNeededA));
            Debug.Log("Compact Regression 1005 Base: " + (passBaseA ? "PASS" : "FAIL") + " -> " + FormatNeeded("Base Needed", compactBaseA));

            var ownedB = new Dictionary<int, int>
            {
                { 1001, 1 },
                { 1002, 1 },
                { 1003, 1 },
                { 3003, 1 },
                { 3002, 1 },
                { 2002, 1 }
            };

            RecipeNode legacyRoot = RecipeBook.BuildRecipeTree(3004, 1, ownedB);
            Dictionary<int, int> legacyNeeded = RecipeBook.CollectNeededItems(legacyRoot);
            Dictionary<int, int> legacyBase = RecipeBook.CollectNeededBaseMaterials(legacyRoot);

            var compactRootB = RecipeBookCompact.BuildTree(3004);
            RecipeBookCompact.MarkOwned(compactRootB, ownedB);
            Dictionary<int, int> compactNeededB = RecipeBookCompact.CollectNeeded(compactRootB);
            Dictionary<int, int> compactBaseB = RecipeBookCompact.CollectNeededBase(compactRootB);

            bool passNeededB = DictionaryEquals(legacyNeeded, compactNeededB);
            bool passBaseB = DictionaryEquals(legacyBase, compactBaseB);

            Debug.Log("Compact Compare 3004 Needed: " + (passNeededB ? "PASS" : "FAIL") + " -> " + FormatNeeded("Legacy", legacyNeeded) + " | " + FormatNeeded("Compact", compactNeededB));
            Debug.Log("Compact Compare 3004 Base: " + (passBaseB ? "PASS" : "FAIL") + " -> " + FormatNeeded("Legacy", legacyBase) + " | " + FormatNeeded("Compact", compactBaseB));
        }

        private static string FormatNeeded(string title, Dictionary<int, int> needed)
        {
            if (needed == null || needed.Count == 0)
            {
                return title + ": (none)";
            }

            var sb = new StringBuilder();
            sb.Append(title).Append(": ");

            bool first = true;
            foreach (KeyValuePair<int, int> pair in needed)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(pair.Key).Append('x').Append(pair.Value);
                first = false;
            }

            return sb.ToString();
        }

        private static bool DictionaryEquals(Dictionary<int, int> a, Dictionary<int, int> b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Count != b.Count)
            {
                return false;
            }

            foreach (KeyValuePair<int, int> pair in a)
            {
                int value;
                if (!b.TryGetValue(pair.Key, out value) || value != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

