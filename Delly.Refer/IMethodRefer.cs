using System;
using System.Collections.Generic;
using System.Text;

namespace Delly.Refer
{
    /// <summary>
    /// 接口引用对象
    /// </summary>
    public interface IMethodRefer
    {
        /// <summary>
        /// 方法名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 返回类型建模
        /// </summary>
        IRefer ReturnRefer { get; }

        /// <summary>
        /// 返回类型信息，源生成阶段使用 typeof(T) 赋值
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// 获取所有方法参数
        /// </summary>
        /// <returns>参数对象数组（无参数时返回空数组，永不返回 null）</returns>
        IMethodReferParameter[] GetParameters();

        /// <summary>
        /// 调用方法
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <param name="args">方法参数</param>
        /// <returns>方法返回值</returns>
#if NET6_0_OR_GREATER
        object? Invoke(object? target, params object?[] args);
#else
        object Invoke(object target, params object[] args);
#endif
    }
}
