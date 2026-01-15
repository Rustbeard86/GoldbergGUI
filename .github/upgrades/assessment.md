# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [GoldbergGUI.Core\GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj)
  - [GoldbergGUI.WPF\GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 2 | All require upgrade |
| Total NuGet Packages | 10 | All compatible |
| Total Code Files | 17 |  |
| Total Code Files with Incidents | 11 |  |
| Total Lines of Code | 2260 |  |
| Total Number of Issues | 66 |  |
| Estimated LOC to modify | 64+ | at least 2.8% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [GoldbergGUI.Core\GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | net8.0 | 🟢 Low | 0 | 42 | 42+ | Wpf, Sdk Style = True |
| [GoldbergGUI.WPF\GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj) | net8.0-windows | 🟢 Low | 0 | 22 | 22+ | Wpf, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 10 | 100.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 0 | 0.0% |
| ***Total NuGet Packages*** | ***10*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 35 | High - Require code changes |
| 🟡 Source Incompatible | 1 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 28 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 3002 |  |
| ***Total APIs Analyzed*** | ***3066*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| AngleSharp | 0.14.0 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |
| MvvmCross | 7.1.2 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |
| MvvmCross.Platforms.Wpf | 7.1.2 |  | [GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj) | ✅Compatible |
| NinjaNye.SearchExtensions | 3.0.1 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |
| Serilog | 2.10.0 |  | [GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj) | ✅Compatible |
| Serilog.Sinks.Console | 3.1.1 |  | [GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj) | ✅Compatible |
| Serilog.Sinks.File | 4.1.0 |  | [GoldbergGUI.WPF.csproj](#goldbergguiwpfgoldbergguiwpfcsproj) | ✅Compatible |
| SharpCompress | 0.35.0 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |
| sqlite-net-pcl | 1.7.335 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |
| SteamStorefrontAPI | 2.0.1.421 |  | [GoldbergGUI.Core.csproj](#goldbergguicoregoldbergguicorecsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| WPF (Windows Presentation Foundation) | 5 | 7.8% | WPF APIs for building Windows desktop applications with XAML-based UI that are available in .NET on Windows. WPF provides rich desktop UI capabilities with data binding and styling. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>. |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |
| T:System.Uri | 13 | 20.3% | Behavioral Change |
| T:System.Net.Http.HttpContent | 6 | 9.4% | Behavioral Change |
| M:System.Uri.#ctor(System.String,System.UriKind) | 5 | 7.8% | Behavioral Change |
| T:System.Windows.TextDataFormat | 4 | 6.3% | Binary Incompatible |
| T:System.Windows.Clipboard | 3 | 4.7% | Binary Incompatible |
| T:System.Windows.Application | 3 | 4.7% | Binary Incompatible |
| M:System.Windows.Application.LoadComponent(System.Object,System.Uri) | 3 | 4.7% | Binary Incompatible |
| T:System.Windows.Markup.IComponentConnector | 3 | 4.7% | Binary Incompatible |
| M:System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat) | 2 | 3.1% | Binary Incompatible |
| T:System.Text.Json.JsonDocument | 2 | 3.1% | Behavioral Change |
| T:System.Windows.Controls.CheckBox | 2 | 3.1% | Binary Incompatible |
| M:System.Windows.Clipboard.GetText | 1 | 1.6% | Binary Incompatible |
| F:System.Windows.TextDataFormat.Text | 1 | 1.6% | Binary Incompatible |
| F:System.Windows.TextDataFormat.UnicodeText | 1 | 1.6% | Binary Incompatible |
| P:Microsoft.Win32.FileDialog.FileName | 1 | 1.6% | Binary Incompatible |
| M:Microsoft.Win32.CommonDialog.ShowDialog | 1 | 1.6% | Binary Incompatible |
| P:Microsoft.Win32.FileDialog.Title | 1 | 1.6% | Binary Incompatible |
| P:Microsoft.Win32.OpenFileDialog.Multiselect | 1 | 1.6% | Binary Incompatible |
| P:Microsoft.Win32.FileDialog.Filter | 1 | 1.6% | Binary Incompatible |
| T:Microsoft.Win32.OpenFileDialog | 1 | 1.6% | Binary Incompatible |
| M:Microsoft.Win32.OpenFileDialog.#ctor | 1 | 1.6% | Binary Incompatible |
| M:System.Net.Http.HttpContent.ReadAsStreamAsync | 1 | 1.6% | Behavioral Change |
| M:System.Net.WebClient.#ctor | 1 | 1.6% | Source Incompatible |
| T:System.Windows.MessageBox | 1 | 1.6% | Binary Incompatible |
| T:System.Windows.MessageBoxResult | 1 | 1.6% | Binary Incompatible |
| M:System.Windows.MessageBox.Show(System.String) | 1 | 1.6% | Binary Incompatible |
| M:System.Uri.#ctor(System.String) | 1 | 1.6% | Behavioral Change |
| M:System.Windows.Application.Run | 1 | 1.6% | Binary Incompatible |
| P:System.Windows.Application.StartupUri | 1 | 1.6% | Binary Incompatible |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;GoldbergGUI.Core.csproj</b><br/><small>net8.0</small>"]
    P2["<b>📦&nbsp;GoldbergGUI.WPF.csproj</b><br/><small>net8.0-windows</small>"]
    P2 --> P1
    click P1 "#goldbergguicoregoldbergguicorecsproj"
    click P2 "#goldbergguiwpfgoldbergguiwpfcsproj"

```

## Project Details

<a id="goldbergguicoregoldbergguicorecsproj"></a>
### GoldbergGUI.Core\GoldbergGUI.Core.csproj

#### Project Info

- **Current Target Framework:** net8.0
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 0
- **Dependants**: 1
- **Number of Files**: 11
- **Number of Files with Incidents**: 6
- **Lines of Code**: 2164
- **Estimated LOC to modify**: 42+ (at least 1.9% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P2["<b>📦&nbsp;GoldbergGUI.WPF.csproj</b><br/><small>net8.0-windows</small>"]
        click P2 "#goldbergguiwpfgoldbergguiwpfcsproj"
    end
    subgraph current["GoldbergGUI.Core.csproj"]
        MAIN["<b>📦&nbsp;GoldbergGUI.Core.csproj</b><br/><small>net8.0</small>"]
        click MAIN "#goldbergguicoregoldbergguicorecsproj"
    end
    P2 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 22 | High - Require code changes |
| 🟡 Source Incompatible | 1 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 19 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 2878 |  |
| ***Total APIs Analyzed*** | ***2920*** |  |

<a id="goldbergguiwpfgoldbergguiwpfcsproj"></a>
### GoldbergGUI.WPF\GoldbergGUI.WPF.csproj

#### Project Info

- **Current Target Framework:** net8.0-windows
- **Proposed Target Framework:** net10.0-windows
- **SDK-style**: True
- **Project Kind:** Wpf
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 6
- **Number of Files with Incidents**: 5
- **Lines of Code**: 96
- **Estimated LOC to modify**: 22+ (at least 22.9% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["GoldbergGUI.WPF.csproj"]
        MAIN["<b>📦&nbsp;GoldbergGUI.WPF.csproj</b><br/><small>net8.0-windows</small>"]
        click MAIN "#goldbergguiwpfgoldbergguiwpfcsproj"
    end
    subgraph downstream["Dependencies (1"]
        P1["<b>📦&nbsp;GoldbergGUI.Core.csproj</b><br/><small>net8.0</small>"]
        click P1 "#goldbergguicoregoldbergguicorecsproj"
    end
    MAIN --> P1

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 13 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 9 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 124 |  |
| ***Total APIs Analyzed*** | ***146*** |  |

#### Project Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |
| WPF (Windows Presentation Foundation) | 5 | 22.7% | WPF APIs for building Windows desktop applications with XAML-based UI that are available in .NET on Windows. WPF provides rich desktop UI capabilities with data binding and styling. Enable Windows Desktop support: Option 1 (Recommended): Target net9.0-windows; Option 2: Add <UseWindowsDesktop>true</UseWindowsDesktop>. |

