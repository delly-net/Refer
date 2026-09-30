using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="System.DateTime"/> 值类型引用
    /// </summary>
    public sealed class DateTimeRefer : ValueReferBase<DateTime>
    {
        /// <summary>
        /// <see cref="System.DateTime"/> 值类型引用实例
        /// </summary>
        public static readonly DateTimeRefer Instance = new DateTimeRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.DateTime; }
        }
    }
}
