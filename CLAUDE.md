# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概览

Delly.Refer 是专用于**源生成（Source Generator）支持**的基础组件库。它通过 `IRefer` 接口体系对 CLR 类型做统一抽象，使上层框架能以一致接口访问类型元数据（名称、命名空间、泛型信息、方法、属性、特性）并创建实例。

仓库以 NuGet 包形式对外分发，同时提供运行时抽象与编译期生成器两个包。

## 构建与打包

```bash
dotnet build Delly.Refer.slnx                     # 构建整个解决方案
dotnet build Delly.Refer.Generator/Delly.Refer.Generator.csproj   # 生成器：构建即产包（GeneratePackageOnBuild=True）
dotnet pack  Delly.Refer/Delly.Refer.csproj       # 核心库需显式打包
```

本仓库**没有测试项目**，也没有配置 lint 或 CI。验证改动的方式是构建成功 + 打包产物正确。

解决方案使用 `.slnx`（XML 格式），需要较新的 .NET SDK / Visual Studio 支持。

## 架构

两个项目职责分离：

**`Delly.Refer/`** — 运行时核心抽象层，多目标 `netstandard2.0;net6.0`，无任何第三方依赖（仅 BCL）。导出接口：

- `IRefer` — 类型引用主接口。承载名称/命名空间、泛型元信息（`IsGeneric` / `IsGenericDefinition` / `GenericDefinitionCount`）、`CreateInstance` 工厂方法，以及 `GetMethods()` / `GetProperties()` / `GetAttributes()` 三个元数据查询入口。
- `IPropertyRefer` — 属性引用，持有 `Name` 与其自身的 `IRefer`（支持属性类型递归描述）。
- `IMethodRefer` — 方法引用，目前为**空接口占位**。

**`Delly.Refer.Generator/`** — Roslyn 源生成器（`netstandard2.0` + `IsRoslynComponent=true`），意图是在编译期为用户的模型类型生成 `IRefer` 实现。**当前仅有 `Class1.cs` 空占位类，尚未实现**。

### 核心设计意图

`IRefer` 的元数据由源生成阶段固化而非运行时反射：`IRefer.cs:32` 的注释明确写道「模型类型信息，源生成阶段使用 `typeof(T)` 赋值」。因此接口上的泛型判定（`IsGeneric` / `IsGenericDefinition` / `GenericDefinitionCount`）应当由生成器在编译期静态计算并输出为常量属性，而非依赖 `Type` 的运行时查询。修改这些成员时需保持这一前提。

## 约定与易错点

- **多目标框架限制**：核心库同时编译 `netstandard2.0` 与 `net6.0`，不得使用 net6 独有 API（如 `ArgumentNullException.ThrowIfNull`、`DateOnly`、`Random.Shared`），除非用 `#if NET6_0_OR_GREATER` 条件编译。
- **XML 注释**：核心库 `Delly.Refer` 开启 `GenerateDocumentationFile=True`，新增 public 成员须补 XML 注释（生成器项目未开启此项）。注释与文档统一使用中文。
- **打包伴随文件**：`README.md` 与 `ref_256.png` 需随包发布到根目录（由 `PackageReadmeFile` / `PackageIcon` 引用）。重命名或移动这些文件必须同步修改 csproj 与 `PackageReadmeFile` / `PackageIcon` 属性。
- **多目标打包陷阱**：核心库是多目标（`<TargetFrameworks>`）项目，`Pack` 目标在外层构建执行，此时默认 `None` 通配项为空，`<None Update=...>` 携带的 `Pack` / `PackagePath` 元数据**不生效**，随包文件会全部丢失并报 `NU5046`。因此随包发布的文件必须写成 `<None Remove="..." />` + `<None Include="..." Pack="True" PackagePath="\" />`。自查：`dotnet msbuild <csproj> -getItem:None` 返回 `[]` 即踩中此坑（单目标项目不受影响，生成器项目沿用 `Update` 可正常工作）。
- **图标三处副本**：`Icon/ref_256.png`、`Delly.Refer/ref_256.png`、`Delly.Refer.Generator/ref_256.png` 内容相同，更换图标需三处同步。
- **版本号需同步**：两个 csproj 的 `<Version>` 目前均为 `1.0.2609.1`，发版时须一并更新，保持两个包版本一致。

## 已知待办

1. `IRefer.TypeCode` 的属性类型存疑：声明为 `TypeCode`（`using System;` 下解析为 `System.TypeCode` 枚举），但注释称由 `typeof(T)` 赋值——后者返回 `System.Type`，类型不匹配。实现生成器前需先澄清是改为 `Type` 还是新增自定义 `TypeCode` 类型。
2. 生成器项目尚未声明 `ProjectReference` 引用核心库，也未显式引用 Roslyn 分析器包（`Microsoft.CodeAnalysis.CSharp`）。
3. `IMethodRefer` 为空接口，待补充成员。
