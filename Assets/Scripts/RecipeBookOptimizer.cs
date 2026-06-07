  using System.Collections.Generic;

namespace Game
{
    // Optimization layer for RecipeBook. Keeps RecipeBook untouched and reusable.
    public static class RecipeBookOptimizer
    {
        /// <summary>
        /// Explicit DFS stack state for iterative traversal.
        /// Expanded=false means first visit, true means post-order visit.
        /// </summary>
        public struct VisitState
        {
            /// <summary>Current node to process.</summary>
            public RecipeNode Node;
            /// <summary>Whether children are already pushed/processed.</summary>
            public bool Expanded;

            public VisitState(RecipeNode node, bool expanded)
            {
                Node = node;
                Expanded = expanded;
            }
        }

        /// <summary>
        /// Convenience API that allocates internal temporary containers.
        /// </summary>
        public static Dictionary<int, int> CollectNeededItems(RecipeNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            var ownedSubtree = new Dictionary<RecipeNode, bool>();
            var postOrder = new List<RecipeNode>();
            var stack = new Stack<VisitState>();

            CollectNeededItemsNonAlloc(root, needed, ownedSubtree, postOrder, stack, ignoreProcessingTool);
            return needed;
        }

        // Reusable-container version to reduce GC in frequent calls.
        public static void CollectNeededItemsNonAlloc(
            RecipeNode root,
            Dictionary<int, int> needed,
            Dictionary<RecipeNode, bool> ownedSubtree,
            List<RecipeNode> postOrder,
            Stack<VisitState> stack,
            bool ignoreProcessingTool = true)
        {
            // Callers can reuse these containers between frames/runs.
            needed.Clear();
            ownedSubtree.Clear();
            postOrder.Clear();
            stack.Clear();

            if (root == null)
            {
                return;
            }

            BuildPostOrder(root, postOrder, stack);

            // Pass 1: compute whether each node's subtree has owned items.
            // We process post-order so child results are always available.
            for (int i = 0; i < postOrder.Count; i++)
            {
                RecipeNode node = postOrder[i];
                if (ignoreProcessingTool && node.IsProcessingTool)
                {
                    ownedSubtree[node] = false;
                    continue;
                }

                bool hasOwned = node.IsOwn;

                if (node.Children != null)
                {
                    for (int c = 0; c < node.Children.Count; c++)
                    {
                        if (ownedSubtree[node.Children[c]])
                        {
                            hasOwned = true;
                            break;
                        }
                    }
                }

                ownedSubtree[node] = hasOwned;
            }

            // Pass 2: collect needed nodes by original RecipeBook rule.
            stack.Push(new VisitState(root, false));
            while (stack.Count > 0)
            {
                VisitState state = stack.Pop();
                RecipeNode node = state.Node;

                if (ignoreProcessingTool && node.IsProcessingTool)
                {
                    continue;
                }

                if (node.IsOwn)
                {
                    continue;
                }

                bool hasOwnedInSubtree;
                ownedSubtree.TryGetValue(node, out hasOwnedInSubtree);
                if (!hasOwnedInSubtree)
                {
                    // No owned item exists in this subtree, so this node itself is needed.
                    AddNeeded(needed, node.Id, node.NeededCount);
                    continue;
                }

                if (node.Children == null || node.Children.Count == 0)
                {
                    // Leaf and still not owned -> directly needed.
                    AddNeeded(needed, node.Id, node.NeededCount);
                    continue;
                }

                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(new VisitState(node.Children[i], false));
                }
            }
        }

        /// <summary>
        /// Convenience API that allocates internal stack for base-material-only collection.
        /// </summary>
        public static Dictionary<int, int> CollectNeededBaseMaterials(RecipeNode root, bool ignoreProcessingTool = true)
        {
            var needed = new Dictionary<int, int>();
            var stack = new Stack<RecipeNode>();
            CollectNeededBaseMaterialsNonAlloc(root, needed, stack, ignoreProcessingTool);
            return needed;
        }

        /// <summary>
        /// Collect only missing base materials.
        /// </summary>
        public static void CollectNeededBaseMaterialsNonAlloc(
            RecipeNode root,
            Dictionary<int, int> needed,
            Stack<RecipeNode> stack,
            bool ignoreProcessingTool = true)
        {
            needed.Clear();
            stack.Clear();

            if (root == null)
            {
                return;
            }

            stack.Push(root);
            while (stack.Count > 0)
            {
                RecipeNode node = stack.Pop();

                if (ignoreProcessingTool && node.IsProcessingTool)
                {
                    continue;
                }

                if (node.IsOwn)
                {
                    continue;
                }

                if (node.IsBaseMaterial)
                {
                    // Base material that is not owned contributes to final requirement.
                    AddNeeded(needed, node.Id, node.NeededCount);
                    continue;
                }

                if (node.Children == null)
                {
                    continue;
                }

                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }
            }
        }

        /// <summary>
        /// Build post-order list iteratively to avoid recursion stack pressure.
        /// </summary>
        private static void BuildPostOrder(RecipeNode root, List<RecipeNode> postOrder, Stack<VisitState> stack)
        {
            stack.Push(new VisitState(root, false));
            while (stack.Count > 0)
            {
                VisitState state = stack.Pop();

                if (state.Node == null)
                {
                    continue;
                }

                if (state.Expanded)
                {
                    postOrder.Add(state.Node);
                    continue;
                }

                // First time we see this node: enqueue a post-order marker,
                // then push children.
                stack.Push(new VisitState(state.Node, true));

                if (state.Node.Children == null)
                {
                    continue;
                }

                for (int i = state.Node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(new VisitState(state.Node.Children[i], false));
                }
            }
        }

        /// <summary>
        /// Accumulate required counts by item id.
        /// </summary>
        private static void AddNeeded(Dictionary<int, int> needed, int itemId, int count)
        {
            int current;
            needed.TryGetValue(itemId, out current);
            needed[itemId] = current + count;
        }
    }
}


