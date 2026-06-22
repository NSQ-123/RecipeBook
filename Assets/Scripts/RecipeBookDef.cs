using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IRecipeNode
{
    ERecipeBookType RecipeBookType { get; }
}


public enum ERecipeBookType
{
    Normal =1,
    Compact = 2,
}


/// <summary>
/// Ingredient requirement entry in a recipe definition.
/// </summary>
public struct Ingredient
{
    public readonly int ItemId;
    public readonly int Count;
    public readonly int Priority;

    public Ingredient(int itemId, int count,int priority)
    {
        ItemId = itemId;
        Count = count;
        Priority = priority;
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
    public readonly float Score;

    public RecipeDefinition(bool isMaterial, bool isBaseMaterial,float score ,params Ingredient[] inputs)
        : this(isMaterial, isBaseMaterial, false,score, inputs)
    {
    }

    public RecipeDefinition(bool isMaterial, bool isBaseMaterial, bool isProcessingTool,float score, params Ingredient[] inputs)
    {
        IsMaterial = isMaterial;
        IsBaseMaterial = isBaseMaterial;
        IsProcessingTool = isProcessingTool;
        Score = score;
        Inputs = new List<Ingredient>(inputs);
        Inputs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
}

/// <summary>
/// Runtime recipe tree node used for ownership marking and need calculation.
/// </summary>
public class RecipeNode : IRecipeNode
{
    public int Id { get; private set; }
    public bool IsMaterial { get; private set; }
    public bool IsBaseMaterial { get; private set; }
    public bool IsProcessingTool { get; private set; }
    public int ChildCount { get; private set; }
    public RecipeNode Parent { get; private set; }
    public List<RecipeNode> Children { get; private set; }
    public bool IsOwn { get; private set; }
    public bool IsOwnDirect { get; private set; }
    public int NeededCount { get; private set; }
    public float Score { get; private set; }

    public RecipeNode(int itemId, bool isMaterial, bool isBaseMaterial,float score ,bool isProcessingTool = false)
    {
        Id = itemId;
        IsMaterial = isMaterial;
        IsBaseMaterial = isBaseMaterial;
        Score = score;
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
        Score = 0;
    }

    public ERecipeBookType RecipeBookType => ERecipeBookType.Normal;
}

public sealed class RecipeCompactNode : IRecipeNode
{
    public int Id;
    public bool IsMaterial;
    public bool IsBaseMaterial;
    public bool IsProcessingTool;
    public int NeededCount;
    public int OwnedDirectCount;
    public int OwnedInheritedCount;
    public RecipeCompactNode Parent;
    public List<RecipeCompactNode> Children;
    public float Score;

    public int OwnedCount => OwnedDirectCount + OwnedInheritedCount;
    public bool IsFullyOwned => OwnedCount >= NeededCount;
    public ERecipeBookType RecipeBookType => ERecipeBookType.Compact;
}

/// <summary>
/// Budget for score-based recipe item collection.
/// Constrains collection by remaining score, item count, and an optional reward-pool whitelist.
/// </summary>
public class RecipeCollectBudget
{
    private const float ScoreEpsilon = 1e-5f;

    /// <summary>Remaining score that can be spent collecting items.</summary>
    public float RemainingScore;

    /// <summary>Remaining number of item units that may be collected.</summary>
    public int RemainingCount;

    /// <summary>
    /// Whitelist of item IDs that are eligible for collection.
    /// <c>null</c> means no restriction — all items are eligible.
    /// </summary>
    public ISet<int> RewardPool;

    /// <summary>Returns true when no further items can be collected.</summary>
    public bool IsExhausted => RemainingCount <= 0 || RemainingScore <= 0f;

    public RecipeCollectBudget(float totalScore, int totalCount, ISet<int> rewardPool = null)
    {
        RemainingScore = totalScore;
        RemainingCount = totalCount;
        RewardPool = rewardPool;
    }

    /// <summary>
    /// Try to collect up to <paramref name="want"/> units of <paramref name="itemId"/>
    /// with per-unit cost <paramref name="itemScore"/>.
    /// Deducts from the budget and returns how many were actually collected.
    /// Returns 0 (no deduction) when:
    ///   • item is not in RewardPool,
    ///   • itemScore is zero/negative,
    ///   • remaining score cannot afford even one unit, or
    ///   • budget is already exhausted.
    /// </summary>
    public int TryCollect(int itemId, float itemScore, int want)
    {
        if (IsExhausted || want <= 0) return 0;
        if (RewardPool != null && !RewardPool.Contains(itemId)) return 0;
        if (itemScore <= 0f || itemScore > RemainingScore + ScoreEpsilon) return 0;

        int affordable = (int)((RemainingScore + ScoreEpsilon) / itemScore);
        int actual = want < affordable ? want : affordable;
        actual = actual < RemainingCount ? actual : RemainingCount;
        if (actual <= 0) return 0;

        RemainingScore -= actual * itemScore;
        if (RemainingScore < 0f && RemainingScore > -ScoreEpsilon)
        {
            RemainingScore = 0f;
        }
        RemainingCount -= actual;
        return actual;
    }
}
