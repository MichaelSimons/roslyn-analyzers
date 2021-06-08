// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines;
using PerformanceTests.Utilities;

namespace PerformanceTests.OnByDefaultAnalyzer
{
    public class CA1018
    {
        [GlobalSetup]
        public static void CreateEnvironmentCA1018()
        {
            var sources = new List<(string name, string content)>();
            for (int i = 0; i < 1000; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System;

class {name} : Attribute
{{
}}
"));
            }

            var compilation = CSharpCompilationHelper.Create(sources.ToArray()).GetAwaiter().GetResult();
            var analyzer = new MarkAttributesWithAttributeUsageAnalyzer();
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static CompilationWithAnalyzers CompilationWithAnalyzers;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Benchmark]
        public void CA1018_Info()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
