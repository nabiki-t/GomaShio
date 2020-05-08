using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using System.IO;
using System.Collections.Generic;

namespace GomaShio
{
    /// <summary>
    /// PasswordListPage class
    /// </summary>
    public sealed partial class PasswordListPage : Page
    {
        /// Loaded accounts information. If it failed to loads the password file, null is set in this field.
        PasswordFile m_PasswordFile;

        /// Password string of loaded password file. Password is encripted and stored in this field.
        string m_Password;

        /// Reference of XAML text block object that holds account name and is in account list object.
        TextBlock [] m_AccountList_ItemNames;

        /// Reference of XAML border object that is in account list object.
        Border [] m_AccountList_Border;

        /// Count number of objects in m_AccountList_ItemNames and m_AccountList_Border
        int m_AccountList_ItemCount;

        /// Reference of XAML text block object that holds inquiry name and is in inquiry list object.
        TextBlock [] m_InquiryList_ItemTitles;

        /// Reference of XAML text block object that holds inquiry value and is in inquiry list object.
        TextBlock [] m_InquiryList_ItemTexts;

        /// Reference of XAML button object that used in copy action and is in inquiry list object.
        Button [] m_InquiryList_CopyButton;

        /// Event handler set to copy button in inquiry list.
        RoutedEventHandler [] m_InquiryList_CopyButton_Click;

        /// Reference of XAML border object that is in inquiry list object.
        Border [] m_InquiryList_Border;

        /// Count number of objects in m_InquiryList_ItemTitles, m_InquiryList_ItemTexts,
        /// m_InquiryList_CopyButton, m_InquiryList_CopyButton_Click and m_InquiryList_Border.
        int m_InquiryList_ItemCount;

        /// selected item index number of account list object.
        /// If not selected, -1 is set in this lield.
        int m_SelectedAccount;

        /// Flag field that is set true if account name text block value will updated by program.
        /// It used to ignore tha event in handler function.
        bool m_AccountNameTextUpdateFlg;

        /// Flag field that if set true if item selection of account list will be updated by program.
        /// It used to ignore tha event in handler function.
        bool m_AccountListSelectionFlg;

        /// Status of editable mode or not.
        bool m_EnableEdit;

        /// Timer object of file saving
        private DispatcherTimer m_SaveTimer;

        /// Last edit date time
        private DateTime m_LastEditDate;

        private static string MODIFIED_LABEL_TEXT;
        private static string SAVED_LABEL_TEXT;

        /// <summary>
        /// Constructor of PasswordListPage class object.
        /// Initialize member fields.
        /// </summary>
        public PasswordListPage()
        {
            m_AccountList_ItemNames = new TextBlock[16];
            m_AccountList_Border = new Border[16];
            m_AccountList_ItemCount = 0;
            m_InquiryList_ItemTitles = new TextBlock[16];
            m_InquiryList_ItemTexts = new TextBlock[16];
            m_InquiryList_CopyButton = new Button[16];
            m_InquiryList_CopyButton_Click = new RoutedEventHandler[16];
            m_InquiryList_Border = new Border[16];
            m_InquiryList_ItemCount = 0;
            m_SelectedAccount = -1;
            m_AccountNameTextUpdateFlg = false;
            m_AccountListSelectionFlg = false;
            m_EnableEdit = false;
            m_SaveTimer = null;
            this.InitializeComponent();
        }

        private async void Page_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            m_SaveTimer = new DispatcherTimer();
            m_SaveTimer.Interval = TimeSpan.FromMilliseconds( 1000 );
            m_SaveTimer.Tick += OnSaveTimer;
            m_SaveTimer.Start();
            m_LastEditDate = DateTime.UtcNow;

            MODIFIED_LABEL_TEXT = GlbFunc.GetResourceString( "EditEnableToggle_ModifiedText", "Ediable *" );
            SAVED_LABEL_TEXT = GlbFunc.GetResourceString( "EditEnableToggle_SavedText", "Ediable" );
            EditEnableToggle.OnContent = SAVED_LABEL_TEXT;

            // all of controls are disabled.
            AccountList.Items.Clear();
            InquiryList.Items.Clear();
            m_AccountList_ItemCount = 0;
            SetEdiableState( false );

            // load password file
            if ( !await LoadPasswordFile().ConfigureAwait( true ) ) {
                // If failed to load password file, all of controls are remain disabled.
                m_PasswordFile = null;
                EditEnableToggle.IsEnabled = false;
                return ;
            }
            AccountList.Items.Clear();
            AccountList.IsEnabled = true;
            m_AccountList_ItemCount = 0;

            // Insert loaded account information to list
            UpdateAccountList();

            if ( m_PasswordFile.GetCount() > 0 ) {
                m_AccountListSelectionFlg = true;
                AccountList.SelectedIndex = 0;
                m_AccountListSelectionFlg = false;
                UpdateInquiryList( 0 );
            }
            else {
                UpdateInquiryList( -1 );
            }

            // If account info is empty, default is editable. Others, default is read-only.
            EditEnableToggle.IsOn = ( m_PasswordFile.GetCount() == 0 );

            Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);
        }

        private void Page_Unloaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            Application.Current.Suspending -= new SuspendingEventHandler(App_Suspending);

            if ( null != m_SaveTimer ) {
                m_SaveTimer.Stop();
                m_SaveTimer.Tick -= OnSaveTimer;
                m_SaveTimer = null;
            }

            if ( null == m_PasswordFile )
                return ;
            SaveAccountFile();
        }

        // On close application
        private void App_Suspending( object sender, object e )
        {
            SaveAccountFile();
        }

        // File saving timer event
        private void OnSaveTimer( object sender, object e )
        {
            _ = sender;
            _ = e;

            // If it is not ediable mode, ignore this event
            if ( !m_EnableEdit )
                return ;

            TimeSpan s = DateTime.UtcNow - m_LastEditDate;

            if ( s.TotalMilliseconds < 0 ) {
                SaveAccountFile();
                return ;
            }

            // If more than 3 seconds have not passed since the last change, wait a little longer.
            if ( s.TotalMilliseconds < 2500 )
                return ;

            // save file
            SaveAccountFile();
        }

        //private async void SaveAccountFile()
        private void SaveAccountFile()
        {
            // If password file is not loaded, there are not to save data.
            if ( null == m_PasswordFile ) return ;

            // If current password file is not modified, skip save file.
            if ( !m_PasswordFile.IsModified )
                return ;

            // save
            m_PasswordFile.Save( m_Password );

            // Update last editing time
            m_LastEditDate = DateTime.UtcNow;

            EditEnableToggle.OnContent = SAVED_LABEL_TEXT;
        }

        private void UpdateAccountList()
        {
            // If password file is not loaded, ignore this operation
            if ( null == m_PasswordFile ) return ;

            // Get current account info count of password file.
            int accountCnt = m_PasswordFile.GetCount();

            if ( m_AccountList_ItemNames.Length <= accountCnt )
                 Array.Resize( ref m_AccountList_ItemNames, Math.Max( m_AccountList_ItemNames.Length * 2, accountCnt + 1 ) );
            if ( m_AccountList_Border.Length <= accountCnt )
                 Array.Resize( ref m_AccountList_Border, Math.Max( m_AccountList_Border.Length * 2, accountCnt + 1 ) );

            for ( int i = 0; i < m_AccountList_ItemCount && i < accountCnt; i++ ) {
                // update Inquiry Title and inquily text in existing control
                m_AccountList_ItemNames[i].Text = m_PasswordFile.GetAccountInfo( i ).AccountName;
                m_AccountList_Border[i].Background = new SolidColorBrush( CalcBorderColor( accountCnt, i, EditEnableToggle.IsOn ) );
            }

            if ( m_AccountList_ItemCount < accountCnt ) {
                // Create and add new list items.

                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo( "en-US" );
                for ( int i = m_AccountList_ItemCount; i < accountCnt; i++ ) {
                    Grid itemGrid = new Grid {
                        Height = 30,
                        Margin = new Thickness( 0, 3, 3, 3 ),
                        Name = i.ToString( ci )
                    };
                    itemGrid.ColumnDefinitions.Add( new ColumnDefinition{ Width = new GridLength( 3.0 ) } );
                    itemGrid.ColumnDefinitions.Add( new ColumnDefinition() );
                    m_AccountList_ItemNames[i] = new TextBlock {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Text = m_PasswordFile.GetAccountInfo( i ).AccountName,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness( 5, 0, 0, 0 )
                    };
                    itemGrid.Children.Add( m_AccountList_ItemNames[i] );
                    Grid.SetColumn( m_AccountList_ItemNames[i], 1 );
                    m_AccountList_Border[i] = new Border{ Background = new SolidColorBrush( CalcBorderColor( accountCnt, i, EditEnableToggle.IsOn ) ) };
                    itemGrid.Children.Add( m_AccountList_Border[i] );
                    Grid.SetColumn( m_AccountList_Border[i], 0 );
                    AccountList.Items.Add( itemGrid );
                }
            }
            else {
                // Remove excessive list items.
                for ( int i = m_AccountList_ItemCount - 1; i >= accountCnt; i-- ) {
                    m_AccountList_ItemNames[i] = null;
                    m_AccountList_Border[i] = null;
                    AccountList.Items.RemoveAt( i );
                }
            }
            m_AccountList_ItemCount = accountCnt;
        }

        private static Windows.UI.Color CalcBorderColor( int count, int idx, bool isEdiable )
        {
            const int h_top = 240;
            const int h_bottom = 40;
            const int s_top = 192;
            const int s_bottom = 160;
            const int v_top = 255;
            const int v_bottom = 192;
            int h,s,v,r,g,b,m;

            if ( isEdiable ) {
                h = 0;
                s = 255;
                v = 255;
            }
            else if ( count <= 1 ) {
                h = h_top;
                s = s_top;
                v = v_top;
            }
            else {
                h = ( h_top + ( ( h_bottom - h_top ) / ( count - 1 ) ) * idx );
                s = ( s_top + ( ( s_bottom - s_top ) / ( count - 1 ) ) * idx );
                v = ( v_top + ( ( v_bottom - v_top ) / ( count - 1 ) ) * idx );
            }

            m = v - ( s * v / 255 );
            if ( h < 60 ) {
                r = v;
                g = h * ( v - m ) / 60 + m;
                b = m;
            }
            else if ( h < 120 ) {
                r = ( 120 - h ) * ( v - m ) / 60 + m;
                g = v;
                b = m;
            }
            else if ( h < 180 ) {
                r = m;
                g = v;
                b = ( h - 120 ) * ( v - m ) / 60 + m;
            }
            else if ( h < 240 ) {
                r = m;
                g = ( 240 - h ) * ( v - m ) / 60 + m;
                b = v;
            }
            else if ( h < 300 ) {
                r = ( h - 240 ) * ( v - m ) / 60 + m;
                g = m;
                b = v;
            }
            else {
                r = v;
                g = m;
                b = ( 360 - h ) * ( v - m ) / 60 + m;
            }
            return Windows.UI.Color.FromArgb( 255, (byte)r, (byte)g, (byte)b );
        }

        private void UpdateInquiryList( int accountInfoIdx )
        {
            string hiddenString = GlbFunc.GetResourceString( "PASS_HIDDEN_STRING", "******" );
            m_SelectedAccount = accountInfoIdx;
            if ( AccountList.Items.Count <= 0 || m_SelectedAccount < 0 ) {
                // Clear all of controls in Account info pane.
                m_AccountNameTextUpdateFlg = true;
                AccountNameText.Text = "";
                m_AccountNameTextUpdateFlg = false;
                InquiryList.Items.Clear();

                for ( int i = 0; i < m_InquiryList_ItemTitles.Length; i++ )
                    m_InquiryList_ItemTitles[i] = null;
                for ( int i = 0; i < m_InquiryList_ItemTexts.Length; i++ )
                    m_InquiryList_ItemTexts[i] = null;
                for ( int i = 0; i < m_InquiryList_CopyButton.Length; i++ )
                    m_InquiryList_CopyButton[i] = null;
                for ( int i = 0; i < m_InquiryList_CopyButton_Click.Length; i++ )
                    m_InquiryList_CopyButton_Click[i] = null;
                for ( int i = 0; i < m_InquiryList_Border.Length; i++ )
                    m_InquiryList_Border[i] = null;
                m_InquiryList_ItemCount = 0;
            }
            else {
                // select specified item in AccountList
                m_AccountListSelectionFlg = true;
                AccountList.SelectedIndex = m_SelectedAccount;
                m_AccountListSelectionFlg = false;

                // Show selected account info in Account info pane.
                AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );
                int inquiryCount = ai.GetInquiryCount();
                m_AccountNameTextUpdateFlg = true;
                AccountNameText.Text = ai.AccountName;
                m_AccountNameTextUpdateFlg = false;

                // Allocate TextBlock vectors if necessary.
                if ( inquiryCount >=  m_InquiryList_ItemTitles.Length )
                    Array.Resize( ref m_InquiryList_ItemTitles, Math.Max( m_InquiryList_ItemTitles.Length * 2, inquiryCount + 1 ) );
                if ( inquiryCount >=  m_InquiryList_ItemTexts.Length )
                    Array.Resize( ref m_InquiryList_ItemTexts, Math.Max( m_InquiryList_ItemTexts.Length * 2, inquiryCount + 1 ) );
                if ( inquiryCount >=  m_InquiryList_CopyButton.Length )
                    Array.Resize( ref m_InquiryList_CopyButton, Math.Max( m_InquiryList_CopyButton.Length * 2, inquiryCount + 1 ) );
                if ( inquiryCount >=  m_InquiryList_CopyButton_Click.Length )
                    Array.Resize( ref m_InquiryList_CopyButton_Click, Math.Max( m_InquiryList_CopyButton_Click.Length * 2, inquiryCount + 1 ) );
                if ( inquiryCount >=  m_InquiryList_Border.Length )
                    Array.Resize( ref m_InquiryList_Border, Math.Max( m_InquiryList_Border.Length * 2, inquiryCount + 1 ) );

                for ( int i = 0; i < m_InquiryList_ItemCount && i < inquiryCount; i++ ) {
                    // update Inquiry Title and inquily text in existing control
                    m_InquiryList_ItemTitles[i].Text = ai.GetInquiryName( i );
                    if ( !ai.GetHideFlag( i ) )
                        m_InquiryList_ItemTexts[i].Text = ai.GetInquiryValue( i );
                    else
                        m_InquiryList_ItemTexts[i].Text = hiddenString;
                    m_InquiryList_Border[i].Background = new SolidColorBrush( CalcBorderColor( m_AccountList_ItemCount, accountInfoIdx, EditEnableToggle.IsOn ) );

                }
                if ( m_InquiryList_ItemCount < inquiryCount ) {
                    System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo( "en-US" );
                    SolidColorBrush wBorderBrush = new SolidColorBrush( CalcBorderColor( m_AccountList_ItemCount, accountInfoIdx, EditEnableToggle.IsOn ) );
                    // add missing items to InquiryList
                    for ( int i = m_InquiryList_ItemCount; i < inquiryCount; i++ ) {
                        int copy_i = i;
                        Grid g = new Grid {
                            Name = i.ToString( ci ),
                            Height = 80,
                            Margin = new Thickness( 0, 3, 0, 3 )
                        };
                        g.ColumnDefinitions.Add( new ColumnDefinition{ Width = new GridLength( 3.0 ) } );
                        g.ColumnDefinitions.Add( new ColumnDefinition() );
                        StackPanel sp = new StackPanel {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness( 10, 0, 0, 0 )
                        };
                        m_InquiryList_CopyButton[i] = 
                            new Button {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                Margin = new Thickness( 5, 0, 5, 0 ),
                                VerticalAlignment = VerticalAlignment.Center,
                                Height = 40,
                                Width = 40,
                                Content = new FontIcon {
                                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                                    Glyph = "\uF0E3"
                                },
                            };
                        m_InquiryList_CopyButton_Click[i] = ( sender, e ) => { OnCopyClipboardInquiryButton_Click( sender, e, copy_i ); };
                        m_InquiryList_CopyButton[i].Click += m_InquiryList_CopyButton_Click[i];
                        sp.Children.Add( m_InquiryList_CopyButton[i] );
                        StackPanel spSub = new StackPanel{
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Margin = new Thickness( 10, 5, 10, 0 )
                        };
                        m_InquiryList_ItemTitles[i] = new TextBlock{
                                Margin = new Thickness( 5, 5, 0, 3 ),
                                Text = ai.GetInquiryName( i )
                        };
                        m_InquiryList_ItemTexts[i] = new TextBlock{
                                Margin = new Thickness( 15, 3, 0, 3 ),
                                FontSize = 20.0
                        };
                        if ( !ai.GetHideFlag( i ) )
                            m_InquiryList_ItemTexts[i].Text = ai.GetInquiryValue( i );
                        else
                            m_InquiryList_ItemTexts[i].Text = hiddenString;
                        spSub.Children.Add( m_InquiryList_ItemTitles[i] );
                        spSub.Children.Add( m_InquiryList_ItemTexts[i] );
                        sp.Children.Add( spSub );
                        Grid.SetColumn( sp, 1 );
                        g.Children.Add( sp );
                        m_InquiryList_Border[i] = new Border{ Background = wBorderBrush };
                        Grid.SetColumn( m_InquiryList_Border[i], 0 );
                        g.Children.Add( m_InquiryList_Border[i] );
                        InquiryList.Items.Add( g );
                    }
                }
                else {
                    // delete excessive items from InquiryList
                    for ( int i = m_InquiryList_ItemCount - 1; i >= inquiryCount; i-- ) {
                        m_InquiryList_ItemTitles[i] = null;
                        m_InquiryList_ItemTexts[i] = null;
                        m_InquiryList_CopyButton[i] = null;
                        m_InquiryList_Border[i] = null;
                        InquiryList.Items.RemoveAt( i );
                    }
                }
                m_InquiryList_ItemCount = inquiryCount;

                if ( m_InquiryList_ItemCount > 0 )
                    InquiryList.SelectedItem = 0;
            }
            SetEdiableState( m_EnableEdit );
        }

        private async Task<bool> LoadPasswordFile() {
            string masterPass = "";
            bool passerror;
            bool isFirst = true;

            // Get saved password
            string savedPass = AppData.GetSavedUserPassword();
            if ( !string.IsNullOrEmpty( savedPass ) ) {
                if ( !AppData.Authenticate( savedPass, out passerror, out masterPass ) ) {
                    // If authenticate is failed by unknown error, it failed to load file.
                    if ( !passerror ) return false;
                    masterPass = "";
                    isFirst = false;
                }
            }

            string d_userPass = "";
            bool d_savePassFlg = false;
            while( string.IsNullOrEmpty( masterPass ) ) {

                // Show password dialog
                PasswordDialog d = new PasswordDialog();
                d.Password = d_userPass;
                d.SavePassword = d_savePassFlg;
                d.IsFirstTime = isFirst;
                await d.ShowAsync();

                // If canceled, failed to load file.
                if ( !d.IsOK ) return false;
                d_userPass = d.Password;
                d_savePassFlg = d.SavePassword;

                if ( !AppData.Authenticate( d_userPass, out passerror, out masterPass ) ) {
                    // If authenticate is failed by unknown error, it failed to load file.
                    if ( !passerror ) return false;
                    masterPass = "";
                    isFirst = false;
                }
            }

            // If authenticated and save password flag is specified, save inputed password string
            if ( d_savePassFlg )
                AppData.SaveUserPassword( d_userPass );

            // Load password file
            PasswordFile pf = new PasswordFile();
            if ( pf.Load( masterPass ) ) {
                // hold opened password file
                m_PasswordFile = pf;
            }
            else {
                // If failed to file, create new empty file.
                m_PasswordFile = new PasswordFile();
            }

            // hold master password string for future saving
            m_Password = masterPass;
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = SAVED_LABEL_TEXT;

            return true;
        }

        // On AddAccountButton clicked
        private void AddAccountButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // Insert new account to last of the list
            int idx = m_PasswordFile.GetCount();
            m_PasswordFile.Insert( idx );

            // Add item to AccountList
            UpdateAccountList();

            // Select inserted account and update right pane.
            UpdateInquiryList( idx );

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // On DeleteAccountButton clicked
        private void DeleteAccountButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // Get selected account info. And if not selected, ignore this operation.
            int sidx = AccountList.SelectedIndex;
            if ( sidx < 0 || sidx >= m_PasswordFile.GetCount() )
                return ;

            // delete selected item
            m_PasswordFile.Delete( sidx );
            UpdateAccountList();

            // select another account info
            if ( m_PasswordFile.GetCount() <= 0 ) {
                // If remaining account info is zero, unselect AccountList
                UpdateInquiryList( -1 );
            }
            else {
                if ( sidx >= m_PasswordFile.GetCount() ) {
                    // If deleted account info is in last of AccountList, select current last one.
                    UpdateInquiryList( sidx - 1 );
                }
                else {
                    // selected index is not changed.
                    UpdateInquiryList( sidx );
                }
            }

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // CopyAccountButton is clicked
        private void CopyAccountButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // Get selected account info. And if not selected, ignore this operation.
            int sidx = AccountList.SelectedIndex;
            if ( sidx < 0 || sidx >= m_PasswordFile.GetCount() )
                return ;

            // Insert new account 
            m_PasswordFile.Copy( sidx );

            // Add item to AccountList
            UpdateAccountList();

            // Select inserted account and update right pane.
            UpdateInquiryList( sidx + 1 );

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // Selection of AccountList is changed.
        private void AccountList_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If AccountList selection is updated by this program, ignore this event
            if ( m_AccountListSelectionFlg )
                return ;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // Get selected account info.
            int sidx = AccountList.SelectedIndex;
            if ( ( sidx < 0 || sidx >= m_PasswordFile.GetCount() ) && m_PasswordFile.GetCount() > 0 ) {
                // If unselected but account list is not empty, ignore this event
                return ;
            }

            // Update account info
            UpdateInquiryList( sidx );
        }

        // Add inquiry button is clicked.
        private async void AddInquiryButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() )
                return ;

            int idx = m_PasswordFile.GetAccountInfo( m_SelectedAccount ).GetInquiryCount();

            // Get new inquiry info
            AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );
            EditInquiry d = new EditInquiry( CreateEditInquiryCondidateDic() );
            d.ItemName = "";
            d.ItemValue = "";
            d.HideItemValue = false;
            await d.ShowAsync();
            if ( !d.IsOK ) return ;

            ai.InsertInquiryFromPlainValue( idx, d.ItemName, d.ItemValue, d.HideItemValue );
            UpdateInquiryList( m_SelectedAccount );
            InquiryList.SelectedIndex = idx;

            // Save explicitly
            SaveAccountFile();
        }

        // Delete inquiry button is clicked.
        private void DeleteInquiryButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() ) return ;

            // If Inquiry list item is not selected, ignore this event
            int si = InquiryList.SelectedIndex;
            if ( si < 0 ) return ;

            // Delete selected inquiry
            m_PasswordFile.GetAccountInfo( m_SelectedAccount ).DeleteInquiry( si );
            UpdateInquiryList( m_SelectedAccount );
            if ( si >= m_InquiryList_ItemCount )
                si--;
            InquiryList.SelectedIndex = si;

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // Copy inquiry button is clicked.
        private void CopyInquiryButton_Click( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e; 
            
            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() ) return ;

            // If Inquiry list item is not selected, ignore this event
            int si = InquiryList.SelectedIndex;
            if ( si < 0 ) return ;

            // Copy selected inquiry item
            AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );
            ai.InsertInquiryFromPlainValue( si, ai.GetInquiryName( si ), ai.GetInquiryValue( si ), ai.GetHideFlag( si ) );
            UpdateInquiryList( m_SelectedAccount );
            InquiryList.SelectedIndex = si;

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // Copy inquiry to clipboard button created on inquiry list control is clicked.
        private void OnCopyClipboardInquiryButton_Click( object sender, RoutedEventArgs e, int idx )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() )
                return ;

            AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );
            DataPackage dataPackage = new DataPackage{
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetText( ai.GetInquiryValue( idx ) );
            Clipboard.SetContent( dataPackage );
        }

        // Selection of inquiry list is changed
        private void InquiryList_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            _ = sender;
            _ = e;

            AddInquiryButton.IsEnabled = true;
            if ( InquiryList.Items.Count <= 0 ) {
                InquiryList.IsEnabled = false;
                DeleteInquiryButton.IsEnabled = false;
                CopyInquiryButton.IsEnabled = false;
            }
            else {
                DeleteInquiryButton.IsEnabled = true;
                CopyInquiryButton.IsEnabled = true;
            }
        }

        // AccountNameText string is edited
        private void AccountNameText_TextChanged( object sender, TextChangedEventArgs e )
        {
            _ = sender;
            _ = e;

            // If AccountNameText is updated by this program, this event is ignored
            if ( m_AccountNameTextUpdateFlg )
                return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile )
                return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() )
                return ;

            m_PasswordFile.GetAccountInfo( m_SelectedAccount ).AccountName = AccountNameText.Text;
            m_AccountList_ItemNames[ m_SelectedAccount ].Text = AccountNameText.Text;

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        // Double clicked on the splitter
        private void GridSplitter_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            if ( MainGrid.ColumnDefinitions[0].Width.Value < 3 ) {
                MainGrid.ColumnDefinitions[0].Width = new GridLength( 250 );
            }
            else
                MainGrid.ColumnDefinitions[0].Width = new GridLength( 0 );
        }

        private void InquiryList_DragItemsCompleted( ListViewBase sender, DragItemsCompletedEventArgs args )
        {
            _ = sender;
            _ = args;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 ) return ;

            int itemCnt = InquiryList.Items.Count;
            int [] vResult = new int[ itemCnt ];

            // Get re-ordered result
            for ( int i = 0; i < itemCnt; i++ ) {
                Grid g = (Grid)InquiryList.Items[i];
                if ( !Int32.TryParse( g.Name, out vResult[i] ) )
                    vResult[i] = -1;
            }
            int ci;
            for ( ci = 0; ci < itemCnt && vResult[ci] == ci; ci++ );
            if ( ci >= itemCnt ) return ;

            // re-order Inquiry list entry
            m_PasswordFile.GetAccountInfo( m_SelectedAccount ).Reorder( vResult );

            // re-order TextBox and Button control vector
            TextBlock [] rvItemTitles = new TextBlock[ m_InquiryList_ItemTitles.Length ];
            TextBlock [] rvItemTexts = new TextBlock[ m_InquiryList_ItemTexts.Length ];
            Button [] rvCopyButton = new Button[ m_InquiryList_CopyButton.Length ];
            Border [] rvBorder = new Border[ m_InquiryList_Border.Length ];
            System.Globalization.CultureInfo cinfo = new System.Globalization.CultureInfo( "en-US" );
            for ( int i = 0; i < itemCnt; i++ ) {
                rvItemTitles[i] = m_InquiryList_ItemTitles[ vResult[i] ];
                rvItemTexts[i] = m_InquiryList_ItemTexts[ vResult[i] ];
                rvCopyButton[i] = m_InquiryList_CopyButton[ vResult[i] ];
                rvBorder[i] = m_InquiryList_Border[ vResult[i] ];
                m_InquiryList_CopyButton[i].Click -= m_InquiryList_CopyButton_Click[i];

                Grid g = (Grid)InquiryList.Items[i];
                g.Name = i.ToString( cinfo );
            }
            m_InquiryList_ItemTitles = rvItemTitles;
            m_InquiryList_ItemTexts = rvItemTexts;
            m_InquiryList_CopyButton = rvCopyButton;
            m_InquiryList_Border = rvBorder;

            // update copy and edit button event handler
            for ( int i = 0; i < InquiryList.Items.Count; i++ ) {
                m_InquiryList_CopyButton[i].Click += m_InquiryList_CopyButton_Click[i];
            }

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        private void AccountList_DragItemsCompleted( ListViewBase sender, DragItemsCompletedEventArgs args )
        {
            _ = sender;
            _ = args;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            int itemCnt = AccountList.Items.Count;
            int [] vResult = new int[ itemCnt ];

            // Get re-ordered result
            for ( int i = 0; i < itemCnt; i++ ) {
                Grid g = (Grid)AccountList.Items[i];
                if ( !Int32.TryParse( g.Name, out vResult[i] ) )
                    vResult[i] = -1;
            }
            int ci;
            for ( ci = 0; ci < itemCnt && vResult[ci] == ci; ci++ );
            if ( ci >= itemCnt ) return ;

            // re-order Account entry
            m_PasswordFile.Reorder( vResult );

            // re-order TextBlock control vector
            TextBlock [] rvAccountTitles = new TextBlock[ m_AccountList_ItemNames.Length ];
            System.Globalization.CultureInfo cinfo = new System.Globalization.CultureInfo( "en-US" );
            for ( int i = 0; i < itemCnt; i++ ) {
                rvAccountTitles[i] = m_AccountList_ItemNames[ vResult[i] ];
                Grid g = (Grid)AccountList.Items[i];    // reset grid control name;
                g.Name = i.ToString( cinfo );
            }
            m_AccountList_ItemNames = rvAccountTitles;

            // re-order Border control vector
            Border [] rvAccountBorder = new Border[ m_AccountList_Border.Length ];
            for ( int i = 0; i < itemCnt; i++ ) {
                rvAccountBorder[i] = m_AccountList_Border[ vResult[i] ];
            }
            m_AccountList_Border = rvAccountBorder;

            UpdateInquiryList( AccountList.SelectedIndex );

            // Update last edit date time
            m_LastEditDate = DateTime.UtcNow;
            EditEnableToggle.OnContent = MODIFIED_LABEL_TEXT;
        }

        private async void InquiryList_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
        {
            _ = sender;

            if ( e.OriginalSource.GetType() == typeof( Grid ) )
                return ;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() ) return ;

            int idx = InquiryList.SelectedIndex;
            AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );

            // show edit dialog
            string hiddenString = GlbFunc.GetResourceString( "PASS_HIDDEN_STRING", "******" );
            EditInquiry d = new EditInquiry( CreateEditInquiryCondidateDic() );
            d.ItemName = ai.GetInquiryName( idx );
            d.ItemValue = ai.GetInquiryValue( idx );
            d.HideItemValue = ai.GetHideFlag( idx );
            await d.ShowAsync();
            if ( !d.IsOK ) return ;

            ai.SetInquiry( idx, d.ItemName, d.ItemValue, d.HideItemValue );
            m_InquiryList_ItemTitles[idx].Text = d.ItemName;
            if ( d.HideItemValue )
                m_InquiryList_ItemTexts[idx].Text = hiddenString;
            else
                m_InquiryList_ItemTexts[idx].Text = d.ItemValue;

            // Save explicitly
            SaveAccountFile();
        }

        private void EditEnableToggle_Toggled( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            if ( null == m_PasswordFile ) {
                // If password file if not loaded, set to readonly mode
                if ( EditEnableToggle.IsOn ) {
                    EditEnableToggle.IsOn = false;
                    SetEdiableState( false );
                }
                return ;
            }

            SetEdiableState( EditEnableToggle.IsOn );

            if ( !m_EnableEdit && m_PasswordFile.IsModified ) {
                // If set read-only mode from ediable mode and password file is modifyied, save tha file
                SaveAccountFile();
            }
        }

        private void SetEdiableState( bool f )
        {
            bool pwfLoaded = ( m_PasswordFile != null );
            bool isAccountSelected = pwfLoaded && ( AccountList.Items.Count > 0 || m_SelectedAccount >= 0 );
            bool isInquiryExist = false;
            
            if ( isAccountSelected ) {
                AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );
                isInquiryExist = ai.GetInquiryCount() > 0;
            }
            bool isInquirySelected = false;
            if ( isInquiryExist ) {
                isInquirySelected = InquiryList.SelectedIndex >= 0;
            }

            m_EnableEdit = f;
            AccountList.CanDragItems = f;
            AccountList.CanReorderItems = f;
            AccountList.AllowDrop = f;
            InquiryList.CanDragItems = f;
            InquiryList.CanReorderItems = f;
            InquiryList.AllowDrop = f;

            if ( f ) {
                AddAccountButton.Visibility = Visibility.Visible;
                DeleteAccountButton.Visibility = Visibility.Visible;
                CopyAccountButton.Visibility = Visibility.Visible;
                AddInquiryButton.Visibility = Visibility.Visible;
                DeleteInquiryButton.Visibility = Visibility.Visible;
                CopyInquiryButton.Visibility = Visibility.Visible;
                AccountList.Margin = new Thickness( AccountList.Margin.Left, AccountList.Margin.Top, AccountList.Margin.Right, 55 );
                InquiryList.Margin = new Thickness( InquiryList.Margin.Left, InquiryList.Margin.Top, InquiryList.Margin.Right, 55 );
            }
            else {
                AddAccountButton.Visibility = Visibility.Collapsed;
                DeleteAccountButton.Visibility = Visibility.Collapsed;
                CopyAccountButton.Visibility = Visibility.Collapsed;
                AddInquiryButton.Visibility = Visibility.Collapsed;
                DeleteInquiryButton.Visibility = Visibility.Collapsed;
                CopyInquiryButton.Visibility = Visibility.Collapsed;
                AccountList.Margin = new Thickness( AccountList.Margin.Left, AccountList.Margin.Top, AccountList.Margin.Right, 10 );
                InquiryList.Margin = new Thickness( InquiryList.Margin.Left, InquiryList.Margin.Top, InquiryList.Margin.Right, 10 );
            }

            AccountList.IsEnabled = true;
            AddAccountButton.IsEnabled = m_EnableEdit;
            DeleteAccountButton.IsEnabled = m_EnableEdit && isAccountSelected;
            CopyAccountButton.IsEnabled = m_EnableEdit && isAccountSelected;

            InquiryList.IsEnabled = isInquiryExist;
            AccountNameText.IsEnabled = m_EnableEdit && isAccountSelected;
            AddInquiryButton.IsEnabled = m_EnableEdit && isAccountSelected;
            DeleteInquiryButton.IsEnabled = m_EnableEdit && isInquirySelected;
            CopyInquiryButton.IsEnabled = m_EnableEdit && isInquirySelected;

            for ( int i = 0; i < m_AccountList_ItemCount; i++ ) {
                m_AccountList_Border[i].Background = new SolidColorBrush( CalcBorderColor( m_AccountList_ItemCount, i, f ) );
            }
            SolidColorBrush inquiryBorderBrush = new SolidColorBrush( CalcBorderColor( m_AccountList_ItemCount, m_SelectedAccount, f ) );
            for ( int i = 0; i < m_InquiryList_ItemCount; i++ ) {
                m_InquiryList_Border[i].Background = inquiryBorderBrush;
            }
        }

        SortedDictionary< string, KeyValuePair< string, SortedDictionary< string, string > > > CreateEditInquiryCondidateDic()
        {
            if ( null == m_PasswordFile ) return null;
            var r = new SortedDictionary< string, KeyValuePair< string, SortedDictionary< string, string > > >();
            for ( int i = 0; i < m_PasswordFile.GetCount(); i++ ) {
                AccountInfo ai = m_PasswordFile.GetAccountInfo( i );
                for ( int j = 0; j < ai.GetInquiryCount(); j++ ) {
                    string name_o = ai.GetInquiryName( j );
                    string name_u = name_o.Trim().ToUpperInvariant();
                    bool hide = ai.GetHideFlag( j );

                    KeyValuePair< string, SortedDictionary< string, string > > wr;
                    if ( r.TryGetValue( name_u, out wr ) ) {
                        // If duplicate item name is exist, add visible value to dictionary.
                        if ( !hide ) {
                            string value_o = ai.GetInquiryValue( j );
                            string value_u = value_o.Trim().ToUpperInvariant();
                            wr.Value.TryAdd( value_u, value_o );
                        }
                    }
                    else {
                        // If current item name is not exist, add this name and first visible value to dictionary.
                        var newvaldic = new SortedDictionary< string, string >();
                        if ( !hide ) {
                            string value_o = ai.GetInquiryValue( j );
                            string value_u = value_o.Trim().ToUpperInvariant();
                            newvaldic.Add( value_u, value_o );
                        }
                        r.TryAdd( name_u, new KeyValuePair< string, SortedDictionary< string, string > >( name_o, newvaldic ) );
                    }
                }
            }
            return r;
        }
    }
}
