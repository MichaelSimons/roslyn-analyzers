// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.NetCore.Analyzers.InteropServices;
using PerformanceTests.Utilities;
using PerfUtilities;

namespace CSharpPerformanceTests.Enabled.Warning
{
    public class CA1416
    {
        [IterationSetup]
        public static void CreateEnvironmentCA1416()
        {
            var sources = new List<(string name, string content)>();
            for (var i = 0; i < Constants.Number_Of_Code_Files; i++)
            {
                var name = "TypeName" + i;
                sources.Add((name, @$"
using System;

class {name}
{{
    public static void Supported()
    {{
        var supported = new TypeWithoutAttributes();
        supported.FunctionSupportedOnWindows(); // This call site is reachable on all platforms. 'TypeWithoutAttributes.FunctionSupportedOnWindows()' is only supported on: 'windows'.
        supported.FunctionSupportedOnWindows10(); // This call site is reachable on all platforms. 'TypeWithoutAttributes.FunctionSupportedOnWindows10()' is only supported on: 'windows' 10.0 and later.
        supported.FunctionSupportedOnWindows10AndBrowser(); // This call site is reachable on all platforms. 'TypeWithoutAttributes.FunctionSupportedOnWindows10AndBrowser()' is only supported on: 'windows' 10.0 and later, 'browser'.

        var supportedOnWindows = new TypeSupportedOnWindows(); // This call site is reachable on all platforms. 'TypeSupportedOnWindows' is only supported on: 'windows'.
        supportedOnWindows.FunctionSupportedOnBrowser(); // browser support ignored
        supportedOnWindows.FunctionSupportedOnWindows11AndBrowser(); //This call site is reachable on all platforms. 'TypeSupportedOnWindows.FunctionSupportedOnWindows11AndBrowser()' is only supported on: 'windows' 11.0 and later.

        var supportedOnBrowser = :new TypeSupportedOnBrowser();
        supportedOnBrowser.FunctionSupportedOnWindows(); // This call site is reachable on all platforms. 'TypeSupportedOnBrowser.FunctionSupportedOnWindows()' is only supported on: 'browser'.

        var supportedOnWindows10 = new TypeSupportedOnWindows10(); // This call site is reachable on all platforms. 'TypeSupportedOnWindows10' is only supported on: 'windows' 10.0 and later.
        supportedOnWindows10.FunctionSupportedOnBrowser(); // child function support will be ignored

        var supportedOnWindowsAndBrowser = new TypeSupportedOnWindowsAndBrowser(); // This call site is reachable on all platforms. 'TypeSupportedOnWindowsAndBrowser' is only supported on: 'windows', 'browser'.
        supportedOnWindowsAndBrowser.FunctionSupportedOnWindows11(); // This call site is reachable on all platforms. 'TypeSupportedOnWindowsAndBrowser.FunctionSupportedOnWindows11()' is only supported on: 'windows' 11.0 and later, 'browser'.
    }}

    public static void Unsupported()
    {{
        var unsupported = new TypeWithoutAttributes();
        unsupported.FunctionUnsupportedOnWindows();
        unsupported.FunctionUnsupportedOnBrowser();
        unsupported.FunctionUnsupportedOnWindows10();
        unsupported.FunctionUnsupportedOnWindowsAndBrowser();
        unsupported.FunctionUnsupportedOnWindows10AndBrowser();

        var unsupportedOnWindows = new TypeUnsupportedOnWindows();
        unsupportedOnWindows.FunctionUnsupportedOnBrowser();
        unsupportedOnWindows.FunctionUnsupportedOnWindows11();
        unsupportedOnWindows.FunctionUnsupportedOnWindows11AndBrowser();

        var unsupportedOnBrowser = new TypeUnsupportedOnBrowser();
        unsupportedOnBrowser.FunctionUnsupportedOnWindows();
        unsupportedOnBrowser.FunctionUnsupportedOnWindows10();

        var unsupportedOnWindows10 = new TypeUnsupportedOnWindows10();
        unsupportedOnWindows10.FunctionUnsupportedOnBrowser();
        unsupportedOnWindows10.FunctionUnsupportedOnWindows11();
        unsupportedOnWindows10.FunctionUnsupportedOnWindows11AndBrowser();

        var unsupportedOnWindowsAndBrowser = new TypeUnsupportedOnWindowsAndBrowser();
        unsupportedOnWindowsAndBrowser.FunctionUnsupportedOnWindows11();

        var unsupportedOnWindows10AndBrowser = new TypeUnsupportedOnWindows10AndBrowser();
        unsupportedOnWindows10AndBrowser.FunctionUnsupportedOnWindows11();
    }}

    public static void UnsupportedCombinations() // no any diagnostics as it is deny list
    {{
        var withoutAttributes = new TypeWithoutAttributes();
        withoutAttributes.FunctionUnsupportedOnWindowsSupportedOnWindows11();
        withoutAttributes.FunctionUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12();
        withoutAttributes.FunctionUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12SupportedOnWindows13();

        var unsupportedOnWindows = new TypeUnsupportedOnWindows();
        unsupportedOnWindows.FunctionSupportedOnWindows11();
        unsupportedOnWindows.FunctionSupportedOnWindows11UnsupportedOnWindows12();
        unsupportedOnWindows.FunctionSupportedOnWindows11UnsupportedOnWindows12SupportedOnWindows13();

        var unsupportedOnBrowser = new TypeUnsupportedOnBrowser();
        unsupportedOnBrowser.FunctionSupportedOnBrowser();

        var unsupportedOnWindowsSupportedOnWindows11 = new TypeUnsupportedOnWindowsSupportedOnWindows11();
        unsupportedOnWindowsSupportedOnWindows11.FunctionUnsupportedOnWindows12();
        unsupportedOnWindowsSupportedOnWindows11.FunctionUnsupportedOnWindows12SupportedOnWindows13();

        var unsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12 = new TypeUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12();
        unsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12.FunctionSupportedOnWindows13();
    }}
}}
"));
            }

            var targetTypesForTest = @"
namespace PlatformCompatDemo.SupportedUnupported
{
    public class TypeWithoutAttributes
    {
        [UnsupportedOSPlatform(""windows"")]
        public void FunctionUnsupportedOnWindows() { }

        [UnsupportedOSPlatform(""browser"")]
        public void FunctionUnsupportedOnBrowser() { }

        [UnsupportedOSPlatform(""windows10.0"")]
        public void FunctionUnsupportedOnWindows10() { }

        [UnsupportedOSPlatform(""windows""), UnsupportedOSPlatform(""browser"")]
        public void FunctionUnsupportedOnWindowsAndBrowser() { }

        [UnsupportedOSPlatform(""windows10.0""), UnsupportedOSPlatform(""browser"")]
        public void FunctionUnsupportedOnWindows10AndBrowser() { }

        [UnsupportedOSPlatform(""windows""), SupportedOSPlatform(""windows11.0"")]
        public void FunctionUnsupportedOnWindowsSupportedOnWindows11() { }

        [UnsupportedOSPlatform(""windows""), SupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""windows12.0"")]
        public void FunctionUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12() { }

        [UnsupportedOSPlatform(""windows""), SupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""windows12.0""), SupportedOSPlatform(""windows13.0"")]
        public void FunctionUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12SupportedOnWindows13() { }

        [SupportedOSPlatform(""windows"")]
        public void FunctionSupportedOnWindows() { }

        [SupportedOSPlatform(""windows10.0"")]
        public void FunctionSupportedOnWindows10() { }

        [SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnBrowser() { }

        [SupportedOSPlatform(""windows""), SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnWindowsAndBrowser() { }

        [SupportedOSPlatform(""windows10.0""), SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnWindows10AndBrowser() { }
    }

    [UnsupportedOSPlatform(""windows"")]
    public class TypeUnsupportedOnWindows {
        [UnsupportedOSPlatform(""browser"")] // more restrictive should be OK
        public void FunctionUnsupportedOnBrowser() { }

        [UnsupportedOSPlatform(""windows11.0"")]
        public void FunctionUnsupportedOnWindows11() { }

        [UnsupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""browser"")]
        public void FunctionUnsupportedOnWindows11AndBrowser() { }

        [SupportedOSPlatform(""windows11.0"")]
        public void FunctionSupportedOnWindows11() { }

        [SupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""windows12.0"")]
        public void FunctionSupportedOnWindows11UnsupportedOnWindows12() { }

        [SupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""windows12.0""), SupportedOSPlatform(""windows13.0"")]
        public void FunctionSupportedOnWindows11UnsupportedOnWindows12SupportedOnWindows13() { }
    }

    [UnsupportedOSPlatform(""browser"")]
    public class TypeUnsupportedOnBrowser
    {
        [UnsupportedOSPlatform(""windows"")] // more restrictive should be OK
        public void FunctionUnsupportedOnWindows() { }

        [UnsupportedOSPlatform(""windows10.0"")] // more restrictive should be OK
        public void FunctionUnsupportedOnWindows10() { }
        
        [SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnBrowser() { }
    }

    [UnsupportedOSPlatform(""windows10.0"")]
    public class TypeUnsupportedOnWindows10
    {
        [UnsupportedOSPlatform(""browser"")] // more restrictive should be OK
        public void FunctionUnsupportedOnBrowser() { }

        [UnsupportedOSPlatform(""windows11.0"")]
        public void FunctionUnsupportedOnWindows11() { }

        [UnsupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""browser"")]
        public void FunctionUnsupportedOnWindows11AndBrowser() { }
    }

    [UnsupportedOSPlatform(""windows""), UnsupportedOSPlatform(""browser"")]
    public class TypeUnsupportedOnWindowsAndBrowser
    {
        [UnsupportedOSPlatform(""windows11.0"")]
        public void FunctionUnsupportedOnWindows11() { }
    }

    [UnsupportedOSPlatform(""windows10.0""), UnsupportedOSPlatform(""browser"")]
    public class TypeUnsupportedOnWindows10AndBrowser
    {
        [UnsupportedOSPlatform(""windows11.0"")]
        public void FunctionUnsupportedOnWindows11() { }
    }

    [UnsupportedOSPlatform(""windows""), SupportedOSPlatform(""windows11.0"")]
    public class TypeUnsupportedOnWindowsSupportedOnWindows11
    {
        [UnsupportedOSPlatform(""windows12.0"")]
        public void FunctionUnsupportedOnWindows12() { }

        [UnsupportedOSPlatform(""windows12.0""), SupportedOSPlatform(""windows13.0"")]
        public void FunctionUnsupportedOnWindows12SupportedOnWindows13() { }
    }

    [UnsupportedOSPlatform(""windows""), SupportedOSPlatform(""windows11.0""), UnsupportedOSPlatform(""windows12.0"")]
    public class TypeUnsupportedOnWindowsSupportedOnWindows11UnsupportedOnWindows12
    {
        [SupportedOSPlatform(""windows13.0"")]
        public void FunctionSupportedOnWindows13() { }
    }
    [SupportedOSPlatform(""windows"")]
    public class TypeSupportedOnWindows {
        [SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnBrowser() { }

        [SupportedOSPlatform(""windows11.0"")] // more restrictive should be OK
        public void FunctionSupportedOnWindows11() { }

        [SupportedOSPlatform(""windows11.0""), SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnWindows11AndBrowser() { }
    }
    [SupportedOSPlatform(""browser"")]
    public class TypeSupportedOnBrowser
    {
        [SupportedOSPlatform(""windows"")]
        public void FunctionSupportedOnWindows() { }

        [SupportedOSPlatform(""windows11.0"")]
        public void FunctionSupportedOnWindows11() { }
    }

    [SupportedOSPlatform(""windows10.0"")]
    public class TypeSupportedOnWindows10
    {
        [SupportedOSPlatform(""windows"")] // less restrictive should be OK
        public void FunctionSupportedOnWindows() { }

        [SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnBrowser() { }

        [SupportedOSPlatform(""windows11.0"")] // more restrictive should be OK
        public void FunctionSupportedOnWindows11() { }

        [SupportedOSPlatform(""windows11.0""), SupportedOSPlatform(""browser"")]
        public void FunctionSupportedOnWindows11AndBrowser() { }
    }


    [SupportedOSPlatform(""windows""), SupportedOSPlatform(""browser"")]
    public class TypeSupportedOnWindowsAndBrowser
    {
        [SupportedOSPlatform(""windows11.0"")] // more restrictive should be OK
        public void FunctionSupportedOnWindows11() { }
    }

    [SupportedOSPlatform(""windows10.0""), SupportedOSPlatform(""browser"")]
    public class TypeSupportedOnWindows10AndBrowser
    {
        [SupportedOSPlatform(""windows11.0"")] // more restrictive should be OK
        public void TypeSupportedOnWindows10AndBrowser_FunctionSupportedOnWindows11() { }
    }
}";
            sources.Add((nameof(targetTypesForTest), targetTypesForTest));

            var (compilation, options) = CSharpCompilationHelper.CreateWithOptionsAsync(sources.ToArray(), "build_property.TargetFramework = net6").GetAwaiter().GetResult();
            BaselineCompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyAnalyzer()), options);
            CompilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new PlatformCompatibilityAnalyzer()), options);
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
        private static CompilationWithAnalyzers CompilationWithAnalyzers;

        [Benchmark]
        public void CA1416_DiagnosticsProduced()
        {
            _ = CompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void CA1416_Baseline()
        {
            _ = BaselineCompilationWithAnalyzers.GetAllDiagnosticsAsync().GetAwaiter().GetResult();
        }
    }
}
