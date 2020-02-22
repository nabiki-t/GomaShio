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

namespace GomaShio
{
    /// <summary>
    /// PasswordListPage class
    /// </summary>
    public sealed partial class PasswordListPage : Page
    {
        PasswordFile m_PasswordFile;
        string m_Password;

        TextBlock [] m_AccountList_ItemNames;
        int m_AccountList_ItemCount;

        TextBlock [] m_InquiryList_ItemTitles;
        TextBlock [] m_InquiryList_ItemTexts;
        Button [] m_InquiryList_CopyButton;
        RoutedEventHandler [] m_InquiryList_CopyButton_Click;

        int m_InquiryList_ItemCount;
        int m_SelectedAccount;
        bool m_AccountNameTextUpdateFlg;
        bool m_AccountListSelectionFlg;
        bool m_EnableEdit;

        public PasswordListPage()
        {
            m_AccountList_ItemNames = new TextBlock[16];
            m_AccountList_ItemCount = 0;
            m_InquiryList_ItemTitles = new TextBlock[16];
            m_InquiryList_ItemTexts = new TextBlock[16];
            m_InquiryList_CopyButton = new Button[16];
            m_InquiryList_CopyButton_Click = new RoutedEventHandler[16];
            m_InquiryList_ItemCount = 0;
            m_SelectedAccount = -1;
            m_AccountNameTextUpdateFlg = false;
            m_AccountListSelectionFlg = false;
            m_EnableEdit = false;
            this.InitializeComponent();
        }

        private async void Page_Loaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            // In first, all of controls are disabled.
            AccountList.Items.Clear();
            InquiryList.Items.Clear();
            m_AccountList_ItemCount = 0;
            SetEdiableState( false );

            // load password file
            if ( !await LoadPasswordFile().ConfigureAwait( true ) ) {
                // If failed to load password file, all of controls are remain disabled.
                m_PasswordFile = null;
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

            // SetEdiableState(  );

            Application.Current.Suspending += new SuspendingEventHandler(App_Suspending);

            // If account info is empty, default is editable. Others, default is read-only.
            EditEnableToggle.IsOn = ( m_PasswordFile.GetCount() == 0 );

        }

        private async void Page_Unloaded( object sender, RoutedEventArgs e )
        {
            _ = sender;
            _ = e;

            Application.Current.Suspending -= new SuspendingEventHandler(App_Suspending);
            if ( null == m_PasswordFile )
                return ;
            await SaveAccountFile().ConfigureAwait( true );
        }

        private async void App_Suspending( object sender, Windows.ApplicationModel.SuspendingEventArgs e )
        {
            await SaveAccountFile().ConfigureAwait( true );
        }

        private async Task SaveAccountFile()
        {
            // If password file is not loaded, there are not to save data.
            if ( null == m_PasswordFile ) return ;

            // If current password file is not modified, skip save file.
            if ( !m_PasswordFile.IsModified ) return ;

            // Get saved file token of password file.
            string ftoken = (string)ApplicationData.Current.LocalSettings.Values[ "FileToken" ];
            if ( string.IsNullOrEmpty( ftoken ) ) {
                // If the token of Password List file is not exists, I give up to save the password file.
                return;
            }

            StorageFile file = null;
            try {
                file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync( ftoken );
            }
            catch ( FileNotFoundException ) { }
            catch ( UnauthorizedAccessException ) { }
            catch ( ArgumentException ) { };
            if ( null == file ) {
                // If failed to get file permission, I give up to read the password file.
                return;
            }

            // save
            await m_PasswordFile.Save( file, m_Password ).ConfigureAwait( true );
        }

        private void UpdateAccountList()
        {
            // If password file is not loaded, ignore this operation
            if ( null == m_PasswordFile ) return ;

            // Get current account info count of password file.
            int accountCnt = m_PasswordFile.GetCount();

            if ( m_AccountList_ItemNames.Length <= accountCnt )
                 Array.Resize( ref m_AccountList_ItemNames, Math.Max( m_AccountList_ItemNames.Length * 2, accountCnt + 1 ) );

            for ( int i = 0; i < m_AccountList_ItemCount && i < accountCnt; i++ ) {
                // update Inquiry Title and inquily text in existing control
                m_AccountList_ItemNames[i].Text = m_PasswordFile.GetAccountInfo( i ).AccountName;
            }

            if ( m_AccountList_ItemCount < accountCnt ) {
                // Create and add new list items.

                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo( "en-US" );
                for ( int i = m_AccountList_ItemCount; i < accountCnt; i++ ) {
                    Grid itemGrid = new Grid {
                        Height = 40,
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
                    Border border = new Border{ Background = new SolidColorBrush( Windows.UI.Color.FromArgb( 255, 128, 128, 255 ) ) };
                    itemGrid.Children.Add( border );
                    Grid.SetColumn( border, 0 );
                    AccountList.Items.Add( itemGrid );
                }
            }
            else {
                // Remove excessive list items.
                for ( int i = m_AccountList_ItemCount - 1; i >= accountCnt; i-- ) {
                    m_AccountList_ItemNames[i] = null;
                    AccountList.Items.RemoveAt( i );
                }
            }
            m_AccountList_ItemCount = accountCnt;
        }

        private void UpdateInquiryList( int accountInfoIdx )
        {
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

                for ( int i = 0; i < m_InquiryList_ItemCount && i < inquiryCount; i++ ) {
                    // update Inquiry Title and inquily text in existing control
                    m_InquiryList_ItemTitles[i].Text = ai.GetInquiryName( i );
                    m_InquiryList_ItemTexts[i].Text = ai.GetInquiryValue( i );
                }
                if ( m_InquiryList_ItemCount < inquiryCount ) {
                    System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo( "en-US" );
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
                                Text = ai.GetInquiryValue( i ),
                                FontSize = 20.0
                        };
                        spSub.Children.Add( m_InquiryList_ItemTitles[i] );
                        spSub.Children.Add( m_InquiryList_ItemTexts[i] );
                        sp.Children.Add( spSub );
                        Grid.SetColumn( sp, 1 );
                        g.Children.Add( sp );
                        Border border = new Border{ Background = new SolidColorBrush( Windows.UI.Color.FromArgb( 255, 128, 128, 255 ) ) };
                        Grid.SetColumn( border, 0 );
                        g.Children.Add( border );
                        InquiryList.Items.Add( g );
                    }
                }
                else {
                    // delete excessive items from InquiryList
                    for ( int i = m_InquiryList_ItemCount - 1; i >= inquiryCount; i-- ) {
                        m_InquiryList_ItemTitles[i] = null;
                        m_InquiryList_ItemTexts[i] = null;
                        m_InquiryList_CopyButton[i] = null;
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

            // clear existing password file object.
            m_PasswordFile = null;
            PasswordFile wPWFile = new PasswordFile();

            string ftoken = (string)ApplicationData.Current.LocalSettings.Values[ "FileToken" ];
            if ( string.IsNullOrEmpty( ftoken ) ) {
                // If the token of Password List file is not exists, I give up to read the password file.
                return false;
            }

            StorageFile file = null;
            try {
                file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync( ftoken );
            }
            catch ( FileNotFoundException ) { }
            catch ( UnauthorizedAccessException ) { }
            catch ( ArgumentException ) { };
            if ( null == file ) {
                // If failed to get file permission, I give up to read the password file.
                GlbFunc.ShowMessage( "MSG_FAILED_LOAD_PASSWORD_FILE", "It failed to load password file." );
                return false;
            }

            // Get saved password
            string password = "";
            bool savePasswordFlg = ApplicationData.Current.LocalSettings.Values.ContainsKey( "Password" );
            if ( savePasswordFlg ) {
                password = (string)ApplicationData.Current.LocalSettings.Values[ "Password" ];
            }

            int loadResult = 2;

            // If password is saved, load specified file.
            if ( !String.IsNullOrEmpty( password ) ) {
                loadResult = await wPWFile.Load( file, password ).ConfigureAwait( true );
                // If faile IO error is occured, return with failed.
                if ( 1 == loadResult ) {
                GlbFunc.ShowMessage( "MSG_FAILED_LOAD_PASSWORD_FILE", "It failed to load password file." );
                    return false;
                }
            }

            PasswordDialog d = new PasswordDialog();
            d.IsFirstTime = true;
            while ( 2 == loadResult ) {
                // Get new password
                d.Password = password;
                d.SavePassword = savePasswordFlg;
                await d.ShowAsync();
                // If canceld, if failed to load.
                if ( !d.IsOK ) return false;

                password = d.Password;
                savePasswordFlg = d.SavePassword;
                d.IsFirstTime = false;

                // load file
                loadResult = await wPWFile.Load( file, password ).ConfigureAwait( true );
                switch( loadResult ) {
                case 0: // success
                        break;
                case 1: // file read error
                        GlbFunc.ShowMessage( "MSG_FAILED_LOAD_PASSWORD_FILE", "It failed to load password file." );
                        return false;
                case 2: // password error
                        Task.Delay( TimeSpan.FromMilliseconds( 500 ) ).Wait();
                        break;
                }
            }

            // hold password file object
            m_PasswordFile = wPWFile;

            // hold password string for future saving
            m_Password = password;

            // If specifyed to save password, write password to local storage.
            if ( savePasswordFlg )
                ApplicationData.Current.LocalSettings.Values[ "Password" ] = password;
            else
                ApplicationData.Current.LocalSettings.Values.Remove( "Password" );

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
            EditInquiry d = new EditInquiry();
            d.ItemName = "";
            d.ItemValue = "";
            await d.ShowAsync();
            if ( d.IsOK ) {
                ai.InsertInquiry( idx, d.ItemName, d.ItemValue );
                UpdateInquiryList( m_SelectedAccount );
                InquiryList.SelectedIndex = idx;
            }
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
            ai.InsertInquiry( si, ai.GetInquiryName( si ), ai.GetInquiryValue( si ) );
            UpdateInquiryList( m_SelectedAccount );
            InquiryList.SelectedIndex = si;
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

        // Edit inquiry button created on inquiry list control is clicked.
        private async void OnEditInquiryButton_Click( object sender, RoutedEventArgs e, int idx )
        {
            _ = sender;
            _ = e;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 || m_SelectedAccount >= m_PasswordFile.GetCount() ) return ;

            AccountInfo ai = m_PasswordFile.GetAccountInfo( m_SelectedAccount );

            // show edit dialog
            EditInquiry d = new EditInquiry();
            d.ItemName = ai.GetInquiryName( idx );
            d.ItemValue = ai.GetInquiryValue( idx );
            await d.ShowAsync();
            if ( d.IsOK ) {
                ai.SetInquiry( idx, d.ItemName, d.ItemValue );
                m_InquiryList_ItemTitles[idx].Text = d.ItemName;
                m_InquiryList_ItemTexts[idx].Text = d.ItemValue;
            }
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
            System.Globalization.CultureInfo cinfo = new System.Globalization.CultureInfo( "en-US" );
            for ( int i = 0; i < itemCnt; i++ ) {
                rvItemTitles[i] = m_InquiryList_ItemTitles[ vResult[i] ];
                rvItemTexts[i] = m_InquiryList_ItemTexts[ vResult[i] ];
                rvCopyButton[i] = m_InquiryList_CopyButton[ vResult[i] ];
                m_InquiryList_CopyButton[i].Click -= m_InquiryList_CopyButton_Click[i];

                Grid g = (Grid)InquiryList.Items[i];
                g.Name = i.ToString( cinfo );
            }
            m_InquiryList_ItemTitles = rvItemTitles;
            m_InquiryList_ItemTexts = rvItemTexts;
            m_InquiryList_CopyButton = rvCopyButton;

            // update copy and edit button event handler
            for ( int i = 0; i < InquiryList.Items.Count; i++ ) {
                m_InquiryList_CopyButton[i].Click += m_InquiryList_CopyButton_Click[i];
            }
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
                Grid g = (Grid)AccountList.Items[i];
                g.Name = i.ToString( cinfo );
            }
            m_AccountList_ItemNames = rvAccountTitles;

            UpdateInquiryList( AccountList.SelectedIndex );
        }

        private void InquiryList_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
        {
            _ = sender;

            if ( e.OriginalSource.GetType() == typeof( Grid ) )
                return ;

            // If password file is not loaded, ignore this operation.
            if ( null == m_PasswordFile ) return ;

            // If in Read-Only mode, ignore this operation.
            if ( !m_EnableEdit ) return ;

            // If Account list item is not selected, ignore this event
            if ( m_SelectedAccount < 0 ) return ;

            int si = InquiryList.SelectedIndex;
            OnEditInquiryButton_Click( null, null, si );
        }

        private async void EditEnableToggle_Toggled( object sender, RoutedEventArgs e )
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
                await SaveAccountFile().ConfigureAwait( true );
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
            }
            else {
                AddAccountButton.Visibility = Visibility.Collapsed;
                DeleteAccountButton.Visibility = Visibility.Collapsed;
                CopyAccountButton.Visibility = Visibility.Collapsed;
                AddInquiryButton.Visibility = Visibility.Collapsed;
                DeleteInquiryButton.Visibility = Visibility.Collapsed;
                CopyInquiryButton.Visibility = Visibility.Collapsed;
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
        }
    }
}
