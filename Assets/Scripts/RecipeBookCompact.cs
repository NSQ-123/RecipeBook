using System.Collections.Generic;
using System.Text;

// Compact tree: each ingredient appears once per recipe edge with NeededCount aggregation.
    // This keeps the existing RecipeBook untouched while testing the single-node+count model.
    public static class RecipeBookCompact
    {
        private static readonly Stack<RecipeCompactNode> s_nodePool = new Stack<RecipeCompactNode>(256);
        private static readonly Dictionary<int, RecipeDefinition> s_recipes = new Dictionary<int, RecipeDefinition>();

        /// <summary>
        /// External recipe provider callback used only by RecipeBookCompact.
        /// </summary>
        public delegate bool TryResolveRecipeDelegate(int itemId, out RecipeDefinition recipe);

        public static TryResolveRecipeDelegate TryRecipeResolver;

        private sealed class BuildContext
        {
            public readonly Dictionary<int, RecipeCompactNode> TemplateCache = new Dictionary<int, RecipeCompactNode>();
            public readonly HashSet<int> Path = new HashSet<int>();
        }

        public static RecipeCompactNode BuildTree(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            var context = new BuildContext();
            return BuildNode(itemId, amount, context);
        }

        // Release a compact tree back to pool after use.
        public static void ReleaseTree(RecipeCompactNode root)
        {
            if (root == null)
            {
                return;
            }

            ReturnNodeRecursive(root);
        }

        /// <summary>
        /// Manually register/update a recipe into RecipeBookCompact local cache.
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
        /// Clear RecipeBookCompact local recipe cache.
        /// </summary>
        public static void ClearRecipes()
        {
            s_recipes.Clear();
        }

        public static void MarkOwned(RecipeCompactNode root, IDictionary<int, int> owned)
        {
            Dictionary<int, int> remaining = CreateRemainingOwnedMap(owned);

            MarkOwnedRecursive(root, remaining, 0);
        }

        /// <summary>
        /// Mark ownership and return remaining items that were not consumed by this compact tree.
        /// </summary>
        public static void MarkOwned(RecipeCompactNode root, IDictionary<int, int> owned, out Dictionary<int, int> unusedOwned)
        {
            Dictionary<int, int> remaining = CreateRemainingOwnedMap(owned);

            if (root != null)
            {
                MarkOwnedRecursive(root, remaining, 0);
            }

            unusedOwned = FilterPositiveCounts(remaining);
        }

        /// <summary>
        /// Mark using owned items, collect needed items, and return unused owned items.
        /// </summary>
        public static Dictionary<int, int> CollectNeededWithUnusedOwned(
            RecipeCompactNode root,
            IDictionary<int, int> owned,
            out Dictionary<int, int> unusedOwned,
            bool ignoreProcessingTool = true)
        {
            MarkOwned(root, owned, out unusedOwned);
            return CollectNeeded(root, ignoreProcessingTool);
        }

        public static Dictionary<int, int> CollectNeededBaseWithUnusedOwned(
            RecipeCompactNode root,
            IDictionary<int, int> owned,
            out Dictionary<int, int> unusedOwned,
            bool ignoreProcessingTool = true)
        {
            MarkOwned(root, owned, out unusedOwned);
            return CollectNeededBase(root, ignoreProcessingTool);
        }

        public static (Dictionary<int, int> Needed, Dictionary<int, int> BaseNeeded, Dictionary<int, int> UnusedOwned)
            CollectNeededAndBaseWithUnusedOwned(
                RecipeCompactNode root,
                IDictionary<int, int> owned,
                bool ignoreProcessingTool = true)
        {
            MarkOwned(root, owned, out var unusedOwned);
            var needed = CollectNeeded(root, ignoreProcessingTool);
            var baseNeeded = CollectNeededBase(root, ignoreProcessingTool);
            return (needed, baseNeeded, unusedOwned);
        }

        public static Dictionary<int, int> CollectNeeded(RecipeCompactNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            if (root == null)
            {
                return needed;
            }

            var ownedDescendantInfluence = new Dictionary<RecipeCompactNode, int>();
            BuildOwnedDescendantInfluenceMap(root, ownedDescendantInfluence, 0);
            CollectNeededRecursive(root, ownedDescendantInfluence, needed, ignoreProcessingTool, root.NeededCount, true);
            return needed;
        }

        public static Dictionary<int, int> CollectNeededBase(RecipeCompactNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            CollectNeededBaseRecursive(root, needed, ignoreProcessingTool);
            return needed;
        }

        /// <summary>
        /// Collect needed items under a score/count/reward-pool budget constraint.
        /// Items that cannot be collected at their current level (wrong pool, no score, insufficient budget)
        /// are drilled down to their child ingredients recursively.
        /// </summary>
        public static Dictionary<int, int> CollectNeededWithBudget(
            RecipeCompactNode root,
            RecipeCollectBudget budget,
            bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            if (root == null || budget == null || budget.IsExhausted)
            {
                return needed;
            }

            var ownedDescendantInfluence = new Dictionary<RecipeCompactNode, int>();
            BuildOwnedDescendantInfluenceMap(root, ownedDescendantInfluence, 0);
            CollectNeededWithBudgetRecursive(root, ownedDescendantInfluence, needed, ignoreProcessingTool,
                root.NeededCount, true, budget);
            return needed;
        }

        /// <summary>
        /// Mark ownership first, then collect needed items under budget constraint.
        /// </summary>
        public static Dictionary<int, int> CollectNeededWithBudget(
            RecipeCompactNode root,
            IDictionary<int, int> owned,
            RecipeCollectBudget budget,
            out Dictionary<int, int> unusedOwned,
            bool ignoreProcessingTool = true)
        {
            MarkOwned(root, owned, out unusedOwned);
            return CollectNeededWithBudget(root, budget, ignoreProcessingTool);
        }

        public static string PrintTree(RecipeCompactNode root)
        {
            if (root == null)
            {
                return "(null)";
            }

            var sb = new StringBuilder();
            PrintNode(root, string.Empty, true, true, sb);
            return sb.ToString();
        }

        private static RecipeCompactNode BuildNode(int itemId, int amount, BuildContext context)
        {
            if (context.Path.Contains(itemId))
            {
                throw new System.InvalidOperationException("Recipe cycle detected at itemId " + itemId);
            }

            if (amount == 1)
            {
                RecipeCompactNode cached;
                if (context.TemplateCache.TryGetValue(itemId, out cached))
                {
                    return CloneFromTemplate(cached, 1);
                }
            }

            RecipeDefinition recipe;
            if (!TryGetRecipe(itemId, out recipe))
            {
                throw new KeyNotFoundException("No recipe found for itemId " + itemId + ".");
            }

            RecipeCompactNode node = RentNode();
            node.Id = itemId;
            node.IsMaterial = recipe.IsMaterial;
            node.IsBaseMaterial = recipe.IsBaseMaterial || recipe.Inputs.Count == 0;
            node.IsProcessingTool = recipe.IsProcessingTool;
            node.Score = recipe.Score;
            node.NeededCount = amount;

            if (recipe.Inputs.Count == 0)
            {
                if (amount == 1)
                {
                    context.TemplateCache[itemId] = CloneTemplate(node, 1);
                }

                return node;
            }

            EnsureChildrenCapacity(node, recipe.Inputs.Count);
            context.Path.Add(itemId);

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                Ingredient ingredient = recipe.Inputs[i];
                int childNeeded = ingredient.Count * amount;
                RecipeCompactNode child = BuildNode(ingredient.ItemId, childNeeded, context);
                child.Parent = node;
                node.Children.Add(child);
            }

            context.Path.Remove(itemId);

            if (amount == 1)
            {
                context.TemplateCache[itemId] = CloneTemplate(node, 1);
            }

            return node;
        }

        private static bool TryGetRecipe(int itemId, out RecipeDefinition recipe)
        {
            if (s_recipes.TryGetValue(itemId, out recipe))
            {
                return true;
            }

            if (TryRecipeResolver == null)
            {
                recipe = null;
                return false;
            }

            if (!TryRecipeResolver(itemId, out recipe) || recipe == null)
            {
                recipe = null;
                return false;
            }

            s_recipes[itemId] = recipe;
            return true;
        }

        private static RecipeCompactNode CloneFromTemplate(RecipeCompactNode source, int neededOverride)
        {
            RecipeCompactNode clone = RentNode();
            clone.Id = source.Id;
            clone.IsMaterial = source.IsMaterial;
            clone.IsBaseMaterial = source.IsBaseMaterial;
            clone.IsProcessingTool = source.IsProcessingTool;
            clone.Score = source.Score;
            clone.NeededCount = neededOverride;

            if (source.Children == null || source.Children.Count == 0)
            {
                return clone;
            }

            EnsureChildrenCapacity(clone, source.Children.Count);
            for (int i = 0; i < source.Children.Count; i++)
            {
                RecipeCompactNode childClone = CloneFromTemplate(source.Children[i], source.Children[i].NeededCount);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }

            return clone;
        }

        private static RecipeCompactNode CloneTemplate(RecipeCompactNode source, int neededOverride)
        {
            var clone = new RecipeCompactNode
            {
                Id = source.Id,
                IsMaterial = source.IsMaterial,
                IsBaseMaterial = source.IsBaseMaterial,
                IsProcessingTool = source.IsProcessingTool,
                Score = source.Score,
                NeededCount = neededOverride
            };

            if (source.Children == null || source.Children.Count == 0)
            {
                return clone;
            }

            clone.Children = new List<RecipeCompactNode>(source.Children.Count);
            for (int i = 0; i < source.Children.Count; i++)
            {
                RecipeCompactNode childClone = CloneTemplate(source.Children[i], source.Children[i].NeededCount);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }

            return clone;
        }

        private static RecipeCompactNode RentNode()
        {
            RecipeCompactNode node = s_nodePool.Count > 0 ? s_nodePool.Pop() : new RecipeCompactNode();
            node.Id = 0;
            node.IsMaterial = false;
            node.IsBaseMaterial = false;
            node.IsProcessingTool = false;
            node.Score = 0f;
            node.NeededCount = 0;
            node.OwnedDirectCount = 0;
            node.OwnedInheritedCount = 0;
            node.Parent = null;

            if (node.Children != null)
            {
                node.Children.Clear();
            }

            return node;
        }

        private static void EnsureChildrenCapacity(RecipeCompactNode node, int capacity)
        {
            if (node.Children == null)
            {
                node.Children = new List<RecipeCompactNode>(capacity);
                return;
            }

            if (node.Children.Capacity < capacity)
            {
                node.Children.Capacity = capacity;
            }
        }

        private static void ReturnNodeRecursive(RecipeCompactNode root)
        {
            var stack = new Stack<RecipeCompactNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                RecipeCompactNode node = stack.Pop();

                if (node.Children != null)
                {
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        stack.Push(node.Children[i]);
                    }

                    node.Children.Clear();
                }

                node.Parent = null;
                node.OwnedDirectCount = 0;
                node.OwnedInheritedCount = 0;
                node.Score = 0f;
                node.NeededCount = 0;
                s_nodePool.Push(node);
            }
        }

        private static void MarkOwnedRecursive(RecipeCompactNode node, Dictionary<int, int> remaining, int inheritedOwned)
        {
            if (node == null)
            {
                return;
            }

            node.OwnedInheritedCount = inheritedOwned;

            int directNeed = node.NeededCount - inheritedOwned;
            if (directNeed < 0)
            {
                directNeed = 0;
            }

            int stock;
            if (directNeed > 0 && remaining.TryGetValue(node.Id, out stock) && stock > 0)
            {
                int consume = stock < directNeed ? stock : directNeed;
                node.OwnedDirectCount = consume;
                remaining[node.Id] = stock - consume;
            }
            else
            {
                node.OwnedDirectCount = 0;
            }

            if (node.Children == null)
            {
                return;
            }

            int fullOwned = node.OwnedCount;
            if (fullOwned > node.NeededCount)
            {
                fullOwned = node.NeededCount;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                RecipeCompactNode child = node.Children[i];

                // Child need is scaled from parent need when building compact tree.
                // Inherited ownership must use the same ratio to stay equivalent to expanded-tree behavior.
                int inheritedForChild = node.NeededCount > 0
                    ? fullOwned * child.NeededCount / node.NeededCount
                    : 0;

                MarkOwnedRecursive(child, remaining, inheritedForChild);
            }
        }

        // For each node, compute how many of this node's units are affected by ownership in descendants,
        // excluding ownership that is only propagated from this node itself.
        // Return value is independent-owned influence in this whole subtree for parent conversion.
        private static int BuildOwnedDescendantInfluenceMap(
            RecipeCompactNode node,
            Dictionary<RecipeCompactNode, int> map,
            int ownedProvidedByParent)
        {
            int selfIndependentOwned = node.OwnedCount - ownedProvidedByParent;
            if (selfIndependentOwned < 0)
            {
                selfIndependentOwned = 0;
            }

            if (selfIndependentOwned > node.NeededCount)
            {
                selfIndependentOwned = node.NeededCount;
            }

            int descendantInfluence = 0;

            if (node.Children != null)
            {
                int fullOwned = node.OwnedCount;
                if (fullOwned > node.NeededCount)
                {
                    fullOwned = node.NeededCount;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    RecipeCompactNode child = node.Children[i];
                    if (child.NeededCount <= 0 || node.NeededCount <= 0)
                    {
                        continue;
                    }

                    int childOwnedProvidedByNode = fullOwned * child.NeededCount / node.NeededCount;
                    int childIndependentInfluence = BuildOwnedDescendantInfluenceMap(child, map, childOwnedProvidedByNode);

                    if (childIndependentInfluence <= 0)
                    {
                        continue;
                    }

                    int parentAffectedUnits = CeilDiv(childIndependentInfluence * node.NeededCount, child.NeededCount);
                    if (parentAffectedUnits > descendantInfluence)
                    {
                        descendantInfluence = parentAffectedUnits;
                    }
                }
            }

            if (descendantInfluence > node.NeededCount)
            {
                descendantInfluence = node.NeededCount;
            }

            map[node] = descendantInfluence;

            int independentSubtreeInfluence = selfIndependentOwned;
            if (descendantInfluence > independentSubtreeInfluence)
            {
                independentSubtreeInfluence = descendantInfluence;
            }

            if (independentSubtreeInfluence > node.NeededCount)
            {
                independentSubtreeInfluence = node.NeededCount;
            }

            return independentSubtreeInfluence;
        }

        private static void CollectNeededRecursive(
            RecipeCompactNode node,
            Dictionary<RecipeCompactNode, int> ownedDescendantInfluence,
            Dictionary<int, int> needed,
            bool ignoreProcessingTool,
            int scopeNeededCount,
            bool isRoot)
        {
            if (node == null || scopeNeededCount <= 0)
            {
                return;
            }

            if (ignoreProcessingTool && node.IsProcessingTool)
            {
                return;
            }

            // Scope here represents the unresolved branch only, so inherited ownership from parent
            // should not be deducted again in this scope.
            int ownedInScope = node.OwnedDirectCount;
            if (ownedInScope > scopeNeededCount)
            {
                ownedInScope = scopeNeededCount;
            }

            int unownedInScope = scopeNeededCount - ownedInScope;
            if (unownedInScope <= 0)
            {
                return;
            }

            if (node.Children == null || node.Children.Count == 0)
            {
                if (node.IsMaterial)
                {
                    AddNeeded(needed, node.Id, unownedInScope);
                }
                return;
            }

            int descendantInfluence;
            ownedDescendantInfluence.TryGetValue(node, out descendantInfluence);
            if (descendantInfluence > unownedInScope)
            {
                descendantInfluence = unownedInScope;
            }

            if (descendantInfluence <= 0)
            {
                if (node.IsMaterial)
                {
                    AddNeeded(needed, node.Id, unownedInScope);
                    return;
                }
            }

            if (node.IsMaterial)
            {
                if (isRoot)
                {
                    // Align with RecipeBook root behavior: when root has any owned-in-subtree,
                    // traverse children for the whole unresolved root scope instead of adding root boundary.
                    int rootDrillScope = unownedInScope;
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        RecipeCompactNode child = node.Children[i];
                        if (child.NeededCount <= 0 || node.NeededCount <= 0)
                        {
                            continue;
                        }

                        int childScope = CeilDiv(rootDrillScope * child.NeededCount, node.NeededCount);
                        CollectNeededRecursive(child, ownedDescendantInfluence, needed, ignoreProcessingTool, childScope,
                            false);
                    }

                    return;
                }

                int boundaryCount = unownedInScope - descendantInfluence;
                if (boundaryCount > 0)
                {
                    AddNeeded(needed, node.Id, boundaryCount);
                }
            }

            int drillDownScope = node.IsMaterial ? descendantInfluence : unownedInScope;
            if (drillDownScope <= 0)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                RecipeCompactNode child = node.Children[i];
                if (child.NeededCount <= 0 || node.NeededCount <= 0)
                {
                    continue;
                }

                int childScope = CeilDiv(drillDownScope * child.NeededCount, node.NeededCount);
                CollectNeededRecursive(child, ownedDescendantInfluence, needed, ignoreProcessingTool, childScope, false);
            }
        }

        private static int CeilDiv(int numerator, int denominator)
        {
            return (numerator + denominator - 1) / denominator;
        }

        private static Dictionary<int, int> CreateRemainingOwnedMap(IDictionary<int, int> owned)
        {
            return owned == null
                ? new Dictionary<int, int>()
                : new Dictionary<int, int>(owned);
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


        private static void CollectNeededBaseRecursive(RecipeCompactNode node, Dictionary<int, int> needed,
            bool ignoreProcessingTool)
        {
            if (node == null)
            {
                return;
            }

            if (ignoreProcessingTool && node.IsProcessingTool)
            {
                return;
            }

            if (node.IsFullyOwned)
            {
                return;
            }

            if (node.IsBaseMaterial)
            {
                AddNeeded(needed, node.Id, node.NeededCount - node.OwnedCount);
                return;
            }

            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                CollectNeededBaseRecursive(node.Children[i], needed, ignoreProcessingTool);
            }
        }

        // ── Budget-constrained collection helpers ──────────────────────────────

        /// <summary>
        /// Try to add <paramref name="count"/> units of <paramref name="node"/> to needed via budget.
        /// Returns the number of units that could NOT be collected (caller should drill to children).
        /// </summary>
        private static int AddNeededWithBudget(
            Dictionary<int, int> needed,
            RecipeCompactNode node,
            int count,
            RecipeCollectBudget budget)
        {
            if (count <= 0) return 0;
            int collected = budget.TryCollect(node.Id, node.Score, count);
            if (collected > 0)
            {
                AddNeeded(needed, node.Id, collected);
            }

            return count - collected;
        }

        private static void DrillChildrenWithBudget(
            RecipeCompactNode node,
            Dictionary<RecipeCompactNode, int> ownedDescendantInfluence,
            Dictionary<int, int> needed,
            bool ignoreProcessingTool,
            int drillScope,
            RecipeCollectBudget budget)
        {
            if (node.Children == null || drillScope <= 0) return;

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (budget.IsExhausted) return;

                RecipeCompactNode child = node.Children[i];
                if (child.NeededCount <= 0 || node.NeededCount <= 0) continue;

                int childScope = CeilDiv(drillScope * child.NeededCount, node.NeededCount);
                CollectNeededWithBudgetRecursive(child, ownedDescendantInfluence, needed,
                    ignoreProcessingTool, childScope, false, budget);
            }
        }

        private static void CollectNeededWithBudgetRecursive(
            RecipeCompactNode node,
            Dictionary<RecipeCompactNode, int> ownedDescendantInfluence,
            Dictionary<int, int> needed,
            bool ignoreProcessingTool,
            int scopeNeededCount,
            bool isRoot,
            RecipeCollectBudget budget)
        {
            if (node == null || scopeNeededCount <= 0 || budget.IsExhausted) return;
            if (ignoreProcessingTool && node.IsProcessingTool) return;

            int ownedInScope = node.OwnedDirectCount;
            if (ownedInScope > scopeNeededCount) ownedInScope = scopeNeededCount;

            int unownedInScope = scopeNeededCount - ownedInScope;
            if (unownedInScope <= 0) return;

            // ── Leaf node: collect what budget allows; no children to fall back on ──
            if (node.Children == null || node.Children.Count == 0)
            {
                if (node.IsMaterial)
                {
                    AddNeededWithBudget(needed, node, unownedInScope, budget);
                }

                return;
            }

            int descendantInfluence;
            ownedDescendantInfluence.TryGetValue(node, out descendantInfluence);
            if (descendantInfluence > unownedInScope) descendantInfluence = unownedInScope;

            // ── No owned influence in subtree ──────────────────────────────────────
            if (descendantInfluence <= 0)
            {
                if (node.IsMaterial)
                {
                    // Try to collect at this level; any remainder falls through to children.
                    int notCollected = AddNeededWithBudget(needed, node, unownedInScope, budget);
                    if (notCollected > 0 && !budget.IsExhausted)
                    {
                        DrillChildrenWithBudget(node, ownedDescendantInfluence, needed,
                            ignoreProcessingTool, notCollected, budget);
                    }
                }
                else
                {
                    DrillChildrenWithBudget(node, ownedDescendantInfluence, needed,
                        ignoreProcessingTool, unownedInScope, budget);
                }

                return;
            }

            // ── Has owned influence in subtree ─────────────────────────────────────
            if (node.IsMaterial)
            {
                if (isRoot)
                {
                    // Root: always drill children for the full unowned scope.
                    DrillChildrenWithBudget(node, ownedDescendantInfluence, needed,
                        ignoreProcessingTool, unownedInScope, budget);
                    return;
                }

                // Boundary units (no owned influence at all) — try to collect at this level.
                int boundaryCount = unownedInScope - descendantInfluence;
                int boundaryNotCollected = 0;
                if (boundaryCount > 0)
                {
                    boundaryNotCollected = AddNeededWithBudget(needed, node, boundaryCount, budget);
                }

                // Drill scope = descendant-influenced units + any boundary units we couldn't collect.
                int drillDownScope = descendantInfluence + boundaryNotCollected;
                if (drillDownScope > 0 && !budget.IsExhausted)
                {
                    DrillChildrenWithBudget(node, ownedDescendantInfluence, needed,
                        ignoreProcessingTool, drillDownScope, budget);
                }
            }
            else
            {
                // Non-material: always drill.
                DrillChildrenWithBudget(node, ownedDescendantInfluence, needed,
                    ignoreProcessingTool, unownedInScope, budget);
            }
        }

        private static void PrintNode(RecipeCompactNode node, string prefix, bool isLast, bool isRoot, StringBuilder sb)
        {
            if (!isRoot)
            {
                sb.Append(prefix).Append(isLast ? "\\- " : "+- ");
            }

            sb.Append(node.Id)
                .Append(" x")
                .Append(node.NeededCount)
                .Append(" [own:")
                .Append(node.OwnedCount)
                .Append("] [score:")
                .Append(node.Score)
                .Append(']')
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

        private static void AddNeeded(Dictionary<int, int> needed, int itemId, int count)
        {
            if (count <= 0)
            {
                return;
            }

            int current;
            needed.TryGetValue(itemId, out current);
            needed[itemId] = current + count;
        }
    }