using System;
using System.Collections.Generic;
using System.Linq;
using HeuristicLab.Optimization;
using System.IO;
using HeuristicLab.Analysis;
using System.Text;
using HeuristicLab.Data;
using System.Globalization;

namespace ExperimentRunner
{
    public static class CsvExporter
    {
        public static int StepSize = 45;
        private static string PathName;

        //10% percent of the best quality value is considered as good quality, used for the RLD calculation
        public static double QualityRange = 0.90;

        public static void Export(string path, List<IOptimizer> experiments, IOptimizer normalRuns)
        {
            PathName = path;
            if (!Directory.Exists(PathName))
            {
                Directory.CreateDirectory(PathName);
            }

            var maxQualities = CalcMaxQualityValues(normalRuns.Runs);
            var maxTestQualities = CalcMaxQualityValues(normalRuns.Runs, true);
            CalcMaxQualityValuesTL(experiments.First().Runs, maxQualities);
            CalcMaxQualityValuesTL(experiments.First().Runs, maxTestQualities, true);
            CalcMaxQualityValuesTL(experiments.Last().Runs, maxQualities);
            CalcMaxQualityValuesTL(experiments.Last().Runs, maxTestQualities, true);

            for (int i = 0; i < maxQualities.Count; i++)
            {
                maxQualities[maxQualities.ElementAt(i).Key] = maxQualities.ElementAt(i).Value * QualityRange;
            }

            for (int i = 0; i < maxTestQualities.Count; i++)
            {
                maxTestQualities[maxTestQualities.ElementAt(i).Key] = maxTestQualities.ElementAt(i).Value * QualityRange;
            }

            foreach (var experiment in experiments)
            {
                RunCollection rc = experiment.Runs;
                ExportOverallRLD(experiment.Name, rc, maxQualities, maxTestQualities);
                ExportAllQualities(experiment.Runs, experiment.Name);
            }

            ExportAllQualities(normalRuns.Runs, normalRuns.Name);
            ExportOverallRLD(normalRuns.Name, normalRuns.Runs, maxQualities, maxTestQualities);
            ExportQualityEvaluations(normalRuns.Runs, normalRuns.Name);
            ExportQualityEvaluations(experiments.First().Runs, experiments.First().Name);
            ExportQualityEvaluations(experiments.Last().Runs, experiments.Last().Name);


            Console.WriteLine("Export finished!");
        }

        public static void ExportAllQualities(RunCollection rc, string name)
        {
            List<IRun> correctRuns = GetRuns(rc);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name;Pearson Training;Pearson Test");

            foreach (Run run in correctRuns)
            {
                String algName = run.Parameters["Algorithm Name"].ToString();
                DoubleValue pTraining = (DoubleValue)run.Results.Single(x => x.Key.Contains("Best training solution.Pearson's R² (training)")).Value;
                DoubleValue pTest = (DoubleValue)run.Results.Single(x => x.Key.Contains("Best training solution.Pearson's R² (test)")).Value;

                sb.AppendLine(algName + ";" + pTraining.Value.ToString(CultureInfo.InvariantCulture) + ";" + pTest.Value.ToString(CultureInfo.InvariantCulture));
            }
            File.WriteAllText(Path.Combine(PathName, name + "_AllQualities.csv"), sb.ToString());
        }


        public static Dictionary<string, double> CalcMaxQualityValues(RunCollection rc, bool test = false)
        {
            var names = rc.Select(x => x.Parameters["Algorithm Name"].ToString()).Distinct();
            Dictionary<string, double> maxQValue = new Dictionary<string, double>();

            foreach (var name in names)
            {
                var currentRuns = rc.Where(x => x.Parameters["Algorithm Name"].ToString() == name);
                maxQValue[name + " (T)"] = -1;
                foreach (Run run in currentRuns)
                {
                    IndexedDataTable<double> rldTable;
                    if (!test)
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluations").Value;
                    }
                    else
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluationsTest").Value;
                    }

                    double max = rldTable.Rows.First().Values.Max(x => x.Item2);

                    if (max > maxQValue[name + " (T)"])
                    {
                        maxQValue[name + " (T)"] = max;
                    }
                }
            }
            return maxQValue;
        }

        public static void CalcMaxQualityValuesTL(RunCollection rc, Dictionary<string, double> maxQValue, bool test = false)
        {
            List<IRun> correctRuns = GetRuns(rc);
            var names = correctRuns.Select(x => x.Parameters["Algorithm Name"].ToString()).Distinct();

            foreach (var name in names)
            {
                var currentRuns = rc.Where(x => x.Parameters["Algorithm Name"].ToString() == name);
                foreach (Run run in currentRuns)
                {
                    IndexedDataTable<double> rldTable;
                    if (!test)
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluations").Value;
                    }
                    else
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluationsTest").Value;
                    }

                    double max = rldTable.Rows.First().Values.Max(x => x.Item2);
                    if (max > maxQValue[name])
                    {
                        maxQValue[name] = max;
                    }
                }
            }
        }

        public static void ExportQualityEvaluations(RunCollection rc, string expName, bool test = true)
        {
            List<IRun> correctRuns = GetRuns(rc);
            var names = correctRuns.Select(x => x.Parameters["Algorithm Name"].ToString()).Distinct();

            foreach (var name in names)
            {
                var currentRuns = rc.Where(x => x.Parameters["Algorithm Name"].ToString() == name);
                int cnt = 1;
                foreach (Run run in currentRuns)
                {
                    IndexedDataTable<double> rldTable;
                    if (!test)
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluations").Value;
                    }
                    else
                    {
                        rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluationsTest").Value;
                    }

                    var qualities = rldTable.Rows.First().Values.Select(x => x.Item2);
                    var evaluations = rldTable.Rows.First().Values.Select(x => x.Item1);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < qualities.Count(); i++)
                    {
                        sb.AppendLine(qualities.ElementAt(i).ToString(CultureInfo.InvariantCulture) +
                            ";" + evaluations.ElementAt(i));
                    }

                    File.WriteAllText(Path.Combine(PathName, expName + "_" + name + "_QE_" + cnt + ".csv"), sb.ToString());
                    cnt++;
                }
            }
        }

        public static void ExportOverallRLD(string fileName, RunCollection rc, Dictionary<string, double> maxQualities, Dictionary<string, double> maxTestQualities)
        {
            List<IRun> correctRuns = GetRuns(rc);
            var names = correctRuns.Select(x => x.Parameters["Algorithm Name"].ToString()).Distinct();
            int maxEval = (int)((IndexedDataTable<double>)correctRuns.First().Results.Single(x => x.Key == "QualityPerEvaluations").Value).Rows.First().Values.Max(x => x.Item1);
            int binSize = (int)Math.Ceiling(maxEval / (double)StepSize);


            foreach (var name in names)
            {
                Dictionary<int, int> countHits = new Dictionary<int, int>();
                Dictionary<int, int> countHitsTest = new Dictionary<int, int>();
                for (int i = 1; i <= StepSize; i++)
                {
                    countHits[i * binSize] = 0;
                    countHitsTest[i * binSize] = 0;
                }

                double maxQ;
                double maxQTest;
                if (name.Contains("(T)"))
                {
                    maxQ = maxQualities[name];
                    maxQTest = maxTestQualities[name];
                }
                else
                {
                    maxQ = maxQualities[name + " (T)"];
                    maxQTest = maxTestQualities[name + " (T)"];
                }

                var currentRuns = correctRuns.Where(x => x.Parameters["Algorithm Name"].ToString() == name);
                int nrOfRuns = currentRuns.Count();
                foreach (Run run in currentRuns)
                {
                    var rldTable = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluations").Value;
                    var rldTableTest = (IndexedDataTable<double>)run.Results.Single(x => x.Key == "QualityPerEvaluationsTest").Value;

                    CountOccurencesInBins(rldTable, binSize, maxQ, countHits);
                    CountOccurencesInBins(rldTableTest, binSize, maxQTest, countHitsTest);
                }

                WriteCountHits(countHits, nrOfRuns, fileName, name);
                WriteCountHits(countHitsTest, nrOfRuns, fileName, name + "_Test");
            }
        }

        private static void WriteCountHits(Dictionary<int, int> countHits, int nrOfRuns, string fileName, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bin;Hits");

            foreach (var tuple in countHits)
            {
                double hit = tuple.Value / (double)nrOfRuns;
                sb.AppendLine(tuple.Key + ";" + hit.ToString(CultureInfo.InvariantCulture));
            }

            //write file
            File.WriteAllText(Path.Combine(PathName, fileName + "_RLD_" + name + ".csv"), sb.ToString());
        }

        private static void CountOccurencesInBins(IndexedDataTable<double> rldTable, int binSize, double maxQ, Dictionary<int, int> countHits)
        {
            //init datastructures
            foreach (var tuple in rldTable.Rows.First().Values)
            {
                int bin = ((int)tuple.Item1 / binSize) + 1;

                if (!countHits.ContainsKey(binSize * bin))
                {
                    countHits[binSize * bin] = 0;
                }
            }

            int binSet = -1;
            //loop until maxQ is reached once
            foreach (var tuple in rldTable.Rows.First().Values)
            {
                int bin = ((int)tuple.Item1 / binSize) + 1;

                if (tuple.Item2 >= maxQ)
                {
                    countHits[binSize * bin]++;
                    //if we reached maxq once, we can increase the rest of the bins,
                    //therefore break and proceed in the next loop
                    binSet = bin;
                    break;
                }
            }

            //finish
            if (binSet != -1)
            {
                foreach (var key in countHits.Keys.ToList())
                {
                    if (key > binSet * binSize)
                    {
                        countHits[key]++;
                    }
                }
            }
        }

        //get correct runs for comparison
        public static List<IRun> GetRuns(RunCollection runs)
        {
            if (runs.Count(x => x.Parameters["Algorithm Name"].ToString().Contains("(T)")) > 0)
            {
                return runs.Where(x => x.Parameters["Algorithm Name"].ToString().Contains("(T)")).ToList();
            }
            else
            {
                return runs.ToList();
            }
        }
    }
}
