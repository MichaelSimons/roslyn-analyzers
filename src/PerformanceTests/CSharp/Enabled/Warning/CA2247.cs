// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.Analyzers.Tasks;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class CA2247
    {
        [IterationSetup]
        public static void CreateEnvironmentCA2247()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System.Threading.Tasks;

class {name}
{{
    void M()
    {{
        // Use TCS correctly without options
        new TaskCompletionSource<int>(null);
        new TaskCompletionSource<int>(""hello"");
        new TaskCompletionSource<int>(new object());
        new TaskCompletionSource<int>(42);

        // Uses TaskCreationOptions correctly
        var validEnum = TaskCreationOptions.RunContinuationsAsynchronously;
        new TaskCompletionSource<int>(validEnum);
        new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        new TaskCompletionSource<int>(this.MyProperty);
        new TaskCompletionSource<int>(new object(), validEnum);
        new TaskCompletionSource<int>(new object(), TaskCreationOptions.RunContinuationsAsynchronously);
        new TaskCompletionSource<int>(new object(), this.MyProperty);
        new TaskCompletionSource<int>(null, validEnum);
        new TaskCompletionSource<int>(null, TaskCreationOptions.RunContinuationsAsynchronously);
        new TaskCompletionSource<int>(null, this.MyProperty);

        // We only pay attention to things of type TaskContinuationOptions
        new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously.ToString());
        new TaskCompletionSource<int>(TaskContinuationOptions.RunContinuationsAsynchronously.ToString());
        new TaskCompletionSource<int>((int)TaskCreationOptions.RunContinuationsAsynchronously);
        new TaskCompletionSource<int>((int)TaskContinuationOptions.RunContinuationsAsynchronously);

        // Explicit choice to store into an object; ignored
        object validObject = TaskCreationOptions.RunContinuationsAsynchronously;
        new TaskCompletionSource<int>(validObject);
        object invalidObject = TaskContinuationOptions.RunContinuationsAsynchronously;
        new TaskCompletionSource<int>(invalidObject);
    }}
    TaskCreationOptions MyProperty {{ get; set; }}
}}
"));
            }

            var compilation = CSharpCompilationHelper.Create(sources.ToArray()).GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()));
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DoNotCreateTaskCompletionSourceWithWrongArguments()));
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void CA2247_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void CA2247_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
