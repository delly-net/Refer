using ReferDemo.Entity;

Console.WriteLine("Hello, World!");

var intArrayTpye = typeof(int[]);
var refer = new NameValue();
Console.WriteLine(intArrayTpye.Name);

// 以下访问由 [UseRefer] 源生成产生的 NameValueRefer：
// 元数据在编译期固化，此处不触发任何运行时反射。
Console.WriteLine($"{NameValueRefer.Instance.Name} @ {NameValueRefer.Instance.Namespace}");
Console.WriteLine($"IsValue={NameValueRefer.Instance.IsValue}, IsArray={NameValueRefer.Instance.IsArray}");

foreach (var property in NameValueRefer.Instance.GetProperties())
{
    Console.WriteLine($"Property: {property.Name} : {property.Refer.Name}");
}

var created = (NameValue)NameValueRefer.Instance.CreateInstance();
created.Name = "generated";
created.Value = "ok";
Console.WriteLine($"Created: {created.Name}={created.Value}");
