using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game
{
    public class Demo : MonoBehaviour
    {
        public int ItemId = 3004;
        
        Dictionary<int, int> ownedItems = new Dictionary<int, int>
        {
            { 1001, 1 },
            { 1002, 1 },
            { 1003, 1 },
            { 3003, 1 },
            { 3002, 1 },
            { 2002, 1 },
        };

        private void Awake()
        {
            
        }
        

        [ContextMenu("Run")]
        public void Run()
        {
            //RecipeBook.ClearRecipes();
            RecipeBook.RecipeResolver = RecipeBookTestProvider.Resolve;
            
            
            var root = RecipeBook.BuildRecipeTree(ItemId,1,ownedItems);
            var treeStr = RecipeBook.PrintTree(root);
            var needed = RecipeBook.CollectNeededItems(root);
            var baseNeeded = RecipeBook.CollectNeededBaseMaterials(root);
            Debug.Log(treeStr);
            Debug.Log(FormatNeeded("Needed", needed));
            Debug.Log(FormatNeeded("Base Needed", baseNeeded));
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
    }
}