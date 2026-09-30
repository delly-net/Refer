using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="byte"/> 值类型引用
    /// </summary>
    public sealed class ByteRefer : ValueReferBase<byte>
    {
        /// <summary>
        /// <see cref="byte"/> 值类型引用实例
        /// </summary>
        public static readonly ByteRefer Instance = new ByteRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Byte; }
        }
    }
}
