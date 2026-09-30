using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="long"/> 值类型引用
    /// </summary>
    public sealed class Int64Refer : ValueReferBase<long>
    {
        /// <summary>
        /// <see cref="long"/> 值类型引用实例
        /// </summary>
        public static readonly Int64Refer Instance = new Int64Refer();

        /// <summary>
        /// 创建值类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns>该值类型的默认实例（<c>default(long)</c>）</returns>
        /// <exception cref="NotSupportedException">传入任何参数时抛出；值类型不具备带参构造语义</exception>
#if NET6_0_OR_GREATER
        public override object CreateInstance(params object?[] args)
#else
        public override object CreateInstance(params object[] args)
#endif
        {
            if (args != null && args.Length != 0)
            {
                throw new NotSupportedException("值类型 " + Name + " 不具备带参构造语义，CreateInstance 不接受任何参数。");
            }

            return default(long);
        }

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Int64; }
        }
    }
}
