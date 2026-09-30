using System;
using System.Collections.Generic;
using System.Text;

namespace Delly.Refer
{
    /// <summary>
    /// 属性引用对象
    /// </summary>
    public interface IPropertyRefer
    {
        /// <summary>
        /// 名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 引用
        /// </summary>
        IRefer Refer { get; }
    }
}
