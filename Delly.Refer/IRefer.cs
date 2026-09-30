using System;
using System.Collections.Generic;
using System.Text;

namespace Delly.Refer
{
    /// <summary>
    /// 引用对象
    /// </summary>
    public interface IRefer
    {

        /// <summary>
        /// 名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命名空间
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// 创建模型类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns>模型类型的新实例</returns>
#if NET6_0_OR_GREATER
        object CreateInstance(params object?[] args);
#else
        object CreateInstance(params object[] args);
#endif

        /// <summary>
        /// 类型编码枚举
        /// </summary>
        TypeCode TypeCode { get; }

        /// <summary>
        /// 是否为值类型对象，基础值类型和 string 为 true，其他 class 类型为 false
        /// </summary>
        bool IsValue { get; }

        /// <summary>
        /// 是否为数组对象
        /// </summary>
        bool IsArray { get; }

        /// <summary>
        /// 是否为泛型模型
        /// </summary>
        /// <remarks>
        /// 对于开放泛型定义（如 List&lt;&gt;）返回 true
        /// 对于已构造泛型（如 List&lt;int&gt;）返回 true
        /// 对于非泛型类型（如 int、string）返回 false
        /// </remarks>
        bool IsGeneric { get; }

        /// <summary>
        /// 是否为开放泛型定义
        /// </summary>
        /// <remarks>
        /// 对于开放泛型定义（如 List&lt;&gt;、Dictionary&lt;,&gt;）返回 true
        /// 对于已构造泛型和非泛型类型返回 false
        /// </remarks>
        bool IsGenericDefinition { get; }

        /// <summary>
        /// 泛型定义数量
        /// </summary>
        /// <remarks>
        /// List&lt;&gt; 返回 1，Dictionary&lt;,&gt; 返回 2
        /// 非泛型类型返回 0
        /// </remarks>
        int GenericDefinitionCount { get; }

        /// <summary>
        /// 获取所有方法
        /// </summary>
        /// <returns>方法对象只读列表</returns>
        IReadOnlyList<IRefer> GetMethods();

        /// <summary>
        /// 获取所有泛型引用
        /// </summary>
        /// <returns>方法对象只读列表</returns>
        IReadOnlyList<IMethodRefer> GetGenericRefers();

        /// <summary>
        /// 获取所有属性
        /// </summary>
        /// <returns>属性对象只读列表</returns>
        IReadOnlyList<IPropertyRefer> GetProperties();

        /// <summary>
        /// 获取所有特性
        /// </summary>
        /// <returns>特性对象只读列表</returns>
        IReadOnlyList<Attribute> GetAttributes();
    }
}
