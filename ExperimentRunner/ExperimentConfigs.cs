using System;

namespace ExperimentRunner
{
    public enum MappingTypeEnum
    {
        Random,
        MonteCarlo,
        Wasserstein,
        MDD
    }

    public interface IExperimentConfig
    {
        int NrOfTargetInstances { get; set; }
        int[] NrOfSourceInstances { get; set; }
        double CacheSizeFactor { get; set; }
        int MaxGenerations { get; set; }
        int PopSize { get; set; }
        int BatchRepetitions { get; set; }
        string DataPath { get; set; }
        bool DataPathSubDirs { get; set; }
        string ResultPath { get; set; }
        MappingTypeEnum MappingType { get; set; }
    }

    //Experiment with PMLB datasets showing variable mapping between instances with different variables names
    public class PmlbVariableMappingConfig : IExperimentConfig
    {
        public int NrOfTargetInstances { get; set; } = 27;
        public int[] NrOfSourceInstances { get; set; } = new int[] { 15 };
        public double CacheSizeFactor { get; set; } = 3.0;
        public int MaxGenerations { get; set; } = 700;
        public int PopSize { get; set; } = 200;
        public int BatchRepetitions { get; set; } = 30;
        public string DataPath { get; set; }
        public bool DataPathSubDirs { get; set; } = true;
        public string ResultPath { get; set; }
        public MappingTypeEnum MappingType { get; set; } = MappingTypeEnum.MonteCarlo;
    }

    //config used for testing different mapping approaches for PMLB datasets
    public class PmlbVariableMappingTestConfig : IExperimentConfig
    {
        public int NrOfTargetInstances { get; set; } = 10;
        public int[] NrOfSourceInstances { get; set; } = new int[] { 10 };
        public double CacheSizeFactor { get; set; } = 3.0;
        public int MaxGenerations { get; set; } = 500;
        public int PopSize { get; set; } = 100;
        public int BatchRepetitions { get; set; } = 30;
        public string DataPath { get; set; }
        public bool DataPathSubDirs { get; set; } = true;
        public string ResultPath { get; set; }
        public MappingTypeEnum MappingType { get; set; } = MappingTypeEnum.MonteCarlo;
    }

    // experiment where the similarity between source and target instances is varied 
    public class GeneratedInstancesSimilarityConfig : IExperimentConfig
    {
        public int NrOfTargetInstances { get; set; } = 85;
        public int[] NrOfSourceInstances { get; set; } = new int[] { 15 };
        public double CacheSizeFactor { get; set; } = 3.0;
        public int MaxGenerations { get; set; } = 700;
        public int PopSize { get; set; } = 200;
        public int BatchRepetitions { get; set; } = 30;
        public string DataPath { get; set; }
        public bool DataPathSubDirs { get; set; } = false;
        public string ResultPath { get; set; }
        public MappingTypeEnum MappingType { get; set; } = MappingTypeEnum.MonteCarlo;
    }

    // experiment with increasing the number of source instances
    public class GeneratedInstancesSourceNrConfig : IExperimentConfig
    {
        public int NrOfTargetInstances { get; set; } = 25;
        public int[] NrOfSourceInstances { get; set; } = new int[] { 150 };
        public double CacheSizeFactor { get; set; } = 3.0;
        public int MaxGenerations { get; set; } = 700;
        public int PopSize { get; set; } = 200;
        public int BatchRepetitions { get; set; } = 30;
        public string DataPath { get; set; }
        public bool DataPathSubDirs { get; set; } = false;
        public string ResultPath { get; set; }
        public MappingTypeEnum MappingType { get; set; } = MappingTypeEnum.MonteCarlo;
    }
}