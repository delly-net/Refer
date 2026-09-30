using System;
using System.Collections.Generic;
using System.Linq;
using Delly.Refer.BasicRefers;
using Delly.Refer.Tests.TestModels;
using Xunit;

namespace Delly.Refer.Tests
{
    /// <summary>
    /// 源生成产物测试：测试项目自身以分析器方式接入 <c>Delly.Refer.Generator</c>，
    /// 故此处断言的是编译期真实生成的 Refer 类型，而非手工构造的替身。
    /// </summary>
    /// <remarks>
    /// 覆盖范围：类型级元数据、<c>CreateInstance</c> 重载分派、属性/方法/泛型引用/特性建模、
    /// 递归展开（含未标注 <c>[UseRefer]</c> 的成员类型与嵌套类型）、<c>Invoke</c> 语义。
    /// </remarks>
    public class GeneratedReferTests
    {
        /// <summary>
        /// 生成的 Refer 与原模型同名空间、同名单例
        /// </summary>
        [Fact]
        public void PersonRefer_InstanceIsSingleton()
        {
            Assert.Same(PersonRefer.Instance, PersonRefer.Instance);
        }

        /// <summary>
        /// 类型级元数据固化：名称、命名空间、类型标记与泛型标记均为常量
        /// </summary>
        [Fact]
        public void PersonRefer_ExposesTypeLevelMetadata()
        {
            IRefer refer = PersonRefer.Instance;

            Assert.Equal("Person", refer.Name);
            Assert.Equal("Delly.Refer.Tests.TestModels", refer.Namespace);
            Assert.Equal(TypeCode.Object, refer.TypeCode);
            Assert.False(refer.IsValue);
            Assert.False(refer.IsArray);
            Assert.False(refer.IsGeneric);
            Assert.False(refer.IsGenericDefinition);
            Assert.Equal(0, refer.GenericDefinitionCount);
        }

        /// <summary>
        /// <c>CreateInstance</c> 按实参个数分派到对应构造函数重载
        /// </summary>
        [Fact]
        public void PersonRefer_CreateInstance_DispatchesOverloadsByArgumentCount()
        {
            var parameterless = Assert.IsType<Person>(PersonRefer.Instance.CreateInstance());
            Assert.Null(parameterless.Name);
            Assert.Equal(0, parameterless.Age);

            var named = Assert.IsType<Person>(PersonRefer.Instance.CreateInstance("Tom"));
            Assert.Equal("Tom", named.Name);

            var aged = Assert.IsType<Person>(PersonRefer.Instance.CreateInstance("Tom", 20));
            Assert.Equal("Tom", aged.Name);
            Assert.Equal(20, aged.Age);
        }

        /// <summary>
        /// <c>null</c> 实参绑定到 <c>params</c> 数组本身，须与「零参数」同义而非抛空引用
        /// </summary>
        [Fact]
        public void PersonRefer_CreateInstance_NullArrayFallsBackToParameterless()
        {
            var instance = Assert.IsType<Person>(PersonRefer.Instance.CreateInstance(null));

            Assert.Null(instance.Name);
        }

        /// <summary>
        /// 无匹配构造函数时抛 <see cref="NotSupportedException" />
        /// </summary>
        [Fact]
        public void PersonRefer_CreateInstance_ThrowsForUnmatchedArgumentCount()
        {
            Assert.Throws<NotSupportedException>(() => PersonRefer.Instance.CreateInstance("a", "b", "c"));
        }

        /// <summary>
        /// 属性建模：仅公开实例属性，属性类型复用基础 Refer 或递归生成的 Refer
        /// </summary>
        [Fact]
        public void PersonRefer_GetProperties_ModelsPublicInstanceProperties()
        {
            IReadOnlyList<IPropertyRefer> properties = PersonRefer.Instance.GetProperties();

            Assert.Equal(new[] { "Name", "Age", "Address" }, properties.Select(p => p.Name));
            Assert.Same(StringRefer.Instance, properties.Single(p => p.Name == "Name").Refer);
            Assert.Same(Int32Refer.Instance, properties.Single(p => p.Name == "Age").Refer);
            Assert.Same(AddressRefer.Instance, properties.Single(p => p.Name == "Address").Refer);
        }

        /// <summary>
        /// <c>GetMethods()</c> 返回全部公开实例方法（含泛型方法）
        /// </summary>
        [Fact]
        public void PersonRefer_GetMethods_ReturnsAllMethods()
        {
            IReadOnlyList<IRefer> methods = PersonRefer.Instance.GetMethods();

            Assert.Equal(
                new[] { "IsAdult", "Rename", "IsTypeOf" }.OrderBy(n => n),
                methods.Select(m => m.Name).OrderBy(n => n));
            Assert.All(methods, m => Assert.False(m.IsValue));
        }

        /// <summary>
        /// <c>GetGenericRefers()</c> 与 <c>GetMethods()</c> 返回类型不同，且为其泛型方法子集
        /// </summary>
        [Fact]
        public void PersonRefer_GetGenericRefers_ReturnsGenericMethodSubset()
        {
            IReadOnlyList<IMethodRefer> genericRefers = PersonRefer.Instance.GetGenericRefers();

            IMethodRefer only = Assert.Single(genericRefers);
            Assert.Equal("IsTypeOf", only.Name);
            Assert.Same(BooleanRefer.Instance, only.ReturnRefer);

            // 方法引用同时实现 IRefer 与 IMethodRefer，泛型元数据经 IRefer 侧读取
            var asRefer = Assert.IsAssignableFrom<IRefer>(only);
            Assert.True(asRefer.IsGeneric);
            Assert.True(asRefer.IsGenericDefinition);
            Assert.Equal(1, asRefer.GenericDefinitionCount);

            // GetMethods() 侧返回的是同一实例，仅静态类型不同
            IMethodRefer fromMethods = Assert.IsAssignableFrom<IMethodRefer>(
                PersonRefer.Instance.GetMethods().Single(m => m.Name == "IsTypeOf"));
            Assert.Same(only, fromMethods);
        }

        /// <summary>
        /// 特性建模：编译期把特性使用还原为可编译的常量构造表达式（含命名参数），
        /// 且生成器指令 <see cref="UseReferAttribute" /> 自身不进入 <c>GetAttributes()</c>
        /// </summary>
        [Fact]
        public void PersonRefer_GetAttributes_ReconstructsConstantAttribute()
        {
            IReadOnlyList<Attribute> attributes = PersonRefer.Instance.GetAttributes();

            TagAttribute tag = Assert.IsType<TagAttribute>(Assert.Single(attributes));
            Assert.Equal("person", tag.Value);
            Assert.Equal(7, tag.Order);
        }

        /// <summary>
        /// 非泛型方法可经 <c>Invoke</c> 直接调用，返回值按原类型装箱
        /// </summary>
        [Fact]
        public void PersonRefer_Invoke_ExecutesNonGenericMethod()
        {
            var person = new Person("Tom", 20);

            Assert.Equal(true, PersonRefer.IsAdultMethodRefer.Instance.Invoke(person));

            PersonRefer.RenameMethodRefer.Instance.Invoke(person, "Jerry");
            Assert.Equal("Jerry", person.Name);
        }

        /// <summary>
        /// 泛型方法在零反射约束下无法闭合类型实参，<c>Invoke</c> 抛 <see cref="NotSupportedException" />
        /// </summary>
        [Fact]
        public void PersonRefer_Invoke_ThrowsForGenericMethod()
        {
            Assert.Throws<NotSupportedException>(
                () => PersonRefer.IsTypeOfMethodRefer.Instance.Invoke(new Person()));
        }

        /// <summary>
        /// <c>void</c> 返回方法以共享的 <c>VoidRefer</c> 承载返回建模，且 <c>Invoke</c> 返回 <c>null</c>
        /// </summary>
        [Fact]
        public void RenameMethodRefer_ModelsVoidReturnAndParameters()
        {
            IMethodRefer rename = PersonRefer.RenameMethodRefer.Instance;

            Assert.Equal("Void", rename.ReturnRefer.Name);

            IMethodReferParameter parameter = Assert.Single(rename.GetParameters());
            Assert.Equal("name", parameter.Name);
            Assert.Same(StringRefer.Instance, parameter.ParameterRefer);

            Assert.Null(PersonRefer.RenameMethodRefer.Instance.Invoke(new Person(), "Ann"));
        }

        /// <summary>
        /// 无参方法的 <c>GetParameters()</c> 返回空数组且永不返回 <see langword="null" />
        /// </summary>
        [Fact]
        public void EmptyParameters_ReturnEmptyArrayNotNull()
        {
            IMethodReferParameter[] parameters = PersonRefer.IsAdultMethodRefer.Instance.GetParameters();

            Assert.NotNull(parameters);
            Assert.Empty(parameters);
        }

        /// <summary>
        /// 成员类型未标注 <c>[UseRefer]</c> 时按「有界展开」递归生成其 Refer
        /// </summary>
        [Fact]
        public void AddressRefer_IsGeneratedForReferencedModel()
        {
            IRefer refer = AddressRefer.Instance;

            Assert.Equal("Address", refer.Name);
            Assert.Equal("Delly.Refer.Tests.TestModels", refer.Namespace);
            Assert.False(refer.IsValue);

            var address = Assert.IsType<Address>(refer.CreateInstance("Beijing"));
            Assert.Equal("Beijing", address.City);
        }

        /// <summary>
        /// 仅声明带参构造函数时，零参 <c>CreateInstance</c> 抛 <see cref="NotSupportedException" />
        /// </summary>
        [Fact]
        public void AddressRefer_CreateInstance_ThrowsWithoutParameterlessConstructor()
        {
            Assert.Throws<NotSupportedException>(() => AddressRefer.Instance.CreateInstance());
        }

        /// <summary>
        /// 嵌套类型的生成类名展平为 <c>{外层}_{内层}Refer</c>，而 <c>Name</c> 仍与 <c>typeof(T).Name</c> 对齐
        /// </summary>
        [Fact]
        public void OrderRefer_NestedTypeGetsFlattenedReferClassName()
        {
            Assert.Equal("Line", Order_LineRefer.Instance.Name);
            Assert.Equal("Delly.Refer.Tests.TestModels", Order_LineRefer.Instance.Namespace);

            var line = Assert.IsType<Order.Line>(Order_LineRefer.Instance.CreateInstance(3));
            Assert.Equal(3, line.Quantity);

            Assert.Same(
                Order_LineRefer.Instance,
                OrderRefer.Instance.GetProperties().Single(p => p.Name == "First").Refer);
        }

        /// <summary>
        /// 枚举模型：值类型标记为 <see langword="true" />，零参 <c>CreateInstance</c> 返回 <c>default</c>，
        /// 且不存在有参构造分派
        /// </summary>
        [Fact]
        public void PersonKindRefer_BehavesAsValueType()
        {
            IRefer refer = PersonKindRefer.Instance;

            Assert.Equal("PersonKind", refer.Name);
            Assert.True(refer.IsValue);
            Assert.False(refer.IsArray);
            Assert.False(refer.IsGeneric);
            Assert.Equal(PersonKind.Unknown, Assert.IsType<PersonKind>(refer.CreateInstance()));

            Assert.Throws<NotSupportedException>(() => refer.CreateInstance(1));
        }

        /// <summary>
        /// 结构体模型：零参走 <c>default</c>，带参走已声明的构造函数
        /// </summary>
        [Fact]
        public void PointRefer_BehavesAsValueTypeWithConstructorDispatch()
        {
            IRefer refer = PointRefer.Instance;

            Assert.True(refer.IsValue);
            Assert.Equal(default, Assert.IsType<Point>(refer.CreateInstance()));
            Assert.Equal(new Point(1, 2), Assert.IsType<Point>(refer.CreateInstance(1, 2)));
        }

        /// <summary>
        /// 基础值类型引用与数组引用由生成代码复用，而非重新生成
        /// </summary>
        [Fact]
        public void GeneratedRefer_ReusesBasicReferSingletons()
        {
            Assert.Same(
                Int32Refer.Instance,
                PersonRefer.Instance.GetProperties().Single(p => p.Name == "Age").Refer);
        }
    }
}
