// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeQuality.Analyzers.QualityGuidelines;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class CA2200
    {
        [IterationSetup]
        public static void CreateEnvironmentCA2200()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System;

class {name}
{{
    void CatchAndRethrowExplicitly()
    {{
        try
        {{
            ThrowException();
        }}
        catch (ArithmeticException e)
        {{
            throw e;
        }}
    }}

    void ThrowException()
    {{
        throw new ArithmeticException();
    }}
}}
"));
            }

            var compilation = CSharpCompilationHelper.Create(sources.ToArray()).GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()));
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new RethrowToPreserveStackDetailsAnalyzer()));
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void CA2200_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void CA2200_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
