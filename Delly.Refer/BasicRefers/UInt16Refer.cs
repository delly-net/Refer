using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="ushort"/> 值类型引用
    /// </summary>
    public sealed class UInt16Refer : ValueReferBase<ushort>
    {
        /// <summary>
        /// <see cref="ushort"/> 值类型引用实例
        /// </summary>
        public static readonly UInt16Refer Instance = new UInt16Refer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.UInt16; }
        }
    }
}
