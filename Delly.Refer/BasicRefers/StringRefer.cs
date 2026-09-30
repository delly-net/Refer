using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="string"/> 引用
    /// </summary>
    /// <remarks>
    /// string 虽为引用类型，但按 <see cref="IRefer.IsValue"/> 的约定视同基础值类型，
    /// 故同样以值类型引用实现，<see cref="IRefer.IsValue"/> 返回 <see langword="true" />。
    /// </remarks>
    public sealed class StringRefer : ValueReferBase<string>
    {
        /// <summary>
        /// <see cref="string"/> 引用实例
        /// </summary>
        public static readonly StringRefer Instance = new StringRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.String; }
        }
    }
}
