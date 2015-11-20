﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace NonVirtualEventAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonVirtualEventAnalyzerCodeFixProvider)), Shared]
    public class NonVirtualEventAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string removeVirtualKeywordTitle = "Remove virtual keyword";
        private const string implementVirtualRaiseEvent = "Implement Virtual Method to Raise Event";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NonVirtualEventAnalyzerAnalyzer.FieldEventDiagnosticId, NonVirtualEventAnalyzerAnalyzer.PropertyEventDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            if (diagnostic.Id == NonVirtualEventAnalyzerAnalyzer.FieldEventDiagnosticId)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<EventFieldDeclarationSyntax>().First();

                // We'll register two actions here.
                // One will simply remove the virtual keyword.
                // The second will remove the virtual keyword,
                // and add a virtual method to raise the event.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: removeVirtualKeywordTitle,
                        createChangedDocument: c => RemoveVirtualEventFieldAsync(context.Document, declaration, c),
                        equivalenceKey: removeVirtualKeywordTitle),
                    diagnostic);
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: implementVirtualRaiseEvent,
                        createChangedDocument: c => ImplementVirtualRaiseEventFieldAsync(context.Document, declaration, c),
                        equivalenceKey: implementVirtualRaiseEvent),
                    diagnostic);
            }
            else if (diagnostic.Id == NonVirtualEventAnalyzerAnalyzer.PropertyEventDiagnosticId)
            {
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                    .OfType<EventDeclarationSyntax>().First();

                // One will simply remove the virtual keyword.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: removeVirtualKeywordTitle,
                        createChangedDocument: c => RemoveVirtualEventPropertyAsync(context.Document, declaration, c),
                        equivalenceKey: removeVirtualKeywordTitle),
                    diagnostic);
            }
        }

        private Task<Document> RemoveVirtualEventPropertyAsync(Document document, EventDeclarationSyntax declaration, CancellationToken c)
        {
            var modifiers = declaration.Modifiers;
            var virtualToken = modifiers.Single(m => m.Kind() == SyntaxKind.VirtualKeyword);
            return RemoveVirtualTokenAsync(document, virtualToken, c);
        }

        private Task<Document> RemoveVirtualEventFieldAsync(Document document, EventFieldDeclarationSyntax declaration, CancellationToken c)
        {
            var modifiers = declaration.Modifiers;
            var virtualToken = modifiers.Single(m => m.Kind() == SyntaxKind.VirtualKeyword);
            return RemoveVirtualTokenAsync(document, virtualToken, c);
        }

        private Task<Document> ImplementVirtualRaiseEventFieldAsync(Document document, EventFieldDeclarationSyntax declaration, CancellationToken c)
        {
            throw new NotImplementedException();
        }

        private static async Task<Document> RemoveVirtualTokenAsync(Document document, SyntaxToken virtualToken, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c);
            var newRoot = root.ReplaceToken(virtualToken, SyntaxFactory.Token(SyntaxKind.None));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}