using Microsoft.CodeAnalysis;

namespace Delly.Refer.Generator
{
    /// <summary>
    /// 生成类型的命名规则
    /// </summary>
    internal static class ReferNaming
    {
        /// <summary>
        /// 依据模型类型计算生成类型名
        /// </summary>
        /// <param name="type">模型类型</param>
        /// <returns>生成类型名</returns>
        /// <remarks>
        /// 顶层类型为 <c>{类型名}Refer</c>；嵌套类型为 <c>{外层类型名}_{类型名}Refer</c>，
        /// 生成位置仍在嵌套类型所属的命名空间顶层，避免与外层类型的生成结果同名冲突。
        /// </remarks>
        public static string GetReferClassName(INamedTypeSymbol type)
        {
            string name = type.Name;

            INamedTypeSymbol? containing = type.ContainingType;
            while (containing != null)
            {
                name = containing.Name + "_" + name;
                containing = containing.ContainingType;
            }

            return name + ReferConstants.ReferSuffix;
        }
    }
}
