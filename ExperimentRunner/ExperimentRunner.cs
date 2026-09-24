using System.Linq;
using HeuristicLab.Algorithms.GeneticAlgorithm;
using HeuristicLab.Analysis;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Optimization;
using HeuristicLab.ParallelEngine;
using HeuristicLab.Problems.DataAnalysis;
using HeuristicLab.Problems.DataAnalysis.Symbolic;
using HeuristicLab.Problems.DataAnalysis.Symbolic.Regression;
using HeuristicLab.Selection;

namespace ExperimentRunner
{
    public abstract class ExperimentRunner : IExperimentRunner
    {
        protected const string ProblemDataParameterName = "ProblemData";
        protected const string SymbolicDataAnalysisTreeInterpreterParameterName = "SymbolicExpressionTreeInterpreter";
        protected const string EstimationLimitsParameterName = "EstimationLimits";

        public IExperimentConfig Config { get; set; }

        public abstract void Run(IExperimentConfig config, int curNrOfSourceInstances);

        protected GeneticAlgorithm CreateCollectionAlgorithm(SymbolicRegressionSingleObjectiveProblem symRegProblem)
        {
            var ga = new GeneticAlgorithm
            {
                MaximumGenerations = { Value = Config.MaxGenerations },
                PopulationSize = { Value = Config.PopSize },
                Problem = symRegProblem,
                Engine = new ParallelEngine()
            };
            ga.SelectorParameter.Value = ga.SelectorParameter.ValidValues.OfType<ISelector>().FirstOrDefault(x => x is TournamentSelector);
            ((TournamentSelector)ga.SelectorParameter.Value).GroupSizeParameter.Value.Value = 4;
            ga.Analyzer.AddOperator(new TransferSubtreeAnalyzer());
            ga.Analyzer.AddOperator(new TLQualityPerEvaluationsAnalyzer());
            ga.Name = symRegProblem.Name;
            return ga;
        }

        protected GeneticAlgorithm CreateTransferAlgorithm(SymbolicRegressionSingleObjectiveProblem symRegProblem, SolutionCreationMethod solCreMethod)
        {
            var creator = new TransferLearningSolutionCreator();

            symRegProblem.SolutionCreatorParameter.Value = creator;
            creator.SolutionCreationMethodParameter.Value.Value = solCreMethod;
            creator.CacheSizeFactor.Value.Value = Config.CacheSizeFactor;

            var ga = CreateCollectionAlgorithm(symRegProblem);
            ga.Name = ga.Name + " (T)";

            ga.Analyzer.RemoveOperator(ga.Analyzer.Operators.Single(x => x is TransferSubtreeAnalyzer));
            return ga;
        }

        protected double Evaluate(ISymbolicExpressionTree tree, IRegressionProblemData problemData, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
                                    DoubleLimit estimationLimits)
        {
            var regressionModel = new SymbolicRegressionModel(problemData.TargetVariable, tree, interpreter, estimationLimits.Lower, estimationLimits.Upper);

            var estimatedValues = regressionModel.GetEstimatedValues(problemData.Dataset, problemData.TrainingIndices);
            var targetValues = problemData.Dataset.GetDoubleValues(problemData.TargetVariable, problemData.TrainingIndices);
            OnlineCalculatorError errorState;
            var r = OnlinePearsonsRCalculator.Calculate(targetValues, estimatedValues, out errorState);
            if (errorState != OnlineCalculatorError.None)
            {
                return double.NaN;
            }
            else
            {
                return r;
            }
        }
    }
}