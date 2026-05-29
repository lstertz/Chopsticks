using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Chopsticks.Analyzers;

/// <summary>
/// Analyzer that detects unconsumed HandlingResultPromise returns from multicast handlers.
/// These allocate memory and prevent proper pool return when not consumed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnconsumedPromiseAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CHOP001";
    
    private const string Category = "Performance";
    
    private static readonly LocalizableString Title = 
        "Unconsumed HandlingResultPromise causes allocation";
    
    private static readonly LocalizableString MessageFormat = 
        "The return value of '{0}' is not consumed. Use TryHandleFireAndForget() for zero-allocation fire-and-forget, or consume the result with await/GetResult().";
    
    private static readonly LocalizableString Description = 
        "HandlingResultPromise allocates memory and prevents pool return when not consumed. " +
        "Use TryHandleFireAndForget() for fire-and-forget scenarios, or explicitly consume the result.";
    
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/chopsticks-framework/docs/performance/CHOP001");
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
        ImmutableArray.Create(Rule);
    
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }
    
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        // Get method name
        string? methodName = GetMethodName(invocation);
        if (methodName == null)
            return;
        
        // Check if it's a TryHandle method call
        if (!IsTryHandleMethod(methodName))
            return;
        
        // Check if the return type is HandlingResultPromise or HandlingResultAwaitable
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;
        
        var returnTypeName = methodSymbol.ReturnType.Name;
        if (returnTypeName != "HandlingResultPromise" && returnTypeName != "HandlingResultAwaitable")
            return;
        
        // Check if the result is being consumed
        if (IsResultConsumed(invocation, context.SemanticModel))
            return;
        
        // Report diagnostic
        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            methodName);
        
        context.ReportDiagnostic(diagnostic);
    }
    
    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }
    
    private static bool IsTryHandleMethod(string methodName)
    {
        return methodName is "TryHandle" or "TryHandleAsync" or "Handle" or "HandleAsync";
    }
    
    private static bool IsResultConsumed(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var parent = invocation.Parent;
        
        while (parent != null)
        {
            switch (parent)
            {
                // await expression - result is consumed
                case AwaitExpressionSyntax:
                    return true;
                
                // Assignment - result is stored for later use
                case AssignmentExpressionSyntax:
                case EqualsValueClauseSyntax:
                    return true;
                
                // Variable declaration - result is stored
                case VariableDeclaratorSyntax:
                    return true;
                
                // Return statement - result is passed up
                case ReturnStatementSyntax:
                case ArrowExpressionClauseSyntax:
                    return true;
                
                // Argument to another method - result is used
                case ArgumentSyntax:
                    return true;
                
                // Member access (e.g., .GetAwaiter(), .ToPromise(), .GetResult())
                case MemberAccessExpressionSyntax memberAccess:
                    // Check if accessing result-consuming members
                    var memberName = memberAccess.Name.Identifier.Text;
                    if (memberName is "GetAwaiter" or "GetResult" or "ToPromise" or "Result" or "Status")
                        return true;
                    break;
                
                // Conditional expression - result is used
                case ConditionalExpressionSyntax:
                    return true;
                
                // Lambda/delegate - result is captured (LambdaExpressionSyntax covers all lambda types)
                case LambdaExpressionSyntax:
                    return true;
                
                // Expression statement - this is the fire-and-forget case we're looking for
                case ExpressionStatementSyntax:
                    return false;
                
                // Block or other container - keep searching up
                case BlockSyntax:
                case GlobalStatementSyntax:
                    return false;
            }
            
            parent = parent.Parent;
        }
        
        return false;
    }
}
