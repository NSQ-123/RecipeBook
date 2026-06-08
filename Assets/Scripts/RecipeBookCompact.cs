using System.Collections.Generic;
using System.Text;

namespace Game
{
    // Compact tree: each ingredient appears once per recipe edge with NeededCount aggregation.
    // This keeps the existing RecipeBook untouched while testing the single-node+count model.
    public static class RecipeBookCompact
    {
        private static readonly Stack<CompactNode> s_nodePool = new Stack<CompactNode>(256);
        private static readonly Dictionary<int, RecipeDefinition> s_recipes = new Dictionary<int, RecipeDefinition>();

        /// <summary>
        /// External recipe provider callback used only by RecipeBookCompact.
        /// </summary>
        public delegate bool TryResolveRecipeDelegate(int itemId, out RecipeDefinition recipe);

        public static TryResolveRecipeDelegate TryRecipeResolver;

        public sealed class CompactNode
        {
            public int Id;
            public bool IsMaterial;
            public bool IsBaseMaterial;
            public bool IsProcessingTool;
            public int NeededCount;
            public int OwnedDirectCount;
            public int OwnedInheritedCount;
            public CompactNode Parent;
            public List<CompactNode> Children;

            public int OwnedCount => OwnedDirectCount + OwnedInheritedCount;
            public bool IsFullyOwned => OwnedCount >= NeededCount;
        }

        private sealed class BuildContext
        {
            public readonly Dictionary<int, CompactNode> TemplateCache = new Dictionary<int, CompactNode>();
            public readonly HashSet<int> Path = new HashSet<int>();
        }

        public static CompactNode BuildTree(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            var context = new BuildContext();
            return BuildNode(itemId, amount, context);
        }

        // Release a compact tree back to pool after use.
        public static void ReleaseTree(CompactNode root)
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

        public static void MarkOwned(CompactNode root, IDictionary<int, int> owned)
        {
            var remaining = owned == null
                ? new Dictionary<int, int>()
                : new Dictionary<int, int>(owned);

            MarkOwnedRecursive(root, remaining, 0);
        }

        public static Dictionary<int, int> CollectNeeded(CompactNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            if (root == null)
            {
                return needed;
            }

            var ownedDescendantInfluence = new Dictionary<CompactNode, int>();
            BuildOwnedDescendantInfluenceMap(root, ownedDescendantInfluence, 0);
            CollectNeededRecursive(root, ownedDescendantInfluence, needed, ignoreProcessingTool, root.NeededCount);
            return needed;
        }

        public static Dictionary<int, int> CollectNeededBase(CompactNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            CollectNeededBaseRecursive(root, needed, ignoreProcessingTool);
            return needed;
        }

        public static string PrintTree(CompactNode root)
        {
            if (root == null)
            {
                return "(null)";
            }

            var sb = new StringBuilder();
            PrintNode(root, string.Empty, true, true, sb);
            return sb.ToString();
        }

        private static CompactNode BuildNode(int itemId, int amount, BuildContext context)
        {
            if (context.Path.Contains(itemId))
            {
                throw new System.InvalidOperationException("Recipe cycle detected at itemId " + itemId);
            }

            if (amount == 1)
            {
                CompactNode cached;
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

            CompactNode node = RentNode();
            node.Id = itemId;
            node.IsMaterial = recipe.IsMaterial;
            node.IsBaseMaterial = recipe.IsBaseMaterial || recipe.Inputs.Count == 0;
            node.IsProcessingTool = recipe.IsProcessingTool;
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
                CompactNode child = BuildNode(ingredient.ItemId, childNeeded, context);
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

        private static CompactNode CloneFromTemplate(CompactNode source, int neededOverride)
        {
            CompactNode clone = RentNode();
            clone.Id = source.Id;
            clone.IsMaterial = source.IsMaterial;
            clone.IsBaseMaterial = source.IsBaseMaterial;
            clone.IsProcessingTool = source.IsProcessingTool;
            clone.NeededCount = neededOverride;

            if (source.Children == null || source.Children.Count == 0)
            {
                return clone;
            }

            EnsureChildrenCapacity(clone, source.Children.Count);
            for (int i = 0; i < source.Children.Count; i++)
            {
                CompactNode childClone = CloneFromTemplate(source.Children[i], source.Children[i].NeededCount);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }

            return clone;
        }

        private static CompactNode CloneTemplate(CompactNode source, int neededOverride)
        {
            var clone = new CompactNode
            {
                Id = source.Id,
                IsMaterial = source.IsMaterial,
                IsBaseMaterial = source.IsBaseMaterial,
                IsProcessingTool = source.IsProcessingTool,
                NeededCount = neededOverride
            };

            if (source.Children == null || source.Children.Count == 0)
            {
                return clone;
            }

            clone.Children = new List<CompactNode>(source.Children.Count);
            for (int i = 0; i < source.Children.Count; i++)
            {
                CompactNode childClone = CloneTemplate(source.Children[i], source.Children[i].NeededCount);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }

            return clone;
        }

        private static CompactNode RentNode()
        {
            CompactNode node = s_nodePool.Count > 0 ? s_nodePool.Pop() : new CompactNode();
            node.Id = 0;
            node.IsMaterial = false;
            node.IsBaseMaterial = false;
            node.IsProcessingTool = false;
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

        private static void EnsureChildrenCapacity(CompactNode node, int capacity)
        {
            if (node.Children == null)
            {
                node.Children = new List<CompactNode>(capacity);
                return;
            }

            if (node.Children.Capacity < capacity)
            {
                node.Children.Capacity = capacity;
            }
        }

        private static void ReturnNodeRecursive(CompactNode root)
        {
            var stack = new Stack<CompactNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                CompactNode node = stack.Pop();

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
                node.NeededCount = 0;
                s_nodePool.Push(node);
            }
        }

        private static void MarkOwnedRecursive(CompactNode node, Dictionary<int, int> remaining, int inheritedOwned)
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
                CompactNode child = node.Children[i];

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
            CompactNode node,
            Dictionary<CompactNode, int> map,
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
                    CompactNode child = node.Children[i];
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
            CompactNode node,
            Dictionary<CompactNode, int> ownedDescendantInfluence,
            Dictionary<int, int> needed,
            bool ignoreProcessingTool,
            int scopeNeededCount)
        {
            if (node == null || scopeNeededCount <= 0)
            {
                return;
            }

            if (ignoreProcessingTool && node.IsProcessingTool)
            {
                return;
            }

            int ownedInScope = node.OwnedCount;
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
                CompactNode child = node.Children[i];
                if (child.NeededCount <= 0 || node.NeededCount <= 0)
                {
                    continue;
                }

                int childScope = CeilDiv(drillDownScope * child.NeededCount, node.NeededCount);
                CollectNeededRecursive(child, ownedDescendantInfluence, needed, ignoreProcessingTool, childScope);
            }
        }

        private static int CeilDiv(int numerator, int denominator)
        {
            return (numerator + denominator - 1) / denominator;
        }

        private static void CollectNeededBaseRecursive(CompactNode node, Dictionary<int, int> needed,
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

        private static void PrintNode(CompactNode node, string prefix, bool isLast, bool isRoot, StringBuilder sb)
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
}