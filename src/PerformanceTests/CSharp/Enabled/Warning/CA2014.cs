// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.CSharp.Analyzers.Runtime;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class CA2014
    {
        [IterationSetup]
        public static void CreateEnvironmentCA2014()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System;
unsafe class {name} {{
    private static void ForLoop() {{
        for (int i = 0; i < 10; i++)
        {{
            Span<byte> span = stackalloc byte[1024]|;
        }}
    }}

    private static void WhileLoop() {{
        while (true)
        {{
            Span<char> span = stackalloc char[1024];
        }}
    }}

    private static void DoWhile() {{
        do
        {{
            Span<int> span = stackalloc int[1024];
        }}
        while (true);
    }}
}}
"));
            }

            var compilation = CSharpCompilationHelper.Create(sources.ToArray()).GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()));
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new CSharpDoNotUseStackallocInLoopsAnalyzer()));
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void CA2014_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void CA2014_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
