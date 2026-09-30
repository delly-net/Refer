using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 特性构造表达式构建器
    /// </summary>
    /// <remarks>
    /// <see cref="Delly.Refer.IRefer" /> 的 <c>GetAttributes()</c> 返回真实 <see cref="System.Attribute" /> 实例，
    /// 故生成器需在编译期把特性使用还原为可编译的 <c>new</c> 表达式。
    /// 仅当构造函数参数与命名参数均可由编译期常量、<c>typeof</c> 或常量数组表达时才会建模。
    /// </remarks>
    internal static class AttributeExpressionBuilder
    {
        private const string AttributeTypeFullName = "global::System.Attribute";

        /// <summary>
        /// 尝试把特性使用还原为构造表达式
        /// </summary>
        /// <param name="attribute">特性使用数据</param>
        /// <param name="expression">构造表达式</param>
        /// <returns>是否可还原</returns>
        public static bool TryBuild(AttributeData attribute, out string expression)
        {
            expression = string.Empty;

            INamedTypeSymbol? attributeClass = attribute.AttributeClass;
            if (attributeClass == null || attributeClass.TypeKind == TypeKind.Error)
            {
                return false;
            }

            if (attributeClass.IsGenericType || !IsAttributeType(attributeClass))
            {
                return false;
            }

            if (attribute.AttributeConstructor == null)
            {
                return false;
            }

            var arguments = new List<string>();
            foreach (TypedConstant argument in attribute.ConstructorArguments)
            {
                if (!TryBuildConstant(argument, out string argumentLiteral))
                {
                    return false;
                }

                arguments.Add(argumentLiteral);
            }

            var namedArguments = new List<string>();
            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
            {
                if (!TryBuildConstant(named.Value, out string namedLiteral))
                {
                    return false;
                }

                namedArguments.Add(named.Key + " = " + namedLiteral);
            }

            var builder = new StringBuilder();
            builder.Append("new ");
            builder.Append(attributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            builder.Append('(');
            builder.Append(string.Join(", ", arguments));
            builder.Append(')');

            if (namedArguments.Count > 0)
            {
                builder.Append(" { ");
                builder.Append(string.Join(", ", namedArguments));
                builder.Append(" }");
            }

            expression = builder.ToString();
            return true;
        }

        /// <summary>
        /// 判断类型是否继承自 <see cref="System.Attribute" />
        /// </summary>
        /// <param name="type">待判断类型</param>
        /// <returns>是否为特性类型</returns>
        private static bool IsAttributeType(INamedTypeSymbol type)
        {
            INamedTypeSymbol? current = type;
            while (current != null)
            {
                if (current.ToDisplayString() == "System.Attribute")
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// 还原单个 <see cref="TypedConstant" /> 为字面量表达式
        /// </summary>
        /// <param name="constant">常量数据</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildConstant(TypedConstant constant, out string literal)
        {
            literal = string.Empty;

            switch (constant.Kind)
            {
                case TypedConstantKind.Error:
                case TypedConstantKind.Primitive:
                    return constant.Kind == TypedConstantKind.Primitive && TryBuildPrimitive(constant, out literal);

                case TypedConstantKind.Type:
                    if (constant.Value is ITypeSymbol typeSymbol &&
                        TypeReferResolver.TryGetTypeFullName(typeSymbol, out string typeName))
                    {
                        literal = "typeof(" + typeName + ")";
                        return true;
                    }

                    return false;

                case TypedConstantKind.Enum:
                    if (constant.Type == null ||
                        constant.Value == null ||
                        !TypeReferResolver.TryGetTypeFullName(constant.Type, out string enumName))
                    {
                        return false;
                    }

                    literal = "((" + enumName + ")(" + System.Convert.ToString(constant.Value, CultureInfo.InvariantCulture) + "))";
                    return true;

                case TypedConstantKind.Array:
                    return TryBuildArray(constant, out literal);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 还原数组常量
        /// </summary>
        /// <param name="constant">常量数据</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildArray(TypedConstant constant, out string literal)
        {
            literal = string.Empty;

            if (constant.IsNull)
            {
                return TryBuildNull(constant.Type, out literal);
            }

            if (!(constant.Type is IArrayTypeSymbol arrayType) || arrayType.Rank != 1)
            {
                return false;
            }

            if (!TypeReferResolver.TryGetTypeFullName(arrayType.ElementType, out string elementTypeName))
            {
                return false;
            }

            var items = new List<string>();
            foreach (TypedConstant item in constant.Values)
            {
                if (!TryBuildConstant(item, out string itemLiteral))
                {
                    return false;
                }

                items.Add(itemLiteral);
            }

            literal = "new " + elementTypeName + "[] { " + string.Join(", ", items) + " }";
            return true;
        }

        /// <summary>
        /// 还原基元常量
        /// </summary>
        /// <param name="constant">常量数据</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildPrimitive(TypedConstant constant, out string literal)
        {
            literal = string.Empty;

            ITypeSymbol? type = constant.Type;
            if (type == null)
            {
                return false;
            }

            object? value = constant.Value;
            if (value == null)
            {
                return TryBuildNull(type, out literal);
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    literal = (bool)value ? "true" : "false";
                    return true;

                case SpecialType.System_String:
                    literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((string)value, true);
                    return true;

                case SpecialType.System_Char:
                    literal = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral((char)value, true);
                    return true;

                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                    literal = System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    return literal.Length > 0;

                case SpecialType.System_UInt32:
                    literal = System.Convert.ToString(value, CultureInfo.InvariantCulture) + "u";
                    return true;

                case SpecialType.System_Int64:
                    literal = System.Convert.ToString(value, CultureInfo.InvariantCulture) + "L";
                    return true;

                case SpecialType.System_UInt64:
                    literal = System.Convert.ToString(value, CultureInfo.InvariantCulture) + "UL";
                    return true;

                case SpecialType.System_Single:
                    return TryBuildSingle((float)value, out literal);

                case SpecialType.System_Double:
                    return TryBuildDouble((double)value, out literal);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 还原 <see cref="float" /> 字面量
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildSingle(float value, out string literal)
        {
            if (float.IsNaN(value))
            {
                literal = "global::System.Single.NaN";
            }
            else if (float.IsPositiveInfinity(value))
            {
                literal = "global::System.Single.PositiveInfinity";
            }
            else if (float.IsNegativeInfinity(value))
            {
                literal = "global::System.Single.NegativeInfinity";
            }
            else
            {
                literal = value.ToString("R", CultureInfo.InvariantCulture) + "f";
            }

            return true;
        }

        /// <summary>
        /// 还原 <see cref="double" /> 字面量
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildDouble(double value, out string literal)
        {
            if (double.IsNaN(value))
            {
                literal = "global::System.Double.NaN";
            }
            else if (double.IsPositiveInfinity(value))
            {
                literal = "global::System.Double.PositiveInfinity";
            }
            else if (double.IsNegativeInfinity(value))
            {
                literal = "global::System.Double.NegativeInfinity";
            }
            else
            {
                literal = value.ToString("R", CultureInfo.InvariantCulture);
            }

            return true;
        }

        /// <summary>
        /// 还原 null 常量，使用显式转换抑制可空上下文告警
        /// </summary>
        /// <param name="type">常量类型</param>
        /// <param name="literal">字面量表达式</param>
        /// <returns>是否可还原</returns>
        private static bool TryBuildNull(ITypeSymbol? type, out string literal)
        {
            literal = string.Empty;

            if (type == null || type.IsValueType)
            {
                return false;
            }

            if (!TypeReferResolver.TryGetTypeFullName(type, out string typeName))
            {
                return false;
            }

            literal = "(" + typeName + ")null";
            return true;
        }
    }
}
