using System;
using System.Text;
using System.Security;
using System.Globalization;

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
        int m_InquiryCount;
        private PasswordFile m_ParentPasswordFile;

        public AccountInfo( PasswordFile argPW )
        {
            m_AccountName = "New Account";
            m_InquiryName = new string[16];
            m_InquiryValue = new string[16];
            m_HideFlag = new bool[16];
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
            m_HideFlag = new bool[ cpSrc.m_HideFlag.Length ];
            for ( int i = 0; i < m_HideFlag.Length; i++ )
                m_HideFlag[i] = cpSrc.m_HideFlag[i];
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
            m_InquiryValue[ idx ] = value;
            m_HideFlag[ idx ] = hideFlag;
        }

        public void InsertInquiry( int idx, string name, string value, bool hideFlag ) {
            if ( idx < 0 || m_InquiryCount < idx )
                return ;
            if ( m_InquiryName.Length <= m_InquiryCount ) {
                Array.Resize( ref m_InquiryName, m_InquiryName.Length * 2 );
                Array.Resize( ref m_InquiryValue, m_InquiryValue.Length * 2 );
                Array.Resize( ref m_HideFlag, m_HideFlag.Length * 2 );
            }
            for ( int i = m_InquiryCount; i > idx; i-- ) {
                m_InquiryName[ i ] = m_InquiryName[ i - 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i - 1 ];
                m_HideFlag[ i ] = m_HideFlag[ i - 1 ];
            }
            m_InquiryCount++;
            m_InquiryName[ idx ] = name;
            m_InquiryValue[ idx ] = value;
            m_HideFlag[ idx ] = hideFlag;
            m_ParentPasswordFile.SetModified();
        }

        public void DeleteInquiry( int idx ) {
            if ( idx < 0 || m_InquiryCount <= idx )
                return ;
            for ( int i = idx; i < m_InquiryCount - 1; i++ ) {
                m_InquiryName[ i ] = m_InquiryName[ i + 1 ];
                m_InquiryValue[ i ] = m_InquiryValue[ i + 1 ];
                m_HideFlag[ i ] = m_HideFlag[ i + 1 ];
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
            for ( int i = 0; i < m_InquiryCount; i++ ) {
                vName[i] = m_InquiryName[ vIdx[i] ];
                vValue[i] = m_InquiryValue[ vIdx[i] ];
                vHide[i] = m_HideFlag[ vIdx[i] ];
            }
            m_InquiryName = vName;
            m_InquiryValue = vValue;
            m_HideFlag = vHide;
        }

        public string BuildStringForOutput( int idx )
        {
            CultureInfo ci = new CultureInfo( "en-US" );

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( ci, "[Account_{0}]\n", idx );
            sb.AppendFormat( ci, "AccountName={0}\n", m_AccountName );
            sb.AppendFormat( ci, "InquiryCount={0}\n", m_InquiryCount );

            for ( int i = 0; i < m_InquiryCount; i++ ) {
                sb.AppendFormat( ci, "InquiryName_{0}={1}\n", i, m_InquiryName[i] );
                if ( m_HideFlag[i] )
                    sb.AppendFormat( ci, "HideFlag_{0}=True\n", i );
                else
                    sb.AppendFormat( ci, "HideFlag_{0}=False\n", i );
                sb.AppendFormat( ci, "InquiryValue_{0}={1}\n", i, m_InquiryValue[i] );
            }
            return sb.ToString();
        }
    }
}
