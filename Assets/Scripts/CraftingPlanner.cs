using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [Serializable]
    public readonly struct Ingredient
    {
        public readonly int ItemId;
        public readonly int Count;

        public Ingredient(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    [Serializable]
    public readonly struct ItemMeta
    {
        public readonly int ItemId;
        public readonly bool IsMaterial;
        public readonly bool IsBaseMaterial;

        public ItemMeta(int itemId, bool isMaterial, bool isBaseMaterial)
        {
            ItemId = itemId;
            IsMaterial = isMaterial;
            IsBaseMaterial = isBaseMaterial;
        }
    }

    [Serializable]
    public sealed class Recipe
    {
        public int OutputId;
        public int OutputCount;
        public List<Ingredient> Inputs;
        public int ToolId;
        public bool ToolConsumed;

        public Recipe(int outputId, int outputCount, List<Ingredient> inputs, int toolId, bool toolConsumed)
        {
            OutputId = outputId;
            OutputCount = outputCount;
            Inputs = inputs;
            ToolId = toolId;
            ToolConsumed = toolConsumed;
        }

        public Recipe(int outputId, int outputCount, params Ingredient[] inputs)
            : this(outputId, outputCount, new List<Ingredient>(inputs), 0, false)
        {
        }

        public Recipe(int outputId, int outputCount, int toolId, bool toolConsumed, params Ingredient[] inputs)
            : this(outputId, outputCount, new List<Ingredient>(inputs), toolId, toolConsumed)
        {
        }
    }

    public sealed class PlanResult
    {
        public readonly Dictionary<int, int> MissingMaterials = new Dictionary<int, int>();
        public readonly Dictionary<int, int> MissingBaseMaterials = new Dictionary<int, int>();

        public override string ToString()
        {
            return $"MissingMaterials: {Format(MissingMaterials)} | MissingBaseMaterials: {Format(MissingBaseMaterials)}";
        }

        private static string Format(Dictionary<int, int> map)
        {
            if (map.Count == 0)
            {
                return "none";
            }

            var parts = new List<string>();
            foreach (var kv in map)
            {
                parts.Add($"{kv.Key}x{kv.Value}");
            }

            return string.Join(", ", parts);
        }
    }


    public sealed class CraftingPlanner
    {
        private readonly Dictionary<int, Recipe> _recipes;
        private readonly Dictionary<int, ItemMeta> _itemMeta;

        public CraftingPlanner(Dictionary<int, Recipe> recipes, Dictionary<int, ItemMeta> itemMeta)
        {
            _recipes = recipes;
            _itemMeta = itemMeta;
        }

        public PlanResult Plan(int targetId, int targetCount, Dictionary<int, int> inventory)
        {
            var inv = new Dictionary<int, int>(inventory);
            var missingMaterials = new Dictionary<int, int>();

            ResolveNeed(targetId, targetCount, inv, missingMaterials, new HashSet<int>(), false);

            var missingBase = new Dictionary<int, int>();
            var invForBase = new Dictionary<int, int>(inventory);
            ResolveNeed(targetId, targetCount, invForBase, missingBase, new HashSet<int>(), true);

            var result = new PlanResult();
            CopyTo(result.MissingMaterials, missingMaterials);
            CopyTo(result.MissingBaseMaterials, missingBase);
            return result;
        }

        private void ResolveNeed(
            int itemId,
            int needCount,
            Dictionary<int, int> inventory,
            Dictionary<int, int> missingMaterials,
            HashSet<int> trace,
            bool baseOnly)
        {
            if (needCount <= 0)
            {
                return;
            }

            int used = ConsumeInventory(itemId, needCount, inventory);
            int remaining = needCount - used;
            if (remaining <= 0)
            {
                return;
            }

            // Table-driven item metadata decides whether player can directly add this item.
            if (CanDirectAdd(itemId, baseOnly))
            {
                Add(missingMaterials, itemId, remaining);
                return;
            }

            if (!_recipes.TryGetValue(itemId, out var recipe))
            {
                throw new InvalidOperationException($"No recipe and not a material item: {itemId}");
            }

            if (!trace.Add(itemId))
            {
                throw new InvalidOperationException($"Recipe cycle detected at item: {itemId}");
            }

            int times = CeilDiv(remaining, recipe.OutputCount);

            // If the tool is consumable, it is part of inputs; if not, one tool instance is required.
            if (recipe.ToolId != 0 && !recipe.ToolConsumed)
            {
                int haveTool = GetCount(inventory, recipe.ToolId);
                if (haveTool <= 0)
                {
                    // Non-consumable tool missing: resolve one required tool.
                    ResolveNeed(recipe.ToolId, 1, inventory, missingMaterials, trace, baseOnly);
                }
            }

            foreach (var input in recipe.Inputs)
            {
                ResolveNeed(input.ItemId, input.Count * times, inventory, missingMaterials, trace, baseOnly);
            }

            if (recipe.ToolId != 0 && recipe.ToolConsumed)
            {
                ResolveNeed(recipe.ToolId, times, inventory, missingMaterials, trace, baseOnly);
            }

            int produced = times * recipe.OutputCount;
            int extra = produced - remaining;
            if (extra > 0)
            {
                Add(inventory, itemId, extra);
            }

            trace.Remove(itemId);
        }

        private static int ConsumeInventory(int itemId, int count, Dictionary<int, int> inventory)
        {
            int have = GetCount(inventory, itemId);
            int use = Math.Min(have, count);
            if (use > 0)
            {
                inventory[itemId] = have - use;
            }

            return use;
        }

        private static int GetCount(Dictionary<int, int> map, int itemId)
        {
            return map.TryGetValue(itemId, out var value) ? value : 0;
        }

        private static void Add(Dictionary<int, int> map, int itemId, int delta)
        {
            if (delta <= 0)
            {
                return;
            }

            map[itemId] = GetCount(map, itemId) + delta;
        }

        private static int CeilDiv(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private bool CanDirectAdd(int itemId, bool baseOnly)
        {
            if (!_itemMeta.TryGetValue(itemId, out var meta))
            {
                return false;
            }

            return baseOnly ? meta.IsBaseMaterial : meta.IsMaterial;
        }

        private static void CopyTo(Dictionary<int, int> target, Dictionary<int, int> source)
        {
            foreach (var kv in source)
            {
                target[kv.Key] = kv.Value;
            }
        }

        public static CraftingPlanner CreateDefault()
        {
            var recipes = new Dictionary<int, Recipe>();

            // 100x chain
            recipes[1002] = new Recipe(1002, 1, new Ingredient(1001, 2));
            recipes[1003] = new Recipe(1003, 1, new Ingredient(1002, 2));
            recipes[1004] = new Recipe(1004, 1, new Ingredient(1003, 2));
            recipes[1005] = new Recipe(1005, 1, new Ingredient(1004, 2));
            recipes[1006] = new Recipe(1006, 1, new Ingredient(1005, 2));
            recipes[1007] = new Recipe(1007, 1, new Ingredient(1006, 2));
            recipes[1008] = new Recipe(1008, 1, new Ingredient(1007, 2));
            recipes[1009] = new Recipe(1009, 1, new Ingredient(1008, 2));
            recipes[1010] = new Recipe(1010, 1, new Ingredient(1009, 2));

            // 200x chain
            recipes[2002] = new Recipe(2002, 1, new Ingredient(2001, 2));
            recipes[2003] = new Recipe(2003, 1, new Ingredient(2002, 2));
            recipes[2004] = new Recipe(2004, 1, new Ingredient(2003, 2));
            recipes[2005] = new Recipe(2005, 1, new Ingredient(2004, 2));

            // Convert chain
            recipes[3001] = new Recipe(3001, 1, new Ingredient(2003, 1));
            recipes[3002] = new Recipe(3002, 1, new Ingredient(3001, 2));
            recipes[3003] = new Recipe(3003, 1, new Ingredient(3002, 2));
            recipes[3004] = new Recipe(3004, 1, new Ingredient(3003, 2));

            // Tools and processed items
            recipes[900002] = new Recipe(900002, 1, new Ingredient(900001, 2));
            recipes[50001] = new Recipe(50001, 1, 900001, false, new Ingredient(1005, 1), new Ingredient(2005, 1));
            recipes[60001] = new Recipe(60001, 1, new Ingredient(1005, 1), new Ingredient(3004, 1));
            recipes[70001] = new Recipe(70001, 1, 900002, false, new Ingredient(60001, 1));

            var itemMeta = new Dictionary<int, ItemMeta>
            {
                [1001] = new ItemMeta(1001, true, true),
                [1002] = new ItemMeta(1002, true, false),
                [1003] = new ItemMeta(1003, true, false),
                [1004] = new ItemMeta(1004, true, false),
                [1005] = new ItemMeta(1005, true, false),
                [1006] = new ItemMeta(1006, true, false),
                [1007] = new ItemMeta(1007, true, false),
                [1008] = new ItemMeta(1008, true, false),
                [1009] = new ItemMeta(1009, true, false),
                [1010] = new ItemMeta(1010, true, false),
                [2001] = new ItemMeta(2001, true, true),
                [2002] = new ItemMeta(2002, true, false),
                [2003] = new ItemMeta(2003, true, false),
                [2004] = new ItemMeta(2004, true, false),
                [2005] = new ItemMeta(2005, true, false),
                [3001] = new ItemMeta(3001, false, false),
                [3002] = new ItemMeta(3002, false, false),
                [3003] = new ItemMeta(3003, false, false),
                [3004] = new ItemMeta(3004, false, false),
                [50001] = new ItemMeta(50001, false, false),
                [60001] = new ItemMeta(60001, false, false),
                [70001] = new ItemMeta(70001, false, false),
                [900001] = new ItemMeta(900001, false, false),
                [900002] = new ItemMeta(900002, false, false)
            };

            return new CraftingPlanner(recipes, itemMeta);
        }
    }

    public class CraftingPlannerDemo : MonoBehaviour
    {
        [ContextMenu("Run 60001 Sample")]
        public void Run60001Sample()
        {
            var planner = CraftingPlanner.CreateDefault();
            var inventory = new Dictionary<int, int>
            {
                { 1001, 1 },
                { 1002, 1 },
                { 1003, 1 },
                { 2001, 1 },
                { 2002, 1 },
                { 2003, 1 },
                { 3002, 1 },
                { 900001, 1 }
            };

            PlanResult result = planner.Plan(60001, 1, inventory);
            Debug.Log("Target 60001 x1");
            Debug.Log($"Need Materials: {FormatDict(result.MissingMaterials)}");
            Debug.Log($"Need Base Materials: {FormatDict(result.MissingBaseMaterials)}");
        }

        private static string FormatDict(Dictionary<int, int> map)
        {
            if (map.Count == 0)
            {
                return "none";
            }

            var ids = new List<int>(map.Keys);
            ids.Sort();
            var parts = new List<string>();
            foreach (var id in ids)
            {
                parts.Add($"{id}x{map[id]}");
            }

            return string.Join(", ", parts);
        }
    }
}


