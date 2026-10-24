using System.Windows;

namespace RailWeldApp.DependencyProperties
{
    public static class TextBoxHelper
    {
        public static readonly DependencyProperty IconTextProperty =
            DependencyProperty.RegisterAttached(
                "IconText",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetIconText(DependencyObject element, string value)
            => element.SetValue(IconTextProperty, value);

        public static string GetIconText(DependencyObject element)
            => (string)element.GetValue(IconTextProperty);

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetPlaceholder(DependencyObject element, string value)
            => element.SetValue(PlaceholderProperty, value);

        public static string GetPlaceholder(DependencyObject element)
            => (string)element.GetValue(PlaceholderProperty);
    }
}
