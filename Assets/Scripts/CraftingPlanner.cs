using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [Serializable]
    public sealed class Recipe
    {
        public int OutputId;
        public int OutputCount;
        public Dictionary<int, int> Inputs;
        public int ToolId;
        public bool ToolConsumed;

        public Recipe(int outputId, int outputCount, Dictionary<int, int> inputs, int toolId = 0, bool toolConsumed = false)
        {
            OutputId = outputId;
            OutputCount = outputCount;
            Inputs = inputs;
            ToolId = toolId;
            ToolConsumed = toolConsumed;
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
        private readonly HashSet<int> _materialItems;
        private readonly HashSet<int> _baseMaterials;

        public CraftingPlanner(Dictionary<int, Recipe> recipes, HashSet<int> materialItems, HashSet<int> baseMaterials)
        {
            _recipes = recipes;
            _materialItems = materialItems;
            _baseMaterials = baseMaterials;
        }

        public PlanResult Plan(int targetId, int targetCount, Dictionary<int, int> inventory)
        {
            var inv = new Dictionary<int, int>(inventory);
            var missingMaterials = new Dictionary<int, int>();

            ResolveNeed(targetId, targetCount, inv, missingMaterials, new HashSet<int>(), _materialItems);

            var missingBase = new Dictionary<int, int>();
            var invForBase = new Dictionary<int, int>(inventory);
            ResolveNeed(targetId, targetCount, invForBase, missingBase, new HashSet<int>(), _baseMaterials);

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
            HashSet<int> directAddableItems)
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

            // Only material items can be directly added by the player.
            if (directAddableItems.Contains(itemId))
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
                    ResolveNeed(recipe.ToolId, 1, inventory, missingMaterials, trace, directAddableItems);
                }
            }

            foreach (var input in recipe.Inputs)
            {
                ResolveNeed(input.Key, input.Value * times, inventory, missingMaterials, trace, directAddableItems);
            }

            if (recipe.ToolId != 0 && recipe.ToolConsumed)
            {
                ResolveNeed(recipe.ToolId, times, inventory, missingMaterials, trace, directAddableItems);
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
            recipes[1002] = new Recipe(1002, 1, new Dictionary<int, int> { { 1001, 2 } });
            recipes[1003] = new Recipe(1003, 1, new Dictionary<int, int> { { 1002, 2 } });
            recipes[1004] = new Recipe(1004, 1, new Dictionary<int, int> { { 1003, 2 } });
            recipes[1005] = new Recipe(1005, 1, new Dictionary<int, int> { { 1004, 2 } });
            recipes[1006] = new Recipe(1006, 1, new Dictionary<int, int> { { 1005, 2 } });
            recipes[1007] = new Recipe(1007, 1, new Dictionary<int, int> { { 1006, 2 } });
            recipes[1008] = new Recipe(1008, 1, new Dictionary<int, int> { { 1007, 2 } });
            recipes[1009] = new Recipe(1009, 1, new Dictionary<int, int> { { 1008, 2 } });
            recipes[1010] = new Recipe(1010, 1, new Dictionary<int, int> { { 1009, 2 } });

            // 200x chain
            recipes[2002] = new Recipe(2002, 1, new Dictionary<int, int> { { 2001, 2 } });
            recipes[2003] = new Recipe(2003, 1, new Dictionary<int, int> { { 2002, 2 } });
            recipes[2004] = new Recipe(2004, 1, new Dictionary<int, int> { { 2003, 2 } });
            recipes[2005] = new Recipe(2005, 1, new Dictionary<int, int> { { 2004, 2 } });

            // Convert chain
            recipes[3001] = new Recipe(3001, 1, new Dictionary<int, int> { { 2003, 1 } });
            recipes[3002] = new Recipe(3002, 1, new Dictionary<int, int> { { 3001, 2 } });
            recipes[3003] = new Recipe(3003, 1, new Dictionary<int, int> { { 3002, 2 } });
            recipes[3004] = new Recipe(3004, 1, new Dictionary<int, int> { { 3003, 2 } });

            // Tools and processed items
            recipes[900002] = new Recipe(900002, 1, new Dictionary<int, int> { { 900001, 2 } });
            recipes[50001] = new Recipe(50001, 1, new Dictionary<int, int> { { 1005, 1 }, { 2005, 1 } }, 900001, false);
            recipes[60001] = new Recipe(60001, 1, new Dictionary<int, int> { { 1005, 1 }, { 3004, 1 } });
            recipes[70001] = new Recipe(70001, 1, new Dictionary<int, int> { { 60001, 1 } }, 900002, false);

            var materialItems = new HashSet<int>
            {
                1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010,
                2001, 2002, 2003, 2004, 2005
            };

            var baseMaterials = new HashSet<int> { 1001, 2001 };

            return new CraftingPlanner(recipes, materialItems, baseMaterials);
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


