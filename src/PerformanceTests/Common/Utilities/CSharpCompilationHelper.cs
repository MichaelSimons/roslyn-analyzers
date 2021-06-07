// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace PerformanceTests.Utilities
{
    public static class CSharpCompilationHelper
    {
        public static async Task<Compilation> Create((string, string)[] sourceFiles)
        {
            var solutionState = ProjectState.Create("TestProject", LanguageNames.CSharp, "/0/Test", "cs");
            foreach (var sourceFile in sourceFiles)
            {
                solutionState.Sources.Add(sourceFile);
            }

            var evaluatedProj = EvaluatedProjectState.Create(solutionState, ReferenceAssemblies.Default);
            var project = await CreateProjectAsync(evaluatedProj);
#pragma warning disable CS8603 // Possible null reference return.
            return await project.GetCompilationAsync().ConfigureAwait(false);
#pragma warning restore CS8603 // Possible null reference return.
        }

        private static async Task<Project> CreateProjectAsync(EvaluatedProjectState primaryProject)
        {
            var projectIdMap = new Dictionary<string, ProjectId>();

            var projectId = ProjectId.CreateNewId(debugName: primaryProject.Name);
            projectIdMap.Add(primaryProject.Name, projectId);
            var solution = await CreateSolutionAsync(projectId, primaryProject);

            foreach (var (newFileName, source) in primaryProject.Sources)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, source, filePath: newFileName);
            }

            foreach (var (newFileName, source) in primaryProject.AdditionalFiles)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddAdditionalDocument(documentId, newFileName, source, filePath: newFileName);
            }

            foreach (var (newFileName, source) in primaryProject.AnalyzerConfigFiles)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddAnalyzerConfigDocument(documentId, newFileName, source, filePath: newFileName);
            }

            return solution.GetProject(projectId)!;
        }

        private static async Task<Solution> CreateSolutionAsync(ProjectId projectId, EvaluatedProjectState projectState)
        {
            var referenceAssemblies = projectState.ReferenceAssemblies ?? ReferenceAssemblies.Default;

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOutputKind(projectState.OutputKind);

            compilationOptions = compilationOptions
                .WithAssemblyIdentityComparer(referenceAssemblies.AssemblyIdentityComparer);

            var parseOptions = new CSharpParseOptions(LanguageVersion.Default)
                .WithDocumentationMode(projectState.DocumentationMode);

            var exportProviderFactory = new Lazy<IExportProviderFactory>(
                () =>
                {
                    var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
                    var parts = Task.Run(() => discovery.CreatePartsAsync(MefHostServices.DefaultAssemblies)).GetAwaiter().GetResult();
                    var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

                    var configuration = CompositionConfiguration.Create(catalog);
                    var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
                    return runtimeComposition.CreateExportProviderFactory();
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
            var exportProvider = exportProviderFactory.Value.CreateExportProvider();
            var host = MefHostServices.Create(exportProvider.AsCompositionContext());
            var workspace = new AdhocWorkspace(host);

            var solution = workspace
                .CurrentSolution
                .AddProject(projectId, projectState.Name, projectState.Name, projectState.Language)
                .WithProjectCompilationOptions(projectId, compilationOptions)
                .WithProjectParseOptions(projectId, parseOptions);

            var metadataReferences = await referenceAssemblies.ResolveAsync(projectState.Language);
            solution = solution.AddMetadataReferences(projectId, metadataReferences);

            return solution;
        }
    }
}
