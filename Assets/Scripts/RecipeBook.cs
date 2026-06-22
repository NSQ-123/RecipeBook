using System.Collections.Generic;
using System.Text;
// using TLF;
using UnityEngine;

/// <summary>
/// Main entry for building recipe trees, marking ownership, and collecting missing items.
/// </summary>
public static class RecipeBook
{
    /// <summary>
    /// External recipe provider callback; returns true when recipe exists.
    /// </summary>
    public delegate bool TryResolveRecipeDelegate(int itemId, out RecipeDefinition recipe);

    public static TryResolveRecipeDelegate TryRecipeResolver;

    // Local cache for already-resolved recipes.
    private static readonly Dictionary<int, RecipeDefinition> s_recipes = new Dictionary<int, RecipeDefinition>();

    // Per-build state: subtree templates and current recursion path.
    private sealed class BuildContext
    {
        public readonly Dictionary<int, RecipeNode> TemplateCache = new Dictionary<int, RecipeNode>();
        public readonly HashSet<int> Path = new HashSet<int>();
    }

    /// <summary>
    /// Manually register/update a recipe into local cache.
    /// </summary>
    public static void RegisterRecipe(int itemId, RecipeDefinition recipe)
    {
        if (recipe == null)
        {
            throw new System.ArgumentNullException(nameof(recipe));
        }

        s_recipes[itemId] = recipe;
    }

    /// <summary>
    /// Clear local recipe cache.
    /// </summary>
    public static void ClearRecipes()
    {
        s_recipes.Clear();
    }

    /// <summary>
    /// Build a recipe tree for target item and quantity.
    /// </summary>
    public static RecipeNode BuildRecipeTree(int itemId, int amount = 1)
    {
        if (amount <= 0)
        {
            amount = 1;
        }

        var context = new BuildContext();
        return BuildNode(itemId, amount, context);
    }

    /// <summary>
    /// Mark owned nodes in tree using the given inventory map.
    /// </summary>
    public static void MarkOwnedItems(RecipeNode root, IDictionary<int, int> ownedItems)
    {
        if (root == null)
        {
            return;
        }

        Dictionary<int, int> remaining = CreateRemainingOwnedMap(ownedItems);

        MarkOwnedRecursive(root, remaining, false);
    }

    /// <summary>
    /// Mark ownership and return remaining items that were not consumed by this tree.
    /// </summary>
    public static void MarkOwnedItems(RecipeNode root, IDictionary<int, int> ownedItems,
        out Dictionary<int, int> unusedOwnedItems)
    {
        var remaining = CreateRemainingOwnedMap(ownedItems);

        if (root != null)
        {
            MarkOwnedRecursive(root, remaining, false);
        }

        unusedOwnedItems = FilterPositiveCounts(remaining);
    }

    /// <summary>
    /// Mark using owned items, collect needed items, and return unused owned items.
    /// </summary>
    public static Dictionary<int, int> CollectNeededItemsWithUnusedOwned(
        RecipeNode root,
        IDictionary<int, int> ownedItems,
        out Dictionary<int, int> unusedOwnedItems,
        bool ignoreProcessingTool = true)
    {
        MarkOwnedItems(root, ownedItems, out unusedOwnedItems);
        return CollectNeededItems(root, ignoreProcessingTool);
    }

    /// <summary>
    /// Build and mark in one call.
    /// </summary>
    public static RecipeNode BuildRecipeTree(int itemId, int amount, IDictionary<int, int> ownedItems)
    {
        RecipeNode root = BuildRecipeTree(itemId, amount);
        MarkOwnedItems(root, ownedItems);
        return root;
    }


    /// <summary>
    /// Print tree with ownership/base tags.
    /// </summary>
    public static string PrintTree(RecipeNode root)
    {
        if (root == null)
        {
            return "(null)";
        }

        var sb = new StringBuilder();
        PrintNode(root, string.Empty, true, true, sb);
        return sb.ToString();
    }

    /// <summary>
    /// Collect missing items using the current recipe rule.
    /// </summary>
    public static Dictionary<int, int> CollectNeededItems(RecipeNode root, bool ignoreProcessingTool = true)
    {
        var needed = new Dictionary<int, int>();
        if (root == null)
        {
            return needed;
        }

        var ownedInSubtree = new Dictionary<RecipeNode, bool>();
        BuildOwnedSubtreeMap(root, ownedInSubtree);
        CollectNeededRecursive(root, ownedInSubtree, needed, ignoreProcessingTool);
        return needed;
    }

    /// <summary>
    /// Collect missing items but only base materials.
    /// </summary>
    public static Dictionary<int, int> CollectNeededBaseMaterials(RecipeNode root, bool ignoreProcessingTool = true)
    {
        var needed = new Dictionary<int, int>();
        if (root == null)
        {
            return needed;
        }

        CollectNeededBaseRecursive(root, needed, ignoreProcessingTool);
        return needed;
    }

    /// <summary>
    /// Collect needed items under a score/count/reward-pool budget constraint.
    /// Items that cannot be collected at their current level (wrong pool, no score, insufficient budget)
    /// are drilled down to their child ingredients recursively.
    /// </summary>
    public static Dictionary<int, int> CollectNeededWithBudget(
        RecipeNode root,
        RecipeCollectBudget budget,
        bool ignoreProcessingTool = true)
    {
        var needed = new Dictionary<int, int>();
        if (root == null || budget == null || budget.IsExhausted)
        {
            return needed;
        }

        var ownedInSubtree = new Dictionary<RecipeNode, bool>();
        BuildOwnedSubtreeMap(root, ownedInSubtree);
        CollectNeededWithBudgetRecursive(root, ownedInSubtree, needed, ignoreProcessingTool, budget);
        return needed;
    }

    /// <summary>
    /// Mark ownership first, then collect needed items under budget constraint.
    /// </summary>
    public static Dictionary<int, int> CollectNeededWithBudget(
        RecipeNode root,
        IDictionary<int, int> ownedItems,
        RecipeCollectBudget budget,
        out Dictionary<int, int> unusedOwnedItems,
        bool ignoreProcessingTool = true)
    {
        MarkOwnedItems(root, ownedItems, out unusedOwnedItems);
        return CollectNeededWithBudget(root, budget, ignoreProcessingTool);
    }

    private static void PrintNode(RecipeNode node, string prefix, bool isLast, bool isRoot, StringBuilder sb)
    {
        if (!isRoot)
        {
            sb.Append(prefix)
                .Append(isLast ? "\\- " : "+- ");
        }

        sb.Append(node.Id)
            .Append(" x")
            .Append(node.NeededCount)
            .Append(" [score:")
            .Append(node.Score)
            .Append(']')
            .Append(node.IsOwnDirect ? " [own:self]" : "")
            .Append(!node.IsOwnDirect && node.IsOwn ? " [own:parent]" : "")
            .Append(node.IsBaseMaterial ? " [base]" : "")
            .AppendLine();

        if (node.Children == null)
        {
            return;
        }

        string childPrefix = isRoot ? string.Empty : prefix + (isLast ? "   " : "|  ");
        for (int i = 0; i < node.Children.Count; i++)
        {
            bool childIsLast = i == node.Children.Count - 1;
            PrintNode(node.Children[i], childPrefix, childIsLast, false, sb);
        }
    }

    private static RecipeNode BuildNode(int itemId, int amount, BuildContext context)
    {
        // Guard against bad config: recipe cycles are not allowed.
        if (context.Path.Contains(itemId))
        {
            throw new System.InvalidOperationException($"Recipe cycle detected at itemId {itemId}.");
        }

        if (amount == 1)
        {
            // Reuse prebuilt template for common single-amount subtrees.
            RecipeNode template;
            if (context.TemplateCache.TryGetValue(itemId, out template))
            {
                return CloneNode(template);
            }
        }

        RecipeDefinition def;
        if (!TryGetRecipeDefinition(itemId, out def))
        {
            throw new KeyNotFoundException(
                $"No recipe found for itemId {itemId}. Please assign RecipeBook.TryRecipeResolver or register the recipe externally.");
        }

        bool isBaseMaterial = def.IsBaseMaterial || def.Inputs.Count == 0;
        bool isMaterial = def.IsMaterial;
        bool isProcessingTool = def.IsProcessingTool;
        var score =def.Score;

        var node = new RecipeNode(itemId, isMaterial, isBaseMaterial, score, isProcessingTool);
        node.SetNeededCount(amount);

        if (def.Inputs.Count == 0)
        {
            if (amount == 1)
            {
                // Cache leaf template for future clones.
                context.TemplateCache[itemId] = CloneNode(node);
            }

            return node;
        }

        int childCount = 0;
        for (int i = 0; i < def.Inputs.Count; i++)
        {
            childCount += def.Inputs[i].Count * amount;
        }

        // Reduce list growth reallocations.
        node.PrepareChildrenCapacity(childCount);

        context.Path.Add(itemId);

        for (int i = 0; i < def.Inputs.Count; i++)
        {
            Ingredient ingredient = def.Inputs[i];
            int required = ingredient.Count * amount;

            // Expand by unit count to preserve existing ownership-marking behavior.
            for (int n = 0; n < required; n++)
            {
                node.AddChild(BuildNode(ingredient.ItemId, 1, context));
            }
        }

        context.Path.Remove(itemId);

        if (amount == 1)
        {
            // Cache non-leaf template as well.
            context.TemplateCache[itemId] = CloneNode(node);
        }

        return node;
    }

    private static RecipeNode CloneNode(RecipeNode source)
    {
        var clone = new RecipeNode(source.Id, source.IsMaterial, source.IsBaseMaterial, source.Score, source.IsProcessingTool);
        clone.SetNeededCount(source.NeededCount);

        if (source.Children == null || source.Children.Count == 0)
        {
            return clone;
        }

        clone.PrepareChildrenCapacity(source.Children.Count);
        for (int i = 0; i < source.Children.Count; i++)
        {
            clone.AddChild(CloneNode(source.Children[i]));
        }

        return clone;
    }

    private static bool TryGetRecipeDefinition(int itemId, out RecipeDefinition recipe)
    {
        if (s_recipes.TryGetValue(itemId, out recipe))
        {
            return true;
        }

        if (TryRecipeResolver == null)
        {
            return false;
        }

        if (!TryRecipeResolver(itemId, out recipe) || recipe == null)
        {
            recipe = null;
            return false;
        }

        // Cache resolved recipe to avoid repeated resolver calls.
        s_recipes[itemId] = recipe;
        return true;
    }

    private static void MarkOwnedRecursive(RecipeNode node, Dictionary<int, int> remaining, bool inheritedOwn)
    {
        if (inheritedOwn)
        {
            node.SetOwn(true);

            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                MarkOwnedRecursive(node.Children[i], remaining, true);
            }

            return;
        }

        int count;
        bool ownFromInventory = remaining.TryGetValue(node.Id, out count) && count > 0;
        node.SetOwn(ownFromInventory, ownFromInventory);

        if (ownFromInventory)
        {
            remaining[node.Id] = count - 1;
        }

        if (node.Children == null)
        {
            return;
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            MarkOwnedRecursive(node.Children[i], remaining, ownFromInventory);
        }
    }

    private static bool BuildOwnedSubtreeMap(RecipeNode node, Dictionary<RecipeNode, bool> map)
    {
        // Post-order style aggregation: any owned descendant marks subtree as owned.
        bool hasOwned = node.IsOwn;

        if (node.Children != null)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                hasOwned = BuildOwnedSubtreeMap(node.Children[i], map) || hasOwned;
            }
        }

        // Cache "does this subtree contain any owned node" for fast lookup in need collection.
        map[node] = hasOwned;
        return hasOwned;
    }

    private static void CollectNeededRecursive(
        RecipeNode node,
        Dictionary<RecipeNode, bool> ownedInSubtree,
        Dictionary<int, int> needed,
        bool ignoreProcessingTool)
    {
        if (ignoreProcessingTool && node.IsProcessingTool)
        {
            return;
        }

        if (node.IsOwn)
        {
            return;
        }

        // Shared rule keeps RecipeBook and RecipeBookOptimizer behavior aligned.
        NeedCollectAction action = RecipeNeedCollectionRule.Evaluate(node, ownedInSubtree[node]);
        if (action == NeedCollectAction.AddCurrent)
        {
            AddNeeded(needed, node.Id, node.NeededCount);
            return;
        }

        if (action == NeedCollectAction.Stop || node.Children == null)
        {
            return;
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            CollectNeededRecursive(node.Children[i], ownedInSubtree, needed, ignoreProcessingTool);
        }
    }

    private static void AddNeeded(Dictionary<int, int> needed, int itemId, int count)
    {
        int current;
        needed.TryGetValue(itemId, out current);
        needed[itemId] = current + count;
    }

    private static Dictionary<int, int> CreateRemainingOwnedMap(IDictionary<int, int> ownedItems)
    {
        return ownedItems == null
            ? new Dictionary<int, int>()
            : new Dictionary<int, int>(ownedItems);
    }

    private static Dictionary<int, int> FilterPositiveCounts(Dictionary<int, int> source)
    {
        var result = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> pair in source)
        {
            if (pair.Value > 0)
            {
                result[pair.Key] = pair.Value;
            }
        }

        return result;
    }

    private static void CollectNeededBaseRecursive(RecipeNode node, Dictionary<int, int> needed,
        bool ignoreProcessingTool)
    {
        if (ignoreProcessingTool && node.IsProcessingTool)
        {
            return;
        }

        if (node.IsOwn)
        {
            return;
        }

        NeedCollectAction action = RecipeNeedCollectionRule.EvaluateBase(node);
        if (action == NeedCollectAction.AddCurrent)
        {
            AddNeeded(needed, node.Id, node.NeededCount);
            return;
        }

        if (action == NeedCollectAction.Stop || node.Children == null)
        {
            return;
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            CollectNeededBaseRecursive(node.Children[i], needed, ignoreProcessingTool);
        }
    }

    // ── Budget-constrained collection ──────────────────────────────────────────

    private static void CollectNeededWithBudgetRecursive(
        RecipeNode node,
        Dictionary<RecipeNode, bool> ownedInSubtree,
        Dictionary<int, int> needed,
        bool ignoreProcessingTool,
        RecipeCollectBudget budget)
    {
        if (budget.IsExhausted) return;
        if (ignoreProcessingTool && node.IsProcessingTool) return;
        if (node.IsOwn) return;

        NeedCollectAction action = RecipeNeedCollectionRule.Evaluate(node, ownedInSubtree[node]);

        if (action == NeedCollectAction.AddCurrent)
        {
            // RecipeBook expands every unit as its own node (NeededCount == 1).
            int collected = budget.TryCollect(node.Id, node.Score, node.NeededCount);
            if (collected > 0)
            {
                AddNeeded(needed, node.Id, collected);
            }
            else if (node.Children != null)
            {
                // Can't collect at this level (not in pool / no score / can't afford) → drill children.
                TraverseChildrenWithBudgetPriority(node, ownedInSubtree, needed, ignoreProcessingTool, budget);
            }

            return;
        }

        if (action == NeedCollectAction.Stop || node.Children == null) return;

        TraverseChildrenWithBudgetPriority(node, ownedInSubtree, needed, ignoreProcessingTool, budget);
    }

    private static void TraverseChildrenWithBudgetPriority(
        RecipeNode node,
        Dictionary<RecipeNode, bool> ownedInSubtree,
        Dictionary<int, int> needed,
        bool ignoreProcessingTool,
        RecipeCollectBudget budget)
    {
        if (node.Children == null)
        {
            return;
        }

        // Keep original recipe/input order. Only inside same-item sibling runs,
        // process non-owned-influenced branches first.
        int index = 0;
        while (index < node.Children.Count)
        {
            int runEnd = index + 1;
            int runItemId = node.Children[index].Id;
            while (runEnd < node.Children.Count && node.Children[runEnd].Id == runItemId)
            {
                runEnd++;
            }

            TraverseChildRun(node.Children, index, runEnd, false, ownedInSubtree, needed, ignoreProcessingTool, budget);
            TraverseChildRun(node.Children, index, runEnd, true, ownedInSubtree, needed, ignoreProcessingTool, budget);

            index = runEnd;
        }
    }

    private static void TraverseChildRun(
        List<RecipeNode> children,
        int start,
        int end,
        bool targetOwned,
        Dictionary<RecipeNode, bool> ownedInSubtree,
        Dictionary<int, int> needed,
        bool ignoreProcessingTool,
        RecipeCollectBudget budget)
    {
        for (int i = start; i < end; i++)
        {
            if (budget.IsExhausted)
            {
                return;
            }

            RecipeNode child = children[i];
            bool childOwnedInfluenced;
            ownedInSubtree.TryGetValue(child, out childOwnedInfluenced);
            if (childOwnedInfluenced != targetOwned)
            {
                continue;
            }

            CollectNeededWithBudgetRecursive(child, ownedInSubtree, needed,
                ignoreProcessingTool, budget);
        }
    }
}