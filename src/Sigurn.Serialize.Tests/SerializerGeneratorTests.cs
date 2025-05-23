using System.Runtime.CompilerServices;
using DiffEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sigurn.Serialize.Generator;

namespace Sigurn.Serialize.Tests;

public static class VerifyInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyDiffPlex.Initialize();
        VerifySourceGenerators.Initialize();
        DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.Rider);
    }
}

public class SerializerGeneratorTests
{
    private static readonly string dotNetAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
    
    [Fact]
    public Task Run() => VerifyChecks.Run();


    [Fact]
    public async Task TestClassSerializer()
    {
        Compilation inputCompilation = CreateCompilation(
@"
using Sigurn.Serialize;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System;

namespace MyCode
{
    [GenerateSerializer(true)]
    public class TestClass
    {
        public string Prop1 { get; set; }

        public int Prop2 { get; internal set; }

        private int Prop3 { get; set; }

        public IList<Guid> Prop4 { get; init; }

        public bool Prop5 { get; }

        private DateTime _dt;
        public DateTime Prop6 { set => _dt = value; }

        public long Prop7 { get; private set; }

        [SerializeOrder(-1)]
        internal DateTime Prop8 { get; set; }

        [SerializeIgnore]
        public decimal Prop9 { get; set; }

        public void TestMethod()
        {
        }
    }
}
");
        var diag = inputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diag);

        SerializationGenerator generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(inputCompilation);
        await Verify(driver);
        //driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
    }

    [Fact]
    public async Task TestEmptyClassSerializer()
    {
        Compilation inputCompilation = CreateCompilation(
@"
using Sigurn.Serialize;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System;

namespace MyCode
{
    [GenerateSerializer]
    public class EmptyClass
    {
    }
}
");
        var diag = inputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diag);

        SerializationGenerator generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(inputCompilation);
        await Verify(driver);
    }

    [Fact]
    public async Task TestDoNotUseGloballySerializer()
    {
        Compilation inputCompilation = CreateCompilation(
@"
using Sigurn.Serialize;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System;

namespace MyCode
{
    [GenerateSerializer(false)]
    public class TestClass
    {
    }
}
");
        var diag = inputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diag);

        SerializationGenerator generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(inputCompilation);
        await Verify(driver);
    }

    [Fact]
    public async Task TestRecordSerializer()
    {
        Compilation inputCompilation = CreateCompilation(
@"
using Sigurn.Serialize;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System;

namespace MyCode
{
    [GenerateSerializer]
    public record TestRecord
    {
        [SerializeOrder(10)]
        public string Prop1 { get; init; }

        [SerializeOrder(20)]
        public int Prop2 { get; internal init; }

        [SerializeOrder(30)]
        private int Prop3 { get; init; }

        [SerializeOrder(40)]
        public IList<Guid> Prop4 { get; init; }

        public bool Prop5 { get; }

        private DateTime _dt;
        public DateTime Prop6 { init => _dt = value; }

        public long Prop7 { get; private init; }

        [SerializeOrder(50)]
        internal DateTime Prop8 { get; init; }

        [SerializeIgnore]
        public decimal Prop9 { get; init; }

        public void TestMethod()
        {
        }
    }
}
");
        var diag = inputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diag);

        SerializationGenerator generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(inputCompilation);
        await Verify(driver);
    }

    [Fact]
    public async Task TestStructSerializer()
    {
        Compilation inputCompilation = CreateCompilation(
@"
using Sigurn.Serialize;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System;

namespace MyCode
{
    [GenerateSerializer]
    public struct TestStruct
    {
        public string Prop1 { get; set; }

        public int Prop2 { get; internal set; }

        private int Prop3 { get; set; }

        public IList<Guid> Prop4 { get; init; }

        public bool Prop5 { get; }

        private DateTime _dt;
        public DateTime Prop6 { set => _dt = value; }

        public long Prop7 { get; private set; }

        [SerializeOrder(-1)]
        internal DateTime Prop8 { get; set; }

        [SerializeIgnore]
        public decimal Prop9 { get; set; }

        public void TestMethod()
        {
        }
    }
}
");
        var diag = inputCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(diag);

        SerializationGenerator generator = new SerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(inputCompilation);
        await Verify(driver);
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { 
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Private.Xml.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Xml.ReaderWriter.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(GenerateSerializerAttribute).Assembly.Location)
             },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

