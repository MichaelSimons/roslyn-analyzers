
[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $root,
    [String] $output
  )

try {
    $project = Join-Path $root "src\PerformanceTests\CSharp\CSharpPerformanceTests.csproj"
    Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList "run -c Release --project $project -- --memory --exporters JSON --artifacts $output --profiler ETW --filter *"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    $host.SetShouldExit(1)
    exit
}
finally {
}