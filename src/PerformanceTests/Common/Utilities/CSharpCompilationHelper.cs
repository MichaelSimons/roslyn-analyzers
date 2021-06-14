// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace PerformanceTests.Utilities
{
    public static class CSharpCompilationHelper
    {
        public static async Task<Compilation?> CreateAsync((string, string)[] sourceFiles)
        {
            var project = await CreateProjectAsync(sourceFiles, null);
            return await project.GetCompilationAsync().ConfigureAwait(false);
        }

        public static async Task<(Compilation?, AnalyzerOptions)> CreateWithOptionsAsync((string, string)[] sourceFiles, string editorconfigText)
        {
            var project = await CreateProjectAsync(sourceFiles, editorconfigText);
            return (await project.GetCompilationAsync().ConfigureAwait(false), project.AnalyzerOptions);
        }

        private static async Task<Project> CreateProjectAsync((string, string)[] sourceFiles, string? editorconfigText = null)
        {
            editorconfigText ??= string.Empty;
            var projectState = ProjectState.Create("TestProject", LanguageNames.CSharp, "/0/Test", "cs");
            foreach (var (filename, content) in sourceFiles)
            {
                projectState.Sources.Add(("/0/Test" + filename + ".cs", content));
            }

            projectState.AnalyzerConfigFiles.Add(("/.editorconfig", $@"root = true
[*]
{editorconfigText}
"));

            var evaluatedProj = EvaluatedProjectState.Create(projectState, ReferenceAssemblies.Default);
            return await CreateProjectAsync(evaluatedProj);
        }

        private static async Task<Project> CreateProjectAsync(EvaluatedProjectState primaryProject)
        {
            var projectId = ProjectId.CreateNewId(debugName: primaryProject.Name);
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
