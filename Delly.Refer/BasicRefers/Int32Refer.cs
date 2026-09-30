using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="int"/> 值类型引用
    /// </summary>
    public sealed class Int32Refer : ValueReferBase<int>
    {
        /// <summary>
        /// <see cref="int"/> 值类型引用实例
        /// </summary>
        public static readonly Int32Refer Instance = new Int32Refer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Int32; }
        }
    }
}
