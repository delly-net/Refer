using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Delly.Refer.Generator.Models;
using Microsoft.CodeAnalysis.CSharp;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 引用模型的 C# 源码发射器
    /// </summary>
    /// <remarks>
    /// 生成代码的全部类型引用均使用 <c>global::</c> 限定，文件内不产生任何 <c>using</c> 指令依赖。
    /// 所有成员签名按 <c>#if NET6_0_OR_GREATER</c> 输出 <c>object?[]</c> / <c>object[]</c> 双分支，
    /// 与核心库 <c>BasicRefers</c> 的既有约定保持一致。
    /// </remarks>
    internal static class ReferEmitter
    {
        private const string ObjectTypeName = "global::System.Object";
        private const string StringTypeName = "global::System.String";
        private const string TypeCodeTypeName = "global::System.TypeCode";
        private const string AttributeTypeName = "global::System.Attribute";
        private const string ReferTypeName = "global::Delly.Refer.IRefer";
        private const string MethodReferTypeName = "global::Delly.Refer.IMethodRefer";
        private const string ParameterReferTypeName = "global::Delly.Refer.IMethodReferParameter";
        private const string PropertyReferTypeName = "global::Delly.Refer.IPropertyRefer";
        private const string ReadOnlyListTypeName = "global::System.Collections.Generic.IReadOnlyList";
        private const string ArrayTypeName = "global::System.Array";

        /// <summary>
        /// 发射单个类型的引用实现
        /// </summary>
        /// <param name="model">引用模型</param>
        /// <returns>C# 源码</returns>
        public static string EmitType(ReferTypeModel model)
        {
            var writer = new CodeWriter();
            WriteHeader(writer);

            bool namespaced = model.Namespace.Length > 0;
            if (namespaced)
            {
                writer.Line("namespace " + model.Namespace);
                writer.OpenBrace();
            }

            writer.Line("/// <summary>");
            writer.Line("/// <c>" + EscapeXml(model.TypeFullName) + "</c> 的 <c>IRefer</c> 实现");
            writer.Line("/// </summary>");
            writer.Line("/// <remarks>");
            writer.Line("/// 元数据由源生成阶段固化，不进行任何运行时反射；");
            writer.Line("/// 生成器指令 <c>UseReferAttribute</c> 不参与 <c>GetAttributes()</c> 建模。");
            writer.Line("/// </remarks>");
            writer.Line("public sealed class " + model.ReferClassName + " : " + ReferTypeName);
            writer.OpenBrace();

            WriteInstanceField(writer, model.ReferClassName);
            WriteListField(writer, "s_methods", ReferTypeName, BuildMethodExpressions(model));
            WriteListField(writer, "s_genericRefers", MethodReferTypeName, BuildGenericMethodExpressions(model));
            WriteListField(writer, "s_properties", PropertyReferTypeName, BuildPropertyExpressions(model));
            WriteListField(writer, "s_attributes", AttributeTypeName, model.Attributes);

            writer.Line("private " + model.ReferClassName + "() { }");
            writer.Blank();

            WriteTypeLevelMembers(writer, model);
            WriteCreateInstance(writer, model);

            foreach (ReferPropertyModel property in model.Properties)
            {
                writer.Blank();
                WritePropertyRefer(writer, property);
            }

            foreach (ReferMethodModel method in model.Methods)
            {
                writer.Blank();
                WriteMethodRefer(writer, model, method);
            }

            writer.CloseBrace();

            if (namespaced)
            {
                writer.CloseBrace();
            }

            return writer.ToString();
        }

        /// <summary>
        /// 发射公共支撑类型（void 引用与方法参数引用）
        /// </summary>
        /// <returns>C# 源码</returns>
        public static string EmitInfrastructure()
        {
            var writer = new CodeWriter();
            WriteHeader(writer);

            writer.Line("namespace " + ReferConstants.InfrastructureNamespace);
            writer.OpenBrace();

            // void 引用
            writer.Line("/// <summary>");
            writer.Line("/// <c>void</c> 返回类型的引用，供源生成的方法引用复用");
            writer.Line("/// </summary>");
            writer.Line("/// <remarks>");
            writer.Line("/// <c>void</c> 不构成模型类型，本类型仅作为方法返回建模的占位引用，不可实例化。");
            writer.Line("/// </remarks>");
            writer.Line("public sealed class VoidRefer : " + ReferTypeName);
            writer.OpenBrace();

            WriteInstanceField(writer, "VoidRefer");

            writer.Line("private VoidRefer() { }");
            writer.Blank();

            WriteSimpleMember(writer, StringTypeName, "Name", "\"Void\"");
            WriteSimpleMember(writer, StringTypeName, "Namespace", "\"System\"");
            WriteSimpleMember(writer, TypeCodeTypeName, "TypeCode", TypeCodeTypeName + ".Object");
            WriteSimpleMember(writer, "global::System.Boolean", "IsValue", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsArray", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsGeneric", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsGenericDefinition", "false");
            WriteSimpleMember(writer, "global::System.Int32", "GenericDefinitionCount", "0");

            WriteEmptyListMember(writer, ReferTypeName, "GetMethods");
            WriteEmptyListMember(writer, MethodReferTypeName, "GetGenericRefers");
            WriteEmptyListMember(writer, PropertyReferTypeName, "GetProperties");
            WriteEmptyListMember(writer, AttributeTypeName, "GetAttributes");
            WriteCreateInstanceSignature(writer);
            writer.OpenBrace();
            writer.Line("throw new global::System.NotSupportedException(\"void 不构成模型类型，CreateInstance 不受支持。\");");
            writer.CloseBrace();

            writer.CloseBrace();
            writer.Blank();

            // 方法参数引用
            writer.Line("/// <summary>");
            writer.Line("/// 方法参数引用，供源生成的方法引用复用");
            writer.Line("/// </summary>");
            writer.Line("public sealed class MethodReferParameter : " + ParameterReferTypeName);
            writer.OpenBrace();

            writer.Line("private readonly " + StringTypeName + " _name;");
            writer.Line("private readonly " + ReferTypeName + " _refer;");
            writer.Blank();

            writer.Line("/// <summary>");
            writer.Line("/// 构造函数");
            writer.Line("/// </summary>");
            writer.Line("/// <param name=\"name\">参数名称</param>");
            writer.Line("/// <param name=\"refer\">参数类型的引用</param>");
            writer.Line("public MethodReferParameter(" + StringTypeName + " name, " + ReferTypeName + " refer)");
            writer.OpenBrace();
            writer.Line("_name = name;");
            writer.Line("_refer = refer;");
            writer.CloseBrace();
            writer.Blank();

            writer.Line("/// <inheritdoc />");
            writer.Line("public " + StringTypeName + " Name { get { return _name; } }");
            writer.Blank();
            writer.Line("/// <inheritdoc />");
            writer.Line("public " + ReferTypeName + " ParameterRefer { get { return _refer; } }");

            writer.CloseBrace();
            writer.CloseBrace();

            return writer.ToString();
        }

        /// <summary>
        /// 写入文件头
        /// </summary>
        /// <param name="writer">写入器</param>
        private static void WriteHeader(CodeWriter writer)
        {
            writer.Line("// <auto-generated />");
            writer.Line("// 本文件由 Delly.Refer.Generator 依据 [UseRefer] 标记生成，请勿手动修改。");
            writer.Line("#pragma warning disable");
            writer.Blank();
        }

        /// <summary>
        /// 写入单例字段
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="className">类型名</param>
        private static void WriteInstanceField(CodeWriter writer, string className)
        {
            writer.Line("/// <summary>");
            writer.Line("/// <see cref=\"" + className + "\" /> 单例");
            writer.Line("/// </summary>");
            writer.Line("public static readonly " + className + " " + ReferConstants.InstanceFieldName +
                         " = new " + className + "();");
            writer.Blank();
        }

        /// <summary>
        /// 写入只读列表字段
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="elementTypeName">元素类型名</param>
        /// <param name="items">元素表达式集合</param>
        private static void WriteListField(
            CodeWriter writer,
            string fieldName,
            string elementTypeName,
            IReadOnlyList<string> items)
        {
            string initializer = items.Count == 0
                ? ArrayTypeName + ".Empty<" + elementTypeName + ">()"
                : "new " + elementTypeName + "[] { " + string.Join(", ", items) + " }";

            writer.Line("private static readonly " + ReadOnlyListTypeName + "<" + elementTypeName + "> " +
                        fieldName + " = " + initializer + ";");
            writer.Blank();
        }

        /// <summary>
        /// 写入类型级元数据成员
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="model">引用模型</param>
        private static void WriteTypeLevelMembers(CodeWriter writer, ReferTypeModel model)
        {
            WriteSimpleMember(writer, StringTypeName, "Name", StringLiteral(model.Name));
            WriteSimpleMember(writer, StringTypeName, "Namespace", StringLiteral(model.Namespace));
            WriteSimpleMember(writer, TypeCodeTypeName, "TypeCode", TypeCodeTypeName + ".Object");
            WriteSimpleMember(writer, "global::System.Boolean", "IsValue", BooleanLiteral(model.IsValue));
            WriteSimpleMember(writer, "global::System.Boolean", "IsArray", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsGeneric", BooleanLiteral(model.IsGeneric));
            WriteSimpleMember(writer, "global::System.Boolean", "IsGenericDefinition", BooleanLiteral(model.IsGenericDefinition));
            WriteSimpleMember(writer, "global::System.Int32", "GenericDefinitionCount",
                model.GenericDefinitionCount.ToString(CultureInfo.InvariantCulture));

            WriteListMember(writer, ReferTypeName, "GetMethods", "s_methods");
            WriteListMember(writer, MethodReferTypeName, "GetGenericRefers", "s_genericRefers");
            WriteListMember(writer, PropertyReferTypeName, "GetProperties", "s_properties");
            WriteListMember(writer, AttributeTypeName, "GetAttributes", "s_attributes");
        }

        /// <summary>
        /// 写入简单只读成员
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="typeName">成员类型名</param>
        /// <param name="memberName">成员名</param>
        /// <param name="expression">取值表达式</param>
        private static void WriteSimpleMember(CodeWriter writer, string typeName, string memberName, string expression)
        {
            writer.Line("/// <inheritdoc />");
            writer.Line("public " + typeName + " " + memberName + " { get { return " + expression + "; } }");
            writer.Blank();
        }

        /// <summary>
        /// 写入返回只读列表字段的方法（<c>IRefer</c> 的四个元数据入口均为方法而非属性）
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="elementTypeName">元素类型名</param>
        /// <param name="memberName">方法名</param>
        /// <param name="fieldName">字段名</param>
        private static void WriteListMember(
            CodeWriter writer,
            string elementTypeName,
            string memberName,
            string fieldName)
        {
            writer.Line("/// <inheritdoc />");
            writer.Line("public " + ReadOnlyListTypeName + "<" + elementTypeName + "> " + memberName +
                        "() { return " + fieldName + "; }");
            writer.Blank();
        }

        /// <summary>
        /// 写入恒返回空集合的成员
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="elementTypeName">元素类型名</param>
        /// <param name="memberName">成员名</param>
        private static void WriteEmptyListMember(CodeWriter writer, string elementTypeName, string memberName)
        {
            writer.Line("/// <inheritdoc />");
            writer.Line("public " + ReadOnlyListTypeName + "<" + elementTypeName + "> " + memberName +
                        "() { return " + ArrayTypeName + ".Empty<" + elementTypeName + ">(); }");
            writer.Blank();
        }

        /// <summary>
        /// 写入 CreateInstance 签名（含多目标条件编译双分支）
        /// </summary>
        /// <param name="writer">写入器</param>
        private static void WriteCreateInstanceSignature(CodeWriter writer)
        {
            writer.Line("/// <inheritdoc />");
            writer.Line("#if NET6_0_OR_GREATER");
            writer.Line("public " + ObjectTypeName + " CreateInstance(params object?[] args)");
            writer.Line("#else");
            writer.Line("public " + ObjectTypeName + " CreateInstance(params object[] args)");
            writer.Line("#endif");
        }

        /// <summary>
        /// 写入 CreateInstance 实现
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="model">引用模型</param>
        private static void WriteCreateInstance(CodeWriter writer, ReferTypeModel model)
        {
            WriteCreateInstanceSignature(writer);
            writer.OpenBrace();

            if (!model.IsConstructible)
            {
                writer.Line("throw new global::System.NotSupportedException(" +
                            "\"模型类型 \" + Name + \" 不支持实例化，CreateInstance 不受支持。\");");
                writer.CloseBrace();
                return;
            }

            writer.Line("if (args == null || args.Length == 0)");
            writer.OpenBrace();
            if (model.HasParameterlessConstructor)
            {
                writer.Line("return " + (model.UsesDefaultForParameterless
                    ? "default(" + model.TypeFullName + ")"
                    : "new " + model.TypeFullName + "()") + ";");
            }
            else
            {
                writer.Line("throw new global::System.NotSupportedException(" +
                            "\"模型类型 \" + Name + \" 不存在公开的无参构造函数。\");");
            }

            writer.CloseBrace();
            writer.Blank();

            if (model.Constructors.Count > 0)
            {
                writer.Line("switch (args.Length)");
                writer.OpenBrace();
                foreach (ReferConstructorModel constructor in model.Constructors)
                {
                    writer.Line("case " + constructor.ParameterTypeFullNames.Count.ToString(CultureInfo.InvariantCulture) + ":");
                    writer.Line("    return new " + model.TypeFullName + "(" +
                                BuildArgumentCasts(constructor.ParameterTypeFullNames) + ");");
                }

                writer.CloseBrace();
                writer.Blank();
            }

            writer.Line("throw new global::System.NotSupportedException(" +
                        "\"模型类型 \" + Name + \" 不存在参数个数为 \" + args.Length + \" 的构造函数。\");");
            writer.CloseBrace();
        }

        /// <summary>
        /// 构建按索引取参并强制转换的实参串
        /// </summary>
        /// <param name="parameterTypeFullNames">参数类型完整限定名</param>
        /// <returns>实参串</returns>
        private static string BuildArgumentCasts(IReadOnlyList<string> parameterTypeFullNames)
        {
            var arguments = new List<string>(parameterTypeFullNames.Count);
            for (int i = 0; i < parameterTypeFullNames.Count; i++)
            {
                arguments.Add("(" + parameterTypeFullNames[i] + ")(object)args[" +
                              i.ToString(CultureInfo.InvariantCulture) + "]");
            }

            return string.Join(", ", arguments);
        }

        /// <summary>
        /// 写入属性引用嵌套类型
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="property">属性模型</param>
        private static void WritePropertyRefer(CodeWriter writer, ReferPropertyModel property)
        {
            writer.Line("/// <summary>");
            writer.Line("/// 属性 <c>" + EscapeXml(property.Name) + "</c> 的引用");
            writer.Line("/// </summary>");
            writer.Line("public sealed class " + property.ReferClassName + " : " + PropertyReferTypeName);
            writer.OpenBrace();

            WriteInstanceField(writer, property.ReferClassName);
            writer.Line("private " + property.ReferClassName + "() { }");
            writer.Blank();

            WriteSimpleMember(writer, StringTypeName, "Name", StringLiteral(property.Name));
            WriteSimpleMember(writer, ReferTypeName, "Refer", property.ReferExpression);

            writer.CloseBrace();
        }

        /// <summary>
        /// 写入方法引用嵌套类型
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="model">所属类型模型</param>
        /// <param name="method">方法模型</param>
        /// <remarks>
        /// 方法引用同时实现 <c>IRefer</c> 与 <c>IMethodRefer</c>：
        /// <c>GetMethods()</c> 得以返回全部方法，<c>GetGenericRefers()</c> 返回其中的泛型方法子集。
        /// </remarks>
        private static void WriteMethodRefer(CodeWriter writer, ReferTypeModel model, ReferMethodModel method)
        {
            writer.Line("/// <summary>");
            writer.Line("/// 方法 <c>" + EscapeXml(method.Name) + "</c> 的引用");
            writer.Line("/// </summary>");
            writer.Line("public sealed class " + method.ReferClassName + " : " + ReferTypeName + ", " + MethodReferTypeName);
            writer.OpenBrace();

            WriteInstanceField(writer, method.ReferClassName);

            var parameters = new List<string>(method.Parameters.Count);
            foreach (ReferParameterModel parameter in method.Parameters)
            {
                parameters.Add("new global::" + ReferConstants.InfrastructureNamespace + ".MethodReferParameter(" +
                               StringLiteral(parameter.Name) + ", " + parameter.ReferExpression + ")");
            }

            writer.Line("private static readonly " + ParameterReferTypeName + "[] s_parameters = " +
                        (parameters.Count == 0
                            ? ArrayTypeName + ".Empty<" + ParameterReferTypeName + ">()"
                            : "new " + ParameterReferTypeName + "[] { " + string.Join(", ", parameters) + " }") +
                        ";");
            writer.Blank();

            writer.Line("private " + method.ReferClassName + "() { }");
            writer.Blank();

            WriteSimpleMember(writer, StringTypeName, "Name", StringLiteral(method.Name));
            WriteSimpleMember(writer, ReferTypeName, "ReturnRefer", method.ReturnExpression);
            writer.Line("/// <inheritdoc />");
            writer.Line("public " + ParameterReferTypeName + "[] GetParameters() { return s_parameters; }");
            writer.Blank();

            WriteSimpleMember(writer, StringTypeName, "Namespace", StringLiteral(model.Namespace));
            WriteSimpleMember(writer, TypeCodeTypeName, "TypeCode", TypeCodeTypeName + ".Object");
            WriteSimpleMember(writer, "global::System.Boolean", "IsValue", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsArray", "false");
            WriteSimpleMember(writer, "global::System.Boolean", "IsGeneric", BooleanLiteral(method.IsGeneric));
            WriteSimpleMember(writer, "global::System.Boolean", "IsGenericDefinition", BooleanLiteral(method.IsGeneric));
            WriteSimpleMember(writer, "global::System.Int32", "GenericDefinitionCount",
                method.GenericParameterCount.ToString(CultureInfo.InvariantCulture));

            WriteEmptyListMember(writer, ReferTypeName, "GetMethods");
            WriteEmptyListMember(writer, MethodReferTypeName, "GetGenericRefers");
            WriteEmptyListMember(writer, PropertyReferTypeName, "GetProperties");
            WriteEmptyListMember(writer, AttributeTypeName, "GetAttributes");

            WriteCreateInstanceSignature(writer);
            writer.OpenBrace();
            writer.Line("throw new global::System.NotSupportedException(" +
                        "\"方法引用不支持创建实例，请改用所属模型类型的 CreateInstance。\");");
            writer.CloseBrace();
            writer.Blank();

            WriteInvoke(writer, model, method);

            writer.CloseBrace();
        }

        /// <summary>
        /// 写入方法调用实现
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="model">所属类型模型</param>
        /// <param name="method">方法模型</param>
        private static void WriteInvoke(CodeWriter writer, ReferTypeModel model, ReferMethodModel method)
        {
            writer.Line("/// <inheritdoc />");
            writer.Line("#if NET6_0_OR_GREATER");
            writer.Line("public " + ObjectTypeName + "? Invoke(" + ObjectTypeName + "? target, params object?[] args)");
            writer.Line("#else");
            writer.Line("public " + ObjectTypeName + " Invoke(" + ObjectTypeName + " target, params object[] args)");
            writer.Line("#endif");
            writer.OpenBrace();

            if (method.IsGeneric)
            {
                writer.Line("throw new global::System.NotSupportedException(" +
                            "\"泛型方法 \" + Name + \" 需在调用时闭合类型实参，零反射约束下 Invoke 不受支持。\");");
                writer.CloseBrace();
                return;
            }

            string invocation = "((" + model.TypeFullName + ")(" + ObjectTypeName + ")target)." +
                                EscapeIdentifier(method.Name) + "(" + BuildArgumentCastsForInvoke(method) + ")";

            if (method.ReturnsVoid)
            {
                writer.Line(invocation + ";");
                // void 方法以显式转换的 null 表达返回值，规避可空上下文下的 CS8603
                writer.Line("return (" + ObjectTypeName + ")null;");
            }
            else
            {
                writer.Line("return " + invocation + ";");
            }

            writer.CloseBrace();
        }

        /// <summary>
        /// 构建 Invoke 的实参串
        /// </summary>
        /// <param name="method">方法模型</param>
        /// <returns>实参串</returns>
        private static string BuildArgumentCastsForInvoke(ReferMethodModel method)
        {
            var arguments = new List<string>(method.Parameters.Count);
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                arguments.Add("(" + method.Parameters[i].TypeFullName + ")(object)args[" +
                              i.ToString(CultureInfo.InvariantCulture) + "]");
            }

            return string.Join(", ", arguments);
        }

        /// <summary>
        /// 构建类型级方法列表表达式
        /// </summary>
        /// <param name="model">引用模型</param>
        /// <returns>表达式集合</returns>
        private static List<string> BuildMethodExpressions(ReferTypeModel model)
        {
            var expressions = new List<string>(model.Methods.Count);
            foreach (ReferMethodModel method in model.Methods)
            {
                expressions.Add(method.ReferClassName + "." + ReferConstants.InstanceFieldName);
            }

            return expressions;
        }

        /// <summary>
        /// 构建泛型方法子集表达式
        /// </summary>
        /// <param name="model">引用模型</param>
        /// <returns>表达式集合</returns>
        private static List<string> BuildGenericMethodExpressions(ReferTypeModel model)
        {
            var expressions = new List<string>();
            foreach (ReferMethodModel method in model.Methods)
            {
                if (method.IsGeneric)
                {
                    expressions.Add(method.ReferClassName + "." + ReferConstants.InstanceFieldName);
                }
            }

            return expressions;
        }

        /// <summary>
        /// 构建属性列表表达式
        /// </summary>
        /// <param name="model">引用模型</param>
        /// <returns>表达式集合</returns>
        private static List<string> BuildPropertyExpressions(ReferTypeModel model)
        {
            var expressions = new List<string>(model.Properties.Count);
            foreach (ReferPropertyModel property in model.Properties)
            {
                expressions.Add(property.ReferClassName + "." + ReferConstants.InstanceFieldName);
            }

            return expressions;
        }

        /// <summary>
        /// 转义标识符（关键字加 <c>@</c> 前缀）
        /// </summary>
        /// <param name="name">标识符</param>
        /// <returns>可编译的标识符</returns>
        private static string EscapeIdentifier(string name)
        {
            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ||
                   SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
                ? "@" + name
                : name;
        }

        /// <summary>
        /// 生成 C# 字符串字面量
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>字面量</returns>
        private static string StringLiteral(string value)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value ?? string.Empty, true);
        }

        /// <summary>
        /// 生成 C# 布尔字面量
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <returns>字面量</returns>
        private static string BooleanLiteral(bool value)
        {
            return value ? "true" : "false";
        }

        /// <summary>
        /// 转义 XML 文档注释内容
        /// </summary>
        /// <param name="value">原始文本</param>
        /// <returns>转义后文本</returns>
        private static string EscapeXml(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
