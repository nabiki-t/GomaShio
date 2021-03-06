﻿using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace GomaShio
{
    internal class PasswordFile
    {
        private AccountInfo[] m_AccountInfo;
        private int m_Count;
        private bool m_Modified;
        //private SemaphoreSlim m_UpdateLock;

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
        public bool Load( string password )
        {
            // Get plain data
            string plainData = AppData.Load( password );
            if ( String.IsNullOrEmpty( plainData ) )
                return false;

            // Recognize loaded string to AoountInfo
            var result = Recognize( plainData, false );
            m_AccountInfo = result.Item1;
            m_Count = result.Item2;

            // Clear modified flag.
            m_Modified = false;

            return true;
        }

        // Write encrypted password file.
        public bool Save( string password )
        {
            // Save data
            bool result = AppData.Save( password, BuildStringForOutput( false ) );
            if ( !result )
                return false;

            // Clear modified flag.
            m_Modified = false;

            return true;
        }

        // Import account information from plain text file.
        public bool Import( string str )
        {
            // Clear modified flag.
            m_Modified = false;
            var r = Recognize( str, true );
            m_AccountInfo = r.Item1;
            m_Count = r.Item2;
            return true;
        }

        // Recognize text string to account info
        public Tuple< AccountInfo[], int > Recognize( string str, bool importPlainFlag )
        {
            CultureInfo ci = new CultureInfo( "en-US" );
            AccountInfo[] retval = new AccountInfo[16];
            int AccInfoCnt = 0;
            Regex regSection = new Regex( @"^ *\[([^]]*)\] *$" );
            Regex regKeyVal = new Regex( @"^ *([^=]*)=(.*)$" );
            Regex regComment = new Regex( @"^ *;.*$" );

            // Divite single string to lines by \n
            string [] lines = str.Split( '\n' );

            Dictionary< string, Dictionary< string, string > > divValue = new Dictionary< string, Dictionary< string, string > >();
            Dictionary< string, string > currentSection = null;

            // Devite lines into sections and key-values
            for ( int i = 0; i < lines.Length; i++ ) {
                // Skip comment line
                if ( lines[i].Length == 0 || regComment.IsMatch( lines[i] ) )
                    continue;

                // If section name is specified, add a new section object
                Match resSection = regSection.Match( lines[i] );
                if ( resSection.Success ) {
                    if ( !divValue.ContainsKey( resSection.Groups[1].Value ) ) {
                        // Add new section
                        currentSection = new Dictionary< string, string >();
                        divValue.Add( resSection.Groups[1].Value, currentSection );
                    }
                    else {
                        // If section name is duplicated, ignore this section
                        currentSection = null;
                    }
                    continue;
                }

                // If section is not specified, ignore lines other than section name.
                if ( null == currentSection ) continue;

                // If key-value line is specified, add a new key-value info to current section dictionary object
                Match resInquiry = regKeyVal.Match( lines[i] );
                if ( resInquiry.Success ) {
                    if ( !currentSection.ContainsKey( resInquiry.Groups[1].Value ) ) {
                        currentSection.Add( resInquiry.Groups[1].Value, resInquiry.Groups[2].Value );
                    }
                    else {
                        // If key name is duplicated, ignore this key-value
                    }
                    continue;
                }

                // ignore any other lines
            }

            // Get Account count
            if ( divValue.ContainsKey( "General" ) ) {
                currentSection = divValue.GetValueOrDefault( "General", null );
                if ( !Int32.TryParse( currentSection.GetValueOrDefault( "AccountCount", "0" ), out AccInfoCnt ) )
                    AccInfoCnt = 0;
            }
            if ( AccInfoCnt < 0 ) AccInfoCnt = 0;

            int curAccInfo = 0;
            retval = new AccountInfo[ Math.Max( 16, AccInfoCnt ) ];
            for ( int i = 0; i < AccInfoCnt; i++ ) {
                string AccSecName = String.Format( ci, "Account_{0}", i );

                currentSection = null;
                if ( divValue.ContainsKey( AccSecName ) )
                    currentSection = divValue.GetValueOrDefault( AccSecName, null );
                if ( null == currentSection )
                    continue;

                // Get account name
                string accountName = currentSection.GetValueOrDefault( "AccountName", null );
                if ( null == accountName )
                    continue;

                // Get inquiry count
                int inquiryCount = 0;
                if ( !Int32.TryParse( currentSection.GetValueOrDefault( "InquiryCount", "-1" ), out inquiryCount ) )
                    inquiryCount = -1;
                if ( inquiryCount < 0 )
                    continue;

                // Create AccountInfo object and load inquiry names and values
                retval[curAccInfo] = new AccountInfo( this );
                retval[curAccInfo].AccountName = accountName;
                for ( int j = 0; j < inquiryCount; j++ ) {
                    string inquiryName = currentSection.GetValueOrDefault( String.Format( ci, "InquiryName_{0}", j ), null );
                    string inquiryValue = currentSection.GetValueOrDefault( String.Format( ci, "InquiryValue_{0}", j ), null );
                    string hideFlagStr = currentSection.GetValueOrDefault( String.Format( ci, "HideFlag_{0}", j ), null );
                    if ( null == inquiryName || null == inquiryValue || null == hideFlagStr )
                        continue;
                    bool hideFlag = String.Compare( hideFlagStr, "True", true, ci ) == 0;
                    if ( importPlainFlag ) {
                        // If it is importing plain text file, all values is written in plain text.
                        // There are not TempStr_*** values and hideFlag values is set true or false.
                        retval[curAccInfo].InsertInquiryFromPlainValue( retval[curAccInfo].GetInquiryCount(), inquiryName, inquiryValue, hideFlag );
                    }
                    else {
                        // If it is not importing plain text file, hidden value is encrypted by TempStr_*** value.
                        if ( hideFlag ) {
                            // If hide flag is set true, there must be TempStr_*** value.
                            string tempStr = currentSection.GetValueOrDefault( String.Format( ci, "TempStr_{0}", j ), null );
                            if ( null == tempStr ) continue;
                            retval[curAccInfo].InsertInquiryWithTempStr( retval[curAccInfo].GetInquiryCount(), inquiryName, inquiryValue, tempStr );
                        }
                        else {
                            // If hide flag is set to false, value is not encrypted.
                            retval[curAccInfo].InsertInquiryFromPlainValue( retval[curAccInfo].GetInquiryCount(), inquiryName, inquiryValue, false );
                        }
                    }
                }
                curAccInfo++;
            }

            return new Tuple<AccountInfo[], int>( retval, curAccInfo );
        }

        // Build string data for output file
        public string BuildStringForOutput( bool outputPlainFlg )
        {
            CultureInfo ci = new CultureInfo( "en-US" );
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( ci, "; GomaShio password file. {0}\n\n", DateTime.Now.ToString( ci ) );
            sb.Append( "[General]\n" );
            sb.AppendFormat( ci, "AccountCount={0}\n\n", m_Count );
            for ( int i = 0; i < m_Count; i++ ) {
                if ( outputPlainFlg )
                    sb.Append( "\n; ----------------------------------------------------\n" );
                sb.Append( m_AccountInfo[i].BuildStringForOutput( i, outputPlainFlg ) );
            }
            return sb.ToString();
        }
    }
}
