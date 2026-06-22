using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
// using cfg;
// using TLF;
using UnityEngine;
using UnityEngine.Rendering;

public static class RecipeBookProvider
{
    private static readonly Dictionary<int, RecipeDefinition> s_Recipes = new();
    private static readonly HashSet<int> s_Series = new();
    // public static bool TryResolve(int itemId, out RecipeDefinition recipe)
    // {
    //     if(s_Recipes.TryGetValue(itemId, out recipe)) return true;
    //     AddRecipeDefinition(itemId);
    //     return s_Recipes.TryGetValue(itemId, out recipe);
    // }

    // private static void AddRecipeDefinition(int itemId)
    // {
    //     ConfigManager.Instance.Tables.TbLevelGoodsBase.DataMap.TryGetValue(itemId, out var levelGoodsBase);
    //     if (levelGoodsBase == null) return;
    //     var isFromCooking = IsFromCooking(levelGoodsBase);
    //     if (isFromCooking)
    //     {
    //         AddProductionRecipe(levelGoodsBase);
    //     }
    //     else
    //     {
    //         //这里通过Type进行不同的处理。默认都使用合成链
    //         AddMergeChain(levelGoodsBase);
    //     }
        
    // }

    // #region 合成链

    // private static void AddMergeChain(int itemId)
    // {
    //     ConfigManager.Instance.Tables.TbLevelGoodsBase.DataMap.TryGetValue(itemId, out var levelGoodsBase);
    //     if (levelGoodsBase == null) return;
    //     AddMergeChain(levelGoodsBase);
    // }
    // private static void AddMergeChain(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     if(s_Series.Contains(levelGoodsBase.Series)) return;
        
    //     //获取整个系列的物品，并添加进去
    //     var seriesList = GameLevelManager.Instance.GetGoodsBaseVosBySeries(levelGoodsBase.Series);

    //     foreach (var seriesItem in seriesList)
    //     {
    //         var isMaterial = IsMaterial(seriesItem); 
    //         var isFromResult=IsFrom(seriesItem);
    //         var isBaseMaterial = isMaterial && isFromResult.SourceType == ESourceType.Creator && seriesItem.Level == 1;
    //         var isProcessingTool = IsProcessingTool(seriesItem);
    //         var score = seriesItem.Goal;
    //         if (seriesItem.Level >= 2)
    //         {
    //             var lowerGoods = GameLevelManager.Instance.GetGoodsBaseVoBySeriesAndLevel(seriesItem.Series, seriesItem.Level - 1);
    //             var inputs = new[] { new Ingredient(lowerGoods.GoodsID, 2,lowerGoods.Series) };
    //             var recipe = new RecipeDefinition(isMaterial, isBaseMaterial, isProcessingTool, score, inputs);
    //             s_Recipes[seriesItem.GoodsID] = recipe;
    //         }
    //         else
    //         {
    //             // 等级为1的物品
    //             //转化来的物品要设置 inputs
    //             if(isFromResult.SourceType == ESourceType.Conversion)
    //             {
    //                 var inputs = new[] { new Ingredient(isFromResult.SourceId, 1,isFromResult.Series) };
    //                 var recipe = new RecipeDefinition(isMaterial,isBaseMaterial, score,inputs);
    //                 s_Recipes[seriesItem.GoodsID] = recipe;
    //             }
    //             else
    //             {
    //                 var recipe = new RecipeDefinition(isMaterial,isBaseMaterial,isProcessingTool,score);
    //                 s_Recipes[seriesItem.GoodsID] = recipe;
    //             }
    //         }
    //     }
    //     // 记录已经添加过的系列，避免重复添加
    //     s_Series.Add(levelGoodsBase.Series);
    // }

    
    // #endregion


    // #region 制作品

    // /// <summary>
    // ///  生产物的配方
    // /// </summary>
    // /// <param name="levelGoodsBase"></param>
    // private static void AddProductionRecipe(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     ConfigManager.Instance.Tables.TbLevelGoodsCooking.DataMap.TryGetValue(levelGoodsBase.GoodsID, out var levelGoodsCooking);
    //     if (levelGoodsCooking == null) return;
    //     var materials = levelGoodsCooking.GetMaterials();
    //     var isTool = GoodsConfigTool.IsInstrumentSeries_MultiUse(levelGoodsCooking.InstrumentType);
        
    //     var count =isTool? materials.Count : materials.Count + 1;
    //     Ingredient[] inputs = new Ingredient[count];
    //     var index = 0;
    //     foreach (var material in materials)
    //     {
    //         ConfigManager.Instance.Tables.TbLevelGoodsBase.DataMap.TryGetValue(material.id, out var materialGoodsBase);
    //         var series = materialGoodsBase?.Series ?? 0;
    //         inputs[index]= new Ingredient(material.id, material.num, series);
    //         index++;
    //     }
       
    //     //不是工具--看是否是一次性工具
    //     if (!isTool)
    //     {
    //         var seriesList = GameLevelManager.Instance.GetGoodsBaseVosBySeries(levelGoodsCooking.InstrumentType);
    //         var onceToolId = -1;
    //         foreach (var seriesItem in seriesList)
    //         {
    //             var isOnceTool =IsOnceMachine(seriesItem);
    //             if (isOnceTool)
    //             {
    //                 onceToolId = seriesItem.GoodsID;
    //                 break;
    //             }
    //         }
    //         ConfigManager.Instance.Tables.TbLevelGoodsBase.DataMap.TryGetValue(onceToolId, out var onceToolGoodsBase);
    //         var series = onceToolGoodsBase?.Series ?? 0;
    //         inputs[index] = new Ingredient(onceToolId, 1, series);
    //     }

    //     var recipe = new RecipeDefinition(false, false, levelGoodsBase.Goal, inputs);
    //     s_Recipes[levelGoodsBase.GoodsID] = recipe;
    // }

    // #endregion
    
    


    // #region 辅助方法

    // public enum ESourceType
    // {
    //     None = 0, // 未知
    //     Creator, // 生成器
    //     Conversion,// 转化
    // }
    
    
    // private static bool IsMaterial(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return levelGoodsBase.Type == 1 || levelGoodsBase.Type == 3||IsOnceMachine(levelGoodsBase);
    // }
    
    // private static bool IsProcessingTool(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return Tr_LevelGoodsBase.IsPermanentlyInstrument(levelGoodsBase.Type);
    // }
    
    // private static bool IsConversion(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return Tr_LevelGoodsBase.IsInitiativeConversion(levelGoodsBase.Type);
    // }
    
    // private static bool IsCreator(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return Tr_LevelGoodsBase.IsCreator(levelGoodsBase.Type);
    // }
    
    // private static (ESourceType SourceType,int SourceId,int Series) IsFrom(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     var sources = levelGoodsBase.GetSource;
    //     if (sources == null || sources.Count == 0) return (ESourceType.None,-1,-1);
    //     var firstSourceId = sources[0];
    //     ConfigManager.Instance.Tables.TbLevelGoodsBase.DataMap.TryGetValue(firstSourceId, out var firstSourceGoodsBase);
    //     if(firstSourceGoodsBase==null) return (ESourceType.None,-1,-1);
    //     var isFromCreator = IsCreator(firstSourceGoodsBase);
    //     if(isFromCreator) return (ESourceType.Creator,firstSourceId,firstSourceGoodsBase.Series);
    //     var isFromConversion = IsConversion(firstSourceGoodsBase);
    //     if(isFromConversion) return (ESourceType.Conversion,firstSourceId,firstSourceGoodsBase.Series);
    //     return (ESourceType.None, -1,-1);
    // }
    
    // private static bool IsFromCooking(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return levelGoodsBase.ProductionType==1;
    // }
    
    // private static bool IsOnceMachine(Tr_LevelGoodsBase levelGoodsBase)
    // {
    //     return Tr_LevelGoodsBase.IsDisposableMachine(levelGoodsBase.Type);
    // }

    // #endregion


    // #region 打印

    // public static void DebugRecipes()
    // {
    //     var sb = new StringBuilder();
    //     foreach (var recipe in s_Recipes)
    //     {
    //         sb.AppendLine($"{recipe.Key}:{recipe.Value.ToJson()}");
    //     }
    //     Debug.LogError(sb.ToString());
    // }

    // #endregion
}


#region 数据示例

// 500058:{"IsMaterial":false,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200703,"Count":1,"Priority":2007},{"ItemId":200802,"Count":1,"Priority":2008},{"ItemId":201505,"Count":1,"Priority":2015}],"Score":29.763}
// 200701:{"IsMaterial":true,"IsBaseMaterial":true,"IsProcessingTool":false,"Inputs":[],"Score":1.912}
// 200702:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200701,"Count":2,"Priority":2007}],"Score":4.284}
// 200703:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200702,"Count":2,"Priority":2007}],"Score":8.505}
// 200704:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200703,"Count":2,"Priority":2007}],"Score":17.333}
// 200705:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200704,"Count":2,"Priority":2007}],"Score":34.621}
// 200706:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200705,"Count":2,"Priority":2007}],"Score":70.029}
// 200707:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200706,"Count":2,"Priority":2007}],"Score":140.601}
// 200708:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200707,"Count":2,"Priority":2007}],"Score":280.638}
// 200801:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200702,"Count":1,"Priority":2007}],"Score":4.902}
// 200802:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200801,"Count":2,"Priority":2008}],"Score":10.461}
// 200803:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200802,"Count":2,"Priority":2008}],"Score":20.836}
// 200804:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200803,"Count":2,"Priority":2008}],"Score":42.19}
// 200805:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200804,"Count":2,"Priority":2008}],"Score":84.345}
// 200806:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":200805,"Count":2,"Priority":2008}],"Score":171.367}
// 201501:{"IsMaterial":true,"IsBaseMaterial":true,"IsProcessingTool":false,"Inputs":[],"Score":0.574}
// 201502:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201501,"Count":2,"Priority":2015}],"Score":1.187}
// 201503:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201502,"Count":2,"Priority":2015}],"Score":2.474}
// 201504:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201503,"Count":2,"Priority":2015}],"Score":5.307}
// 201505:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201504,"Count":2,"Priority":2015}],"Score":10.626}
// 201506:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201505,"Count":2,"Priority":2015}],"Score":21.427}
// 201507:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201506,"Count":2,"Priority":2015}],"Score":42.602}
// 201508:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201507,"Count":2,"Priority":2015}],"Score":86.852}
// 201509:{"IsMaterial":true,"IsBaseMaterial":false,"IsProcessingTool":false,"Inputs":[{"ItemId":201508,"Count":2,"Priority":2015}],"Score":174.138}


#endregion