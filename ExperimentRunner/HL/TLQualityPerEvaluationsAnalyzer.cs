using System;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Operators;
using HeuristicLab.Optimization;
using HeuristicLab.Parameters;
using HEAL.Attic;
using HeuristicLab.Problems.DataAnalysis.Symbolic.Regression;

namespace HeuristicLab.Analysis
{
  [Item("TLQualityPerEvaluationsAnalyzer", @"Creates a plot of the solution quality with respect to the number of evaluated solutions.
        Copy of HLs QualityPerEvaluationsAnalyzer, but incorporating test quality as well.")]
  [StorableType("9DFC6B67-4507-42B8-ADF3-0D807A4BDF54")]
  public class TLQualityPerEvaluationsAnalyzer : SingleSuccessorOperator, IAnalyzer, ISingleObjectiveOperator
  {
    public virtual bool EnabledByDefault
    {
      get { return false; }
    }

    public ILookupParameter<DoubleValue> BestQualityParameter
    {
      get { return (ILookupParameter<DoubleValue>)Parameters["BestQuality"]; }
    }
    public ILookupParameter<IntValue> EvaluatedSolutionsParameter
    {
      get { return (ILookupParameter<IntValue>)Parameters["EvaluatedSolutions"]; }
    }
    public IResultParameter<IndexedDataTable<double>> QualityPerEvaluationsParameter
    {
      get { return (IResultParameter<IndexedDataTable<double>>)Parameters["QualityPerEvaluations"]; }
    }
    public IResultParameter<IndexedDataTable<double>> QualityPerEvaluationsTestParameter
    {
      get { return (IResultParameter<IndexedDataTable<double>>)Parameters["QualityPerEvaluationsTest"]; }
    }

    public ILookupParameter<ResultCollection> ResultsParameter
    {
      get { return (ILookupParameter<ResultCollection>)Parameters["Results"]; }
    }

    [StorableConstructor]
    protected TLQualityPerEvaluationsAnalyzer(StorableConstructorFlag _) : base(_) { }
    protected TLQualityPerEvaluationsAnalyzer(TLQualityPerEvaluationsAnalyzer original, Cloner cloner) : base(original, cloner) { }
    public TLQualityPerEvaluationsAnalyzer()
      : base()
    {
      Parameters.Add(new LookupParameter<DoubleValue>("BestQuality", "The quality value that should be compared."));
      Parameters.Add(new LookupParameter<IntValue>("EvaluatedSolutions", "The number of evaluated solutions."));
      Parameters.Add(new ResultParameter<IndexedDataTable<double>>("QualityPerEvaluations", "Data table containing the first hitting graph with evaluations as the x-axis."));
      Parameters.Add(new ResultParameter<IndexedDataTable<double>>("QualityPerEvaluationsTest", "Data table containing the first hitting graph with evaluations as the x-axis."));
      Parameters.Add(new LookupParameter<ResultCollection>("Results", "The results collection to store the results."));
      QualityPerEvaluationsParameter.DefaultValue = new IndexedDataTable<double>("Quality per Evaluations")
      {
        VisualProperties = {
          XAxisTitle = "Evaluations",
          YAxisTitle = "Quality"
        },
        Rows = { new IndexedDataRow<double>("First-hit Graph") { VisualProperties = {
          ChartType = DataRowVisualProperties.DataRowChartType.StepLine,
          LineWidth = 2
        } } }
      };
      QualityPerEvaluationsTestParameter.DefaultValue = new IndexedDataTable<double>("Quality per Evaluations (Test)")
      {
        VisualProperties = {
          XAxisTitle = "Evaluations",
          YAxisTitle = "Quality"
        },
        Rows = { new IndexedDataRow<double>("First-hit Graph") { VisualProperties = {
          ChartType = DataRowVisualProperties.DataRowChartType.StepLine,
          LineWidth = 2
        } } }
      };
    }

    public override IDeepCloneable Clone(Cloner cloner)
    {
      return new TLQualityPerEvaluationsAnalyzer(this, cloner);
    }

    public override IOperation Apply()
    {
      var bestQuality = BestQualityParameter.ActualValue.Value;
      var evalSols = EvaluatedSolutionsParameter.ActualValue;
      var results = ResultsParameter.ActualValue;
      double bestTestQuality = 0.0;

      if (results == null) return base.Apply();

      ISymbolicRegressionSolution trainingSolution = (ISymbolicRegressionSolution)results["Best training solution"].Value;
      if (trainingSolution != null)
      {
        bestTestQuality = trainingSolution.TestRSquared;
      }

      var evaluations = 0.0;
      if (evalSols != null) evaluations += evalSols.Value;

      if (evaluations > 0)
      {
        var dataTable = QualityPerEvaluationsParameter.ActualValue;
        var values = dataTable.Rows["First-hit Graph"].Values;

        dataTable = QualityPerEvaluationsTestParameter.ActualValue;
        var testValues = dataTable.Rows["First-hit Graph"].Values;

        var newEntry = Tuple.Create(evaluations, bestQuality);
        var newEntryTest = Tuple.Create(evaluations, bestTestQuality);

        if (values.Count == 0)
        {
          values.Add(newEntry); // record the first data
          values.Add(Tuple.Create(evaluations, bestQuality)); // last entry records max number of evaluations

          //same for test
          testValues.Add(newEntryTest);
          testValues.Add(Tuple.Create(evaluations, bestTestQuality));

          return base.Apply();
        }

        var improvement = values.Last().Item2 != bestQuality;
        if (improvement)
        {
          values[values.Count - 1] = newEntry; // record the improvement
          values.Add(Tuple.Create(evaluations, bestQuality)); // last entry records max number of evaluations
        }
        else
        {
          values[values.Count - 1] = Tuple.Create(evaluations, bestQuality); // the last entry is updated
        }

        var improvementTest = testValues.Last().Item2 != bestTestQuality;
        if (improvementTest)
        {
          testValues[testValues.Count - 1] = newEntryTest;
          testValues.Add(Tuple.Create(evaluations, bestTestQuality));
        }
        else
        {
          testValues[testValues.Count - 1] = Tuple.Create(evaluations, bestTestQuality);
        }
      }
      return base.Apply();
    }
  }
}
