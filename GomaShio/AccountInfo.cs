using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        int m_InquiryCount;
        private PasswordFile m_ParentPasswordFile;

        public AccountInfo( PasswordFile argPW )
        {
            m_AccountName = "New Account";
            m_InquiryName = new string[16];
            m_InquiryValue = new string[16];
            m_InquiryCount = 0;
            m_ParentPasswordFile = argPW;
        }

        public AccountInfo( PasswordFile argPW, AccountInfo cpSrc )
        {
            m_AccountName = cpSrc.m_AccountName;
            m_InquiryName = new string[ cpSrc.m_InquiryName.Length ];
            for ( int i = 0; i < m_InquiryName.Length; i++ )
                m_InquiryName[i] = cpSrc.m_InquiryName[i];
            m_InquiryValue = new string[ cpSrc.m_InquiryValue.Length ];
            for ( int i = 0; i < m_InquiryValue.Length; i++ )
                m_InquiryValue[i] = cpSrc.m_InquiryValue[i];
            m_InquiryCount = cpSrc.m_InquiryCount;
            m_ParentPasswordFile = argPW;
        }

        public string GetInquiryName( int idx ) {
            if ( idx < 0 || idx >= m_InquiryCount )
                return "";
            return m_InquiryName[ idx ];

        }
        public string GetInquiryValue( int idx ) {
            if ( idx < 0 || idx >= m_InquiryCount )
                return "";
            return m_InquiryValue[ idx ];
        }

        public int GetInquiryCount() {
            return m_InquiryCount;
        }

        public void SetInquiry( int idx, string name, string value ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            if ( name != m_InquiryName[ idx ] || value != m_InquiryValue[ idx ] )
                m_ParentPasswordFile.SetModified();
            m_InquiryName[ idx ] = name;
            m_InquiryValue[ idx ] = value;
        }

        public void SetInquiryValue( int idx, string value ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            if ( value == m_InquiryValue[ idx ] )
                return ;
            m_InquiryValue[ idx ] = value;
            m_ParentPasswordFile.SetModified();
        }

        public void InsertInquiry( int idx, string name, string value ) {
            if ( idx < 0 || m_InquiryCount < idx )
                return ;
            if ( m_InquiryName.Length <= m_InquiryCount ) {
                Array.Resize( ref m_InquiryName, m_InquiryName.Length * 2 );
                Array.Resize( ref m_InquiryValue, m_InquiryValue.Length * 2 );
            }
            for ( int i = m_InquiryCount; i > idx; i-- ) {
                m_InquiryName[ i ] = m_InquiryName[ i - 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i - 1 ];
            }
            m_InquiryCount++;
            m_InquiryName[ idx ] = name;
            m_InquiryValue[ idx ] = value;
            m_ParentPasswordFile.SetModified();
        }

        public void DeleteInquiry( int idx ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            for ( int i = idx; i < m_InquiryCount - 1; i++ ) {
                m_InquiryName[ i ] = m_InquiryName[ i + 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i + 1 ];
            }
            m_InquiryCount--;
            m_ParentPasswordFile.SetModified();
        }
/*
        public void UpTo( int idx ) {
            if ( idx <= 0 || m_InquiryCount <= idx )
                return ;
            string w = m_InquiryName[ idx - 1 ];
            m_InquiryName[ idx - 1 ] = m_InquiryName[ idx ];
            m_InquiryName[ idx ] = w;
            w = m_InquiryValue[ idx - 1 ];
            m_InquiryValue[ idx - 1 ] = m_InquiryValue[ idx ];
            m_InquiryValue[ idx ] = w;
            m_ParentPasswordFile.SetModified();
        }

        public void DownTo( int idx ) {
            if ( idx < 0 || m_InquiryCount - 1 <= idx )
                return ;
            string w = m_InquiryName[ idx + 1 ];
            m_InquiryName[ idx + 1 ] = m_InquiryName[ idx ];
            m_InquiryName[ idx ] = w;
            w = m_InquiryValue[ idx + 1 ];
            m_InquiryValue[ idx + 1 ] = m_InquiryValue[ idx ];
            m_InquiryValue[ idx ] = w;
            m_ParentPasswordFile.SetModified();
        }
*/
        public void Reorder( int[] vIdx )
        {
            if ( m_InquiryCount != vIdx.Length )
                return ;

            string[] vName = new string[ m_InquiryName.Length ];
            string[] vValue = new string[ m_InquiryValue.Length ];
            for ( int i = 0; i < m_InquiryCount; i++ ) {
                vName[i] = m_InquiryName[ vIdx[i] ];
                vValue[i] = m_InquiryValue[ vIdx[i] ];
            }
            m_InquiryName = vName;
            m_InquiryValue = vValue;
        }

        public string BuildStringForOutput()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "\n[" );
            sb.Append( m_AccountName );
            sb.Append( "]\n" );
            for ( int i = 0; i < m_InquiryCount; i++ ) {
                sb.Append( m_InquiryName[i] );
                sb.Append( "=" );
                sb.Append( m_InquiryValue[i] );
                sb.Append( "\n" );
            }
            return sb.ToString();
        }
    }
}
