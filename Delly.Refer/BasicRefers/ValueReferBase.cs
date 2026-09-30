using System;
using System.Collections.Generic;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// 值类型引用基类
    /// </summary>
    /// <remarks>
    /// 为常见基础值类型提供 <see cref="IRefer"/> 的通用实现。
    /// 值类型均为非泛型、编译期静态确定的类型，故泛型相关成员为固定值。
    /// 元数据由源生成阶段固化，本基类不进行任何运行时反射。
    /// </remarks>
    /// <typeparam name="T">被引用的值类型</typeparam>
    public abstract class ValueReferBase<T> : IRefer
    {
        /// <summary>
        /// 名称，取自 <c>typeof(T).Name</c>
        /// </summary>
        public virtual string Name
        {
            get { return typeof(T).Name; }
        }

        /// <summary>
        /// 命名空间，取自 <c>typeof(T).Namespace</c>
        /// </summary>
        public virtual string Namespace
        {
            get { return typeof(T).Namespace ?? string.Empty; }
        }

        /// <summary>
        /// 创建值类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns>该值类型的默认实例（<c>default(T)</c>）</returns>
        /// <exception cref="NotSupportedException">传入任何参数时抛出；值类型不具备带参构造语义</exception>
#if NET6_0_OR_GREATER
        public abstract object CreateInstance(params object?[] args);
#else
        public abstract object CreateInstance(params object[] args);
#endif

        /// <summary>
        /// 模型类型信息，由具体值类型引用子类赋值
        /// </summary>
        public abstract TypeCode TypeCode { get; }

        /// <summary>
        /// 是否为值类型对象，值类型引用恒为 <see langword="true" />
        /// </summary>
        /// <remarks>
        /// 与 <see cref="IRefer.IsValue"/> 的约定一致：基础值类型和 string 均为 true。
        /// </remarks>
        public virtual bool IsValue
        {
            get { return true; }
        }

        /// <summary>
        /// 是否为泛型模型，值类型引用恒为 <see langword="false" />
        /// </summary>
        public virtual bool IsGeneric
        {
            get { return false; }
        }

        /// <summary>
        /// 是否为开放泛型定义，值类型引用恒为 <see langword="false" />
        /// </summary>
        public virtual bool IsGenericDefinition
        {
            get { return false; }
        }

        /// <summary>
        /// 泛型定义数量，值类型引用恒为 <c>0</c>
        /// </summary>
        public virtual int GenericDefinitionCount
        {
            get { return 0; }
        }

        /// <summary>
        /// 获取所有方法
        /// </summary>
        /// <returns>方法对象只读列表，值类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<IMethodRefer> GetMethods()
        {
            return Array.Empty<IMethodRefer>();
        }

        /// <summary>
        /// 获取所有属性
        /// </summary>
        /// <returns>属性对象只读列表，值类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<IPropertyRefer> GetProperties()
        {
            return Array.Empty<IPropertyRefer>();
        }

        /// <summary>
        /// 获取所有特性
        /// </summary>
        /// <returns>特性对象只读列表，值类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<Attribute> GetAttributes()
        {
            return Array.Empty<Attribute>();
        }
    }
}
