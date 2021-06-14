// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.Analyzers.InteropServices;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class CA1417
    {
        [IterationSetup]
        public static void CreateEnvironmentCA1417()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System;
using System.Runtime.InteropServices;

class {name}
{{
    [DllImport(""user32.dll"", CharSet=CharSet.Unicode)]
    private static extern void Method1([Out] string s); // Should not have [Out] string

    [DllImport(""user32.dll"", CharSet=CharSet.Unicode)]
    private static extern void Method2(string s1, [Out] string s2, [Out] string s3); // Should not have [Out] string
}}
"));
            }

            // TODO: Verify  Out attribute is defined
            var compilation = CSharpCompilationHelper.Create(sources.ToArray()).GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()));
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DoNotUseOutAttributeStringPInvokeParametersAnalyzer()));
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void CA1417_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void CA1417_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
