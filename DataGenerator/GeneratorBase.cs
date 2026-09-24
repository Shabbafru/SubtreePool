using HeuristicLab.Common;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Problems.DataAnalysis.Symbolic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DataGenerator
{
    public abstract class GeneratorBase
    {
        public static double CalcSimilarity(IList<ISymbolicExpressionTree> trees, int nrOfDatasets, string prefix)
        {
            var simMatrix = SymbolicExpressionTreeHash.ComputeSimilarityMatrix(trees);

            string similarity = "";
            double sum = 0;
            double sumLength = 0;
            for (int i = 0; i < nrOfDatasets; i++)
            {
                for (int j = 0; j < nrOfDatasets; j++)
                {
                    similarity += simMatrix[i, j].ToString(new CultureInfo("de-DE")) + " ";
                    sum += simMatrix[i, j];
                }
                sumLength += trees[i].Root.GetLength();
                similarity += Environment.NewLine;
            }

            string avgStr = "[" + prefix + "] Average similarity: " + sum / (nrOfDatasets * nrOfDatasets);
            avgStr += Environment.NewLine + "Average length: " + sumLength / nrOfDatasets + Environment.NewLine;
            avgStr += "Variance: " + trees.Select(x => (double)x.Root.GetLength()).Variance() + Environment.NewLine;
            avgStr += "Avg. nr. of variables: " + trees.Select(x => x.Root.IterateNodesBreadth().Count(y => y.Symbol is Variable)).Average() + Environment.NewLine;

            similarity += avgStr;
            Console.WriteLine(avgStr);
            File.WriteAllText(prefix + "\\similarity.csv", similarity);
            return sum / (nrOfDatasets * nrOfDatasets);
        }

        public abstract void Generate(int nrOfDatasets, double similarity);
    }
}
