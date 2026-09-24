namespace ExperimentRunner
{
    public interface IExperimentRunner
    {
        public IExperimentConfig Config { get; set; }

        public void Run(IExperimentConfig config, int curNrOfSourceInstances);
    }
}