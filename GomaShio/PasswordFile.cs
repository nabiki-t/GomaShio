using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using System.Collections.Generic;
using System.Globalization;

namespace GomaShio
{
    internal class PasswordFile
    {
        private AccountInfo[] m_AccountInfo;
        private int m_Count;
        private bool m_Modified;

        public PasswordFile() {
            m_AccountInfo = new AccountInfo[16];
            m_Count = 0;
            m_Modified = false;
        }

        public void SetModified()
        {
            m_Modified = true;
        }

        public bool IsModified {
            get { return m_Modified; }
        }

        public int GetCount()
        {
            return m_Count;
        }

        public AccountInfo GetAccountInfo( int idx )
        {
            if ( idx < 0 || m_Count <= idx )
                return null;
            return m_AccountInfo[idx];
        }

        public void Insert( int idx )
        {
            if ( idx < 0 || m_Count < idx )
                return ;
            if ( m_AccountInfo.Length <= m_Count ) {
                Array.Resize( ref m_AccountInfo, m_AccountInfo.Length * 2 );
            }
            for ( int i = m_Count; i > idx; i-- )
                m_AccountInfo[i] = m_AccountInfo[ i - 1 ];
            m_AccountInfo[ idx ] = new AccountInfo( this );
            m_Count++;
            SetModified();
        }

        public void Copy( int idx )
        {
            if ( idx < 0 || m_Count < idx )
                return ;
            if ( m_AccountInfo.Length <= m_Count ) {
                Array.Resize( ref m_AccountInfo, m_AccountInfo.Length * 2 );
            }
            for ( int i = m_Count; i > idx; i-- )
                m_AccountInfo[i] = m_AccountInfo[ i - 1 ];
            m_AccountInfo[ idx ] = new AccountInfo( this, m_AccountInfo[ idx + 1 ] );
            m_Count++;
            SetModified();
        }

        public void Delete( int idx )
        {
            if ( idx < 0 || m_Count <= idx )
                return ;
            for ( int i = idx; i < m_Count - 1; i++ )
                m_AccountInfo[i] = m_AccountInfo[i+1];
            m_Count--;
            m_AccountInfo[m_Count] = null;
            SetModified();
        }

        public void Reorder( int[] vIdx )
        {
            if ( m_Count != vIdx.Length )
                return ;
            AccountInfo[] rv = new AccountInfo[ m_AccountInfo.Length ];
            for ( int i = 0; i < m_Count; i++ ) {
                rv[i] = m_AccountInfo[ vIdx[i] ];
            }
            m_AccountInfo = rv;
            SetModified();
        }

        // Read encrypted password file.
        // 0 : succeed
        // 1 : file read error
        // 2 : password error
        public async Task< int > Load( StorageFile file, string password )
        {
            // get password file size
            Windows.Storage.FileProperties.BasicProperties fProp = await file.GetBasicPropertiesAsync();
            ulong fileSize = fProp.Size;
            // If file is empty, loading is failed.
            if ( fileSize <= 0 ) return 1;

            // Read all of specified file.
            byte[] encriptedData = new byte[ fileSize ];
            using( IInputStream s = await file.OpenSequentialReadAsync() ) {
                using ( var r = new DataReader( s ) ) {
                    await r.LoadAsync( (uint)fileSize );
                    r.ReadBytes( encriptedData );
                }
            }

            bool passwordError = true;
            string plainData = "";

            // Decrypt read data.
            if ( !Crypt.CipherDecryption( encriptedData, password, out plainData, out passwordError ) ) {
                if ( passwordError )
                    return 2;
                else
                    return 1;
            }

            // Recognize loaded string to AoountInfo
            var result = Recognize( plainData );
            m_AccountInfo = result.Item1;
            m_Count = result.Item2;

            // Clear modified flag.
            m_Modified = false;

            return 0;

        }

        // Write encrypted password file.
        public async Task<bool> Save( StorageFile file, string password )
        {
            // Create backup folder
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder backupFolder = await localFolder.CreateFolderAsync( "backups", CreationCollisionOption.OpenIfExists );
            
            // Get old backup files list.
            IReadOnlyList<StorageFile> oldBackups = await backupFolder.GetFilesAsync( Windows.Storage.Search.CommonFileQuery.OrderByName );

            // Delete old backup files
            for ( int i = 0; i < oldBackups.Count - 15; i++ )
                await oldBackups[i].DeleteAsync();

            // Build plain text data for output and encript that string.
            byte[] encryptedData = Crypt.CipherEncryption( BuildStringForOutput(), password );

            // Start to create a new backup file
            string backupFileName = DateTime.Now.ToString( "yyyy-MM-dd-hh-mm-ss-fff", CultureInfo.InvariantCulture ) + ".GomaShio";
            StorageFile backupFile = await backupFolder.CreateFileAsync( backupFileName );

            // Write backup file
            await FileIO.WriteBufferAsync( backupFile, encryptedData.AsBuffer() );
            backupFile = null;

            // Write target file
            await FileIO.WriteBufferAsync( file, encryptedData.AsBuffer() );

            // Clear modified flag.
            m_Modified = false;

            return true;
        }

        // Import account information from plain text file.
        public bool Import( string str )
        {
            // Clear modified flag.
            m_Modified = false;
            var r = Recognize( str );
            m_AccountInfo = r.Item1;
            m_Count = r.Item2;
            return true;
        }

        // Recognize text string to account info
        public Tuple< AccountInfo[], int > Recognize( string str )
        {
            AccountInfo[] retval = new AccountInfo[16];
            int AccInfoCnt = 0;
            int curAccInfo = -1;
            Regex regSection = new Regex( @"^ *\[(.*)\] *$" );
            Regex regKeyVal = new Regex( @"^(([^=]|\\=)*)=(([^=]|\\=)*)$" );
            Regex regComment = new Regex( @"^ *;.*$" );
            Regex regEscapeEqual = new Regex( @"\\=" );
            string [] lines = str.Split( '\n' );

            for ( int i = 0; i < lines.Length; i++ ) {
                // Skip comment line
                if ( regComment.IsMatch( lines[i] ) )
                    continue;

                // If section name is specified, add a new AccountInfo object
                Match resSection = regSection.Match( lines[i] );
                if ( resSection.Success ) {
                    if ( retval.Length <= AccInfoCnt ) {
                        Array.Resize( ref retval, retval.Length * 2 );
                    }
                    curAccInfo = AccInfoCnt;
                    AccInfoCnt++;
                    retval[curAccInfo] = new AccountInfo( this );
                    retval[curAccInfo].AccountName = resSection.Groups[1].Value;
                    continue;
                }

                // If key-value line is specified, add a new inquiry info to current AccountInfo object;
                if ( curAccInfo < 0 ) continue;
                Match resInquiry = regKeyVal.Match( lines[i] );
                if ( resInquiry.Success ) {
                    retval[curAccInfo].InsertInquiry(
                        retval[curAccInfo].GetInquiryCount(),
                        regEscapeEqual.Replace( resInquiry.Groups[1].Value, "=" ),
                        regEscapeEqual.Replace( resInquiry.Groups[3].Value, "=" )
                    );
                }
            }
            return new Tuple<AccountInfo[], int>( retval, AccInfoCnt );
        }

        // Build string data for output file
        public string BuildStringForOutput()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "; GomaShio password file.\n" );
            for ( int i = 0; i < m_Count; i++ ) {
                sb.Append( m_AccountInfo[i].BuildStringForOutput() );
            }
            return sb.ToString();
        }
    }
}
