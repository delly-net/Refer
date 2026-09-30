#if !NETSTANDARD2_0
#nullable enable
#endif

using Delly;
using System;

namespace Delly.Refer
{
    /// <summary>
    /// 模型方法参数接口，提供参数元数据信息
    /// </summary>
    public interface IMethodReferParameter
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 参数类型建模
        /// </summary>
        IRefer ParameterRefer { get; }
    }
}
