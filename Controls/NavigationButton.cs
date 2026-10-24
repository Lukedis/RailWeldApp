using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RailWeldApp.Enums;

namespace RailWeldApp.Controls
{
    public class NavigationButton : Button
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(NavigationButton),
                new PropertyMetadata(false));

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty IconTextProperty =
            DependencyProperty.Register(
                nameof(IconText),
                typeof(string),
                typeof(NavigationButton),
                new PropertyMetadata(string.Empty));

        public string IconText
        {
            get { return (string)GetValue(IconTextProperty); }
            set { SetValue(IconTextProperty, value); }
        }

        // ------------------------------------------------------------------
        //  InfoBadge
        // ------------------------------------------------------------------

        public static readonly DependencyProperty InfoBadgeKindProperty =
            DependencyProperty.Register(
                nameof(InfoBadgeKind),
                typeof(InfoBadgeKind),
                typeof(NavigationButton),
                new PropertyMetadata(InfoBadgeKind.None));

        public InfoBadgeKind InfoBadgeKind
        {
            get { return (InfoBadgeKind)GetValue(InfoBadgeKindProperty); }
            set { SetValue(InfoBadgeKindProperty, value); }
        }

        public static readonly DependencyProperty InfoBadgeValueProperty =
            DependencyProperty.Register(
                nameof(InfoBadgeValue),
                typeof(object),
                typeof(NavigationButton),
                new PropertyMetadata(null));

        public object InfoBadgeValue
        {
            get { return GetValue(InfoBadgeValueProperty); }
            set { SetValue(InfoBadgeValueProperty, value); }
        }

        public static readonly DependencyProperty InfoBadgeCountProperty =
            DependencyProperty.Register(
                nameof(InfoBadgeCount),
                typeof(int),
                typeof(NavigationButton),
                new PropertyMetadata(0, OnInfoBadgeCountChanged));

        public int InfoBadgeCount
        {
            get { return (int)GetValue(InfoBadgeCountProperty); }
            set { SetValue(InfoBadgeCountProperty, value); }
        }

        public static readonly DependencyProperty InfoBadgeBackgroundProperty =
            DependencyProperty.Register(
                nameof(InfoBadgeBackground),
                typeof(Brush),
                typeof(NavigationButton),
                new PropertyMetadata(null));

        public Brush InfoBadgeBackground
        {
            get { return (Brush)GetValue(InfoBadgeBackgroundProperty); }
            set { SetValue(InfoBadgeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InfoBadgeForegroundProperty =
            DependencyProperty.Register(
                nameof(InfoBadgeForeground),
                typeof(Brush),
                typeof(NavigationButton),
                new PropertyMetadata(null));

        public Brush InfoBadgeForeground
        {
            get { return (Brush)GetValue(InfoBadgeForegroundProperty); }
            set { SetValue(InfoBadgeForegroundProperty, value); }
        }

        private static void OnInfoBadgeCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NavigationButton button)
            {
                var count = (int)e.NewValue;

                if (count > 0)
                {
                    button.InfoBadgeKind = InfoBadgeKind.Value;
                    button.InfoBadgeValue = count > 99 ? "99+" : count.ToString();
                }
                else
                {
                    button.InfoBadgeKind = InfoBadgeKind.None;
                    button.ClearValue(InfoBadgeValueProperty);
                }
            }
        }
    }
}
