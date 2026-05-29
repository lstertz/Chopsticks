using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;

namespace Chopsticks.Analyzers;

/// <summary>
/// Code fix provider for CHOP001 - suggests using TryHandleFireAndForget or consuming the result.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnconsumedPromiseCodeFixProvider))]
[Shared]
public sealed class UnconsumedPromiseCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(UnconsumedPromiseAnalyzer.DiagnosticId);
    
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;
    
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        
        if (root == null)
            return;
        
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        
        var invocation = root.FindNode(diagnosticSpan)
            .FirstAncestorOrSelf<InvocationExpressionSyntax>();
        
        if (invocation == null)
            return;
        
        // Get the method name to determine the fix
        var methodName = GetMethodName(invocation);
        if (methodName == null)
            return;
        
        // Fix 1: Replace with TryHandleFireAndForget
        if (methodName is "TryHandle" or "TryHandleAsync")
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use TryHandleFireAndForget() (zero allocation)",
                    createChangedDocument: c => ReplaceWithFireAndForgetAsync(context.Document, invocation, c),
                    equivalenceKey: "UseFireAndForget"),
                diagnostic);
        }
        
        // Fix 2: Replace with HandleSync (if TryHandle)
        if (methodName == "TryHandle")
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use HandleSync() (sync, returns result)",
                    createChangedDocument: c => ReplaceWithHandleSyncAsync(context.Document, invocation, c),
                    equivalenceKey: "UseHandleSync"),
                diagnostic);
        }
        
        // Fix 3: Await the result
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Await the result",
                createChangedDocument: c => AddAwaitAsync(context.Document, invocation, c),
                equivalenceKey: "AwaitResult"),
            diagnostic);
        
        // Fix 4: Discard with _
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Explicitly discard with _ = ...",
                createChangedDocument: c => AddDiscardAsync(context.Document, invocation, c),
                equivalenceKey: "DiscardResult"),
            diagnostic);
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
    
    private static async Task<Document> ReplaceWithFireAndForgetAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
            .ConfigureAwait(false);
        
        // Replace method name
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var newName = SyntaxFactory.IdentifierName("TryHandleFireAndForget");
            var newMemberAccess = memberAccess.WithName(newName);
            var newInvocation = invocation.WithExpression(newMemberAccess);
            
            editor.ReplaceNode(invocation, newInvocation);
        }
        
        return editor.GetChangedDocument();
    }
    
    private static async Task<Document> ReplaceWithHandleSyncAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
            .ConfigureAwait(false);
        
        // Replace method name
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var newName = SyntaxFactory.IdentifierName("HandleSync");
            var newMemberAccess = memberAccess.WithName(newName);
            var newInvocation = invocation.WithExpression(newMemberAccess);
            
            // Wrap with discard since HandleSync returns HandlingResult
            var discardAssignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_"),
                newInvocation);
            
            editor.ReplaceNode(invocation, discardAssignment);
        }
        
        return editor.GetChangedDocument();
    }
    
    private static async Task<Document> AddAwaitAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
            .ConfigureAwait(false);
        
        // Wrap with await and discard
        var awaitExpression = SyntaxFactory.AwaitExpression(invocation);
        var discardAssignment = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName("_"),
            awaitExpression);
        
        editor.ReplaceNode(invocation, discardAssignment);
        
        return editor.GetChangedDocument();
    }
    
    private static async Task<Document> AddDiscardAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
            .ConfigureAwait(false);
        
        // Wrap with _ = 
        var discardAssignment = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName("_"),
            invocation);
        
        editor.ReplaceNode(invocation, discardAssignment);
        
        return editor.GetChangedDocument();
    }
}
