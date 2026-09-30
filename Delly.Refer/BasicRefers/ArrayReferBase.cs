using System;
using System.Collections.Generic;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// 数组类型引用基类
    /// </summary>
    /// <remarks>
    /// 为常见基础类型的一维数组提供 <see cref="IRefer"/> 的通用实现。
    /// 数组为引用类型，且 <c>TElement[]</c> 并非泛型类型，故 <see cref="IRefer.IsValue" />
    /// 与泛型相关成员均为固定值，仅 <see cref="IRefer.IsArray" /> 为 <see langword="true" />。
    /// 元数据由源生成阶段固化，本基类不进行任何运行时反射。
    /// </remarks>
    /// <typeparam name="TElement">数组的元素类型</typeparam>
    public abstract class ArrayReferBase<TElement> : IRefer
    {
        /// <summary>
        /// 名称，取自 <c>typeof(TElement[]).Name</c>（形如 <c>Int32[]</c>）
        /// </summary>
        public virtual string Name
        {
            get { return typeof(TElement[]).Name; }
        }

        /// <summary>
        /// 命名空间，取自 <c>typeof(TElement[]).Namespace</c>
        /// </summary>
        public virtual string Namespace
        {
            get { return typeof(TElement[]).Namespace ?? string.Empty; }
        }

        /// <summary>
        /// 创建数组类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组；无参或 <see langword="null" /> 时返回长度 0 的空数组，单个 <see cref="int" /> 参数时返回该长度的数组</param>
        /// <returns><typeparamref name="TElement" /> 类型的新数组</returns>
        /// <exception cref="ArgumentOutOfRangeException">长度为负时抛出</exception>
        /// <exception cref="NotSupportedException">参数个数不为 0 或 1，或单个参数不是 <see cref="int" /> 时抛出</exception>
#if NET6_0_OR_GREATER
        public abstract object CreateInstance(params object?[] args);
#else
        public abstract object CreateInstance(params object[] args);
#endif

        /// <summary>
        /// 类型编码枚举
        /// </summary>
        /// <remarks>
        /// <see cref="System.TypeCode" /> 未定义数组专用成员（<c>Type.GetTypeCode(typeof(int[]))</c> 亦返回
        /// <see cref="System.TypeCode.Object" />），故统一返回 <see cref="System.TypeCode.Object" />，
        /// 与 <see cref="GuidRefer" /> 的既有取舍一致。
        /// </remarks>
        public virtual TypeCode TypeCode
        {
            get { return TypeCode.Object; }
        }

        /// <summary>
        /// 是否为值类型对象，数组为引用类型，恒为 <see langword="false" />
        /// </summary>
        public virtual bool IsValue
        {
            get { return false; }
        }

        /// <summary>
        /// 是否为数组对象，数组类型引用恒为 <see langword="true" />
        /// </summary>
        public virtual bool IsArray
        {
            get { return true; }
        }

        /// <summary>
        /// 是否为泛型模型，数组类型引用恒为 <see langword="false" />
        /// </summary>
        /// <remarks>
        /// <c>TElement[]</c> 本身不是泛型类型（<c>typeof(int[]).IsGenericType</c> 为 false），
        /// 即使元素类型为泛型，数组类型引用也不建模泛型。
        /// </remarks>
        public virtual bool IsGeneric
        {
            get { return false; }
        }

        /// <summary>
        /// 是否为开放泛型定义，数组类型引用恒为 <see langword="false" />
        /// </summary>
        public virtual bool IsGenericDefinition
        {
            get { return false; }
        }

        /// <summary>
        /// 泛型定义数量，数组类型引用恒为 <c>0</c>
        /// </summary>
        public virtual int GenericDefinitionCount
        {
            get { return 0; }
        }

        /// <summary>
        /// 获取所有方法
        /// </summary>
        /// <returns>方法对象只读列表，数组类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<IRefer> GetMethods()
        {
            return Array.Empty<IRefer>();
        }

        /// <summary>
        /// 获取所有泛型引用
        /// </summary>
        /// <returns>泛型引用对象只读列表，数组类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<IMethodRefer> GetGenericRefers()
        {
            return Array.Empty<IMethodRefer>();
        }

        /// <summary>
        /// 获取所有属性
        /// </summary>
        /// <returns>属性对象只读列表，数组类型引用恒为空集合</returns>
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
        /// <returns>特性对象只读列表，数组类型引用恒为空集合</returns>
        /// <remarks>
        /// 本项目元数据由源生成阶段固化而非运行时反射，故此处直接返回空集合。
        /// </remarks>
        public virtual IReadOnlyList<Attribute> GetAttributes()
        {
            return Array.Empty<Attribute>();
        }

        /// <summary>
        /// 供具体数组类型引用子类复用的实例创建实现
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns><typeparamref name="TElement" /> 类型的新数组</returns>
        /// <exception cref="ArgumentOutOfRangeException">长度为负时抛出</exception>
        /// <exception cref="NotSupportedException">参数个数不为 0 或 1，或单个参数不是 <see cref="int" /> 时抛出</exception>
        /// <remarks>
        /// 实现落在各具体子类的 <c>override</c>（决策 #315），子类仅需转发到本方法，
        /// 使数组实例化语义保持单点修改。
        /// </remarks>
#if NET6_0_OR_GREATER
        protected object CreateArrayInstance(object?[] args)
#else
        protected object CreateArrayInstance(object[] args)
#endif
        {
            if (args == null || args.Length == 0)
            {
                return Array.Empty<TElement>();
            }

            if (args.Length != 1)
            {
                throw new NotSupportedException(
                    "数组类型 " + Name + " 的 CreateInstance 仅接受 0 个或 1 个参数，实际传入 " + args.Length + " 个。");
            }

            // 以 var 承接，net6.0 下推导为 object?、netstandard2.0 下推导为 object，
            // 无需为可空注解再加条件编译。
            var argument = args[0];
            if (argument == null)
            {
                throw new NotSupportedException(
                    "数组类型 " + Name + " 的 CreateInstance 仅接受 int 类型的长度参数，实际传入 null。");
            }

            if (!(argument is int))
            {
                throw new NotSupportedException(
                    "数组类型 " + Name + " 的 CreateInstance 仅接受 int 类型的长度参数，实际传入 " + argument.GetType().Name + "。");
            }

            int length = (int)argument;
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(args), length, "数组长度不能为负数。");
            }

            return new TElement[length];
        }
    }
}
