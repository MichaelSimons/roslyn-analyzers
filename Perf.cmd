@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\perf\ComparePerfToBaseLine.ps1"""  -baselineSHA bbd59082491596bca2bfc477e435dc1c148bb340 -testSHA HEAD -output artifacts\perfResults %*"
exit /b %ErrorLevel%