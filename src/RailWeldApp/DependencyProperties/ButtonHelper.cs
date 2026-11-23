using System.Windows;
using System.Windows.Media;

namespace RailWeldApp.DependencyProperties
{
    public static class ButtonHelper
    {
        public static readonly DependencyProperty IsLogginginProperty =
           DependencyProperty.RegisterAttached(
               "IsLoggingin",
               typeof(bool),
               typeof(ButtonHelper),
               new FrameworkPropertyMetadata(false));

        public static void SetIsLoggingin(DependencyObject element, bool value)
            => element.SetValue(IsLogginginProperty, value);

        public static bool GetIsLoggingin(DependencyObject element)
            => (bool)element.GetValue(IsLogginginProperty);

        public static readonly DependencyProperty ProfilePictureSourceProperty =
            DependencyProperty.RegisterAttached(
                "ProfilePictureSource",
                typeof(ImageSource),
                typeof(ButtonHelper),
                new FrameworkPropertyMetadata(null));

        public static void SetProfilePictureSource(DependencyObject element, ImageSource? value)
            => element.SetValue(ProfilePictureSourceProperty, value);

        public static ImageSource? GetProfilePictureSource(DependencyObject element)
            => (ImageSource?)element.GetValue(ProfilePictureSourceProperty);

        public static readonly DependencyProperty NumberCountProperty =
                DependencyProperty.RegisterAttached(
                            "NumberCount",
                            typeof(int),
                            typeof(ButtonHelper),
                            new FrameworkPropertyMetadata(0));

        public static void SetNumberCount(DependencyObject element, int value)
            => element.SetValue(NumberCountProperty, value);

        public static int GetNumberCount(DependencyObject element)
            => (int)element.GetValue(NumberCountProperty);
    }
}
