[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baseline,
    [String] $results,
    [String] $output
  )
  
function EnsureFolder {
param (
    [String] $path
)
    If(!(test-path $path))
    {
        New-Item -ItemType Directory -Force -Path $path
    }
}

$currentLocation = Get-Location
try {
    
    #Get result files
    $baselineFolder = Join-Path $baseline "results"
    
    $resultsFolder = Join-Path $results "results"
    
    $logFile = Join-Path $output "compareLog.txt"
    
    # Get json comparison tool

    $perfDiff = Join-Path $root "src\Tools\PerfDiff\PerfDiff.csproj"
    Invoke-Expression "dotnet run -c Release --project $perfDiff -- --baseline $baselineFolder --results $resultsFolder --failOnRegression --verbosity diag" > $logFile
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    ExitWithExitCode 1
}
finally {
    Set-Location $currentLocation
}