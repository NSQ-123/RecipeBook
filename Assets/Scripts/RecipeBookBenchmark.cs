// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Text;
// using UnityEngine;
// using Debug = UnityEngine.Debug;

// namespace Game
// {
//     public class RecipeBookBenchmark : MonoBehaviour
//     {
//         public int Iterations = 500;

//         private readonly struct ScenarioBenchmark
//         {
//             public readonly string ScenarioName;
//             public readonly BenchmarkResult Legacy;
//             public readonly BenchmarkResult Optimized;
//             public readonly BenchmarkResult Compact;

//             public ScenarioBenchmark(string scenarioName, BenchmarkResult legacy, BenchmarkResult optimized, BenchmarkResult compact)
//             {
//                 ScenarioName = scenarioName;
//                 Legacy = legacy;
//                 Optimized = optimized;
//                 Compact = compact;
//             }
//         }

//         [ContextMenu("Run Benchmark")]
//         public void RunBenchmark()
//         {
//             RecipeBook.ClearRecipes();
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;

//             var scenarios = new List<Scenario>
//             {
//                 new Scenario("1005/basic", 1005, new Dictionary<int, int>
//                 {
//                     { 1001, 1 },
//                     { 1002, 1 },
//                     { 1003, 1 }
//                 }),
//                 new Scenario("3004/mixed", 3004, new Dictionary<int, int>
//                 {
//                     { 1001, 1 },
//                     { 1002, 1 },
//                     { 1003, 1 },
//                     { 3003, 1 },
//                     { 3002, 1 },
//                     { 2002, 1 }
//                 }),
//                 new Scenario("70001/process", 70001, new Dictionary<int, int>
//                 {
//                     { 1005, 1 },
//                     { 2005, 1 },
//                     { 900001, 1 },
//                     { 3004, 1 }
//                 })
//             };

//             var sb = new StringBuilder();
//             sb.AppendLine("=== Recipe Benchmark ===");
//             sb.AppendLine("Iterations: " + Iterations);

//             var results = new List<ScenarioBenchmark>(scenarios.Count);

//             for (int i = 0; i < scenarios.Count; i++)
//             {
//                 Scenario scenario = scenarios[i];

//                 BenchmarkResult legacy = MeasureLegacy(scenario, Iterations);
//                 BenchmarkResult optimized = MeasureOptimized(scenario, Iterations);
//                 BenchmarkResult compact = MeasureCompact(scenario, Iterations);
//                 results.Add(new ScenarioBenchmark(scenario.Name, legacy, optimized, compact));

//                 sb.AppendLine("Scenario: " + scenario.Name);
//                 sb.AppendLine("- Legacy   : " + legacy.ElapsedMs.ToString("F2") + " ms, mem=" + legacy.ManagedDeltaBytes + " B, gen0=" + legacy.Gen0Collections + ", checksum=" + legacy.Checksum);
//                 sb.AppendLine("- Optimizer: " + optimized.ElapsedMs.ToString("F2") + " ms, mem=" + optimized.ManagedDeltaBytes + " B, gen0=" + optimized.Gen0Collections + ", checksum=" + optimized.Checksum);
//                 sb.AppendLine("- Compact  : " + compact.ElapsedMs.ToString("F2") + " ms, mem=" + compact.ManagedDeltaBytes + " B, gen0=" + compact.Gen0Collections + ", checksum=" + compact.Checksum);
//             }

//             Debug.Log(sb.ToString());
//         }

//         [ContextMenu("Run Benchmark CSV")]
//         public void RunBenchmarkCsv()
//         {
//             RecipeBook.ClearRecipes();
//             RecipeBook.TryRecipeResolver = RecipeBookTestProvider.TryResolve;

//             var scenarios = new List<Scenario>
//             {
//                 new Scenario("1005/basic", 1005, new Dictionary<int, int>
//                 {
//                     { 1001, 1 },
//                     { 1002, 1 },
//                     { 1003, 1 }
//                 }),
//                 new Scenario("3004/mixed", 3004, new Dictionary<int, int>
//                 {
//                     { 1001, 1 },
//                     { 1002, 1 },
//                     { 1003, 1 },
//                     { 3003, 1 },
//                     { 3002, 1 },
//                     { 2002, 1 }
//                 }),
//                 new Scenario("70001/process", 70001, new Dictionary<int, int>
//                 {
//                     { 1005, 1 },
//                     { 2005, 1 },
//                     { 900001, 1 },
//                     { 3004, 1 }
//                 })
//             };

//             var results = new List<ScenarioBenchmark>(scenarios.Count);
//             for (int i = 0; i < scenarios.Count; i++)
//             {
//                 Scenario scenario = scenarios[i];
//                 BenchmarkResult legacy = MeasureLegacy(scenario, Iterations);
//                 BenchmarkResult optimized = MeasureOptimized(scenario, Iterations);
//                 BenchmarkResult compact = MeasureCompact(scenario, Iterations);
//                 results.Add(new ScenarioBenchmark(scenario.Name, legacy, optimized, compact));
//             }

//             string csv = BuildCsv(results, Iterations);
//             string fileName = "recipe_benchmark_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
//             string path = Path.Combine(Application.persistentDataPath, fileName);
//             File.WriteAllText(path, csv, Encoding.UTF8);
//             Debug.Log("Benchmark CSV written: " + path + "\n" + csv);
//         }

//         private static string BuildCsv(List<ScenarioBenchmark> results, int iterations)
//         {
//             var sb = new StringBuilder();
//             sb.AppendLine("iterations," + iterations);
//             sb.AppendLine("scenario,variant,elapsed_ms,managed_delta_bytes,gen0_collections,checksum");

//             for (int i = 0; i < results.Count; i++)
//             {
//                 ScenarioBenchmark row = results[i];
//                 AppendCsvRow(sb, row.ScenarioName, "legacy", row.Legacy);
//                 AppendCsvRow(sb, row.ScenarioName, "optimizer", row.Optimized);
//                 AppendCsvRow(sb, row.ScenarioName, "compact", row.Compact);
//             }

//             return sb.ToString();
//         }

//         private static void AppendCsvRow(StringBuilder sb, string scenario, string variant, BenchmarkResult r)
//         {
//             sb.Append(scenario).Append(',')
//                 .Append(variant).Append(',')
//                 .Append(r.ElapsedMs.ToString("F3")).Append(',')
//                 .Append(r.ManagedDeltaBytes).Append(',')
//                 .Append(r.Gen0Collections).Append(',')
//                 .Append(r.Checksum)
//                 .AppendLine();
//         }

//         private static BenchmarkResult MeasureLegacy(Scenario scenario, int iterations)
//         {
//             WarmupLegacy(scenario);
//             PrepareForMeasure();

//             long checksum = 0;
//             int gen0Before = GC.CollectionCount(0);
//             long memBefore = GC.GetTotalMemory(true);
//             var sw = Stopwatch.StartNew();
//             for (int i = 0; i < iterations; i++)
//             {
//                 RecipeNode root = RecipeBook.BuildRecipeTree(scenario.ItemId, 1, scenario.Owned);
//                 var needed = RecipeBook.CollectNeededItems(root);
//                 var baseNeeded = RecipeBook.CollectNeededBaseMaterials(root);
//                 checksum += CalculateChecksum(needed, baseNeeded);
//             }

//             sw.Stop();
//             long memAfter = GC.GetTotalMemory(false);
//             int gen0After = GC.CollectionCount(0);
//             return new BenchmarkResult(sw.Elapsed.TotalMilliseconds, checksum, memAfter - memBefore, gen0After - gen0Before);
//         }

//         private static BenchmarkResult MeasureOptimized(Scenario scenario, int iterations)
//         {
//             WarmupOptimizer(scenario);
//             PrepareForMeasure();

//             long checksum = 0;
//             int gen0Before = GC.CollectionCount(0);
//             long memBefore = GC.GetTotalMemory(true);
//             var sw = Stopwatch.StartNew();
//             for (int i = 0; i < iterations; i++)
//             {
//                 RecipeNode root = RecipeBook.BuildRecipeTree(scenario.ItemId, 1, scenario.Owned);
//                 var needed = RecipeBookOptimizer.CollectNeededItems(root);
//                 var baseNeeded = RecipeBookOptimizer.CollectNeededBaseMaterials(root);
//                 checksum += CalculateChecksum(needed, baseNeeded);
//             }

//             sw.Stop();
//             long memAfter = GC.GetTotalMemory(false);
//             int gen0After = GC.CollectionCount(0);
//             return new BenchmarkResult(sw.Elapsed.TotalMilliseconds, checksum, memAfter - memBefore, gen0After - gen0Before);
//         }

//         private static BenchmarkResult MeasureCompact(Scenario scenario, int iterations)
//         {
//             WarmupCompact(scenario);
//             PrepareForMeasure();

//             long checksum = 0;
//             int gen0Before = GC.CollectionCount(0);
//             long memBefore = GC.GetTotalMemory(true);
//             var sw = Stopwatch.StartNew();
//             for (int i = 0; i < iterations; i++)
//             {
//                 RecipeBookCompact.CompactNode root = RecipeBookCompact.BuildTree(scenario.ItemId, 1);
//                 RecipeBookCompact.MarkOwned(root, scenario.Owned);
//                 var needed = RecipeBookCompact.CollectNeeded(root);
//                 var baseNeeded = RecipeBookCompact.CollectNeededBase(root);
//                 checksum += CalculateChecksum(needed, baseNeeded);
//                 RecipeBookCompact.ReleaseTree(root);
//             }

//             sw.Stop();
//             long memAfter = GC.GetTotalMemory(false);
//             int gen0After = GC.CollectionCount(0);
//             return new BenchmarkResult(sw.Elapsed.TotalMilliseconds, checksum, memAfter - memBefore, gen0After - gen0Before);
//         }

//         private static void PrepareForMeasure()
//         {
//             GC.Collect();
//             GC.WaitForPendingFinalizers();
//             GC.Collect();
//         }

//         private static void WarmupLegacy(Scenario scenario)
//         {
//             RecipeNode root = RecipeBook.BuildRecipeTree(scenario.ItemId, 1, scenario.Owned);
//             RecipeBook.CollectNeededItems(root);
//             RecipeBook.CollectNeededBaseMaterials(root);
//         }

//         private static void WarmupOptimizer(Scenario scenario)
//         {
//             RecipeNode root = RecipeBook.BuildRecipeTree(scenario.ItemId, 1, scenario.Owned);
//             RecipeBookOptimizer.CollectNeededItems(root);
//             RecipeBookOptimizer.CollectNeededBaseMaterials(root);
//         }

//         private static void WarmupCompact(Scenario scenario)
//         {
//             RecipeBookCompact.CompactNode root = RecipeBookCompact.BuildTree(scenario.ItemId, 1);
//             RecipeBookCompact.MarkOwned(root, scenario.Owned);
//             RecipeBookCompact.CollectNeeded(root);
//             RecipeBookCompact.CollectNeededBase(root);
//             RecipeBookCompact.ReleaseTree(root);
//         }

//         private static long CalculateChecksum(Dictionary<int, int> a, Dictionary<int, int> b)
//         {
//             return CalculateChecksum(a) * 31L + CalculateChecksum(b);
//         }

//         private static long CalculateChecksum(Dictionary<int, int> map)
//         {
//             if (map == null)
//             {
//                 return 0;
//             }

//             long sum = 0;
//             foreach (KeyValuePair<int, int> pair in map)
//             {
//                 sum += pair.Key * 397L + pair.Value;
//             }

//             return sum;
//         }

//         private readonly struct Scenario
//         {
//             public readonly string Name;
//             public readonly int ItemId;
//             public readonly Dictionary<int, int> Owned;

//             public Scenario(string name, int itemId, Dictionary<int, int> owned)
//             {
//                 Name = name;
//                 ItemId = itemId;
//                 Owned = owned;
//             }
//         }

//         private readonly struct BenchmarkResult
//         {
//             public readonly double ElapsedMs;
//             public readonly long Checksum;
//             public readonly long ManagedDeltaBytes;
//             public readonly int Gen0Collections;

//             public BenchmarkResult(double elapsedMs, long checksum, long managedDeltaBytes, int gen0Collections)
//             {
//                 ElapsedMs = elapsedMs;
//                 Checksum = checksum;
//                 ManagedDeltaBytes = managedDeltaBytes;
//                 Gen0Collections = gen0Collections;
//             }
//         }
//     }
// }

