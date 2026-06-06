using System.Collections.Generic;
using System.Text;

namespace Game
{
    public class RecipeBook
    {
        private struct Ingredient
        {
            public int ItemId;
            public int Count;

            public Ingredient(int itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }
        }

        private class RecipeDefinition
        {
            public bool IsMaterial;
            public bool IsBaseMaterial;
            public List<Ingredient> Inputs;

            public RecipeDefinition(bool isMaterial, bool isBaseMaterial, params Ingredient[] inputs)
            {
                IsMaterial = isMaterial;
                IsBaseMaterial = isBaseMaterial;
                Inputs = new List<Ingredient>(inputs);
            }
        }

        private static readonly Dictionary<int, RecipeDefinition> s_recipes = CreateRecipes();

        public static RecipeNode CreateDefault()
        {
            return BuildRecipeTree(1005);
        }

        public static RecipeNode BuildRecipeTree(int itemId, int amount = 1)
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            return BuildNode(itemId, amount);
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
            bool hasRecipe = s_recipes.TryGetValue(itemId, out def);
            bool isBaseMaterial = !hasRecipe || def.IsBaseMaterial || def.Inputs.Count == 0;
            bool isMaterial = hasRecipe && def.IsMaterial;

            var node = new RecipeNode(itemId, isMaterial, isBaseMaterial);
            node.SetNeededCount(amount);

            if (!hasRecipe || def.Inputs.Count == 0)
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

        private static Dictionary<int, RecipeDefinition> CreateRecipes()
        {
            var map = new Dictionary<int, RecipeDefinition>();

            AddMergeChain(map, 1001, 1010, true);
            AddMergeChain(map, 2001, 2005, true);

            // 转化物
            map[3001] = new RecipeDefinition(false, false, new Ingredient(2003, 1));
            map[3002] = new RecipeDefinition(false, false, new Ingredient(3001, 2));
            map[3003] = new RecipeDefinition(false, false, new Ingredient(3002, 2));
            map[3004] = new RecipeDefinition(false, false, new Ingredient(3003, 2));

            // 器械
            map[900002] = new RecipeDefinition(false, false, new Ingredient(900001, 2));

            // 加工
            map[50001] = new RecipeDefinition(false, false,
                new Ingredient(1005, 1),
                new Ingredient(2005, 1),
                new Ingredient(900001, 1));

            map[60001] = new RecipeDefinition(false, false,
                new Ingredient(1005, 1),
                new Ingredient(3004, 1));

            map[70001] = new RecipeDefinition(false, false,
                new Ingredient(60001, 1),
                new Ingredient(900002, 1));

            return map;
        }

        private static void AddMergeChain(Dictionary<int, RecipeDefinition> map, int startId, int endId, bool isMaterial)
        {
            map[startId] = new RecipeDefinition(isMaterial, true);

            for (int itemId = startId + 1; itemId <= endId; itemId++)
            {
                map[itemId] = new RecipeDefinition(isMaterial, false, new Ingredient(itemId - 1, 2));
            }
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
        public int NeededCount { get; private set; }
        public bool IsLeaf => Children == null || Children.Count == 0;
        
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
        
        public void SetOwn(bool isOwn)
        {
            IsOwn = isOwn;
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
        }
    }
    
    
}