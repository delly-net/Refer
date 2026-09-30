# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概览

Delly.Refer 是专用于**源生成（Source Generator）支持**的基础组件库。它通过 `IRefer` 接口体系对 CLR 类型做统一抽象，使上层框架能以一致接口访问类型元数据（名称、命名空间、泛型信息、方法、属性、特性）并创建实例。

使用方式：在模型类型上标注 `[UseRefer]`，编译期由 `Delly.Refer.Generator` 生成同命名空间的 `{类型名}Refer` 类型（含 `Instance` 单例），其全部元数据在生成阶段固化，运行时**零反射**。

仓库以 NuGet 包形式对外分发，同时提供运行时抽象与编译期生成器两个包。

## 构建与打包

```bash
dotnet build Delly.Refer.slnx                     # 构建整个解决方案
dotnet test  Delly.Refer.Tests/Delly.Refer.Tests.csproj   # 运行单元测试（xUnit）
dotnet build Delly.Refer.Generator/Delly.Refer.Generator.csproj   # 生成器：构建即产包（GeneratePackageOnBuild=True）
dotnet pack  Delly.Refer/Delly.Refer.csproj       # 核心库需显式打包
```

本仓库配置了单元测试项目 `Delly.Refer.Tests/`（xUnit，单目标 `net6.0`，`IsPackable=false`），未配置 lint 或 CI。验证改动的方式是构建成功 + 测试通过 + 打包产物正确。

改动生成器时建议额外用 `dotnet build Delly.Refer.Tests/Delly.Refer.Tests.csproj -p:EmitCompilerGeneratedFiles=true` 查看产物落盘（位于 `Delly.Refer.Tests/obj/Debug/net6.0/generated/`），便于肉眼核对生成源码。

解决方案使用 `.slnx`（XML 格式），需要较新的 .NET SDK / Visual Studio 支持。

## 架构

三个项目职责分离：

**`Delly.Refer/`** — 运行时核心抽象层，多目标 `netstandard2.0;net6.0`，无任何第三方依赖（仅 BCL）。导出接口：

- `IRefer` — 类型引用主接口。承载名称/命名空间、类型标记（`IsValue` / `IsArray`）、泛型元信息（`IsGeneric` / `IsGenericDefinition` / `GenericDefinitionCount`）、`TypeCode`、`CreateInstance` 工厂方法，以及 `GetMethods()` / `GetGenericRefers()` / `GetProperties()` / `GetAttributes()` 四个元数据查询入口。注意 `GetMethods()` 返回 `IReadOnlyList<IRefer>`，`GetGenericRefers()` 返回 `IReadOnlyList<IMethodRefer>`，两者不可互换。
- `IPropertyRefer` — 属性引用，持有 `Name` 与其自身的 `IRefer`（支持属性类型递归描述）。
- `IMethodRefer` — 方法引用，承载 `Name` / `ReturnRefer` / `GetParameters()` / `Invoke()`；参数由 `IMethodReferParameter`（`Name` + `ParameterRefer`）描述。注意 `IMethodRefer` **不继承 `IRefer`**，其 `Name` 与 `IRefer.Name` 是两个无关联的成员。
- `UseReferAttribute` — 标记特性（`Delly.Refer/UseReferAttribute.cs`），仅作生成器指令使用。目标限定 `Class | Struct | Interface | Enum`，`AllowMultiple = false`、`Inherited = false`，无构造函数参数。

`Delly.Refer/BasicRefers/` 下是 `IRefer` 的基础实现，分两族、共 34 个文件，均以「泛型抽象基类 + 每类型一个 `sealed` 子类 + `public static readonly XxxRefer Instance` 单例」组织，基类集中实现公共成员、子类只承载差异化部分（`TypeCode` 与/或 `CreateInstance` override，决策 #315）：

- **值类型族**（16 个）：`BooleanRefer`、`Int32Refer`、`StringRefer`、`GuidRefer` 等，均继承 `ValueReferBase<T>`。`IsValue` 为 `true`（`StringRefer` 同）、`IsArray` 为 `false`、非泛型；`CreateInstance` 无参或 `null` 参返回 `default(T)`，带参抛 `NotSupportedException`（值类型无构造语义）。
- **数组族**（16 个）：`BooleanArrayRefer` … `GuidArrayRefer`，均继承 `ArrayReferBase<TElement>`，建模 `TElement[]`。`IsArray` 为 `true`、`IsValue` 为 `false`、`TypeCode` 统一为 `TypeCode.Object`（`System.TypeCode` 无数组专用成员，与 `GuidRefer` 的取舍一致）、非泛型；`CreateInstance` 语义：无参或 `null` 参返回长度 0 的空数组，单个 `int` 参数返回该长度的数组（负数抛 `ArgumentOutOfRangeException`、非 `int` 抛 `NotSupportedException`），参数个数为 2 及以上抛 `NotSupportedException`。

两族的元数据查询方法均恒返回空集合（手工 `Array.Empty<T>()`，基类复用同一实例，**永不使用运行时反射**）。

**`Delly.Refer.Generator/`** — Roslyn 增量源生成器（`netstandard2.0` + `IsRoslynComponent=true` + `LangVersion=latest` + `Nullable=enable`），在编译期为标注 `[UseRefer]` 的模型类型生成 `IRefer` 实现。入口为 `ReferGenerator`（`IIncrementalGenerator`），流程为 `ForAttributeWithMetadataName("Delly.Refer.UseReferAttribute", …)` → `Collect()` → 在 `RegisterSourceOutput` 中广度优先展开。各文件职责：

- `ReferConstants.cs` — 特性元数据全名、生成类/成员后缀、`Instance` 字段名、支撑类型命名空间 `Delly.Refer.Generated`。
- `ReferDiagnostics.cs` — `DRF0001`（成员类型不在有界展开范围，警告）、`DRF0002`（特性无法编译期构造，警告）。
- `Models/ReferModels.cs` — 建模用记录类型（`ReferTypeModel` / `ReferPropertyModel` / `ReferMethodModel` / `ReferParameterModel` / `ReferConstructorModel`）。
- `TypeReferResolver.cs` — 类型符号 → Refer 表达式字符串（有界展开的判定中枢）。
- `ReferNaming.cs` — 生成类命名（顶层 `{类型名}Refer`，嵌套 `{外层}_{内层}Refer`）。
- `AttributeExpressionBuilder.cs` — 把特性使用还原为可编译的 `new XxxAttribute(...) { Prop = val }` 常量表达式。
- `ModelFactory.cs` — `INamedTypeSymbol` → `ReferTypeModel`，含成员筛选、唯一命名与诊断收集。
- `CodeWriter.cs` — 带缩进的源码文本写入器。
- `ReferEmitter.cs` — `ReferTypeModel` → C# 源码文本（含 `Delly.Refer.Generated` 支撑类型发射）。
- `ReferGenerator.cs` — 增量生成管道入口与 `AddSource` 调度。

**生成产物形态**：与原模型**同命名空间**、类名 `{类型名}Refer`、`public static readonly {类型名}Refer Instance` 单例、私有构造函数，全部元数据固化为常量属性与 `static readonly` 只读集合；`CreateInstance` 按 `args.Length` 分派到编译期枚举出的构造函数，实参写作 `({参数类型})(object)args[i]`。属性/方法引用以 **嵌套 `sealed` 类**（`{成员名}PropertyRefer` / `{成员名}MethodRefer`）生成，重名时追加从 `2` 开始的序号。方法引用类**同时实现 `IRefer` 与 `IMethodRefer`**，故 `GetMethods()` 返回全部方法、`GetGenericRefers()` 返回其中泛型方法子集，二者返回同一批实例、仅静态类型不同。

**`Delly.Refer.Tests/`** — xUnit 单元测试（单目标 `net6.0`，`IsPackable=false`）。既有的 BasicRefers 契约测试见 `CreateInstanceTests` / `BasicRefersContractTests` / `ArrayReferContractTests`；源生成器测试分两层：

- `TestModels/SampleModels.cs` — 测试项目自身带 `[UseRefer]` 的模型，使测试项目成为生成器的真实宿主。
- `GeneratedReferTests.cs` — 断言生成产物的**运行时行为**（元数据常量、`CreateInstance` 重载分派与异常、成员集合、`Invoke` 语义、递归展开、嵌套类型、值类型/枚举语义）。
- `ReferGeneratorTests.cs` — 以 `CSharpGeneratorDriver` 驱动生成器编译源码片段，断言**产物形态**：hint 名、递归展开、`BasicRefers` 复用、`DRF0001` / `DRF0002` 告警、`UseReferAttribute` 不进入 `GetAttributes()`、无标记类型时不产出源码、以及「零反射」契约（生成源码不含 `System.Reflection` / `Activator` / `typeof(` / `#nullable`）。

### 核心设计意图

`IRefer` 的元数据由源生成阶段固化而非运行时反射——`ValueReferBase<T>` / `ArrayReferBase<TElement>` 的类注释均明确「元数据由源生成阶段固化，本基类不进行任何运行时反射」，基础实现也一律以手工空集合与常量属性体现。因此接口上的泛型判定（`IsGeneric` / `IsGenericDefinition` / `GenericDefinitionCount`）与类型标记（`IsValue` / `IsArray`）应当由生成器在编译期静态计算并输出为常量属性，而非依赖 `Type` 的运行时查询。修改这些成员时需保持这一前提。

## 约定与易错点

- **多目标框架限制**：核心库同时编译 `netstandard2.0` 与 `net6.0`，不得使用 net6 独有 API（如 `ArgumentNullException.ThrowIfNull`、`DateOnly`、`Random.Shared`），除非用 `#if NET6_0_OR_GREATER` 条件编译。
- **语言版本差异**：`netstandard2.0` 目标按 **C# 7.3** 编译，而 `Nullable` 仅在非 netstandard2.0 目标开启。故 null 宽容运算符 `!`（C# 8 特性）与可空引用类型注解在核心库中**必须**用 `#if NET6_0_OR_GREATER` 包裹，否则 netstandard2.0 报 `CS8370`。`StringRefer.CreateInstance` 即按此写法。
- **XML 注释**：核心库 `Delly.Refer` 开启 `GenerateDocumentationFile=True`，新增 public 成员须补 XML 注释（生成器项目未开启此项）。注释与文档统一使用中文。
- **打包伴随文件**：`README.md` 与 `ref_256.png` 需随包发布到根目录（由 `PackageReadmeFile` / `PackageIcon` 引用）。重命名或移动这些文件必须同步修改 csproj 与 `PackageReadmeFile` / `PackageIcon` 属性。
- **多目标打包陷阱**：核心库是多目标（`<TargetFrameworks>`）项目，`Pack` 目标在外层构建执行，此时默认 `None` 通配项为空，`<None Update=...>` 携带的 `Pack` / `PackagePath` 元数据**不生效**，随包文件会全部丢失并报 `NU5046`。因此随包发布的文件必须写成 `<None Remove="..." />` + `<None Include="..." Pack="True" PackagePath="\" />`。自查：`dotnet msbuild <csproj> -getItem:None` 返回 `[]` 即踩中此坑（单目标项目不受影响，生成器项目沿用 `Update` 可正常工作）。
- **图标三处副本**：`Icon/ref_256.png`、`Delly.Refer/ref_256.png`、`Delly.Refer.Generator/ref_256.png` 内容相同，更换图标需三处同步。
- **版本号需同步**：两个 csproj 的 `<Version>` 目前均为 `1.0.2609.1`，发版时须一并更新，保持两个包版本一致。

### 源生成器专项约定

- **生成器刻意不声明对核心库的 `ProjectReference`**：分析器在编译期由 Roslyn 加载，若生成器程序集依赖 `Delly.Refer.dll`，一旦该 DLL 未随分析器一同部署就会加载失败。故 `UseReferAttribute` 以**元数据全名字符串** `"Delly.Refer.UseReferAttribute"` 匹配（`ForAttributeWithMetadataName` 正是按元数据名查找），保证分析器单 DLL 自包含。新增任何需要感知核心库类型的逻辑时，都应以字符串/元数据名表达，而非引入类型引用。
- **`Microsoft.CodeAnalysis.CSharp` 版本必须与测试项目一致**：生成器与 `Delly.Refer.Tests`（驱动式测试）均锁定 `4.12.0`。版本错配会导致 `CSharpGeneratorDriver` 加载生成器时类型不匹配。
- **测试项目对生成器有两条引用**：`OutputItemType="Analyzer"` 的 `ProjectReference`（让测试项目成为真实宿主）+ 指向 `bin\$(Configuration)\netstandard2.0\Delly.Refer.Generator.dll` 的普通 `<Reference>`（供驱动式测试在代码中构造 `ReferGenerator`）。二者缺一不可——`ProjectReference` 的 `OutputItemType` 只能取单一值，无法同时充当二者。替换掉其中任一条前先确认这两种测试仍能编译。
- **`DRF` 诊断文案用中文全角句号**：`RS1033`（诊断说明须以标点结尾）只认半角 `.`，识别不了 `。`，故已在生成器 csproj 的 `NoWarn` 中连同 `NU5128`（分析器包无 `lib/` 产物）、`RS2008`（未维护 `AnalyzerReleases.*.md` 发布跟踪文件）一并抑制。修改 `NoWarn` 前先确认这三条警告的成因是否仍然成立。
- **分析器随包路径须显式声明**：`IsRoslynComponent=true` 不会自动把 DLL 放进 `analyzers/dotnet/cs`，缺 `<None Include="$(OutputPath)\$(AssemblyName).dll" Pack="True" PackagePath="analyzers/dotnet/cs" />` 时 nupkg 无法作为分析器加载；此时还需 `IncludeBuildOutput=false`（避免投放空的 `lib/`）。
- **生成代码必须同时兼容 C# 7.3 与可空/非可空上下文**：生成文件以 `// <auto-generated />` + `#pragma warning disable` 开头，**不输出任何 `#nullable` 指令**；所有框架类型一律 `global::` 全限定；实参统一写作 `({参数类型})(object)args[i]` 以规避 CS8600/CS8604 且不使用 `!` 运算符。
- **`#if NET6_0_OR_GREATER` 双分支必须成对输出**：`CreateInstance` 与 `Invoke` 均按 `object?[]` / `object[]` 两套签名条件编译，少写任一半支会导致使用者在某个 TFM 下编译失败（与 BasicRefers 的既有踩坑一致）。
- **`params` 判空必须写 `args == null || args.Length == 0`**：只判 `Length` 会在 `CreateInstance(null)`（`null` 绑定到数组本身）时抛 NRE。
- **`Name` 语义与 `typeof(T).Name` 对齐**：嵌套类型的 `Name` 是内层类型名（如 `Line`），**不是**生成类名（`Order_LineRefer`）。
- **生成器指令自身不进入元数据**：`UseReferAttribute` 会被 `GetAttributes()` 跳过——它是生成器指令，不是模型元数据。

### 类型 → Refer 的「有界展开」边界

`TypeReferResolver` 只解析下列类型，其余一律跳过该成员并发出 `DRF0001`：

| 类型分类 | 解析结果 |
|---|---|
| 16 个基础值类型（`bool`/`byte`/`sbyte`/`char`/`short`/`ushort`/`int`/`uint`/`long`/`ulong`/`float`/`double`/`decimal`/`DateTime`/`string`/`Guid`） | `Delly.Refer.BasicRefers.{Xxx}Refer.Instance` |
| 上述 16 类的一维数组 | `Delly.Refer.BasicRefers.{Xxx}ArrayRefer.Instance` |
| 源码声明的非泛型类型（含 `enum` / `struct` / `interface` / 嵌套类型） | 同命名空间的 `{类型名}Refer.Instance`，并**递归展开**为它生成 Refer |
| 构造泛型、开放泛型、泛型类型参数、多维/锯齿数组、指针、函数指针、`dynamic`、委托、`ref`/`out` 参数、`ref` 返回、无源码位置的框架类型 | 跳过该成员 + `DRF0001` |

递归展开以 `SymbolEqualityComparer.Default` 维护访问集，跨根去重与循环引用终止由此天然保证，无需额外诊断。数组族的元素类型、编译器符（`delegate`）与 `Nullable<T>` 均不单独建模。

特性建模另有一条边界：仅当构造函数参数与命名参数**均**可由编译期常量、`typeof`、常量枚举或一维常量数组表达时才会生成 `new XxxAttribute(...)` 表达式，否则跳过并发出 `DRF0002`（`float`/`double` 的 `NaN` / `±Infinity` 有专门字面量处理）。

## 已知待办

1. `IRefer.TypeCode` 的属性类型已澄清为 `System.TypeCode` 枚举——接口注释已由「模型类型信息，源生成阶段使用 `typeof(T)` 赋值」改为「类型编码枚举」，不再与 `typeof(T)`（`System.Type`）冲突。遗留问题是该枚举无法表达自定义模型类型与数组类型（`typeof(int[]).GetTypeCode()` 亦为 `TypeCode.Object`）。生成器已按**非破坏性**取舍统一取 `TypeCode.Object`（与 `GuidRefer`、数组族一致）；若要改为 `Type` 或新增自定义类型编码类型，属破坏性变更，需独立评估。
2. 生成器已显式引用 `Microsoft.CodeAnalysis.CSharp` 4.12.0，且**刻意不引用核心库**（理由见「源生成器专项约定」）——原「生成器缺少依赖声明」待办**已按有意取舍关闭**，不要再照搬该待办新增 `ProjectReference`。
3. `ValueReferBase<T>` / `ArrayReferBase<TElement>` 的 `GetMethods()` 与 `GetGenericRefers()` 仍恒返回空集合——基础值类型与基础数组不建模方法与泛型引用。生成器侧已对用户模型真实建模，基础类型是否跟进属独立取舍。
4. `StringRefer.CreateInstance()` 返回 `default(string)`（即 `null`），与 `IRefer.CreateInstance` 在 `net6.0` 下的非空返回注解不符，现以 null 宽容运算符局部规避。若需改为「非 null 空串」语义，或把返回类型改为 `object?`，均属破坏性变更，需独立评估。
5. 数组族仅覆盖 16 个基础类型的一维数组，且 `ArrayReferBase<TElement>` 未建模元素类型（无 `ElementRefer` 属性，元素类型只能从 `Name` 反推）。生成器的有界展开同样止步于此——多维/锯齿数组、自定义元素类型数组与元素引用建模需一并扩展（注意扩展时核心库与生成器两侧必须同步）。
6. **泛型方法仅建模元数据，`Invoke` 抛 `NotSupportedException`**：零反射约束下无法在调用时闭合类型实参（如 `IsTypeOf<T>()` 的 `T`）。这是既定限制而非缺陷，`Invoke` 的异常消息已说明原因；后续若要支持，需引入显式的类型实参闭合协议，属独立设计。
7. **泛型类型本身不被展开**：`[UseRefer]` 标在泛型类型上时，其 Refer 会生成 `IsGeneric` / `IsGenericDefinition` / `GenericDefinitionCount` 元数据，但 `IsConstructible` 为 `false`（`CreateInstance` 一律抛异常），`{类型名}Refer` 也不是泛型类、不含类型参数。开放泛型定义（`List<>`）作为成员类型同样被跳过并发 `DRF0001`。
8. **成员级特性未建模**：`GetAttributes()` 目前只覆盖**类型自身**声明的特性；属性/方法引用上的 `GetAttributes()` 恒返回空集合。同理 `GetProperties()` / `GetMethods()` 只覆盖 `public` 实例成员，不含 `static`、索引器、非 `public` getter 的属性，以及属性访问器、运算符等非 `Ordinary` 方法。
