using System.Collections.Generic;

namespace Game
{
        /// <summary>
    /// Ingredient requirement entry in a recipe definition.
    /// </summary>
    public struct Ingredient
    {
        public readonly int ItemId;
        public readonly int Count;

        public Ingredient(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    /// <summary>
    /// Static recipe data for one output item.
    /// </summary>
    public class RecipeDefinition
    {
        public readonly bool IsMaterial;
        public readonly bool IsBaseMaterial;
        public readonly bool IsProcessingTool;
        public readonly List<Ingredient> Inputs;

        public RecipeDefinition(bool isMaterial, bool isBaseMaterial, params Ingredient[] inputs)
            : this(isMaterial, isBaseMaterial, false, inputs)
        {
        }

        public RecipeDefinition(bool isMaterial, bool isBaseMaterial, bool isProcessingTool, params Ingredient[] inputs)
        {
            IsMaterial = isMaterial;
            IsBaseMaterial = isBaseMaterial;
            IsProcessingTool = isProcessingTool;
            Inputs = new List<Ingredient>(inputs);
        }
    }
    
    /// <summary>
    /// Runtime recipe tree node used for ownership marking and need calculation.
    /// </summary>
    public class RecipeNode
    {
        public int Id {get; private set; }
        public bool IsMaterial {get; private set; }
        public bool IsBaseMaterial  {get; private set; }
        public bool IsProcessingTool {get; private set; }
        public int ChildCount {get; private set; }
        public RecipeNode Parent{get; private set; }
        public List<RecipeNode> Children {get; private set; }
        public bool IsOwn{get; private set; }
        public bool IsOwnDirect { get; private set; }
        public int NeededCount { get; private set; }
        
        public RecipeNode(int itemId, bool isMaterial, bool isBaseMaterial, bool isProcessingTool = false)
        {
            Id = itemId;
            IsMaterial = isMaterial;
            IsBaseMaterial = isBaseMaterial;
            IsProcessingTool = isProcessingTool;
            NeededCount = 1;
        }
        
        public void AddChild(RecipeNode child)
        {
            // Keep parent/child references in sync for tree traversal.
            Children ??= new List<RecipeNode>();
            child.SetParent(this);
            Children.Add(child);
            ChildCount = Children.Count;
        }

        public void PrepareChildrenCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                return;
            }

            Children ??= new List<RecipeNode>(capacity);
            if (Children.Capacity < capacity)
            {
                Children.Capacity = capacity;
            }
        }
        
        public void SetParent(RecipeNode parent)
        {
            Parent = parent;
        }
        
        public void SetOwn(bool isOwn, bool isOwnDirect = false)
        {
            IsOwn = isOwn;
            IsOwnDirect = isOwnDirect;
        }

        public void SetNeededCount(int count)
        {
            NeededCount = count;
        }

        public void Clear()
        {
            Id = 0;
            IsMaterial = false;
            IsBaseMaterial = false;
            IsProcessingTool = false;
            ChildCount = 0;
            Parent = null;
            NeededCount = 0;
            Children?.Clear();
            IsOwn = false;
            IsOwnDirect = false;
        }
    }
    
}