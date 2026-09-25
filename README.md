# Description
Source code and datasets for the paper "Multi-Source Subtree-Pool Transfer Learning for Symbolic Regression".

## Building the code
- Requirements: .NET 9 SDK
- Dependencies: After checkout, run `git submodule init` and `git submodule update`
- Compile HeuristicLab: `dotnet publish Headless-HeuristicLab`
- Compiling: 
    - DataGenerator: `dotnet build DataGenerator`
    - ExperimentRunner: `dotnet build ExperimentRunner`
- The compiled programs are then located in `bin/Debug/net9.0/` of the respective folder
- All experiments were performed on Windows, other operating systems should theoretically work (the path separators in the code would need to be adapted at least)

## Running the applications

### DataGenerator
- Run `DataGenerator\bin\Debug\net9.0\DataGenerator.exe`
- 2 folders will be created with datasets of 0.3 and 0.8 similarity
- To change the number and similarity of created datasets, change the parameters in `Program.cs` and recompile

### ExperimentRunner
- Run `ExperimentRunner\bin\Debug\net9.0\ExperimentRunner.exe`
- By default, the 0.3 datasets in datasets_diversity are used
- To change the config for the different experiments, edit the config in `Program.cs` and recompile


## Datasets
The folder datasets_diversity contains two subfolders for datasets with a similarity of 0.3 and 0.8. 
This is used to compare how well the method performs on different dataset similarities. 
The folder datasets_source_nr contains 200 instances of the 0.3 similarity domain. 
It is used to show improvements when used more datasets as source instances. 





