﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace PinionCore.Remote.Tools.Protocol.Sources.Tests
{
    public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : ISourceGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<TSourceGenerator, NUnitVerifier>
        {
            public Test()
            {
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                CompilationOptions compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
            {
                string[] args = { "/warnaserror:nullable" };
                CSharpCommandLineArguments commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
                ImmutableDictionary<string, ReportDiagnostic> nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

                return nullableWarnings;
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }
        }
    }
}
