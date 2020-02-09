using Windows.UI.Xaml.Controls;

namespace GomaShio
{
    public sealed partial class SetNewPassDialog : ContentDialog
    {
        public bool IsOK { get; set; }
        public string Password { get; set; }
        public bool SavePassword { get; set; }

        public SetNewPassDialog()
        {
            this.InitializeComponent();
            IsOK = false;
            if ( null == Password )
                Password = "";

            PasswordText1.Text = Password;
            PasswordText2.Text = Password;
            SavePasswordCheck.IsChecked = SavePassword;

            this.IsPrimaryButtonEnabled = ( PasswordText1.Text.Length > 1 );
        }

        // OK button is clicked
        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
            if ( PasswordText1.Text != PasswordText2.Text || PasswordText1.Text.Length <= 1 ) {
                return ;
            }
            IsOK = true;
            Password = PasswordText1.Text;
            SavePassword = SavePasswordCheck.IsChecked.GetValueOrDefault( false );
        }

        // Cancel button is clicked
        private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
            IsOK = false;
        }

        // On edit PasswordText1 textbox
        private void PasswordText1_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;
            IsPrimaryButtonEnabled = ( PasswordText1.Text == PasswordText2.Text && PasswordText1.Text.Length > 1 );
        }

        // On edit PasswordText2 textbox
        private void PasswordText2_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;
            IsPrimaryButtonEnabled = ( PasswordText1.Text == PasswordText2.Text && PasswordText1.Text.Length > 1 );
        }
    }
}
