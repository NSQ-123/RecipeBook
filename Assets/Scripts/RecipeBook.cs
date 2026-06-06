using System.Collections.Generic;
using System.Text;

namespace Game
{
    
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

    public class RecipeDefinition
    {
        public readonly bool IsMaterial;
        public readonly bool IsBaseMaterial;
        public readonly List<Ingredient> Inputs;

        public RecipeDefinition(bool isMaterial, bool isBaseMaterial, params Ingredient[] inputs)
        {
            IsMaterial = isMaterial;
            IsBaseMaterial = isBaseMaterial;
            Inputs = new List<Ingredient>(inputs);
        }
    }
    
    public class RecipeNode
    {
        public int Id {get; private set; }
        public bool IsMaterial {get; private set; }
        public bool IsBaseMaterial  {get; private set; }
        public int ChildCount {get; private set; }
        public RecipeNode Parent{get; private set; }
        public List<RecipeNode> Children {get; private set; }
        public bool IsOwn{get; private set; }
        public bool IsOwnDirect { get; private set; }
        public int NeededCount { get; private set; }
        
        public RecipeNode(int itemId, bool isMaterial, bool isBaseMaterial)
        {
            Id = itemId;
            IsMaterial = isMaterial;
            IsBaseMaterial = isBaseMaterial;
            NeededCount = 1;
        }
        
        public void AddChild(RecipeNode child)
        {
            Children ??= new List<RecipeNode>();
            child.SetParent(this);
            Children.Add(child);
            ChildCount = Children.Count;
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
            ChildCount = 0;
            Parent = null;
            NeededCount = 0;
            Children?.Clear();
            IsOwn = false;
            IsOwnDirect = false;
        }
    }
    
    
    public static class RecipeBook
    {
        public delegate bool TryResolveRecipeDelegate(int itemId, out RecipeDefinition recipe);
        public static TryResolveRecipeDelegate TryRecipeResolver;

        private static readonly Dictionary<int, RecipeDefinition> s_recipes = new  Dictionary<int, RecipeDefinition>();

        public static void RegisterRecipe(int itemId, RecipeDefinition recipe)
        {
            if (recipe == null)
            {
                throw new System.ArgumentNullException(nameof(recipe));
            }

            s_recipes[itemId] = recipe;
        }

        public static void ClearRecipes()
        {
            s_recipes.Clear();
        }

        public static RecipeNode BuildRecipeTree(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            return BuildNode(itemId, amount);
        }

        public static void MarkOwnedItems(RecipeNode root, IDictionary<int, int> ownedItems)
        {
            if (root == null)
            {
                return;
            }

            var remaining = ownedItems == null
                ? new Dictionary<int, int>()
                : new Dictionary<int, int>(ownedItems);

            MarkOwnedRecursive(root, remaining, false);
        }

        public static RecipeNode BuildRecipeTree(int itemId, int amount, IDictionary<int, int> ownedItems)
        {
            RecipeNode root = BuildRecipeTree(itemId, amount);
            MarkOwnedItems(root, ownedItems);
            return root;
        }
        
        
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

        public static Dictionary<int, int> CollectNeededItems(RecipeNode root)
        {
            var needed = new Dictionary<int, int>();
            if (root == null)
            {
                return needed;
            }

            var ownedInSubtree = new Dictionary<RecipeNode, bool>();
            BuildOwnedSubtreeMap(root, ownedInSubtree);
            CollectNeededRecursive(root, ownedInSubtree, needed);
            return needed;
        }

        public static Dictionary<int, int> CollectNeededBaseMaterials(RecipeNode root)
        {
            var needed = new Dictionary<int, int>();
            if (root == null)
            {
                return needed;
            }

            CollectNeededBaseRecursive(root, needed);
            return needed;
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

        private static RecipeNode BuildNode(int itemId, int amount)
        {
            RecipeDefinition def;
            if (!TryGetRecipeDefinition(itemId, out def))
            {
                throw new KeyNotFoundException($"No recipe found for itemId {itemId}. Please assign RecipeBook.TryRecipeResolver or register the recipe externally.");
            }

            bool isBaseMaterial = def.IsBaseMaterial || def.Inputs.Count == 0;
            bool isMaterial = def.IsMaterial;

            var node = new RecipeNode(itemId, isMaterial, isBaseMaterial);
            node.SetNeededCount(amount);

            if (def.Inputs.Count == 0)
            {
                return node;
            }

            for (int i = 0; i < def.Inputs.Count; i++)
            {
                Ingredient ingredient = def.Inputs[i];
                int required = ingredient.Count * amount;

                // 这里按“份数”展开，便于后续标记哪些分支已满足。
                for (int n = 0; n < required; n++)
                {
                    node.AddChild(BuildNode(ingredient.ItemId, 1));
                }
            }

            return node;
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
            bool hasOwned = node.IsOwn;

            if (node.Children != null)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    hasOwned = BuildOwnedSubtreeMap(node.Children[i], map) || hasOwned;
                }
            }

            map[node] = hasOwned;
            return hasOwned;
        }

        private static void CollectNeededRecursive(
            RecipeNode node,
            Dictionary<RecipeNode, bool> ownedInSubtree,
            Dictionary<int, int> needed)
        {
            if (node.IsOwn)
            {
                return;
            }

            bool hasOwned = ownedInSubtree[node];
            if (!hasOwned)
            {
                AddNeeded(needed, node.Id, node.NeededCount);
                return;
            }

            if (node.Children == null)
            {
                AddNeeded(needed, node.Id, node.NeededCount);
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                CollectNeededRecursive(node.Children[i], ownedInSubtree, needed);
            }
        }

        private static void AddNeeded(Dictionary<int, int> needed, int itemId, int count)
        {
            int current;
            needed.TryGetValue(itemId, out current);
            needed[itemId] = current + count;
        }

        private static void CollectNeededBaseRecursive(RecipeNode node, Dictionary<int, int> needed)
        {
            if (node.IsOwn)
            {
                return;
            }

            if (node.IsBaseMaterial)
            {
                AddNeeded(needed, node.Id, node.NeededCount);
                return;
            }

            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                CollectNeededBaseRecursive(node.Children[i], needed);
            }
        }

    }
    
}