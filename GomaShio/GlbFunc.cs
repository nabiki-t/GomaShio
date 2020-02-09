using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

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

    }
}
