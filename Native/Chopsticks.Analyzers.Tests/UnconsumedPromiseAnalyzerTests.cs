using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System.Collections.Immutable;

namespace Chopsticks.Analyzers.Tests;

[TestFixture]
public class UnconsumedPromiseAnalyzerTests
{
    [Test]
    public void Analyzer_ReportsCorrectDiagnosticId()
    {
        var analyzer = new UnconsumedPromiseAnalyzer();
        Assert.That(analyzer.SupportedDiagnostics, Has.Length.EqualTo(1));
        Assert.That(analyzer.SupportedDiagnostics[0].Id, Is.EqualTo("CHOP001"));
    }
    
    [Test]
    public void Analyzer_HasCorrectSeverity()
    {
        var analyzer = new UnconsumedPromiseAnalyzer();
        Assert.That(analyzer.SupportedDiagnostics[0].DefaultSeverity, Is.EqualTo(DiagnosticSeverity.Warning));
    }
    
    [Test]
    public void Analyzer_IsEnabledByDefault()
    {
        var analyzer = new UnconsumedPromiseAnalyzer();
        Assert.That(analyzer.SupportedDiagnostics[0].IsEnabledByDefault, Is.True);
    }
    
    [Test]
    public async Task UnconsumedTryHandle_ReportsDiagnostic()
    {
        var code = @"
namespace Chopsticks.Messages
{
    public struct HandlingResultPromise { }
}

namespace Chopsticks.Messages.Handlers.Multicast
{
    public class MulticastMessageHandler<T>
    {
        public Chopsticks.Messages.HandlingResultPromise TryHandle(T message) => default;
    }
}

class Test
{
    void Method()
    {
        var handler = new Chopsticks.Messages.Handlers.Multicast.MulticastMessageHandler<int>();
        handler.TryHandle(42);
    }
}";
        
        var diagnostics = await GetDiagnosticsAsync(code);
        
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Id, Is.EqualTo("CHOP001"));
    }
    
    [Test]
    public async Task AwaitedTryHandle_NoDiagnostic()
    {
        var code = @"
using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public struct HandlingResultAwaitable 
    {
        public HandlingResultAwaiter GetAwaiter() => default;
    }
    public struct HandlingResultAwaiter
    {
        public bool IsCompleted => true;
        public int GetResult() => 0;
        public void OnCompleted(System.Action continuation) { }
    }
}

namespace Chopsticks.Messages.Handlers.Multicast
{
    public class MulticastMessageHandler<T>
    {
        public Chopsticks.Messages.HandlingResultAwaitable TryHandleAsync(T message) => default;
    }
}

class Test
{
    async Task Method()
    {
        var handler = new Chopsticks.Messages.Handlers.Multicast.MulticastMessageHandler<int>();
        await handler.TryHandleAsync(42);
    }
}";
        
        var diagnostics = await GetDiagnosticsAsync(code);
        
        Assert.That(diagnostics.Where(d => d.Id == "CHOP001"), Is.Empty);
    }
    
    [Test]
    public async Task AssignedTryHandle_NoDiagnostic()
    {
        var code = @"
namespace Chopsticks.Messages
{
    public struct HandlingResultPromise { }
}

namespace Chopsticks.Messages.Handlers.Multicast
{
    public class MulticastMessageHandler<T>
    {
        public Chopsticks.Messages.HandlingResultPromise TryHandle(T message) => default;
    }
}

class Test
{
    void Method()
    {
        var handler = new Chopsticks.Messages.Handlers.Multicast.MulticastMessageHandler<int>();
        var result = handler.TryHandle(42);
    }
}";
        
        var diagnostics = await GetDiagnosticsAsync(code);
        
        Assert.That(diagnostics.Where(d => d.Id == "CHOP001"), Is.Empty);
    }
    
    [Test]
    public async Task DiscardedTryHandle_NoDiagnostic()
    {
        var code = @"
namespace Chopsticks.Messages
{
    public struct HandlingResultPromise { }
}

namespace Chopsticks.Messages.Handlers.Multicast
{
    public class MulticastMessageHandler<T>
    {
        public Chopsticks.Messages.HandlingResultPromise TryHandle(T message) => default;
    }
}

class Test
{
    void Method()
    {
        var handler = new Chopsticks.Messages.Handlers.Multicast.MulticastMessageHandler<int>();
        _ = handler.TryHandle(42);
    }
}";
        
        var diagnostics = await GetDiagnosticsAsync(code);
        
        Assert.That(diagnostics.Where(d => d.Id == "CHOP001"), Is.Empty);
    }
    
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        };
        
        // Add runtime references
        var runtimePath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var netstandardRef = System.IO.Path.Combine(runtimePath, "netstandard.dll");
        if (System.IO.File.Exists(netstandardRef))
            references = references.Append(MetadataReference.CreateFromFile(netstandardRef)).ToArray();
        
        var systemRuntimeRef = System.IO.Path.Combine(runtimePath, "System.Runtime.dll");
        if (System.IO.File.Exists(systemRuntimeRef))
            references = references.Append(MetadataReference.CreateFromFile(systemRuntimeRef)).ToArray();
        
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var analyzer = new UnconsumedPromiseAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        
        return await compilationWithAnalyzers.GetAllDiagnosticsAsync();
    }
}
