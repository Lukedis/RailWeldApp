using System.Windows;

namespace RailWeldApp.DependencyProperties;

public static class CornerRadiusHelper
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(CornerRadiusHelper),
            new FrameworkPropertyMetadata(
                new CornerRadius(8),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static void SetCornerRadius(DependencyObject element, CornerRadius value) =>
        element.SetValue(CornerRadiusProperty, value);

    public static CornerRadius GetCornerRadius(DependencyObject element) =>
        (CornerRadius)element.GetValue(CornerRadiusProperty);
}
