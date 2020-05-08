using Windows.UI.Xaml.Controls;

namespace GomaShio
{
    public sealed partial class SetNewPassDialog : ContentDialog
    {
        public bool IsOK { get; set; }
        public string OldPassword { get; set; }
        public string Password { get; set; }
        public bool SavePassword { get; set; }

        private bool m_NeedOldPassword;

        public SetNewPassDialog( bool needOldPassword, bool unmatchMsg )
        {
            this.InitializeComponent();

            IsOK = false;
            if ( null == Password )
                Password = "";

            m_NeedOldPassword = needOldPassword;

            if ( needOldPassword ) {
                SetPasswordDlgOldTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
                OldPasswordText.Visibility = Windows.UI.Xaml.Visibility.Visible;
                if ( unmatchMsg ) {
                    PasswordErrorText.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else {
                    PasswordErrorText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                PrimaryButtonText = GlbFunc.GetResourceString( "SetNewPassDialog_ChangePass_PrimaryButtonText", "OK" );
            }
            else {
                SetPasswordDlgOldTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                OldPasswordText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                PasswordErrorText.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                PrimaryButtonText = GlbFunc.GetResourceString( "SetNewPassDialog_SetNewPass_PrimaryButtonText", "OK" );
            }

            OldPasswordText.Text = "";
            NewPasswordText.Text = Password;
            ConfirmText.Text = Password;
            SavePasswordCheck.IsChecked = false;

            this.IsPrimaryButtonEnabled = false;
        }

        // OK button is clicked
        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;

            if ( !CheckPrimaryButtonStatus() ) return ;

            IsOK = true;
            OldPassword = OldPasswordText.Text;
            Password = NewPasswordText.Text;
            SavePassword = SavePasswordCheck.IsChecked.GetValueOrDefault( false );
        }

        // Cancel button is clicked
        private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
            IsOK = false;
        }

        private void OldPasswordText_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;
            this.IsPrimaryButtonEnabled = CheckPrimaryButtonStatus();
        }

        // On edit NewPasswordText textbox
        private void NewPasswordText_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;
            this.IsPrimaryButtonEnabled = CheckPrimaryButtonStatus();
        }

        // On edit ConfirmText textbox
        private void ConfirmText_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;
            this.IsPrimaryButtonEnabled = CheckPrimaryButtonStatus();
        }

        // Check OK button is enabled or not
        bool CheckPrimaryButtonStatus()
        {
            // If old password is needed and old password is empty or extremely short, OK button is disabled.
            if ( m_NeedOldPassword && OldPasswordText.Text.Length <= 1 )
                return false;

            // If new password and confirm is not match, or new password is extremely short, OK button is disabled.
            if ( NewPasswordText.Text != ConfirmText.Text || NewPasswordText.Text.Length <= 1 )
                return false;

            return true;
        }
    }
}
