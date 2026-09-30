using System;

namespace Delly.Refer
{
    /// <summary>
    /// 标记模型类型以启用 <see cref="IRefer" /> 源生成
    /// </summary>
    /// <remarks>
    /// <para>
    /// 在模型类型（<c>class</c> / <c>struct</c> / <c>interface</c> / <c>enum</c>）上标注本特性后，
    /// 源生成器 <c>Delly.Refer.Generator</c> 将在编译期为该类型生成一个 <c>{类型名}Refer</c> 类型，
    /// 实现 <see cref="IRefer" /> 并固化其全部元数据。
    /// </para>
    /// <para>
    /// 生成类型与原模型位于同一命名空间，并暴露 <c>public static readonly {类型名}Refer Instance</c> 单例。
    /// </para>
    /// <para>
    /// 本特性仅作为生成器指令使用，不会出现在生成类型的 <see cref="IRefer.GetAttributes" /> 结果中。
    /// </para>
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class UseReferAttribute : Attribute
    {
    }
}
