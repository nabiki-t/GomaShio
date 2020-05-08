using System;
using System.Text;
using System.Security;
using System.Globalization;
using System.Linq;

namespace GomaShio
{
    /// <summary>
    /// AccountInfo class.
    /// This class holes information of an account.
    /// </summary>
    class AccountInfo
    {
        private string m_AccountName;
        public string AccountName {
            get {
                return m_AccountName;
            }
            set {
                if ( m_AccountName != value ) {
                    m_ParentPasswordFile.SetModified();
                    m_AccountName = value;
                }
            }
        }
        private string [] m_InquiryName;
        private string [] m_InquiryValue;
        private bool [] m_HideFlag;
        private string [] m_obfusPass;
        int m_InquiryCount;
        private PasswordFile m_ParentPasswordFile;

        public AccountInfo( PasswordFile argPW )
        {
            m_AccountName = "New Account";
            m_InquiryName = new string[16];
            m_InquiryValue = new string[16];
            m_HideFlag = new bool[16];
            m_obfusPass = new string[16];
            m_InquiryCount = 0;
            m_ParentPasswordFile = argPW;
        }

        public AccountInfo( PasswordFile argPW, AccountInfo cpSrc )
        {
            m_AccountName = cpSrc.m_AccountName;
            m_InquiryName = new string[ cpSrc.m_InquiryName.Length ];
            m_InquiryValue = new string[ cpSrc.m_InquiryValue.Length ];
            m_HideFlag = new bool[ cpSrc.m_HideFlag.Length ];
            m_obfusPass = new string[ cpSrc.m_obfusPass.Length ];

            Array.Copy( cpSrc.m_InquiryName, m_InquiryName, cpSrc.m_InquiryName.Length );
            Array.Copy( cpSrc.m_InquiryValue, m_InquiryValue, cpSrc.m_InquiryValue.Length );
            Array.Copy( cpSrc.m_HideFlag, m_HideFlag, cpSrc.m_HideFlag.Length );
            Array.Copy( cpSrc.m_obfusPass, m_obfusPass, cpSrc.m_obfusPass.Length );

            m_InquiryCount = cpSrc.m_InquiryCount;
            m_ParentPasswordFile = argPW;
        }

        public string GetInquiryName( int idx ) {
            if ( idx < 0 || idx >= m_InquiryCount )
                return "";
            return m_InquiryName[ idx ];

        }
        public string GetInquiryValue( int idx ) {
            string rstr;
            bool err;
            if ( idx < 0 || idx >= m_InquiryCount )
                return "";
            if ( m_HideFlag[ idx ] ) {
                // If hide flag is on, the inquiry value is encrypted.
                if ( !Crypt.CipherDecryption( Crypt.Base64Decoding( m_InquiryValue[ idx ] ), m_obfusPass[ idx ], out rstr, out err, true ) )
                    return "";
                return rstr;
            }
            else {
                // If hide flag is off, the inquiry value is plain text.
                return m_InquiryValue[ idx ];
            }
        }
        public bool GetHideFlag( int idx ) {
            if ( idx < 0 || idx >= m_InquiryCount )
                return false;
            return m_HideFlag[ idx ];
        }

        public int GetInquiryCount() {
            return m_InquiryCount;
        }

        public void SetInquiry( int idx, string name, string value, bool hideFlag ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            if ( name != m_InquiryName[ idx ] || value != m_InquiryValue[ idx ] || hideFlag != m_HideFlag[ idx ] )
                m_ParentPasswordFile.SetModified();
            m_InquiryName[ idx ] = name;
            m_HideFlag[ idx ] = hideFlag;
            if ( hideFlag ) {
                // If hide flag is on, specified inquiry value is stored by encrypted string.
                m_obfusPass[ idx ] = GenObfusPass();
                m_InquiryValue[ idx ] = Crypt.Base64Encoding( Crypt.CipherEncryption( value, m_obfusPass[ idx ], true ) );
            }
            else {
                // If hide flag is off, specified inquiry value is stored by plain text.
                // And obfus pass string is not used.
                m_obfusPass[ idx ] = "";
                m_InquiryValue[ idx ] = value;
            }
        }

        public void InsertInquiryFromPlainValue( int idx, string name, string value, bool hideFlag ) {
            if ( hideFlag ) {
                // If hide flag is on, specified inquiry value is stored by encrypted string.
                string obfusStr = GenObfusPass();
                InsertInquiryValue( idx, name, Crypt.Base64Encoding( Crypt.CipherEncryption( value, obfusStr, true ) ), true, obfusStr );
            }
            else {
                // If hide flag is off, specified inquiry value is stored by plain text.
                // And obfus pass string is not used.
                InsertInquiryValue( idx, name, value, false, "" );
            }
        }

        public void InsertInquiryWithTempStr( int idx, string name, string value, string tempStr ) {
            // value is always encrypted by obfus pass string that is encoded by base64.
            InsertInquiryValue( idx, name, value, true, Crypt.Base64DecodingToStr( tempStr ) );
        }

        private void InsertInquiryValue( int idx, string name, string value, bool hideFlag, string obfusStr ) {
            if ( idx < 0 || m_InquiryCount < idx )
                return ;
            if ( m_InquiryName.Length <= m_InquiryCount ) {
                Array.Resize( ref m_InquiryName, m_InquiryName.Length * 2 );
                Array.Resize( ref m_InquiryValue, m_InquiryValue.Length * 2 );
                Array.Resize( ref m_HideFlag, m_HideFlag.Length * 2 );
                Array.Resize( ref m_obfusPass, m_obfusPass.Length * 2 );
            }
            for ( int i = m_InquiryCount; i > idx; i-- ) {
                m_InquiryName[ i ] = m_InquiryName[ i - 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i - 1 ];
                m_HideFlag[ i ] = m_HideFlag[ i - 1 ];
                m_obfusPass[ i ] = m_obfusPass[ i - 1 ];
            }
            m_InquiryCount++;
            m_InquiryName[ idx ] = name;
            m_InquiryValue[ idx ] = value;
            m_HideFlag[ idx ] = hideFlag;
            m_obfusPass[ idx ] = obfusStr;
            m_ParentPasswordFile.SetModified();
        }

        private static string GenObfusPass()
        {
            int digit = 16;
            Random r = new Random();
            StringBuilder sb = new StringBuilder();
            for ( int i = 0; i < digit; i++ ) {
                char c = (char)( r.Next( 1, 254 ) * 8 + r.Next( 1, 254 ) );
                sb.Append( c );
            }
            return new String( sb.ToString().OrderBy( i => r.Next( 0, 1073741824 ) ).ToArray() );
        }

        public void DeleteInquiry( int idx ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            for ( int i = idx; i < m_InquiryCount - 1; i++ ) {
                m_InquiryName[ i ] = m_InquiryName[ i + 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i + 1 ];
                m_HideFlag[ i ] = m_HideFlag[ i + 1 ];
                m_obfusPass[ i ] = m_obfusPass[ i + 1 ];
            }
            m_InquiryCount--;
            m_ParentPasswordFile.SetModified();
        }

        public void Reorder( int[] vIdx )
        {
            if ( m_InquiryCount != vIdx.Length )
                return ;

            string [] vName = new string[ m_InquiryName.Length ];
            string [] vValue = new string[ m_InquiryValue.Length ];
            bool [] vHide = new bool[ m_HideFlag.Length ];
            string [] vObfusPass = new string[ m_obfusPass.Length ];
            for ( int i = 0; i < m_InquiryCount; i++ ) {
                vName[i] = m_InquiryName[ vIdx[i] ];
                vValue[i] = m_InquiryValue[ vIdx[i] ];
                vHide[i] = m_HideFlag[ vIdx[i] ];
                vObfusPass[i] = m_obfusPass[ vIdx[i] ];
            }
            m_InquiryName = vName;
            m_InquiryValue = vValue;
            m_HideFlag = vHide;
            m_obfusPass = vObfusPass;
        }

        public string BuildStringForOutput( int idx, bool outputPlainFlg )
        {
            CultureInfo ci = new CultureInfo( "en-US" );

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( ci, "[Account_{0}]\n", idx );

            if ( outputPlainFlg ) {
                sb.AppendFormat( ci, "; {0}\n", m_AccountName );
                for ( int i = 0; i < m_InquiryCount; i++ ) {
                    sb.AppendFormat( ci, "; {0}\t:\t{1}\n", m_InquiryName[i], GetInquiryValue( i ) );
                }
                sb.Append( '\n' );
            }

            sb.AppendFormat( ci, "AccountName={0}\n", m_AccountName );
            sb.AppendFormat( ci, "InquiryCount={0}\n", m_InquiryCount );
            if ( outputPlainFlg )
                sb.Append( '\n' );

            for ( int i = 0; i < m_InquiryCount; i++ ) {
                sb.AppendFormat( ci, "InquiryName_{0}={1}\n", i, m_InquiryName[i] );
                if ( !m_HideFlag[i] ) {
                    // If hide flag is off, inquiry value is stored by plain text.
                    sb.AppendFormat( ci, "HideFlag_{0}=False\n", i );
                    sb.AppendFormat( ci, "InquiryValue_{0}={1}\n", i, m_InquiryValue[ i ] );
                }
                else {
                    sb.AppendFormat( ci, "HideFlag_{0}=True\n", i );
                    if ( outputPlainFlg ) {
                        // Oputput decrypted inquiry value
                        sb.AppendFormat( ci, "InquiryValue_{0}={1}\n", i, GetInquiryValue( i ) );
                    }
                    else {
                        // Output encrypted string and base64 encoded obfus pass string
                        sb.AppendFormat( ci, "InquiryValue_{0}={1}\n", i, m_InquiryValue[i] );
                        sb.AppendFormat( ci, "TempStr_{0}={1}\n", i, Crypt.Base64EncodingFromStr( m_obfusPass[i] ) );
                    }
                }
                if ( outputPlainFlg )
                    sb.Append( '\n' );
            }
            return sb.ToString();
        }
    }
}
