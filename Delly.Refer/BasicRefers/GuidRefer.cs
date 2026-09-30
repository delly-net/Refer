using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="System.Guid"/> 值类型引用
    /// </summary>
    public sealed class GuidRefer : ValueReferBase<Guid>
    {
        /// <summary>
        /// <see cref="System.Guid"/> 值类型引用实例
        /// </summary>
        public static readonly GuidRefer Instance = new GuidRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        /// <remarks>
        /// <see cref="System.TypeCode"/> 枚举未定义 Guid 专用成员，此处取语义最接近的
        /// <see cref="TypeCode.Object"/>。此为枚举覆盖面所致的取舍，非缺陷。
        /// </remarks>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Object; }
        }
    }
}
