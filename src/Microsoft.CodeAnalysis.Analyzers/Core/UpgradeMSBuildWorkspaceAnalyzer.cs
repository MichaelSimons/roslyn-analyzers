﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1023: <inheritdoc cref="UpgradeMSBuildWorkspaceTitle"/>
    /// </summary>
    public abstract class UpgradeMSBuildWorkspaceAnalyzer : DiagnosticAnalyzer
    {
        private const string WorkspacesDesktop = "Microsoft.CodeAnalysis.Workspaces.Desktop";
        private const string WorkspacesMSBuild = "Microsoft.CodeAnalysis.Workspaces.MSBuild";
        private const string MSBuildWorkspaceFullName = "Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace";
        protected const string MSBuildWorkspace = "MSBuildWorkspace";

        public static readonly DiagnosticDescriptor UpgradeMSBuildWorkspaceDiagnosticRule = new(
            DiagnosticIds.UpgradeMSBuildWorkspaceRuleId,
            CreateLocalizableResourceString(nameof(UpgradeMSBuildWorkspaceTitle)),
            CreateLocalizableResourceString(nameof(UpgradeMSBuildWorkspaceMessage)),
            DiagnosticCategory.Library,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(UpgradeMSBuildWorkspaceDescription)),
            helpLinkUri: "https://go.microsoft.com/fwlink/?linkid=874285",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(UpgradeMSBuildWorkspaceDiagnosticRule);

        private readonly bool _performAssemblyChecks;

        protected UpgradeMSBuildWorkspaceAnalyzer(bool performAssemblyChecks)
        {
            _performAssemblyChecks = performAssemblyChecks;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            if (_performAssemblyChecks)
            {
                context.RegisterCompilationStartAction(AnalyzeAssemblyReferences);
            }
            else
            {
                RegisterIdentifierAnalysis(context);
            }
        }

        protected abstract void RegisterIdentifierAnalysis(CompilationStartAnalysisContext context);
        protected abstract void RegisterIdentifierAnalysis(AnalysisContext context);

        private void AnalyzeAssemblyReferences(CompilationStartAnalysisContext context)
        {
            // We have to be careful not to report the "upgrade MSBuildWorkspace" diagnostic in such
            // a way that it won't conflict with IDE code fixes, such as "Add Using".
            // To do that, we only report the diagnostic if the compilation meets the following conditions:
            //
            //     1. Has a reference Microsoft.CodeAnalysis.Workspaces.Desktop.
            //     2. Does not have a reference Microsoft.CodeAnalysis.Workspaces.MSBuild.
            //     3. Does not include the type Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.
            //
            // It's possible that this diagnostic might be reported when the "Add NuGet package" code fix
            // is offered for "Microsoft.CodeAnalysis.Workspaces.MSBuild", but that's OK. When the user
            // applies that code fix, this diagnostic should go away.

            var foundWorkspacesDesktop = false;

            foreach (var assemblyIdentity in context.Compilation.ReferencedAssemblyNames)
            {
                if (assemblyIdentity.Name == WorkspacesMSBuild)
                {
                    // If a reference to Workspaces.MSBuild exists, we're done.
                    return;
                }

                if (!foundWorkspacesDesktop && assemblyIdentity.Name == WorkspacesDesktop)
                {
                    foundWorkspacesDesktop = true;
                }
            }

            // If there isn't a reference to Workspaces.Desktop, we're done.
            if (!foundWorkspacesDesktop)
            {
                return;
            }

            // If this compilation contains the type, Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace, we're done.
            var msbuildWorkspace = context.Compilation.GetOrCreateTypeByMetadataName(MSBuildWorkspaceFullName);
            if (msbuildWorkspace != null)
            {
                return;
            }

            // OK, add a syntax node action to look for unbound MSBuildWorkspace symbols.
            RegisterIdentifierAnalysis(context);
        }
    }
}
