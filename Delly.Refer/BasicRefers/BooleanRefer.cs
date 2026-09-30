using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="bool"/> 值类型引用
    /// </summary>
    public sealed class BooleanRefer : ValueReferBase<bool>
    {
        /// <summary>
        /// <see cref="bool"/> 值类型引用实例
        /// </summary>
        public static readonly BooleanRefer Instance = new BooleanRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Boolean; }
        }
    }
}
