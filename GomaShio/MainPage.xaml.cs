using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GomaShio
{
    /// <summary>
    /// MainPage class.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// It called on starting of the application.
        /// It decide that configurations is made up or not, and show the next page.
        /// </summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if ( !await AppData.IsInitialized().ConfigureAwait( true ) ) {
                // If first time, show FileSettingsPage.
                await GlbFunc.ShowMessage( "MSG_INITIAL_USE_MSG", "On first use, specify password file name and master pasword." ).ConfigureAwait( true );
                MainPageConfiguRadio.IsChecked = true;
            }
            else {
                MainPageAccountListRadio.IsChecked = true;
            }
        }

        /// <summary>
        /// FileSettingsPage was selected in the menu.
        /// Navigate to File setting page.
        /// </summary>
        private void RadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            MainContentFrame.Navigate(typeof(FileSettingsPage));
            splitView.IsPaneOpen = false;
        }

        /// <summary>
        /// PasswordListPage was selected in the menu.
        /// If configurations is made up already, navigate to password list page, if not, ignore this event.
        /// </summary>
        private async void RadioButton2_Checked(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            // if it is not initialized, show FileSettingsPage forcly.
            if ( !await AppData.IsInitialized().ConfigureAwait( true ) ) {
                MainPageConfiguRadio.IsChecked = true;
            }
            else {
                MainContentFrame.Navigate(typeof(PasswordListPage));
                splitView.IsPaneOpen = false;
            }
        }
    }
}
