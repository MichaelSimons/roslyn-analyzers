// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.Analyzers.Publish;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class IL3000
    {
        [IterationSetup]
        public static void CreateEnvironmentIL3000()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System.Reflection;
class {name}
{{
    public void M()
    {{
        var a = Assembly.GetExecutingAssembly();
        _ = a.Location;
        _ = a.EscapedCodeBase;
    }}
}}"));
            }

            var compilation = CSharpCompilationHelper.Create(sources.ToArray(), "build_property.PublishSingleFile = true").GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()));
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new AvoidAssemblyLocationInSingleFile()));
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void IL3000_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void IL3000_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
