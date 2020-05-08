using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace GomaShio
{
    static class AppData
    {
        static string MASTERPASS_KEYNAME = "P7k3iGDGLvT7BtRc";
        static string USERPASS_KEYNAME = "h27seRYM5LHBufu4";
        static string USERPASS_ENC_FIXEDPASS = "KBECXSQ4j]k[v2NvMVtmeZTKcX{8Vusj";
        static string CURRENT_DATA_KEYNAME = "VFahdeeE3h5NBKp5";
        static int MAX_BACKUP_COUNT = 16;

        // Get GomaShio user password is specified or not.
        // Is this function returns false, it is needed setting master password and initialize relative data structure.
        public async static Task< bool > IsInitialized()
        {
            // If master password is not registered, it may be not initialized.
            if ( !ApplicationData.Current.LocalSettings.Values.ContainsKey( MASTERPASS_KEYNAME ) )
                return false;
            string masterPass = (string)ApplicationData.Current.LocalSettings.Values[ MASTERPASS_KEYNAME ];
            if ( String.IsNullOrEmpty( masterPass ) )
                return false;

            // If current file is not exist, it may be not initialized.
            try {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile currentFile = await localFolder.GetFileAsync( CURRENT_DATA_KEYNAME );
                return true;
            }
            catch ( FileNotFoundException e ) {
                _ = e;
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
            }
            return false;
        }

        // Specify user pasword and initialize relative data structure.
        public static bool Initialize( string userPass, bool saveUserPass )
        {
            // Clear saved old data
            ApplicationData.Current.LocalSettings.Values.Clear();

            // Create new master password string
            Random r = new Random();
            StringBuilder masterPass = new StringBuilder();
            for ( int i = 0; i < 64; i++ ) {
                char c = (char)( r.Next( 1, 254 ) * 8 + r.Next( 1, 254 ) );
                masterPass.Append( c );
            }
            string masterPassStr = masterPass.ToString();

            // Save master password string
            ApplicationData.Current.LocalSettings.Values[ MASTERPASS_KEYNAME ] =
                Crypt.Base64Encoding( Crypt.CipherEncryption( masterPassStr, userPass, false ) );

            // if user password saving is specified, encrypt user password by fixed string
            if ( saveUserPass )
                SaveUserPassword( userPass );

            // Create empty current file.
            PasswordFile pf = new PasswordFile();
            return Save( masterPassStr, pf.BuildStringForOutput( false ) );
        }

        // Get saved user password
        public static string GetSavedUserPassword()
        {
            // If user password is not saved, it returns an empty string
            if ( !ApplicationData.Current.LocalSettings.Values.ContainsKey( USERPASS_KEYNAME ) )
                return "";

            // Get encrypted user password
            string encryptedUserPass = (string)ApplicationData.Current.LocalSettings.Values[ USERPASS_KEYNAME ];
            if ( String.IsNullOrEmpty( encryptedUserPass ) )
                return "";

            // Decrypt user password. If en error is occurred, returns an empty string.
            string plainUserPass;
            bool passerr;
            if ( !Crypt.CipherDecryption( Crypt.Base64Decoding( encryptedUserPass ), USERPASS_ENC_FIXEDPASS, out plainUserPass, out passerr, false ) )
                return "";
            return plainUserPass;
        }

        // Save user password string
        public static void SaveUserPassword( string userPass )
        {
            ApplicationData.Current.LocalSettings.Values[ USERPASS_KEYNAME ] =
                Crypt.Base64Encoding( Crypt.CipherEncryption( userPass, USERPASS_ENC_FIXEDPASS, false ) );
        }

        // Clear saved user password
        public static void ClearSavedUserPassword()
        {
            ApplicationData.Current.LocalSettings.Values.Remove( USERPASS_KEYNAME );
        }

        // Authenticate by user password and get master password.
        public static bool Authenticate( string userPass, out bool passerr, out string masterPass )
        {
            passerr = false;
            masterPass = "";

            // If not initialized, authentication is failed.
            if ( !ApplicationData.Current.LocalSettings.Values.ContainsKey( MASTERPASS_KEYNAME ) )
                return false;

            // decrypt master password string
            string encryptedMasterPass = (string)ApplicationData.Current.LocalSettings.Values[ MASTERPASS_KEYNAME ];
            if ( String.IsNullOrEmpty( encryptedMasterPass ) )
                return false;

            return Crypt.CipherDecryption( Crypt.Base64Decoding( encryptedMasterPass ), userPass, out masterPass, out passerr, false );
        }

        public static string Load( string masterPass )
        {
            byte[] encriptedData = null;
            try {
                // Get current used password file
                DirectoryInfo di = new DirectoryInfo( ApplicationData.Current.LocalFolder.Path );
                FileInfo [] fsi = di.GetFiles( CURRENT_DATA_KEYNAME, SearchOption.TopDirectoryOnly );

                // If file is missing, failed to load.
                if ( fsi.Length <= 0 ) return "";

                // If file is empty, failed to load.
                long fileSize = fsi[0].Length;
                if ( fileSize <= 0 ) return "";

                // Read all of file data
                encriptedData = new byte[ fileSize ];
                using( FileStream fs = fsi[0].OpenRead() ) {
                    fs.Read( encriptedData, 0, (int)fileSize );
                    fs.Close();
                }
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
                return "";
            }
            catch ( IOException e ) {
                _ = e;
                return "";
            }
            catch ( SecurityException e ) {
                _ = e;
                return "";
            }

            // decrypt read data. 
            string plainDataString;
            bool passerr;
            if ( !Crypt.CipherDecryption( encriptedData, masterPass, out plainDataString, out passerr, false ) )
                return "";
            return plainDataString;

        }

        //public async static Task<bool> Save( string masterPass, string optStr )
        public static bool Save( string masterPass, string optStr )
        {
            // Encrypt output string by master password
            byte[] encryptedData = Crypt.CipherEncryption( optStr, masterPass, false );
            DirectoryInfo localFolder = new DirectoryInfo( ApplicationData.Current.LocalFolder.Path );

            try {
                // Create backup folder
                DirectoryInfo backupFolder = localFolder.CreateSubdirectory( "backups" );
                FileInfo [] oldBackupFiles = backupFolder.GetFiles( "*", SearchOption.TopDirectoryOnly );

                // Delete old backup files
                int i = 0;
                foreach( FileInfo fi in oldBackupFiles.OrderByDescending( fi => fi.Name ) ) {
                    if ( i >= MAX_BACKUP_COUNT ) fi.Delete();
                    i++;
                }

                // create a new backup file
                string backupFileName = DateTime.Now.ToString( "yyyy-MM-dd", CultureInfo.InvariantCulture );
                FileInfo backupFileInfo = new FileInfo( backupFolder.FullName + "\\" + backupFileName );
                using( FileStream fs = backupFileInfo.Create() ) {
                    fs.Write( encryptedData, 0, encryptedData.Length );
                }
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
            }
            catch ( IOException e ) {
                _ = e;
            }
            catch ( SecurityException e ) {
                _ = e;
            }

            try {
                // output for current used file
                FileInfo currentFileInfo = new FileInfo( localFolder.FullName + "\\" + CURRENT_DATA_KEYNAME );
                using( FileStream fs = currentFileInfo.Create() ) {
                    fs.Write( encryptedData, 0, encryptedData.Length );
                }

                return true;
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
            }
            catch ( IOException e ) {
                _ = e;
            }
            catch ( SecurityException e ) {
                _ = e;
            }
            return false;
        }

        public static string[] GetBackupDate()
        {
            DirectoryInfo localFolder = new DirectoryInfo( ApplicationData.Current.LocalFolder.Path );
            try {
                // Create backup folder
                DirectoryInfo backupFolder = localFolder.CreateSubdirectory( "backups" );

                // Get backup files list.
                FileInfo [] oldBackupFiles = backupFolder.GetFiles( "*", SearchOption.TopDirectoryOnly );
                string [] rv = new string[ oldBackupFiles.Length ];

                int i = 0;
                foreach( FileInfo fi in oldBackupFiles.OrderByDescending( fi => fi.Name ) ) {
                    rv[i] = fi.Name;
                    i++;
                }
                return rv;
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
            }
            catch ( IOException e ) {
                _ = e;
            }
            catch ( SecurityException e ) {
                _ = e;
            }
            return Array.Empty<string>();
        }

        public static bool RecoveryFromBackup( string bkDate )
        {
            DirectoryInfo localFolder = new DirectoryInfo( ApplicationData.Current.LocalFolder.Path );
            try {
                // Create backup folder
                DirectoryInfo backupFolder = localFolder.CreateSubdirectory( "backups" );

                // Get backup files list.
                FileInfo [] oldBackupFiles = backupFolder.GetFiles( bkDate, SearchOption.TopDirectoryOnly );
                if ( oldBackupFiles.Length <= 0 ) return false;

                // Delete current used file
                FileInfo [] currentFiles = localFolder.GetFiles( CURRENT_DATA_KEYNAME, SearchOption.TopDirectoryOnly );
                if ( currentFiles.Length > 0 )
                    currentFiles[0].Delete();

                // Move backup file to current used file.
                oldBackupFiles[0].MoveTo( ApplicationData.Current.LocalFolder.Path + "\\" + CURRENT_DATA_KEYNAME );
                return true;
            }
            catch ( UnauthorizedAccessException e ) {
                _ = e;
            }
            catch ( IOException e ) {
                _ = e;
            }
            catch ( SecurityException e ) {
                _ = e;
            }
            return false;
        }

        public static bool ChangeUserPassword( string currentPass, string newPass )
        {
            // If not initialized, authentication is failed.
            if ( !ApplicationData.Current.LocalSettings.Values.ContainsKey( MASTERPASS_KEYNAME ) )
                return false;

            // decrypt master password string
            string encryptedMasterPass = (string)ApplicationData.Current.LocalSettings.Values[ MASTERPASS_KEYNAME ];
            if ( String.IsNullOrEmpty( encryptedMasterPass ) )
                return false;

            string masterPass;
            bool passerr;
            if ( !Crypt.CipherDecryption( Crypt.Base64Decoding( encryptedMasterPass ), currentPass, out masterPass, out passerr, false ) )
                return false;

            // encrypt master password by new user password
            ApplicationData.Current.LocalSettings.Values[ MASTERPASS_KEYNAME ] =
                Crypt.Base64Encoding( Crypt.CipherEncryption( masterPass, newPass, false ) );
            return true;
        }
    }
}
