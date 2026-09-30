using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="float"/> 值类型引用
    /// </summary>
    public sealed class SingleRefer : ValueReferBase<float>
    {
        /// <summary>
        /// <see cref="float"/> 值类型引用实例
        /// </summary>
        public static readonly SingleRefer Instance = new SingleRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Single; }
        }
    }
}
