using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Security;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;

namespace GomaShio
{
    /// <summary>
    /// FileSettingsPage class
    /// </summary>
    public sealed partial class FileSettingsPage : Page
    {
        public FileSettingsPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;
            await UpdateWindowStat().ConfigureAwait( true );
        }

        /// <summary>
        /// CreateNewPasswordFileButton button was selected.
        /// </summary>
        private async void CreateNewPasswordFileButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            ResourceLoader r = ResourceLoader.GetForCurrentView();
            if ( await AppData.IsInitialized().ConfigureAwait( true ) ) {
                // Clear existing account information and set new password
           
                string titleStr = null;
                if ( null != r )
                    titleStr = r.GetString( "INIT_CONFIRM_MSG_TITLE" );
                if ( null == titleStr )
                    titleStr = "Confirmation";

                string msgStr = null;
                if ( null != r )
                   msgStr = r.GetString( "INIT_CONFIRM_MSG" );
                if ( null == msgStr )
                    msgStr = "All registered data will be deleted, including backup. Do you want to run it?";

                MessageDialog msgDlg = new MessageDialog( msgStr, titleStr );
                string okLabel = GlbFunc.GetResourceString( "CONFIRM_INITIALIZE_OK_MSG", "OK" );
                string cancelLabel = GlbFunc.GetResourceString( "CONFIRM_INITIALIZE_CANCEL_MSG", "Cancel" );
                msgDlg.Commands.Add( new UICommand( okLabel ) );
                msgDlg.Commands.Add( new UICommand( cancelLabel ) );
                var result = await msgDlg.ShowAsync();
                if ( result.Label != okLabel ) return ;

                // Get the password of the new file.
                SetNewPassDialog newPassDlg = new SetNewPassDialog( false, false );
                await newPassDlg.ShowAsync();
                if ( !newPassDlg.IsOK ) return;
                AppData.Initialize( newPassDlg.Password, newPassDlg.SavePassword );

            }
            else {
                // Specify new password and initialize data structure.

                // Get the password of the new file.
                SetNewPassDialog d = new SetNewPassDialog( false, false );
                await d.ShowAsync();
                if ( !d.IsOK ) return;
                AppData.Initialize( d.Password, d.SavePassword );
            }
            await UpdateWindowStat().ConfigureAwait( true );
        }

        private async void ChangePasswordButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Show password dialog at first time.
            SetNewPassDialog d = new SetNewPassDialog( true, false );
            d.Password = "";
            await d.ShowAsync();

            // If canceled, give up to change password.
            if ( !d.IsOK ) return;

            string currentPass = d.OldPassword;
            string newPass = d.Password;
            bool saveFlg = d.SavePassword;

            // Try to change password, and if failed, request new password again.
            while ( !AppData.ChangeUserPassword( currentPass, newPass ) ) {
                // Show dialog
                d = new SetNewPassDialog( true, true );
                d.Password = newPass;
                d.SavePassword = saveFlg;
                await d.ShowAsync();

                // If canceled, give up to change password.
                if ( !d.IsOK ) return;

                currentPass = d.OldPassword;
                newPass = d.Password;
                saveFlg = d.SavePassword;
            }

            if ( saveFlg ) {
                // If save password flag is set, save new password string.
                AppData.SaveUserPassword( newPass );
            }
            else {
                // If save password flag is not set, clear saved old password.
                AppData.ClearSavedUserPassword();
            }

        }

        async Task UpdateWindowStat()
        {
            bool initflg = await AppData.IsInitialized().ConfigureAwait( true );
            CreateNewPasswordFileButton.IsEnabled = true;
            ChangePasswordButton.IsEnabled = initflg;
            ExportPasswordFileButton.IsEnabled = initflg;
            ImportPasswordFileButton.IsEnabled = initflg;
            RecoveryFromBackupButton.IsEnabled = initflg;
        }

        /// <summary>
        /// Export password file button is clicked.
        /// </summary>
        private async void ExportPasswordFileButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Load selected password file. If not selected ignore this event
            var r = await LoadSelectedPasswordFile().ConfigureAwait( true );
            PasswordFile pwfile = r.Item1;
            string password = r.Item2;
            if ( string.IsNullOrEmpty( password ) || null == pwfile )
                return ;

            // Get output path name of a new plain text file
            FileSavePicker p = new FileSavePicker();
            p.FileTypeChoices.Add( GlbFunc.GetResourceString( "TextFileTypeDescription", "Text File" ), new List<string>() { ".txt" } );
            p.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFile txtFile = await p.PickSaveFileAsync();
            if ( null == txtFile ) return ;

            // Generate string for output
            string plainText = pwfile.BuildStringForOutput( true );

            // write plain text file
            await FileIO.WriteBufferAsync( txtFile, Encoding.GetEncoding( "UTF-8" ).GetBytes( plainText ).AsBuffer() );
        }

        /// <summary>
        /// Import password file button is clicked.
        /// </summary>
        private async void ImportPasswordFileButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Load selected password file. If not selected ignore this event
            var r = await LoadSelectedPasswordFile().ConfigureAwait( true );
            PasswordFile pwfile = r.Item1;
            string password = r.Item2;
            if ( string.IsNullOrEmpty( password ) || null == pwfile )
                return ;

            // get import source file.
            FileOpenPicker p = new FileOpenPicker();
            p.FileTypeFilter.Add( ".txt" );
            StorageFile txtFile = await p.PickSingleFileAsync();
            if ( null == txtFile ) return ;

            // read all of text from file
            string str = await FileIO.ReadTextAsync( txtFile );

            // Initialize password file object
            pwfile.Import( str );

            // Save new password file
            pwfile.Save( password );
        }

        // Load password file
        private static async Task< Tuple< PasswordFile, string > > LoadSelectedPasswordFile()
        {
            // Get stored password
            string savedPass = AppData.GetSavedUserPassword();
            string masterPass = "";
            bool passerror;
            bool isFirst = true;

            if ( !string.IsNullOrEmpty( savedPass ) ) {
                if ( !AppData.Authenticate( savedPass, out passerror, out masterPass ) ) {
                    if ( !passerror ) {
                        // Unexpected error
                        return new Tuple< PasswordFile, string >( null, "" );
                    }
                    masterPass = "";
                    isFirst = false;
                }
            }

            string d_userPass = "";
            bool d_savePassFlg = false;
            while( string.IsNullOrEmpty( masterPass ) ) {

                // Show password dialog
                PasswordDialog d = new PasswordDialog();
                d.Password = d_userPass;
                d.SavePassword = d_savePassFlg;                
                d.IsFirstTime = isFirst;
                await d.ShowAsync();

                // If canceled, failed to load file
                if ( !d.IsOK )
                    return new Tuple< PasswordFile, string >( null, "" );
                d_userPass = d.Password;
                d_savePassFlg = d.SavePassword;

                if ( !AppData.Authenticate( d_userPass, out passerror, out masterPass ) ) {
                    if ( !passerror ) {
                        // Unexpected error
                        return new Tuple< PasswordFile, string >( null, "" );
                    }
                    // Retry
                    masterPass = "";
                    isFirst = false;
                }
            }

            // If authenticated and save password flag is specified, save inputed password string
            if ( d_savePassFlg )
                AppData.SaveUserPassword( d_userPass );

            PasswordFile pwFile = new PasswordFile();
            bool loadResult = pwFile.Load( masterPass );
            if ( !loadResult ) {
                // Unexpected error
                return new Tuple< PasswordFile, string >( null, "" );
            }

            return new Tuple< PasswordFile, string >( pwFile, masterPass );
        }

        private async void RecoveryFromBackupButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Show recovery dialog
            await ( new RecoveryBackupDialog() ).ShowAsync();
        }

    }
}
