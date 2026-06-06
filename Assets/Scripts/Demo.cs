using System.Collections.Generic;
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
            { 1003, 1 }
        };
        

        [ContextMenu("Run")]
        public void Run()
        {
            var root = RecipeBook.BuildRecipeTree(ItemId,1,ownedItems);
            var treeStr = RecipeBook.PrintTree(root);
            Debug.Log(treeStr);
        }
    }
}