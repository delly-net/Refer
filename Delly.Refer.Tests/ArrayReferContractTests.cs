using System;
using System.Collections.Generic;
using Delly.Refer;
using Delly.Refer.BasicRefers;
using Xunit;

namespace Delly.Refer.Tests
{
    /// <summary>
    /// 数组类型引用集合的固定契约测试：数组/引用类型标记、名称与命名空间、
    /// <see cref="IRefer.TypeCode" /> 映射、空元数据与 <see cref="IRefer.CreateInstance" /> 语义。
    /// </summary>
    /// <remarks>
    /// 语义约定（决策 #322 ~ #324）：<c>ArrayReferBase&lt;TElement&gt;</c> + 每类型一个 <c>sealed</c> 子类；
    /// <see cref="IRefer.IsArray" /> 为 true 且 <see cref="IRefer.IsValue" /> 为 false；
    /// <see cref="IRefer.TypeCode" /> 统一取 <see cref="System.TypeCode.Object" />；
    /// <see cref="IRefer.CreateInstance" /> 无参或 <see langword="null" /> 参返回长度 0 的空数组，
    /// 单个 <see cref="int" /> 参数返回该长度的数组，其余抛 <see cref="NotSupportedException" />。
    /// </remarks>
    public class ArrayReferContractTests
    {
        /// <summary>
        /// 全部 16 个数组类型引用实例，供跨类型契约断言遍历使用。
        /// </summary>
        private static readonly IRefer[] AllArrayRefers =
        {
            BooleanArrayRefer.Instance,
            ByteArrayRefer.Instance,
            SByteArrayRefer.Instance,
            CharArrayRefer.Instance,
            Int16ArrayRefer.Instance,
            UInt16ArrayRefer.Instance,
            Int32ArrayRefer.Instance,
            UInt32ArrayRefer.Instance,
            Int64ArrayRefer.Instance,
            UInt64ArrayRefer.Instance,
            SingleArrayRefer.Instance,
            DoubleArrayRefer.Instance,
            DecimalArrayRefer.Instance,
            DateTimeArrayRefer.Instance,
            StringArrayRefer.Instance,
            GuidArrayRefer.Instance
        };

        /// <summary>
        /// 各数组类型引用的期望元数据：数组名称（<c>typeof(TElement[]).Name</c>）、命名空间、元素类型与 <see cref="System.TypeCode" />。
        /// </summary>
        private static readonly (IRefer Refer, string Name, string Namespace, Type ElementType, TypeCode TypeCode)[] Expectations =
        {
            (BooleanArrayRefer.Instance, "Boolean[]", "System", typeof(bool), TypeCode.Object),
            (ByteArrayRefer.Instance, "Byte[]", "System", typeof(byte), TypeCode.Object),
            (SByteArrayRefer.Instance, "SByte[]", "System", typeof(sbyte), TypeCode.Object),
            (CharArrayRefer.Instance, "Char[]", "System", typeof(char), TypeCode.Object),
            (Int16ArrayRefer.Instance, "Int16[]", "System", typeof(short), TypeCode.Object),
            (UInt16ArrayRefer.Instance, "UInt16[]", "System", typeof(ushort), TypeCode.Object),
            (Int32ArrayRefer.Instance, "Int32[]", "System", typeof(int), TypeCode.Object),
            (UInt32ArrayRefer.Instance, "UInt32[]", "System", typeof(uint), TypeCode.Object),
            (Int64ArrayRefer.Instance, "Int64[]", "System", typeof(long), TypeCode.Object),
            (UInt64ArrayRefer.Instance, "UInt64[]", "System", typeof(ulong), TypeCode.Object),
            (SingleArrayRefer.Instance, "Single[]", "System", typeof(float), TypeCode.Object),
            (DoubleArrayRefer.Instance, "Double[]", "System", typeof(double), TypeCode.Object),
            (DecimalArrayRefer.Instance, "Decimal[]", "System", typeof(decimal), TypeCode.Object),
            (DateTimeArrayRefer.Instance, "DateTime[]", "System", typeof(DateTime), TypeCode.Object),
            (StringArrayRefer.Instance, "String[]", "System", typeof(string), TypeCode.Object),

            // System.TypeCode 未定义数组专用成员，统一按 Object 返回（决策 #324）。
            (GuidArrayRefer.Instance, "Guid[]", "System", typeof(Guid), TypeCode.Object)
        };

        /// <summary>
        /// 断言单个数组类型引用的 <see cref="IRefer.CreateInstance" /> 全部语义。
        /// </summary>
        /// <param name="refer">待断言的数组类型引用</param>
        /// <param name="elementType">期望的数组元素类型</param>
        private static void AssertCreateInstanceSemantics(IRefer refer, Type elementType)
        {
            Type expectedArrayType = elementType.MakeArrayType();

            // 无参：返回长度 0 的空数组，且元素类型与 TElement 一致
            object empty = refer.CreateInstance();
            Assert.Equal(expectedArrayType, empty.GetType());
            Assert.Empty((Array)empty);

            // null 参：params 数组本身为 null，等价于无参
            object?[]? nullArgs = null;
            object fromNullArgs = refer.CreateInstance(nullArgs!);
            Assert.Equal(expectedArrayType, fromNullArgs.GetType());
            Assert.Empty((Array)fromNullArgs);

            // 空参数数组：同样视为无参
            object fromEmptyArgs = refer.CreateInstance(Array.Empty<object?>());
            Assert.Equal(expectedArrayType, fromEmptyArgs.GetType());
            Assert.Empty((Array)fromEmptyArgs);

            // 单参 int 长度：返回该长度的数组
            object fixedLength = refer.CreateInstance(3);
            Assert.Equal(expectedArrayType, fixedLength.GetType());
            Assert.Equal(3, ((Array)fixedLength).Length);

            object zeroLength = refer.CreateInstance(0);
            Assert.Empty((Array)zeroLength);

            // 负数长度：抛 ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => refer.CreateInstance(-1));

            // 单个非 int 参数（含 null 元素）：抛 NotSupportedException
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance("3"));
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance(new object?[] { null }));

            // 参数个数不为 0 或 1：抛 NotSupportedException
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance(1, 2));
        }

        [Fact]
        public void AllArrayRefers_AreArrayAndReferenceType()
        {
            foreach (IRefer refer in AllArrayRefers)
            {
                Assert.True(refer.IsArray, refer.Name + " 的 IsArray 应为 true");
                Assert.False(refer.IsValue, refer.Name + " 的 IsValue 应为 false（数组是引用类型）");
                Assert.False(refer.IsGeneric, refer.Name + " 的 IsGeneric 应为 false");
                Assert.False(refer.IsGenericDefinition, refer.Name + " 的 IsGenericDefinition 应为 false");
                Assert.Equal(0, refer.GenericDefinitionCount);
            }
        }

        [Fact]
        public void AllArrayRefers_MatchExpectedNameNamespaceAndTypeCode()
        {
            Assert.Equal(AllArrayRefers.Length, Expectations.Length);

            foreach ((IRefer refer, string name, string ns, Type elementType, TypeCode typeCode) in Expectations)
            {
                Assert.Equal(name, refer.Name);
                Assert.Equal(ns, refer.Namespace);
                Assert.Equal(typeCode, refer.TypeCode);

                // 名称与元素类型互为印证：Name 应为元素类型数组名
                Assert.Equal(name, elementType.MakeArrayType().Name);
            }
        }

        [Fact]
        public void AllArrayRefers_ReturnEmptyMetadata()
        {
            foreach (IRefer refer in AllArrayRefers)
            {
                Assert.Empty(refer.GetMethods());
                Assert.Empty(refer.GetGenericRefers());
                Assert.Empty(refer.GetProperties());
                Assert.Empty(refer.GetAttributes());
            }
        }

        /// <summary>
        /// 空集合须由基类复用同一实例返回——借此反证实现未误走运行时反射（反射会按类型各分配一份空集合）。
        /// </summary>
        [Fact]
        public void EmptyMetadata_ReusesSameInstanceAcrossArrayRefers()
        {
            IReadOnlyList<IRefer> methods = AllArrayRefers[0].GetMethods();
            IReadOnlyList<IMethodRefer> genericRefers = AllArrayRefers[0].GetGenericRefers();
            IReadOnlyList<IPropertyRefer> properties = AllArrayRefers[0].GetProperties();
            IReadOnlyList<Attribute> attributes = AllArrayRefers[0].GetAttributes();

            foreach (IRefer refer in AllArrayRefers)
            {
                Assert.Same(methods, refer.GetMethods());
                Assert.Same(genericRefers, refer.GetGenericRefers());
                Assert.Same(properties, refer.GetProperties());
                Assert.Same(attributes, refer.GetAttributes());
            }
        }

        [Fact]
        public void BooleanArrayRefer_CreateInstance_ReturnsBooleanArray()
        {
            AssertCreateInstanceSemantics(BooleanArrayRefer.Instance, typeof(bool));
        }

        [Fact]
        public void ByteArrayRefer_CreateInstance_ReturnsByteArray()
        {
            AssertCreateInstanceSemantics(ByteArrayRefer.Instance, typeof(byte));
        }

        [Fact]
        public void SByteArrayRefer_CreateInstance_ReturnsSByteArray()
        {
            AssertCreateInstanceSemantics(SByteArrayRefer.Instance, typeof(sbyte));
        }

        [Fact]
        public void CharArrayRefer_CreateInstance_ReturnsCharArray()
        {
            AssertCreateInstanceSemantics(CharArrayRefer.Instance, typeof(char));
        }

        [Fact]
        public void Int16ArrayRefer_CreateInstance_ReturnsInt16Array()
        {
            AssertCreateInstanceSemantics(Int16ArrayRefer.Instance, typeof(short));
        }

        [Fact]
        public void UInt16ArrayRefer_CreateInstance_ReturnsUInt16Array()
        {
            AssertCreateInstanceSemantics(UInt16ArrayRefer.Instance, typeof(ushort));
        }

        [Fact]
        public void Int32ArrayRefer_CreateInstance_ReturnsInt32Array()
        {
            AssertCreateInstanceSemantics(Int32ArrayRefer.Instance, typeof(int));

            // 定长数组的元素为元素类型默认值
            Assert.Equal(new[] { 0, 0, 0 }, (int[])Int32ArrayRefer.Instance.CreateInstance(3));
        }

        [Fact]
        public void UInt32ArrayRefer_CreateInstance_ReturnsUInt32Array()
        {
            AssertCreateInstanceSemantics(UInt32ArrayRefer.Instance, typeof(uint));
        }

        [Fact]
        public void Int64ArrayRefer_CreateInstance_ReturnsInt64Array()
        {
            AssertCreateInstanceSemantics(Int64ArrayRefer.Instance, typeof(long));
        }

        [Fact]
        public void UInt64ArrayRefer_CreateInstance_ReturnsUInt64Array()
        {
            AssertCreateInstanceSemantics(UInt64ArrayRefer.Instance, typeof(ulong));
        }

        [Fact]
        public void SingleArrayRefer_CreateInstance_ReturnsSingleArray()
        {
            AssertCreateInstanceSemantics(SingleArrayRefer.Instance, typeof(float));
        }

        [Fact]
        public void DoubleArrayRefer_CreateInstance_ReturnsDoubleArray()
        {
            AssertCreateInstanceSemantics(DoubleArrayRefer.Instance, typeof(double));
        }

        [Fact]
        public void DecimalArrayRefer_CreateInstance_ReturnsDecimalArray()
        {
            AssertCreateInstanceSemantics(DecimalArrayRefer.Instance, typeof(decimal));
        }

        [Fact]
        public void DateTimeArrayRefer_CreateInstance_ReturnsDateTimeArray()
        {
            AssertCreateInstanceSemantics(DateTimeArrayRefer.Instance, typeof(DateTime));
        }

        [Fact]
        public void StringArrayRefer_CreateInstance_ReturnsStringArray()
        {
            AssertCreateInstanceSemantics(StringArrayRefer.Instance, typeof(string));
        }

        [Fact]
        public void GuidArrayRefer_CreateInstance_ReturnsGuidArray()
        {
            AssertCreateInstanceSemantics(GuidArrayRefer.Instance, typeof(Guid));
        }
    }
}
