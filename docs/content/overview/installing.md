---
lastmod: 2020-01-07
date: 2016-10-16
menu:
  main:
    parent: getting started
title: Installing
weight: 10
---

Installing
==========

The recommended way of installing SingleStoreConnector is through NuGet.
Note that if you are using the `SingleStore.Data` NuGet package, it must be uninstalled first.

### Automatically

If using the new project system, run: `dotnet add package SingleStoreConnector`

Or, in Visual Studio, use the _NuGet Package Manager_ to browse for and install `SingleStoreConnector`.

### Manually

**Step 1:** Add SingleStoreConnector to the dependencies in your `csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyTitle>My Application</AssemblyTitle>
    <Description>A great application</Description>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SingleStoreConnector" Version="0.61.0" />
  </ItemGroup>

</Project>
```

**Step 2:** Run the command `dotnet restore`
