using Microsoft.CodeAnalysis;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 源生成器诊断描述符
    /// </summary>
    internal static class ReferDiagnostics
    {
        private const string Category = "Delly.Refer.Generator";

        /// <summary>
        /// DRF0001：成员类型不在有界展开范围内，该成员已被跳过
        /// </summary>
        public static readonly DiagnosticDescriptor UnsupportedMemberType = new DiagnosticDescriptor(
            id: "DRF0001",
            title: "成员类型不支持源生成建模",
            messageFormat: "成员“{0}”的类型“{1}”暂不支持源生成建模，已跳过该成员。",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "有界展开仅覆盖 16 个基础值类型、其一维数组，以及源码声明的非泛型类型；构造泛型、开放泛型、多维/锯齿数组、指针、ref 参数、ref 返回与框架类型暂不建模。");

        /// <summary>
        /// DRF0002：特性无法在编译期构造，已被跳过
        /// </summary>
        public static readonly DiagnosticDescriptor UnconstructibleAttribute = new DiagnosticDescriptor(
            id: "DRF0002",
            title: "特性无法在编译期构造",
            messageFormat: "类型“{0}”上的特性“{1}”无法在编译期构造，已跳过该特性。",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "仅当特性的构造函数参数与命名参数均可由编译期常量、typeof 或常量数组表达时，该特性才会被建模。");
    }
}
