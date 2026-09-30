using System;

namespace Delly.Refer.BasicRefers
{
    /// <summary>
    /// <see cref="char"/> 值类型引用
    /// </summary>
    public sealed class CharRefer : ValueReferBase<char>
    {
        /// <summary>
        /// <see cref="char"/> 值类型引用实例
        /// </summary>
        public static readonly CharRefer Instance = new CharRefer();

        /// <summary>
        /// 模型类型信息
        /// </summary>
        public override TypeCode TypeCode
        {
            get { return TypeCode.Char; }
        }
    }
}
