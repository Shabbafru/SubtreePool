using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Problems.DataAnalysis.Symbolic.Regression;
using HeuristicLab.Problems.DataAnalysis.Symbolic;
using HeuristicLab.Problems.Instances.DataAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HeuristicLab.Common;
using HeuristicLab.Random;
using HeuristicLab.Algorithms.GeneticAlgorithm;
using HeuristicLab.ParallelEngine;
using HeuristicLab.Data;
using HeuristicLab.Optimization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;

namespace DataGenerator
{
    internal class FlexibleGenerator : GeneratorBase
    {
        private static readonly int maxTreeLength = 100;
        private static readonly int maxTreeDepth = 25;
        private static readonly int minTreeLength = 40;
        private static readonly int retries = 150;

        public override void Generate(int nrOfDatasets, double similarity)
        {
            GenerateModelsFlexible(nrOfDatasets, similarity, string.Empty);
        }

        public static List<string> GenerateModelsFlexible(int nrOfDatasets, double similarity, string prefix)
        {
            var sources = new List<string>();
            var rand = new HeuristicLab.Random.FastRandom();
            var formatter = new CSharpSymbolicExpressionTreeStringFormatter();
            var ys = new List<List<double>>();

            //create a grammar, maybe don't need a problem for that?
            var provider = new FriedmanRandomFunctionInstanceProvider();
            var instance = provider.GetDataDescriptors().Single(x => x.Name == "FriedmanRandomFunction-1% (10 dim)");
            var data = provider.LoadData(instance);
            var symRegProblem = new SymbolicRegressionSingleObjectiveProblem();
            symRegProblem.Load(data);
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Logarithm").Enabled = false;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Exponential").Enabled = false;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Sine").Enabled = true;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Cosine").Enabled = true;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Tangent").Enabled = true;

            IList<ISymbolicExpressionTree> trees = new List<ISymbolicExpressionTree>();
            for (int i = 0; i < nrOfDatasets; i++)
            {
                ISymbolicExpressionTree tree = null;
                int counter = 0;
                do
                {
                    if (tree == null || counter > retries)
                    {
                        if (counter > retries)
                        {
                            Console.WriteLine("Retries exceeded, starting over...-> " + similarity);
                            counter = 0;
                            trees.Clear();
                            i = 0;
                        }
                        tree = CreateNewTreeOfCorrectLength(minTreeLength, similarity, rand, symRegProblem.SymbolicExpressionTreeGrammar);
                        if (trees.Count() == 0)
                        {
                            trees.Add(tree);
                        }
                    }

                    var nodeTypeTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    ChangeNodeTypeManipulation.ChangeNodeType(rand, nodeTypeTree);

                    var onePointTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    OnePointShaker.Shake(rand, onePointTree, 1.0);

                    var replaceBranchTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    ReplaceBranchManipulation.ReplaceRandomBranch(rand, replaceBranchTree, maxTreeLength, maxTreeDepth);

                    var compNewTree = CreateNewTreeOfCorrectLength(minTreeLength, similarity, rand, symRegProblem.SymbolicExpressionTreeGrammar);

                    List<ISymbolicExpressionTree> lst = new List<ISymbolicExpressionTree>();
                    lst.AddRange(new[] { tree, nodeTypeTree, onePointTree, replaceBranchTree, compNewTree });

                    if (trees.Count() > 1)
                    {
                        nodeTypeTree = (ISymbolicExpressionTree)trees.SelectRandom(rand).Clone(new Cloner());
                        ChangeNodeTypeManipulation.ChangeNodeType(rand, nodeTypeTree);

                        onePointTree = (ISymbolicExpressionTree)trees.SelectRandom(rand).Clone(new Cloner());
                        OnePointShaker.Shake(rand, onePointTree, 1.0);

                        replaceBranchTree = (ISymbolicExpressionTree)trees.SelectRandom(rand).Clone(new Cloner());
                        ReplaceBranchManipulation.ReplaceRandomBranch(rand, replaceBranchTree, maxTreeLength, maxTreeDepth);

                        lst.AddRange(new[] { nodeTypeTree, onePointTree, replaceBranchTree });
                    }

                    //apply mutation operators multiple times
                    int rk = rand.Next(50, 800);
                    nodeTypeTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    onePointTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    replaceBranchTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    var allTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                    for (int k = 0; k < rk; k++)
                    {
                        ChangeNodeTypeManipulation.ChangeNodeType(rand, nodeTypeTree);
                        OnePointShaker.Shake(rand, onePointTree, 1.0);
                        ReplaceBranchManipulation.ReplaceRandomBranch(rand, replaceBranchTree, maxTreeLength, maxTreeDepth);

                        ChangeNodeTypeManipulation.ChangeNodeType(rand, allTree);
                        OnePointShaker.Shake(rand, allTree, 1.0);

                    }
                    lst.AddRange(new[] { nodeTypeTree, onePointTree, replaceBranchTree, allTree });

                    List<double> sims = lst.Select(t => CalcAvgSim(trees, t)).ToList();

                    ISymbolicExpressionTree tmp = null;
                    double min = double.MaxValue;

                    for (int j = 0; j < lst.Count; j++)
                    {
                        if (Math.Abs(similarity - sims[j]) < min)
                        {
                            min = Math.Abs(similarity - sims[j]);
                            tmp = lst[j];
                        }
                    }
                    tree = tmp;
                    counter++;
                } while (!AcceptNewTree(trees, tree, similarity));

                var newTree = (ISymbolicExpressionTree)tree.Clone(new Cloner());
                trees.Add(newTree);
                string strTree = formatter.Format(newTree);
                strTree = strTree.Replace("Models", "DataGen");
                strTree = strTree.Replace("Model", "Model" + i);
                File.WriteAllText(prefix + "\\Model" + i + ".cs", strTree);

                //test if valid tree
                try
                {
                    var (model, context) = CompileClassWithRoslyn(strTree, "Model" + i);
                    var yres = GenerateData(rand, model, prefix);
                    context.Unload();
                    ys.Add(yres);
                    sources.Add(strTree);
                    Console.WriteLine("Tree " + i + " length: " + newTree.Root.GetLength() + " added... (similarity target: " + similarity + ")");
                }
                catch (Exception e)
                {
                    // sometimes garbage is generated, then redo again
                    Console.WriteLine("Error: " + e.Message);
                    trees.RemoveAt(trees.Count - 1);
                    i--;
                }
            }

            CalcSimilarity(trees, nrOfDatasets, prefix);
            return sources;
        }

        private static ISymbolicExpressionTree CreateNewTreeOfCorrectLength(int minLength, double similarity, FastRandom rand, ISymbolicDataAnalysisGrammar grammar)
        {
            ISymbolicExpressionTree tree = ProbabilisticTreeCreator.Create(rand, grammar, maxTreeLength, maxTreeDepth);
            while (tree.Root.GetLength() < minLength)
            {
                tree = ProbabilisticTreeCreator.Create(rand, grammar, maxTreeLength, maxTreeDepth);
            }
            return tree;
        }


        public static double CalcAvgSim(IList<ISymbolicExpressionTree> trees, ISymbolicExpressionTree newTree)
        {
            var sims = new List<double>();
            foreach (var tree in trees)
            {
                sims.Add(SymbolicExpressionTreeHash.ComputeSimilarity(tree, newTree));
            }

            return sims.Average();
        }

        public static bool AcceptNewTree(IList<ISymbolicExpressionTree> trees, ISymbolicExpressionTree newTree, double similarity)
        {
            if (trees.Count < 1)
            {
                return true;
            }

            double lowerBound = similarity * 0.90;
            double upperBound = similarity * 1.10;

            double sim = CalcAvgSim(trees, newTree);

            //never accept trees with too much similarity
            if (sim > 0.99)
            {
                return false;
            }

            if (sim < lowerBound || sim > upperBound)
            {
                return false;
            }

            return true;
        }

        public static List<double> GenerateData(FastRandom random, Type t, string prefix)
        {
            string csv = " y; x1; x2; x3; x4; x5; x6; x7; x8; x9; x10" + Environment.NewLine;
            var ys = new List<double>();

            for (int i = 0; i < 1000; i++)
            {
                var x = new object[10];
                for (int j = 0; j < 10; j++)
                {
                    x[j] = random.NextDouble();
                }

                int nrOfParameters = t.GetMethod("Evaluate").GetParameters().Count();

                double res = (double)t.GetMethod("Evaluate").Invoke(null, x.Take(nrOfParameters).ToArray());
                csv += $"{res}; {x[0]}; {x[1]}; {x[2]}; {x[3]}; {x[4]}; {x[5]}; {x[6]}; {x[7]}; {x[8]}; {x[9]}" + Environment.NewLine;
                ys.Add(res);
            }
            File.WriteAllText(prefix + "\\" + t.Name + "p.csv", csv);
            Console.WriteLine(prefix + " / " + t.Name + "p.csv");
            TestData(prefix + "\\" + t.Name + "p.csv");
            return ys;
        }

        public static void TestData(string instName)
        {
            var provider = new RegressionCSVInstanceProvider();
            var problemData = provider.ImportData(instName);
            var symRegProblem = new SymbolicRegressionSingleObjectiveProblem();
            symRegProblem.Load(problemData);
            symRegProblem.Name = instName;
            symRegProblem.ProblemData.Name = instName;
            symRegProblem.ProblemData.TargetVariable = "y";
            symRegProblem.ProblemData.InputVariables.SetItemCheckedState(symRegProblem.ProblemData.InputVariables.Single(x => x.Value.Trim() == "y"), false);
            symRegProblem.ProblemData.InputVariables.SetItemCheckedState(symRegProblem.ProblemData.InputVariables.Single(x => x.Value.Trim() == "x10"), true);

            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Logarithm").Enabled = false;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Exponential").Enabled = false;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Sine").Enabled = true;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Cosine").Enabled = true;
            symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Tangent").Enabled = true;

            symRegProblem.MaximumSymbolicExpressionTreeDepth.Value = 12;
            symRegProblem.MaximumSymbolicExpressionTreeLength.Value = 100;

            var ga = new GeneticAlgorithm
            {
                MaximumGenerations = { Value = 70 },
                Problem = symRegProblem,
                Engine = new ParallelEngine()
            };

            ga.Start();

            if (ga.Engine.Log.Messages.Any(x => x.Contains("Exception")))
            {
                throw new Exception("Problem with dataset: " + instName + " , skipping...");
            }

            // don't use datasets that are too easy or too hard
            var trainingQuality = ((DoubleValue)ga.Results["BestQuality"].Value).Value;
            if (trainingQuality > 0.9)
            {
                throw new Exception("Invalid results, skipping... ");
            }

            if (trainingQuality < 0.05)
            {
                throw new Exception("Invalid results, skipping... ");
            }

            var trainingSolution = (SymbolicRegressionSolution)((Result)ga.Results["Best training solution"]).Value;
            var testingQuality = trainingSolution.TestRSquared;
            if (testingQuality < 0.05 || Math.Abs(trainingQuality - testingQuality) > 0.4)
            {
                throw new Exception("Invalid results, skipping... ");
            }
            if (testingQuality > 0.9)
            {
                throw new Exception("Invalid results, skipping... ");
            }
        }

        private static (Type type, AssemblyLoadContext context) CompileClassWithRoslyn(string src, string className)
        {
            var comp = CreateCompilation(CSharpSyntaxTree.ParseText(src), className);
            using var ms = new MemoryStream();
            var result = comp.Emit(ms);
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }
                throw new Exception("Compilation failed");
            }

            ms.Position = 0;
            var context = new AssemblyLoadContext(className, isCollectible: true);
            var loadedAssembly = context.LoadFromStream(ms);

            var type = loadedAssembly.GetTypes().First();
            if (type == null)
            {
                context.Unload();
                throw new Exception("No type found in the compiled assembly.");
            }
            return (type, context);
        }

        private static CSharpCompilation CreateCompilation(SyntaxTree tree, string name)
        {
            IEnumerable<MetadataReference> references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            return CSharpCompilation
                .Create(name, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(tree);
        }
    }
}
