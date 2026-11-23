using System.Windows;
using System.Windows.Controls;

namespace RailWeldApp.DependencyProperties
{
    public static class PassWordBoxHelper
    {
        public static readonly DependencyProperty IconTextProperty =
            DependencyProperty.RegisterAttached(
                "IconText",
                typeof(string),
                typeof(PassWordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetIconText(DependencyObject element, string value)
            => element.SetValue(IconTextProperty, value);

        public static string GetIconText(DependencyObject element)
            => (string)element.GetValue(IconTextProperty);

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(PassWordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetPlaceholder(DependencyObject element, string value)
            => element.SetValue(PlaceholderProperty, value);

        public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.RegisterAttached(
                "IsEmpty",
                typeof(bool),
                typeof(PassWordBoxHelper),
                new PropertyMetadata(true));

        public static void SetIsEmpty(DependencyObject element, bool value) => element.SetValue(IsEmptyProperty, value);

        public static bool GetIsEmpty(DependencyObject element) => (bool)element.GetValue(IsEmptyProperty);

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached(
                "Password",
                typeof(string),
                typeof(PassWordBoxHelper),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordChanged));

        private static readonly DependencyProperty _isUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PassWordBoxHelper),
                new PropertyMetadata(false));

        public static void SetPassword(DependencyObject element, string value) => element.SetValue(PasswordProperty, value);

        public static string GetPassword(DependencyObject element) => (string)element.GetValue(PasswordProperty);

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
                return;

            if ((bool)passwordBox.GetValue(_isUpdatingProperty))
                return;

            var password = e.NewValue as string ?? string.Empty;

            if (passwordBox.Password == password)
                return;

            passwordBox.SetValue(_isUpdatingProperty, true);

            try
            {
                passwordBox.Password = password;
            }
            finally
            {
                passwordBox.SetValue(_isUpdatingProperty, false);
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox passwordBox)
                return;

            // 占位提示用：无论用户输入还是代码赋值，都实时同步"是否为空"
            passwordBox.SetValue(IsEmptyProperty, string.IsNullOrEmpty(passwordBox.Password));

            if ((bool)passwordBox.GetValue(_isUpdatingProperty))
                return;

            passwordBox.SetValue(_isUpdatingProperty, true);

            try
            {
                SetPassword(passwordBox, passwordBox.Password);
            }
            finally
            {
                passwordBox.SetValue(_isUpdatingProperty, false);
            }
        }

        static PassWordBoxHelper()
        {
            EventManager.RegisterClassHandler(typeof(PasswordBox), PasswordBox.PasswordChangedEvent,
                new RoutedEventHandler(PasswordBox_PasswordChanged));
        }
    }
}
