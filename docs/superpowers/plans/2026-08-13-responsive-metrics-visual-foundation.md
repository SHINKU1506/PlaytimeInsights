# Responsive Metrics Visual Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the fixed `218 × 154` Dashboard metric-card wrap with a tested native WPF 1–4-column responsive layout and introduce theme-aware semantic text opacity resources without changing application behavior.

**Architecture:** A focused `ResponsiveUniformPanel : Panel` owns only WPF measure/arrange behavior and exposes layout inputs as dependency properties. The existing nine Dashboard `Border` cards remain unchanged consumers except for size and container declarations; semantic opacity values stay local to the Dashboard resource dictionary. The existing custom regression executable drives both live STA/WPF layout tests and static XAML guardrails.

**Tech Stack:** C# 7.3, .NET Framework 4.6.2, WPF, Playnite SDK 6.16.x, SDK-style MSBuild, custom console regression harness.

## Global Constraints

- Use native WPF and existing Playnite dynamic theme brushes; add no WebView, remote asset, runtime dependency, or third-party UI/chart library.
- Do not change statistics, filtering, refresh behavior, storage schema, import/export behavior, plugin ID, version, installed extension, themes, configuration, or user data.
- Keep all nine metric cards as non-focusable, non-clickable `Border` elements with unchanged order, bindings, icons, trend tags, and copy.
- Do not add Skeleton UI, asynchronous refresh, full-page fades, metric-card hover/scale/lift/shadow states, DataGrid, or session-list changes.
- Target `net462` and C# 7.3; continue to use the repository's custom `Tests/Program.cs` harness.
- Preserve the untracked `perf_test.ps1`; never stage, modify, delete, or commit it.
- Do not deploy, package, start Playnite, or write `ExtensionsData` in this plan.

---

## File Map

- `Controls/ResponsiveUniformPanel.cs`: owns dependency properties, input normalization, responsive column selection, row measurement, centered grid placement, and centered incomplete-row arrangement.
- `Views/PlaytimeInsightsDashboardView.xaml`: declares semantic opacity resources, consumes the Panel for the nine existing cards, and removes fixed card dimensions and fixed helper-text color.
- `Tests/Program.cs`: registers and implements live WPF layout tests plus static XAML visual-foundation guardrails.
- `docs/IMPLEMENTATION_STATUS.md`: records actual implementation, exact verification evidence, limits, and client acceptance steps after code is complete.
- `docs/VISUAL_UX_OPTIMIZATION_PLAN.md`: marks visual stage 1 and the scoped text-foundation portion as implemented only after verification passes.

### Task 1: ResponsiveUniformPanel layout engine

**Files:**
- Create: `Controls/ResponsiveUniformPanel.cs`
- Modify: `Tests/Program.cs:30-127`
- Modify: `Tests/Program.cs:2715-2910`

**Interfaces:**
- Consumes: WPF `Panel`, `UIElementCollection`, `DependencyProperty`, `MeasureOverride(Size)`, and `ArrangeOverride(Size)`.
- Produces: public sealed `PlaytimeInsights.Controls.ResponsiveUniformPanel` with dependency properties and CLR wrappers named exactly `MinItemWidth`, `PreferredItemWidth`, `MaxItemWidth`, `MinColumns`, `MaxColumns`, `HorizontalSpacing`, `VerticalSpacing`, and `CenterIncompleteRow`.
- Produces: defaults `204d`, `232d`, `300d`, `1`, `4`, `12d`, `12d`, and `true`; every property metadata declaration includes `FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange`.
- Produces: live WPF behavior where widths `360`, `640`, `900`, and `1200` arrange visible children into exactly `1`, `2`, `3`, and `4` columns.

- [ ] **Step 1: Register failing layout tests**

Add these registrations after `TestThemeAndResponsiveLayout` in `Main()`:

```csharp
Run("Responsive metric panel selects expected columns", TestResponsiveMetricPanelColumns);
Run("Responsive metric panel centers and equalizes rows", TestResponsiveMetricPanelArrangement);
Run("Responsive metric panel contains invalid inputs", TestResponsiveMetricPanelEdgeCases);
```

Add the following tests next to `TestTrendChartSourceLifecycle`. Add both `using System.Windows.Controls;` and `using System.Windows.Controls.Primitives;` for `Border`, `TextBlock`, and `LayoutInformation`.

```csharp
private static void TestResponsiveMetricPanelColumns()
{
    RunOnSta(() =>
    {
        foreach (var sample in new[]
        {
            new { Width = 320d, Columns = 1 },
            new { Width = 360d, Columns = 1 },
            new { Width = 640d, Columns = 2 },
            new { Width = 900d, Columns = 3 },
            new { Width = 1200d, Columns = 4 }
        })
        {
            var panel = CreateMetricPanel(9, 154);
            LayoutMetricPanel(panel, sample.Width);
            var firstTop = GetLayoutSlot(panel.Children[0]).Top;
            var columns = panel.Children
                .Cast<UIElement>()
                .TakeWhile(child =>
                    Math.Abs(GetLayoutSlot(child).Top - firstTop) < 0.01)
                .Count();
            Equal(sample.Columns, columns);
        }
    });
}

private static void TestResponsiveMetricPanelArrangement()
{
    RunOnSta(() =>
    {
        var panel = CreateMetricPanel(9, 154);
        ((Border)panel.Children[1]).MinHeight = 190;
        LayoutMetricPanel(panel, 1200);

        var first = GetLayoutSlot(panel.Children[0]);
        var second = GetLayoutSlot(panel.Children[1]);
        var fourth = GetLayoutSlot(panel.Children[3]);
        var ninth = GetLayoutSlot(panel.Children[8]);

        Equal(true, Math.Abs(first.Width - second.Width) < 0.01);
        Equal(true, Math.Abs(first.Height - second.Height) < 0.01);
        Equal(true, first.Width >= 204 && first.Width <= 300);
        Equal(true, fourth.Right <= 1200);
        Equal(true, Math.Abs(ninth.Left - ((1200 - ninth.Width) / 2)) < 0.01);
    });
}

private static void TestResponsiveMetricPanelEdgeCases()
{
    RunOnSta(() =>
    {
        foreach (var count in new[] { 0, 1, 9, 10 })
        {
            var panel = CreateMetricPanel(count, 154);
            LayoutMetricPanel(panel, 640);
            Equal(true, IsFiniteNonNegative(panel.DesiredSize.Width));
            Equal(true, IsFiniteNonNegative(panel.DesiredSize.Height));
            foreach (UIElement child in panel.Children)
            {
                var slot = GetLayoutSlot(child);
                Equal(true, IsFiniteNonNegative(slot.X));
                Equal(true, IsFiniteNonNegative(slot.Y));
                Equal(true, IsFiniteNonNegative(slot.Width));
                Equal(true, IsFiniteNonNegative(slot.Height));
            }

            var slots = panel.Children
                .Cast<UIElement>()
                .Select(GetLayoutSlot)
                .Where(slot => slot.Width > 0 && slot.Height > 0)
                .ToList();
            for (var left = 0; left < slots.Count; left++)
            {
                for (var right = left + 1; right < slots.Count; right++)
                {
                    Equal(false, slots[left].IntersectsWith(slots[right]));
                }
            }
        }

        var collapsed = CreateMetricPanel(3, 154);
        collapsed.Children[1].Visibility = Visibility.Collapsed;
        LayoutMetricPanel(collapsed, 640);
        Equal(new Rect(0, 0, 0, 0), GetLayoutSlot(collapsed.Children[1]));

        var invalid = CreateMetricPanel(3, 154);
        invalid.MinItemWidth = double.NaN;
        invalid.PreferredItemWidth = double.PositiveInfinity;
        invalid.MaxItemWidth = -1;
        invalid.MinColumns = 0;
        invalid.MaxColumns = -4;
        invalid.HorizontalSpacing = -12;
        invalid.VerticalSpacing = double.NaN;
        LayoutMetricPanel(invalid, 0);
        invalid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Equal(true, IsFiniteNonNegative(invalid.DesiredSize.Width));
        Equal(true, IsFiniteNonNegative(invalid.DesiredSize.Height));
    });
}

private static ResponsiveUniformPanel CreateMetricPanel(int count, double minHeight)
{
    var panel = new ResponsiveUniformPanel();
    for (var index = 0; index < count; index++)
    {
        panel.Children.Add(new Border
        {
            MinHeight = minHeight,
            Child = new TextBlock
            {
                Text = index == 1
                    ? "Long localized helper text that wraps onto another line"
                    : "Metric " + index,
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    return panel;
}

private static void LayoutMetricPanel(
    ResponsiveUniformPanel panel,
    double width)
{
    panel.Measure(new Size(width, double.PositiveInfinity));
    panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
    panel.UpdateLayout();
}

private static Rect GetLayoutSlot(UIElement element)
{
    return LayoutInformation.GetLayoutSlot((FrameworkElement)element);
}

private static bool IsFiniteNonNegative(double value)
{
    return !double.IsNaN(value) &&
        !double.IsInfinity(value) &&
        value >= 0;
}
```

- [ ] **Step 2: Run the focused harness and confirm the new tests fail**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
```

Expected: compilation fails because `ResponsiveUniformPanel` does not exist. No production file should have changed yet.

- [ ] **Step 3: Implement dependency properties and normalized layout inputs**

Create `Controls/ResponsiveUniformPanel.cs` with this namespace, class declaration, dependency-property set, and normalization code:

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PlaytimeInsights.Controls
{
    public sealed class ResponsiveUniformPanel : Panel
    {
public static readonly DependencyProperty MinItemWidthProperty =
    DependencyProperty.Register(
        nameof(MinItemWidth),
        typeof(double),
        typeof(ResponsiveUniformPanel),
        new FrameworkPropertyMetadata(
            204d,
            FrameworkPropertyMetadataOptions.AffectsMeasure |
            FrameworkPropertyMetadataOptions.AffectsArrange));

public double MinItemWidth
{
    get => (double)GetValue(MinItemWidthProperty);
    set => SetValue(MinItemWidthProperty, value);
}

        public static readonly DependencyProperty PreferredItemWidthProperty =
            DependencyProperty.Register(
                nameof(PreferredItemWidth),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    232d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double PreferredItemWidth
        {
            get => (double)GetValue(PreferredItemWidthProperty);
            set => SetValue(PreferredItemWidthProperty, value);
        }

        public static readonly DependencyProperty MaxItemWidthProperty =
            DependencyProperty.Register(
                nameof(MaxItemWidth),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    300d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double MaxItemWidth
        {
            get => (double)GetValue(MaxItemWidthProperty);
            set => SetValue(MaxItemWidthProperty, value);
        }

        public static readonly DependencyProperty MinColumnsProperty =
            DependencyProperty.Register(
                nameof(MinColumns),
                typeof(int),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public int MinColumns
        {
            get => (int)GetValue(MinColumnsProperty);
            set => SetValue(MinColumnsProperty, value);
        }

        public static readonly DependencyProperty MaxColumnsProperty =
            DependencyProperty.Register(
                nameof(MaxColumns),
                typeof(int),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    4,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public int MaxColumns
        {
            get => (int)GetValue(MaxColumnsProperty);
            set => SetValue(MaxColumnsProperty, value);
        }

        public static readonly DependencyProperty HorizontalSpacingProperty =
            DependencyProperty.Register(
                nameof(HorizontalSpacing),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    12d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double HorizontalSpacing
        {
            get => (double)GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }

        public static readonly DependencyProperty VerticalSpacingProperty =
            DependencyProperty.Register(
                nameof(VerticalSpacing),
                typeof(double),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    12d,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public double VerticalSpacing
        {
            get => (double)GetValue(VerticalSpacingProperty);
            set => SetValue(VerticalSpacingProperty, value);
        }

        public static readonly DependencyProperty CenterIncompleteRowProperty =
            DependencyProperty.Register(
                nameof(CenterIncompleteRow),
                typeof(bool),
                typeof(ResponsiveUniformPanel),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public bool CenterIncompleteRow
        {
            get => (bool)GetValue(CenterIncompleteRowProperty);
            set => SetValue(CenterIncompleteRowProperty, value);
        }

        private LayoutSettings GetSettings()
        {
            var minWidth = NormalizePositive(MinItemWidth, 204d);
            var preferredWidth = Math.Max(
                minWidth,
                NormalizePositive(PreferredItemWidth, 232d));
            var maxWidth = Math.Max(
                preferredWidth,
                NormalizePositive(MaxItemWidth, 300d));
            var minColumns = Math.Max(1, MinColumns);
            var maxColumns = Math.Max(minColumns, MaxColumns);

            return new LayoutSettings
            {
                MinItemWidth = minWidth,
                PreferredItemWidth = preferredWidth,
                MaxItemWidth = maxWidth,
                MinColumns = minColumns,
                MaxColumns = maxColumns,
                HorizontalSpacing = NormalizeSpacing(HorizontalSpacing, 12d),
                VerticalSpacing = NormalizeSpacing(VerticalSpacing, 12d),
                CenterIncompleteRow = CenterIncompleteRow
            };
        }

        private static double NormalizePositive(double value, double fallback)
        {
            return IsFinite(value) && value > 0 ? value : fallback;
        }

        private static double NormalizeSpacing(double value, double fallback)
        {
            if (!IsFinite(value))
            {
                return fallback;
            }

            return Math.Max(0, value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class LayoutSettings
        {
            public double MinItemWidth { get; set; }
            public double PreferredItemWidth { get; set; }
            public double MaxItemWidth { get; set; }
            public int MinColumns { get; set; }
            public int MaxColumns { get; set; }
            public double HorizontalSpacing { get; set; }
            public double VerticalSpacing { get; set; }
            public bool CenterIncompleteRow { get; set; }
        }
    }
}
```

- [ ] **Step 4: Implement measure and arrange behavior**

Add the following members before `LayoutSettings` in the same class. They are the complete measure/arrange implementation and use only the Step 3 properties and normalization helpers:

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var visibleChildren = GetVisibleChildren(InternalChildren);
    if (visibleChildren.Count == 0)
    {
        return new Size(0, 0);
    }

    var settings = GetSettings();
    var layoutWidth = ResolveLayoutWidth(
        availableSize.Width,
        visibleChildren.Count,
        settings);
    var columns = SelectColumnCount(
        layoutWidth,
        visibleChildren.Count,
        settings);
    var itemWidth = CalculateItemWidth(layoutWidth, columns, settings);

    foreach (var child in visibleChildren)
    {
        child.Measure(new Size(itemWidth, double.PositiveInfinity));
    }

    var rowHeights = GetRowHeights(visibleChildren, columns);
    return new Size(
        CalculateGridWidth(itemWidth, columns, settings),
        CalculateGridHeight(rowHeights, settings.VerticalSpacing));
}

protected override Size ArrangeOverride(Size finalSize)
{
    foreach (UIElement child in InternalChildren)
    {
        if (child.Visibility == Visibility.Collapsed)
        {
            child.Arrange(new Rect(0, 0, 0, 0));
        }
    }

    var visibleChildren = GetVisibleChildren(InternalChildren);
    if (visibleChildren.Count == 0)
    {
        return finalSize;
    }

    var settings = GetSettings();
    var layoutWidth = IsFinite(finalSize.Width)
        ? Math.Max(0, finalSize.Width)
        : ResolveLayoutWidth(
            double.PositiveInfinity,
            visibleChildren.Count,
            settings);
    var columns = SelectColumnCount(
        layoutWidth,
        visibleChildren.Count,
        settings);
    var itemWidth = CalculateItemWidth(layoutWidth, columns, settings);
    var rowHeights = GetRowHeights(visibleChildren, columns);
    var gridWidth = CalculateGridWidth(itemWidth, columns, settings);
    var gridStart = Math.Max(0, (layoutWidth - gridWidth) / 2);
    var childIndex = 0;
    var y = 0d;

    for (var rowIndex = 0;
        rowIndex < rowHeights.Count;
        rowIndex++)
    {
        var itemsInRow = Math.Min(
            columns,
            visibleChildren.Count - childIndex);
        var rowWidth = CalculateGridWidth(
            itemWidth,
            itemsInRow,
            settings);
        var rowStart = settings.CenterIncompleteRow && itemsInRow < columns
            ? Math.Max(0, (layoutWidth - rowWidth) / 2)
            : gridStart;

        for (var column = 0; column < itemsInRow; column++)
        {
            visibleChildren[childIndex].Arrange(new Rect(
                rowStart + (column * (itemWidth + settings.HorizontalSpacing)),
                y,
                itemWidth,
                rowHeights[rowIndex]));
            childIndex++;
        }

        y += rowHeights[rowIndex];
        if (rowIndex < rowHeights.Count - 1)
        {
            y += settings.VerticalSpacing;
        }
    }

    return finalSize;
}

private int SelectColumnCount(
    double availableWidth,
    int visibleChildCount,
    LayoutSettings settings)
{
    var upperBound = Math.Min(settings.MaxColumns, visibleChildCount);
    var lowerBound = Math.Min(settings.MinColumns, upperBound);
    var preferredColumns = (int)Math.Floor(
        (availableWidth + settings.HorizontalSpacing) /
        (settings.PreferredItemWidth + settings.HorizontalSpacing));
    var columns = Math.Max(
        lowerBound,
        Math.Min(upperBound, preferredColumns));

    while (columns > 1 &&
        CalculateItemWidth(availableWidth, columns, settings) <
            settings.MinItemWidth)
    {
        columns--;
    }

    return Math.Max(1, columns);
}

private static double CalculateItemWidth(
    double availableWidth,
    int columns,
    LayoutSettings settings)
{
    var spacingWidth = (columns - 1) * settings.HorizontalSpacing;
    var width = Math.Max(0, (availableWidth - spacingWidth) / columns);
    return Math.Min(settings.MaxItemWidth, width);
}

private static List<UIElement> GetVisibleChildren(
    UIElementCollection children)
{
    var visible = new List<UIElement>();
    foreach (UIElement child in children)
    {
        if (child.Visibility != Visibility.Collapsed)
        {
            visible.Add(child);
        }
    }

    return visible;
}

private static double ResolveLayoutWidth(
    double availableWidth,
    int visibleChildCount,
    LayoutSettings settings)
{
    if (IsFinite(availableWidth))
    {
        return Math.Max(0, availableWidth);
    }

    var columns = Math.Min(
        settings.MaxColumns,
        Math.Max(1, visibleChildCount));
    return (columns * settings.PreferredItemWidth) +
        ((columns - 1) * settings.HorizontalSpacing);
}

private static double CalculateGridWidth(
    double itemWidth,
    int columns,
    LayoutSettings settings)
{
    if (columns <= 0)
    {
        return 0;
    }

    return (columns * itemWidth) +
        ((columns - 1) * settings.HorizontalSpacing);
}

private static List<double> GetRowHeights(
    IList<UIElement> children,
    int columns)
{
    var heights = new List<double>();
    for (var start = 0; start < children.Count; start += columns)
    {
        var height = 0d;
        var end = Math.Min(start + columns, children.Count);
        for (var index = start; index < end; index++)
        {
            var desiredHeight = children[index].DesiredSize.Height;
            if (IsFinite(desiredHeight))
            {
                height = Math.Max(height, Math.Max(0, desiredHeight));
            }
        }

        heights.Add(height);
    }

    return heights;
}

private static double CalculateGridHeight(
    IList<double> rowHeights,
    double verticalSpacing)
{
    var height = 0d;
    for (var index = 0; index < rowHeights.Count; index++)
    {
        height += rowHeights[index];
        if (index < rowHeights.Count - 1)
        {
            height += verticalSpacing;
        }
    }

    return height;
}
```

- [ ] **Step 5: Run the regression executable and confirm all live layout tests pass**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
& .\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe
```

Expected: build succeeds with 0 warnings and 0 errors; the three new tests print `[PASS]`, every prior test remains `[PASS]`, and the final line is `All Playtime Insights tests passed.`

- [ ] **Step 6: Commit the independently testable Panel**

```powershell
git add -- Controls\ResponsiveUniformPanel.cs Tests\Program.cs
git commit -m "feat: add responsive metric card panel"
```

Before committing, `git status --short` must still show `?? perf_test.ps1` and no unrelated paths.

### Task 2: Dashboard container and semantic text resources

**Files:**
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:13-145`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:263-277`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:509-628`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:660-675`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:768-772`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:800-823`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:883-889`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:953-959`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:1013-1044`
- Modify: `Views/PlaytimeInsightsDashboardView.xaml:1090-1092`
- Modify: `Tests/Program.cs:30-127`
- Modify: `Tests/Program.cs:1964-2040`

**Interfaces:**
- Consumes: `ResponsiveUniformPanel` and its eight public properties from Task 1.
- Produces: Dashboard resources named exactly `TextOpacityPrimary`, `TextOpacitySecondary`, `TextOpacityTertiary`, and `TextOpacityDisabled`, typed as `sys:Double` with values `1`, `0.72`, `0.58`, and `0.45`.
- Produces: a named metric container `MetricCardsPanel` whose runtime type is `ResponsiveUniformPanel`, with `MinItemWidth=204`, `PreferredItemWidth=232`, `MaxItemWidth=300`, `MinColumns=1`, `MaxColumns=4`, `HorizontalSpacing=12`, `VerticalSpacing=12`, and `CenterIncompleteRow=true`.
- Produces: metric cards with `MinHeight="154"`, no fixed `Width`, no fixed `Height`, and no card-level `Margin`.

- [ ] **Step 1: Add a failing live WPF visual-foundation test**

Register after the three Task 1 tests:

```csharp
Run("Dashboard metrics use responsive semantic visual foundation", TestResponsiveMetricVisualFoundation);
```

Add `using PlaytimeInsights.Views;` and this test next to the other STA/WPF tests. It loads the compiled XAML, resolves the named visual element, applies parsed styles to real controls, and asserts their runtime properties; it must not read or regex-match the XAML source file.

```csharp
private static void TestResponsiveMetricVisualFoundation()
{
    RunOnSta(() =>
    {
        var view = new PlaytimeInsightsDashboardView();
        var panel = view.FindName("MetricCardsPanel") as
            ResponsiveUniformPanel;
        Equal(true, panel != null);
        Equal(9, panel.Children.Count);
        Equal(204d, panel.MinItemWidth);
        Equal(232d, panel.PreferredItemWidth);
        Equal(300d, panel.MaxItemWidth);
        Equal(1, panel.MinColumns);
        Equal(4, panel.MaxColumns);
        Equal(12d, panel.HorizontalSpacing);
        Equal(12d, panel.VerticalSpacing);
        Equal(true, panel.CenterIncompleteRow);

        Equal(1d, (double)view.Resources["TextOpacityPrimary"]);
        Equal(0.72d, (double)view.Resources["TextOpacitySecondary"]);
        Equal(0.58d, (double)view.Resources["TextOpacityTertiary"]);
        Equal(0.45d, (double)view.Resources["TextOpacityDisabled"]);

        var card = new Border
        {
            Style = (Style)view.Resources["MetricCardStyle"]
        };
        Equal(154d, card.MinHeight);
        Equal(true, double.IsNaN(card.Width));
        Equal(true, double.IsNaN(card.Height));
        Equal(new Thickness(0), card.Margin);

        var header = new TextBlock
        {
            Style = (Style)view.Resources["MetricHeaderStyle"]
        };
        var icon = new TextBlock
        {
            Style = (Style)view.Resources["MetricIconStyle"]
        };
        var helper = new TextBlock
        {
            Style = (Style)view.Resources["MetricHelperTextStyle"]
        };
        Equal(0.72d, header.Opacity);
        Equal(0.58d, icon.Opacity);
        Equal(0.58d, helper.Opacity);

        LayoutMetricPanel(panel, 1200);
        var ninth = GetLayoutSlot(panel.Children[8]);
        Equal(true,
            Math.Abs(ninth.Left - ((1200 - ninth.Width) / 2)) < 0.01);
    });
}
```

- [ ] **Step 2: Run the harness and confirm the static test fails**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
& .\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe
```

Expected: `Dashboard metrics use responsive semantic visual foundation` fails because `FindName("MetricCardsPanel")` returns null before the metric container is migrated and named. If view construction exposes a missing test-only theme resource, add only the minimum resource setup in the test harness and keep the production XAML unchanged.

- [ ] **Step 3: Add semantic opacity resources and migrate relevant text**

Add the namespace to the root `UserControl`:

```xml
xmlns:sys="clr-namespace:System;assembly=mscorlib"
```

Add these resources before `PanelStyle`:

```xml
<sys:Double x:Key="TextOpacityPrimary">1</sys:Double>
<sys:Double x:Key="TextOpacitySecondary">0.72</sys:Double>
<sys:Double x:Key="TextOpacityTertiary">0.58</sys:Double>
<sys:Double x:Key="TextOpacityDisabled">0.45</sys:Double>
```

Update these styles exactly:

```xml
<Setter Property="Opacity" Value="{StaticResource TextOpacitySecondary}" />
```

for `MetricHeaderStyle`, `FieldLabelStyle`, `HelpIconButtonStyle`, and the default `RankingPositionTextStyle`.

Use:

```xml
<Setter Property="Opacity" Value="{StaticResource TextOpacityTertiary}" />
```

for `MetricIconStyle`. In `MetricHelperTextStyle`, replace fixed gray with:

```xml
<Setter Property="Foreground" Value="{DynamicResource TextBrush}" />
<Setter Property="Opacity" Value="{StaticResource TextOpacityTertiary}" />
```

Replace text-only literal opacities as follows:

- Primary: ranking `PrimaryValueText`, anomaly `Reason`, and bottom `StatusText`.
- Secondary: anomaly `StartedText` and `DurationText`; drilldown `StartedText` and `DurationText`.
- Tertiary: ranking `DetailText`; hour-distribution labels; both heatmap axis labels; `SessionDetailCountText`; drilldown `SourceText`.
- Leave `DropShadowEffect Opacity="0.32"`, ranking progress `Opacity="0.12"`, gold/silver/bronze trigger `Opacity="1"`, and other non-text visual effects unchanged.
- Change the weekday disabled trigger to `Value="{StaticResource TextOpacityDisabled}"`.

- [ ] **Step 4: Replace the metric WrapPanel and remove card-fixed layout**

Change `MetricCardStyle` to:

```xml
<Style x:Key="MetricCardStyle"
       TargetType="Border"
       BasedOn="{StaticResource PanelStyle}">
    <Setter Property="MinHeight" Value="154" />
    <Setter Property="Padding" Value="16" />
</Style>
```

Replace only the `WrapPanel` that directly contains the nine metric cards with:

```xml
<controls:ResponsiveUniformPanel x:Name="MetricCardsPanel"
                                 Margin="0,0,0,18"
                                 MinItemWidth="204"
                                 PreferredItemWidth="232"
                                 MaxItemWidth="300"
                                 MinColumns="1"
                                 MaxColumns="4"
                                 HorizontalSpacing="12"
                                 VerticalSpacing="12"
                                 CenterIncompleteRow="True">
```

and close it with `</controls:ResponsiveUniformPanel>`. Do not alter the filter `WrapPanel` ending near the original line 505 or any card content.

- [ ] **Step 5: Run the focused and full regression checks**

Run:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
& .\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe
```

Expected: `Dashboard metrics use responsive semantic visual foundation` passes, all Task 1 layout tests pass, every prior test passes, and the final line is `All Playtime Insights tests passed.`

- [ ] **Step 6: Build the production project**

Run:

```powershell
dotnet build PlaytimeInsights.csproj -c Release --no-restore
```

Expected: `Build succeeded.` with 0 warnings and 0 errors. Do not copy the resulting files to `Extensions`, `staging`, `dist`, or `D:\software\Playnite`.

- [ ] **Step 7: Commit the Dashboard visual migration**

```powershell
git add -- Views\PlaytimeInsightsDashboardView.xaml Tests\Program.cs
git commit -m "feat: make dashboard metrics responsive"
```

Before committing, inspect `git diff --check` and `git status --short`; `perf_test.ps1` must remain untracked and unstaged.

### Task 3: Status records and final verification

**Files:**
- Modify: `docs/VISUAL_UX_OPTIMIZATION_PLAN.md:1-20`
- Modify: `docs/VISUAL_UX_OPTIMIZATION_PLAN.md` stage 1 and stage 3 sections
- Modify: `docs/IMPLEMENTATION_STATUS.md:1-20`

**Interfaces:**
- Consumes: verified code and exact command output from Tasks 1–2.
- Produces: an implementation record that states actual test count, build warning/error counts, changed files, non-goals, no-deployment/no-user-data status, and exact client acceptance steps.
- Produces: visual plan status marking only responsive metrics and the scoped Dashboard semantic-text foundation complete; empty states, cross-page token sharing, microinteractions, and session column governance remain future work.

- [ ] **Step 1: Update visual-plan progress without rewriting history**

At the top of `docs/VISUAL_UX_OPTIMIZATION_PLAN.md`, change the status from waiting for 0.9.8 to an in-progress statement that names this completed batch. Under stage 1, add an `实施结果（2026-08-13）` subsection containing:

```markdown
- 新增原生 `ResponsiveUniformPanel`，按 204/232/300 像素宽度约束和 1–4 列规则布局九张指标卡；
- 横纵间距统一为 12 像素，同排等宽等高，受最大宽度限制的网格和不完整末行居中；
- 指标卡删除固定 218 × 154，保留 `MinHeight=154` 并允许长本地化文本增高；
- 自动化覆盖 360/640/900/1200 宽度、0/1/9/10 子元素、长内容、无限约束和无效属性值；
- 本批次未改变指标内容、交互、统计、刷新、存储、安装目录或用户数据。
```

Under stage 3, add an implementation note stating that Dashboard now has four named semantic opacity resources and that Sessions/settings cross-page extraction remains pending.

- [ ] **Step 2: Add a current implementation-status entry**

Prepend a section to `docs/IMPLEMENTATION_STATUS.md` after its current-phase header. Include:

- files added/changed;
- the final actual regression count copied from the test executable output;
- both build commands and their actual warning/error results;
- confirmation that no deployment, package, Playnite launch, theme/config change, or user-data write occurred;
- the preserved untracked `perf_test.ps1`;
- client steps: Chinese and English; 100%, 125%, 150%, 200% DPI; default dark, Seaside, light, high contrast; widths 320, 360, 640, 900, 1200 and continuous resize; check 1/2/3/4 columns, equal rows, centered final card, readable helper text, no hover/click affordance, unchanged charts/filters/drilldown/wheel behavior.

Do not claim client acceptance has passed; record it as pending.

- [ ] **Step 3: Run fresh final verification**

Run from a clean command invocation after all source and document changes:

```powershell
dotnet build Tests\PlaytimeInsights.Tests.csproj -c Release --no-restore
& .\Tests\bin\Release\net462\PlaytimeInsightsRegression.exe
dotnet build PlaytimeInsights.csproj -c Release --no-restore
git diff --check
git status --short
```

Expected:

- both builds succeed with 0 warnings and 0 errors;
- every regression is `[PASS]` and the executable ends with `All Playtime Insights tests passed.`;
- `git diff --check` emits no errors;
- only the two documentation files are modified at this task boundary, plus the intentionally untracked `perf_test.ps1`.

- [ ] **Step 4: Commit verified status documentation**

```powershell
git add -- docs\VISUAL_UX_OPTIMIZATION_PLAN.md docs\IMPLEMENTATION_STATUS.md
git commit -m "docs: record responsive visual foundation"
```

Do not stage `perf_test.ps1`.

- [ ] **Step 5: Inspect final repository state and commits**

Run:

```powershell
git status --short
git log -4 --oneline --decorate
```

Expected: the only working-tree entry is `?? perf_test.ps1`; the design commit, Panel commit, Dashboard commit, and status-document commit are visible. Client visual acceptance remains the only unfinished external check.
