using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="decimal"/> 值类型引用
    /// </summary>
    public sealed class DecimalRefer : ValueReferBase<decimal>
    {
        /// <summary>
        /// <see cref="decimal"/> 值类型引用实例
        /// </summary>
        public static readonly DecimalRefer Instance = new DecimalRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Decimal; }
        }
    }
}
