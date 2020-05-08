using System;
using System.Security;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Windows.UI.Popups;
using System.Threading.Tasks;

namespace GomaShio
{
    class GlbFunc
    {
        public static async Task ShowMessage( string resName, string defaultMsg )
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

            MessageDialog msgDlg = new MessageDialog( msgStr, titleStr );
            msgDlg.Commands.Add( new UICommand( "OK" ) );
            await msgDlg.ShowAsync();
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
    }
}
