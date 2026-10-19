using System.Windows;
using System.Windows.Input;
using DynamicIslandMedia.Models;
using DynamicIslandMedia.ViewModels;

namespace DynamicIslandMedia
{
    public partial class SettingsWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly AppSettings _settings;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _settings = viewModel.Settings;

            // Load values
            BottomMarginSlider.Value = _settings.BottomMargin;
            AutoExpandCheckBox.IsChecked = _settings.AutoExpandOnHover;
            AutoHideCheckBox.IsChecked = _settings.AutoHideWhenIdle;
            StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            BottomMarginSlider.Value = AppSettings.DefaultBottomMargin;
            AutoExpandCheckBox.IsChecked = true;
            AutoHideCheckBox.IsChecked = false;
            StartWithWindowsCheckBox.IsChecked = false;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.BottomMargin = BottomMarginSlider.Value;
            _settings.AutoExpandOnHover = AutoExpandCheckBox.IsChecked ?? true;
            _settings.AutoHideWhenIdle = AutoHideCheckBox.IsChecked ?? false;
            _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;

            _settings.Save();
            _viewModel.BottomMargin = _settings.BottomMargin;
            _viewModel.UpdatePresenceVisibility();

            Close();
        }
    }
}
