  using System.Collections.Generic;

namespace Game
{
    public static class RecipeBookTestProvider
    {
        private static readonly Dictionary<int, RecipeDefinition> s_testRecipes = CreateRecipes();


        public static bool TryResolve(int itemId, out RecipeDefinition recipe)
        {
            return s_testRecipes.TryGetValue(itemId, out recipe);
        }

        private static Dictionary<int, RecipeDefinition> CreateRecipes()
        {
            var map = new Dictionary<int, RecipeDefinition>();

            AddMergeChain(map, 1001, 1010, true);
            AddMergeChain(map, 2001, 2005, true);

            map[3001] = new RecipeDefinition(false, false, new Ingredient(2003, 1));
            map[3002] = new RecipeDefinition(false, false, new Ingredient(3001, 2));
            map[3003] = new RecipeDefinition(false, false, new Ingredient(3002, 2));
            map[3004] = new RecipeDefinition(false, false, new Ingredient(3003, 2));

            map[900002] = new RecipeDefinition(false, false, new Ingredient(900001, 2));

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
}

