using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Delly.Refer.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 从 Roslyn 符号构建 <see cref="ReferTypeModel" />
    /// </summary>
    internal static class ModelFactory
    {
        /// <summary>
        /// 依据模型类型构建引用模型
        /// </summary>
        /// <param name="symbol">模型类型符号</param>
        /// <param name="diagnostics">诊断收集器</param>
        /// <param name="discovered">递归展开过程中新发现的、需生成 Refer 的源码声明类型</param>
        /// <returns>引用模型；类型不受支持时返回 <see langword="null" /></returns>
        public static ReferTypeModel? Create(
            INamedTypeSymbol symbol,
            List<Diagnostic> diagnostics,
            List<INamedTypeSymbol> discovered)
        {
            if (symbol.TypeKind == TypeKind.Error || symbol.TypeKind == TypeKind.Delegate)
            {
                return null;
            }

            var model = new ReferTypeModel
            {
                Namespace = GetNamespace(symbol),
                Name = symbol.Name,
                TypeFullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReferClassName = ReferNaming.GetReferClassName(symbol),
                IsValue = symbol.IsValueType,
                IsGeneric = symbol.IsGenericType,
                IsGenericDefinition = symbol.IsGenericType && symbol.IsDefinition,
                GenericDefinitionCount = symbol.IsGenericType ? symbol.TypeParameters.Length : 0,
            };

            BuildConstructors(symbol, model);
            BuildAttributes(symbol, symbol, model, diagnostics);
            BuildProperties(symbol, model, diagnostics, discovered);
            BuildMethods(symbol, model, diagnostics, discovered);

            return model;
        }

        /// <summary>
        /// 获取类型所属命名空间
        /// </summary>
        /// <param name="symbol">类型符号</param>
        /// <returns>命名空间；全局命名空间返回空串</returns>
        private static string GetNamespace(INamedTypeSymbol symbol)
        {
            INamespaceSymbol? ns = symbol.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
            {
                return string.Empty;
            }

            return ns.ToDisplayString();
        }

        /// <summary>
        /// 获取符号的源码位置，用于诊断定位
        /// </summary>
        /// <param name="symbol">符号</param>
        /// <returns>位置</returns>
        private static Location GetLocation(ISymbol symbol)
        {
            foreach (Location location in symbol.Locations)
            {
                if (location.IsInSource)
                {
                    return location;
                }
            }

            return Location.None;
        }

        /// <summary>
        /// 构建构造函数分派模型
        /// </summary>
        /// <param name="symbol">模型类型符号</param>
        /// <param name="model">引用模型</param>
        private static void BuildConstructors(INamedTypeSymbol symbol, ReferTypeModel model)
        {
            if (symbol.TypeKind == TypeKind.Enum)
            {
                // enum 无构造语义，零参分支返回 default(TEnum)
                model.IsConstructible = true;
                model.HasParameterlessConstructor = true;
                model.UsesDefaultForParameterless = true;
                return;
            }

            bool constructible = (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Struct)
                && !symbol.IsAbstract
                && !symbol.IsStatic
                && !symbol.IsGenericType;

            if (!constructible)
            {
                return;
            }

            model.IsConstructible = true;

            if (symbol.TypeKind == TypeKind.Struct)
            {
                // struct 恒存在默认值语义
                model.HasParameterlessConstructor = true;
                model.UsesDefaultForParameterless = true;
            }

            foreach (IMethodSymbol constructor in symbol.InstanceConstructors)
            {
                if (constructor.IsStatic)
                {
                    continue;
                }

                Accessibility accessibility = constructor.DeclaredAccessibility;
                if (accessibility != Accessibility.Public &&
                    accessibility != Accessibility.Internal &&
                    accessibility != Accessibility.ProtectedOrInternal)
                {
                    continue;
                }

                if (constructor.Parameters.Length == 0)
                {
                    model.HasParameterlessConstructor = true;
                    model.UsesDefaultForParameterless = false;
                    continue;
                }

                var constructorModel = new ReferConstructorModel();
                bool supported = true;

                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (parameter.RefKind != RefKind.None ||
                        !TypeReferResolver.TryGetTypeFullName(parameter.Type, out string parameterTypeName))
                    {
                        supported = false;
                        break;
                    }

                    constructorModel.ParameterTypeFullNames.Add(parameterTypeName);
                }

                if (supported)
                {
                    model.Constructors.Add(constructorModel);
                }
            }

            if (!model.HasParameterlessConstructor && model.Constructors.Count == 0)
            {
                // 无可访问构造函数，退化为不支持实例化
                model.IsConstructible = false;
            }
        }

        /// <summary>
        /// 构建特性模型
        /// </summary>
        /// <param name="symbol">承载特性的符号</param>
        /// <param name="declaringType">所属模型类型，用于诊断消息</param>
        /// <param name="model">引用模型</param>
        /// <param name="diagnostics">诊断收集器</param>
        private static void BuildAttributes(
            ISymbol symbol,
            INamedTypeSymbol declaringType,
            ReferTypeModel model,
            List<Diagnostic> diagnostics)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                INamedTypeSymbol? attributeClass = attribute.AttributeClass;
                if (attributeClass == null)
                {
                    continue;
                }

                // 生成器指令不是模型元数据，不参与建模
                if (attributeClass.ToDisplayString() == ReferConstants.UseReferAttributeMetadataName)
                {
                    continue;
                }

                if (!AttributeExpressionBuilder.TryBuild(attribute, out string expression))
                {
                    diagnostics.Add(Diagnostic.Create(
                        ReferDiagnostics.UnconstructibleAttribute,
                        GetLocation(symbol),
                        declaringType.Name,
                        attributeClass.ToDisplayString()));

                    continue;
                }

                model.Attributes.Add(expression);
            }
        }

        /// <summary>
        /// 构建属性模型
        /// </summary>
        /// <param name="symbol">模型类型符号</param>
        /// <param name="model">引用模型</param>
        /// <param name="diagnostics">诊断收集器</param>
        /// <param name="discovered">递归展开收集器</param>
        private static void BuildProperties(
            INamedTypeSymbol symbol,
            ReferTypeModel model,
            List<Diagnostic> diagnostics,
            List<INamedTypeSymbol> discovered)
        {
            var usedNames = new HashSet<string>();

            foreach (IPropertySymbol property in symbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer)
                {
                    continue;
                }

                if (property.DeclaredAccessibility != Accessibility.Public ||
                    property.GetMethod == null ||
                    property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (!TypeReferResolver.TryResolve(property.Type, out string expression, out INamedTypeSymbol? referenced))
                {
                    ReportUnsupportedMember(diagnostics, property, symbol);
                    continue;
                }

                if (referenced != null)
                {
                    discovered.Add(referenced);
                }

                model.Properties.Add(new ReferPropertyModel
                {
                    Name = property.Name,
                    ReferClassName = MakeUniqueName(usedNames, property.Name + ReferConstants.PropertyReferSuffix),
                    ReferExpression = expression,
                });
            }
        }

        /// <summary>
        /// 构建方法模型
        /// </summary>
        /// <param name="symbol">模型类型符号</param>
        /// <param name="model">引用模型</param>
        /// <param name="diagnostics">诊断收集器</param>
        /// <param name="discovered">递归展开收集器</param>
        private static void BuildMethods(
            INamedTypeSymbol symbol,
            ReferTypeModel model,
            List<Diagnostic> diagnostics,
            List<INamedTypeSymbol> discovered)
        {
            var usedNames = new HashSet<string>();

            foreach (IMethodSymbol method in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind != MethodKind.Ordinary || method.IsStatic)
                {
                    continue;
                }

                if (method.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                {
                    ReportUnsupportedMember(diagnostics, method, symbol);
                    continue;
                }

                string returnExpression = TypeReferResolver.VoidReferExpression;
                if (!method.ReturnsVoid)
                {
                    if (!TypeReferResolver.TryResolve(method.ReturnType, out returnExpression, out INamedTypeSymbol? returnReferenced))
                    {
                        ReportUnsupportedMember(diagnostics, method, symbol);
                        continue;
                    }

                    if (returnReferenced != null)
                    {
                        discovered.Add(returnReferenced);
                    }
                }

                var methodModel = new ReferMethodModel
                {
                    Name = method.Name,
                    ReferClassName = MakeUniqueName(usedNames, method.Name + ReferConstants.MethodReferSuffix),
                    ReturnExpression = returnExpression,
                    ReturnsVoid = method.ReturnsVoid,
                    IsGeneric = method.IsGenericMethod,
                    GenericParameterCount = method.IsGenericMethod ? method.TypeParameters.Length : 0,
                };

                bool supported = true;
                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    if (parameter.RefKind != RefKind.None)
                    {
                        supported = false;
                        break;
                    }

                    if (!TypeReferResolver.TryResolve(parameter.Type, out string parameterExpression, out INamedTypeSymbol? parameterReferenced) ||
                        !TypeReferResolver.TryGetTypeFullName(parameter.Type, out string parameterTypeName))
                    {
                        supported = false;
                        break;
                    }

                    if (parameterReferenced != null)
                    {
                        discovered.Add(parameterReferenced);
                    }

                    methodModel.Parameters.Add(new ReferParameterModel
                    {
                        Name = parameter.Name,
                        ReferExpression = parameterExpression,
                        TypeFullName = parameterTypeName,
                    });
                }

                if (!supported)
                {
                    ReportUnsupportedMember(diagnostics, method, symbol);
                    continue;
                }

                model.Methods.Add(methodModel);
            }
        }

        /// <summary>
        /// 报告不可建模成员
        /// </summary>
        /// <param name="diagnostics">诊断收集器</param>
        /// <param name="member">成员符号</param>
        /// <param name="declaringType">所属模型类型</param>
        private static void ReportUnsupportedMember(
            List<Diagnostic> diagnostics,
            ISymbol member,
            INamedTypeSymbol declaringType)
        {
            string memberName = declaringType.Name + "." + member.Name;
            string typeName = member is IPropertySymbol property
                ? property.Type.ToDisplayString()
                : member is IMethodSymbol method
                    ? method.ReturnType.ToDisplayString()
                    : member.ToDisplayString();

            diagnostics.Add(Diagnostic.Create(
                ReferDiagnostics.UnsupportedMemberType,
                GetLocation(member),
                memberName,
                typeName));
        }

        /// <summary>
        /// 生成唯一嵌套类型名（重载方法会产生同名候选）
        /// </summary>
        /// <param name="usedNames">已占用名称集合</param>
        /// <param name="baseName">候选名称</param>
        /// <returns>唯一名称</returns>
        private static string MakeUniqueName(HashSet<string> usedNames, string baseName)
        {
            string name = baseName;
            int index = 2;

            while (!usedNames.Add(name))
            {
                name = baseName + index.ToString(CultureInfo.InvariantCulture);
                index++;
            }

            return name;
        }
    }
}
