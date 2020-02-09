using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace GomaShio
{
    public sealed partial class RecoveryBackupDialog : ContentDialog
    {
        private StorageFile [] m_oldBackupFiles;

        public RecoveryBackupDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            BackupFilesCombo.Items.Clear();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder backupFolder = await localFolder.CreateFolderAsync( "backups", CreationCollisionOption.OpenIfExists );
            var oldBackups = await backupFolder.GetFilesAsync( Windows.Storage.Search.CommonFileQuery.OrderByName );
            m_oldBackupFiles = new StorageFile[ oldBackups.Count ];

            // Add backup file names to combo box
            for ( int i = 0; i < oldBackups.Count; i++ ) {
                BackupFilesCombo.Items.Add( oldBackups[i].Name );
                m_oldBackupFiles[i] = oldBackups[i];
            }

            if ( BackupFilesCombo.Items.Count > 0 ) {
                BackupFilesCombo.SelectedIndex = 0;
                BackupFilesCombo.IsEnabled = true;
                RecoveryBackupGetButton.IsEnabled = true;
            }
            else {
                BackupFilesCombo.IsEnabled = false;
                RecoveryBackupGetButton.IsEnabled = false;
                GlbFunc.ShowMessage( "MSG_BUCKUP_FILE_NOT_EXIST", "There are no Backup files." );
            }
        }

        private async void RecoveryBackupGetButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // Get selected index
            int si = BackupFilesCombo.SelectedIndex;
            if ( si < 0 || si >= m_oldBackupFiles.Length ) return ;

            // Get output path name
            FileSavePicker p = new FileSavePicker();
            p.FileTypeChoices.Add( GlbFunc.GetResourceString( "GomaShioFileTypeDescription", "GomaShio Password File" ), new List<string>() { ".GomaShio" } );
            p.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFile targetFile = await p.PickSaveFileAsync();
            if ( null == targetFile ) return ;

            // copy file
            using ( var rs = await m_oldBackupFiles[si].OpenStreamForReadAsync().ConfigureAwait( true ) ) {
                using ( var ws = await targetFile.OpenStreamForWriteAsync().ConfigureAwait( true ) ) {
                    rs.CopyTo( ws );
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
        }
    }
}
