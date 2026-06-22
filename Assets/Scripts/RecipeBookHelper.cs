// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using UnityEngine;


// public class RecipeBookHelper
// {

//     static RecipeBookHelper()
//     {
//         Init();
//     }

//     private static void Init()
//     {
//         RecipeBookCompact.TryRecipeResolver = RecipeBookProvider.TryResolve;
//         RecipeBook.TryRecipeResolver = RecipeBookProvider.TryResolve;
//     }


//     public static IRecipeNode GetNodeById(int id, int amount, ERecipeBookType type = ERecipeBookType.Normal)
//     {
//         switch (type)
//         {
//             case ERecipeBookType.Compact:
//                 return GetCompactNodeById(id, amount);
//             case ERecipeBookType.Normal:
//                 return GetNormalNodeById(id, amount);
//             default:
//                 return null;
//         }
//     }

//     public static (Dictionary<int, int> Need, Dictionary<int, int> UnusedOwned) CollectNeededWithUnusedOwned(IRecipeNode node, Dictionary<int, int> owned)
//     {
//         switch (node.RecipeBookType)
//         {
//             case ERecipeBookType.Compact:
//                 return CollectNeededWithUnusedOwned(node as RecipeCompactNode, owned);
//             case ERecipeBookType.Normal:
//                 return CollectNeededWithUnusedOwned(node as RecipeNode, owned);
//             default:
//                 return (null, null);
//         }
//     }

//     public static (Dictionary<int, int> Need, Dictionary<int, int> UnusedOwned) CollectNeededBaseWithUnusedOwned(IRecipeNode node, Dictionary<int, int> owned)
//     {
//         switch (node.RecipeBookType)
//         {
//             case ERecipeBookType.Compact:
//                 return CollectNeededBaseWithUnusedOwned(node as RecipeCompactNode, owned);
//             case ERecipeBookType.Normal:
//                 return CollectNeededBaseWithUnusedOwned(node as RecipeNode, owned);
//             default:
//                 return (null, null);
//         }
//     }

//     public static (Dictionary<int, int> Need, Dictionary<int, int> BaseNeed, Dictionary<int, int> UnusedOwned)
//         CollectNeededAndBaseWithUnusedOwned(IRecipeNode node, Dictionary<int, int> owned)
//     {
//         switch (node.RecipeBookType)
//         {
//             case ERecipeBookType.Compact:
//                 return CollectNeededAndBaseWithUnusedOwned(node as RecipeCompactNode, owned);
//             case ERecipeBookType.Normal:
//                 return CollectNeededAndBaseWithUnusedOwned(node as RecipeNode, owned);
//             default:
//                 return (null, null, null);
//         }
//     }

//     /// <summary>
//     /// Collect needed items under budget constraint (score + count + reward-pool whitelist).
//     /// Dispatches to the appropriate implementation based on <see cref="IRecipeNode.RecipeBookType"/>.
//     /// </summary>
//     public static (Dictionary<int, int> Need, Dictionary<int, int> UnusedOwned) CollectNeededWithBudget(
//         IRecipeNode node,
//         RecipeCollectBudget budget,
//         Dictionary<int, int> owned = null)
//     {
//         if (node == null || budget == null) return (null, null);

//         switch (node.RecipeBookType)
//         {
//             case ERecipeBookType.Compact:
//             {
//                 var compactNode = node as RecipeCompactNode;
//                 var need = RecipeBookCompact.CollectNeededWithBudget(compactNode, owned, budget, out var unusedOwned);
//                 return (need, unusedOwned);
//             }
//             case ERecipeBookType.Normal:
//             {
//                 var normalNode = node as RecipeNode;
//                 //var need = RecipeBook.CollectNeededWithBudget(normalNode, owned, budget, out var unusedOwned);
//                 var need = RecipeBookOptimizer.CollectNeededWithBudget(normalNode, owned, budget, out var unusedOwned);
//                 return (need, unusedOwned);
//             }
//             default:
//                 return (null, null);
//         }
//     }


//     #region RecipeBook

//     private static Dictionary<(int, int), RecipeNode> _cacheNormalNode = new();

//     private static RecipeNode GetNormalNodeById(int id, int amount = 1)
//     {
//         _cacheNormalNode.TryGetValue((id, amount), out var node);
//         if (node == null)
//         {
//             node = RecipeBook.BuildRecipeTree(id, amount);
//             _cacheNormalNode[(id, amount)] = node;
//         }

//         return node;
//     }

//     private static (Dictionary<int, int> Need, Dictionary<int, int> UnusedOwned)
//         CollectNeededWithUnusedOwned(RecipeNode node, Dictionary<int, int> owned)
//     {
//         if (node == null) return (null, null);
//         var need = RecipeBookOptimizer.CollectNeededItemsWithUnusedOwned(node, owned, out var unusedOwned);
//         return (need, unusedOwned);
//     }

//     private static (Dictionary<int, int> Need, Dictionary<int, int> UnusedOwned)
//         CollectNeededBaseWithUnusedOwned(RecipeNode node, Dictionary<int, int> owned)
//     {
//         if (node == null) return (null, null);
//         RecipeBook.MarkOwnedItems(node, owned, out var unusedOwned);
//         var need = RecipeBookOptimizer.CollectNeededBaseMaterials(node);
//         return (need, unusedOwned);
//     }

//     private static (Dictionary<int, int> Need, Dictionary<int, int> BaseNeed, Dictionary<int, int> UnusedOwned)
//         CollectNeededAndBaseWithUnusedOwned(RecipeNode node, Dictionary<int, int> owned)
//     {
//         if (node == null) return (null, null, null);
//         // MarkOwned once, then collect both needed and base-needed via Optimizer.
//         RecipeBook.MarkOwnedItems(node, owned, out var unusedOwned);
//         var need = RecipeBookOptimizer.CollectNeededItems(node);
//         var baseNeed = RecipeBookOptimizer.CollectNeededBaseMaterials(node);
//         return (need, baseNeed, unusedOwned);
//     }

//     private static void ReleaseNormalNodeTreeById(int id, int amount = 1)
//     {
//         // RecipeBook does not use a node pool; simply remove from cache.
//         _cacheNormalNode.Remove((id, amount));
//     }

//     #endregion
    
    

//     #region RecipeBookCompact


//     private static Dictionary<(int,int),RecipeCompactNode> _cacheCompactNode = new();
    
//     private static RecipeCompactNode GetCompactNodeById(int id,int amount =1)
//     {
//         _cacheCompactNode.TryGetValue((id,amount), out var node);
//         if (node == null)
//         {
//             var root = RecipeBookCompact.BuildTree(id);
//             _cacheCompactNode[(id,amount)] = root;
//         }
//         return node;
//     }
    
//     private static (Dictionary<int, int> Need,Dictionary<int, int>UnusedOwned) CollectNeededWithUnusedOwned(RecipeCompactNode node, Dictionary<int, int> owned)
//     {        
//         if (node != null)
//         {
//             var need = RecipeBookCompact.CollectNeededWithUnusedOwned(node, owned, out var unusedOwned);
//             return (need,unusedOwned);
//         }
//         return (null, null);
//     }

//     private static (Dictionary<int, int> Need,Dictionary<int, int>UnusedOwned) CollectNeededBaseWithUnusedOwned(RecipeCompactNode node, Dictionary<int, int> owned)
//     {        
//         if (node != null)
//         {
//             var need = RecipeBookCompact.CollectNeededBaseWithUnusedOwned(node, owned, out var unusedOwned);
//             return (need,unusedOwned);
//         }
//         return (null, null);
//     }

//     private static (Dictionary<int, int> Need, Dictionary<int, int> BaseNeed, Dictionary<int, int> UnusedOwned)
//         CollectNeededAndBaseWithUnusedOwned(RecipeCompactNode node, Dictionary<int, int> owned)
//     {
//         if (node != null)
//         {
//             return RecipeBookCompact.CollectNeededAndBaseWithUnusedOwned(node, owned);
//         }

//         return (null, null, null);
//     }

//     private static void ReleaseCompactNodeTree(RecipeCompactNode node)
//     {
//         if (node == null) return;
//         RecipeBookCompact.ReleaseTree(node);
//     }
    
//     private static void ReleaseCompactNodeTreeById(int id,int amount =1)
//     {
//         if (_cacheCompactNode.TryGetValue((id,amount), out var node))
//         {
//             ReleaseCompactNodeTree(node);
//             _cacheCompactNode.Remove((id,amount));
//         }
//     }
    
    
    

//     #endregion
    
    
    
//     #region 功能测试

//     public static (Dictionary<int, int> Needed, Dictionary<int, int> UnusedOwned)
//         GetNeedItemWithBudget_Debug(
//             int itemId,
//             RecipeCollectBudget budget,
//             Dictionary<int, int> owned,
//             int amount = 1,
//             ERecipeBookType type = ERecipeBookType.Normal,
//             bool isLog = false)
//     {
//         if (budget == null)
//         {
//             return (null, null);
//         }

//         IRecipeNode root = type switch
//         {
//             ERecipeBookType.Normal => RecipeBook.BuildRecipeTree(itemId, amount),
//             ERecipeBookType.Compact => RecipeBookCompact.BuildTree(itemId, amount),
//             _ => throw new NotImplementedException()
//         };

//         float beforeScore = budget.RemainingScore;
//         int beforeCount = budget.RemainingCount;

//         var result = CollectNeededWithBudget(root, budget, owned);

//         if (isLog)
//         {
//             LogInfo("<color=yellow>============ Run With Budget ============</color>");
//             if (type == ERecipeBookType.Normal)
//             {
//                 LogInfo(RecipeBook.PrintTree(root as RecipeNode));
//             }
//             else
//             {
//                 LogInfo(RecipeBookCompact.PrintTree(root as RecipeCompactNode));
//             }

//             LogInfo(FormatNeeded("Budget Needed", result.Need));
//             LogInfo(FormatNeeded("Budget Unused Owned", result.UnusedOwned));
//             LogInfo(string.Format(
//                 "Budget Score: {0} -> {1} (spent: {2})",
//                 beforeScore,
//                 budget.RemainingScore,
//                 beforeScore - budget.RemainingScore));
//             LogInfo(string.Format(
//                 "Budget Count: {0} -> {1} (spent: {2})",
//                 beforeCount,
//                 budget.RemainingCount,
//                 beforeCount - budget.RemainingCount));
//         }

//         return (result.Need, result.UnusedOwned);
//     }

//     /// <summary>
//     /// Compare budget-collection results between RecipeBook (baseline recursive)
//     /// and RecipeBookOptimizer (iterative) under the same Normal-tree input.
//     /// </summary>
//     public static void CompareNeedItemWithBudget_Normal_Debug(
//         int itemId,
//         RecipeCollectBudget budget,
//         Dictionary<int, int> owned,
//         int amount = 1,
//         bool isLog = true)
//     {
//         if (!isLog || budget == null)
//         {
//             return;
//         }

//         var baselineBudget = CloneBudget(budget);
//         var optimizerBudget = CloneBudget(budget);

//         var rootForBaseline = RecipeBook.BuildRecipeTree(itemId, amount);
//         var baselineNeed = RecipeBook.CollectNeededWithBudget(
//             rootForBaseline, owned, baselineBudget, out var baselineUnusedOwned);

//         var rootForOptimizer = RecipeBook.BuildRecipeTree(itemId, amount);
//         var optimizerNeed = RecipeBookOptimizer.CollectNeededWithBudget(
//             rootForOptimizer, owned, optimizerBudget, out var optimizerUnusedOwned);

//         LogInfo("<color=yellow>============ Compare Budget (Normal) ============</color>");
//         LogInfo(RecipeBook.PrintTree(rootForBaseline));

//         LogInfo("<color=cyan>[Baseline] RecipeBook</color>");
//         LogInfo(FormatNeeded("Needed", baselineNeed));
//         LogInfo(FormatNeeded("Unused Owned", baselineUnusedOwned));
//         LogInfo(string.Format("Budget Score Left: {0}", baselineBudget.RemainingScore));
//         LogInfo(string.Format("Budget Count Left: {0}", baselineBudget.RemainingCount));

//         LogInfo("<color=green>[Optimizer] RecipeBookOptimizer</color>");
//         LogInfo(FormatNeeded("Needed", optimizerNeed));
//         LogInfo(FormatNeeded("Unused Owned", optimizerUnusedOwned));
//         LogInfo(string.Format("Budget Score Left: {0}", optimizerBudget.RemainingScore));
//         LogInfo(string.Format("Budget Count Left: {0}", optimizerBudget.RemainingCount));

//         LogInfo("<color=magenta>[Diff] Optimizer - Baseline</color>");
//         LogInfo(FormatNeeded("Need Diff", BuildCountDiff(optimizerNeed, baselineNeed)));
//         LogInfo(FormatNeeded("UnusedOwned Diff", BuildCountDiff(optimizerUnusedOwned, baselineUnusedOwned)));
//         LogInfo(string.Format("Score Left Diff: {0}", optimizerBudget.RemainingScore - baselineBudget.RemainingScore));
//         LogInfo(string.Format("Count Left Diff: {0}", optimizerBudget.RemainingCount - baselineBudget.RemainingCount));
//     }

//     public static (Dictionary<int, int> Needed, Dictionary<int, int> UnusedOwned) GetNeedItem_Debug(int itemId, Dictionary<int, int> owned ,int amount =1,ERecipeBookType type = ERecipeBookType.Normal,bool isLog = false)
//     {
//         return type switch
//         {
//             ERecipeBookType.Normal => GetNeedItemInternal_Debug(itemId, owned,amount,isLog),
//             ERecipeBookType.Compact => GetNeedItemCompactInternal_Debug(itemId, owned, amount,isLog),
//             _ => throw new NotImplementedException()
//         };
//     }
    
//     private static (Dictionary<int, int>Needed, Dictionary<int, int>UnusedOwned)GetNeedItemInternal_Debug (int itemId,Dictionary<int, int> owned ,int amount =1,bool isLog = false)
//     {
//         var root = RecipeBook.BuildRecipeTree(itemId, amount);
//         var needed = RecipeBookOptimizer.CollectNeededItemsWithUnusedOwned(root, owned, out var unusedOwned);
//         if (isLog)
//         {
//             LogInfo("<color=red>============ Run With Unused Owned ============</color>");
//             LogInfo(RecipeBook.PrintTree(root));
//             LogInfo(FormatNeeded("Needed", needed));
//             LogInfo(FormatNeeded("Unused Owned", unusedOwned));
//         }
//         return (needed, unusedOwned);;
//     }
    
//     private static (Dictionary<int, int>Needed, Dictionary<int, int>UnusedOwned) GetNeedItemCompactInternal_Debug (int itemId,Dictionary<int, int> owned,int amount =1,bool isLog = false)
//     {
//         var root = RecipeBookCompact.BuildTree(itemId,amount);
//         var needed = RecipeBookCompact.CollectNeededWithUnusedOwned(root, owned, out var unusedOwned);
//         if (isLog)
//         {
//             LogInfo("<color=red>============ Run Compact With Unused Owned ============</color>");
//             LogInfo(RecipeBookCompact.PrintTree(root));
//             LogInfo(FormatNeeded("Compact Needed", needed));
//             LogInfo(FormatNeeded("Compact Unused Owned", unusedOwned));
//         }
//         return (needed, unusedOwned);
//     }

//     public static Dictionary<int, int> GetNeedBaseItem_Debug(int itemId, Dictionary<int, int> owned,int amount =1, ERecipeBookType type = ERecipeBookType.Normal,bool isLog = false)
//     {
//         return type switch
//         {
//             ERecipeBookType.Normal => GetNeedBaseItemInternal_Debug(itemId, owned, amount,isLog),
//             ERecipeBookType.Compact => GetNeedBaseItemCompactInternal_Debug(itemId, owned, amount,isLog),
//             _ => throw new NotImplementedException()
//         };
//     }
    
//     private static Dictionary<int, int> GetNeedBaseItemInternal_Debug(int itemId, Dictionary<int, int> owned,int amount =1, bool isLog = false)
//     {
//         var root = RecipeBook.BuildRecipeTree(itemId,amount,owned);
//         var baseNeeded = RecipeBookOptimizer.CollectNeededBaseMaterials(root);
//         if (isLog)
//         {
//             var treeStr = RecipeBook.PrintTree(root);
//             LogInfo(treeStr);
//             LogInfo(FormatNeeded("Needed Base Item", baseNeeded));
//         }
//         return baseNeeded;
//     }

//     private static Dictionary<int, int>GetNeedBaseItemCompactInternal_Debug (int itemId,Dictionary<int, int> owned,int amount =1,bool isLog = false)
//     {
//         var root = RecipeBookCompact.BuildTree(itemId,amount);
//         var baseNeeded = RecipeBookCompact.CollectNeededBaseWithUnusedOwned(root, owned, out _);
//         if (isLog)
//         {
//             var treeStr = RecipeBookCompact.PrintTree(root);
//             LogInfo(treeStr);
//             LogInfo(FormatNeeded("Compact Needed Base Item", baseNeeded));
//         }
//         return baseNeeded;
//     }
    
//     private static void LogInfo(string info)
//     {
//         Debug.LogError(info);
//     }

//     private static RecipeCollectBudget CloneBudget(RecipeCollectBudget source)
//     {
//         if (source == null)
//         {
//             return null;
//         }

//         return new RecipeCollectBudget(source.RemainingScore, source.RemainingCount, source.RewardPool);
//     }

//     private static Dictionary<int, int> BuildCountDiff(
//         Dictionary<int, int> left,
//         Dictionary<int, int> right)
//     {
//         var diff = new Dictionary<int, int>();

//         if (left != null)
//         {
//             foreach (var pair in left)
//             {
//                 diff[pair.Key] = pair.Value;
//             }
//         }

//         if (right != null)
//         {
//             foreach (var pair in right)
//             {
//                 int current;
//                 diff.TryGetValue(pair.Key, out current);
//                 int next = current - pair.Value;
//                 if (next == 0)
//                 {
//                     diff.Remove(pair.Key);
//                 }
//                 else
//                 {
//                     diff[pair.Key] = next;
//                 }
//             }
//         }

//         return diff;
//     }
    
    
//     private static string FormatNeeded(string title, Dictionary<int, int> needed)
//     {
//         if (needed == null || needed.Count == 0)
//         {
//             return title + ": (none)";
//         }

//         var sb = new StringBuilder();
//         sb.Append(title).Append(": ");

//         bool first = true;
//         foreach (KeyValuePair<int, int> pair in needed)
//         {
//             if (!first)
//             {
//                 sb.Append(", ");
//             }

//             sb.Append(pair.Key).Append('x').Append(pair.Value);
//             first = false;
//         }

//         return sb.ToString();
//     }
    

//     #endregion
// }
