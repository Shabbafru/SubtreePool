# Subtree-Pool


## Building
- Building requirement: .NET 9 SDK
- Dependencies: After checkout, run `git submodule init` and `git submodule update`
- Compile HeuristicLab: `dotnet publish Headless-HeuristicLab`
- Compiling: 
    - DataGenerator: `dotnet build DataGenerator`
    - ExperimentRunner: `dotnet build ExperimentRunner`
- The programs are located in `bin/Debug/net9.0/` of the respective folder


## Running the applications

### DataGenerator
- Run `DataGenerator\bin\Debug\net9.0\DataGenerator.exe`
- 2 folders will be created with datasets of 0.3 and 0.8 similarity
- To change the number and similarity of created datasets, change the parameters in `Program.cs` and recompile

### ExperimentRunner
- Run `ExperimentRunner\bin\Debug\net9.0\ExperimentRunner.exe`
- To change the config for the different experiments, check out `Program.cs`








