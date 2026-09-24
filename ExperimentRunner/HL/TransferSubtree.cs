using System.Collections.Generic;
using HEAL.Attic;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;

namespace HeuristicLab.Problems.DataAnalysis.Symbolic.Regression
{

  [Item("Subtree", "A wrapper for a subtree. Contains additional information about the subtree used for transferring the subtree. ")]
  [StorableType("85A1AB82-689F-5225-B888-A4886707BE88")]
  public class TransferSubtree : Item
  {
    [Storable]
    public ISymbolicExpressionTreeNode SubTree { get; set; }

    [Storable]
    public string ProblemName { get; set; }

    [Storable]
    public Dictionary<string, double> ProblemQualities { get; set; }

    public IDataset DataSet { get; set; }

    [StorableConstructor]
    private TransferSubtree(StorableConstructorFlag _) : base(_) { }
    protected TransferSubtree(TransferSubtree original, Cloner cloner) : base(original, cloner)
    {
      this.SubTree = (ISymbolicExpressionTreeNode)original.SubTree.Clone(cloner);
      this.ProblemName = original.ProblemName;
      this.ProblemQualities = new Dictionary<string, double>(original.ProblemQualities);
      this.DataSet = original.DataSet; // Assuming DataSet is a reference type and shallow copy is sufficient
    }
    public TransferSubtree()
      : base()
    {
      ProblemQualities = new Dictionary<string, double>();
    }
    public TransferSubtree(ISymbolicExpressionTreeNode subtree)
      : base()
    {
      SubTree = subtree;
      ProblemQualities = new Dictionary<string, double>();
    }
    public TransferSubtree(ISymbolicExpressionTreeNode subtree, string problemName)
     : base()
    {
      SubTree = subtree;
      ProblemName = problemName;
      ProblemQualities = new Dictionary<string, double>();
    }
    public override IDeepCloneable Clone(Cloner cloner)
    {
      return new TransferSubtree(this, cloner);
    }
  }
}

