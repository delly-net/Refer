using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="ushort" /> 数组类型引用
    /// </summary>
    public sealed class UInt16ArrayRefer : ArrayReferBase<ushort>
    {
        /// <summary>
        /// <see cref="ushort" /> 数组类型引用实例
        /// </summary>
        public static readonly UInt16ArrayRefer Instance = new UInt16ArrayRefer();

        /// <summary>
        /// 创建数组类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组；无参或 <see langword="null" /> 时返回长度 0 的空数组，单个 <see cref="int" /> 参数时返回该长度的数组</param>
        /// <returns><see cref="ushort" /> 类型的新数组</returns>
        /// <exception cref="ArgumentOutOfRangeException">长度为负时抛出</exception>
        /// <exception cref="NotSupportedException">参数个数不为 0 或 1，或单个参数不是 <see cref="int" /> 时抛出</exception>
#if NET6_0_OR_GREATER
        public override object CreateInstance(params object?[] args)
#else
        public override object CreateInstance(params object[] args)
#endif
        {
            return CreateArrayInstance(args);
        }
    }
}
