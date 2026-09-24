using System;
using System.Collections.Generic;
using System.Linq;
using HEAL.Attic;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Parameters;


namespace HeuristicLab.Problems.DataAnalysis.Symbolic.Regression
{
  [StorableType("7363A140-0EF5-4FDD-A56E-333CBF29BB9E")]
  public enum SolutionCreationMethod
  {
    Transfer, // start with a TL subtree and then mutate with TL subtrees
    IncrementalTransfer, // like transfer, but check after every mutation if quality improved, take the best solution
  }

  [StorableType("5A26A7D9-60CC-4723-94ED-A275BB716CD9")]
  [Item("TransferLearningSolutionCreator", "Creates a symbolic expression tree using subtrees from previous problems.")]
  public class TransferLearningSolutionCreator : ProbabilisticTreeCreator, ISymbolicDataAnalysisSolutionCreator
  {
    private const string ProblemDataParameterName = "ProblemData";
    private const string SymbolicDataAnalysisTreeInterpreterParameterName = "SymbolicExpressionTreeInterpreter";
    private const string EstimationLimitsParameterName = "EstimationLimits";
    private const string SolutionCreationMethodParameterName = "SolutionCreationMethod";
    private const string CacheSizeFactorParameterName = "CacheSizeFactor";

    public ValueLookupParameter<ItemArray<TransferSubtree>> SubtreesListParameter
    {
      get { return (ValueLookupParameter<ItemArray<TransferSubtree>>)Parameters["SubtreesList"]; }
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
    private ValueLookupParameter<IntValue> PopulationSizeParameter
    {
      get { return (ValueLookupParameter<IntValue>)Parameters["PopulationSize"]; }
    }
    public ILookupParameter<IntValue> EvaluatedSolutionsParameter
    {
      get { return (ILookupParameter<IntValue>)Parameters["EvaluatedSolutions"]; }
    }
    public ValueParameter<DoubleValue> CacheSizeFactor
    {
      get { return (ValueParameter<DoubleValue>)Parameters[CacheSizeFactorParameterName]; }
    }
    public ValueParameter<EnumValue<SolutionCreationMethod>> SolutionCreationMethodParameter
    {
      get { return (ValueParameter<EnumValue<SolutionCreationMethod>>)Parameters[SolutionCreationMethodParameterName]; }
    }
    public ScopeParameter CurrentScopeParameter
    {
      get { return (ScopeParameter)Parameters["CurrentScope"]; }
    }
    public IScope CurrentScope
    {
      get { return CurrentScopeParameter.ActualValue; }
    }

    [StorableConstructor]
    protected TransferLearningSolutionCreator(StorableConstructorFlag _) : base(_) { }
    protected TransferLearningSolutionCreator(TransferLearningSolutionCreator original, Cloner cloner) : base(original, cloner)
    {
    }

    public TransferLearningSolutionCreator()
      : base()
    {
      Parameters.Add(new ScopeParameter("CurrentScope", "The current scope into which the parameter values are cloned."));
      Parameters.Add(new LookupParameter<IRegressionProblemData>(ProblemDataParameterName, "The problem data for the symbolic regression solution."));
      Parameters.Add(new LookupParameter<ISymbolicDataAnalysisExpressionTreeInterpreter>(SymbolicDataAnalysisTreeInterpreterParameterName, "The symbolic data analysis tree interpreter for the symbolic expression tree."));
      Parameters.Add(new ValueLookupParameter<DoubleLimit>(EstimationLimitsParameterName, "The lower and upper limit for the estimated values produced by the symbolic regression model."));
      Parameters.Add(new LookupParameter<IntValue>("EvaluatedSolutions", "The number of evaluated solutions."));
      Parameters.Add(new ValueLookupParameter<ItemArray<TransferSubtree>>("SubtreesList", "List of subtrees used for construction new solutions."));
      Parameters.Add(new ValueLookupParameter<IntValue>("PopulationSize", "Size of the population. "));
      Parameters.Add(new ValueParameter<EnumValue<SolutionCreationMethod>>(SolutionCreationMethodParameterName, "The method used for creating new solutions.", new EnumValue<SolutionCreationMethod>(SolutionCreationMethod.Transfer)));
      Parameters.Add(new ValueParameter<DoubleValue>(CacheSizeFactorParameterName, "Amount of subtrees to take from pool for solution construction; multiple of population size.", new DoubleValue(3.0)));
    }

    public override IDeepCloneable Clone(Cloner cloner)
    {
      return new TransferLearningSolutionCreator(this, cloner);
    }
   
    private void IncrementEvalSolutions(int increment)
    {
      var evalSols = EvaluatedSolutionsParameter.ActualValue;
      if (evalSols == null)
      {
        CurrentScope.Parent.Variables.Add(new Core.Variable(EvaluatedSolutionsParameter.Name, EvaluatedSolutionsParameter.Description, new IntValue(0)));
        evalSols = EvaluatedSolutionsParameter.ActualValue;
      }
      evalSols.Value += increment;
    }

    private ISymbolicExpressionTree TransferMethod(IRandom random, ISymbolicExpressionGrammar grammar, int maxTreeLength, int maxTreeDepth, IEnumerable<ISymbolicExpressionTreeNode> subtrees)
    {
      //no quality selection here, just mutate and take the result
      var subtree = subtrees.ElementAt(random.Next(subtrees.Count()));
      ISymbolicExpressionTree newTree = new SymbolicExpressionTree();
      newTree.Root = new ProgramRootSymbol().CreateTreeNode();
      newTree.Root.AddSubtree(new StartSymbol().CreateTreeNode());
      newTree.Root.GetSubtree(0).AddSubtree((ISymbolicExpressionTreeNode)subtree.Clone(new Cloner()));

      foreach (var node in newTree.Root.IterateNodesPrefix().OfType<SymbolicExpressionTreeTopLevelNode>())
        node.SetGrammar(grammar.CreateExpressionTreeGrammar());
      int repetitions = random.Next(2, 10);
      for (int j = 0; j < repetitions; j++)
      {
        ReplaceRandomBranch(random, newTree, maxTreeLength, maxTreeDepth, subtrees);
      }
      return newTree;
    }

    private ISymbolicExpressionTree IncrementalTransferMethod(IRandom random, ISymbolicExpressionGrammar grammar, int maxTreeLength, int maxTreeDepth, IEnumerable<ISymbolicExpressionTreeNode> subtrees)
    {
      var subtree = subtrees.ElementAt(random.Next(subtrees.Count()));
      ISymbolicExpressionTree t = new SymbolicExpressionTree();
      t.Root = new ProgramRootSymbol().CreateTreeNode();
      t.Root.AddSubtree(new StartSymbol().CreateTreeNode());
      t.Root.GetSubtree(0).AddSubtree((ISymbolicExpressionTreeNode)subtree.Clone(new Cloner()));

      foreach (var node in t.Root.IterateNodesPrefix().OfType<SymbolicExpressionTreeTopLevelNode>())
        node.SetGrammar(grammar.CreateExpressionTreeGrammar());

      int repetitions = random.Next(2, 10); //TODO: does this need to be a fixed value for results processing?
      IncrementEvalSolutions(repetitions);
      for (int j = 0; j < repetitions; j++)
      {
        double curQuality = Evaluate(t);
        var oldTree = (ISymbolicExpressionTree)t.Clone(new Cloner());
        ReplaceRandomBranch(random, t, maxTreeLength, maxTreeDepth, subtrees);
        double newQuality = Evaluate(t);
        if (newQuality < curQuality)
        {
          t = oldTree;
        }
      }
      return t;
    }

    public override ISymbolicExpressionTree CreateTree(IRandom random, ISymbolicExpressionGrammar grammar, int maxTreeLength, int maxTreeDepth)
    {
      if (SubtreesListParameter.Value != null)
      {
        var subtrees = SelectBestTrees();

        if (SolutionCreationMethodParameter.Value.Value == SolutionCreationMethod.Transfer)
        {
          return TransferMethod(random, grammar, maxTreeLength, maxTreeDepth, subtrees);
        }
        else if (SolutionCreationMethodParameter.Value.Value == SolutionCreationMethod.IncrementalTransfer)
        {
          return IncrementalTransferMethod(random, grammar, maxTreeLength, maxTreeDepth, subtrees);
        }
        else
        {
          throw new ArgumentException("Invalid solution creation method");
        }
      }
      else
      {
        return Create(random, grammar, maxTreeLength, maxTreeDepth);
      }
    }

    private IEnumerable<ISymbolicExpressionTreeNode> SelectBestTrees()
    {
      //assumes the quality has already been calculated
      var subtrees = SubtreesListParameter.Value;
      return subtrees.OrderByDescending(x => x.ProblemQualities[ProblemDataParameter.ActualValue.Name]).Select(x => x.SubTree).Take((int)(PopulationSizeParameter.ActualValue.Value * CacheSizeFactor.Value.Value));
    }

    private double Evaluate(ISymbolicExpressionTree tree)
    {
      var regressionModel = new SymbolicRegressionModel(ProblemDataParameter.ActualValue.TargetVariable, tree, SymbolicDataAnalysisTreeInterpreterParameter.ActualValue, EstimationLimitsParameter.ActualValue.Lower, EstimationLimitsParameter.ActualValue.Upper);
      var regressionProblemData = ProblemDataParameter.ActualValue;

      IEnumerable<double> estimatedValues = regressionModel.GetEstimatedValues(regressionProblemData.Dataset, ProblemDataParameter.ActualValue.TrainingIndices);
      IEnumerable<double> targetValues = regressionProblemData.Dataset.GetDoubleValues(regressionProblemData.TargetVariable, ProblemDataParameter.ActualValue.TrainingIndices);

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

    //adapted from ReplaceBranchManipulation
    public static void ReplaceRandomBranch(IRandom random, ISymbolicExpressionTree symbolicExpressionTree, int maxTreeLength, int maxTreeDepth, IEnumerable<ISymbolicExpressionTreeNode> newSubtrees)
    {
      int MAX_TRIES = 100;
      var allowedSymbols = new List<ISymbol>();
      ISymbolicExpressionTreeNode parent;
      int childIndex;
      int maxLength;
      int maxDepth;
      // repeat until a fitting parent and child are found (MAX_TRIES times)
      int tries = 0;
      do
      {
#pragma warning disable 612, 618
        parent = symbolicExpressionTree.Root.IterateNodesPrefix().Skip(1).Where(n => n.SubtreeCount > 0).SelectRandom(random);
#pragma warning restore 612, 618

        childIndex = random.Next(parent.SubtreeCount);
        var child = parent.GetSubtree(childIndex);
        maxLength = maxTreeLength - symbolicExpressionTree.Length + child.GetLength();
        maxDepth = maxTreeDepth - symbolicExpressionTree.Depth + child.GetDepth();

        allowedSymbols.Clear();
        foreach (var symbol in parent.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex))
        {
          // check basic properties that the new symbol must have
          if (symbol.Name != child.Symbol.Name &&
            symbol.InitialFrequency > 0 &&
            parent.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth &&
            parent.Grammar.GetMinimumExpressionLength(symbol) <= maxLength)
          {
            allowedSymbols.Add(symbol);
          }
        }
        tries++;
      } while (tries < MAX_TRIES && allowedSymbols.Count == 0);

      if (tries < MAX_TRIES)
      {
        foreach (var st in newSubtrees.OrderBy(a => random.Next()).ToList())
        {
          if (st.GetDepth() <= maxDepth && st.GetLength() <= maxLength)
          {
            if (allowedSymbols.Any(x => x.Name == st.Symbol.Name))
            {
              parent.RemoveSubtree(childIndex);
              parent.InsertSubtree(childIndex, (ISymbolicExpressionTreeNode)st.Clone(new Cloner()));
              break;
            }
          }
        }
      }
    }
  }
}