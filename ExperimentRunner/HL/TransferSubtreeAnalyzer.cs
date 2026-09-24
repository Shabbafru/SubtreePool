using System;
using System.Collections.Generic;
using System.Linq;
using HEAL.Attic;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Operators;
using HeuristicLab.Optimization;
using HeuristicLab.Parameters;

namespace HeuristicLab.Problems.DataAnalysis.Symbolic.Regression
{

  [Item("TransferSubtreeAnalyzer", "Collects random subtrees from the best solutions in the population in the last generation.")]
  [StorableType("85B2CA71-689D-4925-B818-A2876707BE88")]
  public class TransferSubtreeAnalyzer : SingleSuccessorOperator, IAnalyzer
  {
    public virtual bool EnabledByDefault
    {
      get { return false; }
    }
    private const string ProblemDataParameterName = "ProblemData";
    private const string SymbolicDataAnalysisTreeInterpreterParameterName = "SymbolicExpressionTreeInterpreter";
    private const string EstimationLimitsParameterName = "EstimationLimits";

    public ScopeTreeLookupParameter<ISymbolicExpressionTree> SolutionParameter
    {
      get { return (ScopeTreeLookupParameter<ISymbolicExpressionTree>)Parameters["SymbolicExpressionTree"]; }
    }

    public ScopeTreeLookupParameter<DoubleValue> QualityParameter
    {
      get { return (ScopeTreeLookupParameter<DoubleValue>)Parameters["Quality"]; }
    }

    public ValueLookupParameter<ResultCollection> ResultsParameter
    {
      get { return (ValueLookupParameter<ResultCollection>)Parameters["Results"]; }
    }
    public ValueLookupParameter<IntValue> MaximumGenerationsParameter
    {
      get { return (ValueLookupParameter<IntValue>)Parameters["MaximumGenerations"]; }
    }
    public ILookupParameter<IRegressionProblemData> ProblemDataParameter
    {
      get { return (ILookupParameter<IRegressionProblemData>)Parameters[ProblemDataParameterName]; }
    }
    public ILookupParameter<ISymbolicDataAnalysisExpressionTreeInterpreter> SymbolicDataAnalysisTreeInterpreterParameter
    {
      get { return (ILookupParameter<ISymbolicDataAnalysisExpressionTreeInterpreter>)Parameters[SymbolicDataAnalysisTreeInterpreterParameterName]; }
    }
    public IValueLookupParameter<DoubleLimit> EstimationLimitsParameter
    {
      get { return (IValueLookupParameter<DoubleLimit>)Parameters[EstimationLimitsParameterName]; }
    }

    [StorableConstructor]
    private TransferSubtreeAnalyzer(StorableConstructorFlag _) : base(_) { }
    private TransferSubtreeAnalyzer(TransferSubtreeAnalyzer original, Cloner cloner) : base(original, cloner) { }
    public TransferSubtreeAnalyzer()
      : base()
    {
      Parameters.Add(new ScopeTreeLookupParameter<ISymbolicExpressionTree>("SymbolicExpressionTree", "The solutions whose alleles should be analyzed."));
      Parameters.Add(new ScopeTreeLookupParameter<DoubleValue>("Quality", "The solutions qualities."));
      Parameters.Add(new ValueLookupParameter<ResultCollection>("Results", "The result collection where the allele frequency analysis results should be stored."));
      Parameters.Add(new ValueLookupParameter<IntValue>("MaximumGenerations", "The maximum number of generations which should be processed."));
      Parameters.Add(new LookupParameter<IRegressionProblemData>(ProblemDataParameterName, "The problem data for the symbolic regression solution."));
      Parameters.Add(new LookupParameter<ISymbolicDataAnalysisExpressionTreeInterpreter>(SymbolicDataAnalysisTreeInterpreterParameterName, "The symbolic data analysis tree interpreter for the symbolic expression tree."));
      Parameters.Add(new ValueLookupParameter<DoubleLimit>(EstimationLimitsParameterName, "The lower and upper limit for the estimated values produced by the symbolic regression model."));
    }

    public override IDeepCloneable Clone(Cloner cloner)
    {
      return new TransferSubtreeAnalyzer(this, cloner);
    }

    public override IOperation Apply()
    {
      if (ResultsParameter.ActualValue != null)
      {
        int currentGeneration = ((IntValue)ResultsParameter.ActualValue["Generations"].Value).Value;
        int maximumGenerations = MaximumGenerationsParameter.ActualValue.Value;
        IRandom random = new Random.FastRandom();

        //only calculate in last generation
        if (currentGeneration == maximumGenerations)
        {
          var results = ResultsParameter.ActualValue;
          Dictionary<ulong, ISymbolicExpressionTreeNode> uniqueSubtrees = new Dictionary<ulong, ISymbolicExpressionTreeNode>();

          ItemArray<ISymbolicExpressionTree> solutions = SolutionParameter.ActualValue;
          double cutoff = QualityParameter.ActualValue.Average(x => x.Value);
          for (int i = 0; i < solutions.Count(); i++)
          {
            if (QualityParameter.ActualValue[i].Value > cutoff)
            {
              var solution = solutions[i];
              int treeLength = solution.Root.GetLength() - 2;

              //sample 2 random subtrees from the solution
              for (int j = 0; j < 2; j++)
              {
                var selectedSubtree = solution.Root.GetSubtree(0).GetSubtree(0).IterateNodesPrefix().Skip(random.Next(treeLength)).Take(1).Single();
                if (!uniqueSubtrees.ContainsKey(selectedSubtree.ComputeHash(false, true)))
                {
                  uniqueSubtrees.Add(selectedSubtree.ComputeHash(false, true), (ISymbolicExpressionTreeNode)selectedSubtree.Clone(new Cloner()));
                }
              }
            }
          }
          List<TransferSubtree> TransferSubtrees = new List<TransferSubtree>();
          foreach (var item in uniqueSubtrees.Values)
          {
            TransferSubtrees.Add(new TransferSubtree(item, ProblemDataParameter.ActualValue.Name));
          }
          var result = new Result("TransferSubtrees", "Subtrees for transfer", new ItemArray<TransferSubtree>(TransferSubtrees));
          results.Add(result);
        }
      }

      return base.Apply();
    }
  }
}

