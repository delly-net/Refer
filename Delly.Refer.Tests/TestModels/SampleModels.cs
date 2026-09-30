using System;
using Delly.Refer;

namespace Delly.Refer.Tests.TestModels
{
    /// <summary>
    /// 常量可构造的测试特性，用于验证生成器在编译期把特性使用还原为 <c>new</c> 表达式
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TagAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">标记值</param>
        /// <param name="order">次序</param>
        public TagAttribute(string value, int order = 0)
        {
            Value = value;
            Order = order;
        }

        /// <summary>
        /// 标记值
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 次序（命名参数建模用例）
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// 被 <see cref="Person" /> 引用的模型类型，用于验证「有界展开」的递归生成
    /// </summary>
    /// <remarks>
    /// 本类型未标注 <see cref="UseReferAttribute" />，其 Refer 由 <see cref="Person" /> 的成员引用触发。
    /// 仅声明带参构造函数，故 <c>CreateInstance()</c> 应当抛 <see cref="NotSupportedException" />。
    /// </remarks>
    public sealed class Address
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="city">城市</param>
        public Address(string city)
        {
            City = city;
        }

        /// <summary>
        /// 城市
        /// </summary>
        public string City { get; }
    }

    /// <summary>
    /// 主测试模型：覆盖属性、无参/带参构造函数重载、<c>void</c> 方法、泛型方法与常量特性
    /// </summary>
    [UseRefer]
    [Tag("person", 1, Order = 7)]
    public sealed class Person
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Person()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">姓名</param>
        public Person(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">姓名</param>
        /// <param name="age">年龄</param>
        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        /// <summary>
        /// 姓名
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; }

        /// <summary>
        /// 住址，类型未标注 <see cref="UseReferAttribute" />，用于验证递归展开
        /// </summary>
        public Address? Address { get; set; }

        /// <summary>
        /// 非 void 返回方法
        /// </summary>
        /// <returns>是否成年</returns>
        public bool IsAdult()
        {
            return Age >= 18;
        }

        /// <summary>
        /// void 返回方法
        /// </summary>
        /// <param name="name">新姓名</param>
        public void Rename(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 泛型方法：零反射约束下无法闭合类型实参，<c>Invoke</c> 抛 <see cref="NotSupportedException" />
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <returns>恒为 <see langword="false" />，仅用于承载泛型元数据</returns>
        public bool IsTypeOf<T>()
        {
            return typeof(T) == typeof(Person);
        }
    }

    /// <summary>
    /// 嵌套类型展开用例：<c>Line</c> 的 Refer 生成为 <c>Order_LineRefer</c>
    /// </summary>
    [UseRefer]
    public sealed class Order
    {
        /// <summary>
        /// 订单行
        /// </summary>
        public sealed class Line
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="quantity">数量</param>
            public Line(int quantity)
            {
                Quantity = quantity;
            }

            /// <summary>
            /// 数量
            /// </summary>
            public int Quantity { get; }
        }

        /// <summary>
        /// 首个订单行
        /// </summary>
        public Line? First { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string? Code { get; set; }
    }

    /// <summary>
    /// 枚举模型：<c>IsValue</c> 为 <see langword="true" />，无参 <c>CreateInstance</c> 返回 <c>default</c>
    /// </summary>
    [UseRefer]
    public enum PersonKind
    {
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 学生
        /// </summary>
        Student = 1,

        /// <summary>
        /// 教师
        /// </summary>
        Teacher = 2,
    }

    /// <summary>
    /// 结构体模型：零参走 <c>default</c>，带参走已声明的构造函数
    /// </summary>
    [UseRefer]
    public struct Point
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">横坐标</param>
        /// <param name="y">纵坐标</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 横坐标
        /// </summary>
        public int X { get; }

        /// <summary>
        /// 纵坐标
        /// </summary>
        public int Y { get; }
    }
}
