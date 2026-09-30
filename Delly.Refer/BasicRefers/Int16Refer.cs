using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="short"/> 值类型引用
    /// </summary>
    public sealed class Int16Refer : ValueReferBase<short>
    {
        /// <summary>
        /// <see cref="short"/> 值类型引用实例
        /// </summary>
        public static readonly Int16Refer Instance = new Int16Refer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Int16; }
        }
    }
}
