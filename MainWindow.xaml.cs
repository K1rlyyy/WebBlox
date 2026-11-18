using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.CompilerServices;

namespace WebBlox;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PageElement> _elements = new();
    private readonly List<string> _allBlocks = new();
    private PageElement? _selected;
    private bool _loadingProperties;
    private bool _loadingCss;
    private string _globalCss = "";
    private string _javaScript = "";
    private string? _projectDirectory;
    private Point _dragStart;
    private readonly ObservableCollection<CssDeclaration> _cssDeclarations = new();
    private bool _loadingCssDesigner;
    private bool _isDarkTheme;
    private bool _themeRefreshPending;
    private double _canvasZoom = 1.0;
    private readonly ObservableCollection<JsVisualBlock> _jsBlocks = new();
    private readonly ObservableCollection<JsBlockTemplate> _visibleJsBlockTemplates = new();
    private List<JsBlockTemplate> _allJsBlockTemplates = new();
    private readonly ObservableCollection<JsFunctionSnippet> _visibleJsFunctionSnippets = new();
    private List<JsFunctionSnippet> _allJsFunctionSnippets = new();
    private bool _loadingJsBlockFields;
    private Point _jsBlockDragStart;

    private static readonly Dictionary<Color, Color> LightToDarkThemeColors = new()
    {
        // Soft blue light-theme palette -> deep navy surfaces. Every target is unique so
        // the reverse map remains deterministic when switching back to light mode.
        [Color.FromRgb(226, 235, 245)] = Color.FromRgb(7, 23, 42),   // #E2EBF5 window
        [Color.FromRgb(213, 225, 239)] = Color.FromRgb(16, 32, 57),  // #D5E1EF panels
        [Color.FromRgb(237, 242, 248)] = Color.FromRgb(24, 43, 67),  // #EDF2F8 cards
        [Color.FromRgb(223, 233, 246)] = Color.FromRgb(32, 56, 83),  // #DFE9F6 library
        [Color.FromRgb(220, 232, 245)] = Color.FromRgb(36, 59, 89),  // #DCE8F5 soft accent
        [Color.FromRgb(199, 220, 239)] = Color.FromRgb(46, 70, 99),  // #C7DCEF selected
        [Color.FromRgb(196, 210, 225)] = Color.FromRgb(53, 75, 103), // #C4D2E1 borders
        [Color.FromRgb(38, 59, 83)] = Color.FromRgb(223, 234, 248), // #263B53 text
        [Color.FromRgb(113, 129, 151)] = Color.FromRgb(181, 195, 212), // #718197 muted text
        [Color.FromRgb(109, 159, 208)] = Color.FromRgb(112, 167, 234), // #6D9FD0 accent
        [Color.FromRgb(228, 235, 243)] = Color.FromRgb(18, 35, 56),  // #E4EBF3 code
        [Color.FromRgb(248, 250, 253)] = Color.FromRgb(26, 46, 71),  // #F8FAFD inputs
        [Color.FromRgb(230, 237, 246)] = Color.FromRgb(33, 54, 80),  // #E6EDF6 tabs
        [Color.FromRgb(45, 69, 96)] = Color.FromRgb(215, 228, 244),   // #2D4560 secondary text
        [Color.FromRgb(49, 95, 145)] = Color.FromRgb(168, 201, 237),  // #315F91 section labels
        [Color.FromRgb(96, 117, 143)] = Color.FromRgb(164, 184, 208), // #60758F tab labels
        [Color.FromRgb(185, 203, 226)] = Color.FromRgb(62, 90, 120),  // #B9CBE2 splitters
        [Color.FromRgb(243, 247, 253)] = Color.FromRgb(9, 21, 37),
        [Color.FromRgb(255, 255, 255)] = Color.FromRgb(20, 36, 58),
        [Color.FromRgb(245, 248, 254)] = Color.FromRgb(23, 43, 69),
        [Color.FromRgb(220, 235, 255)] = Color.FromRgb(20, 45, 77),
        [Color.FromRgb(234, 242, 255)] = Color.FromRgb(28, 54, 87),
        [Color.FromRgb(232, 241, 255)] = Color.FromRgb(26, 53, 87),
        [Color.FromRgb(241, 245, 251)] = Color.FromRgb(26, 42, 66),
        [Color.FromRgb(247, 250, 255)] = Color.FromRgb(13, 27, 45),
        [Color.FromRgb(241, 234, 254)] = Color.FromRgb(43, 35, 68),
        [Color.FromRgb(227, 238, 255)] = Color.FromRgb(29, 56, 93),
        [Color.FromRgb(223, 232, 245)] = Color.FromRgb(42, 65, 95),
        [Color.FromRgb(217, 228, 242)] = Color.FromRgb(51, 73, 101),
        [Color.FromRgb(225, 234, 246)] = Color.FromRgb(41, 65, 93),
        [Color.FromRgb(226, 234, 245)] = Color.FromRgb(40, 64, 92),
        [Color.FromRgb(216, 226, 240)] = Color.FromRgb(45, 69, 98),
        [Color.FromRgb(185, 210, 244)] = Color.FromRgb(54, 90, 133),
        [Color.FromRgb(138, 184, 242)] = Color.FromRgb(85, 149, 233),
        [Color.FromRgb(113, 129, 155)] = Color.FromRgb(168, 185, 207),
        [Color.FromRgb(36, 70, 111)] = Color.FromRgb(225, 236, 250),
        [Color.FromRgb(23, 43, 77)] = Color.FromRgb(239, 245, 255),
        [Color.FromRgb(32, 54, 83)] = Color.FromRgb(220, 232, 247),
        [Color.FromRgb(36, 91, 158)] = Color.FromRgb(166, 204, 255),
        [Color.FromRgb(31, 94, 174)] = Color.FromRgb(154, 198, 255),
        [Color.FromRgb(55, 100, 157)] = Color.FromRgb(192, 217, 250),
        [Color.FromRgb(56, 81, 111)] = Color.FromRgb(189, 206, 226),
        [Color.FromRgb(82, 102, 129)] = Color.FromRgb(183, 200, 223),
        [Color.FromRgb(81, 65, 109)] = Color.FromRgb(219, 201, 255),
        [Color.FromRgb(112, 66, 200)] = Color.FromRgb(199, 168, 255),
        [Color.FromRgb(252, 235, 236)] = Color.FromRgb(72, 38, 50),
        [Color.FromRgb(182, 75, 87)] = Color.FromRgb(255, 173, 181),
        [Color.FromRgb(67, 132, 217)] = Color.FromRgb(110, 175, 255),
        [Color.FromRgb(53, 81, 111)] = Color.FromRgb(210, 225, 245),
        [Color.FromRgb(49, 91, 214)] = Color.FromRgb(119, 160, 255),
        [Color.FromRgb(32, 38, 51)] = Color.FromRgb(231, 239, 250),
        [Color.FromRgb(240, 240, 240)] = Color.FromRgb(26, 43, 67),
        [Color.FromRgb(40, 120, 229)] = Color.FromRgb(67, 142, 255)
    };

    private static readonly Dictionary<Color, Color> DarkToLightThemeColors =
        LightToDarkThemeColors.ToDictionary(pair => pair.Value, pair => pair.Key);

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
        { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
    private static readonly HashSet<string> ContainerTags = new(StringComparer.OrdinalIgnoreCase)
        { "html", "head", "body", "div", "main", "header", "footer", "nav", "section", "article", "aside", "hgroup", "search", "menu", "slot", "form", "ul", "ol", "dl", "table", "thead", "tbody", "tfoot", "tr", "select", "optgroup", "fieldset", "figure", "figcaption", "details", "dialog", "picture", "video", "audio", "blockquote", "datalist", "template", "noscript", "colgroup", "map" };

    public MainWindow()
    {
        InitializeComponent();
        BuildPalette();
        BuildCssDesigner();
        BuildJavaScriptBlocksEditor();
        BuildJavaScriptFunctionLibrary();
        RenderTree();
    }

    private void LibraryScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Give both category and element columns all remaining vertical space in the library.
        double listHeight = Math.Max(260, e.NewSize.Height - 390);
        HtmlPaletteCategoryList.Height = listHeight;
        PaletteList.Height = listHeight;
        CssPaletteCategoryList.Height = listHeight;
        CssPalettePropertyList.Height = listHeight;
        double jsListHeight = Math.Max(230, e.NewSize.Height - 470);
        if (JsFunctionCategoryList is not null) JsFunctionCategoryList.Height = jsListHeight;
        if (JsFunctionPalette is not null) JsFunctionPalette.Height = jsListHeight;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        bool nextThemeIsDark = !_isDarkTheme;
        if (ThemeIcon.RenderTransform is RotateTransform iconRotation)
        {
            var spin = new DoubleAnimation
            {
                From = iconRotation.Angle,
                To = iconRotation.Angle + 360,
                Duration = TimeSpan.FromMilliseconds(520),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            iconRotation.BeginAnimation(RotateTransform.AngleProperty, spin);
        }
        ApplyTheme(nextThemeIsDark);
        StatusText.Text = nextThemeIsDark ? "Темна тема увімкнена" : "Світла тема увімкнена";
    }

    private void ApplyTheme(bool dark)
    {
        _isDarkTheme = dark;
        var map = dark ? LightToDarkThemeColors : DarkToLightThemeColors;

        // Refresh shared brushes for controls created after the theme switch.
        foreach (object key in Application.Current.Resources.Keys.Cast<object>().ToList())
        {
            if (Application.Current.Resources[key] is SolidColorBrush brush && map.TryGetValue(brush.Color, out Color mapped))
                Application.Current.Resources[key] = new SolidColorBrush(mapped);
        }

        UpdateLayout();
        RecolorVisualTree(this, map);
        Background = new SolidColorBrush(dark ? Color.FromRgb(9, 21, 37) : Color.FromRgb(226, 235, 245));

        ThemeIcon.Text = dark ? "☾" : "☀";
        ThemeIcon.Foreground = new SolidColorBrush(dark ? Color.FromRgb(202, 218, 255) : Color.FromRgb(210, 151, 43));
        ThemeLabel.Text = dark ? "Темна" : "Світла";
        ThemeToggleButton.Background = new SolidColorBrush(dark ? Color.FromRgb(35, 58, 88) : Color.FromRgb(220, 232, 245));
        ThemeToggleButton.Foreground = new SolidColorBrush(dark ? Color.FromRgb(239, 245, 255) : Color.FromRgb(38, 59, 83));
        ScheduleThemeRefresh();
    }

    private static void RecolorVisualTree(DependencyObject root, Dictionary<Color, Color> map)
    {
        Brush? Recolor(Brush? brush, bool keepWhite = false)
        {
            if (brush is SolidColorBrush solid && !(keepWhite && solid.Color == Colors.White) && map.TryGetValue(solid.Color, out Color target))
                return new SolidColorBrush(target);
            return brush;
        }

        if (root is Control control)
        {
            control.SetCurrentValue(Control.BackgroundProperty, Recolor(control.Background));
            control.SetCurrentValue(Control.BorderBrushProperty, Recolor(control.BorderBrush));
            control.SetCurrentValue(Control.ForegroundProperty, Recolor(control.Foreground, keepWhite: true));
        }
        if (root is Border border)
        {
            border.SetCurrentValue(Border.BackgroundProperty, Recolor(border.Background));
            border.SetCurrentValue(Border.BorderBrushProperty, Recolor(border.BorderBrush));
        }
        if (root is Panel panel)
            panel.SetCurrentValue(Panel.BackgroundProperty, Recolor(panel.Background));
        if (root is TextBlock textBlock)
            textBlock.SetCurrentValue(TextBlock.ForegroundProperty, Recolor(textBlock.Foreground, keepWhite: true));
        if (root is Shape shape)
        {
            shape.SetCurrentValue(Shape.FillProperty, Recolor(shape.Fill, keepWhite: true));
            shape.SetCurrentValue(Shape.StrokeProperty, Recolor(shape.Stroke));
        }

        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
            RecolorVisualTree(VisualTreeHelper.GetChild(root, i), map);
    }

    private void UniformScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        if (ScrollWheelInScrollableArea(e.OriginalSource as DependencyObject, e.Delta))
            e.Handled = true;
    }

    // Shared by every tab and nested list: wheel motion is measured in physical pixels,
    // rather than logical item counts. At a nested viewer's edge, the unused movement
    // continues into its parent (for example, from the HTML cards to the library panel).
    private static bool ScrollWheelInScrollableArea(DependencyObject? source, int delta)
    {
        if (source is null || delta == 0) return false;

        var scrollViewers = new List<ScrollViewer>();
        var visited = new HashSet<DependencyObject>();
        DependencyObject? current = source;
        while (current is not null && visited.Add(current))
        {
            if (current is ScrollViewer scrollViewer)
                scrollViewers.Add(scrollViewer);

            current = GetParentElement(current);
        }

        if (scrollViewers.Count == 0) return false;

        // 32 px is approximately two text lines: responsive but not jumpy.
        double notches = Math.Clamp(delta / 120.0, -1.0, 1.0);
        double remainingMovement = -notches * 32.0;
        if (Math.Abs(remainingMovement) < 0.1) return false;

        foreach (ScrollViewer scrollViewer in scrollViewers)
        {
            // Keep the thumb live and offsets in pixels, including ListBox template viewers.
            ScrollViewer.SetCanContentScroll(scrollViewer, false);
            ScrollViewer.SetIsDeferredScrollingEnabled(scrollViewer, false);

            double before = scrollViewer.VerticalOffset;
            double desired = before + remainingMovement;
            double target = Math.Clamp(desired, 0.0, Math.Max(0.0, scrollViewer.ScrollableHeight));
            double consumedMovement = target - before;

            if (Math.Abs(consumedMovement) > 0.1)
            {
                scrollViewer.ScrollToVerticalOffset(target);
                remainingMovement -= consumedMovement;
            }

            if (Math.Abs(remainingMovement) < 0.1)
                break;
        }

        return true;
    }

    private static DependencyObject? GetParentElement(DependencyObject element)
    {
        try
        {
            DependencyObject? visualParent = VisualTreeHelper.GetParent(element);
            if (visualParent is not null) return visualParent;
        }
        catch (InvalidOperationException) { }
        catch (ArgumentException) { }

        return LogicalTreeHelper.GetParent(element);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            T? nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private void InspectorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged also bubbles from ComboBox/ListBox controls inside a tab.
        // Only refresh when the actual inspector tab changes.
        if (!ReferenceEquals(e.OriginalSource, InspectorTabs)) return;
        ScheduleThemeRefresh();
    }

    private void ScheduleThemeRefresh()
    {
        if (!IsInitialized || _themeRefreshPending) return;
        _themeRefreshPending = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _themeRefreshPending = false;
            if (!IsLoaded) return;
            UpdateLayout();
            RecolorVisualTree(this, _isDarkTheme ? LightToDarkThemeColors : DarkToLightThemeColors);
        }));
    }

    private static readonly (string Name, string Color)[] HtmlPaletteCategories =
    {
        ("Усі елементи", "#4C97FF"), ("Структура", "#FF8C1A"), ("Текст", "#9966FF"),
        ("Медіа", "#0FBD8C"), ("Списки", "#59A65A"), ("Форми", "#FF6680"),
        ("Таблиці", "#CF63CF"), ("Інтерактивні", "#D9A600"), ("Графіка й службові", "#64748B")
    };

    private void BuildPalette()
    {
        _allBlocks.AddRange(new[] {
            "div", "header", "nav", "main", "section", "article", "aside", "footer", "address",
            "h1", "h2", "h3", "h4", "h5", "h6", "hgroup", "p", "span", "strong", "em", "b", "i", "u", "s", "small", "mark", "code", "pre", "blockquote", "q", "dfn",
            "a", "img", "picture", "video", "audio", "source", "iframe", "slot",
            "ul", "ol", "menu", "li", "dl", "dt", "dd",
            "button", "input", "textarea", "select", "option", "label", "form", "fieldset", "legend",
            "table", "thead", "tbody", "tr", "th", "td", "caption",
            "hr", "br", "figure", "figcaption", "details", "summary", "progress", "meter", "time", "script", "style", "meta", "link", "title", "head", "body", "html", "canvas", "svg", "path", "noscript", "template", "dialog", "search", "abbr", "cite", "del", "ins", "sub", "sup", "kbd", "samp", "var", "wbr", "col", "colgroup", "tfoot", "datalist", "output", "optgroup", "object", "embed", "track", "map", "area", "ruby", "rt", "rp", "bdi", "bdo", "data"
        });
        HtmlPaletteCategoryList.ItemsSource = HtmlPaletteCategories
            .Select(c => new CssPaletteCategory(c.Name, c.Color, c.Name)).ToList();
        HtmlPaletteCategoryList.SelectedIndex = 0;
        RefreshPalette();
    }

    private static string GetTagCategory(string tag)
    {
        tag = tag.ToLowerInvariant();
        if (new[] { "html", "head", "body", "header", "nav", "main", "section", "article", "aside", "footer", "div", "address", "hgroup", "search" }.Contains(tag)) return "Структура";
        if (new[] { "h1", "h2", "h3", "h4", "h5", "h6", "p", "span", "strong", "em", "b", "i", "u", "s", "small", "mark", "code", "pre", "blockquote", "q", "dfn", "abbr", "cite", "del", "ins", "sub", "sup", "kbd", "samp", "var", "bdi", "bdo", "ruby", "rt", "rp", "data" }.Contains(tag)) return "Текст";
        if (new[] { "a", "img", "picture", "video", "audio", "source", "iframe", "figure", "figcaption", "track", "map", "area", "object", "embed" }.Contains(tag)) return "Медіа";
        if (new[] { "ul", "ol", "menu", "li", "dl", "dt", "dd" }.Contains(tag)) return "Списки";
        if (new[] { "button", "input", "textarea", "select", "option", "label", "form", "fieldset", "legend", "datalist", "output", "optgroup" }.Contains(tag)) return "Форми";
        if (new[] { "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "col", "colgroup" }.Contains(tag)) return "Таблиці";
        if (new[] { "details", "summary", "progress", "meter", "time", "dialog" }.Contains(tag)) return "Інтерактивні";
        return "Графіка й службові";
    }

    private void HtmlPaletteCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;
        // Rebuilding the element list after a category click can make the parent
        // ScrollViewer jump. Preserve the outer position while resetting only the item list.
        double libraryOffset = LibraryScrollViewer?.VerticalOffset ?? 0;
        RefreshPalette(preserveViewState: false);
        Dispatcher.BeginInvoke(new Action(() => LibraryScrollViewer?.ScrollToVerticalOffset(
            Math.Min(libraryOffset, LibraryScrollViewer.ScrollableHeight))),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void HtmlPaletteElement_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaletteList.SelectedItem is not ListBoxItem item || item.Tag is not string tag) return;
        HtmlPaletteHintText.Text = TagDescriptions.TryGetValue(tag, out var description)
            ? $"<{tag}> — {description}"
            : $"<{tag}> — HTML-елемент для побудови вебсторінки.";
    }

    private void RefreshPalette(bool preserveViewState = false)
    {
        double previousOffset = 0;
        string? previousSelection = null;
        if (preserveViewState)
        {
            previousOffset = FindVisualChild<ScrollViewer>(PaletteList)?.VerticalOffset ?? 0;
            previousSelection = (PaletteList.SelectedItem as ListBoxItem)?.Tag as string;
        }

        string filter = SearchBox?.Text?.Trim() ?? "";
        string category = (HtmlPaletteCategoryList?.SelectedItem as CssPaletteCategory)?.Name ?? "Усі елементи";
        PaletteList.Items.Clear();
        var filtered = _allBlocks.Where(tag =>
            (category == "Усі елементи" || GetTagCategory(tag) == category) &&
            (tag.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             (TagDescriptions.TryGetValue(tag, out var desc) && desc.Contains(filter, StringComparison.OrdinalIgnoreCase))));

        var tags = filtered.ToList();
        HtmlElementCountText.Text = $"{tags.Count} ЕЛЕМЕНТІВ";
        foreach (string tag in tags)
        {
            string description = TagDescriptions.TryGetValue(tag, out var text) ? text : "HTML-елемент для побудови сторінки.";
            var card = new StackPanel { Orientation = Orientation.Vertical };
            card.Children.Add(new TextBlock { Text = $"<{tag}>", FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = new SolidColorBrush(_isDarkTheme ? Color.FromRgb(168, 201, 237) : Color.FromRgb(49, 95, 145)) });
            card.Children.Add(new TextBlock { Text = description, FontSize = 10, Foreground = new SolidColorBrush(_isDarkTheme ? Color.FromRgb(181, 195, 212) : Color.FromRgb(113, 129, 151)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0), MaxHeight = 72 });
            var item = new ListBoxItem
            {
                Content = card, Tag = tag, Padding = new Thickness(11, 9, 11, 9), Margin = new Thickness(2, 2, 2, 5),
                Background = new SolidColorBrush(_isDarkTheme ? Color.FromRgb(16, 32, 57) : Color.FromRgb(237, 242, 248)), Foreground = new SolidColorBrush(_isDarkTheme ? Color.FromRgb(223, 234, 248) : Color.FromRgb(38, 59, 83)), BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(_isDarkTheme ? Color.FromRgb(53, 75, 103) : Color.FromRgb(196, 210, 225)), HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = BuildTagToolTip(tag)
            };
            PaletteList.Items.Add(item);
        }

        bool selectionRestored = false;
        if (preserveViewState && !string.IsNullOrEmpty(previousSelection))
        {
            var selected = PaletteList.Items.OfType<ListBoxItem>().FirstOrDefault(item => string.Equals(item.Tag as string, previousSelection, StringComparison.OrdinalIgnoreCase));
            if (selected is not null)
            {
                PaletteList.SelectedItem = selected;
                selectionRestored = true;
            }
        }
        if (!selectionRestored)
            HtmlPaletteHintText.Text = "Вибери HTML-елемент зі списку, щоб побачити його призначення.";

        ScheduleThemeRefresh();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var scroll = FindVisualChild<ScrollViewer>(PaletteList);
            if (scroll is null) return;
            if (preserveViewState) scroll.ScrollToVerticalOffset(Math.Min(previousOffset, scroll.ScrollableHeight));
            else scroll.ScrollToTop();
        }));
    }

    private static string BuildTagToolTip(string tag)
    {
        string description = TagDescriptions.TryGetValue(tag, out var text)
            ? text
            : "HTML-елемент для створення частини вебсторінки. Подвійний клік додає його на полотно.";
        return $"<{tag}>\n{description}\n\nПодвійний клік — додати | Перетягування — розмістити на полотні";
    }

    private static readonly Dictionary<string, string> TagDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["html"] = "Кореневий елемент HTML-документа. Містить head і body.",
        ["head"] = "Службова частина сторінки: назва, метадані, стилі та підключення файлів.",
        ["body"] = "Уся видима частина вебсторінки: текст, зображення, кнопки та інші елементи.",
        ["title"] = "Назва сторінки, яка показується у вкладці браузера.",
        ["meta"] = "Метадані сторінки: кодування, масштаб, опис для пошукових систем тощо.",
        ["link"] = "Підключає зовнішній ресурс, найчастіше CSS-файл або іконку сайту.",
        ["style"] = "Містить CSS-правила для оформлення сторінки.",
        ["script"] = "Додає JavaScript-код, який робить сторінку інтерактивною.",
        ["div"] = "Універсальний блок-контейнер для групування елементів і побудови макета.",
        ["header"] = "Верхня частина сторінки або секції: логотип, назва, вступ.",
        ["nav"] = "Блок навігації з посиланнями на розділи або сторінки сайту.",
        ["main"] = "Основний унікальний вміст сторінки. Зазвичай використовується один раз.",
        ["section"] = "Тематичний розділ сторінки, часто із власним заголовком.",
        ["article"] = "Самостійний матеріал: стаття, новина, допис або картка публікації.",
        ["aside"] = "Додатковий або боковий вміст: сайдбар, примітки, рекомендації.",
        ["footer"] = "Нижня частина сторінки або секції: контакти, авторство, копірайт.",
        ["address"] = "Контактна інформація автора або власника найближчого розділу.",
        ["hgroup"] = "Групує заголовок і пов'язаний із ним підзаголовок або додатковий текст.",
        ["search"] = "Позначає область сторінки, яка містить форму або інструменти пошуку.",
        ["menu"] = "Список команд або дій. Може бути оформлений як звичайний список.",
        ["slot"] = "Місце для вставлення вмісту у Web Component; також може мати резервний вміст.",
        ["q"] = "Позначає коротку вбудовану цитату; браузер зазвичай додає лапки.",
        ["dfn"] = "Позначає термін, який визначається в поточному тексті.",
        ["s"] = "Позначає текст, який більше не є актуальним або правильним.",
        ["h1"] = "Головний заголовок сторінки. Зазвичай найважливіший і використовується один раз.",
        ["h2"] = "Заголовок великого розділу сторінки.", ["h3"] = "Заголовок підрозділу.",
        ["h4"] = "Заголовок четвертого рівня.", ["h5"] = "Заголовок п’ятого рівня.", ["h6"] = "Заголовок шостого рівня — найнижчий рівень заголовків.",
        ["p"] = "Звичайний абзац тексту.", ["span"] = "Маленький рядковий контейнер для частини тексту або іншого рядкового вмісту.",
        ["strong"] = "Позначає важливий текст; зазвичай браузер показує його жирним.",
        ["b"] = "Візуально виділяє текст жирним без додаткового значення важливості.",
        ["em"] = "Наголошує на слові або фразі; зазвичай показується курсивом.",
        ["i"] = "Виділяє текст курсивом, наприклад термін або альтернативний голос.",
        ["u"] = "Позначає фрагмент тексту з підкресленням.", ["small"] = "Показує примітки або другорядний текст меншим шрифтом.",
        ["mark"] = "Підсвічує фрагмент тексту як маркером.", ["code"] = "Позначає короткий фрагмент програмного коду.",
        ["pre"] = "Показує попередньо форматований текст зі збереженням пробілів і переносів.",
        ["blockquote"] = "Оформлює довгу цитату з іншого джерела.",
        ["a"] = "Створює посилання на іншу сторінку, файл або адресу. Для адреси заповни href.",
        ["img"] = "Вставляє зображення. Для файлу або URL заповни src, а для опису — alt.",
        ["picture"] = "Дозволяє браузеру вибирати різні версії зображення для різних екранів.",
        ["video"] = "Додає відеоплеєр на сторінку.", ["audio"] = "Додає аудіоплеєр для відтворення звуку.",
        ["source"] = "Вказує альтернативний файл медіа для video, audio або picture.",
        ["iframe"] = "Вбудовує іншу вебсторінку або зовнішній контент у поточну.",
        ["ul"] = "Створює маркований список.", ["ol"] = "Створює нумерований список.",
        ["li"] = "Один пункт списку ul або ol.", ["dl"] = "Список термінів та їхніх описів.",
        ["dt"] = "Термін або назва у списку описів dl.", ["dd"] = "Опис терміна у списку dl.",
        ["button"] = "Створює кнопку, на яку користувач може натиснути.",
        ["input"] = "Поле введення: текст, email, пароль, число, прапорець та інші типи.",
        ["textarea"] = "Багаторядкове поле для введення довгого тексту.",
        ["select"] = "Випадаючий список варіантів.", ["option"] = "Один варіант усередині select або datalist.",
        ["label"] = "Підпис поля форми; пов’язується з input через for та id.",
        ["form"] = "Об’єднує поля введення та кнопки для надсилання даних.",
        ["fieldset"] = "Групує пов’язані поля форми в один блок.", ["legend"] = "Назва групи полів усередині fieldset.",
        ["table"] = "Створює таблицю для структурованих рядків і стовпців.",
        ["thead"] = "Група заголовкових рядків таблиці.", ["tbody"] = "Основна група рядків таблиці.",
        ["tfoot"] = "Підсумкова частина таблиці.", ["tr"] = "Один рядок таблиці.",
        ["th"] = "Комірка-заголовок таблиці.", ["td"] = "Звичайна комірка таблиці.",
        ["caption"] = "Назва або короткий опис таблиці.", ["hr"] = "Тематичний розділювач між частинами вмісту.",
        ["br"] = "Переносить текст на новий рядок.", ["figure"] = "Групує ілюстрацію, схему, фото або приклад із підписом.",
        ["figcaption"] = "Підпис до вмісту figure.", ["details"] = "Створює блок, який можна розгортати та згортати.",
        ["summary"] = "Видимий заголовок-кнопка для блоку details.",
        ["progress"] = "Показує прогрес виконання завдання.", ["meter"] = "Показує значення в межах відомого діапазону, наприклад рівень.",
        ["time"] = "Позначає дату або час у машинозчитуваному форматі.",
        ["canvas"] = "Полотно для малювання графіки через JavaScript.", ["svg"] = "Контейнер для векторної графіки.",
        ["path"] = "Описує лінії та фігури всередині SVG.", ["dialog"] = "Створює діалогове вікно або модальне повідомлення.",
        ["abbr"] = "Позначає скорочення; title може містити повне пояснення.",
        ["cite"] = "Позначає назву твору або джерела.", ["del"] = "Позначає видалений фрагмент тексту.",
        ["ins"] = "Позначає доданий фрагмент тексту.", ["sub"] = "Показує нижній індекс, наприклад H₂O.",
        ["sup"] = "Показує верхній індекс, наприклад x².", ["kbd"] = "Позначає клавішу або комбінацію клавіш.",
        ["samp"] = "Позначає приклад результату роботи програми.", ["var"] = "Позначає змінну у математичному або програмному тексті.",
        ["wbr"] = "Позначає місце, де браузер може перенести довге слово.",
        ["datalist"] = "Надає підказки-варіанти для поля input.", ["output"] = "Показує результат обчислення або дії форми.",
        ["optgroup"] = "Групує варіанти всередині випадаючого списку.",
        ["object"] = "Вбудовує зовнішній ресурс, наприклад документ або медіа.",
        ["embed"] = "Вбудовує зовнішній інтерактивний ресурс.", ["track"] = "Додає субтитри або текстові доріжки до video чи audio.",
        ["map"] = "Створює карту клікабельних областей зображення.", ["area"] = "Описує клікабельну область усередині map.",
        ["col"] = "Задає властивості одного або кількох стовпців таблиці.",
        ["colgroup"] = "Групує стовпці таблиці для спільного форматування.",
        ["noscript"] = "Показує альтернативний вміст, якщо JavaScript вимкнений.",
        ["template"] = "Зберігає HTML-шаблон, який JavaScript може клонувати пізніше.",
        ["ruby"] = "Додає вимову або пояснення над/поруч із символами, часто для азійських мов.",
        ["rt"] = "Текст вимови всередині ruby.", ["rp"] = "Резервні дужки для браузерів без підтримки ruby.",
        ["bdi"] = "Ізолює напрямок тексту, щоб він не впливав на сусідній текст.",
        ["bdo"] = "Примусово задає напрямок відображення тексту.",
        ["data"] = "Пов’язує видимий текст із машинозчитуваним значенням."
    };

    private static readonly Dictionary<string, string[]> CssPropertyGroups = new()
    {
        ["Layout · Розташування"] = new[] { "display", "position", "top", "right", "bottom", "left", "inset", "z-index", "float", "clear", "overflow", "overflow-x", "overflow-y", "visibility", "isolation", "box-sizing", "anchor-name", "anchor-scope", "position-anchor", "position-area", "position-try", "position-try-fallbacks", "position-try-order", "position-visibility" },
        ["Flexbox"] = new[] { "flex", "flex-basis", "flex-direction", "flex-flow", "flex-grow", "flex-shrink", "flex-wrap", "order", "justify-content", "align-items", "align-content", "align-self", "gap", "row-gap", "column-gap" },
        ["Grid"] = new[] { "grid", "grid-template", "grid-template-columns", "grid-template-rows", "grid-template-areas", "grid-auto-columns", "grid-auto-rows", "grid-auto-flow", "grid-column", "grid-column-start", "grid-column-end", "grid-row", "grid-row-start", "grid-row-end", "grid-area", "justify-items", "justify-self", "place-items", "place-content", "place-self" },
        ["Розміри та відступи"] = new[] { "width", "height", "min-width", "max-width", "min-height", "max-height", "margin", "margin-top", "margin-right", "margin-bottom", "margin-left", "padding", "padding-top", "padding-right", "padding-bottom", "padding-left", "aspect-ratio" },
        ["Typography · Текст"] = new[] { "color", "font", "font-family", "font-size", "font-style", "font-weight", "font-variant", "line-height", "letter-spacing", "word-spacing", "text-align", "text-align-last", "text-decoration", "text-decoration-color", "text-decoration-line", "text-decoration-style", "text-indent", "text-transform", "text-shadow", "text-overflow", "text-wrap", "white-space", "word-break", "overflow-wrap", "hyphens", "vertical-align", "direction", "writing-mode", "font-stretch", "font-kerning", "font-optical-sizing", "font-synthesis", "font-synthesis-weight", "font-synthesis-style", "font-synthesis-small-caps", "font-variant-caps", "font-variant-ligatures", "font-variant-numeric", "font-variant-east-asian", "text-decoration-thickness", "text-decoration-skip-ink", "text-underline-offset", "text-emphasis", "text-emphasis-color", "text-emphasis-style", "text-emphasis-position", "text-wrap-mode", "text-wrap-style", "line-clamp", "hyphenate-character", "text-rendering", "text-size-adjust", "initial-letter", "hanging-punctuation" },
        ["Background · Фон"] = new[] { "background", "background-color", "background-image", "background-position", "background-size", "background-repeat", "background-attachment", "background-origin", "background-clip", "background-blend-mode", "background-position-x", "background-position-y", "background-repeat-x", "background-repeat-y" },
        ["Borders · Рамки"] = new[] { "border", "border-top", "border-right", "border-bottom", "border-left", "border-width", "border-style", "border-color", "border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-left-radius", "border-bottom-right-radius", "outline", "outline-color", "outline-style", "outline-width", "outline-offset", "box-shadow" },
        ["Effects · Ефекти"] = new[] { "opacity", "filter", "backdrop-filter", "mix-blend-mode", "clip-path", "mask", "mask-image", "object-fit", "object-position", "transform", "transform-origin", "transform-style", "perspective", "perspective-origin", "backface-visibility", "rotate", "scale", "translate", "transform-box", "shape-outside", "shape-margin", "shape-image-threshold", "zoom", "interpolate-size" },
        ["Animation · Анімації"] = new[] { "transition", "transition-property", "transition-duration", "transition-timing-function", "transition-delay", "animation", "animation-name", "animation-duration", "animation-timing-function", "animation-delay", "animation-iteration-count", "animation-direction", "animation-fill-mode", "animation-play-state", "animation-composition", "transition-behavior", "animation-timeline", "animation-range", "animation-range-start", "animation-range-end" },
        ["Responsive · Адаптивність"] = new[] { "container", "container-name", "container-type", "contain", "content-visibility", "view-transition-name", "view-transition-class", "contain-intrinsic-size", "contain-intrinsic-width", "contain-intrinsic-height", "contain-intrinsic-block-size", "contain-intrinsic-inline-size" },
        ["Lists · Таблиці та списки"] = new[] { "list-style", "list-style-type", "list-style-position", "list-style-image", "border-collapse", "border-spacing", "table-layout", "caption-side", "empty-cells" },
        ["Scroll · Прокрутка"] = new[] { "scroll-behavior", "scroll-snap-type", "scroll-snap-align", "scroll-snap-stop", "scroll-margin", "scroll-padding", "overscroll-behavior", "scrollbar-color", "scrollbar-width", "scrollbar-gutter", "scroll-margin-top", "scroll-margin-right", "scroll-margin-bottom", "scroll-margin-left", "scroll-padding-top", "scroll-padding-right", "scroll-padding-bottom", "scroll-padding-left", "overscroll-behavior-block", "overscroll-behavior-inline", "scroll-timeline", "scroll-timeline-name", "scroll-timeline-axis", "view-timeline", "view-timeline-name", "view-timeline-axis", "view-timeline-inset" },
        ["Інше / CSS Variables"] = new[] { "--custom-property", "all", "appearance", "accent-color", "caret-color", "cursor", "pointer-events", "user-select", "resize", "content", "counter-reset", "counter-increment", "quotes", "will-change", "touch-action", "color-scheme", "font-feature-settings", "font-variation-settings", "field-sizing", "forced-color-adjust", "print-color-adjust" },
        ["Логічні властивості"] = new[] { "margin-block", "margin-block-start", "margin-block-end", "margin-inline", "margin-inline-start", "margin-inline-end", "padding-block", "padding-block-start", "padding-block-end", "padding-inline", "padding-inline-start", "padding-inline-end", "inset-block", "inset-block-start", "inset-block-end", "inset-inline", "inset-inline-start", "inset-inline-end", "border-block", "border-block-color", "border-block-style", "border-block-width", "border-block-start", "border-block-start-color", "border-block-start-style", "border-block-start-width", "border-block-end", "border-block-end-color", "border-block-end-style", "border-block-end-width", "border-inline", "border-inline-color", "border-inline-style", "border-inline-width", "border-inline-start", "border-inline-start-color", "border-inline-start-style", "border-inline-start-width", "border-inline-end", "border-inline-end-color", "border-inline-end-style", "border-inline-end-width", "border-start-start-radius", "border-start-end-radius", "border-end-start-radius", "border-end-end-radius", "inline-size", "block-size", "min-inline-size", "max-inline-size", "min-block-size", "max-block-size" }
    };

    private void BuildCssDesigner()
    {
        CssCategoryBox.ItemsSource = CssPropertyGroups.Keys.ToList();
        CssCategoryBox.SelectedIndex = 0;
        CssBlocksList.ItemsSource = _cssDeclarations;

        var paletteCategories = new[]
        {
            new CssPaletteCategory("Розташування", "#4C97FF", "Layout · Розташування"),
            new CssPaletteCategory("Flexbox", "#0FBD8C", "Flexbox"),
            new CssPaletteCategory("Сітка Grid", "#9966FF", "Grid"),
            new CssPaletteCategory("Розміри й відступи", "#FF8C1A", "Розміри та відступи"),
            new CssPaletteCategory("Текст", "#CF63CF", "Typography · Текст"),
            new CssPaletteCategory("Фон", "#D9A600", "Background · Фон"),
            new CssPaletteCategory("Рамки й тіні", "#E64C6A", "Borders · Рамки"),
            new CssPaletteCategory("Ефекти", "#D65CD6", "Effects · Ефекти"),
            new CssPaletteCategory("Анімації", "#59A65A", "Animation · Анімації"),
            new CssPaletteCategory("Адаптивність", "#18A6A6", "Responsive · Адаптивність"),
            new CssPaletteCategory("Списки й таблиці", "#8B8B8B", "Lists · Таблиці та списки"),
            new CssPaletteCategory("Прокрутка", "#527A9E", "Scroll · Прокрутка"),
            new CssPaletteCategory("Логічні властивості", "#6685A3", "Логічні властивості"),
            new CssPaletteCategory("Інше та змінні", "#64748B", "Інше / CSS Variables")
        };
        CssPaletteCategoryList.ItemsSource = paletteCategories;
        CssPaletteCategoryList.SelectedIndex = 0;
        RefreshCssProperties();
    }

    private void EditorMode_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || HtmlLibraryPanel is null) return;
        bool cssMode = CssModeButton.IsChecked == true;
        bool jsMode = JsModeButton.IsChecked == true;
        HtmlLibraryPanel.Visibility = (!cssMode && !jsMode) ? Visibility.Visible : Visibility.Collapsed;
        CssLibraryPanel.Visibility = cssMode ? Visibility.Visible : Visibility.Collapsed;
        JsLibraryPanel.Visibility = jsMode ? Visibility.Visible : Visibility.Collapsed;
        if (InspectorTabs is not null)
            InspectorTabs.SelectedIndex = cssMode ? 1 : jsMode ? 2 : 0;
        if (jsMode && JavaScriptEditorTabs is not null)
            JavaScriptEditorTabs.SelectedIndex = 0;
        LibraryScrollViewer.ScrollToTop();
        StatusText.Text = cssMode
            ? "CSS-режим · обери категорію та властивість"
            : jsMode ? "JavaScript · вибери готову функцію або відкрий візуальні блоки"
            : "HTML-режим · перетягуй елементи на полотно";
        ScheduleThemeRefresh();
    }

    private void CssPaletteCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CssPaletteCategoryList.SelectedItem is not CssPaletteCategory category) return;
        if (!CssPropertyGroups.TryGetValue(category.GroupKey, out var properties)) return;
        CssPalettePropertyList.ItemsSource = properties.Select(CreateCssPaletteItem).ToList();
        if (CssPalettePropertyList.Items.Count > 0) CssPalettePropertyList.SelectedIndex = 0;
        Dispatcher.BeginInvoke(new Action(() => FindVisualChild<ScrollViewer>(CssPalettePropertyList)?.ScrollToTop()));
        ScheduleThemeRefresh();
    }

    private static CssPaletteItem CreateCssPaletteItem(string property)
    {
        var descriptions = new Dictionary<string, (string Name, string Hint, string Value)>(StringComparer.OrdinalIgnoreCase)
        {
            ["display"] = ("Як показувати елемент", "Визначає, як елемент займає місце: звичайний блок, Flex, Grid або прихований.", "block"),
            ["position"] = ("Спосіб розташування", "Визначає, чи елемент стоїть у звичайному потоці, закріплений або розміщений точно.", "relative"),
            ["justify-content"] = ("Розподілити по горизонталі", "Розміщує дочірні елементи вздовж головної осі Flex або Grid.", "center"),
            ["align-items"] = ("Вирівняти елементи", "Вирівнює дочірні елементи поперек головної осі.", "center"),
            ["flex-direction"] = ("Напрямок елементів", "Розташовує елементи в рядок або стовпчик.", "row"),
            ["flex-wrap"] = ("Переносити на новий рядок", "Дозволяє елементам переходити на наступний рядок, якщо не вистачає місця.", "wrap"),
            ["gap"] = ("Відстань між елементами", "Задає проміжок між дочірніми елементами.", "12px"),
            ["grid-template-columns"] = ("Кількість колонок Grid", "Визначає колонки сітки. Наприклад repeat(3, 1fr) створює три однакові колонки.", "repeat(3, 1fr)"),
            ["width"] = ("Ширина", "Задає ширину елемента. Можна використовувати px, %, vw або auto.", "100%"),
            ["height"] = ("Висота", "Задає висоту елемента.", "auto"),
            ["margin"] = ("Зовнішній відступ", "Відстань зовні елемента — між ним та сусідніми елементами.", "12px"),
            ["padding"] = ("Внутрішній відступ", "Відстань між вмістом елемента та його краями.", "12px"),
            ["color"] = ("Колір тексту", "Змінює колір тексту. Наприклад #222222 або blue.", "#222222"),
            ["font-size"] = ("Розмір тексту", "Задає розмір шрифту в px, rem або інших одиницях.", "16px"),
            ["font-weight"] = ("Товщина тексту", "Робить текст звичайним, напівжирним або жирним.", "600"),
            ["text-align"] = ("Вирівнювання тексту", "Вирівнює текст ліворуч, по центру або праворуч.", "center"),
            ["background-color"] = ("Колір фону", "Змінює колір заднього плану елемента.", "#EAF2FF"),
            ["background-image"] = ("Зображення або градієнт фону", "Додає картинку або градієнт на фон.", "linear-gradient(135deg, #2878e5, #8b5cf6)"),
            ["border"] = ("Рамка навколо елемента", "Задає товщину, тип і колір рамки.", "1px solid #D8E2F0"),
            ["border-radius"] = ("Заокруглення кутів", "Робить кути елемента округлими.", "12px"),
            ["box-shadow"] = ("Тінь елемента", "Додає тінь навколо блока.", "0 4px 12px #00000022"),
            ["opacity"] = ("Прозорість", "0 — повністю прозорий, 1 — повністю видимий.", "1"),
            ["transform"] = ("Повернути або змінити розмір", "Дозволяє повернути, перемістити або масштабувати елемент.", "scale(1.05)"),
            ["transition"] = ("Плавність змін", "Робить зміну стилю плавною, наприклад під час наведення курсора.", "all .2s ease"),
            ["animation"] = ("Запустити анімацію", "Застосовує CSS-анімацію, описану через @keyframes.", "none"),
            ["cursor"] = ("Вигляд курсора", "Змінює курсор миші над елементом.", "pointer"),
            ["overflow"] = ("Що робити з вмістом за межами", "Визначає, чи обрізати, показувати або прокручувати вміст, що не помістився.", "hidden"),
            ["z-index"] = ("Порядок шарів", "Визначає, який елемент буде поверх іншого.", "1"),
            ["max-width"] = ("Максимальна ширина", "Обмежує ширину елемента, наприклад для адаптивного контейнера.", "100%"),
            ["min-height"] = ("Мінімальна висота", "Задає висоту, нижче якої елемент не зменшуватиметься.", "100px"),
            ["object-fit"] = ("Як вписати зображення", "Визначає, як зображення заповнює заданий прямокутник.", "cover"),
            ["scroll-behavior"] = ("Плавна прокрутка", "Вмикає плавний перехід під час прокручування до якоря.", "smooth"),
            ["--custom-property"] = ("Власна CSS-змінна", "Створи власне значення, яке можна повторно використовувати через var(--назва).", "#2878e5")
        };
        if (descriptions.TryGetValue(property, out var info)) return new CssPaletteItem(info.Name, property, info.Hint, info.Value);
        return new CssPaletteItem(GetCssPropertyTitle(property), property,
            DescribeCssProperty(property), GetCssDefaultValue(property));
    }

    private static string GetCssPropertyTitle(string property)
    {
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["top"] = "Відступ зверху", ["right"] = "Відступ справа", ["bottom"] = "Відступ знизу", ["left"] = "Відступ зліва",
            ["inset"] = "Позиція з усіх боків", ["z-index"] = "Порядок шарів", ["float"] = "Обтікання елемента", ["clear"] = "Скасувати обтікання",
            ["isolation"] = "Окремий шар накладання", ["box-sizing"] = "Спосіб розрахунку розміру",
            ["flex-basis"] = "Початковий розмір Flex", ["flex-flow"] = "Напрямок і перенос Flex", ["flex-grow"] = "Зростання Flex", ["flex-shrink"] = "Стиснення Flex",
            ["order"] = "Порядок елемента", ["row-gap"] = "Відстань між рядками", ["column-gap"] = "Відстань між колонками",
            ["grid-template"] = "Шаблон Grid", ["grid-template-rows"] = "Рядки Grid", ["grid-template-areas"] = "Іменовані області Grid",
            ["grid-auto-columns"] = "Автоматичні колонки Grid", ["grid-auto-rows"] = "Автоматичні рядки Grid", ["grid-auto-flow"] = "Автоматичне розміщення Grid",
            ["grid-column"] = "Позиція в колонках", ["grid-column-start"] = "Початок колонки", ["grid-column-end"] = "Кінець колонки",
            ["grid-row"] = "Позиція в рядках", ["grid-row-start"] = "Початок рядка", ["grid-row-end"] = "Кінець рядка", ["grid-area"] = "Область Grid",
            ["justify-items"] = "Вирівняти вміст комірок", ["justify-self"] = "Вирівняти один елемент", ["place-items"] = "Вирівняти всі комірки",
            ["place-content"] = "Розмістити всю сітку", ["place-self"] = "Вирівнювання елемента",
            ["min-width"] = "Мінімальна ширина", ["max-width"] = "Максимальна ширина", ["min-height"] = "Мінімальна висота", ["max-height"] = "Максимальна висота",
            ["aspect-ratio"] = "Пропорції елемента", ["font-family"] = "Тип шрифту", ["font-size"] = "Розмір шрифту", ["font-style"] = "Стиль шрифту",
            ["font-weight"] = "Товщина шрифту", ["font-variant"] = "Варіант шрифту", ["line-height"] = "Висота рядка", ["letter-spacing"] = "Відстань між літерами",
            ["word-spacing"] = "Відстань між словами", ["text-align-last"] = "Вирівнювання останнього рядка", ["text-indent"] = "Відступ першого рядка",
            ["text-transform"] = "Зміна регістру", ["text-shadow"] = "Тінь тексту", ["text-overflow"] = "Обрізання довгого тексту",
            ["text-wrap"] = "Перенесення тексту", ["white-space"] = "Обробка пробілів", ["word-break"] = "Перенос довгих слів",
            ["overflow-wrap"] = "Переносити довгі слова", ["vertical-align"] = "Вертикальне вирівнювання", ["writing-mode"] = "Напрямок письма",
            ["background-attachment"] = "Закріплення фону", ["background-origin"] = "Початок фону", ["background-clip"] = "Область малювання фону",
            ["background-blend-mode"] = "Змішування фонів", ["border-collapse"] = "Об’єднання меж таблиці", ["border-spacing"] = "Відстань між комірками",
            ["table-layout"] = "Розрахунок ширини таблиці", ["caption-side"] = "Розташування підпису таблиці", ["empty-cells"] = "Показ порожніх комірок",
            ["outline"] = "Зовнішній контур", ["outline-color"] = "Колір контуру", ["outline-style"] = "Тип контуру", ["outline-width"] = "Товщина контуру", ["outline-offset"] = "Відстань контуру",
            ["backdrop-filter"] = "Ефект для фону позаду", ["mix-blend-mode"] = "Режим змішування", ["clip-path"] = "Форма обрізання",
            ["mask"] = "Маска видимості", ["mask-image"] = "Зображення-маска", ["object-fit"] = "Вписування зображення", ["object-position"] = "Позиція зображення",
            ["transform-origin"] = "Точка трансформації", ["transform-style"] = "3D-трансформація дочірніх блоків", ["perspective-origin"] = "Точка перспективи",
            ["backface-visibility"] = "Видимість зворотного боку", ["transition-property"] = "Що саме анімувати", ["transition-duration"] = "Тривалість переходу",
            ["transition-timing-function"] = "Темп переходу", ["transition-delay"] = "Затримка переходу", ["animation-name"] = "Назва анімації",
            ["animation-duration"] = "Тривалість анімації", ["animation-timing-function"] = "Темп анімації", ["animation-delay"] = "Затримка анімації",
            ["animation-iteration-count"] = "Кількість повторів", ["animation-direction"] = "Напрямок анімації", ["animation-fill-mode"] = "Стиль до/після анімації",
            ["animation-play-state"] = "Стан анімації", ["animation-composition"] = "Поєднання анімацій", ["container-name"] = "Назва контейнера",
            ["container-type"] = "Тип контейнерного запиту", ["content-visibility"] = "Відкладене відображення", ["view-transition-name"] = "Назва переходу між видами",
            ["list-style-type"] = "Вигляд маркера списку", ["list-style-position"] = "Позиція маркера", ["list-style-image"] = "Зображення маркера",
            ["scroll-behavior"] = "Плавність прокрутки", ["scroll-snap-type"] = "Тип прилипання прокрутки", ["scroll-snap-align"] = "Точка прилипання елемента",
            ["scroll-snap-stop"] = "Зупинка на точці прокрутки", ["scroll-margin"] = "Зовнішній відступ прокрутки", ["scroll-padding"] = "Внутрішній відступ прокрутки",
            ["overscroll-behavior"] = "Поведінка на межі прокрутки", ["scrollbar-color"] = "Кольори смуги прокрутки", ["scrollbar-width"] = "Ширина смуги прокрутки",
            ["--custom-property"] = "Власна CSS-змінна", ["accent-color"] = "Колір елементів керування", ["caret-color"] = "Колір курсора введення",
            ["pointer-events"] = "Реакція на вказівник", ["user-select"] = "Дозвіл виділяти текст", ["touch-action"] = "Дії сенсорного керування",
            ["font-feature-settings"] = "Додаткові функції шрифту", ["font-variation-settings"] = "Налаштування варіативного шрифту",
            ["anchor-name"] = "Ім’я якоря", ["anchor-scope"] = "Область дії якоря", ["position-anchor"] = "Якір позиціонування",
            ["position-area"] = "Область відносно якоря", ["position-try"] = "Альтернативне позиціонування", ["position-try-fallbacks"] = "Резервні позиції",
            ["position-try-order"] = "Порядок резервних позицій", ["position-visibility"] = "Видимість біля якоря",
            ["inline-size"] = "Розмір по рядку", ["block-size"] = "Розмір по блоку", ["min-inline-size"] = "Мінімальний розмір по рядку",
            ["max-inline-size"] = "Максимальний розмір по рядку", ["min-block-size"] = "Мінімальний розмір по блоку", ["max-block-size"] = "Максимальний розмір по блоку",
            ["font-stretch"] = "Ширина шрифту", ["font-kerning"] = "Кернінг шрифту", ["font-optical-sizing"] = "Оптичне налаштування шрифту",
            ["font-synthesis"] = "Синтез накреслення шрифту", ["font-synthesis-weight"] = "Синтез жирності шрифту", ["font-synthesis-style"] = "Синтез стилю шрифту",
            ["font-synthesis-small-caps"] = "Синтез малих капітелей", ["font-variant-caps"] = "Варіант великих літер", ["font-variant-ligatures"] = "Лігатури шрифту",
            ["font-variant-numeric"] = "Формат цифр", ["font-variant-east-asian"] = "Варіант східноазійського тексту", ["text-decoration-thickness"] = "Товщина підкреслення",
            ["text-decoration-skip-ink"] = "Обхід символів у підкресленні", ["text-underline-offset"] = "Відступ підкреслення", ["text-emphasis"] = "Акцент на символах",
            ["text-emphasis-color"] = "Колір акценту тексту", ["text-emphasis-style"] = "Стиль акценту тексту", ["text-emphasis-position"] = "Позиція акценту тексту",
            ["text-wrap-mode"] = "Режим перенесення тексту", ["text-wrap-style"] = "Стиль перенесення тексту", ["line-clamp"] = "Обмеження кількості рядків",
            ["hyphenate-character"] = "Символ перенесення слова", ["text-rendering"] = "Оптимізація відображення тексту", ["text-size-adjust"] = "Автоматичне коригування розміру тексту",
            ["initial-letter"] = "Початкова літера абзацу", ["hanging-punctuation"] = "Висячі розділові знаки",
            ["background-position-x"] = "Позиція фону по горизонталі", ["background-position-y"] = "Позиція фону по вертикалі",
            ["background-repeat-x"] = "Повторення фону по горизонталі", ["background-repeat-y"] = "Повторення фону по вертикалі",
            ["rotate"] = "Поворот елемента", ["scale"] = "Масштаб елемента", ["translate"] = "Переміщення елемента", ["transform-box"] = "Область трансформації",
            ["shape-outside"] = "Форма обтікання текстом", ["shape-margin"] = "Відступ навколо форми", ["shape-image-threshold"] = "Поріг прозорості форми",
            ["transition-behavior"] = "Поведінка переходу", ["animation-timeline"] = "Шкала часу анімації", ["animation-range"] = "Діапазон анімації",
            ["animation-range-start"] = "Початок діапазону анімації", ["animation-range-end"] = "Кінець діапазону анімації",
            ["view-transition-class"] = "Клас переходу між видами", ["contain-intrinsic-size"] = "Внутрішній розмір контейнера",
            ["contain-intrinsic-width"] = "Внутрішня ширина контейнера", ["contain-intrinsic-height"] = "Внутрішня висота контейнера",
            ["contain-intrinsic-block-size"] = "Внутрішній розмір блока", ["contain-intrinsic-inline-size"] = "Внутрішній розмір рядка",
            ["scrollbar-gutter"] = "Резерв місця для смуги прокрутки", ["scroll-timeline"] = "Шкала прокрутки для анімації",
            ["scroll-timeline-name"] = "Назва шкали прокрутки", ["scroll-timeline-axis"] = "Вісь шкали прокрутки",
            ["view-timeline"] = "Шкала видимості елемента", ["view-timeline-name"] = "Назва шкали видимості",
            ["view-timeline-axis"] = "Вісь шкали видимості", ["view-timeline-inset"] = "Відступ шкали видимості",
            ["field-sizing"] = "Автоматичний розмір поля", ["forced-color-adjust"] = "Коригування примусових кольорів", ["print-color-adjust"] = "Кольори під час друку",
            ["interpolate-size"] = "Анімація автоматичних розмірів",
            ["margin-block"] = "Зовнішній відступ по блоку", ["margin-block-start"] = "Зовнішній відступ на початку блока", ["margin-block-end"] = "Зовнішній відступ у кінці блока",
            ["margin-inline"] = "Зовнішній відступ по рядку", ["margin-inline-start"] = "Зовнішній відступ на початку рядка", ["margin-inline-end"] = "Зовнішній відступ у кінці рядка",
            ["padding-block"] = "Внутрішній відступ по блоку", ["padding-block-start"] = "Внутрішній відступ на початку блока", ["padding-block-end"] = "Внутрішній відступ у кінці блока",
            ["padding-inline"] = "Внутрішній відступ по рядку", ["padding-inline-start"] = "Внутрішній відступ на початку рядка", ["padding-inline-end"] = "Внутрішній відступ у кінці рядка",
            ["inset-block"] = "Позиція по блоку", ["inset-block-start"] = "Позиція на початку блока", ["inset-block-end"] = "Позиція в кінці блока",
            ["inset-inline"] = "Позиція по рядку", ["inset-inline-start"] = "Позиція на початку рядка", ["inset-inline-end"] = "Позиція в кінці рядка",
            ["border-block"] = "Рамка по блоку", ["border-block-color"] = "Колір рамки по блоку", ["border-block-style"] = "Стиль рамки по блоку", ["border-block-width"] = "Товщина рамки по блоку",
            ["border-block-start"] = "Рамка на початку блока", ["border-block-start-color"] = "Колір рамки на початку блока", ["border-block-start-style"] = "Стиль рамки на початку блока", ["border-block-start-width"] = "Товщина рамки на початку блока",
            ["border-block-end"] = "Рамка в кінці блока", ["border-block-end-color"] = "Колір рамки в кінці блока", ["border-block-end-style"] = "Стиль рамки в кінці блока", ["border-block-end-width"] = "Товщина рамки в кінці блока",
            ["border-inline"] = "Рамка по рядку", ["border-inline-color"] = "Колір рамки по рядку", ["border-inline-style"] = "Стиль рамки по рядку", ["border-inline-width"] = "Товщина рамки по рядку",
            ["border-inline-start"] = "Рамка на початку рядка", ["border-inline-start-color"] = "Колір рамки на початку рядка", ["border-inline-start-style"] = "Стиль рамки на початку рядка", ["border-inline-start-width"] = "Товщина рамки на початку рядка",
            ["border-inline-end"] = "Рамка в кінці рядка", ["border-inline-end-color"] = "Колір рамки в кінці рядка", ["border-inline-end-style"] = "Стиль рамки в кінці рядка", ["border-inline-end-width"] = "Товщина рамки в кінці рядка",
            ["border-start-start-radius"] = "Заокруглення початкового кута блока", ["border-start-end-radius"] = "Заокруглення кінцевого кута блока",
            ["border-end-start-radius"] = "Заокруглення початкового кута рядка", ["border-end-end-radius"] = "Заокруглення кінцевого кута рядка"
        };
        if (titles.TryGetValue(property, out string? title)) return title;
        string friendly = property.Replace("--", "Власна ").Replace("-", " ");
        return char.ToUpperInvariant(friendly[0]) + friendly[1..];
    }

    private static string DescribeCssProperty(string property)
    {
        if (property.StartsWith("margin-block", StringComparison.Ordinal) || property.StartsWith("margin-inline", StringComparison.Ordinal))
            return "Задає зовнішній відступ у логічному напрямку блока або рядка; напрям залежить від мови та writing-mode.";
        if (property.StartsWith("padding-block", StringComparison.Ordinal) || property.StartsWith("padding-inline", StringComparison.Ordinal))
            return "Задає внутрішній відступ у логічному напрямку блока або рядка; зручніше за top/right/bottom/left для різних мов.";
        if (property.StartsWith("inset-block", StringComparison.Ordinal) || property.StartsWith("inset-inline", StringComparison.Ordinal))
            return "Задає логічне позиціонування від початку або кінця блока чи рядка, враховуючи напрямок тексту.";
        if (property.StartsWith("border-block", StringComparison.Ordinal) || property.StartsWith("border-inline", StringComparison.Ordinal) || property.StartsWith("border-start-", StringComparison.Ordinal) || property.StartsWith("border-end-", StringComparison.Ordinal))
            return "Налаштовує рамку з логічного боку елемента, що коректно адаптується до напрямку письма.";
        if (property.Contains("timeline", StringComparison.Ordinal) || property.StartsWith("animation-range", StringComparison.Ordinal))
            return "Пов'язує анімацію з прокручуванням або видимістю елемента замість лише часу.";
        if (property.StartsWith("margin", StringComparison.Ordinal))
            return property == "margin" ? "Задає зовнішній простір навколо елемента. Впливає на відстань до сусідніх блоків." : $"Задає зовнішній відступ з боку «{property[7..]}» навколо елемента.";
        if (property.StartsWith("padding", StringComparison.Ordinal))
            return property == "padding" ? "Задає внутрішній простір між вмістом елемента та його рамкою." : $"Задає внутрішній відступ з боку «{property[8..]}» — відстань від вмісту до рамки.";
        if (property.StartsWith("grid-template", StringComparison.Ordinal))
            return property.EndsWith("columns", StringComparison.Ordinal) ? "Описує кількість і ширину колонок CSS Grid." : property.EndsWith("rows", StringComparison.Ordinal) ? "Описує кількість і висоту рядків CSS Grid." : "Задає шаблон сітки: розміри рядків, колонок або іменованих областей.";
        if (property.StartsWith("grid-auto", StringComparison.Ordinal))
            return "Налаштовує автоматичне створення та розміри рядків або колонок для елементів, які не помістилися у задану сітку.";
        if (property.StartsWith("grid-column", StringComparison.Ordinal))
            return "Визначає, у якій колонці або між якими вертикальними лініями сітки розміщується елемент.";
        if (property.StartsWith("grid-row", StringComparison.Ordinal))
            return "Визначає, у якому рядку або між якими горизонтальними лініями сітки розміщується елемент.";
        if (property.StartsWith("grid", StringComparison.Ordinal) || property.StartsWith("place-", StringComparison.Ordinal))
            return "Керує розташуванням елементів у CSS Grid: розмірами комірок, областями або вирівнюванням.";
        if (property.StartsWith("flex", StringComparison.Ordinal))
            return property == "flex" ? "Скорочено задає, як Flex-елемент росте, стискається та якого початкового розміру прагне." : "Налаштовує поведінку елемента у Flex-контейнері: напрямок, перенесення, ріст або стискання.";
        if (property.StartsWith("justify", StringComparison.Ordinal) || property.StartsWith("align", StringComparison.Ordinal))
            return "Вирівнює елементи або розподіляє вільний простір уздовж однієї з осей Flexbox чи Grid.";
        if (property.StartsWith("background", StringComparison.Ordinal))
            return "Налаштовує фон елемента: колір, зображення, розмір, позицію, повторення або спосіб відображення.";
        if (property.StartsWith("border", StringComparison.Ordinal) && property != "border-collapse" && property != "border-spacing")
            return "Налаштовує рамку елемента або окремого його боку: товщину, тип лінії, колір чи заокруглення.";
        if (property.StartsWith("outline", StringComparison.Ordinal))
            return "Налаштовує зовнішній контур навколо елемента. Контур не займає місце в макеті.";
        if (property.StartsWith("font", StringComparison.Ordinal))
            return "Налаштовує вигляд шрифту: сімейство, розмір, товщину, стиль або додаткові типографічні можливості.";
        if (property.StartsWith("text", StringComparison.Ordinal) || property is "white-space" or "word-break" or "overflow-wrap" or "hyphens" or "direction" or "writing-mode")
            return "Керує відображенням тексту: вирівнюванням, переносами, регістром, декорацією або напрямком написання.";
        if (property.StartsWith("transition", StringComparison.Ordinal))
            return "Налаштовує плавний перехід між двома станами стилю, наприклад під час наведення курсора.";
        if (property.StartsWith("animation", StringComparison.Ordinal))
            return "Налаштовує CSS-анімацію: її назву, тривалість, швидкість, затримку, повтори та стан відтворення.";
        if (property.StartsWith("scroll-snap", StringComparison.Ordinal) || property.StartsWith("scroll-margin", StringComparison.Ordinal) || property.StartsWith("scroll-padding", StringComparison.Ordinal) || property.StartsWith("scrollbar", StringComparison.Ordinal) || property is "scroll-behavior" or "overscroll-behavior")
            return "Керує прокручуванням сторінки або контейнера: плавністю, точками зупинки, смугою прокрутки та поведінкою на межах.";
        if (property.StartsWith("list-style", StringComparison.Ordinal))
            return "Змінює вигляд маркерів списку: тип символу, картинку та розташування відносно тексту.";

        return property switch
        {
            "top" => "Задає відстань від верхнього краю для позиціонованого елемента.",
            "right" => "Задає відстань від правого краю для позиціонованого елемента.",
            "bottom" => "Задає відстань від нижнього краю для позиціонованого елемента.",
            "left" => "Задає відстань від лівого краю для позиціонованого елемента.",
            "inset" => "Одночасно задає відступи елемента від верхнього, правого, нижнього та лівого країв.",
            "display" => "Визначає тип відображення елемента: block, inline, flex, grid або none.",
            "position" => "Визначає спосіб позиціонування: звичайний потік, відносне, абсолютне, фіксоване або липке.",
            "z-index" => "Визначає порядок накладання елементів: більше значення зазвичай показується поверх меншого.",
            "float" => "Дозволяє елементу притискатися до лівого або правого боку, щоб текст обтікав його.",
            "clear" => "Забороняє елементу розташовуватися поруч із елементами, для яких використано float.",
            "overflow" or "overflow-x" or "overflow-y" => "Визначає, що робити з вмістом, який виходить за межі блока: показати, приховати або прокручувати.",
            "visibility" => "Керує видимістю елемента, зберігаючи або прибираючи його місце в макеті.",
            "isolation" => "Створює окремий контекст накладання, щоб керувати змішуванням шарів усередині блока.",
            "box-sizing" => "Визначає, чи входять padding і border у задану ширину та висоту елемента.",
            "gap" or "row-gap" or "column-gap" => "Задає проміжок між дочірніми елементами Flex або Grid без додавання зовнішніх відступів.",
            "width" => "Задає ширину елемента.", "height" => "Задає висоту елемента.",
            "min-width" => "Обмежує мінімальну ширину, нижче якої елемент не стискатиметься.",
            "max-width" => "Обмежує максимальну ширину елемента.",
            "min-height" => "Задає мінімальну висоту елемента.", "max-height" => "Обмежує максимальну висоту елемента.",
            "aspect-ratio" => "Задає пропорцію ширини до висоти, наприклад 16 / 9.",
            "color" => "Задає колір тексту та інших елементів переднього плану.",
            "line-height" => "Визначає висоту рядка та відстань між базовими лініями тексту.",
            "letter-spacing" => "Змінює відстань між символами тексту.", "word-spacing" => "Змінює відстань між словами.",
            "vertical-align" => "Вирівнює елемент відносно базової лінії або сусіднього тексту.",
            "background" => "Скорочено задає колір, зображення, позицію, розмір та інші параметри фону.",
            "border" => "Скорочено задає товщину, стиль і колір рамки з усіх боків.",
            "border-collapse" => "Визначає, чи зливаються межі сусідніх комірок HTML-таблиці.",
            "border-spacing" => "Задає відстань між межами сусідніх комірок таблиці.",
            "table-layout" => "Визначає алгоритм розрахунку ширини колонок таблиці.",
            "caption-side" => "Визначає, з якого боку таблиці показувати її підпис.",
            "empty-cells" => "Керує відображенням рамок і фону порожніх комірок таблиці.",
            "opacity" => "Задає прозорість елемента: 0 — невидимий, 1 — повністю непрозорий.",
            "filter" => "Застосовує візуальні фільтри, наприклад розмиття, яскравість або відтінки сірого.",
            "backdrop-filter" => "Застосовує фільтр до вмісту, який видно позаду напівпрозорого елемента.",
            "mix-blend-mode" => "Визначає, як кольори елемента змішуються з кольорами того, що знаходиться позаду.",
            "clip-path" => "Обрізає видиму частину елемента за заданою геометричною формою.",
            "mask" or "mask-image" => "Використовує маску, щоб зробити частини елемента прозорими або видимими.",
            "object-fit" => "Визначає, як зображення або відео заповнює рамку заданого розміру.",
            "object-position" => "Задає положення зображення або відео всередині його рамки.",
            "transform" => "Переміщує, повертає, нахиляє або масштабує елемент без зміни його місця в потоці.",
            "transform-origin" => "Задає точку, навколо якої обертається або масштабується елемент.",
            "transform-style" => "Визначає, чи зберігають дочірні елементи 3D-позицію.",
            "perspective" => "Створює ефект глибини для 3D-трансформацій дочірніх елементів.",
            "perspective-origin" => "Задає точку, з якої спостерігається 3D-перспектива.",
            "backface-visibility" => "Визначає, чи видно зворотну сторону повернутого елемента.",
            "container" or "container-name" or "container-type" => "Дозволяє налаштувати контейнер для container query — адаптивних правил залежно від його розміру.",
            "contain" => "Обмежує вплив вмісту елемента на решту сторінки для кращої ізоляції та продуктивності.",
            "content-visibility" => "Дозволяє браузеру відкладати відображення вмісту, який поки не видно на екрані.",
            "view-transition-name" => "Призначає елементу ім’я для анімованих переходів між станами сторінки.",
            "--custom-property" => "Створює власну CSS-змінну, значення якої можна повторно використовувати через var(--назва).",
            "all" => "Скидає або встановлює одразу більшість CSS-властивостей елемента одним значенням.",
            "appearance" => "Керує стандартним виглядом елементів браузера, наприклад полів і кнопок.",
            "accent-color" => "Задає акцентний колір прапорців, перемикачів та інших системних елементів форми.",
            "caret-color" => "Задає колір вертикального курсора в текстовому полі.",
            "cursor" => "Змінює вигляд курсора миші, коли він знаходиться над елементом.",
            "pointer-events" => "Визначає, чи може елемент реагувати на наведення та натискання миші.",
            "user-select" => "Дозволяє або забороняє користувачу виділяти текст елемента.",
            "resize" => "Дозволяє змінювати розмір елемента вручну, зазвичай текстового поля.",
            "content" => "Додає текст або інший вміст через псевдоелементи ::before та ::after.",
            "counter-reset" => "Створює або скидає CSS-лічильник для нумерації елементів.",
            "counter-increment" => "Збільшує значення CSS-лічильника, наприклад для нумерованих пунктів.",
            "quotes" => "Задає символи лапок, які використовуються для вкладених цитат.",
            "will-change" => "Підказує браузеру, яка властивість скоро зміниться, щоб підготувати анімацію.",
            "touch-action" => "Керує жестами на сенсорних екранах, наприклад прокручуванням і масштабуванням.",
            "color-scheme" => "Повідомляє браузеру, чи підтримує елемент світлу, темну або обидві кольорові схеми.",
            "font-feature-settings" => "Вмикає або вимикає спеціальні OpenType-функції шрифту.",
            "font-variation-settings" => "Точно налаштовує осі варіативного шрифту, наприклад товщину або ширину.",
            _ => $"Керує параметром «{GetCssPropertyTitle(property).ToLowerInvariant()}» для поточного елемента. Значення залежить від потрібного дизайну та CSS-контексту."
        };
    }

    private static string GetCssDefaultValue(string property)
    {
        if (property.StartsWith("margin", StringComparison.Ordinal) || property.StartsWith("padding", StringComparison.Ordinal) || property.EndsWith("gap", StringComparison.Ordinal)) return "12px";
        if (property.StartsWith("border", StringComparison.Ordinal) && property.EndsWith("width", StringComparison.Ordinal)) return "1px";
        if (property is "inline-size" or "block-size" or "min-inline-size" or "max-inline-size" or "min-block-size" or "max-block-size") return "auto";
        if (property.Contains("width", StringComparison.Ordinal) || property.Contains("height", StringComparison.Ordinal)) return "auto";
        if (property.StartsWith("color", StringComparison.Ordinal) || property.EndsWith("color", StringComparison.Ordinal)) return "#2878e5";
        if (property.StartsWith("background", StringComparison.Ordinal)) return property == "background-color" ? "#EAF2FF" : "none";
        if (property.StartsWith("border", StringComparison.Ordinal) || property == "outline") return "1px solid #D8E2F0";
        if (property.StartsWith("animation", StringComparison.Ordinal)) return property == "animation-iteration-count" ? "1" : "none";
        if (property.StartsWith("transition", StringComparison.Ordinal)) return property == "transition-duration" ? ".2s" : "all .2s ease";
        if (property.StartsWith("scroll", StringComparison.Ordinal)) return "auto";
        if (property.StartsWith("grid", StringComparison.Ordinal)) return "auto";
        if (property.StartsWith("flex", StringComparison.Ordinal)) return property == "flex-direction" ? "row" : "initial";
        return property switch
        {
            "display" => "block", "position" => "relative", "opacity" => "1", "z-index" => "1", "cursor" => "pointer",
            "overflow" or "overflow-x" or "overflow-y" => "auto", "object-fit" => "cover", "text-align" => "left",
            "font-size" => "16px", "font-weight" => "400", "line-height" => "1.5", "transform" => "none",
            "--custom-property" => "#2878e5", _ => "initial"
        };
    }

    private void CssPaletteProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CssPalettePropertyList.SelectedItem is not CssPaletteItem item) return;
        CssPaletteHintText.Text = item.Hint;
        CssPaletteValueBox.Text = item.DefaultValue;
        _loadingCssDesigner = true;
        CssPropertyBox.Text = item.Property;
        _loadingCssDesigner = false;
    }

    private void AddCssPaletteProperty_Click(object sender, RoutedEventArgs e)
    {
        if (CssPalettePropertyList.SelectedItem is not CssPaletteItem item) return;
        CssPropertyBox.Text = item.Property;
        CssValueBox.Text = CssPaletteValueBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(CssValueBox.Text))
        {
            MessageBox.Show("Введи значення для цієї властивості.", "CSS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        AddCssDeclaration_Click(sender, e);
    }

    private void CssCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized) RefreshCssProperties();
    }

    private void RefreshCssProperties()
    {
        if (CssPropertyBox is null) return;
        string category = CssCategoryBox.SelectedItem?.ToString() ?? CssPropertyGroups.Keys.First();
        CssPropertyBox.ItemsSource = CssPropertyGroups.TryGetValue(category, out var props) ? props : Array.Empty<string>();
        if (CssPropertyBox.Items.Count > 0) CssPropertyBox.SelectedIndex = 0;
    }

    private void CssSelector_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized || _loadingCssDesigner) return;
        LoadCssBlocksForSelector();
    }

    private void GlobalCss_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingCss) return;
        _globalCss = GlobalCssBox.Text;
        if (!_loadingCssDesigner && IsInitialized) LoadCssBlocksForSelector();
        StatusText.Text = "CSS-стилі оновлено";
    }

    private void LoadCssBlocksForSelector()
    {
        string selector = CssSelectorBox.Text.Trim();
        _loadingCssDesigner = true;
        try
        {
            _cssDeclarations.Clear();
            if (string.IsNullOrWhiteSpace(selector)) return;

            string css = GlobalCssBox.Text ?? string.Empty;
            CssRuleLocation? rule = FindCssRule(css, selector);
            if (rule is null) return;

            string declarations = css[(rule.Value.OpenBrace + 1)..rule.Value.CloseBrace];
            foreach (var declaration in ParseCssDeclarations(declarations))
                _cssDeclarations.Add(new CssDeclaration(declaration.Property, declaration.Value));
        }
        finally
        {
            _loadingCssDesigner = false;
            ScheduleThemeRefresh();
        }
    }

    private readonly record struct CssRuleLocation(int Start, int OpenBrace, int CloseBrace, int Depth);

    /// <summary>
    /// Finds an exact CSS selector rule without matching substrings such as "div" inside ".mydiv".
    /// Brace/string/comment aware so rules inside @media and @supports blocks can be read too.
    /// A top-level rule is preferred; a nested rule is used only if no top-level rule exists.
    /// </summary>
    private static CssRuleLocation? FindCssRule(string css, string selector)
    {
        if (string.IsNullOrWhiteSpace(css) || string.IsNullOrWhiteSpace(selector)) return null;
        string expected = NormalizeCssSelector(selector);
        int preludeStart = 0;
        int depth = 0;
        bool inComment = false;
        char quote = '\0';
        bool escaped = false;
        CssRuleLocation? nestedMatch = null;

        for (int i = 0; i < css.Length; i++)
        {
            char ch = css[i];
            if (inComment)
            {
                if (ch == '*' && i + 1 < css.Length && css[i + 1] == '/') { inComment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (escaped) { escaped = false; continue; }
                if (ch == '\\') { escaped = true; continue; }
                if (ch == quote) quote = '\0';
                continue;
            }
            if (ch == '/' && i + 1 < css.Length && css[i + 1] == '*') { inComment = true; i++; continue; }
            if (ch is '\'' or '"') { quote = ch; continue; }

            if (ch == '{')
            {
                string prelude = StripCssComments(css[preludeStart..i]).Trim();
                if (NormalizeCssSelector(prelude) == expected)
                {
                    int closeBrace = FindMatchingCssBrace(css, i);
                    if (closeBrace >= 0)
                    {
                        var location = new CssRuleLocation(FindFirstCssToken(css, preludeStart, i), i, closeBrace, depth);
                        if (depth == 0) return location;
                        nestedMatch ??= location;
                    }
                }
                depth++;
                preludeStart = i + 1;
            }
            else if (ch == '}')
            {
                depth = Math.Max(0, depth - 1);
                preludeStart = i + 1;
            }
            else if (ch == ';')
            {
                preludeStart = i + 1;
            }
        }

        return nestedMatch;
    }

    private static int FindMatchingCssBrace(string css, int openBrace)
    {
        int depth = 0;
        bool inComment = false;
        char quote = '\0';
        bool escaped = false;
        for (int i = openBrace; i < css.Length; i++)
        {
            char ch = css[i];
            if (inComment)
            {
                if (ch == '*' && i + 1 < css.Length && css[i + 1] == '/') { inComment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (escaped) { escaped = false; continue; }
                if (ch == '\\') { escaped = true; continue; }
                if (ch == quote) quote = '\0';
                continue;
            }
            if (ch == '/' && i + 1 < css.Length && css[i + 1] == '*') { inComment = true; i++; continue; }
            if (ch is '\'' or '"') { quote = ch; continue; }
            if (ch == '{') depth++;
            else if (ch == '}' && --depth == 0) return i;
        }
        return -1;
    }

    private static int FindFirstCssToken(string css, int start, int end)
    {
        int i = start;
        while (i < end)
        {
            if (char.IsWhiteSpace(css[i])) { i++; continue; }
            if (css[i] == '/' && i + 1 < end && css[i + 1] == '*')
            {
                int close = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (close < 0 || close + 2 > end) return i;
                i = close + 2;
                continue;
            }
            break;
        }
        return i;
    }

    private static string StripCssComments(string text)
    {
        var result = new StringBuilder(text.Length);
        bool inComment = false;
        char quote = '\0';
        bool escaped = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (inComment)
            {
                if (ch == '*' && i + 1 < text.Length && text[i + 1] == '/') { inComment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                result.Append(ch);
                if (escaped) { escaped = false; continue; }
                if (ch == '\\') { escaped = true; continue; }
                if (ch == quote) quote = '\0';
                continue;
            }
            if (ch == '/' && i + 1 < text.Length && text[i + 1] == '*') { inComment = true; i++; continue; }
            if (ch is '\'' or '"') quote = ch;
            result.Append(ch);
        }
        return result.ToString();
    }

    private static string NormalizeCssSelector(string selector) =>
        Regex.Replace(StripCssComments(selector).Trim(), @"\s+", " ");

    private static IEnumerable<(string Property, string Value)> ParseCssDeclarations(string text)
    {
        foreach (string part in SplitCssAtTopLevel(text, ';'))
        {
            string declaration = StripCssComments(part).Trim();
            if (declaration.Length == 0) continue;
            int colon = FindCssTopLevelColon(declaration);
            if (colon <= 0) continue;
            string property = declaration[..colon].Trim();
            string value = declaration[(colon + 1)..].Trim();
            if (property.Length > 0 && value.Length > 0)
                yield return (property, value);
        }
    }

    private static IEnumerable<string> SplitCssAtTopLevel(string text, char delimiter)
    {
        int start = 0, parenDepth = 0, bracketDepth = 0;
        bool inComment = false;
        char quote = '\0';
        bool escaped = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (inComment)
            {
                if (ch == '*' && i + 1 < text.Length && text[i + 1] == '/') { inComment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (escaped) { escaped = false; continue; }
                if (ch == '\\') { escaped = true; continue; }
                if (ch == quote) quote = '\0';
                continue;
            }
            if (ch == '/' && i + 1 < text.Length && text[i + 1] == '*') { inComment = true; i++; continue; }
            if (ch is '\'' or '"') { quote = ch; continue; }
            if (ch == '(') parenDepth++;
            else if (ch == ')') parenDepth = Math.Max(0, parenDepth - 1);
            else if (ch == '[') bracketDepth++;
            else if (ch == ']') bracketDepth = Math.Max(0, bracketDepth - 1);
            else if (ch == delimiter && parenDepth == 0 && bracketDepth == 0)
            {
                yield return text[start..i];
                start = i + 1;
            }
        }
        if (start < text.Length) yield return text[start..];
    }

    private static int FindCssTopLevelColon(string declaration)
    {
        int parenDepth = 0, bracketDepth = 0;
        bool inComment = false;
        char quote = '\0';
        bool escaped = false;
        for (int i = 0; i < declaration.Length; i++)
        {
            char ch = declaration[i];
            if (inComment)
            {
                if (ch == '*' && i + 1 < declaration.Length && declaration[i + 1] == '/') { inComment = false; i++; }
                continue;
            }
            if (quote != '\0')
            {
                if (escaped) { escaped = false; continue; }
                if (ch == '\\') { escaped = true; continue; }
                if (ch == quote) quote = '\0';
                continue;
            }
            if (ch == '/' && i + 1 < declaration.Length && declaration[i + 1] == '*') { inComment = true; i++; continue; }
            if (ch is '\'' or '"') { quote = ch; continue; }
            if (ch == '(') parenDepth++;
            else if (ch == ')') parenDepth = Math.Max(0, parenDepth - 1);
            else if (ch == '[') bracketDepth++;
            else if (ch == ']') bracketDepth = Math.Max(0, bracketDepth - 1);
            else if (ch == ':' && parenDepth == 0 && bracketDepth == 0) return i;
        }
        return -1;
    }

    private void AddCssDeclaration_Click(object sender, RoutedEventArgs e)
    {
        string property = CssPropertyBox.Text.Trim();
        string value = CssValueBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
        {
            MessageBox.Show("Вкажи CSS-властивість та її значення.", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (property.StartsWith('@') || property.Contains('{') || property.Contains('}') || property.Contains(':') || property.Contains(';') || property.Any(char.IsWhiteSpace))
        {
            MessageBox.Show("Вкажи одну CSS-властивість без пробілів, двокрапок або крапок з комою, наприклад display чи --my-color.", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var existing = _cssDeclarations.FirstOrDefault(x => x.Property.Equals(property, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) _cssDeclarations.Remove(existing);
        _cssDeclarations.Add(new CssDeclaration(property, value));
        WriteCssBlocksToCode();
        ScheduleThemeRefresh();
        StatusText.Text = $"CSS: додано {property}";
    }

    private void RemoveCssDeclaration_Click(object sender, RoutedEventArgs e)
    {
        if (CssBlocksList.SelectedItem is not CssDeclaration selected) return;
        _cssDeclarations.Remove(selected);
        WriteCssBlocksToCode();
        ScheduleThemeRefresh();
    }

    private void WriteCssBlocksToCode()
    {
        string selector = CssSelectorBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(selector)) selector = "body";
        if (selector.Contains('{') || selector.Contains('}'))
        {
            MessageBox.Show("Вкажи селектор без фігурних дужок, наприклад .card, #header або button:hover.", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string rule = selector + " {\n" + string.Join("\n", _cssDeclarations.Select(x => $"  {x.Property}: {x.Value};")) + "\n}";
        string css = GlobalCssBox.Text ?? string.Empty;
        CssRuleLocation? existing = FindCssRule(css, selector);
        if (existing is not null)
        {
            var location = existing.Value;
            css = css[..location.Start] + rule + css[(location.CloseBrace + 1)..];
        }
        else
        {
            css = css.TrimEnd() + (string.IsNullOrWhiteSpace(css) ? "" : "\n\n") + rule + "\n";
        }

        _loadingCss = true;
        try
        {
            GlobalCssBox.Text = css;
            _globalCss = css;
        }
        finally { _loadingCss = false; }
    }

    private void AddCssMedia_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CssBreakpointBox.Text.Trim(), out int width) || width < 1)
        {
            MessageBox.Show("Вкажи коректну ширину екрана у px, наприклад 768.", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string selector = string.IsNullOrWhiteSpace(CssSelectorBox.Text) ? "body" : CssSelectorBox.Text.Trim();
        string declarations = string.Join("\n", _cssDeclarations.Select(x => $"    {x.Property}: {x.Value};"));
        if (string.IsNullOrWhiteSpace(declarations)) { StatusText.Text = "Спочатку додай CSS-блоки"; return; }
        string media = $"@media (max-width: {width}px) {{\n  {selector} {{\n{declarations}\n  }}\n}}";
        AppendCss(media);
        StatusText.Text = $"Створено адаптивне правило до {width}px";
    }

    private void CssPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string declarations) return;
        foreach (string part in declarations.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = part.IndexOf(':');
            if (colon <= 0) continue;
            string property = part[..colon].Trim(), value = part[(colon + 1)..].Trim();
            var existing = _cssDeclarations.FirstOrDefault(x => x.Property.Equals(property, StringComparison.OrdinalIgnoreCase));
            if (existing is not null) _cssDeclarations.Remove(existing);
            _cssDeclarations.Add(new CssDeclaration(property, value));
        }
        WriteCssBlocksToCode();
        ScheduleThemeRefresh();
        if (declarations.Contains("webblox-pulse", StringComparison.Ordinal))
            AppendCss("@keyframes webblox-pulse {\n  0%, 100% { transform: scale(1); }\n  50% { transform: scale(1.04); }\n}");
        StatusText.Text = "Готовий CSS-стиль додано";
    }

    private void AppendCss(string snippet)
    {
        _loadingCss = true;
        GlobalCssBox.Text = GlobalCssBox.Text.TrimEnd() + (string.IsNullOrWhiteSpace(GlobalCssBox.Text) ? "" : "\n\n") + snippet + "\n";
        _globalCss = GlobalCssBox.Text;
        _loadingCss = false;
    }

    private void ImportCss_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSS files (*.css)|*.css|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            _loadingCss = true;
            try
            {
                GlobalCssBox.Text = File.ReadAllText(dialog.FileName);
                _globalCss = GlobalCssBox.Text;
            }
            finally { _loadingCss = false; }
            _projectDirectory ??= System.IO.Path.GetDirectoryName(dialog.FileName);
            LoadCssBlocksForSelector();
            StatusText.Text = "CSS-файл імпортовано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося імпортувати CSS:\n{ex.Message}", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportCss_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSS files (*.css)|*.css", FileName = "styles.css" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, GlobalCssBox.Text, new UTF8Encoding(false));
            _projectDirectory ??= System.IO.Path.GetDirectoryName(dialog.FileName);
            StatusText.Text = "CSS-файл експортовано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося експортувати CSS:\n{ex.Message}", "WebBlox CSS", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CssSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string snippet) return;
        if (!string.IsNullOrWhiteSpace(GlobalCssBox.Text)) GlobalCssBox.AppendText(Environment.NewLine + Environment.NewLine);
        GlobalCssBox.AppendText(snippet);
        StatusText.Text = "CSS-приклад додано";
    }

    private void ClearCss_Click(object sender, RoutedEventArgs e)
    {
        GlobalCssBox.Clear();
        StatusText.Text = "Глобальні CSS-стилі очищено";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshPalette(preserveViewState: true);

    private void PaletteList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(PaletteList);
    }

    private void PaletteList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || PaletteList.SelectedItem is not ListBoxItem item) return;
        Point current = e.GetPosition(PaletteList);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        string tag = item.Tag?.ToString() ?? "div";
        DragDrop.DoDragDrop(PaletteList, tag, DragDropEffects.Copy);
    }

    private void PaletteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PaletteList.SelectedItem is ListBoxItem item)
        {
            string tag = item.Tag?.ToString() ?? "div";
            AddElement(tag, _selected is not null && CanContain(_selected) ? _selected : null);
        }
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
        string tag = e.Data.GetData(DataFormats.StringFormat)?.ToString() ?? "p";
        e.Handled = true;
        PageElement? parent = null;
        if (e.OriginalSource is DependencyObject d)
        {
            var item = FindAncestor<TreeViewItem>(d);
            if (item?.Tag is PageElement target && CanContain(target)) parent = target;
        }
        AddElement(tag, parent);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        var visited = new HashSet<DependencyObject>();
        while (current is not null && visited.Add(current))
        {
            if (current is T found) return found;
            DependencyObject? parent = null;
            try { parent = VisualTreeHelper.GetParent(current); } catch (InvalidOperationException) { }
            if (parent is null)
            {
                try { parent = LogicalTreeHelper.GetParent(current); } catch (InvalidOperationException) { }
            }
            current = parent;
        }
        return null;
    }

    private void AddElement(string tag, PageElement? parent)
    {
        var element = CreateElement(tag);
        if (parent is null) _elements.Add(element); else parent.Children.Add(element);
        RenderTree();
        SelectElement(element);
        StatusText.Text = $"Додано <{tag}> · {GetTagCategory(tag)}";
    }

    private static PageElement CreateElement(string tag)
    {
        string content = tag.ToLowerInvariant() switch
        {
            "h1" => "Main heading", "h2" => "Section heading", "h3" => "Subheading",
            "html" or "head" or "body" or "hgroup" or "menu" or "search" => "", "slot" => "Fallback content",
            "title" => "Page title", "meta" => "Page description", "link" => "",
            "q" => "Quoted text", "dfn" => "Term being defined", "s" => "No longer accurate",
            "p" => "Write your paragraph here.", "a" => "Link text", "button" => "Click me",
            "img" => "", "input" => "", "textarea" => "", "hr" or "br" => "",
            "ul" or "ol" => "", "li" => "List item", "div" => "", "section" => "", "header" => "", "footer" => "",
            "video" or "audio" or "iframe" or "source" => "", "table" => "", "tr" => "", "td" or "th" => "Cell",
            "label" => "Label", "form" => "", "blockquote" => "Quote", "pre" or "code" => "Code",
            _ => "Content"
        };
        var element = new PageElement(tag, content);
        if (tag == "a") element.Href = "https://example.com";
        if (tag == "link") { element.Href = "styles.css"; element.Extra = "stylesheet"; }
        if (tag == "meta") element.Extra = "description";
        if (tag == "img") { element.Src = "https://via.placeholder.com/320x160"; element.Extra = "Placeholder image"; }
        if (tag == "input") element.Extra = "text";
        if (tag == "textarea") element.Extra = "Enter text";
        if (tag == "button") element.Extra = "button";
        if (tag == "iframe") element.Src = "https://example.com";
        return element;
    }

    private static bool CanContain(PageElement e) => ContainerTags.Contains(e.TagName) && !VoidTags.Contains(e.TagName);

    private void CanvasZoomOut_Click(object sender, RoutedEventArgs e) => SetCanvasZoom(_canvasZoom - 0.1);

    private void CanvasZoomIn_Click(object sender, RoutedEventArgs e) => SetCanvasZoom(_canvasZoom + 0.1);

    private void CanvasZoomReset_Click(object sender, RoutedEventArgs e) => SetCanvasZoom(1.0);

    private void SetCanvasZoom(double zoom)
    {
        _canvasZoom = Math.Clamp(Math.Round(zoom, 1), 0.7, 1.4);
        if (CanvasZoomText is not null) CanvasZoomText.Text = $"{_canvasZoom:P0}";

        foreach (TreeViewItem root in CanvasTree.Items)
            ApplyCanvasZoom(root, isDocumentRoot: root.Tag is not PageElement);

        // Re-measure after changing card widths so both scrollbars report the true extent.
        CanvasTree.InvalidateMeasure();
        CanvasTree.UpdateLayout();
        UpdateCanvasSelectionVisuals();
    }

    private void ApplyCanvasZoom(TreeViewItem item, bool isDocumentRoot = false)
    {
        double scale = _canvasZoom;
        item.Margin = new Thickness(2 * scale, scale, 2 * scale, 2 * scale);
        item.Padding = new Thickness(0);

        if (item.Header is Border header)
        {
            header.Padding = isDocumentRoot
                ? new Thickness(14 * scale, 10 * scale, 14 * scale, 10 * scale)
                : new Thickness(12 * scale, 8 * scale, 14 * scale, 8 * scale);
            header.Margin = isDocumentRoot
                ? new Thickness(2 * scale, 2 * scale, 2 * scale, 10 * scale)
                : new Thickness(0, 2 * scale, 8 * scale, 5 * scale);

            if (isDocumentRoot)
            {
                if (header.Child is TextBlock rootLabel)
                    rootLabel.FontSize = 13 * scale;
            }
            else
            {
                header.MinWidth = 210 * scale;
                header.MaxWidth = 560 * scale;
                if (header.Child is StackPanel content && content.Children.Count >= 2)
                {
                    if (content.Children[0] is StackPanel titleRow && titleRow.Children.Count >= 2)
                    {
                        if (titleRow.Children[0] is TextBlock tagText) tagText.FontSize = 13 * scale;
                        if (titleRow.Children[1] is TextBlock categoryText) categoryText.FontSize = 10 * scale;
                    }
                    if (content.Children[1] is TextBlock previewText)
                    {
                        previewText.FontSize = 11 * scale;
                        previewText.Margin = new Thickness(scale, 3 * scale, 0, 0);
                    }
                }
            }
        }

        foreach (TreeViewItem child in item.Items)
            ApplyCanvasZoom(child, isDocumentRoot: false);
    }

    private void UpdateCanvasSelectionVisuals()
    {
        foreach (TreeViewItem root in CanvasTree.Items)
            UpdateCanvasSelectionVisuals(root);
    }

    private void UpdateCanvasSelectionVisuals(TreeViewItem item)
    {
        if (item.Tag is PageElement element && item.Header is Border header)
        {
            bool selected = ReferenceEquals(element, _selected);
            header.BorderBrush = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromArgb(65, 0, 0, 0));
            header.BorderThickness = selected
                ? new Thickness(2 * _canvasZoom)
                : new Thickness(0, 0, 0, 3 * _canvasZoom);
        }

        foreach (TreeViewItem child in item.Items)
            UpdateCanvasSelectionVisuals(child);
    }

    private void CollapseCanvas_Click(object sender, RoutedEventArgs e)
    {
        foreach (TreeViewItem root in CanvasTree.Items)
        {
            root.IsExpanded = true; // Keep the document root visible.
            foreach (TreeViewItem child in root.Items)
                SetCanvasExpanded(child, expanded: false);
        }
    }

    private void ExpandCanvas_Click(object sender, RoutedEventArgs e)
    {
        foreach (TreeViewItem root in CanvasTree.Items)
            SetCanvasExpanded(root, expanded: true);
    }

    private static void SetCanvasExpanded(TreeViewItem item, bool expanded)
    {
        item.IsExpanded = expanded;
        foreach (TreeViewItem child in item.Items)
            SetCanvasExpanded(child, expanded);
    }

    private void RenderTree()
    {
        CanvasTree.Items.Clear();
        EmptyCanvasHint.Visibility = _elements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var root = new TreeViewItem
        {
            Header = new Border { Background = new SolidColorBrush(Color.FromRgb(35, 83, 151)), CornerRadius = new CornerRadius(9), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(2, 2, 2, 10), Child = new TextBlock { Text = "🌐  HTML DOCUMENT  ·  BODY", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13 } },
            IsExpanded = true,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(8, 7, 8, 7)
        };
        foreach (var element in _elements) root.Items.Add(BuildTreeItem(element));
        CanvasTree.Items.Add(root);
        ApplyCanvasZoom(root, isDocumentRoot: true);
        if (CanvasZoomText is not null) CanvasZoomText.Text = $"{_canvasZoom:P0}";
        UpdateCanvasSelectionVisuals();
        ScheduleThemeRefresh();
    }

    private static Color GetBlockColor(string tag)
    {
        return GetTagCategory(tag) switch
        {
            "Структура сторінки" => Color.FromRgb(54, 119, 220),
            "Текст і заголовки" => Color.FromRgb(103, 82, 196),
            "Посилання та медіа" => Color.FromRgb(35, 158, 177),
            "Списки" => Color.FromRgb(75, 166, 112),
            "Форми та введення" => Color.FromRgb(225, 145, 48),
            "Таблиці" => Color.FromRgb(204, 103, 69),
            "Інтерактивні елементи" => Color.FromRgb(194, 91, 151),
            _ => Color.FromRgb(91, 119, 145)
        };
    }

    private TreeViewItem BuildTreeItem(PageElement element)
    {
        Color blockColor = GetBlockColor(element.TagName);
        string category = GetTagCategory(element.TagName);
        string preview = string.IsNullOrWhiteSpace(element.Content) ? "" : element.Content.Length > 42 ? element.Content[..42] + "…" : element.Content;

        var header = new Border
        {
            Background = new SolidColorBrush(blockColor),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(12, 8, 14, 8),
            Margin = new Thickness(0, 2, 8, 5),
            MinWidth = 245,
            MaxWidth = 590,
            BorderBrush = new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)),
            BorderThickness = new Thickness(0, 0, 0, 3),
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new TextBlock { Text = "<" + element.TagName + ">", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13 },
                            new TextBlock { Text = "  " + category, Foreground = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), FontSize = 10, VerticalAlignment = VerticalAlignment.Center }
                        }
                    },
                    new TextBlock { Text = preview, Foreground = Brushes.White, FontSize = 11, Margin = new Thickness(1, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis }
                }
            }
        };

        var item = new TreeViewItem
        {
            Header = header,
            Tag = element,
            IsExpanded = true,
            Foreground = Brushes.White,
            FontWeight = _selected == element ? FontWeights.Bold : FontWeights.Normal,
            Padding = new Thickness(0),
            Margin = new Thickness(2, 1, 2, 2),
            AllowDrop = true,
            ToolTip = BuildTagToolTip(element.TagName),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        item.DragOver += Canvas_DragOver;
        item.Drop += Canvas_Drop;
        foreach (var child in element.Children) item.Items.Add(BuildTreeItem(child));
        return item;
    }

    private void CanvasTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Clicking a canvas block must move keyboard focus from the property editor
        // to the canvas, otherwise Delete may be routed to a stale TextBox instead.
        if (!CanvasTree.IsKeyboardFocusWithin)
            Keyboard.Focus(CanvasTree);
    }

    private void CanvasTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem item && item.Tag is PageElement element) SelectElement(element);
    }

    private void SelectElement(PageElement element)
    {
        _selected = element;
        _loadingProperties = true;
        TagBox.Text = element.TagName;
        ContentBox.Text = element.Content;
        IdBox.Text = element.Id;
        ClassBox.Text = element.ClassName;
        HrefBox.Text = element.Href;
        SrcBox.Text = element.Src;
        ExtraBox.Text = element.Extra;
        HrefBox.ToolTip = element.TagName.Equals("link", StringComparison.OrdinalIgnoreCase)
            ? "Шлях до таблиці стилів: styles.css або https://example.com/styles.css"
            : "Посилання (URL або відносний шлях)";
        ExtraBox.ToolTip = element.TagName.Equals("link", StringComparison.OrdinalIgnoreCase)
            ? "Для <link> тут вкажи rel: stylesheet, icon, preconnect тощо. Для CSS використовуй stylesheet."
            : element.TagName.Equals("meta", StringComparison.OrdinalIgnoreCase)
                ? "Для <meta> вкажи name (наприклад description) або charset; текст/вміст задається в полі вище."
                : "Alt / placeholder / type залежно від HTML-тега";
        ColorBox.Text = element.Color;
        BackgroundBox.Text = element.Background;
        FontSizeBox.Text = element.FontSize;
        // Migrate older projects where one "Spacing" value was applied to both sides.
        if (!string.IsNullOrWhiteSpace(element.Spacing))
        {
            if (string.IsNullOrWhiteSpace(element.Margin)) element.Margin = element.Spacing;
            if (string.IsNullOrWhiteSpace(element.Padding)) element.Padding = element.Spacing;
            element.Spacing = string.Empty;
        }
        if (SpacingTypeBox is not null) SpacingTypeBox.SelectedIndex = 0;
        SpacingBox.Text = element.Margin;
        CustomCssBox.Text = element.CustomCss;
        _loadingProperties = false;
        SyncCssSelectorToElement(element);

        TreeViewItem? canvasItem = FindCanvasTreeItem(element);
        if (canvasItem is not null && !canvasItem.IsSelected)
            canvasItem.IsSelected = true;

        UpdateCanvasSelectionVisuals();
    }

    private TreeViewItem? FindCanvasTreeItem(PageElement target)
    {
        foreach (TreeViewItem root in CanvasTree.Items)
        {
            TreeViewItem? found = FindCanvasTreeItem(root, target);
            if (found is not null) return found;
        }
        return null;
    }

    private static TreeViewItem? FindCanvasTreeItem(TreeViewItem current, PageElement target)
    {
        if (ReferenceEquals(current.Tag, target)) return current;
        foreach (TreeViewItem child in current.Items)
        {
            TreeViewItem? found = FindCanvasTreeItem(child, target);
            if (found is not null) return found;
        }
        return null;
    }

    private void SpacingType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingProperties || _selected is null || SpacingBox is null || SpacingTypeBox is null) return;
        SpacingBox.Text = SpacingTypeBox.SelectedIndex == 1 ? _selected.Padding : _selected.Margin;
        if (SpacingLabel is not null)
            SpacingLabel.Text = SpacingTypeBox.SelectedIndex == 1 ? "Внутрішній відступ (padding)" : "Зовнішній відступ (margin)";
    }

    private void Property_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingProperties || _selected is null) return;
        UpdateSelectedFromFields(false);
        if (ReferenceEquals(sender, IdBox) || ReferenceEquals(sender, ClassBox))
            SyncCssSelectorToElement(_selected);
    }

    private static string BuildCssSelectorForElement(PageElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.Id)) return "#" + element.Id.Trim();
        string[] classes = (element.ClassName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (classes.Length > 0) return "." + classes[0];
        return element.TagName;
    }

    private void SyncCssSelectorToElement(PageElement element)
    {
        _loadingCssDesigner = true;
        try { CssSelectorBox.Text = BuildCssSelectorForElement(element); }
        finally { _loadingCssDesigner = false; }
        LoadCssBlocksForSelector();
    }

    private void CssBlocksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CssBlocksList.SelectedItem is not CssDeclaration selected) return;
        _loadingCssDesigner = true;
        try
        {
            CssPropertyBox.Text = selected.Property;
            CssValueBox.Text = selected.Value;
        }
        finally { _loadingCssDesigner = false; }
    }

    private void UpdateSelectedFromFields(bool rerender)
    {
        if (_selected is null) return;
        _selected.Content = ContentBox.Text;
        _selected.Id = IdBox.Text;
        _selected.ClassName = ClassBox.Text;
        _selected.Href = HrefBox.Text;
        _selected.Src = SrcBox.Text;
        _selected.Extra = ExtraBox.Text;
        _selected.Color = ColorBox.Text;
        _selected.Background = BackgroundBox.Text;
        _selected.FontSize = FontSizeBox.Text;
        if (SpacingTypeBox?.SelectedIndex == 1) _selected.Padding = SpacingBox.Text;
        else _selected.Margin = SpacingBox.Text;
        _selected.Spacing = string.Empty;
        _selected.CustomCss = CustomCssBox.Text;
        if (rerender) RenderTree();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) { StatusText.Text = "Спочатку вибери блок на полотні"; return; }
        UpdateSelectedFromFields(true);
        StatusText.Text = "Властивості застосовано";
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Back) return;

        // Always prefer the actual selected canvas node. The property inspector can
        // otherwise keep keyboard focus after a click, leaving _selected out of sync
        // with the item visibly selected in CanvasTree.
        PageElement? canvasSelection = (CanvasTree.SelectedItem as TreeViewItem)?.Tag as PageElement;
        if (canvasSelection is not null)
            _selected = canvasSelection;

        if (_selected is null) return;

        DependencyObject? focus = Keyboard.FocusedElement as DependencyObject
            ?? e.OriginalSource as DependencyObject;
        bool eventFromCanvas = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject)?.Tag is PageElement;
        bool canvasHasFocus = CanvasTree.IsKeyboardFocusWithin || eventFromCanvas;

        // Preserve normal text editing, but don't let stale focus in the inspector
        // block removal after the user has selected a canvas item.
        if (!canvasHasFocus &&
            (FindAncestor<TextBox>(focus) is not null || FindAncestor<ComboBox>(focus) is not null ||
             FindAncestor<PasswordBox>(focus) is not null || FindAncestor<RichTextBox>(focus) is not null))
            return;

        DeleteSelectedElement();
        e.Handled = true;
    }

    private void DeleteSelectedElement()
    {
        if (_selected is null) return;
        if (RemoveElement(_elements, _selected))
        {
            _selected = null;
            ClearProperties();
            RenderTree();
            StatusText.Text = "Блок видалено";
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelectedElement();

    private static bool RemoveElement(ObservableCollection<PageElement> list, PageElement target)
    {
        if (list.Remove(target)) return true;
        foreach (var item in list) if (RemoveElement(item.Children, target)) return true;
        return false;
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);
    private void MoveSelected(int direction)
    {
        if (_selected is null) return;
        var parent = FindParentCollection(_elements, _selected);
        if (parent is null) return;
        int index = parent.IndexOf(_selected), next = index + direction;
        if (next < 0 || next >= parent.Count) return;
        parent.Move(index, next); RenderTree(); StatusText.Text = "Блок переміщено";
    }

    private static ObservableCollection<PageElement>? FindParentCollection(ObservableCollection<PageElement> list, PageElement target)
    {
        if (list.Contains(target)) return list;
        foreach (var item in list) { var found = FindParentCollection(item.Children, target); if (found is not null) return found; }
        return null;
    }

    private void ClearProperties()
    {
        _loadingProperties = true;
        foreach (var box in new[] { TagBox, ContentBox, IdBox, ClassBox, HrefBox, SrcBox, ExtraBox, ColorBox, BackgroundBox, FontSizeBox, SpacingBox, CustomCssBox }) box.Clear();
        if (SpacingTypeBox is not null) SpacingTypeBox.SelectedIndex = 0;
        if (SpacingLabel is not null) SpacingLabel.Text = "Зовнішній відступ (margin)";
        _loadingProperties = false;
    }

    private void NewPage_Click(object sender, RoutedEventArgs e)
    {
        if ((_elements.Count > 0 || !string.IsNullOrWhiteSpace(GlobalCssBox.Text) || !string.IsNullOrWhiteSpace(JavaScriptBox.Text) || _jsBlocks.Count > 0) &&
            MessageBox.Show("Створити нову сторінку й очистити HTML, CSS та JavaScript?", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _elements.Clear(); _selected = null; ClearProperties(); GlobalCssBox.Clear(); _globalCss = ""; JavaScriptBox.Clear(); _javaScript = ""; _jsBlocks.Clear(); RefreshJsBlockEditor(null); RenderTree(); StatusText.Text = "Створено нову сторінку";
    }

    private void JavaScript_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (JavaScriptBox is not null) _javaScript = JavaScriptBox.Text;
    }

    private void JavaScriptExample_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        _javaScript = button.Tag?.ToString() switch
        {
            "alert" => "document.addEventListener(\"DOMContentLoaded\", () => {\n" +
                       "  const button = document.getElementById(\"myButton\");\n" +
                       "  if (!button) {\n" +
                       "    console.warn(\"WebBlox: задай кнопці ID myButton у властивостях HTML.\");\n" +
                       "    return;\n" +
                       "  }\n" +
                       "  button.addEventListener(\"click\", () => {\n" +
                       "    alert(\"Привіт з WebBlox!\");\n" +
                       "  });\n" +
                       "});\n",
            "counter" => "document.addEventListener(\"DOMContentLoaded\", () => {\n" +
                         "  const button = document.getElementById(\"counterButton\");\n" +
                         "  const output = document.getElementById(\"counterValue\");\n" +
                         "  if (!button || !output) {\n" +
                         "    console.warn(\"WebBlox: потрібні елементи з ID counterButton та counterValue.\");\n" +
                         "    return;\n" +
                         "  }\n" +
                         "  let count = Number(output.textContent) || 0;\n" +
                         "  button.addEventListener(\"click\", () => {\n" +
                         "    count += 1;\n" +
                         "    output.textContent = String(count);\n" +
                         "  });\n" +
                         "});\n",
            _ => _javaScript
        };
        JavaScriptBox.Text = _javaScript;
        JavaScriptBox.CaretIndex = JavaScriptBox.Text.Length;
        JavaScriptBox.Focus();
        StatusText.Text = button.Tag?.ToString() == "counter"
            ? "Шаблон лічильника додано — признач ID елементам"
            : "Шаблон обробника кнопки додано — признач ID myButton";
    }

    private void BuildJavaScriptFunctionLibrary()
    {
        _allJsFunctionSnippets = new List<JsFunctionSnippet>();
        void S(string category, string title, string description, string requirements, string color, string code)
            => _allJsFunctionSnippets.Add(new JsFunctionSnippet(category, title, description, requirements, color, code));

        const string events = "#D5A323";
        const string dom = "#39956C";
        const string forms = "#347FC4";
        const string effects = "#9C5CAF";
        const string data = "#8461C6";
        const string games = "#D36555";
        const string network = "#298C9C";

        S("Кнопки та події", "Кнопка: показати повідомлення", "Показує alert після натискання кнопки.", "Потрібен елемент з ID myButton.", events,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                const button = document.getElementById('myButton');
                if (!button) return console.warn('WebBlox: додай кнопку з ID myButton');
                button.addEventListener('click', () => alert('Привіт з WebBlox!'));
              });
            })();
            """);

        S("Кнопки та події", "Лічильник натискань", "Рахує кількість натискань по кнопці та показує результат.", "Кнопка ID counterButton; елемент результату ID counterValue.", events,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                const button = document.getElementById('counterButton');
                const output = document.getElementById('counterValue');
                if (!button || !output) return console.warn('WebBlox: потрібні counterButton і counterValue');
                let count = Number(output.textContent) || 0;
                button.addEventListener('click', () => { count += 1; output.textContent = String(count); });
              });
            })();
            """);

        S("Текст і DOM", "Змінити текст елемента", "За кліком на одну кнопку змінює текст іншого елемента.", "Кнопка ID changeTextButton; цільовий елемент ID targetText.", dom,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                const button = document.getElementById('changeTextButton');
                const output = document.getElementById('targetText');
                if (!button || !output) return console.warn('WebBlox: потрібні changeTextButton і targetText');
                button.addEventListener('click', () => { output.textContent = 'Текст успішно змінено!'; });
              });
            })();
            """);

        S("Текст і DOM", "Показати або приховати блок", "Перемикає видимість цільового елемента.", "Кнопка ID toggleButton; блок ID targetElement.", dom,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                const button = document.getElementById('toggleButton');
                const target = document.getElementById('targetElement');
                if (!button || !target) return console.warn('WebBlox: потрібні toggleButton і targetElement');
                button.addEventListener('click', () => {
                  const willShow = target.hidden || getComputedStyle(target).display === 'none';
                  target.hidden = !willShow;
                  if (willShow) target.style.removeProperty('display');
                  else target.style.display = 'none';
                });
              });
            })();
            """);

        S("Текст і DOM", "Прокрутити до секції", "Плавно прокручує сторінку до секції за посиланням-якорем.", "Посилання href='#about' і секція ID about; працює для всіх якорів.", dom,
            """
            (() => {
              document.addEventListener('click', (event) => {
                const link = event.target.closest('a[href^="#"]');
                if (!link) return;
                const selector = link.getAttribute('href');
                if (!selector || selector === '#') return;
                const target = document.querySelector(selector);
                if (!target) return;
                event.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                history.replaceState(null, '', selector);
              });
            })();
            """);

        S("Ефекти та анімації", "Перемикач темної теми", "Перемикає легку темну тему для body без зовнішніх бібліотек.", "Кнопка ID themeToggle.", effects,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                if (!document.getElementById('webblox-dark-theme-css')) {
                  const style = document.createElement('style');
                  style.id = 'webblox-dark-theme-css';
                  style.textContent = 'body.wb-dark-theme{background:#111827!important;color:#f3f4f6!important}body.wb-dark-theme a{color:#93c5fd!important}body.wb-dark-theme input,body.wb-dark-theme textarea,body.wb-dark-theme select{background:#1f2937;color:#f9fafb;border-color:#4b5563}';
                  document.head.appendChild(style);
                }
                const button = document.getElementById('themeToggle');
                if (!button) return console.warn('WebBlox: додай кнопку з ID themeToggle');
                button.addEventListener('click', () => document.body.classList.toggle('wb-dark-theme'));
              });
            })();
            """);

        S("Ефекти та анімації", "Плавна поява карток", "Поступово показує елементи класу reveal при появі на екрані.", "Признач клас reveal елементам, які потрібно анімувати.", effects,
            """
            (() => {
              const style = document.createElement('style');
              style.textContent = '.wb-reveal{opacity:0;transform:translateY(16px);transition:opacity .5s ease,transform .5s ease}.wb-reveal.is-visible{opacity:1;transform:translateY(0)}';
              document.head.appendChild(style);
              const items = document.querySelectorAll('.reveal');
              items.forEach(item => item.classList.add('wb-reveal'));
              if (!('IntersectionObserver' in window)) {
                items.forEach(item => item.classList.add('is-visible'));
                return;
              }
              const observer = new IntersectionObserver(entries => entries.forEach(entry => {
                if (entry.isIntersecting) { entry.target.classList.add('is-visible'); observer.unobserve(entry.target); }
              }), { threshold: 0.12 });
              items.forEach(item => observer.observe(item));
            })();
            """);

        S("Форми", "Перевірити email у формі", "Не дозволяє відправити форму з неправильним email.", "Форма ID contactForm; поле input type=email всередині неї.", forms,
            """
            (() => {
              const run = (fn) => document.readyState === 'loading'
                ? document.addEventListener('DOMContentLoaded', fn, { once: true }) : fn();
              run(() => {
                const form = document.getElementById('contactForm');
                if (!form) return console.warn('WebBlox: додай форму з ID contactForm');
                form.addEventListener('submit', event => {
                  const email = form.querySelector('input[type="email"]');
                  if (!form.checkValidity() || (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim()))) {
                    event.preventDefault();
                    form.reportValidity();
                    if (email) email.focus();
                  }
                });
              });
            })();
            """);

        S("Форми", "Лічильник символів у полі", "Показує, скільки символів користувач ввів у текстове поле.", "Поле ID messageInput; текстовий лічильник ID charCount.", forms,
            """
            (() => {
              const input = document.getElementById('messageInput');
              const output = document.getElementById('charCount');
              if (!input || !output) return console.warn('WebBlox: потрібні messageInput і charCount');
              const update = () => { output.textContent = `${input.value.length} символів`; };
              input.addEventListener('input', update);
              update();
            })();
            """);

        S("Форми", "Показати або сховати пароль", "Перемикає тип password/text і напис на кнопці.", "Поле ID passwordInput; кнопка ID togglePasswordButton.", forms,
            """
            (() => {
              const input = document.getElementById('passwordInput');
              const button = document.getElementById('togglePasswordButton');
              if (!input || !button) return console.warn('WebBlox: потрібні passwordInput і togglePasswordButton');
              button.addEventListener('click', () => {
                const show = input.type === 'password';
                input.type = show ? 'text' : 'password';
                button.textContent = show ? 'Сховати пароль' : 'Показати пароль';
                button.setAttribute('aria-pressed', String(show));
              });
            })();
            """);

        S("Форми", "Пошук по картках", "Фільтрує картки за введеним текстом без перезавантаження сторінки.", "Поле ID searchInput; кожній картці признач клас search-item.", forms,
            """
            (() => {
              const input = document.getElementById('searchInput');
              const items = [...document.querySelectorAll('.search-item')];
              if (!input || items.length === 0) return console.warn('WebBlox: потрібні searchInput та елементи .search-item');
              input.addEventListener('input', () => {
                const query = input.value.trim().toLocaleLowerCase();
                items.forEach(item => {
                  item.hidden = !item.textContent.toLocaleLowerCase().includes(query);
                });
              });
            })();
            """);

        S("Форми", "Проста валідація обов’язкових полів", "Підсвічує незаповнені поля і показує повідомлення перед відправленням.", "Форма ID contactForm; для обов’язкових полів установи атрибут required.", forms,
            """
            (() => {
              const form = document.getElementById('contactForm');
              if (!form) return console.warn('WebBlox: додай форму з ID contactForm');
              form.addEventListener('submit', event => {
                if (!form.checkValidity()) {
                  event.preventDefault();
                  form.reportValidity();
                  const firstInvalid = form.querySelector(':invalid');
                  if (firstInvalid) firstInvalid.focus();
                } else {
                  event.preventDefault();
                  alert('Дякуємо! Форма пройшла перевірку. Підключи сервер, щоб реально надіслати дані.');
                }
              });
            })();
            """);

        S("Ігри й утиліти", "Випадкове число", "Показує випадкове ціле число від 1 до 100.", "Кнопка ID randomNumberButton; результат ID randomNumberValue.", games,
            """
            (() => {
              const button = document.getElementById('randomNumberButton');
              const output = document.getElementById('randomNumberValue');
              if (!button || !output) return console.warn('WebBlox: потрібні randomNumberButton і randomNumberValue');
              button.addEventListener('click', () => { output.textContent = String(Math.floor(Math.random() * 100) + 1); });
            })();
            """);

        S("Ігри й утиліти", "Калькулятор суми двох чисел", "Додає два числа і виводить результат на сторінку.", "Поля numberA і numberB; кнопка sumButton; результат sumResult.", games,
            """
            (() => {
              const a = document.getElementById('numberA');
              const b = document.getElementById('numberB');
              const button = document.getElementById('sumButton');
              const output = document.getElementById('sumResult');
              if (!a || !b || !button || !output) return console.warn('WebBlox: потрібні numberA, numberB, sumButton і sumResult');
              button.addEventListener('click', () => {
                const x = Number(a.value), y = Number(b.value);
                if (a.value.trim() === '' || b.value.trim() === '' || !Number.isFinite(x) || !Number.isFinite(y)) {
                  output.textContent = 'Введи два коректні числа';
                  return;
                }
                output.textContent = String(x + y);
              });
            })();
            """);

        S("Ігри й утиліти", "Цитата навмання", "Показує випадкову цитату з масиву, який легко змінити.", "Кнопка ID quoteButton; текстовий елемент ID quoteText.", games,
            """
            (() => {
              const quotes = [
                'Великі справи починаються з маленьких кроків.',
                'Помилка — це підказка, що можна зробити краще.',
                'Створи щось корисне, а потім зроби це ще кращим.'
              ];
              const button = document.getElementById('quoteButton');
              const output = document.getElementById('quoteText');
              if (!button || !output) return console.warn('WebBlox: потрібні quoteButton і quoteText');
              button.addEventListener('click', () => { output.textContent = quotes[Math.floor(Math.random() * quotes.length)]; });
            })();
            """);

        S("Ігри й утиліти", "Поточний рік у футері", "Автоматично встановлює поточний рік у всіх позначених елементах.", "Признач елементу ID currentYear або атрибут data-current-year.", games,
            """
            (() => {
              const year = String(new Date().getFullYear());
              document.querySelectorAll('[data-current-year], #currentYear').forEach(element => {
                element.textContent = year;
              });
            })();
            """);

        S("Ігри й утиліти", "Прокрутка догори", "Показує кнопку при прокручуванні та плавно повертає сторінку догори.", "Кнопка ID scrollTopButton.", games,
            """
            (() => {
              const button = document.getElementById('scrollTopButton');
              if (!button) return console.warn('WebBlox: додай кнопку з ID scrollTopButton');
              button.hidden = window.scrollY < 250;
              window.addEventListener('scroll', () => { button.hidden = window.scrollY < 250; }, { passive: true });
              button.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
            })();
            """);

        S("Ігри й утиліти", "Таймер зворотного відліку", "Рахує секунди від 10 до нуля після натискання кнопки.", "Кнопка ID startTimer; текстовий елемент ID timerValue.", games,
            """
            (() => {
              const button = document.getElementById('startTimer');
              const output = document.getElementById('timerValue');
              if (!button || !output) return console.warn('WebBlox: потрібні startTimer і timerValue');
              let timerId = null;
              button.addEventListener('click', () => {
                if (timerId !== null) clearInterval(timerId);
                let seconds = 10;
                output.textContent = String(seconds);
                timerId = setInterval(() => {
                  seconds -= 1;
                  output.textContent = String(Math.max(seconds, 0));
                  if (seconds <= 0) { clearInterval(timerId); timerId = null; output.textContent = 'Час вийшов!'; }
                }, 1000);
              });
            })();
            """);

        S("Дані та сховище", "Зберегти ім’я на цьому пристрої", "Зберігає введене ім’я у localStorage та показує його після повторного відкриття.", "Поле ID nameInput; кнопка ID saveNameButton; результат ID savedName.", data,
            """
            (() => {
              const input = document.getElementById('nameInput');
              const button = document.getElementById('saveNameButton');
              const output = document.getElementById('savedName');
              if (!input || !button || !output) return console.warn('WebBlox: потрібні nameInput, saveNameButton і savedName');
              try { input.value = localStorage.getItem('webblox-name') || ''; output.textContent = input.value; }
              catch (error) { console.warn('localStorage недоступний', error); }
              button.addEventListener('click', () => {
                try {
                  localStorage.setItem('webblox-name', input.value.trim());
                  output.textContent = input.value.trim() || 'Ім’я не вказано';
                } catch (error) { console.error('Не вдалося зберегти ім’я', error); }
              });
            })();
            """);

        S("Дані та сховище", "Список завдань із збереженням", "Додає завдання, дозволяє позначити виконаним і видалити; список зберігається в браузері.", "Поле todoInput; кнопка todoAdd; список ul/ol з ID todoList.", data,
            """
            (() => {
              const input = document.getElementById('todoInput');
              const addButton = document.getElementById('todoAdd');
              const list = document.getElementById('todoList');
              if (!input || !addButton || !list) return console.warn('WebBlox: потрібні todoInput, todoAdd і todoList');
              const key = 'webblox-todos-v1';
              let todos = [];
              try { todos = JSON.parse(localStorage.getItem(key) || '[]'); if (!Array.isArray(todos)) todos = []; }
              catch { todos = []; }
              const save = () => { try { localStorage.setItem(key, JSON.stringify(todos)); } catch (error) { console.warn(error); } };
              const render = () => {
                list.replaceChildren();
                todos.forEach((todo, index) => {
                  const item = document.createElement('li');
                  const checkbox = document.createElement('input'); checkbox.type = 'checkbox'; checkbox.checked = Boolean(todo.done);
                  checkbox.addEventListener('change', () => { todos[index].done = checkbox.checked; save(); render(); });
                  const label = document.createElement('span'); label.textContent = todo.text; label.style.margin = '0 8px';
                  if (todo.done) label.style.textDecoration = 'line-through';
                  const remove = document.createElement('button'); remove.type = 'button'; remove.textContent = 'Видалити';
                  remove.addEventListener('click', () => { todos.splice(index, 1); save(); render(); });
                  item.append(checkbox, label, remove); list.appendChild(item);
                });
              };
              const add = () => { const text = input.value.trim(); if (!text) return; todos.push({ text, done: false }); input.value = ''; save(); render(); input.focus(); };
              addButton.addEventListener('click', add);
              input.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); add(); } });
              render();
            })();
            """);

        S("Дані та сховище", "Скопіювати текст у буфер", "Копіює вміст елемента після натискання кнопки.", "Кнопка copyButton; елемент з текстом ID copyText. Браузер може вимагати HTTPS або localhost.", data,
            """
            (() => {
              const button = document.getElementById('copyButton');
              const source = document.getElementById('copyText');
              if (!button || !source) return console.warn('WebBlox: потрібні copyButton і copyText');
              button.addEventListener('click', async () => {
                try {
                  await navigator.clipboard.writeText(source.value ?? source.textContent ?? '');
                  const oldText = button.textContent;
                  button.textContent = 'Скопійовано!';
                  setTimeout(() => { button.textContent = oldText; }, 1200);
                } catch (error) { console.error('Не вдалося скопіювати текст', error); alert('Копіювання недоступне у цьому браузері.'); }
              });
            })();
            """);

        S("Дані та сховище", "Завантажити текстовий файл", "Створює локальний .txt-файл із тексту на сторінці.", "Кнопка ID downloadTextButton; джерело тексту ID downloadTextSource.", data,
            """
            (() => {
              const button = document.getElementById('downloadTextButton');
              const source = document.getElementById('downloadTextSource');
              if (!button || !source) return console.warn('WebBlox: потрібні downloadTextButton і downloadTextSource');
              button.addEventListener('click', () => {
                const text = source.value ?? source.textContent ?? '';
                const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a'); link.href = url; link.download = 'webblox.txt';
                document.body.appendChild(link); link.click(); link.remove();
                setTimeout(() => URL.revokeObjectURL(url), 1000);
              });
            })();
            """);

        S("Дані та сховище", "Попередній перегляд зображення", "Завантажує URL зображення в попередній перегляд.", "Поле URL ID imageUrlInput; кнопка showImageButton; зображення imagePreview.", data,
            """
            (() => {
              const input = document.getElementById('imageUrlInput');
              const button = document.getElementById('showImageButton');
              const image = document.getElementById('imagePreview');
              if (!input || !button || !image) return console.warn('WebBlox: потрібні imageUrlInput, showImageButton і imagePreview');
              button.addEventListener('click', () => {
                const url = input.value.trim();
                if (!url) return;
                image.onerror = () => { image.alt = 'Не вдалося завантажити зображення'; };
                image.onload = () => { image.alt = 'Попередній перегляд'; };
                image.src = url;
              });
            })();
            """);

        S("Дані та сховище", "Завантажити JSON з API", "Виконує GET-запит і показує форматований JSON на сторінці.", "Кнопка ID loadDataButton; результат ID apiResult. Зовнішній сервер має дозволяти CORS.", network,
            """
            (() => {
              const button = document.getElementById('loadDataButton');
              const output = document.getElementById('apiResult');
              if (!button || !output) return console.warn('WebBlox: потрібні loadDataButton і apiResult');
              button.addEventListener('click', async () => {
                button.disabled = true;
                output.textContent = 'Завантаження…';
                try {
                  const response = await fetch('https://jsonplaceholder.typicode.com/todos/1');
                  if (!response.ok) throw new Error(`HTTP ${response.status}`);
                  const data = await response.json();
                  output.textContent = JSON.stringify(data, null, 2);
                } catch (error) { output.textContent = `Помилка: ${error.message}`; }
                finally { button.disabled = false; }
              });
            })();
            """);

        S("Кнопки та події", "Підтвердити видалення", "Показує діалог підтвердження й виконує код лише після згоди.", "Кнопка ID deleteButton; елемент для видалення ID itemToDelete.", events,
            """
            (() => {
              const button = document.getElementById('deleteButton');
              const target = document.getElementById('itemToDelete');
              if (!button || !target) return console.warn('WebBlox: потрібні deleteButton і itemToDelete');
              button.addEventListener('click', () => {
                if (confirm('Точно видалити цей елемент?')) target.remove();
              });
            })();
            """);

        S("Кнопки та події", "Відкрити або закрити діалог", "Працює з нативним HTML dialog.", "Діалог ID myDialog; кнопки myDialogOpen і myDialogClose.", events,
            """
            (() => {
              const dialog = document.getElementById('myDialog');
              const openButton = document.getElementById('myDialogOpen');
              const closeButton = document.getElementById('myDialogClose');
              if (!(dialog instanceof HTMLDialogElement) || !openButton || !closeButton)
                return console.warn('WebBlox: потрібні dialog myDialog і кнопки myDialogOpen/myDialogClose');
              openButton.addEventListener('click', () => { if (!dialog.open) dialog.showModal(); });
              closeButton.addEventListener('click', () => dialog.close());
              dialog.addEventListener('click', event => { if (event.target === dialog) dialog.close(); });
            })();
            """);

        S("Ефекти та анімації", "Карусель зображень", "Перемикає зображення вперед і назад у простій каруселі.", "Зображення ID carouselImage; кнопки carouselPrev і carouselNext; заміни URL у масиві.", effects,
            """
            (() => {
              const image = document.getElementById('carouselImage');
              const prev = document.getElementById('carouselPrev');
              const next = document.getElementById('carouselNext');
              const images = ['https://picsum.photos/id/1015/800/450', 'https://picsum.photos/id/1016/800/450', 'https://picsum.photos/id/1018/800/450'];
              if (!image || !prev || !next) return console.warn('WebBlox: потрібні carouselImage, carouselPrev і carouselNext');
              let index = 0;
              const show = () => { image.src = images[index]; image.alt = `Зображення ${index + 1} з ${images.length}`; };
              prev.addEventListener('click', () => { index = (index - 1 + images.length) % images.length; show(); });
              next.addEventListener('click', () => { index = (index + 1) % images.length; show(); });
              show();
            })();
            """);

        S("Ігри й утиліти", "Вікторина з однією відповіддю", "Перевіряє відповідь користувача та показує результат.", "Поле ID quizAnswer; кнопка quizCheck; результат quizResult. Правильна відповідь зараз: Київ.", games,
            """
            (() => {
              const input = document.getElementById('quizAnswer');
              const button = document.getElementById('quizCheck');
              const output = document.getElementById('quizResult');
              if (!input || !button || !output) return console.warn('WebBlox: потрібні quizAnswer, quizCheck і quizResult');
              button.addEventListener('click', () => {
                const correct = input.value.trim().toLocaleLowerCase() === 'київ';
                output.textContent = correct ? 'Правильно! +1 бал' : 'Спробуй ще раз.';
                output.style.color = correct ? '#15803d' : '#b91c1c';
              });
            })();
            """);

        S("Ігри й утиліти", "Випадковий колір фону", "Змінює колір фону сторінки на випадковий пастельний колір.", "Кнопка ID randomColorButton.", games,
            """
            (() => {
              const button = document.getElementById('randomColorButton');
              if (!button) return console.warn('WebBlox: додай кнопку з ID randomColorButton');
              button.addEventListener('click', () => {
                const hue = Math.floor(Math.random() * 360);
                document.body.style.backgroundColor = `hsl(${hue} 70% 90%)`;
              });
            })();
            """);

        S("Утиліти JavaScript", "Функція debounce", "Відкладає часті виклики, доки користувач трохи не зупиниться — корисно для пошуку та автозбереження.", "Створює window.wbDebounce(fn, delay). Приклад: const search = wbDebounce(() => console.log('search'), 300);", data,
            """
            window.wbDebounce = function (callback, delay = 250) {
              let timerId;
              return function (...args) {
                clearTimeout(timerId);
                const context = this;
                timerId = setTimeout(() => callback.apply(context, args), Math.max(0, Number(delay) || 0));
              };
            };
            """);

        S("Утиліти JavaScript", "Обмежити число діапазоном", "Повертає число не менше min і не більше max.", "Створює window.wbClamp(value, min, max). Приклад: wbClamp(120, 0, 100) поверне 100.", data,
            """
            window.wbClamp = function (value, min, max) {
              const lower = Math.min(Number(min), Number(max));
              const upper = Math.max(Number(min), Number(max));
              const number = Number(value);
              if (![lower, upper, number].every(Number.isFinite)) return NaN;
              return Math.min(Math.max(number, lower), upper);
            };
            """);

        S("Утиліти JavaScript", "Випадкове ціле число", "Повертає випадкове ціле число у включному діапазоні.", "Створює window.wbRandomInt(min, max). Приклад: wbRandomInt(1, 6) для кубика.", data,
            """
            window.wbRandomInt = function (min, max) {
              const a = Math.ceil(Number(min));
              const b = Math.floor(Number(max));
              if (!Number.isFinite(a) || !Number.isFinite(b) || b < a) throw new RangeError('Невірний діапазон');
              return Math.floor(Math.random() * (b - a + 1)) + a;
            };
            """);

        S("Утиліти JavaScript", "Перемішати масив", "Створює новий масив у випадковому порядку, не змінюючи вихідний.", "Створює window.wbShuffle(array). Приклад: wbShuffle(['a', 'b', 'c']).", data,
            """
            window.wbShuffle = function (items) {
              if (!Array.isArray(items)) return [];
              const result = [...items];
              for (let i = result.length - 1; i > 0; i -= 1) {
                const j = Math.floor(Math.random() * (i + 1));
                [result[i], result[j]] = [result[j], result[i]];
              }
              return result;
            };
            """);

        S("Утиліти JavaScript", "Форматувати дату українською", "Перетворює дату на читабельний український формат.", "Створює window.wbFormatDate(value). Приклад: wbFormatDate(new Date()).", data,
            """
            window.wbFormatDate = function (value = new Date()) {
              const date = value instanceof Date ? value : new Date(value);
              if (Number.isNaN(date.getTime())) return 'Невідома дата';
              return new Intl.DateTimeFormat('uk-UA', { day: 'numeric', month: 'long', year: 'numeric' }).format(date);
            };
            """);

        S("Утиліти JavaScript", "Безпечно екранувати HTML-текст", "Перетворює спецсимволи на HTML-сутності, щоб текст не трактувався як розмітка.", "Створює window.wbEscapeHtml(value). Для показу тексту зазвичай краще використовувати textContent.", data,
            """
            window.wbEscapeHtml = function (value) {
              const element = document.createElement('span');
              element.textContent = String(value ?? '');
              return element.innerHTML;
            };
            """);

        S("Утиліти JavaScript", "Пауза для async-функцій", "Створює проміс, який завершується через вказану кількість мілісекунд.", "Створює window.wbSleep(ms). Використання: await wbSleep(500); всередині async-функції.", data,
            """
            window.wbSleep = function (milliseconds = 0) {
              const delay = Math.max(0, Number(milliseconds) || 0);
              return new Promise(resolve => setTimeout(resolve, delay));
            };
            """);

        S("Утиліти JavaScript", "Створити URL-slug", "Перетворює текст на короткий URL-дружній ідентифікатор латиницею та цифрами.", "Створює window.wbSlugify(text) і транслітерує українські літери. Приклад: wbSlugify('Моя сторінка') → 'moya-storinka'.", data,
            """
            window.wbSlugify = function (text) {
              const letters = {
                'а':'a','б':'b','в':'v','г':'h','ґ':'g','д':'d','е':'e','є':'ye','ж':'zh','з':'z','и':'y','і':'i','ї':'yi','й':'i',
                'к':'k','л':'l','м':'m','н':'n','о':'o','п':'p','р':'r','с':'s','т':'t','у':'u','ф':'f','х':'kh','ц':'ts','ч':'ch',
                'ш':'sh','щ':'shch','ь':'','ю':'yu','я':'ya','ъ':'','ы':'y','э':'e','ё':'yo','’':'','ʼ':'','-':'-'
              };
              return Array.from(String(text ?? '').toLocaleLowerCase('uk-UA'), character => letters[character] ?? character).join('')
                .normalize('NFD')
                .replace(/[\u0300-\u036f]/g, '')
                .trim()
                .replace(/[^a-z0-9]+/g, '-')
                .replace(/^-+|-+$/g, '');
            };
            """);

        S("Утиліти JavaScript", "Форматувати число", "Форматує число локальними роздільниками української мови.", "Створює window.wbFormatNumber(value). Приклад: wbFormatNumber(12345.67).", data,
            """
            window.wbFormatNumber = function (value) {
              const number = Number(value);
              if (!Number.isFinite(number)) return '—';
              return new Intl.NumberFormat('uk-UA', { maximumFractionDigits: 2 }).format(number);
            };
            """);

        S("Утиліти JavaScript", "Перемкнути акордеон", "Відкриває або закриває панелі акордеона з доступними aria-атрибутами.", "Кожну групу обгорни у [data-wb-accordion], кнопку познач класом wb-accordion-toggle, панель — wb-accordion-panel.", effects,
            """
            (() => {
              document.querySelectorAll('[data-wb-accordion]').forEach(group => {
                const button = group.querySelector('.wb-accordion-toggle');
                const panel = group.querySelector('.wb-accordion-panel');
                if (!button || !panel) return;
                panel.hidden = true;
                button.setAttribute('aria-expanded', 'false');
                button.addEventListener('click', () => {
                  const open = panel.hidden;
                  panel.hidden = !open;
                  button.setAttribute('aria-expanded', String(open));
                });
              });
            })();
            """);

        S("Утиліти JavaScript", "Перемикання вкладок сайту", "Перемикає панелі в межах HTML-сторінки, зберігаючи aria-selected та hidden.", "Кнопки мають data-wb-tab-target='#panel1'; панелі — відповідний ID і атрибут data-wb-tab-panel.", effects,
            """
            (() => {
              const buttons = [...document.querySelectorAll('[data-wb-tab-target]')];
              const panels = [...document.querySelectorAll('[data-wb-tab-panel]')];
              if (!buttons.length || !panels.length) return console.warn('WebBlox: додай data-wb-tab-target і data-wb-tab-panel');
              const activate = button => {
                const target = button.getAttribute('data-wb-tab-target');
                buttons.forEach(item => item.setAttribute('aria-selected', String(item === button)));
                panels.forEach(panel => { panel.hidden = ('#' + panel.id) !== target; });
              };
              buttons.forEach(button => button.addEventListener('click', () => activate(button)));
              activate(buttons.find(button => document.querySelector(button.getAttribute('data-wb-tab-target'))) || buttons[0]);
            })();
            """);

        S("Утиліти JavaScript", "Живий цифровий годинник", "Оновлює поточний час щосекунди.", "Потрібен елемент ID liveClock.", effects,
            """
            (() => {
              const output = document.getElementById('liveClock');
              if (!output) return console.warn('WebBlox: додай елемент з ID liveClock');
              const update = () => { output.textContent = new Intl.DateTimeFormat('uk-UA', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(new Date()); };
              update();
              window.setInterval(update, 1000);
            })();
            """);

        _visibleJsFunctionSnippets.Clear();
        JsFunctionPalette.ItemsSource = _visibleJsFunctionSnippets;

        var jsCategories = new List<CssPaletteCategory>
        {
            new("Усі функції", "#4C97FF", "Усі функції")
        };
        jsCategories.AddRange(_allJsFunctionSnippets
            .GroupBy(item => item.Category)
            .Select(group => new CssPaletteCategory(group.Key, group.First().Color, group.Key)));
        JsFunctionCategoryList.ItemsSource = jsCategories;
        JsFunctionCategoryList.SelectedIndex = 0;
        ApplyJsFunctionFilter();
        if (JsFunctionPalette.Items.Count > 0) JsFunctionPalette.SelectedIndex = 0;
        UpdateJsFunctionDetails(JsFunctionPalette.SelectedItem as JsFunctionSnippet);
    }

    private void ApplyJsFunctionFilter()
    {
        if (JsFunctionCategoryList is null || JsFunctionSearchBox is null || JsFunctionPalette is null) return;
        string category = (JsFunctionCategoryList.SelectedItem as CssPaletteCategory)?.GroupKey ?? "Усі функції";
        string query = JsFunctionSearchBox.Text?.Trim() ?? "";
        var matches = _allJsFunctionSnippets.Where(item =>
            (category == "Усі функції" || item.Category.Equals(category, StringComparison.Ordinal)) &&
            (query.Length == 0 || item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             item.Requirements.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             item.Category.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        _visibleJsFunctionSnippets.Clear();
        foreach (JsFunctionSnippet item in matches) _visibleJsFunctionSnippets.Add(item);
        if (JsFunctionCountText is not null) JsFunctionCountText.Text = $"ГОТОВІ ФУНКЦІЇ · {matches.Count}";
        if (JsFunctionPalette.Items.Count > 0 && JsFunctionPalette.SelectedItem is null) JsFunctionPalette.SelectedIndex = 0;
        else if (JsFunctionPalette.Items.Count == 0) UpdateJsFunctionDetails(null);
        Dispatcher.BeginInvoke(new Action(() => FindVisualChild<ScrollViewer>(JsFunctionPalette)?.ScrollToTop()));
    }

    private void JsFunctionSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyJsFunctionFilter();
    private void JsFunctionCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyJsFunctionFilter();

    private void JsFunctionPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateJsFunctionDetails(JsFunctionPalette.SelectedItem as JsFunctionSnippet);

    private void UpdateJsFunctionDetails(JsFunctionSnippet? snippet)
    {
        if (JsFunctionDetailsTitleText is null) return;
        JsFunctionDetailsTitleText.Text = snippet is null ? "ОБЕРИ ФУНКЦІЮ" : snippet.Title.ToUpperInvariant();
        JsFunctionDetailsDescriptionText.Text = snippet?.Description ?? "Вибери картку, щоб переглянути опис і вимоги.";
        JsFunctionRequirementsText.Text = snippet is null ? "" : "Потрібно: " + snippet.Requirements;
    }

    private void JsFunctionPalette_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => AppendSelectedJsFunction(openCodeTab: true);

    private void AppendJsFunction_Click(object sender, RoutedEventArgs e)
        => AppendSelectedJsFunction(openCodeTab: true);

    private void AppendSelectedJsFunction(bool openCodeTab)
    {
        if (JsFunctionPalette.SelectedItem is not JsFunctionSnippet snippet) return;
        string current = JavaScriptBox.Text.TrimEnd();
        string separator = $"{Environment.NewLine}{Environment.NewLine}// ===== WebBlox: {snippet.Title} ====={Environment.NewLine}";
        JavaScriptBox.Text = current.Length == 0 ? snippet.Code.Trim() : current + separator + snippet.Code.Trim();
        JavaScriptBox.CaretIndex = JavaScriptBox.Text.Length;
        if (openCodeTab)
        {
            InspectorTabs.SelectedIndex = 2;
            JavaScriptEditorTabs.SelectedIndex = 1;
            JavaScriptBox.Focus();
        }
        StatusText.Text = $"Функцію «{snippet.Title}» додано до JavaScript";
    }

    private void ReplaceJsFunction_Click(object sender, RoutedEventArgs e)
    {
        if (JsFunctionPalette.SelectedItem is not JsFunctionSnippet snippet) return;
        if (!string.IsNullOrWhiteSpace(JavaScriptBox.Text) &&
            MessageBox.Show("Замінити весь поточний JavaScript вибраним шаблоном?", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        JavaScriptBox.Text = snippet.Code.Trim();
        JavaScriptBox.CaretIndex = 0;
        InspectorTabs.SelectedIndex = 2;
        JavaScriptEditorTabs.SelectedIndex = 1;
        JavaScriptBox.Focus();
        StatusText.Text = $"JavaScript замінено шаблоном «{snippet.Title}»";
    }

    private void AddJsFunctionAsBlock_Click(object sender, RoutedEventArgs e)
    {
        if (JsFunctionPalette.SelectedItem is not JsFunctionSnippet snippet) return;
        var block = new JsVisualBlock
        {
            Kind = "custom-code",
            Category = snippet.Category,
            Title = snippet.Title,
            Description = snippet.Description,
            Color = snippet.Color,
            BodyLabel = "Код JavaScript",
            Body = snippet.Code.Trim()
        };
        _jsBlocks.Add(block);
        JavaScriptEditorTabs.SelectedIndex = 0;
        JsBlockWorkspace.SelectedItem = block;
        InspectorTabs.SelectedIndex = 2;
        StatusText.Text = $"Функцію «{snippet.Title}» додано як візуальний блок";
    }

    private void ShowJsBlocks_Click(object sender, RoutedEventArgs e)
    {
        JavaScriptEditorTabs.SelectedIndex = 0;
        InspectorTabs.SelectedIndex = 2;
        StatusText.Text = "Відкрито візуальні JavaScript-блоки";
    }

    private void BuildJavaScriptBlocksEditor()
    {
        _allJsBlockTemplates = new List<JsBlockTemplate>();
        void T(string category, string title, string description, string kind, string color,
            string p1Label = "", string p1 = "", string p2Label = "", string p2 = "",
            string p3Label = "", string p3 = "", string bodyLabel = "", string body = "")
        {
            _allJsBlockTemplates.Add(new JsBlockTemplate(category, title, description, kind, color,
                p1Label, p1, p2Label, p2, p3Label, p3, bodyLabel, body));
        }

        const string events = "#D5A323";
        const string dom = "#39956C";
        const string css = "#347FC4";
        const string data = "#8461C6";
        const string flow = "#BB7045";
        const string funcs = "#9C5CAF";
        const string dialogs = "#D36555";
        const string storage = "#637D9B";
        const string network = "#298C9C";
        const string browser = "#4B9784";
        const string custom = "#64748B";

        // Події — найтиповіша точка входу для блокового програмування.
        T("Події", "Коли сторінка завантажилась", "Запускає код після побудови HTML-документа.", "page-load", events,
            bodyLabel: "Код після завантаження", body: "console.log('WebBlox готовий!');");
        T("Події", "Коли натиснуто елемент", "Запускає дію при кліку по елементу.", "event-listener", events,
            "ID / CSS-селектор", "myButton", "Подія", "click", bodyLabel: "Код при кліку", body: "alert('Кнопка працює!');");
        T("Події", "Коли двічі клацнули", "Обробник подвійного кліку.", "event-listener", events,
            "ID / CSS-селектор", "myButton", "Подія", "dblclick", bodyLabel: "Код при події", body: "console.log('Подвійний клік');");
        T("Події", "Коли змінюється текст у полі", "Реакція на введення користувача.", "event-listener", events,
            "ID / CSS-селектор", "myInput", "Подія", "input", bodyLabel: "Код при введенні", body: "console.log('Введено:', event.target.value);");
        T("Події", "Коли значення змінено", "Реакція на зміну значення поля або перемикача.", "event-listener", events,
            "ID / CSS-селектор", "myInput", "Подія", "change", bodyLabel: "Код при зміні", body: "console.log('Нове значення:', event.target.value);");
        T("Події", "Коли курсор наведено", "Запускає код при наведенні курсора.", "event-listener", events,
            "ID / CSS-селектор", "myCard", "Подія", "mouseenter", bodyLabel: "Код при наведенні", body: "event.currentTarget.classList.add('is-hovered');");
        T("Події", "Коли курсор забрано", "Запускає код після відведення курсора.", "event-listener", events,
            "ID / CSS-селектор", "myCard", "Подія", "mouseleave", bodyLabel: "Код після наведення", body: "event.currentTarget.classList.remove('is-hovered');");
        T("Події", "Коли відправляється форма", "Перехоплює submit і не перезавантажує сторінку автоматично.", "event-listener", events,
            "ID / CSS-селектор форми", "myForm", "Подія", "submit", bodyLabel: "Код після перевірки", body: "event.preventDefault();\nalert('Форму оброблено!');");
        T("Події", "Коли натиснуто клавішу", "Слухає клавіатуру на всій сторінці.", "keydown", events,
            "Клавіша", "Enter", bodyLabel: "Код при клавіші", body: "console.log('Натиснуто Enter');");
        T("Події", "Коли змінюється розмір вікна", "Виконує дію після зміни розмірів браузера.", "event-listener", events,
            "Ціль", "window", "Подія", "resize", bodyLabel: "Код при зміні розміру", body: "console.log('Ширина:', window.innerWidth);");

        // HTML і DOM.
        T("HTML та DOM", "Змінити текст", "Безпечно змінює текст усередині елемента.", "set-text", dom,
            "ID / CSS-селектор", "message", "Новий текст", "Привіт!", bodyLabel: "");
        T("HTML та DOM", "Змінити HTML-вміст", "Вставляє HTML-розмітку всередину елемента.", "set-html", dom,
            "ID / CSS-селектор", "content", "HTML-вміст", "<strong>Готово!</strong>");
        T("HTML та DOM", "Показати елемент", "Робить елемент видимим.", "show-element", dom,
            "ID / CSS-селектор", "message");
        T("HTML та DOM", "Приховати елемент", "Приховує елемент.", "hide-element", dom,
            "ID / CSS-селектор", "message");
        T("HTML та DOM", "Перемкнути видимість", "Показує або приховує елемент.", "toggle-element", dom,
            "ID / CSS-селектор", "menu");
        T("HTML та DOM", "Змінити значення поля", "Установлює value для input, textarea або select.", "set-value", dom,
            "ID / CSS-селектор", "myInput", "Нове значення", "WebBlox");
        T("HTML та DOM", "Змінити placeholder", "Задає підказку поля введення.", "set-placeholder", dom,
            "ID / CSS-селектор", "myInput", "Placeholder", "Введи текст...");
        T("HTML та DOM", "Установити атрибут", "Задає або оновлює HTML-атрибут.", "set-attribute", dom,
            "ID / CSS-селектор", "myImage", "Атрибут", "alt", "Значення", "Опис зображення");
        T("HTML та DOM", "Видалити атрибут", "Видаляє атрибут елемента.", "remove-attribute", dom,
            "ID / CSS-селектор", "myButton", "Атрибут", "disabled");
        T("HTML та DOM", "Додати CSS-клас", "Додає клас без стирання інших класів.", "add-class", dom,
            "ID / CSS-селектор", "myCard", "Назва класу", "active");
        T("HTML та DOM", "Видалити CSS-клас", "Видаляє вказаний клас.", "remove-class", dom,
            "ID / CSS-селектор", "myCard", "Назва класу", "active");
        T("HTML та DOM", "Перемкнути CSS-клас", "Перемикає наявність класу.", "toggle-class", dom,
            "ID / CSS-селектор", "myCard", "Назва класу", "active");
        T("HTML та DOM", "Перемістити до елемента", "Плавно прокручує сторінку до заданого елемента.", "scroll-to-element", dom,
            "ID / CSS-селектор", "#contacts");
        T("HTML та DOM", "Сфокусувати поле", "Переміщує клавіатурний фокус на елемент.", "focus-element", dom,
            "ID / CSS-селектор", "myInput");
        T("HTML та DOM", "Видалити елемент", "Видаляє вибраний DOM-елемент.", "remove-element", dom,
            "ID / CSS-селектор", "temporaryBlock");
        T("HTML та DOM", "Створити дочірній елемент", "Створює HTML-тег і додає його до контейнера.", "create-element", dom,
            "Батьківський ID / селектор", "container", "Тег", "p", "Текст", "Новий абзац");
        T("HTML та DOM", "Змінити джерело зображення", "Оновлює src для img.", "set-src", dom,
            "ID / CSS-селектор", "myImage", "URL зображення", "images/photo.jpg");
        T("HTML та DOM", "Змінити посилання", "Оновлює href у посилання.", "set-href", dom,
            "ID / CSS-селектор", "myLink", "URL", "https://example.com");
        T("HTML та DOM", "Показати дату в елементі", "Виводить поточну локальну дату.", "current-date-text", dom,
            "ID / CSS-селектор", "todayDate");
        T("HTML та DOM", "Показати URL сторінки", "Виводить поточне посилання сторінки.", "current-url-text", dom,
            "ID / CSS-селектор", "currentUrl");
        T("HTML та DOM", "Порахувати елементи", "Зберігає кількість елементів за CSS-селектором у window-змінну.", "count-elements", dom,
            "CSS-селектор", ".card", "Назва змінної", "cardCount");

        // CSS і візуальні ефекти.
        T("CSS та анімації", "Змінити CSS-властивість", "Встановлює одну CSS-властивість елемента.", "set-style", css,
            "ID / CSS-селектор", "myCard", "CSS-властивість", "border-radius", "Значення", "16px");
        T("CSS та анімації", "Змінити колір тексту", "Задає колір тексту елемента.", "set-style", css,
            "ID / CSS-селектор", "myTitle", "CSS-властивість", "color", "Значення", "#2878e5");
        T("CSS та анімації", "Змінити колір фону", "Задає фон елемента.", "set-style", css,
            "ID / CSS-селектор", "myCard", "CSS-властивість", "background-color", "Значення", "#e2ebf5");
        T("CSS та анімації", "Змінити CSS-змінну", "Задає CSS custom property на :root або вибраному елементі.", "set-css-variable", css,
            "Ціль (ID / селектор)", "html", "CSS-змінна", "--brand-color", "Значення", "#2878e5");
        T("CSS та анімації", "Плавна поява", "Анімує появу елемента через Web Animations API.", "animate-in", css,
            "ID / CSS-селектор", "myCard", "Тривалість (мс)", "450");
        T("CSS та анімації", "Плавне зникнення", "Анімує зникнення, потім ховає елемент.", "animate-out", css,
            "ID / CSS-селектор", "myCard", "Тривалість (мс)", "350");
        T("CSS та анімації", "Пульсація елемента", "Коротка повторювана анімація масштабу.", "animate-pulse", css,
            "ID / CSS-селектор", "myButton", "Тривалість (мс)", "600", "Кількість повторів", "2");
        T("CSS та анімації", "Прокрутити сторінку догори", "Плавно повертає на початок сторінки.", "scroll-top", css);
        T("CSS та анімації", "Увімкнути темний клас", "Перемикає клас dark на HTML-документі.", "toggle-page-dark", css,
            "Клас темної теми", "dark");

        // Змінні, обчислення та відлагодження.
        T("Змінні й математика", "Створити змінну", "Створює глобальне значення window[name], доступне іншим блокам.", "set-variable", data,
            "Назва змінної", "score", "Початкове значення (JS-вираз)", "0");
        T("Змінні й математика", "Задати значення змінній", "Оновлює window[name] виразом JavaScript.", "set-variable", data,
            "Назва змінної", "score", "Нове значення (JS-вираз)", "10");
        T("Змінні й математика", "Збільшити змінну", "Додає число до window[name].", "increment-variable", data,
            "Назва змінної", "score", "На скільки", "1");
        T("Змінні й математика", "Зменшити змінну", "Віднімає число від window[name].", "decrement-variable", data,
            "Назва змінної", "score", "На скільки", "1");
        T("Змінні й математика", "Випадкове ціле число", "Зберігає випадкове ціле число включно з межею max.", "random-number", data,
            "Назва змінної", "randomNumber", "Мінімум", "1", "Максимум", "10");
        T("Змінні й математика", "Обчислити вираз", "Обчислює JS-вираз і зберігає результат у window[name].", "math-expression", data,
            "Назва змінної", "result", "Математичний вираз", "(5 + 3) * 2");
        T("Змінні й математика", "Вивести повідомлення в консоль", "Виводить текст у DevTools Console.", "console-log", data,
            "Текст", "Повідомлення з WebBlox");
        T("Змінні й математика", "Вивести вираз у консоль", "Виводить обчислений вираз у консоль.", "console-expression", data,
            "JS-вираз", "window.score");
        T("Змінні й математика", "Попередження в консолі", "Виводить console.warn.", "console-warn", data,
            "Текст", "Перевір значення");
        T("Змінні й математика", "Поточний час у мілісекундах", "Зберігає Date.now() у window-змінну.", "timestamp", data,
            "Назва змінної", "timestamp");

        // Умови та цикли. Код тіла доступний у багаторядковому полі блока.
        T("Умови та цикли", "Якщо умова істинна", "Виконує код усередині, лише коли умова JavaScript істинна.", "if", flow,
            "Умова (JS-вираз)", "window.score > 5", bodyLabel: "Код, якщо так", body: "console.log('Більше за 5');");
        T("Умови та цикли", "Якщо / інакше", "Вибирає одну з двох гілок за умовою.", "if-else", flow,
            "Умова (JS-вираз)", "window.score > 5", "Код, якщо ні", "console.log('5 або менше');", bodyLabel: "Код, якщо так", body: "console.log('Більше за 5');");
        T("Умови та цикли", "Повторити N разів", "Цикл for із заданою кількістю повторів.", "repeat", flow,
            "Кількість повторів (вираз)", "5", bodyLabel: "Код кожного повтору", body: "console.log('Повтор');");
        T("Умови та цикли", "Повторювати поки умова істинна", "Цикл while; переконайся, що умова колись стане false.", "while", flow,
            "Умова (JS-вираз)", "window.score < 10", bodyLabel: "Код циклу", body: "window.score = (Number(window.score) || 0) + 1;");
        T("Умови та цикли", "Спробувати / перехопити помилку", "Запускає код у try/catch.", "try-catch", flow,
            "Назва змінної помилки", "error", "Код catch (один рядок або вираз)", "console.error(error);", bodyLabel: "Код try", body: "JSON.parse('{invalid json}');");
        T("Умови та цикли", "Виконати власний JavaScript", "Для досвідчених користувачів: встав довільний код у блок.", "custom-code", flow,
            bodyLabel: "JavaScript-код", body: "// Встав свій JavaScript сюди");

        // Функції й таймери.
        T("Функції й таймери", "Створити функцію", "Функцію можна викликати іншими блоками як window.functionName().", "function", funcs,
            "Назва функції", "showMessage", "Параметри через кому", "", bodyLabel: "Код функції", body: "console.log('Функцію викликано');");
        T("Функції й таймери", "Викликати функцію", "Викликає раніше оголошену функцію з window.", "call-function", funcs,
            "Назва функції", "showMessage", "Аргументи (JS-вирази)", "");
        T("Функції й таймери", "Виконати через затримку", "Запускає код один раз після затримки.", "timeout", funcs,
            "Затримка (мс)", "1000", bodyLabel: "Код після затримки", body: "console.log('Минуло 1 сек');");
        T("Функції й таймери", "Повторювати через інтервал", "Запускає код кожні N мілісекунд; збережи ID таймера, щоб його зупинити.", "interval", funcs,
            "Назва змінної таймера", "webbloxTimer", "Інтервал (мс)", "1000", bodyLabel: "Код повтору", body: "console.log('Тік');");
        T("Функції й таймери", "Зупинити інтервал", "Зупиняє таймер, створений блоком інтервалу.", "clear-interval", funcs,
            "Назва змінної таймера", "webbloxTimer");
        T("Функції й таймери", "Почекати й потім виконати", "Спрощений блок відкладеної дії.", "timeout", funcs,
            "Почекати (мс)", "500", bodyLabel: "Код після очікування", body: "console.log('Готово');");

        // Діалоги та навігація.
        T("Повідомлення й діалоги", "Показати повідомлення", "Показує просте вікно alert.", "alert", dialogs,
            "Текст повідомлення", "Привіт з WebBlox!");
        T("Повідомлення й діалоги", "Запитати підтвердження", "Показує confirm і зберігає відповідь у window-змінну.", "confirm", dialogs,
            "Назва змінної", "isConfirmed", "Запитання", "Продовжити?");
        T("Повідомлення й діалоги", "Запросити текст", "Показує prompt і зберігає відповідь у window-змінну.", "prompt", dialogs,
            "Назва змінної", "userName", "Повідомлення", "Як тебе звати?", "Початкове значення", "");
        T("Повідомлення й діалоги", "Змінити заголовок сторінки", "Змінює document.title у вкладці браузера.", "set-page-title", dialogs,
            "Новий заголовок", "Мій сайт WebBlox");
        T("Повідомлення й діалоги", "Перезавантажити сторінку", "Перезавантажує поточну сторінку браузера.", "reload-page", dialogs);
        T("Повідомлення й діалоги", "Перейти за URL", "Навігує поточну вкладку браузера на URL.", "navigate-url", dialogs,
            "URL", "https://example.com");
        T("Повідомлення й діалоги", "Відкрити посилання", "Відкриває URL у новій вкладці браузера.", "open-url", dialogs,
            "URL", "https://example.com");

        // Додаткові події для форм, миші та прокручування.
        T("Події", "Коли поле отримало фокус", "Запускає код, коли користувач переходить до поля.", "event-listener", events,
            "ID / CSS-селектор", "myInput", "Подія", "focus", bodyLabel: "Код при фокусі", body: "event.target.classList.add('focused');");
        T("Події", "Коли поле втратило фокус", "Запускає код, коли користувач залишає поле.", "event-listener", events,
            "ID / CSS-селектор", "myInput", "Подія", "blur", bodyLabel: "Код після фокусу", body: "console.log('Поле залишено');");
        T("Події", "Коли натиснуто праву кнопку миші", "Обробляє контекстний клік по елементу.", "event-listener", events,
            "ID / CSS-селектор", "myCard", "Подія", "contextmenu", bodyLabel: "Код контекстного меню", body: "event.preventDefault();\nconsole.log('Власне контекстне меню');");
        T("Події", "Коли користувач прокручує сторінку", "Запускає дію при прокручуванні вікна браузера.", "event-listener", events,
            "Ціль", "window", "Подія", "scroll", bodyLabel: "Код прокручування", body: "console.log('Y:', window.scrollY);");
        T("Події", "Делегована подія для CSS-селектора", "Обробляє подію на елементах, що відповідають селектору, навіть якщо вони створені пізніше.", "event-delegation", events,
            "CSS-селектор", ".actionButton", "Подія", "click", bodyLabel: "Код події", body: "console.log('Натиснутий елемент:', event.target);");

        // Додаткові операції з DOM та формами.
        T("HTML та DOM", "Додати текст у кінець", "Додає текст після поточного текстового вмісту елемента.", "append-text", dom,
            "ID / CSS-селектор", "message", "Текст", " Ще текст");
        T("HTML та DOM", "Додати текст на початок", "Додає текст перед поточним текстовим вмістом елемента.", "prepend-text", dom,
            "ID / CSS-селектор", "message", "Текст", "Початок: ");
        T("HTML та DOM", "Очистити вміст елемента", "Видаляє дочірні вузли всередині вибраного елемента.", "clear-element", dom,
            "ID / CSS-селектор", "content");
        T("HTML та DOM", "Установити прапорець", "Вмикає або вимикає checkbox чи checked-стан radio.", "set-checked", dom,
            "ID / CSS-селектор", "agreeCheckbox", "Checked (true/false)", "true");
        T("HTML та DOM", "Заблокувати елемент", "Вимикає кнопку або поле введення.", "disable-element", dom,
            "ID / CSS-селектор", "submitButton");
        T("HTML та DOM", "Розблокувати елемент", "Знову дозволяє взаємодію з кнопкою або полем.", "enable-element", dom,
            "ID / CSS-селектор", "submitButton");
        T("HTML та DOM", "Установити вибраний індекс", "Вибирає опцію у select за індексом, починаючи з нуля.", "set-selected-index", dom,
            "ID / CSS-селектор", "mySelect", "Індекс", "0");
        T("HTML та DOM", "Створити кнопку", "Створює кнопку в контейнері; вкажи батьківський елемент, напис і ID.", "create-button", dom,
            "Батьківський ID / селектор", "container", "Напис кнопки", "Натисни мене", "ID кнопки", "newButton");
        T("HTML та DOM", "Створити посилання", "Створює посилання в контейнері.", "create-link", dom,
            "Батьківський ID / селектор", "container", "URL (href)", "https://example.com", "Текст посилання", "Відкрити сайт");
        T("HTML та DOM", "Створити зображення", "Створює img з URL і альтернативним описом.", "create-image", dom,
            "Батьківський ID / селектор", "gallery", "URL зображення", "images/photo.jpg", "Alt текст", "Фото");
        T("HTML та DOM", "Замінити текстовий фрагмент", "Замінює буквальну частину тексту всередині елемента.", "replace-text", dom,
            "ID / CSS-селектор", "message", "Знайти текст", "старий", "Замінити на", "новий");
        T("HTML та DOM", "Скопіювати текст елемента", "Копіює текст елемента до буфера обміну за дією користувача.", "copy-element-text", dom,
            "ID / CSS-селектор", "copyText");
        T("HTML та DOM", "Задати текст кнопки за змінною", "Виводить значення window-змінної в елемент.", "set-text-from-variable", dom,
            "ID / CSS-селектор", "scoreValue", "Назва змінної", "score");
        T("HTML та DOM", "Додати опцію до списку", "Створює option і додає його в select.", "add-select-option", dom,
            "ID / CSS-селектор select", "mySelect", "Текст опції", "Нова опція", "Значення option", "new-value");
        T("HTML та DOM", "Установити мінімальне значення поля", "Задає атрибут min для числового поля.", "set-min-max", dom,
            "ID / CSS-селектор", "ageInput", "Атрибут (min/max/step)", "min", "Значення", "0");

        // Розширені стилі та анімації.
        T("CSS та анімації", "Задати прозорість", "Установлює прозорість від 0 до 1.", "set-opacity", css,
            "ID / CSS-селектор", "myCard", "Прозорість (0–1)", "0.75");
        T("CSS та анімації", "Установити фонове зображення", "Задає background-image з безпечним екрануванням URL.", "set-background-image", css,
            "ID / CSS-селектор", "hero", "URL зображення", "images/hero.jpg");
        T("CSS та анімації", "Установити transform", "Задає CSS transform, наприклад translateX(10px) або scale(1.1).", "set-transform", css,
            "ID / CSS-селектор", "myCard", "Transform", "scale(1.05)");
        T("CSS та анімації", "Установити transition", "Налаштовує плавний перехід CSS-властивостей.", "set-transition", css,
            "ID / CSS-селектор", "myCard", "Transition", "all 0.25s ease");
        T("CSS та анімації", "Задати тінь елемента", "Установлює box-shadow.", "set-style", css,
            "ID / CSS-селектор", "myCard", "CSS-властивість", "box-shadow", "Значення", "0 8px 24px rgba(0,0,0,.15)");
        T("CSS та анімації", "Перемкнути клас для анімації", "Додає або прибирає клас для CSS-анімації.", "toggle-class", css,
            "ID / CSS-селектор", "myCard", "CSS-клас", "is-animated");

        // Додаткові значення, рядки, числа та масиви.
        T("Змінні й математика", "Помножити змінну", "Множить числове значення window-змінної.", "multiply-variable", data,
            "Назва змінної", "score", "Множник", "2");
        T("Змінні й математика", "Поділити змінну", "Ділить змінну на число; ділення на нуль ігнорується.", "divide-variable", data,
            "Назва змінної", "score", "Дільник", "2");
        T("Змінні й математика", "Додати текст до змінної", "Перетворює значення на рядок і дописує текст у кінець.", "append-variable-string", data,
            "Назва змінної", "userName", "Текст", " WebBlox");
        T("Змінні й математика", "Довжина рядка", "Записує довжину рядка у змінну.", "string-length", data,
            "Назва змінної результату", "textLength", "Рядок або вираз", "'WebBlox'");
        T("Змінні й математика", "Перевірити, чи рядок містить текст", "Записує true або false за результатом includes().", "string-includes", data,
            "Назва змінної результату", "containsWord", "Рядок (JS-вираз)", "'WebBlox editor'", "Шукати (JS-вираз)", "'editor'");
        T("Змінні й математика", "Округлити число", "Округлює число до вказаної кількості знаків після коми.", "round-number", data,
            "Назва змінної результату", "rounded", "Число (JS-вираз)", "3.14159", "Кількість знаків", "2");
        T("Змінні й математика", "Обмежити число діапазоном", "Обмежує значення між мінімумом і максимумом.", "clamp-number", data,
            "Назва змінної результату", "bounded", "Число (JS-вираз)", "150", "Межі min,max", "0,100");
        T("Змінні й математика", "Створити масив", "Створює масив із JavaScript-виразу, наприклад ['a', 'b'].", "array-create", data,
            "Назва масиву", "items", "Початковий масив (JS-вираз)", "['a', 'b', 'c']");
        T("Змінні й математика", "Додати елемент до масиву", "Викликає push() для масиву у window-змінній.", "array-push", data,
            "Назва масиву", "items", "Елемент (JS-вираз)", "'new item'");
        T("Змінні й математика", "Видалити останній елемент масиву", "Викликає pop() і за бажанням зберігає результат у змінну.", "array-pop", data,
            "Назва масиву", "items", "Змінна результату (необов'язково)", "lastItem");
        T("Змінні й математика", "Об'єднати масив у рядок", "Викликає join() і записує результат у змінну.", "array-join", data,
            "Назва масиву", "items", "Роздільник", ", ", "Змінна результату", "joinedItems");
        T("Змінні й математика", "Розібрати JSON", "Перетворює JSON-рядок або вираз на об'єкт і зберігає його у window-змінну.", "json-parse", data,
            "Назва змінної", "data", "JSON (JS-вираз або рядок)", "'{\"name\":\"WebBlox\"}'");
        T("Змінні й математика", "Отримати значення з об'єкта", "Читає властивість об'єкта через вираз, наприклад window.user.name.", "object-get-property", data,
            "Назва змінної результату", "userName", "Властивість (JS-вираз)", "window.user.name");
        T("Змінні й математика", "Установити логічне значення", "Задає змінній true або false.", "boolean-variable", data,
            "Назва змінної", "isVisible", "Значення (true/false)", "true");

        // Розширений потік керування.
        T("Умови та цикли", "Цикл із лічильником", "Виконує код для кожного значення від початку до межі не включно.", "for-loop", flow,
            "Змінна лічильника", "i", "Початок", "0", "Межа (не включно)", "5", bodyLabel: "Код кожної ітерації", body: "console.log(i);");
        T("Умови та цикли", "Для кожного елемента CSS-селектора", "Перебирає всі DOM-елементи за CSS-селектором.", "for-each-selector", flow,
            "CSS-селектор", ".card", "Змінна елемента", "element", bodyLabel: "Код для кожного елемента", body: "element.classList.add('seen');");
        T("Умови та цикли", "Для кожного елемента масиву", "Перебирає масив або інший ітерований JS-вираз.", "for-each-array", flow,
            "Масив (JS-вираз)", "window.items", "Змінна елемента", "item", bodyLabel: "Код для елемента", body: "console.log(item);");
        T("Умови та цикли", "Виконати код лише один раз", "Запускає блок тільки під час першого виклику.", "run-once", flow,
            "Назва прапорця", "webbloxAlreadyRan", bodyLabel: "Код першого запуску", body: "console.log('Виконано один раз');");

        // Додаткові функції та таймери.
        T("Функції й таймери", "Зупинити timeout", "Скасовує timeout, ID якого збережено у window-змінній.", "clear-timeout", funcs,
            "Назва змінної таймера", "webbloxTimeoutId");
        T("Функції й таймери", "Встановити timeout зі змінною ID", "Зберігає ID відкладеної дії, щоб її можна було скасувати.", "timeout-named", funcs,
            "Назва змінної ID", "webbloxTimeoutId", "Затримка (мс)", "1000", bodyLabel: "Код після затримки", body: "console.log('Timeout завершився');");

        // Розширення браузера, URL та дати.
        T("Форми й браузер", "Показати поточний час", "Виводить локальний час у форматі браузера.", "current-time-text", browser,
            "ID / CSS-селектор", "currentTime");
        T("Форми й браузер", "Отримати параметр URL", "Читає query-параметр із поточного URL та записує його у window-змінну.", "url-param", browser,
            "Назва параметра", "name", "Назва змінної результату", "queryValue");
        T("Форми й браузер", "Установити параметр URL", "Оновлює query-параметр у адресі сторінки без перезавантаження.", "set-url-param", browser,
            "Назва параметра", "mode", "Значення", "preview");
        T("Форми й браузер", "Перемкнути повноекранний режим", "Увімкнути або вийти з fullscreen за дією користувача.", "toggle-fullscreen", browser,
            "Ціль (ID / селектор, необов'язково)", "document.documentElement");
        T("Форми й браузер", "Змінити мову документа", "Змінює атрибут lang у кореневого HTML-елемента.", "set-document-language", browser,
            "Мова", "uk");
        T("Форми й браузер", "Показати ширину вікна", "Виводить поточну ширину вікна у вибраний елемент.", "window-width-text", browser,
            "ID / CSS-селектор", "windowWidth");
        T("Форми й браузер", "Валідувати числовий діапазон", "Перевіряє, чи числове поле потрапляє у заданий діапазон.", "validate-number-range", browser,
            "ID / CSS-селектор поля", "ageInput", "Межі min,max", "1,120", bodyLabel: "Повідомлення", body: "Введи число в дозволеному діапазоні.");
        T("Форми й браузер", "Перемкнути fullscreen для елемента", "Перемикає повноекранний режим для конкретного елемента.", "toggle-fullscreen", browser,
            "Ціль (ID / CSS-селектор)", "gameCanvas");

        // Додаткові сховища даних.
        T("Сховище й дані", "Зберегти значення в sessionStorage", "Зберігає значення до закриття вкладки браузера.", "session-storage-set", storage,
            "Ключ", "temporaryName", "Значення", "Гість");
        T("Сховище й дані", "Зчитати sessionStorage у змінну", "Читає збережене значення у window-змінну.", "session-storage-get", storage,
            "Ключ", "temporaryName", "Назва змінної", "temporaryNameValue");
        T("Сховище й дані", "Зберегти JS-об'єкт у localStorage", "Серіалізує JS-вираз у JSON і зберігає за ключем.", "storage-set-json", storage,
            "Ключ", "userSettings", "Об'єкт (JS-вираз)", "({theme: 'dark', fontSize: 16})");
        T("Сховище й дані", "Зчитати JSON із localStorage", "Завантажує JSON і розбирає його у window-змінну.", "storage-get-json", storage,
            "Ключ", "userSettings", "Назва змінної", "userSettings");

        // Додаткові мережеві операції.
        T("Мережа й API", "Завантажити JSON у змінну", "Виконує fetch GET і зберігає результат JSON у window-змінну.", "fetch-json-variable", network,
            "URL API", "https://jsonplaceholder.typicode.com/todos/1", "Назва змінної результату", "apiData");
        T("Мережа й API", "Завантажити дані з API та показати поле", "Читає JSON і показує одну властивість у вибраному елементі.", "fetch-json-property", network,
            "URL API", "https://jsonplaceholder.typicode.com/todos/1", "Шлях до властивості", "title", bodyLabel: "ID / CSS-селектор результату", body: "apiOutput");

        // Локальні дані.
        T("Сховище й дані", "Зберегти значення", "Записує текст у localStorage браузера.", "storage-set", storage,
            "Ключ", "username", "Значення", "Гість");
        T("Сховище й дані", "Завантажити значення в елемент", "Читає localStorage і виводить значення у текст елемента.", "storage-get-element", storage,
            "Ключ", "username", "ID / CSS-селектор", "savedName");
        T("Сховище й дані", "Видалити збережене значення", "Видаляє один ключ localStorage.", "storage-remove", storage,
            "Ключ", "username");
        T("Сховище й дані", "Очистити сховище сайту", "Видаляє весь localStorage для цього сайту.", "storage-clear", storage);
        T("Сховище й дані", "Перетворити дані на JSON", "Серіалізує JS-вираз і виводить JSON у консоль.", "json-stringify", storage,
            "JS-вираз", "({name: 'WebBlox', score: 10})");

        // HTTP / API.
        T("Мережа й API", "Завантажити текст з URL", "Виконує fetch GET і вставляє текст у елемент. Для зовнішнього API можуть діяти CORS-обмеження.", "fetch-text", network,
            "URL", "https://example.com/data.txt", "ID / CSS-селектор результату", "apiOutput");
        T("Мережа й API", "Завантажити JSON з URL", "Виконує fetch, читає JSON та показує його форматованим текстом.", "fetch-json", network,
            "URL API", "https://jsonplaceholder.typicode.com/todos/1", "ID / CSS-селектор результату", "apiOutput");
        T("Мережа й API", "Надіслати POST JSON", "Надсилає JSON через fetch POST; сервер має дозволяти CORS.", "fetch-post-json", network,
            "URL API", "https://example.com/api", "JSON-тіло (JS-вираз)", "({message: 'Привіт'})", "ID / CSS-селектор результату", "apiOutput");

        // Форми, браузер та утиліти.
        T("Форми й браузер", "Перевірити обов'язкове поле", "Показує повідомлення й фокусує поле, якщо воно порожнє.", "validate-required", browser,
            "ID / CSS-селектор", "emailInput", "Повідомлення", "Заповни це поле");
        T("Форми й браузер", "Зчитати значення поля у змінну", "Читає value і зберігає його в window-змінну.", "read-input-variable", browser,
            "ID / CSS-селектор поля", "myInput", "Назва змінної", "inputValue");
        T("Форми й браузер", "Скопіювати текст", "Копіює текст до буфера обміну; браузер може вимагати HTTPS або дію користувача.", "clipboard-copy", browser,
            "Текст", "Текст для копіювання");
        T("Форми й браузер", "Завантажити текстовий файл", "Створює й завантажує текстовий файл у браузері.", "download-text", browser,
            "Назва файлу", "webblox.txt", "Текст", "Створено у WebBlox");
        T("Форми й браузер", "Друк сторінки", "Відкриває системний діалог друку браузера.", "print-page", browser);
        T("Форми й браузер", "Перевірити email", "Перевіряє простий формат адреси й показує повідомлення, якщо він схожий на неправильний.", "validate-email", browser,
            "ID / CSS-селектор", "emailInput", "Повідомлення", "Введи коректний email");
        T("Форми й браузер", "Прокрутити вгору й показати", "Плавна прокрутка до верху сторінки.", "scroll-top", browser);

        _visibleJsBlockTemplates.Clear();
        JsBlockPalette.ItemsSource = _visibleJsBlockTemplates;
        JsBlockWorkspace.ItemsSource = _jsBlocks;
        JsBlockCategoryBox.ItemsSource = new[] { "Усі категорії" }.Concat(_allJsBlockTemplates.Select(x => x.Category).Distinct()).ToList();
        JsBlockCategoryBox.SelectedIndex = 0;
        ApplyJsBlockFilter();
        RefreshJsBlockEditor(null);
    }

    private void ApplyJsBlockFilter()
    {
        if (JsBlockCategoryBox is null || JsBlockPalette is null || JsBlockSearchBox is null) return;
        string category = JsBlockCategoryBox.SelectedItem as string ?? "Усі категорії";
        string search = JsBlockSearchBox.Text?.Trim() ?? "";
        var matches = _allJsBlockTemplates.Where(t =>
            (category == "Усі категорії" || t.Category.Equals(category, StringComparison.Ordinal)) &&
            (search.Length == 0 || t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             t.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             t.Kind.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
        _visibleJsBlockTemplates.Clear();
        foreach (JsBlockTemplate template in matches) _visibleJsBlockTemplates.Add(template);
    }

    private void JsBlockSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyJsBlockFilter();
    private void JsBlockCategory_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyJsBlockFilter();

    private void JsBlockPalette_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _jsBlockDragStart = e.GetPosition(JsBlockPalette);
    }

    private void JsBlockPalette_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || JsBlockPalette.SelectedItem is not JsBlockTemplate template) return;
        Point current = e.GetPosition(JsBlockPalette);
        if (Math.Abs(current.X - _jsBlockDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _jsBlockDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var data = new DataObject(typeof(JsBlockTemplate), template);
        DragDrop.DoDragDrop(JsBlockPalette, data, DragDropEffects.Copy);
    }

    private void JsBlockPalette_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (JsBlockPalette.SelectedItem is JsBlockTemplate template) AddJsBlockTemplate(template);
    }

    private void JsBlockWorkspace_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(JsBlockTemplate)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void JsBlockWorkspace_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(JsBlockTemplate)) is JsBlockTemplate template) AddJsBlockTemplate(template);
        e.Handled = true;
    }

    private JsVisualBlock? AddJsBlockTemplate(JsBlockTemplate template)
    {
        if (_jsBlocks.Count >= 500)
        {
            MessageBox.Show("У робочій області дозволено до 500 блоків, щоб редактор залишався швидким.", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        var block = new JsVisualBlock(template);
        _jsBlocks.Add(block);
        JsBlockWorkspace.SelectedItem = block;
        JsBlockWorkspace.ScrollIntoView(block);
        StatusText.Text = $"Додано блок: {block.Title}";
        return block;
    }

    private void AddJsBlock_Click(object sender, RoutedEventArgs e)
    {
        if (JsBlockPalette.SelectedItem is JsBlockTemplate template) AddJsBlockTemplate(template);
        else MessageBox.Show("Спочатку вибери блок у бібліотеці.", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddJsSample_Click(object sender, RoutedEventArgs e)
    {
        JsBlockTemplate? onClick = _allJsBlockTemplates.FirstOrDefault(t => t.Kind == "event-listener" && t.DefaultParam2 == "click");
        JsBlockTemplate? onLoad = _allJsBlockTemplates.FirstOrDefault(t => t.Kind == "page-load");
        if (onLoad is not null)
        {
            JsVisualBlock? load = AddJsBlockTemplate(onLoad);
            if (load is not null) load.Body = "console.log('Сторінка запущена');";
        }
        if (onClick is not null)
        {
            JsVisualBlock? click = AddJsBlockTemplate(onClick);
            if (click is not null)
            {
                click.Param1 = "myButton";
                click.Param2 = "click";
                click.Body = "const output = document.getElementById('message');\nif (output) output.textContent = 'Кнопку натиснуто!';\nelse alert('Кнопка працює!');";
            }
        }
        StatusText.Text = "Додано приклад. Перевір HTML ID myButton та необов’язковий message.";
    }

    private void JsBlockWorkspace_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshJsBlockEditor(JsBlockWorkspace.SelectedItem as JsVisualBlock);
    }

    private void RefreshJsBlockEditor(JsVisualBlock? block)
    {
        if (JsBlockParam1Box is null) return;
        _loadingJsBlockFields = true;
        SetJsBlockField(JsBlockParam1Label, JsBlockParam1Box, block?.Param1Label, block?.Param1);
        SetJsBlockField(JsBlockParam2Label, JsBlockParam2Box, block?.Param2Label, block?.Param2);
        SetJsBlockField(JsBlockParam3Label, JsBlockParam3Box, block?.Param3Label, block?.Param3);
        SetJsBlockField(JsBlockBodyLabel, JsBlockBodyBox, block?.BodyLabel, block?.Body);
        _loadingJsBlockFields = false;
    }

    private static void SetJsBlockField(TextBlock label, TextBox box, string? labelText, string? value)
    {
        bool visible = !string.IsNullOrWhiteSpace(labelText);
        label.Text = visible ? labelText! : "";
        label.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        box.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        box.Text = visible ? (value ?? "") : "";
    }

    private void JsBlockParameter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingJsBlockFields || JsBlockWorkspace.SelectedItem is not JsVisualBlock block) return;
        block.Param1 = JsBlockParam1Box.Text;
        block.Param2 = JsBlockParam2Box.Text;
        block.Param3 = JsBlockParam3Box.Text;
        block.Body = JsBlockBodyBox.Text;
    }

    private void MoveJsBlockUp_Click(object sender, RoutedEventArgs e) => MoveSelectedJsBlock(-1);
    private void MoveJsBlockDown_Click(object sender, RoutedEventArgs e) => MoveSelectedJsBlock(1);

    private void MoveSelectedJsBlock(int offset)
    {
        if (JsBlockWorkspace.SelectedItem is not JsVisualBlock block) return;
        int index = _jsBlocks.IndexOf(block);
        int newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= _jsBlocks.Count) return;
        _jsBlocks.Move(index, newIndex);
        JsBlockWorkspace.SelectedItem = block;
        JsBlockWorkspace.ScrollIntoView(block);
    }

    private void DuplicateJsBlock_Click(object sender, RoutedEventArgs e)
    {
        if (JsBlockWorkspace.SelectedItem is not JsVisualBlock block) return;
        if (_jsBlocks.Count >= 500) return;
        int index = _jsBlocks.IndexOf(block) + 1;
        var copy = block.Clone();
        _jsBlocks.Insert(index, copy);
        JsBlockWorkspace.SelectedItem = copy;
        JsBlockWorkspace.ScrollIntoView(copy);
        StatusText.Text = "Блок продубльовано";
    }

    private void RemoveJsBlock_Click(object sender, RoutedEventArgs e)
    {
        if (JsBlockWorkspace.SelectedItem is not JsVisualBlock block) return;
        int index = _jsBlocks.IndexOf(block);
        _jsBlocks.Remove(block);
        if (_jsBlocks.Count > 0) JsBlockWorkspace.SelectedIndex = Math.Clamp(index, 0, _jsBlocks.Count - 1);
        else RefreshJsBlockEditor(null);
        StatusText.Text = "Блок видалено";
    }

    private void ClearJsBlocks_Click(object sender, RoutedEventArgs e)
    {
        if (_jsBlocks.Count == 0) return;
        if (MessageBox.Show("Очистити всі візуальні JavaScript-блоки?", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _jsBlocks.Clear();
        RefreshJsBlockEditor(null);
        StatusText.Text = "Візуальні блоки очищено";
    }

    private void GenerateJsFromBlocks_Click(object sender, RoutedEventArgs e)
    {
        string generated = BuildJavaScriptFromBlocks();
        if (string.IsNullOrWhiteSpace(generated))
        {
            MessageBox.Show("Додай хоча б один блок у робочу область.", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!string.IsNullOrWhiteSpace(JavaScriptBox.Text) && !JavaScriptBox.Text.Equals(generated, StringComparison.Ordinal) &&
            MessageBox.Show("Замінити наявний JavaScript кодом зі всіх візуальних блоків? Поточний код буде замінено.", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        JavaScriptBox.Text = generated;
        JavaScriptEditorTabs.SelectedIndex = 1;
        StatusText.Text = $"JavaScript згенеровано з {_jsBlocks.Count} блоків";
    }

    private void AppendJsFromBlocks_Click(object sender, RoutedEventArgs e)
    {
        string generated = BuildJavaScriptFromBlocks();
        if (string.IsNullOrWhiteSpace(generated))
        {
            MessageBox.Show("Додай хоча б один блок у робочу область.", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        string current = JavaScriptBox.Text.TrimEnd();
        JavaScriptBox.Text = current.Length == 0 ? generated : current + Environment.NewLine + Environment.NewLine + generated;
        JavaScriptEditorTabs.SelectedIndex = 1;
        StatusText.Text = "Згенерований код додано в кінець JavaScript";
    }

    private string BuildJavaScriptFromBlocks()
    {
        var sections = new List<string>();
        for (int i = 0; i < _jsBlocks.Count; i++)
        {
            JsVisualBlock block = _jsBlocks[i];
            string code = GenerateJsBlockCode(block);
            if (string.IsNullOrWhiteSpace(code)) continue;
            sections.Add($"// WebBlox block {i + 1}: {block.Title}\n{code.TrimEnd()}");
        }
        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private string GenerateJsBlockCode(JsVisualBlock block)
    {
        string p1 = block.Param1?.Trim() ?? "";
        string p2 = block.Param2?.Trim() ?? "";
        string p3 = block.Param3?.Trim() ?? "";
        string body = block.Body ?? "";
        string target = BuildJsTargetExpression(p1);
        string bodyIndented = IndentJsBody(body, 2);
        string q1 = JsStringLiteral(p1);
        string q2 = JsStringLiteral(p2);
        string q3 = JsStringLiteral(p3);

        switch (block.Kind)
        {
            case "page-load":
                return "(() => {\n  const __wbReady = (callback) => { if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', callback, { once: true }); else callback(); };\n  __wbReady(() => {\n" + IndentJsBody(body, 2) + "\n  });\n})();";
            case "event-listener":
                return "(() => {\n  const __wbTarget = " + target + ";\n  if (!__wbTarget) { console.warn('WebBlox: елемент не знайдено: ' + " + q1 + "); return; }\n  __wbTarget.addEventListener(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "click" : p2) + ", (event) => {\n" + bodyIndented + "\n  });\n})();";
            case "keydown":
                return "document.addEventListener('keydown', (event) => {\n  if (event.key === " + JsStringLiteral(string.IsNullOrWhiteSpace(p1) ? "Enter" : p1) + ") {\n" + IndentJsBody(body, 2) + "\n  }\n});";
            case "event-delegation":
                const string delegatedVariable = "matchedElement";
                return "document.addEventListener(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "click" : p2) + ", (event) => { const " + delegatedVariable + " = event.target instanceof Element ? event.target.closest(" + q1 + ") : null; if (" + delegatedVariable + ") {\n" + IndentJsBody(body, 2) + "\n} });";
            case "append-text": return "(() => { const el = " + target + "; if (el) el.append(document.createTextNode(" + q2 + ")); })();";
            case "prepend-text": return "(() => { const el = " + target + "; if (el) el.prepend(document.createTextNode(" + q2 + ")); })();";
            case "clear-element": return "(() => { const el = " + target + "; if (el) el.replaceChildren(); })();";
            case "set-checked": return "(() => { const el = " + target + "; if (el && 'checked' in el) el.checked = /^true$/i.test(" + q2 + "); })();";
            case "disable-element": return "(() => { const el = " + target + "; if (el && 'disabled' in el) el.disabled = true; })();";
            case "enable-element": return "(() => { const el = " + target + "; if (el && 'disabled' in el) el.disabled = false; })();";
            case "set-selected-index": return "(() => { const el = " + target + "; if (el && 'selectedIndex' in el) el.selectedIndex = Math.trunc(Number(" + q2 + ") || 0); })();";
            case "create-button": return "(() => { const parent = " + target + "; if (!parent) return; const button = document.createElement('button'); button.type = 'button'; button.textContent = " + q2 + "; if (" + q3 + ") button.id = " + q3 + "; parent.appendChild(button); })();";
            case "create-link": return "(() => { const parent = " + target + "; if (!parent) return; const link = document.createElement('a'); link.href = " + q2 + "; link.textContent = " + q3 + "; parent.appendChild(link); })();";
            case "create-image": return "(() => { const parent = " + target + "; if (!parent) return; const img = document.createElement('img'); img.src = " + q2 + "; img.alt = " + q3 + "; img.loading = 'lazy'; parent.appendChild(img); })();";
            case "replace-text": return "(() => { const el = " + target + "; if (el) el.textContent = String(el.textContent || '').split(" + q2 + ").join(" + q3 + "); })();";
            case "copy-element-text": return "(() => { const el = " + target + "; if (el && navigator.clipboard) navigator.clipboard.writeText(el.textContent || '').catch(error => console.error('Копіювання не вдалося:', error)); })();";
            case "set-text-from-variable": return "(() => { const el = " + target + "; if (el) el.textContent = String(window[" + q2 + "] ?? ''); })();";
            case "add-select-option": return "(() => { const el = " + target + "; if (!el || el.tagName !== 'SELECT') return; const option = document.createElement('option'); option.textContent = " + q2 + "; option.value = " + q3 + "; el.add(option); })();";
            case "set-min-max": return "(() => { const el = " + target + "; const attr = " + q2 + "; if (el && ['min','max','step'].includes(attr)) el.setAttribute(attr, " + q3 + "); })();";
            case "set-text": return "(() => { const el = " + target + "; if (el) el.textContent = " + JsStringLiteral(p2) + "; else console.warn('WebBlox: елемент не знайдено'); })();";
            case "set-html": return "(() => { const el = " + target + "; if (el) el.innerHTML = " + JsStringLiteral(p2) + "; else console.warn('WebBlox: елемент не знайдено'); })();";
            case "show-element": return "(() => { const el = " + target + "; if (el) { el.hidden = false; el.style.removeProperty('display'); } })();";
            case "hide-element": return "(() => { const el = " + target + "; if (el) { el.hidden = true; el.style.display = 'none'; } })();";
            case "toggle-element": return "(() => { const el = " + target + "; if (el) { const hidden = el.hidden || getComputedStyle(el).display === 'none'; el.hidden = !hidden; el.style.display = hidden ? '' : 'none'; } })();";
            case "set-value": return "(() => { const el = " + target + "; if (el) el.value = " + JsStringLiteral(p2) + "; })();";
            case "set-placeholder": return "(() => { const el = " + target + "; if (el) el.setAttribute('placeholder', " + JsStringLiteral(p2) + "); })();";
            case "set-attribute": return "(() => { const el = " + target + "; if (el) el.setAttribute(" + q2 + ", " + q3 + "); })();";
            case "remove-attribute": return "(() => { const el = " + target + "; if (el) el.removeAttribute(" + q2 + "); })();";
            case "add-class": return "(() => { const el = " + target + "; if (el && " + q2 + ") el.classList.add(" + q2 + "); })();";
            case "remove-class": return "(() => { const el = " + target + "; if (el && " + q2 + ") el.classList.remove(" + q2 + "); })();";
            case "toggle-class": return "(() => { const el = " + target + "; if (el && " + q2 + ") el.classList.toggle(" + q2 + "); })();";
            case "scroll-to-element": return "(() => { const el = " + target + "; if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' }); })();";
            case "focus-element": return "(() => { const el = " + target + "; if (el && typeof el.focus === 'function') el.focus(); })();";
            case "remove-element": return "(() => { const el = " + target + "; if (el) el.remove(); })();";
            case "create-element":
                return "(() => { const parent = " + BuildJsTargetExpression(p1) + "; if (!parent) return; const child = document.createElement(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "div" : p2) + "); child.textContent = " + JsStringLiteral(p3) + "; parent.appendChild(child); })();";
            case "set-src": return "(() => { const el = " + target + "; if (el) el.setAttribute('src', " + q2 + "); })();";
            case "set-href": return "(() => { const el = " + target + "; if (el) el.setAttribute('href', " + q2 + "); })();";
            case "current-date-text": return "(() => { const el = " + target + "; if (el) el.textContent = new Date().toLocaleDateString(); })();";
            case "current-url-text": return "(() => { const el = " + target + "; if (el) el.textContent = window.location.href; })();";
            case "count-elements": return "window[" + q2 + "] = document.querySelectorAll(" + q1 + ").length;";
            case "set-style": return "(() => { const el = " + target + "; if (el) el.style.setProperty(" + q2 + ", " + q3 + "); })();";
            case "set-css-variable": return "(() => { const el = " + target + "; if (el) el.style.setProperty(" + q2 + ", " + q3 + "); })();";
            case "animate-in": return "(() => { const el = " + target + "; if (el) el.animate([{ opacity: 0, transform: 'translateY(12px)' }, { opacity: 1, transform: 'translateY(0)' }], { duration: Math.max(1, Number(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "450" : p2) + ") || 450), easing: 'ease-out', fill: 'both' }); })();";
            case "animate-out": return "(() => { const el = " + target + "; if (el) { const anim = el.animate([{ opacity: 1 }, { opacity: 0 }], { duration: Math.max(1, Number(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "350" : p2) + ") || 350), easing: 'ease-in', fill: 'forwards' }); anim.onfinish = () => { el.hidden = true; }; } })();";
            case "animate-pulse": return "(() => { const el = " + target + "; if (el) el.animate([{ transform: 'scale(1)' }, { transform: 'scale(1.05)' }, { transform: 'scale(1)' }], { duration: Math.max(1, Number(" + JsStringLiteral(string.IsNullOrWhiteSpace(p2) ? "600" : p2) + ") || 600), iterations: Math.max(1, Number(" + JsStringLiteral(string.IsNullOrWhiteSpace(p3) ? "1" : p3) + ") || 1) }); })();";
            case "set-opacity": return "(() => { const el = " + target + "; if (el) el.style.opacity = String(Math.max(0, Math.min(1, Number(" + q2 + ") || 0))); })();";
            case "set-background-image": return "(() => { const el = " + target + "; if (el) el.style.backgroundImage = 'url(' + JSON.stringify(" + q2 + ") + ')'; })();";
            case "set-transform": return "(() => { const el = " + target + "; if (el) el.style.transform = " + q2 + "; })();";
            case "set-transition": return "(() => { const el = " + target + "; if (el) el.style.transition = " + q2 + "; })();";
            case "scroll-top": return "window.scrollTo({ top: 0, behavior: 'smooth' });";
            case "toggle-page-dark": return "document.documentElement.classList.toggle(" + q1 + ");";
            case "set-variable": return "window[" + q1 + "] = (" + (string.IsNullOrWhiteSpace(p2) ? "undefined" : p2) + ");";
            case "increment-variable": return "window[" + q1 + "] = (Number(window[" + q1 + "]) || 0) + (Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + ") || 0);";
            case "decrement-variable": return "window[" + q1 + "] = (Number(window[" + q1 + "]) || 0) - (Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + ") || 0);";
            case "random-number": return "window[" + q1 + "] = Math.floor(Math.random() * (Number(" + (string.IsNullOrWhiteSpace(p3) ? "10" : p3) + ") - Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + ") + 1)) + Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + ");";
            case "math-expression": return "window[" + q1 + "] = (" + (string.IsNullOrWhiteSpace(p2) ? "0" : p2) + ");";
            case "console-log": return "console.log(" + q1 + ");";
            case "console-expression": return "console.log(" + (string.IsNullOrWhiteSpace(p1) ? "undefined" : p1) + ");";
            case "console-warn": return "console.warn(" + q1 + ");";
            case "timestamp": return "window[" + q1 + "] = Date.now();";
            case "multiply-variable": return "window[" + q1 + "] = (Number(window[" + q1 + "]) || 0) * (Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + ") || 0);";
            case "divide-variable": return "(() => { const divisor = Number(" + (string.IsNullOrWhiteSpace(p2) ? "1" : p2) + "); if (divisor !== 0) window[" + q1 + "] = (Number(window[" + q1 + "]) || 0) / divisor; else console.warn('WebBlox: ділення на нуль пропущено'); })();";
            case "append-variable-string": return "window[" + q1 + "] = String(window[" + q1 + "] ?? '') + " + q2 + ";";
            case "string-length": return "window[" + q1 + "] = String(" + (string.IsNullOrWhiteSpace(p2) ? "''" : p2) + " ?? '').length;";
            case "string-includes": return "window[" + q1 + "] = String(" + (string.IsNullOrWhiteSpace(p2) ? "''" : p2) + " ?? '').includes(String(" + (string.IsNullOrWhiteSpace(p3) ? "''" : p3) + " ?? ''));";
            case "round-number": return "(() => { const digits = Math.max(0, Math.min(12, Math.trunc(Number(" + (string.IsNullOrWhiteSpace(p3) ? "0" : p3) + ") || 0))); const factor = 10 ** digits; window[" + q1 + "] = Math.round((Number(" + (string.IsNullOrWhiteSpace(p2) ? "0" : p2) + ") || 0) * factor) / factor; })();";
            case "clamp-number": return "(() => { const limits = String(" + q3 + ").split(',').map(Number); const min = Number.isFinite(limits[0]) ? limits[0] : 0; const max = Number.isFinite(limits[1]) ? limits[1] : 100; const value = Number(" + (string.IsNullOrWhiteSpace(p2) ? "0" : p2) + ") || 0; window[" + q1 + "] = Math.min(Math.max(value, Math.min(min, max)), Math.max(min, max)); })();";
            case "array-create": return "window[" + q1 + "] = Array.from(" + (string.IsNullOrWhiteSpace(p2) ? "[]" : p2) + ");";
            case "array-push": return "if (Array.isArray(window[" + q1 + "])) window[" + q1 + "].push(" + (string.IsNullOrWhiteSpace(p2) ? "undefined" : p2) + "); else console.warn('WebBlox: змінна не є масивом: ' + " + q1 + ");";
            case "array-pop": return "(() => { if (!Array.isArray(window[" + q1 + "])) return; const popped = window[" + q1 + "].pop(); if (" + q2 + ") window[" + q2 + "] = popped; })();";
            case "array-join": return "window[" + q3 + "] = Array.isArray(window[" + q1 + "]) ? window[" + q1 + "].join(" + q2 + ") : '';";
            case "json-parse": return "try { window[" + q1 + "] = JSON.parse(" + (string.IsNullOrWhiteSpace(p2) ? "'null'" : p2) + "); } catch (error) { console.error('WebBlox: помилка JSON', error); }";
            case "object-get-property": return "window[" + q1 + "] = (" + (string.IsNullOrWhiteSpace(p2) ? "undefined" : p2) + ");";
            case "boolean-variable": return "window[" + q1 + "] = /^true$/i.test(" + q2 + ");";
            case "if": return "if (" + (string.IsNullOrWhiteSpace(p1) ? "false" : p1) + ") {\n" + bodyIndented + "\n}";
            case "if-else": return "if (" + (string.IsNullOrWhiteSpace(p1) ? "false" : p1) + ") {\n" + bodyIndented + "\n} else {\n" + IndentJsBody(p2, 2) + "\n}";
            case "repeat": return "for (let __wbRepeatIndex = 0; __wbRepeatIndex < Math.max(0, Number(" + (string.IsNullOrWhiteSpace(p1) ? "0" : p1) + ") || 0); __wbRepeatIndex++) {\n" + bodyIndented + "\n}";
            case "while": return "while (" + (string.IsNullOrWhiteSpace(p1) ? "false" : p1) + ") {\n" + bodyIndented + "\n}";
            case "for-loop":
                string loopIndex = SanitizeJsIdentifier(p1, "i");
                return "for (let " + loopIndex + " = Number(" + (string.IsNullOrWhiteSpace(p2) ? "0" : p2) + ") || 0; " + loopIndex + " < (Number(" + (string.IsNullOrWhiteSpace(p3) ? "0" : p3) + ") || 0); " + loopIndex + "++) {\n" + bodyIndented + "\n}";
            case "for-each-selector":
                string eachElement = SanitizeJsIdentifier(p2, "element");
                return "document.querySelectorAll(" + q1 + ").forEach((" + eachElement + ") => {\n" + bodyIndented + "\n});";
            case "for-each-array":
                string eachItem = SanitizeJsIdentifier(p2, "item");
                return "Array.from(" + (string.IsNullOrWhiteSpace(p1) ? "[]" : p1) + ").forEach((" + eachItem + ") => {\n" + bodyIndented + "\n});";
            case "run-once":
                return "if (!window[" + q1 + "]) { window[" + q1 + "] = true;\n" + bodyIndented + "\n}";
            case "try-catch":
                string errorName = SanitizeJsIdentifier(p1, "error");
                return "try {\n" + bodyIndented + "\n} catch (" + errorName + ") {\n" + IndentJsBody(p2, 2) + "\n}";
            case "custom-code": return string.IsNullOrWhiteSpace(body) ? "// Порожній JavaScript-блок" : body;
            case "function":
                string fnName = string.IsNullOrWhiteSpace(p1) ? "webbloxFunction" : p1;
                string parameters = string.Join(", ", (p2 ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(x => SanitizeJsIdentifier(x, "arg")));
                return "window[" + JsStringLiteral(fnName) + "] = function(" + parameters + ") {\n" + bodyIndented + "\n};";
            case "call-function": return "if (typeof window[" + q1 + "] === 'function') window[" + q1 + "](" + p2 + "); else console.warn('WebBlox: функцію не знайдено: ' + " + q1 + ");";
            case "timeout": return "window.setTimeout(() => {\n" + bodyIndented + "\n}, Math.max(0, Number(" + (string.IsNullOrWhiteSpace(p1) ? "0" : p1) + ") || 0));";
            case "interval": return "window[" + q1 + "] = window.setInterval(() => {\n" + bodyIndented + "\n}, Math.max(1, Number(" + (string.IsNullOrWhiteSpace(p2) ? "1000" : p2) + ") || 1000));";
            case "clear-interval": return "if (window[" + q1 + "] !== undefined) { clearInterval(window[" + q1 + "]); delete window[" + q1 + "]; }";
            case "clear-timeout": return "if (window[" + q1 + "] !== undefined) { clearTimeout(window[" + q1 + "]); delete window[" + q1 + "]; }";
            case "timeout-named": return "window[" + q1 + "] = window.setTimeout(() => {\n" + bodyIndented + "\n}, Math.max(0, Number(" + (string.IsNullOrWhiteSpace(p2) ? "0" : p2) + ") || 0));";
            case "alert": return "window.alert(" + q1 + ");";
            case "confirm": return "window[" + q1 + "] = window.confirm(" + q2 + ");";
            case "prompt": return "window[" + q1 + "] = window.prompt(" + q2 + ", " + JsStringLiteral(p3) + ");";
            case "set-page-title": return "document.title = " + q1 + ";";
            case "reload-page": return "window.location.reload();";
            case "navigate-url": return "window.location.href = " + q1 + ";";
            case "open-url": return "window.open(" + q1 + ", '_blank', 'noopener,noreferrer');";
            case "storage-set": return "try { localStorage.setItem(" + q1 + ", " + q2 + "); } catch (error) { console.error('Не вдалося зберегти дані', error); }";
            case "storage-get-element": return "(() => { const value = localStorage.getItem(" + q1 + "); const el = " + BuildJsTargetExpression(p2) + "; if (el && value !== null) el.textContent = value; })();";
            case "storage-remove": return "localStorage.removeItem(" + q1 + ");";
            case "storage-clear": return "localStorage.clear();";
            case "json-stringify": return "console.log(JSON.stringify((" + (string.IsNullOrWhiteSpace(p1) ? "null" : p1) + "), null, 2));";
            case "fetch-text": return "fetch(" + q1 + ").then(response => { if (!response.ok) throw new Error('HTTP ' + response.status); return response.text(); }).then(text => { const el = " + BuildJsTargetExpression(p2) + "; if (el) el.textContent = text; }).catch(error => console.error('WebBlox fetch error:', error));";
            case "fetch-json": return "fetch(" + q1 + ").then(response => { if (!response.ok) throw new Error('HTTP ' + response.status); return response.json(); }).then(data => { const el = " + BuildJsTargetExpression(p2) + "; if (el) el.textContent = JSON.stringify(data, null, 2); }).catch(error => console.error('WebBlox fetch error:', error));";
            case "fetch-post-json": return "fetch(" + q1 + ", { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(" + (string.IsNullOrWhiteSpace(p2) ? "{}" : p2) + ") }).then(response => { if (!response.ok) throw new Error('HTTP ' + response.status); return response.text(); }).then(text => { const el = " + BuildJsTargetExpression(p3) + "; if (el) el.textContent = text; }).catch(error => console.error('WebBlox POST error:', error));";
            case "validate-required": return "(() => { const el = " + target + "; if (!el) { console.warn('WebBlox: поле не знайдено'); return; } if (!String(el.value ?? '').trim()) { alert(" + q2 + "); if (typeof el.focus === 'function') el.focus(); } })();";
            case "read-input-variable": return "(() => { const el = " + target + "; if (el) window[" + q2 + "] = el.value; })();";
            case "clipboard-copy": return "navigator.clipboard.writeText(" + q1 + ").then(() => console.log('Текст скопійовано')).catch(error => console.error('Не вдалося скопіювати текст:', error));";
            case "download-text": return "(() => { const blob = new Blob([" + q2 + "], { type: 'text/plain;charset=utf-8' }); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = " + q1 + "; document.body.appendChild(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000); })();";
            case "print-page": return "window.print();";
            case "validate-email": return "(() => { const el = " + target + "; if (!el) return; const valid = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/.test(String(el.value || '').trim()); if (!valid) { alert(" + q2 + "); if (typeof el.focus === 'function') el.focus(); } })();";
            case "current-time-text": return "(() => { const el = " + target + "; if (el) el.textContent = new Date().toLocaleTimeString(); })();";
            case "url-param": return "window[" + q2 + "] = new URLSearchParams(window.location.search).get(" + q1 + ");";
            case "set-url-param": return "(() => { const url = new URL(window.location.href); url.searchParams.set(" + q1 + ", " + q2 + "); window.history.replaceState({}, '', url); })();";
            case "toggle-fullscreen": return "(() => { const el = " + target + " || document.documentElement; if (document.fullscreenElement) document.exitFullscreen().catch(error => console.warn(error)); else if (el.requestFullscreen) el.requestFullscreen().catch(error => console.warn('Fullscreen недоступний:', error)); })();";
            case "set-document-language": return "document.documentElement.lang = " + q1 + ";";
            case "window-width-text": return "(() => { const el = " + target + "; if (el) el.textContent = String(window.innerWidth); })();";
            case "validate-number-range": return "(() => { const el = " + target + "; if (!el) return; const limits = String(" + q2 + ").split(',').map(Number); const min = Number.isFinite(limits[0]) ? limits[0] : -Infinity; const max = Number.isFinite(limits[1]) ? limits[1] : Infinity; const value = Number(el.value); if (!Number.isFinite(value) || value < min || value > max) { alert(" + JsStringLiteral(body) + "); if (typeof el.focus === 'function') el.focus(); } })();";
            case "session-storage-set": return "try { sessionStorage.setItem(" + q1 + ", " + q2 + "); } catch (error) { console.error('Не вдалося зберегти sessionStorage', error); }";
            case "session-storage-get": return "try { window[" + q2 + "] = sessionStorage.getItem(" + q1 + "); } catch (error) { console.error('Не вдалося прочитати sessionStorage', error); }";
            case "storage-set-json": return "try { localStorage.setItem(" + q1 + ", JSON.stringify(" + (string.IsNullOrWhiteSpace(p2) ? "null" : p2) + ")); } catch (error) { console.error('Не вдалося зберегти JSON', error); }";
            case "storage-get-json": return "try { const raw = localStorage.getItem(" + q1 + "); window[" + q2 + "] = raw === null ? null : JSON.parse(raw); } catch (error) { console.error('Не вдалося прочитати JSON зі сховища', error); }";
            case "fetch-json-variable": return "fetch(" + q1 + ").then(response => { if (!response.ok) throw new Error('HTTP ' + response.status); return response.json(); }).then(data => { window[" + q2 + "] = data; }).catch(error => console.error('WebBlox fetch JSON error:', error));";
            case "fetch-json-property": return "fetch(" + q1 + ").then(response => { if (!response.ok) throw new Error('HTTP ' + response.status); return response.json(); }).then(data => { const path = String(" + JsStringLiteral(p2) + ").split('.'); const value = path.reduce((obj, key) => obj == null ? undefined : obj[key], data); const el = " + BuildJsTargetExpression(body) + "; if (el) el.textContent = value == null ? '' : String(value); }).catch(error => console.error('WebBlox fetch property error:', error));";
            default: return "// Немає генератора JavaScript для блока: " + block.Kind;
        }
    }

    private static string BuildJsTargetExpression(string? targetValue)
    {
        string target = (targetValue ?? "").Trim();
        if (target.Length == 0) return "document.body";
        if (target is "window" or "document" or "document.body" or "document.documentElement") return target;
        if (target.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
            return "document.querySelector(" + JsStringLiteral(target[4..].Trim()) + ")";
        bool looksLikeSelector = target.StartsWith('#') || target.StartsWith('.') || target.Contains('[') || target.Contains(':') ||
                                 target.Contains('>') || target.Contains('+') || target.Contains('~') || target.Contains(' ') ||
                                 new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "html", "head", "body", "button", "input", "textarea", "select", "option", "optgroup", "form", "img", "a", "div", "span", "p", "main", "header", "footer", "nav", "section", "article", "aside", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "label", "fieldset", "legend", "small", "strong", "em", "b", "i", "u", "s", "code", "pre", "blockquote", "figure", "figcaption", "table", "thead", "tbody", "tfoot", "tr", "td", "th", "details", "summary", "dialog", "progress", "meter", "output", "canvas", "svg", "video", "audio", "source", "iframe", "hr", "br", "nav", "search", "menu", "hgroup", "time", "mark", "q", "abbr", "cite", "dfn", "kbd", "samp", "var" }.Contains(target);
        return looksLikeSelector
            ? "document.querySelector(" + JsStringLiteral(target) + ")"
            : "document.getElementById(" + JsStringLiteral(target) + ")";
    }

    private static string JsStringLiteral(string? value) => JsonSerializer.Serialize(value ?? "");

    private static string IndentJsBody(string? code, int indentLevel)
    {
        string source = string.IsNullOrWhiteSpace(code) ? "// Додай JavaScript-код у параметрах блока" : code.TrimEnd();
        string indent = new string(' ', Math.Max(0, indentLevel) * 2);
        return string.Join("\n", source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Select(line => indent + line));
    }

    private static string SanitizeJsIdentifier(string? raw, string fallback)
    {
        string name = Regex.Replace((raw ?? "").Trim(), @"[^A-Za-z0-9_$]", "");
        if (name.Length == 0) name = fallback;
        if (char.IsDigit(name[0])) name = "_" + name;
        if (new HashSet<string>(StringComparer.Ordinal) { "await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "enum", "export", "extends", "false", "finally", "for", "function", "if", "implements", "import", "in", "instanceof", "interface", "let", "new", "null", "package", "private", "protected", "public", "return", "static", "super", "switch", "this", "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield" }.Contains(name)) name = fallback;
        return name;
    }

    private void ImportJsBlocks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "WebBlox JavaScript blocks (*.jsblocks.json;*.json)|*.jsblocks.json;*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            List<JsVisualBlock> loaded = JsonSerializer.Deserialize<List<JsVisualBlock>>(File.ReadAllText(dialog.FileName)) ?? new List<JsVisualBlock>();
            if (loaded.Count > 500) throw new InvalidDataException("Файл містить більше 500 блоків.");
            if (_jsBlocks.Count > 0 && MessageBox.Show("Замінити поточну послідовність імпортованими блоками?", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _jsBlocks.Clear();
            foreach (JsVisualBlock block in loaded) if (block is not null && !string.IsNullOrWhiteSpace(block.Kind)) _jsBlocks.Add(block);
            if (_jsBlocks.Count > 0) JsBlockWorkspace.SelectedIndex = 0;
            StatusText.Text = $"Імпортовано {_jsBlocks.Count} JavaScript-блоків";
        }
        catch (Exception ex) { MessageBox.Show($"Не вдалося імпортувати блоки:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ExportJsBlocks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "WebBlox JavaScript blocks (*.jsblocks.json)|*.jsblocks.json|JSON file (*.json)|*.json", FileName = "script.jsblocks.json" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_jsBlocks.ToList(), new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            StatusText.Text = $"Експортовано {_jsBlocks.Count} JavaScript-блоків";
        }
        catch (Exception ex) { MessageBox.Show($"Не вдалося експортувати блоки:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ImportJavaScript_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JavaScript files (*.js)|*.js|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            JavaScriptBox.Text = File.ReadAllText(dialog.FileName);
            _javaScript = JavaScriptBox.Text;
            StatusText.Text = "JavaScript імпортовано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося імпортувати JavaScript:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportJavaScript_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "JavaScript file (*.js)|*.js", FileName = "script.js" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, JavaScriptBox.Text, new UTF8Encoding(false));
            StatusText.Text = "JavaScript експортовано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося експортувати JavaScript:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearJavaScript_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(JavaScriptBox.Text) &&
            MessageBox.Show("Очистити JavaScript-код?", "WebBlox", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        JavaScriptBox.Clear();
        _javaScript = "";
        StatusText.Text = "JavaScript очищено";
    }

    private string GenerateHtml(string? baseDirectory = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"uk\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        List<PageElement> allElements = EnumeratePageElements(_elements).ToList();
        PageElement? pageTitle = allElements.FirstOrDefault(x => x.TagName.Equals("title", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Content));
        sb.Append("  <title>").Append(WebUtility.HtmlEncode(pageTitle?.Content ?? "WebBlox website")).AppendLine("</title>");

        // Preview HTML is stored in a temp folder, so resolve relative resource paths individually
        // instead of using <base>. This preserves in-page links such as href="#section".

        // Metadata elements belong before stylesheets and document content.
        foreach (PageElement meta in allElements.Where(x => x.TagName.Equals("meta", StringComparison.OrdinalIgnoreCase)))
            AppendMetaToHead(sb, meta);

        // Baseline rules are deliberately emitted before external stylesheets so linked CSS
        // can override them, while CSS entered in WebBlox remains the final author stylesheet.
        sb.AppendLine("  <style id=\"webblox-base-styles\">");
        sb.AppendLine("    *{box-sizing:border-box}");
        sb.AppendLine("    body{font-family:Inter,Arial,sans-serif;margin:0;padding:32px;line-height:1.5;color:#202633}");
        sb.AppendLine("    img,video,iframe{max-width:100%}");
        sb.AppendLine("    button{cursor:pointer;padding:9px 16px}");
        sb.AppendLine("    a{color:#315bd6}");
        sb.AppendLine("  </style>");

        // <link> is a metadata element. Move all link tags to <head> even if the user
        // dragged them onto the visual tree; otherwise external stylesheets may not load.
        foreach (PageElement link in allElements.Where(x => x.TagName.Equals("link", StringComparison.OrdinalIgnoreCase)))
            AppendLinkToHead(sb, link, baseDirectory);

        // Support CSS entered as an HTML <style> element too, by emitting it in <head>.
        foreach (PageElement styleElement in allElements.Where(x => x.TagName.Equals("style", StringComparison.OrdinalIgnoreCase)))
            AppendStyleToHead(sb, styleElement, baseDirectory);

        if (!string.IsNullOrWhiteSpace(_globalCss))
        {
            sb.AppendLine("  <style id=\"webblox-custom-css\">");
            sb.AppendLine(string.IsNullOrWhiteSpace(baseDirectory) ? _globalCss : ResolveCssUrlsForPreview(_globalCss, baseDirectory));
            sb.AppendLine("  </style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        foreach (var item in _elements) AppendElement(sb, item, 1, baseDirectory);
        if (!string.IsNullOrWhiteSpace(_javaScript))
        {
            sb.AppendLine("  <script>");
            // Prevent a literal closing script tag in user code from terminating the HTML script element.
            string safeJavaScript = Regex.Replace(_javaScript, "</script", "<\\/script", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            sb.AppendLine(safeJavaScript);
            sb.AppendLine("  </script>");
        }
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static IEnumerable<PageElement> EnumeratePageElements(IEnumerable<PageElement> elements)
    {
        foreach (PageElement element in elements)
        {
            yield return element;
            foreach (PageElement child in EnumeratePageElements(element.Children))
                yield return child;
        }
    }

    private static void AppendMetaToHead(StringBuilder sb, PageElement element)
    {
        string field = element.Extra?.Trim() ?? string.Empty;
        string content = element.Content?.Trim() ?? string.Empty;
        if (field.Length == 0 && content.Length == 0) return;

        var attrs = new StringBuilder();
        if (field.Equals("charset", StringComparison.OrdinalIgnoreCase))
        {
            AddAttr(attrs, "charset", string.IsNullOrWhiteSpace(content) ? "UTF-8" : content);
        }
        else
        {
            AddAttr(attrs, "name", field);
            AddAttr(attrs, "content", content);
        }
        AddAttr(attrs, "id", element.Id);
        AddAttr(attrs, "class", element.ClassName);
        sb.Append("  <meta").Append(attrs).AppendLine(" />");
    }

    private static void AppendLinkToHead(StringBuilder sb, PageElement element, string? baseDirectory)
    {
        string href = element.Href?.Trim() ?? string.Empty;
        if (href.Length == 0) return;
        if (!string.IsNullOrWhiteSpace(baseDirectory)) href = ResolvePreviewUrl(href, baseDirectory);

        string rel = string.IsNullOrWhiteSpace(element.Extra) ? "stylesheet" : element.Extra.Trim();
        var attrs = new StringBuilder();
        AddAttr(attrs, "rel", rel);
        AddAttr(attrs, "href", href);
        if (rel.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.Equals("stylesheet", StringComparison.OrdinalIgnoreCase)))
            AddAttr(attrs, "type", "text/css");
        AddAttr(attrs, "id", element.Id);
        AddAttr(attrs, "class", element.ClassName);
        sb.Append("  <link").Append(attrs).AppendLine(" />");
    }

    private static void AppendStyleToHead(StringBuilder sb, PageElement element, string? baseDirectory)
    {
        var attrs = new StringBuilder();
        AddAttr(attrs, "id", element.Id);
        AddAttr(attrs, "class", element.ClassName);
        sb.Append("  <style").Append(attrs).AppendLine(">");
        if (!string.IsNullOrEmpty(element.Content))
            sb.AppendLine(string.IsNullOrWhiteSpace(baseDirectory) ? element.Content : ResolveCssUrlsForPreview(element.Content, baseDirectory));
        sb.AppendLine("  </style>");
    }

    private static string ResolvePreviewUrl(string value, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        string trimmed = value.Trim();
        if (trimmed.StartsWith('#') || trimmed.StartsWith("//", StringComparison.Ordinal)) return value;

        // Convert absolute Windows file paths to proper file:// URIs for the preview browser.
        if (Regex.IsMatch(trimmed, @"^[A-Za-z]:[\\/]"))
            return new Uri(System.IO.Path.GetFullPath(trimmed)).AbsoluteUri;

        // Do not rewrite network URLs, data URLs, mailto:, javascript:, etc.
        if (Regex.IsMatch(trimmed, @"^[A-Za-z][A-Za-z0-9+.-]*:")) return value;
        if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory)) return value;

        int suffixIndex = trimmed.IndexOfAny(new[] { '?', '#' });
        string suffix = suffixIndex >= 0 ? trimmed[suffixIndex..] : string.Empty;
        string relativePath = suffixIndex >= 0 ? trimmed[..suffixIndex] : trimmed;
        relativePath = Uri.UnescapeDataString(relativePath).Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar);
        // A website-root path (for example /styles.css) maps to the project root during local preview.
        relativePath = relativePath.TrimStart(System.IO.Path.DirectorySeparatorChar);
        string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, relativePath));
        return new Uri(fullPath).AbsoluteUri + suffix;
    }

    private static string ResolveCssUrlsForPreview(string css, string baseDirectory)
    {
        string result = Regex.Replace(css,
            @"url\(\s*(?:'(?<single>[^']*)'|""(?<double>[^""]*)""|(?<bare>[^)]*?))\s*\)",
            match =>
            {
                string raw = match.Groups["single"].Success ? match.Groups["single"].Value
                    : match.Groups["double"].Success ? match.Groups["double"].Value
                    : match.Groups["bare"].Value.Trim();
                // Keep CSS functions such as url(var(--asset)) unchanged.
                if (raw.Contains('(') || raw.Length == 0) return match.Value;
                string resolved = ResolvePreviewUrl(raw, baseDirectory);
                if (resolved == raw) return match.Value;
                if (match.Groups["single"].Success) return $"url('{resolved}')";
                if (match.Groups["double"].Success) return $"url(\"{resolved}\")";
                return $"url({resolved})";
            }, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        result = Regex.Replace(result,
            @"(?<prefix>@import\s+)(?<quote>['""])(?<url>[^'""]+)\k<quote>",
            match =>
            {
                string raw = match.Groups["url"].Value;
                string resolved = ResolvePreviewUrl(raw, baseDirectory);
                return match.Groups["prefix"].Value + match.Groups["quote"].Value + resolved + match.Groups["quote"].Value;
            }, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return result;
    }

    private static void AppendElement(StringBuilder sb, PageElement e, int depth, string? baseDirectory)
    {
        string tag = e.TagName.ToLowerInvariant();
        // These elements are emitted in <head> by GenerateHtml and must not be duplicated in <body>.
        if (tag is "link" or "style" or "meta" or "title") return;

        // WebBlox already generates the document shell. Treat dropped shell tags as transparent
        // containers instead of outputting invalid nested <html>/<head>/<body> elements.
        if (tag is "html" or "head" or "body")
        {
            foreach (var child in e.Children) AppendElement(sb, child, depth, baseDirectory);
            return;
        }

        string indent = new(' ', depth * 2);
        var attrs = new StringBuilder();
        AddAttr(attrs, "id", e.Id);
        AddAttr(attrs, "class", e.ClassName);
        if (tag is "a" or "area") AddAttr(attrs, "href", string.IsNullOrWhiteSpace(baseDirectory) ? e.Href : ResolvePreviewUrl(e.Href, baseDirectory));
        if (tag is "img" or "video" or "audio" or "source" or "iframe" or "script")
            AddAttr(attrs, "src", string.IsNullOrWhiteSpace(baseDirectory) ? e.Src : ResolvePreviewUrl(e.Src, baseDirectory));
        if (tag == "img") AddAttr(attrs, "alt", e.Extra);
        else if (tag == "input") AddAttr(attrs, "type", string.IsNullOrWhiteSpace(e.Extra) ? "text" : e.Extra);
        else if (tag == "textarea") AddAttr(attrs, "placeholder", e.Extra);
        else if (tag == "button") AddAttr(attrs, "type", string.IsNullOrWhiteSpace(e.Extra) ? "button" : e.Extra);
        string style = BuildStyle(e);
        AddAttr(attrs, "style", style);
        sb.Append(indent).Append('<').Append(tag).Append(attrs);
        if (VoidTags.Contains(tag)) { sb.AppendLine(" />"); return; }
        sb.Append('>');
        if (!string.IsNullOrEmpty(e.Content))
        {
            if (tag is "script") sb.Append(e.Content);
            else sb.Append(WebUtility.HtmlEncode(e.Content));
        }
        if (e.Children.Count > 0)
        {
            sb.AppendLine();
            foreach (var child in e.Children) AppendElement(sb, child, depth + 1, baseDirectory);
            sb.Append(indent);
        }
        sb.Append("</").Append(tag).AppendLine(">");
    }

    private static void AddAttr(StringBuilder sb, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.Append(' ').Append(name).Append("=\"").Append(WebUtility.HtmlEncode(value.Trim())).Append('"');
    }

    private static string BuildStyle(PageElement e)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(e.Color)) parts.Add($"color:{e.Color.Trim()}");
        if (!string.IsNullOrWhiteSpace(e.Background)) parts.Add($"background:{e.Background.Trim()}");
        if (!string.IsNullOrWhiteSpace(e.FontSize)) parts.Add($"font-size:{e.FontSize.Trim()}");
        if (!string.IsNullOrWhiteSpace(e.Margin)) parts.Add($"margin:{e.Margin.Trim()}");
        if (!string.IsNullOrWhiteSpace(e.Padding)) parts.Add($"padding:{e.Padding.Trim()}");
        // Backward compatibility for older project files not yet migrated by the inspector.
        if (string.IsNullOrWhiteSpace(e.Margin) && string.IsNullOrWhiteSpace(e.Padding) && !string.IsNullOrWhiteSpace(e.Spacing))
        { parts.Add($"margin:{e.Spacing.Trim()}"); parts.Add($"padding:{e.Spacing.Trim()}"); }
        if (!string.IsNullOrWhiteSpace(e.CustomCss)) parts.Add(e.CustomCss.Trim().TrimEnd(';'));
        return string.Join(';', parts);
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebBlox-preview.html");
            string resourceDirectory = !string.IsNullOrWhiteSpace(_projectDirectory) && Directory.Exists(_projectDirectory)
                ? _projectDirectory
                : Environment.CurrentDirectory;
            File.WriteAllText(path, GenerateHtml(resourceDirectory), new UTF8Encoding(false));
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusText.Text = "Попередній перегляд відкрито у браузері";
        }
        catch (Exception ex) { MessageBox.Show($"Could not open preview:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "HTML file (*.html)|*.html", FileName = "index.html" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, GenerateHtml(), new UTF8Encoding(false));
            _projectDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
            StatusText.Text = "HTML-сайт експортовано";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося експортувати HTML:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "WebBlox project (*.webblox)|*.webblox|JSON file (*.json)|*.json", FileName = "MyWebsite.webblox" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var project = new WebBloxProject { Elements = _elements.ToList(), GlobalCss = GlobalCssBox.Text, JavaScript = JavaScriptBox.Text, JavaScriptBlocks = _jsBlocks.ToList() };
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true }));
            _projectDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
            StatusText.Text = "Проєкт збережено";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося зберегти проєкт:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "WebBlox project (*.webblox;*.json)|*.webblox;*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            string json = File.ReadAllText(dialog.FileName);
            bool isLegacyElementList = json.TrimStart().StartsWith("[", StringComparison.Ordinal);
            WebBloxProject? project = isLegacyElementList ? null : JsonSerializer.Deserialize<WebBloxProject>(json);
            List<PageElement> loaded = isLegacyElementList
                ? (JsonSerializer.Deserialize<List<PageElement>>(json) ?? new())
                : (project?.Elements ?? new());
            _elements.Clear(); foreach (var item in loaded) _elements.Add(item);
            _loadingCss = true;
            GlobalCssBox.Text = isLegacyElementList ? "" : (project?.GlobalCss ?? "");
            _globalCss = GlobalCssBox.Text;
            _loadingCss = false;
            JavaScriptBox.Text = isLegacyElementList ? "" : (project?.JavaScript ?? "");
            _javaScript = JavaScriptBox.Text;
            _jsBlocks.Clear();
            if (!isLegacyElementList && project?.JavaScriptBlocks is not null) foreach (JsVisualBlock block in project.JavaScriptBlocks) if (block is not null) _jsBlocks.Add(block);
            if (_jsBlocks.Count > 0) JsBlockWorkspace.SelectedIndex = 0; else RefreshJsBlockEditor(null);
            _selected = null; ClearProperties(); RenderTree();
            _projectDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
            StatusText.Text = "Проєкт відкрито";
        }
        catch (Exception ex) { MessageBox.Show($"Could not open project:\n{ex.Message}", "WebBlox", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}

public sealed class JsFunctionSnippet
{
    public string Category { get; }
    public string Title { get; }
    public string Description { get; }
    public string Requirements { get; }
    public string Color { get; }
    public string Code { get; }

    public JsFunctionSnippet(string category, string title, string description, string requirements, string color, string code)
    {
        Category = category;
        Title = title;
        Description = description;
        Requirements = requirements;
        Color = color;
        Code = code;
    }
}

public sealed class JsBlockTemplate
{
    public string Category { get; }
    public string Title { get; }
    public string Description { get; }
    public string Kind { get; }
    public string Color { get; }
    public string Param1Label { get; }
    public string DefaultParam1 { get; }
    public string Param2Label { get; }
    public string DefaultParam2 { get; }
    public string Param3Label { get; }
    public string DefaultParam3 { get; }
    public string BodyLabel { get; }
    public string DefaultBody { get; }

    public JsBlockTemplate(string category, string title, string description, string kind, string color,
        string param1Label, string defaultParam1, string param2Label, string defaultParam2,
        string param3Label, string defaultParam3, string bodyLabel, string defaultBody)
    {
        Category = category; Title = title; Description = description; Kind = kind; Color = color;
        Param1Label = param1Label; DefaultParam1 = defaultParam1; Param2Label = param2Label; DefaultParam2 = defaultParam2;
        Param3Label = param3Label; DefaultParam3 = defaultParam3; BodyLabel = bodyLabel; DefaultBody = defaultBody;
    }
}

public sealed class JsVisualBlock : INotifyPropertyChanged
{
    private string _param1 = "";
    private string _param2 = "";
    private string _param3 = "";
    private string _body = "";

    public string Kind { get; set; } = "custom-code";
    public string Category { get; set; } = "Користувацький код";
    public string Title { get; set; } = "JavaScript-блок";
    public string Description { get; set; } = "Візуальний блок JavaScript.";
    public string Color { get; set; } = "#64748B";
    public string Param1Label { get; set; } = "";
    public string Param2Label { get; set; } = "";
    public string Param3Label { get; set; } = "";
    public string BodyLabel { get; set; } = "";

    public string Param1
    {
        get => _param1;
        set { if (_param1 == value) return; _param1 = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }
    public string Param2
    {
        get => _param2;
        set { if (_param2 == value) return; _param2 = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }
    public string Param3
    {
        get => _param3;
        set { if (_param3 == value) return; _param3 = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }
    public string Body
    {
        get => _body;
        set { if (_body == value) return; _body = value ?? ""; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string Summary
    {
        get
        {
            string summary = string.Join(" · ", new[] { Param1, Param2, Param3 }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (summary.Length > 130) summary = summary[..127] + "…";
            return string.IsNullOrWhiteSpace(summary) ? Description : summary;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public JsVisualBlock() { }

    public JsVisualBlock(JsBlockTemplate template)
    {
        Kind = template.Kind; Category = template.Category; Title = template.Title; Description = template.Description; Color = template.Color;
        Param1Label = template.Param1Label; Param2Label = template.Param2Label; Param3Label = template.Param3Label; BodyLabel = template.BodyLabel;
        Param1 = template.DefaultParam1; Param2 = template.DefaultParam2; Param3 = template.DefaultParam3; Body = template.DefaultBody;
    }

    public JsVisualBlock Clone() => new()
    {
        Kind = Kind, Category = Category, Title = Title, Description = Description, Color = Color,
        Param1Label = Param1Label, Param1 = Param1, Param2Label = Param2Label, Param2 = Param2,
        Param3Label = Param3Label, Param3 = Param3, BodyLabel = BodyLabel, Body = Body
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class CssPaletteCategory
{
    public string Name { get; }
    public string Color { get; }
    public string GroupKey { get; }
    public CssPaletteCategory(string name, string color, string groupKey) { Name = name; Color = color; GroupKey = groupKey; }
}

public sealed class CssPaletteItem
{
    public string DisplayName { get; }
    public string Property { get; }
    public string Hint { get; }
    public string DefaultValue { get; }
    public CssPaletteItem(string displayName, string property, string hint, string defaultValue)
    { DisplayName = displayName; Property = property; Hint = hint; DefaultValue = defaultValue; }
}

public sealed class CssDeclaration
{
    public string Property { get; set; }
    public string Value { get; set; }
    public CssDeclaration(string property, string value) { Property = property; Value = value; }
}

public sealed class WebBloxProject { public int Version { get; set; } = 5; public string GlobalCss { get; set; } = ""; public string JavaScript { get; set; } = ""; public List<JsVisualBlock> JavaScriptBlocks { get; set; } = new(); public List<PageElement> Elements { get; set; } = new(); }

public sealed class PageElement
{
    public string TagName { get; set; } = "p";
    public string Content { get; set; } = "";
    public string Id { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Href { get; set; } = "";
    public string Src { get; set; } = "";
    public string Extra { get; set; } = "";
    public string Color { get; set; } = "";
    public string Background { get; set; } = "";
    public string FontSize { get; set; } = "";
    // Legacy Spacing remains for compatibility with existing .webblox files.
    public string Spacing { get; set; } = "";
    public string Margin { get; set; } = "";
    public string Padding { get; set; } = "";
    public string CustomCss { get; set; } = "";
    public ObservableCollection<PageElement> Children { get; set; } = new();
    public PageElement() { }
    public PageElement(string tagName, string content) { TagName = tagName; Content = content; }
}
