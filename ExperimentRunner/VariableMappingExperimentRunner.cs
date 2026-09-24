using System.Collections.Generic;
using System.Linq;
using HeuristicLab.Optimization;
using HeuristicLab.Problems.DataAnalysis.Symbolic.Regression;
using HeuristicLab.Problems.Instances.DataAnalysis;
using HeuristicLab.Common;
using System.IO;
using System.Data;
using System;
using HeuristicLab.Core;
using HeuristicLab.Problems.DataAnalysis.Symbolic;
using HeuristicLab.Problems.DataAnalysis;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using System.Threading.Tasks;


namespace ExperimentRunner
{
    public class VariableMappingExperimentRunner : ExperimentRunner
    {
        public Dictionary<ulong, TransferSubtree> Subtrees = new Dictionary<ulong, TransferSubtree>();

        private Experiment normalExperiment, incrementalExperiment, transferExperiment;

        private string expIdent;

        public override void Run(IExperimentConfig config, int curNrOfSourceInstances)
        {
            Config = config;
            var tok = config.DataPath.Split('\\');
            expIdent = tok[tok.Length - 1] + "_" + curNrOfSourceInstances.ToString();
            Console.WriteLine("Start of " + expIdent);

            Subtrees.Clear();

            List<SymbolicRegressionSingleObjectiveProblem> problems = null;
            if (Config.DataPathSubDirs)
            {
                problems = CreateCsvProblems(config.DataPath);
            }
            else
            {
                problems = CreateCsvProblemsSynthetic(config.DataPath);
            }

            var collectionProblems = new Dictionary<string, SymbolicRegressionSingleObjectiveProblem>();
            var transferProblems = new Dictionary<string, SymbolicRegressionSingleObjectiveProblem>();

            List<int> sourceIndizes = new List<int>();
            List<int> targetIndizes = new List<int>();
            Random rand = new Random();

            for (int i = 0; i < curNrOfSourceInstances; i++)
            {
                sourceIndizes.Add(rand.Next(problems.Count));
            }

            for (int i = 0; i < config.NrOfTargetInstances; i++)
            {
                int tmpIndex = rand.Next(problems.Count);
                while (sourceIndizes.Contains(tmpIndex) || targetIndizes.Contains(tmpIndex))
                {
                    tmpIndex = rand.Next(problems.Count);
                }
                targetIndizes.Add(tmpIndex);
            }

            foreach (var idx in sourceIndizes)
            {
                var p = problems[idx];
                collectionProblems[p.Name] = p;
            }

            foreach (var idx in targetIndizes)
            {
                var p = problems[idx];
                transferProblems[p.Name] = p;
            }

            //"normal" symbolic regression runs for comparison 
            normalExperiment = new Experiment() { Name = "Normal " + expIdent };
            foreach (var tp in transferProblems.Values)
            {
                string targetName = tp.Name;
                var alg = CreateCollectionAlgorithm((SymbolicRegressionSingleObjectiveProblem)tp.Clone(new Cloner()));
                normalExperiment.Optimizers.Add(new BatchRun() { Optimizer = (IOptimizer)alg, Repetitions = Config.BatchRepetitions, Name = alg.Name });
            }
            normalExperiment.ExceptionOccurred += Experiment_OnExceptionOccurred;
            var neTask = normalExperiment.StartAsync();

            List<IRun> transOldRuns = new List<IRun>();
            List<IRun> incOldRuns = new List<IRun>();
            for (int i = 0; i < Config.BatchRepetitions; i++)
            {
                Console.WriteLine("Starting batch repetition " + (i + 1) + " of " + Config.BatchRepetitions + " for " + expIdent);
                // create collection and transfer experiment
                incrementalExperiment = CreateTLExperiment("Incremental Transfer " + expIdent, collectionProblems, transferProblems, SolutionCreationMethod.IncrementalTransfer);
                transferExperiment = CreateTLExperiment("Transfer " + expIdent, collectionProblems, transferProblems, SolutionCreationMethod.Transfer);

                RegisterExperimentEvents(incrementalExperiment);
                RegisterExperimentEvents(transferExperiment);

                incrementalExperiment.Start();
                Subtrees.Clear();
                transferExperiment.Start();
                Subtrees.Clear();

                DeregisterExperimentEvents(incrementalExperiment);
                DeregisterExperimentEvents(transferExperiment);
                incOldRuns.AddRange(incrementalExperiment.Runs.ToList());
                transOldRuns.AddRange(transferExperiment.Runs.ToList());
            }
            incrementalExperiment.Runs.Clear();
            transferExperiment.Runs.Clear();
            incrementalExperiment.Runs.AddRange(incOldRuns);
            transferExperiment.Runs.AddRange(transOldRuns);

            neTask.Wait();
            Console.WriteLine(expIdent + " is finished, exporting results...");
            try
            {
                CsvExporter.Export(Config.ResultPath, new List<IOptimizer>() { incrementalExperiment, transferExperiment }, normalExperiment);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in export: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private Experiment CreateTLExperiment(string name, Dictionary<string, SymbolicRegressionSingleObjectiveProblem> collectionProblems,
                                                Dictionary<string, SymbolicRegressionSingleObjectiveProblem> transferProblems, SolutionCreationMethod method)
        {
            var experiment = new Experiment() { Name = name, NumberOfWorkers = 1 };

            foreach (var cp in collectionProblems.Values)
            {
                var calg = CreateCollectionAlgorithm((SymbolicRegressionSingleObjectiveProblem)cp.Clone(new Cloner()));
                experiment.Optimizers.Add(calg);
            }

            foreach (var tp in transferProblems.Values)
            {
                string targetName = tp.Name;
                var alg = CreateTransferAlgorithm((SymbolicRegressionSingleObjectiveProblem)transferProblems[targetName].Clone(new Cloner()), method);
                experiment.Optimizers.Add(alg);
            }

            return experiment;
        }

        private void RegisterExperimentEvents(Experiment experiment)
        {
            RegisterOptimizerEvents(experiment.Optimizers);
            experiment.ExceptionOccurred += Experiment_OnExceptionOccurred;
        }

        private void DeregisterExperimentEvents(Experiment experiment)
        {
            DeregisterOptimizerEvents(experiment.Optimizers);
            experiment.ExceptionOccurred -= Experiment_OnExceptionOccurred;
        }

        private void RegisterOptimizerEvents(IEnumerable<IOptimizer> optimizers)
        {
            foreach (var o in optimizers)
            {
                o.Started += optimizer_Started;
                o.Stopped += optimizer_Stopped;
            }
        }

        private void DeregisterOptimizerEvents(IEnumerable<IOptimizer> optimizers)
        {
            foreach (var o in optimizers)
            {
                o.Started -= optimizer_Started;
                o.Stopped -= optimizer_Stopped;
            }
        }

        private void optimizer_Started(object sender, EventArgs e)
        {
            IOptimizer o = (IOptimizer)sender;
            IProblem problem = ((EngineAlgorithm)o).Problem;
            IItem tlSolCreator = problem.Parameters["SolutionCreator"].ActualValue;
            IRegressionProblemData problemData = problem.Parameters[ProblemDataParameterName].ActualValue as IRegressionProblemData;

            if (tlSolCreator is TransferLearningSolutionCreator)
            {
                var treeClones = Subtrees.Values.ToList().Select(x => (TransferSubtree)x.Clone(new Cloner())).ToList();
                var trees = EvaluateSubtreesOnProblems(treeClones, problem);
                var tlc = tlSolCreator as TransferLearningSolutionCreator;
                tlc.SubtreesListParameter.Value = new ItemArray<TransferSubtree>(trees);
            }
        }

        private void optimizer_Stopped(object sender, EventArgs e)
        {
            IOptimizer o = (IOptimizer)sender;
            IProblem problem = ((EngineAlgorithm)o).Problem;
            IItem tlSolCreator = problem.Parameters["SolutionCreator"].ActualValue;
            IRegressionProblemData problemData = problem.Parameters[ProblemDataParameterName].ActualValue as IRegressionProblemData;

            var alg = (EngineAlgorithm)o;
            if (alg.Results.ContainsKey("TransferSubtrees"))
            {
                var curSubtrees = ((ItemArray<TransferSubtree>)alg.Results["TransferSubtrees"].Value).ToList();
                curSubtrees.ForEach(x => x.DataSet = problemData.Dataset);
                curSubtrees.ForEach(x => Subtrees[x.SubTree.ComputeHash()] = x);
                Console.WriteLine("Optimizer " + o.Name + " is finished, Pool Size is: " + Subtrees.Count);
            }
        }

        private List<TransferSubtree> EvaluateSubtreesOnProblems(List<TransferSubtree> subtrees, IProblem problem)
        {
            Random rand = new Random(); 
            List<TransferSubtree> result = new List<TransferSubtree>();
            IRegressionProblemData problemData = problem.Parameters[ProblemDataParameterName].ActualValue as IRegressionProblemData;
            ISymbolicDataAnalysisExpressionTreeInterpreter interpreter = problem.Parameters[SymbolicDataAnalysisTreeInterpreterParameterName].ActualValue as ISymbolicDataAnalysisExpressionTreeInterpreter;
            DoubleLimit estimationLimits = problem.Parameters[EstimationLimitsParameterName].ActualValue as DoubleLimit;

            int nrOfVariables = problemData.InputVariables.CheckedItems.Count();
            List<string> problemInputVariableNames = problemData.InputVariables.CheckedItems.Select(x => x.Value.Value).ToList();
            Dictionary<string, List<double>> similarityMap = new Dictionary<string, List<double>>();

            foreach (var subtree in subtrees)
            {
                var newTree = new SymbolicExpressionTree();
                newTree.Root = new ProgramRootSymbol().CreateTreeNode();
                newTree.Root.AddSubtree(new StartSymbol().CreateTreeNode());
                newTree.Root.GetSubtree(0).AddSubtree(subtree.SubTree);

                IEnumerable<string> treeInputVariableNames = subtree.SubTree.IterateNodesBreadth().Where(x =>
                    x is VariableTreeNode).Select(x => ((VariableTreeNode)x).VariableName)
                .Distinct();

                bool needsVariableMapping = false;
                foreach (string name in treeInputVariableNames)
                {
                    if (!problemInputVariableNames.Contains(name))
                    {
                        needsVariableMapping = true;
                        break;
                    }
                }

                if (needsVariableMapping)
                {
                    if (Config.MappingType == MappingTypeEnum.Random)
                    {
                        RandomVariableMapping(newTree, problemData, interpreter, estimationLimits, 1);
                    }
                    else if (Config.MappingType == MappingTypeEnum.MonteCarlo)
                    {
                        RandomVariableMapping(newTree, problemData, interpreter, estimationLimits, 20);
                    }
                    else if (Config.MappingType == MappingTypeEnum.Wasserstein || Config.MappingType == MappingTypeEnum.MDD)
                    {
                        VariableMapping(subtree, problemData, similarityMap);
                    }
                }
                else
                {
                    IEnumerable<VariableTreeNode> variableNodes = subtree.SubTree.IterateNodesBreadth().Where(x =>
                       x is VariableTreeNode).Select(x => ((VariableTreeNode)x));

                    if (variableNodes.Count() > 0)
                    {
                        //fix possible mismatches between variable name and symbol variable names
                        bool needsFixing = false;
                        foreach (string name in treeInputVariableNames)
                        {
                            if (!variableNodes.First().Symbol.VariableNames.Contains(name))
                            {
                                needsFixing = true;
                                break;
                            }
                        }

                        if (needsFixing || variableNodes.First().Symbol.VariableNames.Count() != problemInputVariableNames.Count)
                        {
                            for (int i = 0; i < variableNodes.Count(); i++)
                            {
                                variableNodes.ElementAt(i).Symbol.VariableNames = problemInputVariableNames;
                            }
                        }
                    }
                }

                subtree.ProblemQualities[problem.Name] = Evaluate(newTree, problemData, interpreter, estimationLimits);
                result.Add(subtree);
            }
            return result;
        }

        private void VariableMapping(TransferSubtree subTree, IRegressionProblemData problemData, Dictionary<string, List<double>> similarityMap)
        {
            ISymbolicExpressionTreeNode tree = subTree.SubTree;
            IDataset subtreeDataSet = subTree.DataSet;
            IDataset targetDataSet = problemData.Dataset;

            List<string> targetInputVariableNames = problemData.InputVariables.CheckedItems.Select(x => x.Value.Value).ToList();

            IEnumerable<VariableTreeNode> variableNodes = tree.IterateNodesBreadth().Where(x =>
                    x is VariableTreeNode).Select(x => ((VariableTreeNode)x));

            foreach (var subVar in subtreeDataSet.VariableNames)
            {
                if (!similarityMap.ContainsKey(subVar))
                {
                    similarityMap[subVar] = new List<double>();
                    double[] subValues = subtreeDataSet.GetDoubleValues(subVar).ToArray();
                    object lockObj = new object();
                    Parallel.ForEach(targetInputVariableNames, targetVar =>
                    {
                        double[] targetValues = targetDataSet.GetDoubleValues(targetVar).ToArray();
                        double similarity = 0.0;
                        if (Config.MappingType == MappingTypeEnum.Wasserstein)
                        {
                            similarity = FeatureMapping.Wasserstein1D(targetValues, subValues);
                        }
                        else if (Config.MappingType == MappingTypeEnum.MDD)
                        {
                            similarity = FeatureMapping.Compute1D(targetValues, subValues);
                        }
                        lock (lockObj)
                        {
                            similarityMap[subVar].Add(similarity);
                        }
                    });
                }
            }

            foreach (var node in variableNodes)
            {
                //get index of minimum
                int minIdx = similarityMap[node.VariableName].Select((val, idx) => (val, idx)).MinBy(x => x.val).idx;
                string n = targetInputVariableNames[minIdx];

                node.VariableName = n;
                node.Symbol.VariableNames = targetInputVariableNames;
            }
        }

        private void RandomVariableMapping(ISymbolicExpressionTree tree, IRegressionProblemData problemData, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, DoubleLimit estimationLimits,
            int maxTries = 1)
        {
            Random rand = new Random(); 
            List<string> problemInputVariableNames = problemData.InputVariables.CheckedItems.Select(x => x.Value.Value).ToList();

            IEnumerable<VariableTreeNode> variableNodes = tree.IterateNodesBreadth().Where(x =>
                    x is VariableTreeNode).Select(x => ((VariableTreeNode)x));

            double bestQuality = double.MinValue;
            List<string> bestAssignment = null;
            for (int i = 0; i < maxTries; i++)
            {
                List<string> assignment = new List<string>();
                foreach (var node in variableNodes)
                {
                    string n = problemInputVariableNames[rand.Next(problemInputVariableNames.Count())];
                    node.VariableName = n;
                    node.Symbol.VariableNames = problemInputVariableNames;
                    assignment.Add(n);
                }

                double quality = Evaluate(tree, problemData, interpreter, estimationLimits);
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    bestAssignment = assignment;
                }
            }

            for (int i = 0; i < variableNodes.Count(); i++)
            {
                variableNodes.ElementAt(i).VariableName = bestAssignment[i];
                variableNodes.ElementAt(i).Symbol.VariableNames = problemInputVariableNames;
            }
        }

        private void Experiment_OnExceptionOccurred(object sender, EventArgs<Exception> e)
        {
            Console.WriteLine("Exception in experiment: " + e.Value.Message);
            Console.WriteLine(e.Value.StackTrace);
        }

        //for PMLB datasets
        private List<SymbolicRegressionSingleObjectiveProblem> CreateCsvProblems(string pathName)
        {
            var result = new List<SymbolicRegressionSingleObjectiveProblem>();

            string[] instanceNames = Directory.GetFiles(pathName, "*.tsv",
                                            SearchOption.AllDirectories)
                                           .ToArray();

            foreach (string instName in instanceNames)
            {
                var provider = new RegressionCSVInstanceProvider();
                var problemData = provider.ImportData(instName);
                var symRegProblem = new SymbolicRegressionSingleObjectiveProblem();
                symRegProblem.Load(problemData);
                symRegProblem.Name = Path.GetFileName(instName);
                symRegProblem.ProblemData.Name = Path.GetFileName(instName);
                symRegProblem.ProblemData.TargetVariable = "target";

                symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Logarithm").Enabled = false;
                symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Exponential").Enabled = false;
                symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Sine").Enabled = true;
                symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Cosine").Enabled = true;
                symRegProblem.SymbolicExpressionTreeGrammar.Symbols.Single(x => x.Name == "Tangent").Enabled = true;

                symRegProblem.MaximumSymbolicExpressionTreeDepth.Value = 12;
                symRegProblem.MaximumSymbolicExpressionTreeLength.Value = 100;

                result.Add(symRegProblem);
            }

            return result;
        }

        //for synthetic datasets from DataGenerator
        private List<SymbolicRegressionSingleObjectiveProblem> CreateCsvProblemsSynthetic(string pathName)
        {
            var result = new List<SymbolicRegressionSingleObjectiveProblem>();

            string[] instanceNames = Directory.GetFiles(pathName, "Model*.csv")
                                           .Select(Path.GetFileName)
                                           .ToArray();

            foreach (string instName in instanceNames)
            {
                var provider = new RegressionCSVInstanceProvider();
                var problemData = provider.ImportData(Path.Combine(pathName, instName));
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

                result.Add(symRegProblem);
            }

            return result;
        }
    }
}