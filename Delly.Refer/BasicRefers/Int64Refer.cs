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
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Int64; }
        }
    }
}
