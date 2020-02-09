using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GomaShio
{
    public sealed partial class PasswordDialog : ContentDialog
    {
        public bool IsFirstTime { get; set; }
        public string Password { get; set; }
        public bool SavePassword { get; set; }
        public bool IsOK { get; set; }

        public PasswordDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            if ( IsFirstTime )
                PasswordErrorText.Visibility = Visibility.Collapsed;
            else
                PasswordErrorText.Visibility = Visibility.Visible;
            PasswordText.Text = Password;
            SavePasswordCheck.IsChecked = SavePassword;
            IsOK = false;
        }

        // On select OK button
        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
            
            Password = PasswordText.Text;
            SavePassword = SavePasswordCheck.IsChecked.GetValueOrDefault( false );
            IsOK = true;
        }

        // On select cancel button
        private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;

            IsOK = false;
        }
    }
}
