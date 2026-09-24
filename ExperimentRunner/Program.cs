using System;

namespace ExperimentRunner
{
    internal class Program
    {
        static IExperimentConfig config = null;

        static IExperimentRunner runner = null;

        static void Main(string[] args)
        {
            runner = new VariableMappingExperimentRunner();

            // config for generated datasets with different similarity levels
            config = new GeneratedInstancesSimilarityConfig();
            config.DataPath = "datasets_diversity\\0_3\\";
            config.ResultPath = "results_diversity\\0_3\\";
            

            // config for generated datasets where the nr. of source instances varies
            /*
            config = new GeneratedInstancesSourceNrConfig();
            config.DataPath = "datasets_source_nr\\0_3\\";
            config.ResultPath = "results_source_nr\\";
            */

            //for pmlb instances
            /*
            config = new PmlbVariableMappingConfig();
            config.DataPath = "datasets_pmlb\\";
            config.ResultPath = "results_pmlb\\";
            */

            //test config pmlb
            /*
            config = new PmlbVariableMappingTestConfig();
            config.DataPath = "datasets_pmlb\\";
            config.ResultPath = "results_pmlb\\";
            config.MappingType = MappingTypeEnum.MDD;
            */

            Console.WriteLine("Running experiments in " + config.DataPath);
            Console.WriteLine("Results will be saved in " + config.ResultPath);

            foreach (var sourceNr in config.NrOfSourceInstances)
            {
                Console.WriteLine("Starting experiment with source instances: " + sourceNr);
                runner.Run(config, sourceNr);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            Console.WriteLine("Done, bye!");
        }
    }
}

