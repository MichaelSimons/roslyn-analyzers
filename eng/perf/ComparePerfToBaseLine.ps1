[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baselineSHA,
    [String] $testSHA,
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

# Setup paths
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$RunPerfTests = Join-Path $PSScriptRoot "RunPerfTests.ps1"
$ComparePerfResults = Join-Path $PSScriptRoot "ComparePerfResults.ps1"
$baselinejson = Join-Path $PSScriptRoot "baseline.json"
$Tools = Join-Path $RepoRoot ".tools"
$Temp = Join-Path $RepoRoot "temp"
$performanceDir = Join-Path $Tools "performance"

try {
    # Verify git installed
    # Set args to default if not specified
    if ($baselineSHA -eq "") {
        $json = (Get-Content $baselinejson  -Raw) | ConvertFrom-Json
        $baselineSHA = $json.sha
    }
    
    if ($testSHA -eq "") {
        $testSHA = "HEAD"
    }
    
    if ($output -eq "") {
        $output = Join-Path $RepoRoot "artifacts\perfResults"
    }
    
    # Ensure we have the needed runtimes installed
    $Build = Join-Path $RepoRoot ".\eng\common\build.ps1"
    Invoke-Expression "$Build -restore"
    
    # Ensure baseline output directory has been created
    $baselineOutput = Join-Path $output "baseline"
    EnsureFolder $baselineOutput
    
    # Checkout baseline SHA
    $baselinePath = Join-Path $Temp "perfBaseline"
    Invoke-Expression "git worktree add $baselinePath $baselineSHA"
    Invoke-Expression "$RunPerfTests -root $baselinePath -output $baselineOutput"
    
    # Ensure test output directory has been created
    $testOutput = Join-Path $output "perfTest"
    EnsureFolder $testOutput
    
    # Checkout Test SHA to temp folder
    $testPath = Join-Path $Temp "perfTest"
    Invoke-Expression "git worktree add $testPath $testSHA"
    Invoke-Expression "$RunPerfTests -root $testPath -output $testOutput"
    
    # Diff Results
    
    Invoke-Expression "$ComparePerfResults -baseline $baselineOutput -results $testOutput -output $output"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    $host.SetShouldExit(1)
    exit
}
finally {
    Invoke-Expression 'git worktree remove perfBaseline'
    Invoke-Expression 'git worktree remove perfTest'
    Invoke-Expression 'git worktree prune'
    Set-Location $currentLocation
}