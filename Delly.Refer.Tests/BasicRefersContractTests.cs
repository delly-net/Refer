using System;
using System.Collections.Generic;
using Delly.Refer;
using Delly.Refer.BasicRefers;
using Xunit;

namespace Delly.Refer.Tests
{
    /// <summary>
    /// 值类型引用集合的固定契约测试：泛型标记、名称/命名空间、空元数据与 <see cref="IRefer.TypeCode" /> 映射。
    /// </summary>
    public class BasicRefersContractTests
    {
        /// <summary>
        /// 全部 16 个值类型引用实例，供跨类型契约断言遍历使用。
        /// </summary>
        private static readonly IRefer[] AllRefers =
        {
            BooleanRefer.Instance,
            ByteRefer.Instance,
            SByteRefer.Instance,
            CharRefer.Instance,
            Int16Refer.Instance,
            UInt16Refer.Instance,
            Int32Refer.Instance,
            UInt32Refer.Instance,
            Int64Refer.Instance,
            UInt64Refer.Instance,
            SingleRefer.Instance,
            DoubleRefer.Instance,
            DecimalRefer.Instance,
            DateTimeRefer.Instance,
            StringRefer.Instance,
            GuidRefer.Instance
        };

        /// <summary>
        /// 各值类型引用的期望元数据：名称（<c>typeof(T).Name</c>）、命名空间与 <see cref="System.TypeCode" />。
        /// </summary>
        private static readonly (IRefer Refer, string Name, string Namespace, TypeCode TypeCode)[] Expectations =
        {
            (BooleanRefer.Instance, "Boolean", "System", TypeCode.Boolean),
            (ByteRefer.Instance, "Byte", "System", TypeCode.Byte),
            (SByteRefer.Instance, "SByte", "System", TypeCode.SByte),
            (CharRefer.Instance, "Char", "System", TypeCode.Char),
            (Int16Refer.Instance, "Int16", "System", TypeCode.Int16),
            (UInt16Refer.Instance, "UInt16", "System", TypeCode.UInt16),
            (Int32Refer.Instance, "Int32", "System", TypeCode.Int32),
            (UInt32Refer.Instance, "UInt32", "System", TypeCode.UInt32),
            (Int64Refer.Instance, "Int64", "System", TypeCode.Int64),
            (UInt64Refer.Instance, "UInt64", "System", TypeCode.UInt64),
            (SingleRefer.Instance, "Single", "System", TypeCode.Single),
            (DoubleRefer.Instance, "Double", "System", TypeCode.Double),
            (DecimalRefer.Instance, "Decimal", "System", TypeCode.Decimal),
            (DateTimeRefer.Instance, "DateTime", "System", TypeCode.DateTime),
            (StringRefer.Instance, "String", "System", TypeCode.String),

            // System.TypeCode 枚举未定义 Guid 专用成员，按既有取舍返回语义最接近的 TypeCode.Object。
            (GuidRefer.Instance, "Guid", "System", TypeCode.Object)
        };

        [Fact]
        public void AllRefers_AreValueAndNonGeneric()
        {
            foreach (IRefer refer in AllRefers)
            {
                Assert.True(refer.IsValue, refer.Name + " 的 IsValue 应为 true");
                Assert.False(refer.IsGeneric, refer.Name + " 的 IsGeneric 应为 false");
                Assert.False(refer.IsGenericDefinition, refer.Name + " 的 IsGenericDefinition 应为 false");
                Assert.Equal(0, refer.GenericDefinitionCount);
            }
        }

        [Fact]
        public void AllRefers_MatchExpectedNameNamespaceAndTypeCode()
        {
            Assert.Equal(AllRefers.Length, Expectations.Length);

            foreach ((IRefer refer, string name, string ns, TypeCode typeCode) in Expectations)
            {
                Assert.Equal(name, refer.Name);
                Assert.Equal(ns, refer.Namespace);
                Assert.Equal(typeCode, refer.TypeCode);
            }
        }

        [Fact]
        public void AllRefers_ReturnEmptyMetadata()
        {
            foreach (IRefer refer in AllRefers)
            {
                Assert.Empty(refer.GetMethods());
                Assert.Empty(refer.GetProperties());
                Assert.Empty(refer.GetAttributes());
            }
        }

        /// <summary>
        /// 空集合须由基类复用同一实例返回——借此反证实现未误走运行时反射（反射会按类型各分配一份空集合）。
        /// </summary>
        [Fact]
        public void EmptyMetadata_ReusesSameInstanceAcrossRefers()
        {
            IReadOnlyList<IMethodRefer> methods = AllRefers[0].GetMethods();
            IReadOnlyList<IPropertyRefer> properties = AllRefers[0].GetProperties();
            IReadOnlyList<Attribute> attributes = AllRefers[0].GetAttributes();

            foreach (IRefer refer in AllRefers)
            {
                Assert.Same(methods, refer.GetMethods());
                Assert.Same(properties, refer.GetProperties());
                Assert.Same(attributes, refer.GetAttributes());
            }
        }
    }
}
