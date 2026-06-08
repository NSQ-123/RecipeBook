  using System.Collections.Generic;

namespace Game
{
    public static class RecipeBookTestProvider
    {
        private static readonly Dictionary<int, RecipeDefinition> s_testRecipes = CreateRecipes1();


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

            map[900002] = new RecipeDefinition(false, false, true, new Ingredient(900001, 2));

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
            
            map[900001] = new RecipeDefinition(false, false, true);
            
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

        private static Dictionary<int, RecipeDefinition> CreateRecipes1()
        {
            var map = new Dictionary<int, RecipeDefinition>();

            map[500058] = new RecipeDefinition(false, false,
                new Ingredient(201505, 1),
                new Ingredient(200703, 1),
                new Ingredient(200802, 1));

            map[201505] = new RecipeDefinition(true, false, new Ingredient(201504, 2));
            map[201504] = new RecipeDefinition(true, false, new Ingredient(201503, 2));
            map[201503] = new RecipeDefinition(true, false, new Ingredient(201502, 2));
            map[201502] = new RecipeDefinition(true, false, new Ingredient(201501, 2));
            map[201501] = new RecipeDefinition(true, true);

            map[200703] = new RecipeDefinition(true, false, new Ingredient(200702, 2));
            map[200702] = new RecipeDefinition(true, false, new Ingredient(200701, 2));
            map[200701] = new RecipeDefinition(true, true);

            map[200802] = new RecipeDefinition(false, false, new Ingredient(200801, 2));
            map[200801] = new RecipeDefinition(true, true, new Ingredient(200702, 1));

            map[200003] = new RecipeDefinition(true, false, new Ingredient(200002, 2));
            map[200002] = new RecipeDefinition(true, false, new Ingredient(200001, 2));
            map[200001] = new RecipeDefinition(true, true);

            return map;
        }
        
        
    }
}

