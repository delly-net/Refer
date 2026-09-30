using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="ulong"/> 值类型引用
    /// </summary>
    public sealed class UInt64Refer : ValueReferBase<ulong>
    {
        /// <summary>
        /// <see cref="ulong"/> 值类型引用实例
        /// </summary>
        public static readonly UInt64Refer Instance = new UInt64Refer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.UInt64; }
        }
    }
}
