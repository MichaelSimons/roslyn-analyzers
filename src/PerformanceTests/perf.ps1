
Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList "run -c Release --project CSharp\CSharpPerformanceTests.csproj -- --memory --exporters JSON --profiler ETW --filter *"