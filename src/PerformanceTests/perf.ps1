
Start-Process -FilePath "dotnet" -Verb RunAs -ArgumentList "run -c Release --project PerformanceTests.csproj -- --memory --exporters JSON --profiler ETW --filter *"