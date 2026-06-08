namespace Game
{
    internal enum NeedCollectAction
    {
        Stop,
        AddCurrent,
        TraverseChildren
    }

    internal static class RecipeNeedCollectionRule
    {
        /// <summary>
        /// Decide how a node should be handled when collecting needed items.
        /// </summary>
        public static NeedCollectAction Evaluate(RecipeNode node, bool hasOwnedInSubtree)
        {
            if (!hasOwnedInSubtree)
            {
                return node.IsMaterial ? NeedCollectAction.AddCurrent : NeedCollectAction.TraverseChildren;
            }

            bool hasChildren = node.Children != null && node.Children.Count > 0;
            if (!hasChildren)
            {
                return node.IsMaterial ? NeedCollectAction.AddCurrent : NeedCollectAction.Stop;
            }

            return NeedCollectAction.TraverseChildren;
        }

        /// <summary>
        /// Decide how a node should be handled when collecting base-material requirements.
        /// </summary>
        public static NeedCollectAction EvaluateBase(RecipeNode node)
        {
            if (node.IsBaseMaterial)
            {
                return NeedCollectAction.AddCurrent;
            }

            bool hasChildren = node.Children != null && node.Children.Count > 0;
            return hasChildren ? NeedCollectAction.TraverseChildren : NeedCollectAction.Stop;
        }
    }
}

