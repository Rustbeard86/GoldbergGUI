# GoldbergGUI .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the GoldbergGUI solution upgrade from .NET 8.0 to .NET 10.0. Both projects will be upgraded simultaneously in a single atomic operation, followed by comprehensive build and code-level validation.

**Progress**: 1/2 tasks complete (50%) ![0%](https://progress-bar.xyz/50)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-01-15 07:49)*
**References**: Plan §Project-by-Project Plans §1. Prerequisites

- [✓] (1) Verify that the .NET 10.0 SDK is installed as required by the plan.
- [✓] (2) Required .NET SDK version is installed and available (**Verify**).

---

### [▶] TASK-002: Atomic upgrade to .NET 10.0 and fix compatibility issues
**References**: Plan §Project-by-Project Plans, Plan §Source Control Strategy

- [✓] (1) Update the `TargetFramework` property in `GoldbergGUI.Core.csproj` and `GoldbergGUI.WPF.csproj` to `net10.0-windows`.
- [✓] (2) Both project files are updated to the target framework (**Verify**).
- [✓] (3) Restore dependencies for the solution.
- [✓] (4) Dependencies are restored successfully (**Verify**).
- [✓] (5) Build the solution and fix all compilation errors, addressing the API compatibility issues detailed in Plan §Project-by-Project Plans (including `WebClient` to `HttpClient`, WPF APIs, `Uri`, and `HttpContent` changes).
- [✓] (6) Solution builds with 0 errors and 0 warnings (**Verify**).
- [▶] (7) Commit all changes with a single atomic commit message per Plan §Source Control Strategy.

---









