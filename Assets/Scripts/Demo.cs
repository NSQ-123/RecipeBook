// using System.Collections.Generic;
// using System.Text;
// using UnityEngine;

// namespace Game
// {
//     public class Demo : MonoBehaviour
//     {
//         public int ItemId = 500058;
        
//         Dictionary<int, int> ownedItems = new Dictionary<int, int>
//         {
//             { 200001, 1 },
//             { 201501, 17 },
//             { 201504, 1 },
//         };

//         private void Awake()
//         {
//             RecipeBook.ClearRecipes();
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//         }
        

//         [ContextMenu("Run")]
//         public void Run()
//         {
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBook.BuildRecipeTree(ItemId,1,ownedItems);
//             var treeStr = RecipeBook.PrintTree(root);
//             var needed = RecipeBook.CollectNeededItems(root);
//             var baseNeeded = RecipeBook.CollectNeededBaseMaterials(root);
//             Debug.Log("<color=red>============ Run Demo ============</color>");
//             Debug.Log(treeStr);
//             Debug.Log(FormatNeeded("Needed", needed));
//             Debug.Log(FormatNeeded("Base Needed", baseNeeded));
//         }

//         [ContextMenu("Run With Unused Owned")]
//         public void RunWithUnusedOwned()
//         {
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBook.BuildRecipeTree(ItemId, 1);
//             var needed = RecipeBook.CollectNeededItemsWithUnusedOwned(root, ownedItems, out var unusedOwned);
//             Debug.Log("<color=red>============ Run With Unused Owned Demo ============</color>");
//             Debug.Log(RecipeBook.PrintTree(root));
//             Debug.Log(FormatNeeded("Needed", needed));
//             Debug.Log(FormatNeeded("Unused Owned", unusedOwned));
//         }

//         [ContextMenu("Run Optimizer")]
//         public void RunOptimizer()
//         {
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBook.BuildRecipeTree(ItemId,1,ownedItems);
//             var treeStr = RecipeBook.PrintTree(root);
//             var needed = RecipeBookOptimizer.CollectNeededItems(root);
//             var baseNeeded = RecipeBookOptimizer.CollectNeededBaseMaterials(root);
//             Debug.Log("<color=red>============ Run Optimizer Demo ============</color>");
//             Debug.Log(treeStr);
//             Debug.Log(FormatNeeded("Needed", needed));
//             Debug.Log(FormatNeeded("Base Needed", baseNeeded));
//         }

//         [ContextMenu("Run Optimizer With Unused Owned")]
//         public void RunOptimizerWithUnusedOwned()
//         {
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBook.BuildRecipeTree(ItemId, 1);
//             var needed = RecipeBookOptimizer.CollectNeededItemsWithUnusedOwned(root, ownedItems, out var unusedOwned);
//             Debug.Log("<color=red>============ Run Optimizer With Unused Owned Demo ============</color>");
//             Debug.Log(RecipeBook.PrintTree(root));
//             Debug.Log(FormatNeeded("Optimizer Needed", needed));
//             Debug.Log(FormatNeeded("Optimizer Unused Owned", unusedOwned));
//         }
        
//         [ContextMenu("Run Compact")]
//         public void RunCompact()
//         {
//             RecipeBookCompact.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBookCompact.BuildTree(ItemId);
//             RecipeBookCompact.MarkOwned(root, ownedItems);

//             var needed = RecipeBookCompact.CollectNeeded(root);
//             var baseNeeded = RecipeBookCompact.CollectNeededBase(root);
//             Debug.Log("<color=red>============ Run Compact Demo ============</color>");
//             Debug.Log(RecipeBookCompact.PrintTree(root));
//             Debug.Log(FormatNeeded("Compact Needed", needed));
//             Debug.Log(FormatNeeded("Compact Base Needed", baseNeeded));

//             //RunCompactRegression();
//         }

//         [ContextMenu("Run Compact With Unused Owned")]
//         public void RunCompactWithUnusedOwned()
//         {
//             RecipeBookCompact.TryRecipeResolver = RecipeBookTestProvider.TryResolve;
//             var root = RecipeBookCompact.BuildTree(ItemId);
//             var needed = RecipeBookCompact.CollectNeededWithUnusedOwned(root, ownedItems, out var unusedOwned);
//             Debug.Log("<color=red>============ Run Compact With Unused Owned Demo ============</color>");
//             Debug.Log(RecipeBookCompact.PrintTree(root));
//             Debug.Log(FormatNeeded("Compact Needed", needed));
//             Debug.Log(FormatNeeded("Compact Unused Owned", unusedOwned));
//         }
        
        
        
        
        
//         [ContextMenu("Run Regression")]
//         public void RunRegression()
//         {
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;

//             // Scenario A: expected answers from requirement comment.
//             var ownedA = new Dictionary<int, int>
//             {
//                 { 1001, 1 },
//                 { 1002, 1 },
//                 { 1003, 1 }
//             };

//             RecipeNode rootA = RecipeBook.BuildRecipeTree(1005, 1, ownedA);
//             Dictionary<int, int> neededA = RecipeBook.CollectNeededItems(rootA);
//             Dictionary<int, int> baseA = RecipeBook.CollectNeededBaseMaterials(rootA);

//             bool passNeededA = neededA.Count == 2 &&
//                                neededA.ContainsKey(1004) && neededA[1004] == 1 &&
//                                neededA.ContainsKey(1001) && neededA[1001] == 1;

//             bool passBaseA = baseA.Count == 1 && baseA.ContainsKey(1001) && baseA[1001] == 9;

//             Debug.Log("Regression 1005 Needed: " + (passNeededA ? "PASS" : "FAIL") + " -> " + FormatNeeded("Needed", neededA));
//             Debug.Log("Regression 1005 Base: " + (passBaseA ? "PASS" : "FAIL") + " -> " + FormatNeeded("Base Needed", baseA));

//             // Scenario B: compare RecipeBook and optimizer outputs for 3004.
//             var ownedB = new Dictionary<int, int>
//             {
//                 { 1001, 1 },
//                 { 1002, 1 },
//                 { 1003, 1 },
//                 { 3003, 1 },
//                 { 3002, 1 },
//                 { 2002, 1 }
//             };

//             RecipeNode rootB = RecipeBook.BuildRecipeTree(3004, 1, ownedB);
//             Dictionary<int, int> neededBookB = RecipeBook.CollectNeededItems(rootB);
//             Dictionary<int, int> baseBookB = RecipeBook.CollectNeededBaseMaterials(rootB);
//             Dictionary<int, int> neededOptB = RecipeBookOptimizer.CollectNeededItems(rootB);
//             Dictionary<int, int> baseOptB = RecipeBookOptimizer.CollectNeededBaseMaterials(rootB);

//             bool passNeededB = DictionaryEquals(neededBookB, neededOptB);
//             bool passBaseB = DictionaryEquals(baseBookB, baseOptB);

//             Debug.Log("Regression 3004 Needed compare: " + (passNeededB ? "PASS" : "FAIL") + " -> " + FormatNeeded("Book", neededBookB) + " | " + FormatNeeded("Opt", neededOptB));
//             Debug.Log("Regression 3004 Base compare: " + (passBaseB ? "PASS" : "FAIL") + " -> " + FormatNeeded("Book", baseBookB) + " | " + FormatNeeded("Opt", baseOptB));
//         }

//         private static string FormatNeeded(string title, Dictionary<int, int> needed)
//         {
//             if (needed == null || needed.Count == 0)
//             {
//                 return title + ": (none)";
//             }

//             var sb = new StringBuilder();
//             sb.Append(title).Append(": ");

//             bool first = true;
//             foreach (KeyValuePair<int, int> pair in needed)
//             {
//                 if (!first)
//                 {
//                     sb.Append(", ");
//                 }

//                 sb.Append(pair.Key).Append('x').Append(pair.Value);
//                 first = false;
//             }

//             return sb.ToString();
//         }

//         private static bool DictionaryEquals(Dictionary<int, int> a, Dictionary<int, int> b)
//         {
//             if (ReferenceEquals(a, b))
//             {
//                 return true;
//             }

//             if (a == null || b == null || a.Count != b.Count)
//             {
//                 return false;
//             }

//             foreach (KeyValuePair<int, int> pair in a)
//             {
//                 int value;
//                 if (!b.TryGetValue(pair.Key, out value) || value != pair.Value)
//                 {
//                     return false;
//                 }
//             }

//             return true;
//         }
//     }
// }