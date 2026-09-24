using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataGenerator
{
    internal class Program
    {
        /*
            Change the values below to configure the amount of datasets to generate
            and the similarity of the datasets. For each set, a new folder is created that
            contains the datasets. 
        */
        static int nrOfDatasets = 50;
        static List<double> set = new List<double> { 0.3, 0.8 };

        static void Main(string[] args)
        {
            GenerateMultipleSets();

            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadLine();
        }

        public static void GenerateMultipleSets()
        {
            Random random = new Random();

            Parallel.ForEach(set, s =>
            {
                string prefix = s.ToString().Replace(',', '_');

                if (Directory.Exists(prefix))
                {
                    Directory.Delete(prefix, true);
                }
                Directory.CreateDirectory(prefix);

                Console.WriteLine("Generating set " + s + " ...");
                FlexibleGenerator.GenerateModelsFlexible(nrOfDatasets, s, prefix);
                Console.WriteLine("Done generating set: " + s);

            });
        }
    }
}

