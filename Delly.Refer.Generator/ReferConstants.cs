namespace Delly.Refer.Generator
{
    /// <summary>
    /// 源生成器使用的固定常量
    /// </summary>
    /// <remarks>
    /// 生成器刻意不引用核心库 <c>Delly.Refer</c>：分析器在编译期由 Roslyn 加载，
    /// 若生成器程序集依赖 <c>Delly.Refer.dll</c>，一旦该 DLL 未随分析器一同部署就会加载失败。
    /// 故特性以元数据全名字符串匹配，保证分析器单 DLL 自包含。
    /// </remarks>
    internal static class ReferConstants
    {
        /// <summary>
        /// 标记特性的元数据全名
        /// </summary>
        public const string UseReferAttributeMetadataName = "Delly.Refer.UseReferAttribute";

        /// <summary>
        /// 生成类型名后缀
        /// </summary>
        public const string ReferSuffix = "Refer";

        /// <summary>
        /// 属性引用嵌套类型名后缀
        /// </summary>
        public const string PropertyReferSuffix = "PropertyRefer";

        /// <summary>
        /// 方法引用嵌套类型名后缀
        /// </summary>
        public const string MethodReferSuffix = "MethodRefer";

        /// <summary>
        /// 单例字段名
        /// </summary>
        public const string InstanceFieldName = "Instance";

        /// <summary>
        /// 生成文件后缀
        /// </summary>
        public const string GeneratedFileExtension = ".g.cs";

        /// <summary>
        /// 生成器公共支撑类型的命名空间
        /// </summary>
        public const string InfrastructureNamespace = "Delly.Refer.Generated";
    }
}
