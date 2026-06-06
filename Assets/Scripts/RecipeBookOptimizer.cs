  using System.Collections.Generic;

namespace Game
{
    // Optimization layer for RecipeBook. Keeps RecipeBook untouched and reusable.
    public static class RecipeBookOptimizer
    {
        public struct VisitState
        {
            public RecipeNode Node;
            public bool Expanded;

            public VisitState(RecipeNode node, bool expanded)
            {
                Node = node;
                Expanded = expanded;
            }
        }

        public static Dictionary<int, int> CollectNeededItems(RecipeNode root)
        {
            var needed = new Dictionary<int, int>();
            var ownedSubtree = new Dictionary<RecipeNode, bool>();
            var postOrder = new List<RecipeNode>();
            var stack = new Stack<VisitState>();

            CollectNeededItemsNonAlloc(root, needed, ownedSubtree, postOrder, stack);
            return needed;
        }

        // Reusable-container version to reduce GC in frequent calls.
        public static void CollectNeededItemsNonAlloc(
            RecipeNode root,
            Dictionary<int, int> needed,
            Dictionary<RecipeNode, bool> ownedSubtree,
            List<RecipeNode> postOrder,
            Stack<VisitState> stack)
        {
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
            for (int i = 0; i < postOrder.Count; i++)
            {
                RecipeNode node = postOrder[i];
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

                if (node.IsOwn)
                {
                    continue;
                }

                bool hasOwnedInSubtree;
                ownedSubtree.TryGetValue(node, out hasOwnedInSubtree);
                if (!hasOwnedInSubtree)
                {
                    AddNeeded(needed, node.Id, node.NeededCount);
                    continue;
                }

                if (node.Children == null || node.Children.Count == 0)
                {
                    AddNeeded(needed, node.Id, node.NeededCount);
                    continue;
                }

                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(new VisitState(node.Children[i], false));
                }
            }
        }

        public static Dictionary<int, int> CollectNeededBaseMaterials(RecipeNode root)
        {
            var needed = new Dictionary<int, int>();
            var stack = new Stack<RecipeNode>();
            CollectNeededBaseMaterialsNonAlloc(root, needed, stack);
            return needed;
        }

        public static void CollectNeededBaseMaterialsNonAlloc(
            RecipeNode root,
            Dictionary<int, int> needed,
            Stack<RecipeNode> stack)
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

                if (node.IsOwn)
                {
                    continue;
                }

                if (node.IsBaseMaterial)
                {
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

        private static void AddNeeded(Dictionary<int, int> needed, int itemId, int count)
        {
            int current;
            needed.TryGetValue(itemId, out current);
            needed[itemId] = current + count;
        }
    }
}


