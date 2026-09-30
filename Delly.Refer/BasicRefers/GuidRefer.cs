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
        /// 创建值类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns>该值类型的默认实例（<c>default(Guid)</c>，即 <see cref="System.Guid.Empty" />）</returns>
        /// <exception cref="NotSupportedException">传入任何参数时抛出；值类型不具备带参构造语义</exception>
#if NET6_0_OR_GREATER
        public override object CreateInstance(params object?[] args)
#else
        public override object CreateInstance(params object[] args)
#endif
        {
            if (args != null && args.Length != 0)
            {
                throw new NotSupportedException("值类型 " + Name + " 不具备带参构造语义，CreateInstance 不接受任何参数。");
            }

            return default(Guid);
        }

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
