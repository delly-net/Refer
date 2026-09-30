using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="uint"/> 值类型引用
    /// </summary>
    public sealed class UInt32Refer : ValueReferBase<uint>
    {
        /// <summary>
        /// <see cref="uint"/> 值类型引用实例
        /// </summary>
        public static readonly UInt32Refer Instance = new UInt32Refer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.UInt32; }
        }
    }
}
