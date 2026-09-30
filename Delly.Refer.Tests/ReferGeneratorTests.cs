using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Delly.Refer.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Delly.Refer.Tests
{
    /// <summary>
    /// 生成器的驱动式测试：以 <see cref="CSharpGeneratorDriver" /> 编译源码片段，
    /// 断言生成产物的形态、诊断告警与「零反射」契约。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="GeneratedReferTests" /> 互补——后者断言测试项目自身作为宿主时生成产物的运行时行为，
    /// 本类则覆盖生成器在受限成员类型、不可构造特性、循环引用等边界输入下的反应。
    /// </remarks>
    public class ReferGeneratorTests
    {
        private const string Preamble = "using System;\nusing Delly.Refer;\n\n";

        private const string InfrastructureHintName = "Delly.Refer.Generated.Infrastructure.g.cs";

        /// <summary>
        /// 编译期引用集合：核心库 + 运行时的可信平台程序集
        /// </summary>
        private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

        private static ImmutableArray<MetadataReference> CreateReferences()
        {
            ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string coreLibrary = typeof(IRefer).Assembly.Location;
            builder.Add(MetadataReference.CreateFromFile(coreLibrary));
            seen.Add(coreLibrary);

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string platformAssemblies)
            {
                foreach (string path in platformAssemblies.Split(Path.PathSeparator))
                {
                    if (path.Length > 0 && seen.Add(path))
                    {
                        builder.Add(MetadataReference.CreateFromFile(path));
                    }
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// 为标注 <see cref="UseReferAttribute" /> 的模型生成 Refer 类型
        /// </summary>
        [Fact]
        public void Generator_ProducesReferTypeForMarkedModel()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    [UseRefer]
    public sealed class Person
    {
        public Person() { }
        public Person(string name) { Name = name; }
        public string? Name { get; private set; }
        public int Age { get; }
        public bool IsAdult() { return Age >= 18; }
        public void Rename(string name) { Name = name; }
    }
}");

            AssertNoCompileErrors(run);
            Assert.Empty(run.GeneratorDiagnostics);

            Assert.True(run.Sources.ContainsKey("Sample.PersonRefer.g.cs"), string.Join(", ", run.Sources.Keys));
            Assert.True(run.Sources.ContainsKey(InfrastructureHintName), string.Join(", ", run.Sources.Keys));

            string person = run.Sources["Sample.PersonRefer.g.cs"];
            Assert.Contains("public sealed class PersonRefer : global::Delly.Refer.IRefer", person);
            Assert.Contains("public static readonly PersonRefer Instance = new PersonRefer();", person);
            Assert.Contains("case 1:", person);
        }

        /// <summary>
        /// 基础设施（<c>VoidRefer</c> / <c>MethodReferParameter</c>）无论标记多少类型都只发射一份
        /// </summary>
        [Fact]
        public void Generator_EmitsInfrastructureExactlyOnce()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    [UseRefer]
    public sealed class A { public A() { } public void Run() { } }

    [UseRefer]
    public sealed class B { public B() { } public void Run() { } }
}");

            AssertNoCompileErrors(run);
            Assert.Equal(1, run.Sources.Keys.Count(name => name == InfrastructureHintName));

            string infrastructure = run.Sources[InfrastructureHintName];
            Assert.Contains("public sealed class VoidRefer : global::Delly.Refer.IRefer", infrastructure);
            Assert.Contains("public sealed class MethodReferParameter : global::Delly.Refer.IMethodReferParameter", infrastructure);
        }

        /// <summary>
        /// 源码声明但未标注 <c>[UseRefer]</c> 的成员类型按「有界展开」递归生成，且循环引用可正常终止
        /// </summary>
        [Fact]
        public void Generator_RecursivelyExpandsSourceDeclaredMemberTypes()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    public sealed class Node
    {
        public Node(string id) { Id = id; }
        public string Id { get; }
        public Node? Parent { get; set; }
    }

    [UseRefer]
    public sealed class Tree
    {
        public Tree() { }
        public Node? Root { get; set; }
    }
}");

            AssertNoCompileErrors(run);
            Assert.Empty(run.GeneratorDiagnostics);

            Assert.True(run.Sources.ContainsKey("Sample.TreeRefer.g.cs"), string.Join(", ", run.Sources.Keys));
            Assert.True(run.Sources.ContainsKey("Sample.NodeRefer.g.cs"), string.Join(", ", run.Sources.Keys));

            string node = run.Sources["Sample.NodeRefer.g.cs"];
            Assert.Contains("global::Sample.NodeRefer.Instance", node);
        }

        /// <summary>
        /// 基础值类型与其一维数组复用 <c>BasicRefers</c> 既有单例，不重复生成
        /// </summary>
        [Fact]
        public void Generator_ReusesBasicRefersForSupportedTypes()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    [UseRefer]
    public sealed class Bag
    {
        public Bag() { }
        public string? Label { get; set; }
        public int Count { get; set; }
        public int[]? Values { get; set; }
    }
}");

            AssertNoCompileErrors(run);
            Assert.Empty(run.GeneratorDiagnostics);

            string bag = run.Sources["Sample.BagRefer.g.cs"];
            Assert.Contains("global::Delly.Refer.BasicRefers.StringRefer.Instance", bag);
            Assert.Contains("global::Delly.Refer.BasicRefers.Int32Refer.Instance", bag);
            Assert.Contains("global::Delly.Refer.BasicRefers.Int32ArrayRefer.Instance", bag);
        }

        /// <summary>
        /// 有界展开范围外的成员类型被跳过并产出 <c>DRF0001</c> 告警，但不阻断生成
        /// </summary>
        [Fact]
        public void Generator_ReportsDRF0001ForUnsupportedMemberType()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    [UseRefer]
    public sealed class Holder
    {
        public Holder() { }
        public System.Collections.Generic.List<int>? Items { get; set; }
        public int Kept { get; set; }
    }
}");

            AssertNoCompileErrors(run);

            Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
            Assert.Equal("DRF0001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("Holder.Items", diagnostic.GetMessage());

            string holder = run.Sources["Sample.HolderRefer.g.cs"];
            Assert.DoesNotContain("Items", holder);
            Assert.Contains("KeptPropertyRefer", holder);
        }

        /// <summary>
        /// 无法在编译期构造的特性被跳过并产出 <c>DRF0002</c> 告警，但不阻断生成
        /// </summary>
        [Fact]
        public void Generator_ReportsDRF0002ForUnconstructibleAttribute()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    public sealed class PointerAttribute : Attribute
    {
        public PointerAttribute(Type type) { }
    }

    [UseRefer]
    [Pointer(typeof(int*))]
    public sealed class Item
    {
        public Item() { }
    }
}");

            AssertNoCompileErrors(run);

            Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
            Assert.Equal("DRF0002", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("PointerAttribute", diagnostic.GetMessage());

            Assert.DoesNotContain("PointerAttribute", run.Sources["Sample.ItemRefer.g.cs"]);
            Assert.Contains(
                "private static readonly global::System.Collections.Generic.IReadOnlyList<global::System.Attribute> s_attributes = global::System.Array.Empty<global::System.Attribute>();",
                run.Sources["Sample.ItemRefer.g.cs"]);
        }

        /// <summary>
        /// 生成器指令自身不进入 <c>GetAttributes()</c>，其余常量特性被还原为构造表达式
        /// </summary>
        [Fact]
        public void Generator_ExcludesUseReferAttributeFromAttributes()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    public sealed class LevelAttribute : Attribute
    {
        public LevelAttribute(int value) { Value = value; }
        public int Value { get; }
        public string? Note { get; set; }
    }

    [UseRefer]
    [Level(3, Note = ""hi"")]
    public sealed class Setting
    {
        public Setting() { }
    }
}");

            AssertNoCompileErrors(run);
            Assert.Empty(run.GeneratorDiagnostics);

            string setting = run.Sources["Sample.SettingRefer.g.cs"];
            Assert.Contains(@"new global::Sample.LevelAttribute(3) { Note = ""hi"" }", setting);
            Assert.DoesNotContain("new global::Delly.Refer.UseReferAttribute", setting);
        }

        /// <summary>
        /// 无标记类型时不产出任何源码
        /// </summary>
        [Fact]
        public void Generator_EmitsNothingWhenNoTypeIsMarked()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    public sealed class Plain
    {
        public Plain() { }
    }
}");

            AssertNoCompileErrors(run);
            Assert.Empty(run.Sources);
        }

        /// <summary>
        /// 零反射契约：生成产物不含任何 <c>System.Reflection</c> 依赖或元数据运行时查询
        /// </summary>
        [Fact]
        public void Generator_GeneratedSourcesContainNoReflectionApi()
        {
            GeneratorRun run = Run(Preamble + @"
namespace Sample
{
    public sealed class Detail
    {
        public Detail(int code) { Code = code; }
        public int Code { get; }
    }

    [UseRefer]
    public sealed class Model
    {
        public Model() { }
        public string? Name { get; set; }
        public Detail? Detail { get; set; }
        public bool Check(int value) { return value > 0; }
    }
}");

            AssertNoCompileErrors(run);

            foreach (KeyValuePair<string, string> source in run.Sources)
            {
                Assert.DoesNotContain("System.Reflection", source.Value);
                Assert.DoesNotContain("Activator", source.Value);
                Assert.DoesNotContain("GetCustomAttributes", source.Value);
                Assert.DoesNotContain("GetType()", source.Value);
                Assert.DoesNotContain("typeof(", source.Value);
                Assert.DoesNotContain("#nullable", source.Value);
            }
        }

        /// <summary>
        /// 编译源码片段并运行生成器
        /// </summary>
        /// <param name="source">源码文本</param>
        /// <returns>运行结果</returns>
        private static GeneratorRun Run(string source)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "ReferGeneratorTests.Sample",
                new[] { CSharpSyntaxTree.ParseText(source) },
                References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ReferGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out Compilation output,
                out ImmutableArray<Diagnostic> diagnostics);

            var sources = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (GeneratorRunResult result in driver.GetRunResult().Results)
            {
                foreach (GeneratedSourceResult generated in result.GeneratedSources)
                {
                    sources[generated.HintName] = generated.SourceText.ToString();
                }
            }

            return new GeneratorRun(output, diagnostics, sources);
        }

        /// <summary>
        /// 断言输出编译无错误
        /// </summary>
        /// <param name="run">运行结果</param>
        private static void AssertNoCompileErrors(GeneratorRun run)
        {
            Diagnostic[] errors = run.Output
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();

            Assert.True(
                errors.Length == 0,
                string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
        }

        /// <summary>
        /// 生成器单次运行结果
        /// </summary>
        private sealed class GeneratorRun
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="output">应用生成产物后的编译</param>
            /// <param name="generatorDiagnostics">生成器上报的诊断</param>
            /// <param name="sources">hint 名 → 生成源码</param>
            public GeneratorRun(
                Compilation output,
                ImmutableArray<Diagnostic> generatorDiagnostics,
                Dictionary<string, string> sources)
            {
                Output = output;
                GeneratorDiagnostics = generatorDiagnostics;
                Sources = sources;
            }

            /// <summary>
            /// 应用生成产物后的编译
            /// </summary>
            public Compilation Output { get; }

            /// <summary>
            /// 生成器上报的诊断
            /// </summary>
            public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

            /// <summary>
            /// hint 名 → 生成源码
            /// </summary>
            public Dictionary<string, string> Sources { get; }
        }
    }
}
