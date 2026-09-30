using System;
using Delly.Refer;
using Delly.Refer.BasicRefers;
using Xunit;

namespace Delly.Refer.Tests
{
    /// <summary>
    /// 值类型引用 <see cref="IRefer.CreateInstance" /> 语义测试。
    /// </summary>
    /// <remarks>
    /// 语义约定（决策 #314）：无参或 <see langword="null" /> 参返回 <c>default(T)</c>；
    /// 带参一律抛 <see cref="NotSupportedException" />（值类型不具备带参构造语义）。
    /// </remarks>
    public class CreateInstanceTests
    {
        /// <summary>
        /// 断言单个值类型引用的 <see cref="IRefer.CreateInstance" /> 全部语义。
        /// </summary>
        /// <param name="refer">待断言的值类型引用</param>
        /// <param name="expectedDefault">期望的默认值，须为装箱后的 <c>default(T)</c>，以此一并校验装箱类型</param>
        private static void AssertCreateInstanceSemantics(IRefer refer, object? expectedDefault)
        {
            // 无参：返回 default(T)
            object actual = refer.CreateInstance();
            Assert.Equal(expectedDefault, actual);

            // 装箱类型须与 T 一致（如 default(long) 不得被装箱成 int）
            if (expectedDefault != null)
            {
                Assert.Equal(expectedDefault.GetType(), actual.GetType());
            }

            // null 参：params 数组本身为 null，等价于无参
            object?[]? nullArgs = null;
            Assert.Equal(expectedDefault, refer.CreateInstance(nullArgs!));

            // 空参数数组：同样视为无参
            Assert.Equal(expectedDefault, refer.CreateInstance(Array.Empty<object?>()));
        }

        /// <summary>
        /// 断言带参调用一律抛 <see cref="NotSupportedException" />。
        /// </summary>
        /// <param name="refer">待断言的值类型引用</param>
        private static void AssertRejectsArguments(IRefer refer)
        {
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance(1));
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance(1, 2));
            Assert.Throws<NotSupportedException>(() => refer.CreateInstance("1"));
        }

        [Fact]
        public void BooleanRefer_CreateInstance_ReturnsDefaultBoolean()
        {
            AssertCreateInstanceSemantics(BooleanRefer.Instance, false);
            AssertRejectsArguments(BooleanRefer.Instance);
        }

        [Fact]
        public void ByteRefer_CreateInstance_ReturnsDefaultByte()
        {
            AssertCreateInstanceSemantics(ByteRefer.Instance, (byte)0);
            AssertRejectsArguments(ByteRefer.Instance);
        }

        [Fact]
        public void SByteRefer_CreateInstance_ReturnsDefaultSByte()
        {
            AssertCreateInstanceSemantics(SByteRefer.Instance, (sbyte)0);
            AssertRejectsArguments(SByteRefer.Instance);
        }

        [Fact]
        public void CharRefer_CreateInstance_ReturnsDefaultChar()
        {
            AssertCreateInstanceSemantics(CharRefer.Instance, '\0');
            AssertRejectsArguments(CharRefer.Instance);
        }

        [Fact]
        public void Int16Refer_CreateInstance_ReturnsDefaultInt16()
        {
            AssertCreateInstanceSemantics(Int16Refer.Instance, (short)0);
            AssertRejectsArguments(Int16Refer.Instance);
        }

        [Fact]
        public void UInt16Refer_CreateInstance_ReturnsDefaultUInt16()
        {
            AssertCreateInstanceSemantics(UInt16Refer.Instance, (ushort)0);
            AssertRejectsArguments(UInt16Refer.Instance);
        }

        [Fact]
        public void Int32Refer_CreateInstance_ReturnsDefaultInt32()
        {
            AssertCreateInstanceSemantics(Int32Refer.Instance, 0);
            AssertRejectsArguments(Int32Refer.Instance);
        }

        [Fact]
        public void UInt32Refer_CreateInstance_ReturnsDefaultUInt32()
        {
            AssertCreateInstanceSemantics(UInt32Refer.Instance, 0u);
            AssertRejectsArguments(UInt32Refer.Instance);
        }

        [Fact]
        public void Int64Refer_CreateInstance_ReturnsDefaultInt64()
        {
            AssertCreateInstanceSemantics(Int64Refer.Instance, 0L);
            AssertRejectsArguments(Int64Refer.Instance);
        }

        [Fact]
        public void UInt64Refer_CreateInstance_ReturnsDefaultUInt64()
        {
            AssertCreateInstanceSemantics(UInt64Refer.Instance, 0ul);
            AssertRejectsArguments(UInt64Refer.Instance);
        }

        [Fact]
        public void SingleRefer_CreateInstance_ReturnsDefaultSingle()
        {
            AssertCreateInstanceSemantics(SingleRefer.Instance, 0f);
            AssertRejectsArguments(SingleRefer.Instance);
        }

        [Fact]
        public void DoubleRefer_CreateInstance_ReturnsDefaultDouble()
        {
            AssertCreateInstanceSemantics(DoubleRefer.Instance, 0d);
            AssertRejectsArguments(DoubleRefer.Instance);
        }

        [Fact]
        public void DecimalRefer_CreateInstance_ReturnsDefaultDecimal()
        {
            AssertCreateInstanceSemantics(DecimalRefer.Instance, 0m);
            AssertRejectsArguments(DecimalRefer.Instance);
        }

        [Fact]
        public void DateTimeRefer_CreateInstance_ReturnsDefaultDateTime()
        {
            AssertCreateInstanceSemantics(DateTimeRefer.Instance, default(DateTime));
            AssertRejectsArguments(DateTimeRefer.Instance);
        }

        [Fact]
        public void GuidRefer_CreateInstance_ReturnsDefaultGuid()
        {
            AssertCreateInstanceSemantics(GuidRefer.Instance, default(Guid));
            AssertRejectsArguments(GuidRefer.Instance);
        }

        /// <summary>
        /// <see cref="StringRefer" /> 的默认实例为 <see langword="null" />，这是 <c>default(string)</c> 的必然结果。
        /// </summary>
        [Fact]
        public void StringRefer_CreateInstance_ReturnsNull()
        {
            AssertCreateInstanceSemantics(StringRefer.Instance, null);
            AssertRejectsArguments(StringRefer.Instance);
        }
    }
}
