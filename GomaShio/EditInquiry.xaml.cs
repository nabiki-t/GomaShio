using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace GomaShio
{
    public sealed partial class EditInquiry : ContentDialog
    {
        public string ItemName { get; set; }
        public string ItemValue { get; set; }
        public bool IsOK { get; set; }
        public bool HideItemValue { get; set; }

        public EditInquiry()
        {
            ItemName = "";
            ItemValue = "";
            IsOK = false;
            HideItemValue = true;
            this.InitializeComponent();
        }

        private void ContentDialog_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Set values before editing to controls.
            if ( null == ItemName )
                ItemName = "";
            if ( null == ItemValue )
                ItemValue = "";
            InquiryItemName.Text = ItemName;
            InquiryItemValue.Text = ItemValue;
            HideItemValueCheck.IsChecked = HideItemValue;
            IsOK = false;

            // Restore password configuration
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PasswordDigit" ) )
                PasswordDigit.Text = (string)ApplicationData.Current.LocalSettings.Values[ "PasswordDigit" ];
            else
                PasswordDigit.Text = GlbFunc.GetResourceString( "DefaultPasswordDigits", "8" );
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassLowerCaseCheck" ) )
                PassLowerCaseCheck.IsChecked = (bool)ApplicationData.Current.LocalSettings.Values[ "PassLowerCaseCheck" ];
            else
                PassLowerCaseCheck.IsChecked = true;
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassUpperCaseCheck" ) )
                PassUpperCaseCheck.IsChecked = (bool)ApplicationData.Current.LocalSettings.Values[ "PassUpperCaseCheck" ];
            else
                PassUpperCaseCheck.IsChecked = true;
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassNumberCheck" ) )
                PassNumberCheck.IsChecked = (bool)ApplicationData.Current.LocalSettings.Values[ "PassNumberCheck" ];
            else
                PassNumberCheck.IsChecked = true;
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassSymbolCheck" ) )
                PassSymbolCheck.IsChecked = (bool)ApplicationData.Current.LocalSettings.Values[ "PassSymbolCheck" ];
            else
                PassSymbolCheck.IsChecked = true;
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassSymbolCandidate" ) )
                PassSymbolCandidate.Text = (string)ApplicationData.Current.LocalSettings.Values[ "PassSymbolCandidate" ];
            else
                PassSymbolCandidate.Text = GlbFunc.GetResourceString( "DefaultSymbolCandidateString", "!$%'()*,/;=>?[]{}" );
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "PassExcludeConfuseCheck" ) )
                PassExcludeConfuseCheck.IsChecked = (bool)ApplicationData.Current.LocalSettings.Values[ "PassExcludeConfuseCheck" ];
            else
                PassExcludeConfuseCheck.IsChecked = true;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;

            // Get values after editing from the controls.
            ItemName = InquiryItemName.Text;
            ItemValue = InquiryItemValue.Text;
            HideItemValue = HideItemValueCheck.IsChecked.GetValueOrDefault( false );
            IsOK = true;

            // ApplicationData.Current.LocalSettings.Values.ContainsKey( "FileName" )
            // Save password configuration to storage
            ApplicationData.Current.LocalSettings.Values[ "PasswordDigit" ] = PasswordDigit.Text;
            ApplicationData.Current.LocalSettings.Values[ "PassLowerCaseCheck" ] = PassLowerCaseCheck.IsChecked.GetValueOrDefault( false );
            ApplicationData.Current.LocalSettings.Values[ "PassUpperCaseCheck" ] = PassUpperCaseCheck.IsChecked.GetValueOrDefault( false );
            ApplicationData.Current.LocalSettings.Values[ "PassNumberCheck" ] = PassNumberCheck.IsChecked.GetValueOrDefault( false );
            ApplicationData.Current.LocalSettings.Values[ "PassSymbolCheck" ] = PassSymbolCheck.IsChecked.GetValueOrDefault( false );
            ApplicationData.Current.LocalSettings.Values[ "PassSymbolCandidate" ] = PassSymbolCandidate.Text;
            ApplicationData.Current.LocalSettings.Values[ "PassExcludeConfuseCheck" ] = PassExcludeConfuseCheck.IsChecked.GetValueOrDefault( false );
        }

        private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;

            IsOK = false;
        }

        // PassGenerateButton is clicked
        private void PassGenerateButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;
            int digit = 8;

            // Get password digit. If failed, default is 8.
            if ( !Int32.TryParse( PasswordDigit.Text, out digit ) ) {
                digit = 8;
                PasswordDigit.Text = GlbFunc.GetResourceString( "DefaultPasswordDigits", "8" );
            }
            if ( digit <= 0 ) {
                InquiryItemValue.Text = "";
                return ;
            }
            if ( digit > 128 ) digit = 128;

            int candCnt = 0;
            if ( PassLowerCaseCheck.IsChecked.GetValueOrDefault( false ) ) candCnt++;
            if ( PassUpperCaseCheck.IsChecked.GetValueOrDefault( false ) ) candCnt++;
            if ( PassNumberCheck.IsChecked.GetValueOrDefault( false ) ) candCnt++;
            if ( PassSymbolCheck.IsChecked.GetValueOrDefault( false ) && PassSymbolCandidate.Text.Length > 0 ) candCnt++;

            if ( candCnt <= 0 ) {
                // If usable condidate chars is not exist, clear password text and exit.
                InquiryItemValue.Text = "";
                return ;
            }

            String [] cand = new String[candCnt];
            bool excludeConfuse = PassExcludeConfuseCheck.IsChecked.GetValueOrDefault( false );

            candCnt = 0;
            if ( PassLowerCaseCheck.IsChecked.GetValueOrDefault( false ) ) {
                if ( excludeConfuse )
                    cand[ candCnt ] = "acdefhijkmprstuvxyz";
                else
                    cand[ candCnt ] = "abcdefghijklmnopqrstuvwxyz";
                candCnt++;
            }
            if ( PassUpperCaseCheck.IsChecked.GetValueOrDefault( false ) ) {
                if ( excludeConfuse )
                    cand[ candCnt ] = "ABCDEFGHJKLMNPQRSTUVXYZ";
                else
                    cand[ candCnt ] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                candCnt++;
            }
            if ( PassNumberCheck.IsChecked.GetValueOrDefault( false ) ) {
                if ( excludeConfuse )
                    cand[ candCnt ] = "1234578";
                else
                    cand[ candCnt ] = "0123456789";
                candCnt++;
            }
            if ( PassSymbolCheck.IsChecked.GetValueOrDefault( false ) && PassSymbolCandidate.Text.Length > 0 ) {
                cand[ candCnt ] = PassSymbolCandidate.Text;
                candCnt++;
            }

            StringBuilder allCandCharsSB = new StringBuilder();
            for ( int i = 0; i < candCnt; i++ )
                allCandCharsSB.Append( cand[i] );
            string allCandChars = allCandCharsSB.ToString();

            StringBuilder sb = new StringBuilder();
            Random r = new Random();

            for ( int i = 0; i < digit; i++ ) {
                if ( i < candCnt ) {
                    sb.Append( cand[i].Substring( r.Next( 0, cand[i].Length - 1 ), 1 ) );
                }
                else {
                    sb.Append( allCandChars.Substring( r.Next( 0, allCandChars.Length - 1 ), 1 ) );
                }
            }

            InquiryItemValue.Text = new String( sb.ToString().OrderBy( i => r.Next( 0, 1073741824 ) ).ToArray() );
        }


    }
}
