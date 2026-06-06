using UnityEngine;

namespace Game
{
    public class Demo : MonoBehaviour
    {
        public int ItemId = 3004;

        [ContextMenu("Run")]
        public void Run()
        {
            var root = RecipeBook.BuildRecipeTree(ItemId);
            var treeStr = RecipeBook.PrintTree(root);
            Debug.Log(treeStr);
        }
    }
}