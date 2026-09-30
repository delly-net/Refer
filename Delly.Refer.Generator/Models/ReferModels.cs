using System.Collections.Generic;

namespace Delly.Refer.Generator.Models
{
    /// <summary>
    /// 属性引用模型
    /// </summary>
    internal sealed class ReferPropertyModel
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 生成的嵌套属性引用类型名
        /// </summary>
        public string ReferClassName { get; set; } = string.Empty;

        /// <summary>
        /// 属性类型的 Refer 表达式
        /// </summary>
        public string ReferExpression { get; set; } = string.Empty;
    }

    /// <summary>
    /// 方法参数模型
    /// </summary>
    internal sealed class ReferParameterModel
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型的 Refer 表达式
        /// </summary>
        public string ReferExpression { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型的完整限定类型名（用于 Invoke 中的强制转换）
        /// </summary>
        public string TypeFullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 方法引用模型
    /// </summary>
    internal sealed class ReferMethodModel
    {
        /// <summary>
        /// 方法名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 生成的嵌套方法引用类型名
        /// </summary>
        public string ReferClassName { get; set; } = string.Empty;

        /// <summary>
        /// 返回类型的 Refer 表达式
        /// </summary>
        public string ReturnExpression { get; set; } = string.Empty;

        /// <summary>
        /// 是否为 void 返回
        /// </summary>
        public bool ReturnsVoid { get; set; }

        /// <summary>
        /// 是否为泛型方法
        /// </summary>
        public bool IsGeneric { get; set; }

        /// <summary>
        /// 泛型方法类型参数数量
        /// </summary>
        public int GenericParameterCount { get; set; }

        /// <summary>
        /// 方法参数
        /// </summary>
        public List<ReferParameterModel> Parameters { get; } = new List<ReferParameterModel>();
    }

    /// <summary>
    /// 构造函数模型
    /// </summary>
    internal sealed class ReferConstructorModel
    {
        /// <summary>
        /// 参数类型的完整限定类型名列表
        /// </summary>
        public List<string> ParameterTypeFullNames { get; } = new List<string>();
    }

    /// <summary>
    /// 类型引用模型
    /// </summary>
    internal sealed class ReferTypeModel
    {
        /// <summary>
        /// 命名空间（全局命名空间为空串）
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// 类型名称（与 <c>typeof(T).Name</c> 对齐，泛型形如 <c>Person`1</c>）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 完整限定类型名
        /// </summary>
        public string TypeFullName { get; set; } = string.Empty;

        /// <summary>
        /// 生成类型名
        /// </summary>
        public string ReferClassName { get; set; } = string.Empty;

        /// <summary>
        /// 是否为值类型
        /// </summary>
        public bool IsValue { get; set; }

        /// <summary>
        /// 是否为泛型类型
        /// </summary>
        public bool IsGeneric { get; set; }

        /// <summary>
        /// 是否为开放泛型定义
        /// </summary>
        public bool IsGenericDefinition { get; set; }

        /// <summary>
        /// 泛型定义数量
        /// </summary>
        public int GenericDefinitionCount { get; set; }

        /// <summary>
        /// 是否可实例化；不可实例化时 CreateInstance 一律抛 NotSupportedException
        /// </summary>
        public bool IsConstructible { get; set; }

        /// <summary>
        /// 零参数分支是否可用
        /// </summary>
        public bool HasParameterlessConstructor { get; set; }

        /// <summary>
        /// 零参数分支是否使用 <c>default(T)</c> 而非 <c>new T()</c>（enum 与 struct 无构造语义）
        /// </summary>
        public bool UsesDefaultForParameterless { get; set; }

        /// <summary>
        /// 带参构造函数
        /// </summary>
        public List<ReferConstructorModel> Constructors { get; } = new List<ReferConstructorModel>();

        /// <summary>
        /// 属性引用
        /// </summary>
        public List<ReferPropertyModel> Properties { get; } = new List<ReferPropertyModel>();

        /// <summary>
        /// 方法引用
        /// </summary>
        public List<ReferMethodModel> Methods { get; } = new List<ReferMethodModel>();

        /// <summary>
        /// 类型自身声明的特性构造表达式
        /// </summary>
        public List<string> Attributes { get; } = new List<string>();
    }
}
