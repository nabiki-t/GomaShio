using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GomaShio
{
    public sealed partial class RecoveryBackupDialog : ContentDialog
    {
        public RecoveryBackupDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;
            RecoveryBackupResultText.Text = "";
            await UpdateBackupFilesList().ConfigureAwait( true );
        }

        private async Task UpdateBackupFilesList()
        {
            BackupFilesCombo.Items.Clear();

            // Get backuped date
            string [] bkDate = AppData.GetBackupDate();

            if ( bkDate.Length <= 0 ) {
                BackupFilesCombo.IsEnabled = false;
                RecoveryBackupGetButton.IsEnabled = false;
                await GlbFunc.ShowMessage( "MSG_BUCKUP_FILE_NOT_EXIST", "There are no Backup files." ).ConfigureAwait( true );
                return ;
            }
            else {
                BackupFilesCombo.SelectedIndex = 0;
                BackupFilesCombo.IsEnabled = true;
                RecoveryBackupGetButton.IsEnabled = true;
            }

            // Add backup file names to combo box
            for ( int i = 0; i < bkDate.Length; i++ ) {
                BackupFilesCombo.Items.Add( bkDate[i] );
            }
        }

        private async void RecoveryBackupGetButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            CultureInfo ci = new CultureInfo( "en-US" );
            string resultMsg = "";
            string bkfname = (string)BackupFilesCombo.SelectedValue;
            if ( string.IsNullOrEmpty( bkfname ) ) return ;

            // Recovery
            if ( AppData.RecoveryFromBackup( bkfname ) )
                resultMsg = GlbFunc.GetResourceString( "RECOVERY_BACKUP_RESULT_SUCCEED", "Succeed recovery from backup {0}" );
            else
                resultMsg = GlbFunc.GetResourceString( "RECOVERY_BACKUP_RESULT_FAILED", "Failed to recovery from backup {0}" );
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( ci, resultMsg, bkfname );
            RecoveryBackupResultText.Text = sb.ToString();

            // Update Conbobox
            await UpdateBackupFilesList().ConfigureAwait( true );
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            _ = sender;
            _ = args;
        }
    }
}
