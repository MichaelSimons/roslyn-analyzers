// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace PerfDiff
{
    public static partial class PerfDiff
    {
        public static async Task<int> CompareAsync(
            string baselineFolder, string resultsFolder, bool failOnRegression, ILogger logger, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var (success, shouldCheckETL) = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(baselineFolder, resultsFolder, logger).ConfigureAwait(false);
            if (!success)
            {
                return 1;
            }

            if (shouldCheckETL)
            {
                // get file paths
                if (!TryGetETLPaths(baselineFolder, out var baselineEtlPath, out var baselineSymbolLookupPath))
                {
                    return 1;
                }

                if (!TryGetETLPaths(resultsFolder, out var resultsEtlPath, out var resultsSymbolLookupPath))
                {
                    return 1;
                }

                // Compare ETL
                if (!EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, resultsSymbolLookupPath, baselineSymbolLookupPath, out var regression))
                {
                    return 1;
                }

                if (regression && failOnRegression)
                {
                    return 1;
                }
            }

            return 0;
        }

        private const string ETLFileExtension = "etl.zip";

        private static bool TryGetETLPaths(string path, [NotNullWhen(true)] out string? etlPath, [NotNullWhen(true)] out string? symbolPath)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, $"*{ETLFileExtension}", SearchOption.AllDirectories);
                symbolPath = files.Single();
                etlPath = files.Single();
                return true;
            }
            else if (File.Exists(path) || !path.EndsWith(ETLFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                etlPath = path;
                symbolPath = path;
                return true;
            }

            symbolPath = null;
            etlPath = null;
            return false;
        }
    }
}
