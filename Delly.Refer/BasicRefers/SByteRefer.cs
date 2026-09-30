using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="sbyte"/> 值类型引用
    /// </summary>
    public sealed class SByteRefer : ValueReferBase<sbyte>
    {
        /// <summary>
        /// <see cref="sbyte"/> 值类型引用实例
        /// </summary>
        public static readonly SByteRefer Instance = new SByteRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.SByte; }
        }
    }
}
