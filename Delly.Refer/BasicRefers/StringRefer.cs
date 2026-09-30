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
        /// 创建值类型的新实例
        /// </summary>
        /// <param name="args">构造函数参数数组</param>
        /// <returns>该值类型的默认实例（<c>default(string)</c>，即 <see langword="null" />）</returns>
        /// <exception cref="NotSupportedException">传入任何参数时抛出；值类型不具备带参构造语义</exception>
        /// <remarks>
        /// <c>default(string)</c> 为 <see langword="null" />，与基类在 net6.0 下的非空返回注解不符，
        /// 故此处以 null 宽容运算符显式表达「默认实例即 null」这一既有约定（决策 #314）。
        /// 若需改为「非 null 空串」语义，应作为独立的破坏性变更评估，不在本实现内擅自更改。
        /// </remarks>
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

#if NET6_0_OR_GREATER
            // net6.0 下启用可空引用类型，default(string) 的可空性与基类非空返回注解不符，
            // 此处以 null 宽容运算符显式表达「默认实例即 null」这一既有约定（决策 #314）。
            return default(string)!;
#else
            return default(string);
#endif
        }

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.String; }
        }
    }
}
