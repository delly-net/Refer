using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="double"/> 值类型引用
    /// </summary>
    public sealed class DoubleRefer : ValueReferBase<double>
    {
        /// <summary>
        /// <see cref="double"/> 值类型引用实例
        /// </summary>
        public static readonly DoubleRefer Instance = new DoubleRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Double; }
        }
    }
}
