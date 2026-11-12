using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Sigurn.Serialize.Generator;
using Xunit;

namespace Sigurn.Serialize.Tests;

// SGS001: a type implementing ISerializable that the generated code cannot make is reported, and only in a project built for Native AOT.
public class GeneratorDiagnosticsTests
{
    private const string Source = @"
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sigurn;
using Sigurn.Serialize;

namespace MyCode
{
    public abstract class Base : ISerializable
    {
        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) => default;
        public ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) => default;
    }

    public sealed class Constructible : Base { }

    public sealed class NeedsArgument : Base
    {
        public NeedsArgument(int value) { }
    }

    public partial class SplitNeedsArgument : Base
    {
        public SplitNeedsArgument(int value) { }
    }

    public partial class SplitNeedsArgument { }

    public static class Outer
    {
        private sealed class Hidden : Base { }
    }

    public struct Value : ISerializable
    {
        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) => default;
        public ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) => default;
    }
}
";

    private static GeneratorDriverRunResult Run(bool aot, string source = Source)
    {
        var assemblies = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var compilation = CSharpCompilation.Create("compilation",
            [CSharpSyntaxTree.ParseText(source, path: "Source.cs")],
            new[] { "System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll" }
                .Select(f => MetadataReference.CreateFromFile(Path.Combine(assemblies, f)))
                .Append(MetadataReference.CreateFromFile(typeof(GenerateSerializerAttribute).Assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var options = new Options(aot ? new() { ["build_property.EnableAotAnalyzer"] = "true" } : new());
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SerializationGenerator().AsSourceGenerator()], optionsProvider: options);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    [Fact]
    public void InAnAotProject_ATypeTheGeneratedCodeCannotMake_IsReportedOnce()
    {
        var result = Run(aot: true);

        var reported = result.Diagnostics.Where(d => d.Id == "SGS001").Select(d => d.GetMessage()).OrderBy(m => m).ToList();
        Assert.Equal(3, reported.Count);
        Assert.Contains("'MyCode.NeedsArgument' implements ISerializable but has no public or internal parameterless constructor", reported[0]);
        Assert.Contains("'MyCode.Outer.Hidden' implements ISerializable but is not accessible to generated code", reported[1]);
        Assert.Contains("'MyCode.SplitNeedsArgument' implements ISerializable but has no public or internal parameterless constructor", reported[2]);
        Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    [Fact]
    public void InAProjectWithoutAot_NothingIsReported()
    {
        Assert.Empty(Run(aot: false).Diagnostics);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnlyTheTypesTheGeneratedCodeCanMake_AreRegistered(bool aot)
    {
        var registration = Run(aot).Results.Single().GeneratedSources.Single(s => s.HintName == "SerializableTypesRegistration.g.cs").SourceText.ToString();

        Assert.Contains("RegisterSerializable<global::MyCode.Constructible>()", registration);
        Assert.Contains("RegisterSerializable<global::MyCode.Value>()", registration);
        Assert.DoesNotContain("NeedsArgument", registration);
        Assert.DoesNotContain("Hidden", registration);
        Assert.DoesNotContain("Base>", registration);
    }

    private const string CallSites = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sigurn;
using Sigurn.Serialize;

namespace MyCode
{
    public sealed class Shipment : ISerializable
    {
        public Queue<Guid> Parcels { get; set; } = new();

        public ValueTask ToStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Serializer.ToStreamAsync(stream, Parcels, context, cancellationToken);

        public async ValueTask FromStreamAsync(Stream stream, SerializationContext context, CancellationToken cancellationToken) =>
            Parcels = await Serializer.FromStreamAsync<Queue<Guid>>(stream, context, cancellationToken) ?? new();
    }

    public static class Helpers
    {
        public static ValueTask WriteTyped(Stream stream, object value) =>
            Serializer.ToStreamAsync(stream, typeof(Dictionary<string, int[]>), value, SerializationContext.Default, CancellationToken.None);

        public static ValueTask WriteAny(Stream stream, Type type, object value) =>
            Serializer.ToStreamAsync(stream, type, value, SerializationContext.Default, CancellationToken.None);

        public static ValueTask WriteGeneric<T>(Stream stream, List<T> value) =>
            Serializer.ToStreamAsync(stream, value, SerializationContext.Default, CancellationToken.None);

        public static ValueTask WriteNumber(Stream stream) =>
            Serializer.ToStreamAsync(stream, 42, SerializationContext.Default, CancellationToken.None);
    }
}
";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheTypesHandedToTheSerializer_AreRegistered(bool aot)
    {
        var sources = Run(aot, CallSites).Results.Single().GeneratedSources;
        var registration = sources.Single(s => s.HintName == "SerializerCallSiteRegistrations.g.cs").SourceText.ToString();

        Assert.Contains("Serializer.RegisterQueue<global::System.Guid>();", registration);
        Assert.Contains("Serializer.RegisterArray<int>();", registration);
        Assert.Contains("Serializer.RegisterDictionary<string, int[]>();", registration);
        Assert.Equal(1, CountOf(registration, "RegisterQueue"));
        Assert.DoesNotContain("<T>", registration);
    }

    [Fact]
    public void ATypeNotKnownAtBuildTime_IsNotReported()
    {
        Assert.Empty(Run(aot: true, CallSites).Diagnostics);
    }

    private static int CountOf(string text, string part) => (text.Length - text.Replace(part, string.Empty).Length) / part.Length;

    private sealed class Options(Dictionary<string, string> global) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(global);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Values(new());

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Values(new());
    }

    private sealed class Values(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }
}
