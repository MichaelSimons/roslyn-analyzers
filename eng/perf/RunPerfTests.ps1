
[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $root,
    [String] $output
  )

try {
    $project = Join-Path $root "src\PerformanceTests\CSharp\CSharpPerformanceTests.csproj"
    $build = Join-Path $root "eng\common\build.ps1"
    Write-Output "builing release"
    Invoke-Expression "$build -restore"
    Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList "run -c Release --project $project -- --memory --exporters JSON --artifacts $output --profiler ETW --filter *"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    ExitWithExitCode 1
}
finally {
}