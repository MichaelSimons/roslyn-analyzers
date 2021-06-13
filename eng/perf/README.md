# Performance Scripts Design

Files
- eng\perf
  - **baseline.json** lists the commit SHA to serve as the baseline
  - **CIPerf.cmd** Runs HEAD against whatever is in baseline.json
  - **ComparePerfToBaseLine.ps1** compares two SHAs
  - **RunPerfTests.ps1** runs a single perf test
  - **ComparePerfResults.ps1** compares two runs

- Perf.cmd
  - no args -> perfTests.ps1
  - diff -> ComparePerfToBaseLine.ps1
    - RunPerfTests.ps1
    - ComparePerfResults.ps1

## Examples

```
.\eng\perf\ComparePerfToBaseLine.ps1 -baselineSHA cf1ada51bd177620a46bc99879b0d018be46d8e4 -testSHA 19b220df0ce06df831e7a230897a1e600e02a34c -output artifacts\perfResults
```

## ComparePerfToBaseLine.ps1

### Inputs
- `baselineSHA`: Baseline SHA
- `testSHA`: Test SHA, default is HEAD
- `output`:Test results Folder
- `failureMode`: whether to have a non-zero exit code if perf regresses
- `variance`: amount of perf variance that is allowed

### Cancellation

- on cancellation all worktrees need to be removed

### Outputs

### Running
1. Verify
    - Git installed
1. Checkout Baseline SHA to temp folder
    - `git worktree add perfBaseline $baselineSHA`
1. Run Baseline perf tests
    - call `perfBaseline\RunPerfTests.ps1 -output $output\baseline`
1. Remove Baseline worktree
    - `git worktree remove perfBaseline`
1. Checkout Test SHA to temp folder
    - `git worktree add perfTest $testSHA`
1. Run perf tests
    - call `perfTest\RunPerfTests.ps1 -output $output\perfTest`
1. Diff Results
    - call `compare.ps1 -baseline $output\baseline -results $output\perfTest -variance $variance`

## RunPerfTests.ps1

### Inputs

-`output` a folder to place the results

### Cancellation
### Outputs
### Running


## ComparePerfResults.ps1

### Inputs
- `baseline`: path to folder/file with baseline results
- `results`: path to folder/file with diff results
- `variance`: threshold for Statistical Test. Examples: 5%, 10ms, 100ns, 1s
- `output`: (Optional) output directory to place results

### Cancellation

- nothing to cleanup in the even of cancellation

### Outputs

- If `output` is specified it outputs a csv file with the comparison results

### Running
- Verify
  - the correct runtime is installed for the global tool
- Grab the results Comparer Nuget Package/Global tool
  - `dotnet tool install --add-source C:\source\dotnet\performance\artifacts\packages\Debug\Shipping -g ResultsComparer --version 1.0.0-dev`
  - `dotnet ResultsComparer --base $baseline --diff $results --threshold $variance --noise 30% -csv 
  - compare csv file results???