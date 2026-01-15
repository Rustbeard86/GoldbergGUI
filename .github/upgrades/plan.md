# .NET 10.0 Upgrade Plan

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Migration Strategy](#migration-strategy)
3. [Detailed Dependency Analysis](#detailed-dependency-analysis)
4. [Project-by-Project Plans](#project-by-project-plans)
5. [Risk Management](#risk-management)
6. [Testing & Validation Strategy](#testing--validation-strategy)
7. [Complexity & Effort Assessment](#complexity--effort-assessment)
8. [Source Control Strategy](#source-control-strategy)
9. [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description
Upgrade the GoldbergGUI solution from .NET 8.0 to **.NET 10.0 (Long Term Support)**. This modernization ensures the application benefits from the latest performance improvements, security updates, and language features while maintaining long-term support through 2031.

### Scope
- **Projects Affected**: 2 projects
  - `GoldbergGUI.Core` (net8.0 ? net10.0-windows)
  - `GoldbergGUI.WPF` (net8.0-windows ? net10.0-windows)
- **Current State**: Both projects targeting .NET 8.0 with WPF support
- **Codebase Size**: 2,260 total lines of code across 17 files
- **API Impact**: 66 API-related issues (35 binary incompatible, 1 source incompatible, 28 behavioral changes)

### Selected Strategy
**All-At-Once Strategy** - All projects upgraded simultaneously in a single atomic operation.

**Rationale**: 
- Small solution (2 projects only)
- Simple linear dependency chain (Core ? WPF)
- All NuGet packages (10 total) are compatible with .NET 10.0 - no package updates required
- No security vulnerabilities detected
- Homogeneous codebase (both WPF projects)
- Low complexity enables coordinated upgrade without intermediate states

### Complexity Assessment
**Classification: Simple**

**Discovered Metrics**:
- Projects: 2
- Dependency Depth: 1 (single layer)
- Risk Indicators: None (no security issues, all packages compatible)
- Estimated Code Changes: 64+ LOC (2.8% of codebase)
- Total API Issues: 66 (manageable scope)

### Critical Issues
- ? **No security vulnerabilities** in NuGet packages
- ?? **35 binary incompatible APIs** requiring code changes
- ?? **28 behavioral changes** requiring runtime validation
- ? **All packages compatible** - no package version updates needed

### Recommended Approach
Single-phase atomic upgrade with all projects updated simultaneously. The simple structure and absence of package conflicts make this the most efficient path forward.

### Iteration Strategy
This plan uses a **fast batch approach**:
- Phase 1-2: Foundation (dependency analysis, strategy, project stubs)
- Phase 3: Consolidated detail generation (both projects in 1-2 iterations)
- Expected total iterations: 5-6

## Migration Strategy

### Approach Selection: All-At-Once Strategy

**Selected Approach**: Upgrade all projects simultaneously in a single coordinated operation.

**Justification**:

**Solution Characteristics Favoring All-At-Once**:
- ? Small solution (2 projects < 30 project threshold)
- ? Both projects currently on .NET 8.0 (homogeneous starting point)
- ? Simple dependency structure (single linear chain)
- ? Low external dependency complexity (all 10 packages compatible)
- ? All packages have compatible versions - no package updates required
- ? No security vulnerabilities requiring staged remediation
- ? Manageable code changes (64+ LOC across 11 files)

**All-At-Once Strategy Rationale**:
1. **Speed**: Fastest path to completion - single operation vs multi-phase approach
2. **Simplicity**: No multi-targeting complexity or intermediate states to manage
3. **Consistency**: Both projects benefit from .NET 10.0 simultaneously
4. **Lower Coordination Overhead**: No need to coordinate gradual rollout across teams
5. **Clean Testing**: Single comprehensive test phase after unified upgrade

**Strategy-Specific Considerations**:
- **Atomic Operation**: All TargetFramework properties updated together
- **Single Build Validation**: One build pass identifies all compilation issues
- **Unified Testing**: All test projects run against fully upgraded solution
- **No Intermediate States**: Solution never exists in partially-upgraded state
- **Single Commit**: Entire upgrade can be captured in one logical commit

### Dependency-Based Ordering

While all projects are upgraded simultaneously, the **build order** naturally respects dependencies:

**Build Sequence** (automatic via MSBuild):
1. `GoldbergGUI.Core` - No project dependencies, builds first
2. `GoldbergGUI.WPF` - Depends on Core, builds second

This natural ordering ensures:
- Core APIs are available when WPF builds
- Type references resolve correctly
- IntelliSense works during development

### Execution Approach

**Single Atomic Upgrade Phase**:
1. Update both project files (TargetFramework properties)
2. Restore dependencies (no package version changes needed)
3. Build entire solution to identify all compilation errors
4. Fix all API compatibility issues in one pass
5. Rebuild to verify fixes
6. Execute all tests

**Why This Works**:
- No package updates means no version conflict resolution
- API issues are localized (64+ LOC in 11 files)
- Both projects have similar WPF patterns
- Small codebase enables comprehensive fix-and-validate cycle

### Risk Management Alignment

The All-At-Once approach is appropriate here because:
- **Low Risk Profile**: No high-risk indicators in assessment
- **Contained Scope**: Limited API surface area (66 issues across 2 projects)
- **Proven Patterns**: WPF upgrades follow well-documented patterns
- **Rollback Ready**: Single commit makes rollback trivial if needed

## Detailed Dependency Analysis

### Dependency Graph Summary

The solution has a simple, linear dependency structure with two projects:

```
GoldbergGUI.WPF (Application)
    ??? GoldbergGUI.Core (Library)
```

**Dependency Characteristics**:
- **Total Projects**: 2
- **Maximum Dependency Depth**: 1
- **Circular Dependencies**: None
- **External Dependencies**: 10 NuGet packages (all compatible)

### Project Groupings by Migration Phase

Since we're using the **All-At-Once Strategy**, all projects are upgraded in a single atomic operation:

**Single Atomic Phase: All Projects Simultaneously**
- `GoldbergGUI.Core\GoldbergGUI.Core.csproj`
- `GoldbergGUI.WPF\GoldbergGUI.WPF.csproj`

Both projects will have their TargetFramework properties updated together, eliminating any intermediate incompatible states.

### Critical Path Identification

**Primary Dependency Flow**: Core ? WPF
- `GoldbergGUI.Core`: Foundation library (0 project dependencies)
- `GoldbergGUI.WPF`: Main WPF application (depends on Core)

**Critical Path**: While WPF depends on Core, the All-At-Once approach means both are upgraded simultaneously. The build order will naturally respect the dependency (Core builds before WPF), but both use the new target framework from the start.

### Migration Order Justification

**Why All-At-Once Works Here**:
1. **Small Scale**: Only 2 projects make coordination trivial
2. **No Package Updates**: All 10 packages are already compatible - no version conflicts to resolve
3. **Single Dependency**: Simple linear structure has no complex interdependencies
4. **Homogeneous Stack**: Both are WPF projects with similar upgrade patterns
5. **Low Risk**: No security vulnerabilities or breaking package changes

The atomic upgrade eliminates the need for:
- Multi-targeting support
- Intermediate build states
- Complex dependency resolution phases
- Staged rollout coordination

## Project-by-Project Plans

### Project: GoldbergGUI.Core

**Current State**: 
- Target Framework: net8.0
- Project Type: Class Library (WPF support)
- Dependencies: 6 NuGet packages
- Lines of Code: 2,164
- Files with Issues: 6 out of 11
- API Issues: 42 total (22 binary incompatible, 1 source incompatible, 19 behavioral changes)

**Target State**: 
- Target Framework: net10.0-windows
- Same dependency set (all compatible)

#### Migration Steps

**1. Prerequisites**
- ? .NET 10.0 SDK installed
- ? On `upgrade-to-NET10` branch
- ? All dependencies available (no package updates needed)

**2. Framework Update**

Update project file `GoldbergGUI.Core\GoldbergGUI.Core.csproj`:

```xml
<!-- Change from: -->
<TargetFramework>net8.0</TargetFramework>

<!-- To: -->
<TargetFramework>net10.0-windows</TargetFramework>
```

**Note**: `-windows` suffix required because project uses WPF APIs.

**3. Package/Dependency Updates**

**No package updates required** - all 6 packages are compatible with .NET 10.0:

| Package | Current Version | Status |
|---------|----------------|--------|
| AngleSharp | 0.14.0 | ? Compatible |
| MvvmCross | 7.1.2 | ? Compatible |
| NinjaNye.SearchExtensions | 3.0.1 | ? Compatible |
| SharpCompress | 0.35.0 | ? Compatible |
| sqlite-net-pcl | 1.7.335 | ? Compatible |
| SteamStorefrontAPI | 2.0.1.421 | ? Compatible |

**4. Expected Breaking Changes**

Based on assessment data, expect the following categories of issues:

**Binary Incompatible APIs (22 instances)**:
- **System.Net.WebClient** usage (1 instance)
  - Location: Likely in HTTP client code
  - Issue: Type is obsolete/changed in .NET 10.0
  - Recommended: Migrate to `HttpClient`

**Behavioral Changes (19 instances)**:
- **System.Uri** construction (13 instances)
  - Stricter validation in .NET 10.0
  - May throw exceptions for previously-accepted invalid URIs
  - Review all `new Uri(...)` calls

- **System.Net.Http.HttpContent** (6 instances)
  - `ReadAsStreamAsync` behavior changes
  - May need to adjust async patterns

**5. Code Modifications**

**Priority 1: Source Incompatible Issues**
- **System.Net.WebClient Constructor**
  - File: Look for HTTP client usage
  - Change: Replace with HttpClient pattern
  - Example:
    ```csharp
    // Old (WebClient)
    using var client = new WebClient();
    var result = client.DownloadString(url);
    
    // New (HttpClient)
    using var client = new HttpClient();
    var result = await client.GetStringAsync(url);
    ```

**Priority 2: Binary Incompatible APIs**
Review assessment for specific file locations of:
- Uri construction patterns
- HttpContent usage
- Any WPF API usages flagged

**Priority 3: Behavioral Changes**
- Test all Uri construction (13 instances)
- Validate HttpContent streaming patterns (6 instances)
- Verify JsonDocument parsing if used

**6. Testing Strategy**

**Unit Testing**:
- Test all HTTP client operations
- Validate Uri parsing with edge cases
- Verify Stream handling patterns

**Integration Testing**:
- Test interactions with Steam API (SteamStorefrontAPI package)
- Validate web scraping (AngleSharp)
- Test file compression (SharpCompress)
- Verify database operations (sqlite-net-pcl)

**Manual Testing Areas**:
- Configuration loading
- External API calls
- File system operations

**7. Validation Checklist**

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All WebClient usages replaced with HttpClient
- [ ] All Uri constructions validated
- [ ] HttpContent streaming tested
- [ ] No API compatibility errors in build output
- [ ] NuGet package restore succeeds
- [ ] Dependent project (GoldbergGUI.WPF) still references correctly

---

### Project: GoldbergGUI.WPF

**Current State**: 
- Target Framework: net8.0-windows
- Project Type: WPF Application
- Dependencies: 1 project (GoldbergGUI.Core), 4 NuGet packages
- Lines of Code: 96
- Files with Issues: 5 out of 6
- API Issues: 22 total (13 binary incompatible, 0 source incompatible, 9 behavioral changes)

**Target State**: 
- Target Framework: net10.0-windows
- Same dependency set (all compatible)

#### Migration Steps

**1. Prerequisites**
- ? .NET 10.0 SDK installed
- ? On `upgrade-to-NET10` branch
- ? GoldbergGUI.Core migrated to net10.0-windows (simultaneous atomic upgrade)
- ? All dependencies available

**2. Framework Update**

Update project file `GoldbergGUI.WPF\GoldbergGUI.WPF.csproj`:

```xml
<!-- Change from: -->
<TargetFramework>net8.0-windows</TargetFramework>

<!-- To: -->
<TargetFramework>net10.0-windows</TargetFramework>
```

**3. Package/Dependency Updates**

**No package updates required** - all 4 packages are compatible with .NET 10.0:

| Package | Current Version | Status |
|---------|----------------|--------|
| MvvmCross.Platforms.Wpf | 7.1.2 | ? Compatible |
| Serilog | 2.10.0 | ? Compatible |
| Serilog.Sinks.Console | 3.1.1 | ? Compatible |
| Serilog.Sinks.File | 4.1.0 | ? Compatible |

**Project Reference**: GoldbergGUI.Core (upgraded simultaneously)

**4. Expected Breaking Changes**

Based on assessment data, expect WPF-specific API issues:

**Binary Incompatible APIs (13 instances)**:

**Clipboard Operations**:
- `System.Windows.Clipboard` (3 instances)
- `System.Windows.TextDataFormat` (4 instances)
- Methods: `ContainsText`, `GetText`, `TextDataFormat.Text`, `TextDataFormat.UnicodeText`
- Impact: WPF clipboard APIs have changed signatures in .NET 10.0
- Review: All clipboard interaction code

**WPF Application APIs**:
- `System.Windows.Application` (3 instances)
- `Application.LoadComponent` (3 instances)
- `Application.Run`, `Application.StartupUri`
- Impact: Application lifetime and component loading changes

**Dialog APIs**:
- `Microsoft.Win32.OpenFileDialog` (1 instance)
- `Microsoft.Win32.FileDialog` properties (4 instances): `FileName`, `Title`, `Multiselect`, `Filter`
- `Microsoft.Win32.CommonDialog.ShowDialog` (1 instance)
- Impact: File dialog API signature changes

**Other WPF Controls**:
- `System.Windows.Controls.CheckBox` (2 instances)
- `System.Windows.MessageBox` (2 instances)
- `System.Windows.Markup.IComponentConnector` (3 instances - likely in generated XAML code)

**Behavioral Changes (9 instances)**:
- `System.Uri` construction (5 instances) - same as Core project
- `System.Net.Http.HttpContent` (1 instance)
- `System.Text.Json.JsonDocument` (2 instances)

**5. Code Modifications**

**Priority 1: WPF API Updates**

**Clipboard Operations**:
```csharp
// Review all clipboard usage
if (Clipboard.ContainsText(TextDataFormat.Text))
{
    var text = Clipboard.GetText();
    // Handle text
}
// May need to adjust to new API signatures
```

**File Dialogs**:
```csharp
// Review all OpenFileDialog usage
var dialog = new OpenFileDialog
{
    Title = "Select File",
    Filter = "All Files (*.*)|*.*",
    Multiselect = false
};
if (dialog.ShowDialog() == true)
{
    var fileName = dialog.FileName;
    // Handle file
}
// Verify API still works or needs adjustments
```

**Application Initialization**:
```csharp
// Check App.xaml.cs and component loading
Application.LoadComponent(this, new Uri("...", UriKind.Relative));
// Ensure Uri construction and LoadComponent still work
```

**Priority 2: Generated XAML Code**
- `IComponentConnector` implementations (3 instances)
- Likely in `.g.cs` files (auto-generated)
- **Action**: Clean and rebuild to regenerate with .NET 10.0 patterns

**Priority 3: Behavioral Changes**
- Uri construction (5 instances) - stricter validation
- HttpContent usage (1 instance)
- JsonDocument parsing (2 instances)

**6. Testing Strategy**

**UI Testing (Critical for WPF)**:
- Launch application and verify it starts
- Test all file open/save dialogs
- Test all clipboard copy/paste operations
- Test all message box displays
- Verify all CheckBox controls work
- Test navigation and component loading

**Functional Testing**:
- Verify MVVM bindings still work (MvvmCross)
- Test logging functionality (Serilog)
- Validate all user interactions
- Test error handling and message display

**Integration Testing**:
- Verify integration with GoldbergGUI.Core
- Test data flow between layers
- Validate service resolution and DI

**7. Validation Checklist**

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] Application launches successfully
- [ ] All dialogs open and function correctly
- [ ] Clipboard operations work
- [ ] Message boxes display properly
- [ ] All UI controls respond correctly
- [ ] XAML component loading succeeds
- [ ] Generated `.g.cs` files compile
- [ ] Logging writes to expected outputs (console, file)
- [ ] No WPF API compatibility errors
- [ ] Application behaves identically to .NET 8.0 version

## Risk Management

### Risk Assessment Overview

**Overall Risk Level**: ?? **Low**

Both projects qualify as low-risk based on:
- Small codebase (2,260 total LOC)
- No package updates required (all compatible)
- No security vulnerabilities
- Well-understood WPF migration patterns
- Manageable API impact (64+ LOC changes)

### High-Risk Changes

**None identified** - No projects meet high-risk criteria:
- ? No large codebases (>10,000 LOC)
- ? No extensive package updates
- ? No security vulnerabilities
- ? No limited test coverage concerns flagged

### Medium-Risk Areas

**API Compatibility Issues**:
- **Risk**: 35 binary incompatible APIs require code changes
- **Impact**: Potential compilation errors if not addressed systematically
- **Mitigation**: 
  - Leverage assessment's detailed API usage locations
  - Fix all issues in single pass during atomic upgrade
  - Validate with full solution build before testing

**Behavioral Changes**:
- **Risk**: 28 APIs have behavioral changes in .NET 10.0
- **Impact**: Runtime behavior may differ from .NET 8.0
- **Mitigation**:
  - Focus testing on areas using affected APIs
  - Validate Uri construction and HTTP content handling
  - Test clipboard operations thoroughly

### Mitigation Strategies

**For API Compatibility Issues**:
1. Use assessment's file and line number details to locate all issues
2. Address binary incompatible APIs first (blocking compilation)
3. Review behavioral change documentation for each affected API
4. Execute comprehensive testing after all fixes applied

**For WPF-Specific Changes**:
1. Test all UI interactions (clipboard, file dialogs, message boxes)
2. Validate XAML component loading
3. Verify data binding and event handling still work correctly

### Contingency Plans

**If Compilation Issues Persist**:
- Review .NET 10.0 breaking changes documentation for each API
- Check for additional WPF-specific migration guidance
- Consider temporary workarounds while investigating complex issues

**If Behavioral Changes Cause Issues**:
- Document specific behavior differences observed
- Adjust code to handle new behavior patterns
- Update tests to reflect new expected behavior

**Rollback Strategy**:
- All changes in single commit on `upgrade-to-NET10` branch
- Can revert commit or delete branch to return to `master`
- No deployment impact (branch-based development)

### Risk Monitoring

**During Upgrade**:
- Monitor build output for unexpected errors beyond assessment findings
- Track test failures to identify behavioral change impacts
- Document any surprises for future reference

**Post-Upgrade**:
- Monitor application startup and runtime behavior
- Watch for WPF-specific rendering or interaction issues
- Validate performance remains acceptable

## Testing & Validation Strategy

### Multi-Level Testing Approach

Since this is an **All-At-Once upgrade**, testing occurs after the complete atomic upgrade of both projects.

### Phase Testing

**Single Atomic Phase: Post-Upgrade Validation**

After both projects are upgraded and built successfully:

**1. Build Validation**
- [ ] Complete solution builds with 0 errors
- [ ] Complete solution builds with 0 warnings
- [ ] All NuGet packages restore successfully
- [ ] Project references resolve correctly (WPF ? Core)

**2. Per-Project Smoke Tests**

**GoldbergGUI.Core**:
- [ ] Library builds independently
- [ ] All public APIs accessible
- [ ] HTTP client operations work (WebClient ? HttpClient migration)
- [ ] Uri construction succeeds with valid inputs
- [ ] Database operations function (sqlite-net-pcl)
- [ ] File compression/decompression works (SharpCompress)
- [ ] Web scraping functions (AngleSharp)
- [ ] Steam API calls succeed (SteamStorefrontAPI)

**GoldbergGUI.WPF**:
- [ ] Application launches without errors
- [ ] Main window displays correctly
- [ ] All views/pages load properly
- [ ] Logging initializes (Serilog to console and file)
- [ ] MvvmCross navigation works
- [ ] Core library integration functions

**3. Comprehensive Functional Testing**

**User Interface Testing**:
- [ ] All windows and dialogs open correctly
- [ ] File open/save dialogs function (`OpenFileDialog` API changes tested)
- [ ] Clipboard copy/paste operations work (`Clipboard` API changes tested)
- [ ] Message boxes display (`MessageBox` API changes tested)
- [ ] CheckBox controls respond to user interaction
- [ ] All buttons and controls are clickable and responsive
- [ ] XAML-based UI renders correctly

**Data Flow Testing**:
- [ ] Data binding updates UI correctly (MVVM patterns)
- [ ] Commands execute properly
- [ ] View models communicate with services
- [ ] Core library services respond correctly from WPF layer

**API Behavioral Change Testing**:
- [ ] **Uri Construction** (18 total instances across both projects)
  - Test with valid URIs (http, https, relative paths)
  - Test with edge cases (special characters, encoded strings)
  - Verify stricter validation doesn't break existing functionality
  
- [ ] **HttpContent Operations** (7 instances)
  - Test `ReadAsStreamAsync` behavior
  - Verify async patterns still work correctly
  - Check for any streaming or buffering differences
  
- [ ] **JsonDocument Parsing** (2 instances in WPF)
  - Verify JSON parsing still works
  - Test with various JSON structures
  - Ensure no behavioral differences in parsing

**Integration Testing**:
- [ ] End-to-end workflows function correctly
- [ ] External API integrations work (Steam API)
- [ ] File system operations succeed
- [ ] Database read/write operations function
- [ ] Logging captures expected events

**Performance Testing** (if applicable):
- [ ] Application startup time acceptable
- [ ] UI responsiveness maintained
- [ ] Memory usage reasonable
- [ ] No obvious performance regressions

### Validation Checkpoints

**Checkpoint 1: Build Success**
- **Criteria**: Solution builds with 0 errors, 0 warnings
- **Action if Failed**: Review compilation errors, fix API compatibility issues, rebuild

**Checkpoint 2: Application Launch**
- **Criteria**: WPF application starts without exceptions
- **Action if Failed**: Review startup logs, check Application.Run and LoadComponent calls

**Checkpoint 3: Core Functionality**
- **Criteria**: All critical user workflows complete successfully
- **Action if Failed**: Identify failing component, review behavioral changes for affected APIs

**Checkpoint 4: Behavioral Validation**
- **Criteria**: Application behaves identically to .NET 8.0 version
- **Action if Failed**: Document differences, determine if acceptable or requires code adjustment

### Test Execution Order

1. **Build Validation** - Must pass before proceeding
2. **GoldbergGUI.Core Smoke Tests** - Validate foundation library
3. **GoldbergGUI.WPF Launch Test** - Ensure application starts
4. **UI Component Tests** - Verify all dialogs and controls
5. **Functional Tests** - Test complete workflows
6. **Behavioral Tests** - Validate API behavior changes
7. **Integration Tests** - End-to-end validation
8. **Performance Tests** - Optional, baseline comparison

### Regression Testing

**Focus Areas** (based on API changes):
- All code paths using Uri construction
- All HTTP client operations (post WebClient migration)
- All WPF dialog interactions
- All clipboard operations
- Application component loading (LoadComponent)

### Test Documentation

**Record the following**:
- Any behavioral differences observed
- API changes that required code modifications
- Test failures and resolutions
- Performance comparison notes (if measured)
- Any unexpected issues encountered

### Success Criteria for Testing Phase

- ? All builds succeed with 0 errors and 0 warnings
- ? Application launches and runs without crashes
- ? All critical user workflows function correctly
- ? All WPF UI components work as expected
- ? No unexpected behavioral changes in core functionality
- ? All API compatibility issues resolved
- ? Integration with external services (Steam API) works
- ? Logging, database, and file operations function normally

## Complexity & Effort Assessment

### Relative Complexity by Project

| Project | Complexity | Dependencies | Risk | Rationale |
|---------|-----------|--------------|------|-----------|
| GoldbergGUI.Core | ?? Medium | 6 packages | ?? Low | 2,164 LOC, 42+ LOC changes needed, 42 API issues, but all packages compatible |
| GoldbergGUI.WPF | ?? Low | 1 project, 4 packages | ?? Low | 96 LOC, 22+ LOC changes, 22 API issues, simple application layer |

### Phase Complexity Assessment

**Single Atomic Phase: All Projects**
- **Combined Complexity**: ?? Medium
- **Total API Issues**: 66 (35 binary incompatible, 1 source incompatible, 28 behavioral changes)
- **Total Code Changes**: 64+ LOC across 11 files
- **Dependency Ordering**: Automatic via MSBuild (Core ? WPF)

**Complexity Factors**:
- ? **Simplifying**: No package updates, simple dependency structure, small solution
- ?? **Moderate**: 35 binary incompatible APIs requiring fixes, WPF-specific migration patterns

### Effort Drivers

**Primary Effort Areas**:
1. **API Compatibility Fixes** (Highest Effort)
   - 35 binary incompatible API usages to update
   - 1 source incompatible issue (System.Net.WebClient)
   - Requires code modifications across 11 files

2. **TargetFramework Updates** (Low Effort)
   - 2 project files to update
   - Straightforward property changes

3. **Behavioral Change Validation** (Medium Effort)
   - 28 APIs with behavior changes
   - Focus: Uri construction, HttpContent usage, JsonDocument handling
   - Requires thorough testing but no code changes unless issues found

4. **Build Validation** (Low Effort)
   - Single solution build
   - No package restoration complexity (all compatible)

### Resource Requirements

**Skill Levels Needed**:
- ? **Primary**: Developer familiar with .NET and WPF patterns
- ? **Secondary**: Understanding of .NET API breaking changes

**Parallel Execution Capacity**:
- **Not Applicable** - All-At-Once strategy means single coordinated operation
- Single developer can execute entire upgrade efficiently
- No need for team coordination across multiple phases

### Relative Effort Distribution

**Across Activities**:
- 50% - Fixing binary incompatible APIs
- 20% - Testing and validating behavioral changes
- 15% - Build validation and error resolution
- 10% - Project file updates and dependency restoration
- 5% - Documentation and commit preparation

**Across Projects**:
- 65% - GoldbergGUI.Core (more API issues, larger codebase)
- 35% - GoldbergGUI.WPF (smaller, dependent project)

## Source Control Strategy

### Branching Strategy

**Current Setup**:
- **Main Branch**: `master`
- **Source Branch**: `master` (starting point)
- **Upgrade Branch**: `upgrade-to-NET10` (already created and checked out)

**Branch Management**:
- All upgrade work occurs on `upgrade-to-NET10` branch
- `master` remains unchanged during upgrade
- Allows for safe experimentation and easy rollback

### Commit Strategy

**Recommended Approach: Single Atomic Commit**

Since this is an **All-At-Once Strategy** upgrade, capture the entire migration in a single logical commit:

**Single Commit Structure**:
```
Upgrade solution to .NET 10.0

- Update GoldbergGUI.Core to net10.0-windows
- Update GoldbergGUI.WPF to net10.0-windows
- Replace WebClient with HttpClient in Core library
- Fix WPF API compatibility issues (Clipboard, Dialogs, Application)
- Address Uri construction and HttpContent behavioral changes
- Verify all 10 NuGet packages compatible (no version updates needed)

All projects build successfully with 0 errors and 0 warnings.
Application tested and functioning correctly on .NET 10.0.
```

**Rationale for Single Commit**:
- Upgrade is atomic - all changes interdependent
- Small solution (2 projects) makes single commit manageable
- Easier to review complete upgrade as one unit
- Simpler rollback if needed (single revert)
- Clean git history with logical boundaries

**Alternative: Checkpoint Commits**

If preferred, can break into logical checkpoints:

1. **Commit 1**: Update project files
   - Change TargetFramework properties in both .csproj files
   
2. **Commit 2**: Fix API compatibility issues
   - WebClient ? HttpClient migration
   - WPF API adjustments
   - Uri and HttpContent fixes
   
3. **Commit 3**: Validation complete
   - Any test adjustments
   - Final cleanup

**Commit Message Format**:
```
<type>: <subject>

<body>

<footer>
```

Example:
```
chore: Upgrade to .NET 10.0

Update both projects from .NET 8.0 to .NET 10.0 (LTS).
Fixed 66 API compatibility issues across 11 files.
All packages remain compatible with no version updates required.

Closes #<issue-number> (if applicable)
```

### Review and Merge Process

**Pull Request Checklist**:
- [ ] Branch `upgrade-to-NET10` is up to date with `master`
- [ ] All commits follow message format
- [ ] Solution builds with 0 errors and 0 warnings
- [ ] All validation checklists completed
- [ ] Application tested and functioning correctly
- [ ] Breaking changes documented (if any behavioral differences observed)
- [ ] No unintended file changes committed (e.g., user settings, bin/obj folders)

**PR Description Template**:
```markdown
## .NET 10.0 Upgrade

### Overview
Upgrades GoldbergGUI solution from .NET 8.0 to .NET 10.0 (Long Term Support).

### Changes
- Updated 2 projects to target net10.0-windows
- Fixed 35 binary incompatible API issues
- Addressed 1 source incompatible issue (WebClient ? HttpClient)
- Validated 28 behavioral changes

### Package Updates
None required - all 10 NuGet packages compatible with .NET 10.0.

### Testing
- [x] Solution builds successfully
- [x] Application launches and runs
- [x] All UI components function correctly
- [x] Critical workflows validated
- [x] Behavioral changes tested

### Breaking Changes
[Document any observable behavioral differences, or state "None"]

### Rollback Plan
Revert merge commit or delete branch to return to .NET 8.0.
```

**Review Criteria**:
1. **Code Quality**: All API fixes follow .NET 10.0 best practices
2. **Completeness**: All 66 identified issues addressed
3. **Testing**: Validation checklist items completed
4. **Documentation**: Changes clearly explained in PR description
5. **Build**: CI/CD pipeline passes (if configured)

**Merge Approach**:
- **Recommended**: Squash merge (consolidates upgrade into single commit on master)
- **Alternative**: Merge commit (preserves individual commits if using checkpoint strategy)
- **Not Recommended**: Rebase (complicates history for feature branch)

### Post-Merge Actions

After merging `upgrade-to-NET10` into `master`:
1. Verify `master` branch builds successfully
2. Tag the merge commit: `git tag v1.0.0-net10` (adjust version as appropriate)
3. Update documentation (README, changelog) to reflect .NET 10.0 requirement
4. Delete `upgrade-to-NET10` branch (optional, after confirming stability)
5. Deploy to test environment for further validation (if applicable)

### Rollback Process

**If issues discovered after merge**:

**Option 1: Revert Merge Commit**
```bash
git revert -m 1 <merge-commit-hash>
git push origin master
```

**Option 2: Hard Reset** (if merge just happened and not pushed to others)
```bash
git reset --hard HEAD~1
git push origin master --force
```

**Option 3: Create Fix Branch**
```bash
git checkout -b hotfix/net10-issue master
# Fix the issue
# Create new PR for fix
```

## Success Criteria

### Technical Criteria

The .NET 10.0 upgrade is technically complete when:

**Framework Migration**:
- ? All 2 projects target `net10.0-windows` (verified in .csproj files)
- ? No projects remain on `net8.0` or `net8.0-windows`
- ? Project references resolve correctly with new target framework

**Package Compatibility**:
- ? All 10 NuGet packages remain at current versions (all compatible, no updates needed)
- ? No package dependency conflicts or warnings
- ? `dotnet restore` succeeds without errors
- ? No security vulnerabilities in package dependencies

**Build Success**:
- ? Complete solution builds with **0 errors**
- ? Complete solution builds with **0 warnings**
- ? All projects build independently
- ? Build succeeds on clean checkout (no environment-specific dependencies)

**API Compatibility**:
- ? All 35 binary incompatible API issues resolved
- ? Source incompatible issue resolved (WebClient ? HttpClient)
- ? No compilation errors from API changes
- ? No runtime exceptions from API behavioral changes

**Testing**:
- ? Application launches successfully
- ? All critical user workflows complete without errors
- ? WPF UI components function correctly (dialogs, clipboard, message boxes)
- ? No unexpected crashes or exceptions during testing
- ? Core library functions correctly in isolation and when integrated

### Quality Criteria

**Code Quality**:
- ? Code follows .NET 10.0 best practices and patterns
- ? Replaced obsolete APIs with recommended modern alternatives
- ? No use of deprecated or obsolete APIs
- ? HttpClient usage follows proper async patterns and disposal
- ? Code readability and maintainability preserved

**Test Coverage**:
- ? All areas with API changes have been tested
- ? Behavioral changes validated through testing
- ? No regression in existing functionality
- ? Edge cases tested (invalid URIs, null handling, etc.)

**Documentation**:
- ? API changes documented in commit messages
- ? Any behavioral differences noted
- ? Breaking changes catalog complete (if any user-facing impacts)
- ? README updated with .NET 10.0 requirement (if applicable)

### Process Criteria

**All-At-Once Strategy Principles**:
- ? Both projects upgraded simultaneously (atomic operation)
- ? No intermediate multi-targeting states
- ? Single coordinated upgrade phase completed
- ? Build order respected dependencies naturally (Core ? WPF)

**Source Control**:
- ? All changes committed to `upgrade-to-NET10` branch
- ? Commit messages follow established format
- ? Single atomic commit (recommended) or logical checkpoint commits
- ? No unintended files committed (bin, obj, user settings)
- ? Branch ready for pull request and review

**Validation**:
- ? All project-specific validation checklists completed
- ? All testing checkpoints passed
- ? Smoke tests, functional tests, and integration tests completed
- ? Performance baseline acceptable (no significant regressions)

### Completeness Checklist

**Before Declaring Upgrade Complete**:

**Project Files**:
- [ ] `GoldbergGUI.Core\GoldbergGUI.Core.csproj` - TargetFramework = net10.0-windows
- [ ] `GoldbergGUI.WPF\GoldbergGUI.WPF.csproj` - TargetFramework = net10.0-windows

**API Fixes**:
- [ ] WebClient replaced with HttpClient (1 instance)
- [ ] All WPF Clipboard operations updated (3 instances)
- [ ] All WPF Dialog operations verified (7 instances)
- [ ] All Application API calls updated (6 instances)
- [ ] All Uri constructions validated (18 instances)
- [ ] All HttpContent operations tested (7 instances)
- [ ] IComponentConnector generated code rebuilt (3 instances)

**Build Verification**:
- [ ] `dotnet restore` - Success
- [ ] `dotnet build` - Success, 0 errors, 0 warnings
- [ ] `dotnet build --configuration Release` - Success

**Functional Verification**:
- [ ] Application starts without errors
- [ ] Main window displays correctly
- [ ] File dialogs open and function
- [ ] Clipboard operations work
- [ ] Message boxes display
- [ ] All critical workflows complete
- [ ] No runtime exceptions in normal usage

**Source Control**:
- [ ] All changes committed
- [ ] Commit messages descriptive
- [ ] Branch pushed to remote (if using remote repository)
- [ ] Ready for pull request creation

### Sign-Off

Upgrade is considered **COMPLETE** when:
1. All Technical Criteria met (? checkmarks)
2. All Quality Criteria met (? checkmarks)
3. All Process Criteria met (? checkmarks)
4. Completeness Checklist 100% checked (all [ ] become [x])
5. Pull request approved and merged to `master`
6. Post-merge validation confirms stable `master` branch

**Final Validation**: After merge, application deployed to test environment (if applicable) and validated in production-like conditions confirms successful .NET 10.0 upgrade.
