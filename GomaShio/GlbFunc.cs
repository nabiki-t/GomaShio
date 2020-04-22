using System;
using System.Security;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using System.Runtime.InteropServices; 

namespace GomaShio
{
    class GlbFunc
    {
        public static async void ShowMessage( string resName, string defaultMsg )
        {
            ResourceLoader r = ResourceLoader.GetForCurrentView();
           
            string titleStr = null;
            if ( null != r )
                titleStr = r.GetString( "AppMessageBoxTitle" );
            if ( null == titleStr )
                titleStr = "GomaShio";

            string msgStr = null;
            if ( null != resName ) {
                if ( null != r )
                    msgStr = r.GetString( resName );
            }
            if ( null == msgStr )
                msgStr = defaultMsg;
            if ( null == msgStr )
                msgStr = "";

            await new ContentDialog {
                Title = titleStr,
                Content = msgStr,
                PrimaryButtonText = "OK"
            }.ShowAsync();
        }

        public static string GetResourceString( string resName, string defaultMsg )
        {
            if ( null == resName )
                return defaultMsg;
            ResourceLoader r = ResourceLoader.GetForCurrentView();
            if ( null == r )
                return defaultMsg;
            string result = r.GetString( resName );
            if ( null == result )
                return defaultMsg;
            return result;
        }
        /*
        public static void AppendStringToSecureString( String src, ref SecureString dst )
        {
            foreach ( char c in src )
                dst.AppendChar( c );
        }

        public static void AppendSecureString( SecureString src, ref SecureString dst )
        {
            unsafe {
                IntPtr secBuf = Marshal.SecureStringToGlobalAllocUnicode( src );
                char *pSecureString = (char*)secBuf;
                for ( int i = 0; i < src.Length; i++ ) {
                    dst.AppendChar( pSecureString[i] );
                }
                Marshal.ZeroFreeGlobalAllocUnicode( secBuf );
            }
        }

        public static string SecureStringToString( SecureString src )
        {
            if ( null == src ) return null;
            IntPtr pSecureString = Marshal.SecureStringToGlobalAllocUnicode( src );
            string rstr = Marshal.PtrToStringUni( pSecureString );
            Marshal.ZeroFreeGlobalAllocUnicode( pSecureString );
            return rstr;
        }

        public static SecureString StringToSecureString( string str ) 
        {
            if ( null == str ) return null;
            SecureString r = new SecureString();
            foreach ( char c in str )
                r.AppendChar( c );
            return r;
        }*/
    }
}
