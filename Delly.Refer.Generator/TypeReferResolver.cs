using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 类型 → Refer 表达式解析器
    /// </summary>
    /// <remarks>
    /// 解析范围遵循「有界展开」约定：16 个基础值类型、其一维数组，以及源码声明的非泛型类型。
    /// 其余类型一律解析失败，由调用方跳过该成员并发出 <c>DRF0001</c> 告警。
    /// </remarks>
    internal static class TypeReferResolver
    {
        /// <summary>
        /// void 返回所使用的 Refer 表达式（由生成器公共支撑类型提供）
        /// </summary>
        public const string VoidReferExpression = "global::Delly.Refer.Generated.VoidRefer.Instance";

        /// <summary>
        /// 基础值类型（命名空间限定名）→ <c>BasicRefers</c> 中的值类型引用类名
        /// </summary>
        private static readonly Dictionary<string, string> BasicReferNames = new Dictionary<string, string>
        {
            { "System.Boolean", "BooleanRefer" },
            { "System.Byte", "ByteRefer" },
            { "System.SByte", "SByteRefer" },
            { "System.Char", "CharRefer" },
            { "System.Int16", "Int16Refer" },
            { "System.UInt16", "UInt16Refer" },
            { "System.Int32", "Int32Refer" },
            { "System.UInt32", "UInt32Refer" },
            { "System.Int64", "Int64Refer" },
            { "System.UInt64", "UInt64Refer" },
            { "System.Single", "SingleRefer" },
            { "System.Double", "DoubleRefer" },
            { "System.Decimal", "DecimalRefer" },
            { "System.DateTime", "DateTimeRefer" },
            { "System.String", "StringRefer" },
            { "System.Guid", "GuidRefer" },
        };

        /// <summary>
        /// 将类型解析为 Refer 表达式
        /// </summary>
        /// <param name="type">待解析类型</param>
        /// <param name="expression">解析得到的 Refer 表达式</param>
        /// <param name="referencedType">需要递归生成 Refer 的源码声明类型；使用既有 Refer 时为 <see langword="null" /></param>
        /// <returns>是否解析成功</returns>
        public static bool TryResolve(ITypeSymbol type, out string expression, out INamedTypeSymbol? referencedType)
        {
            expression = string.Empty;
            referencedType = null;

            if (type is IArrayTypeSymbol array)
            {
                // 仅支持一维数组，且元素类型必须是 16 个基础值类型之一
                if (array.Rank != 1)
                {
                    return false;
                }

                if (!BasicReferNames.TryGetValue(GetNamespaceQualifiedName(array.ElementType), out string elementRefer))
                {
                    return false;
                }

                // StringRefer → String → StringArrayRefer
                string elementName = elementRefer.Substring(0, elementRefer.Length - "Refer".Length);
                expression = "global::Delly.Refer.BasicRefers." + elementName + "ArrayRefer.Instance";
                return true;
            }

            if (!(type is INamedTypeSymbol named) || named.TypeKind == TypeKind.Error)
            {
                return false;
            }

            // 构造泛型、开放泛型定义、泛型类型参数一律不展开
            if (named.IsGenericType)
            {
                return false;
            }

            if (BasicReferNames.TryGetValue(GetNamespaceQualifiedName(named), out string basicRefer))
            {
                expression = "global::Delly.Refer.BasicRefers." + basicRefer + ".Instance";
                return true;
            }

            // 递归展开：仅限源码声明的非泛型类型
            if (!IsSourceDeclared(named))
            {
                return false;
            }

            // 跨命名空间引用需带命名空间限定；生成类型一律落在命名空间层
            expression = "global::" + GetNamespacePrefix(named) +
                         ReferNaming.GetReferClassName(named) + "." + ReferConstants.InstanceFieldName;
            referencedType = named;
            return true;
        }

        /// <summary>
        /// 获取命名空间前缀（形如 <c>My.App.</c>）
        /// </summary>
        /// <param name="type">类型符号</param>
        /// <returns>命名空间前缀；全局命名空间返回空串</returns>
        private static string GetNamespacePrefix(INamedTypeSymbol type)
        {
            INamespaceSymbol? ns = type.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
            {
                return string.Empty;
            }

            return ns.ToDisplayString() + ".";
        }

        /// <summary>
        /// 获取类型的完整限定名（形如 <c>global::System.Collections.Generic.List&lt;global::System.String&gt;</c>）
        /// </summary>
        /// <param name="type">待解析类型</param>
        /// <param name="fullName">完整限定名</param>
        /// <returns>是否可表达</returns>
        public static bool TryGetTypeFullName(ITypeSymbol type, out string fullName)
        {
            fullName = string.Empty;

            switch (type.TypeKind)
            {
                case TypeKind.Error:
                case TypeKind.Pointer:
                case TypeKind.FunctionPointer:
                case TypeKind.Dynamic:
                    return false;
            }

            if (type is ITypeParameterSymbol)
            {
                return false;
            }

            fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return !string.IsNullOrEmpty(fullName);
        }

        /// <summary>
        /// 判断类型是否由源码声明
        /// </summary>
        /// <param name="type">待判断类型</param>
        /// <returns>是否源码声明</returns>
        public static bool IsSourceDeclared(INamedTypeSymbol type)
        {
            foreach (Location location in type.Locations)
            {
                if (location.IsInSource)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取命名空间限定名（形如 <c>System.Int32</c>），用于基础类型查表
        /// </summary>
        /// <param name="type">待解析类型</param>
        /// <returns>命名空间限定名；无法表达时返回空串</returns>
        private static string GetNamespaceQualifiedName(ITypeSymbol type)
        {
            if (!(type is INamedTypeSymbol named))
            {
                return string.Empty;
            }

            INamespaceSymbol? ns = named.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
            {
                return named.MetadataName;
            }

            return ns.ToDisplayString() + "." + named.MetadataName;
        }
    }
}
