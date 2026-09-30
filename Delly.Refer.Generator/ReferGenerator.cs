using System.Collections.Generic;
using System.Collections.Immutable;
using Delly.Refer.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// <c>IRefer</c> 增量源生成器
    /// </summary>
    /// <remarks>
    /// <para>
    /// 扫描标注 <c>[UseRefer]</c> 的模型类型，为每个类型生成一个实现 <see cref="Delly.Refer.IRefer" /> 的
    /// <c>{类型名}Refer</c> 类型，并将其全部元数据固化为常量与静态只读集合——生成产物不含任何运行时反射。
    /// </para>
    /// <para>
    /// 成员类型中出现的、由源码声明的非泛型类型会被「有界展开」递归纳入生成范围，
    /// 展开在 <see cref="SymbolEqualityComparer" /> 访问集内进行，天然对循环引用终止。
    /// </para>
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed class ReferGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// 公共支撑类型所在文件的 hint 名
        /// </summary>
        private const string InfrastructureHintName = "Delly.Refer.Generated.Infrastructure.g.cs";

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol> markedTypes = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ReferConstants.UseReferAttributeMetadataName,
                    static (node, _) => node is BaseTypeDeclarationSyntax,
                    static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

            IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> collected = markedTypes.Collect();

            context.RegisterSourceOutput(collected, static (spc, symbols) => Execute(spc, symbols));
        }

        /// <summary>
        /// 依据标记类型集合生成全部源码
        /// </summary>
        /// <param name="spc">源生成上下文</param>
        /// <param name="symbols">标注 <c>[UseRefer]</c> 的类型集合</param>
        private static void Execute(
            SourceProductionContext spc,
            ImmutableArray<INamedTypeSymbol> symbols)
        {
            if (symbols.IsDefaultOrEmpty)
            {
                return;
            }

            var diagnostics = new List<Diagnostic>();
            List<ReferTypeModel> models = BuildModels(symbols, diagnostics);

            foreach (Diagnostic diagnostic in diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (models.Count == 0)
            {
                return;
            }

            // 支撑类型先于引用类型输出，保证生成顺序与阅读顺序一致
            spc.AddSource(
                InfrastructureHintName,
                SourceText.From(ReferEmitter.EmitInfrastructure(), System.Text.Encoding.UTF8));

            foreach (ReferTypeModel model in models)
            {
                spc.AddSource(
                    GetHintName(model),
                    SourceText.From(ReferEmitter.EmitType(model), System.Text.Encoding.UTF8));
            }
        }

        /// <summary>
        /// 广度优先展开全部需生成的引用模型
        /// </summary>
        /// <param name="symbols">标注 <c>[UseRefer]</c> 的类型集合</param>
        /// <param name="diagnostics">诊断收集器</param>
        /// <returns>引用模型集合</returns>
        private static List<ReferTypeModel> BuildModels(
            ImmutableArray<INamedTypeSymbol> symbols,
            List<Diagnostic> diagnostics)
        {
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var queue = new Queue<INamedTypeSymbol>();

            foreach (INamedTypeSymbol symbol in symbols)
            {
                if (visited.Add(symbol))
                {
                    queue.Enqueue(symbol);
                }
            }

            var models = new List<ReferTypeModel>();

            while (queue.Count > 0)
            {
                INamedTypeSymbol symbol = queue.Dequeue();
                var discovered = new List<INamedTypeSymbol>();

                ReferTypeModel? model = ModelFactory.Create(symbol, diagnostics, discovered);
                if (model != null)
                {
                    models.Add(model);
                }

                foreach (INamedTypeSymbol reference in discovered)
                {
                    if (visited.Add(reference))
                    {
                        queue.Enqueue(reference);
                    }
                }
            }

            return models;
        }

        /// <summary>
        /// 生成产物的唯一 hint 名
        /// </summary>
        /// <param name="model">引用模型</param>
        /// <returns>hint 名</returns>
        private static string GetHintName(ReferTypeModel model)
        {
            string prefix = model.Namespace.Length == 0 ? "Global" : model.Namespace;
            return prefix + "." + model.ReferClassName + ReferConstants.GeneratedFileExtension;
        }
    }
}
