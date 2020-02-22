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
            UpdateWindowStat();
        }

        /// <summary>
        /// SelectExistingPasswordFileButton button was selected.
        /// </summary>
        private async void SelectExistingPasswordFileButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            FileOpenPicker p = new FileOpenPicker();
            p.FileTypeFilter.Add( ".GomaShio" );
            UpdateSelectedFileInfo( await p.PickSingleFileAsync() );
        }

        /// <summary>
        /// CreateNewPasswordFileButton button was selected.
        /// </summary>
        private async void CreateNewPasswordFileButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            FileSavePicker p = new FileSavePicker();
            p.FileTypeChoices.Add( GlbFunc.GetResourceString( "GomaShioFileTypeDescription", "GomaShio Password File" ), new List<string>() { ".GomaShio" } );
            p.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Get the password of the new file.
            SetNewPassDialog d = new SetNewPassDialog();
            await d.ShowAsync();
            if ( !d.IsOK ) return;
            string password = d.Password;
            bool savePasswordFlg = d.SavePassword;

            // Get output path name of a new GomaShio password file.
            StorageFile f = await p.PickSaveFileAsync();
            if ( null == f ) return ;

            // Create empty file
            PasswordFile nf = new PasswordFile();
            if ( !await nf.Save( f, password ).ConfigureAwait( true ) ) {
                GlbFunc.ShowMessage( "MSG_FAILED_CREATE_NEW_FILE", "It failed to create new password file, please fix cause of any error." );
                return ;
            }

            UpdateSelectedFileInfo( f );

            if ( d.SavePassword )
                ApplicationData.Current.LocalSettings.Values[ "Password" ] = password;
            else
                ApplicationData.Current.LocalSettings.Values.Remove( "Password" );
        }

        /// <summary>
        /// Update PassrowdFile file name and permission information.
        /// </summary>
        void UpdateSelectedFileInfo( StorageFile f ) {
            if ( null != f ) {
                SelectedFileNameTextBox.Text = f.Path;
                ApplicationData.Current.LocalSettings.Values[ "FileName" ] = f.Path;
                StorageApplicationPermissions.MostRecentlyUsedList.Clear();
                ApplicationData.Current.LocalSettings.Values[ "FileToken" ] = 
                    StorageApplicationPermissions.MostRecentlyUsedList.Add( f );
            }
            UpdateWindowStat();
        }

        void UpdateWindowStat()
        {
            string ftoken = "";
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "FileToken" ) )
                ftoken = (string)ApplicationData.Current.LocalSettings.Values[ "FileToken" ];
            SelectExistingPasswordFileButton.IsEnabled = true;
            CreateNewPasswordFileButton.IsEnabled = true;
            ExportPasswordFileButton.IsEnabled = !string.IsNullOrEmpty( ftoken );
            ImportPasswordFileButton.IsEnabled = !string.IsNullOrEmpty( ftoken );
            RecoveryFromBackupButton.IsEnabled = !string.IsNullOrEmpty( ftoken );

            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "FileName" ) )
                SelectedFileNameTextBox.Text = (String)ApplicationData.Current.LocalSettings.Values[ "FileName" ];
            else
                SelectedFileNameTextBox.Text = "";
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
            StorageFile stfile = r.Item2;
            string password = r.Item3;
            if ( string.IsNullOrEmpty( password ) || null == stfile || null == pwfile )
                return ;

            // Get output path name of a new plain text file
            FileSavePicker p = new FileSavePicker();
            p.FileTypeChoices.Add( GlbFunc.GetResourceString( "TextFileTypeDescription", "Text File" ), new List<string>() { ".txt" } );
            p.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFile txtFile = await p.PickSaveFileAsync();
            if ( null == txtFile ) return ;

            // Generate string for output
            string plainText = pwfile.BuildStringForOutput();

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
            StorageFile stfile = r.Item2;
            string password = r.Item3;
            if ( string.IsNullOrEmpty( password ) || null == stfile || null == pwfile )
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
            await pwfile.Save( stfile, password ).ConfigureAwait( true );
        }

        // Load password file
        private static async Task< Tuple< PasswordFile, StorageFile, string > > LoadSelectedPasswordFile()
        {
            // If password file is not selected, abort load file
            string ftoken = "";
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "FileToken" ) )
                ftoken = (string)ApplicationData.Current.LocalSettings.Values[ "FileToken" ];
            if ( string.IsNullOrEmpty( ftoken ) )
                return new Tuple< PasswordFile, StorageFile, string >( null, null, "" );
            StorageFile stFile = null;
            try {
                stFile = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync( ftoken );
            }
            catch ( FileNotFoundException ) { }
            catch ( UnauthorizedAccessException ) { }
            catch ( ArgumentException ) { };
            if ( null == stFile )
                return new Tuple< PasswordFile, StorageFile, string >( null, null, "" );

            // Get stored password
            string password = "";
            bool savePasswordFlg = ApplicationData.Current.LocalSettings.Values.ContainsKey( "Password" );
            if ( savePasswordFlg )
                password = (string)ApplicationData.Current.LocalSettings.Values[ "Password" ];

            PasswordFile pwFile = new PasswordFile();

            // If password is saved, load specified file.
            int loadResult = 2;
            if ( !string.IsNullOrEmpty( password ) ) {
                loadResult = await pwFile.Load( stFile, password ).ConfigureAwait( true );
                // If faile IO error is occured, return with failed.
                if ( 1 == loadResult ) {
                    GlbFunc.ShowMessage( "MSG_FAILED_LOAD_PASSWORD_FILE", "Failed to load password file" );
                    return new Tuple< PasswordFile, StorageFile, string >( null, null, "" );
                }
            }

            // Open password file with getting password string.
            PasswordDialog d = new PasswordDialog();
            d.IsFirstTime = true;
            while ( 2 == loadResult ) {
                // Get new password
                d.Password = password;
                d.SavePassword = savePasswordFlg;
                await d.ShowAsync();
                // If canceld, if failed to load.
                if ( !d.IsOK )
                    return new Tuple< PasswordFile, StorageFile, string >( null, null, "" );

                password = d.Password;
                savePasswordFlg = d.SavePassword;
                d.IsFirstTime = false;

                // load file
                loadResult = await pwFile.Load( stFile, password ).ConfigureAwait( true );
                switch( loadResult ) {
                case 0: // success
                        break;
                case 1: // file read error
                        GlbFunc.ShowMessage( "MSG_FAILED_LOAD_PASSWORD_FILE", "Failed to load password file" );
                        return new Tuple< PasswordFile, StorageFile, string >( null, null, "" );
                case 2: // password error
                        Task.Delay( TimeSpan.FromMilliseconds( 500 ) ).Wait();
                        break;
                }
            }

            // If specifyed to save password, write password to local storage.
            if ( savePasswordFlg )
                ApplicationData.Current.LocalSettings.Values[ "Password" ] = password;
            else
                ApplicationData.Current.LocalSettings.Values.Remove( "Password" );

            return new Tuple< PasswordFile, StorageFile, string >( pwFile, stFile, password );
        }

        private async void RecoveryFromBackupButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not selected, ignore this operation
            string ftoken = "";
            if ( ApplicationData.Current.LocalSettings.Values.ContainsKey( "FileToken" ) )
                ftoken = (string)ApplicationData.Current.LocalSettings.Values[ "FileToken" ];
            if ( string.IsNullOrEmpty( ftoken ) )
                return ;

            // Show recovery dialog
            await ( new RecoveryBackupDialog() ).ShowAsync();
        }
    }
}
