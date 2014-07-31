using Mapper.Consumer;
using Microsoft.Live;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.UI.Popups;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Newtonsoft.Json;
#endif

#if !NETFX_CORE
using System.IO.IsolatedStorage;
using System.Windows;
#endif

namespace MeetWhere.Cloud
{
    static class CloudAccesser
    {
        private static MobileServiceClient service = new MobileServiceClient("https://jcookedemo.azure-mobile.net/");
        private static LiveAuthClient liveIdClient = new LiveAuthClient(
#if NETFX_CORE
"https://jcookedemo.azure-mobile.net/"
#else
"00000000440F59EB"
#endif
);

        private static LiveConnectSession session;
        private static string foldername = "Maps";

        public static async void ClearCachedMaps()
        {
            bool ok = true;
#if NETFX_CORE
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                var mapsFolder = await localFolder.GetFolderAsync(foldername);
                foreach (var z in await mapsFolder.GetFilesAsync())
                {
                    try
                    {
                        var tmp = z.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        ok = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                ok = false;
            }
#else
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            foreach (string s in store.GetFileNames("Maps/*"))
            {
                try
                {
                    store.DeleteFile("Maps/" + s);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    ok = false;
                }
            }
            await DummyTask();
#endif

            await MessageBoxShow((ok ? "Successfully cleaned" : "Error cleaning") + " the cache");
        }

        async static public Task<string> LoadMapSvg(RoomInfo location, Action<bool> waiter)
        {
            // Check cache
            waiter(true);
            try
            {
#if NETFX_CORE
                string filename = location.Building + "-" + location.Floor + ".svg";
                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                try
                {
                    var folders = await localFolder.GetFoldersAsync();
                    var mapsFolder = folders.FirstOrDefault(p => p.Name == foldername);
                    if (mapsFolder != null)
                    {
                        var file = await mapsFolder.GetFileAsync(filename);
                        if (file != null)
                        {
                            var content = (await file.OpenSequentialReadAsync()).AsStreamForRead();
                            string svgDocContent = new System.IO.StreamReader(content).ReadToEnd();
                            return svgDocContent;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
#else
                string filename = foldername + "/" + location.Building + "-" + location.Floor + ".svg";
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                if (store.FileExists(filename))
                {
                    var content = store.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    string svgDocContent = new System.IO.StreamReader(content).ReadToEnd();
                    return svgDocContent;
                }
#endif

                // Not in cache, so need to access files on server.
                if (!await TryLogin())
                {
                    return null;
                }

                // OK, user has permission so lets try to get the data from the server.
                var mapTable = service.GetTable<Map>();
                List<Map> maps = null;
                try
                {
                    maps = await mapTable.Where(p => p.Building == location.Building && p.Floor == location.Floor).ToListAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                if (maps == null || maps.Count() == 0)
                {
                    await MessageBoxShow("Server does not have a map for: " + location.ToString());
#if !NETFX_CORE
                    return null;
#else

                    // Upload mode

                    // FilePicker APIs will not work if the application is in a snapped state.
                    if (ApplicationView.Value == ApplicationViewState.Snapped)
                    {
                        await MessageBoxShow("Cannot browse files when snapped");
                        return null;
                    }

                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.ViewMode = PickerViewMode.List;
                    openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                    openPicker.FileTypeFilter.Add(".htm");

                    StorageFile file = await openPicker.PickSingleFileAsync();
                    if (file == null)
                    {
                        await MessageBoxShow("Did not select file");
                        return null;
                    }

                    var s = await file.OpenSequentialReadAsync();
                    string content0 = new StreamReader(s.AsStreamForRead()).ReadToEnd();

                    // Verify that the file is OK before uploading.
                    var mapObjects = Mapper.Parser.SvgParser.ParseSvgDoc(location, content0);
                    var mapObjectsJson = JsonConvert.SerializeObject(mapObjects, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                    var chunks = Chunker(mapObjectsJson, 1024 * 128);

                    for (int i = 0; i < chunks.Length; i++)
                    {
                        var newMap = new Map() { Floor = location.Floor, Building = location.Building, Part = i, SVG = chunks[i] };
                        Debug.WriteLine("inserting " + i + " of " + chunks.Length + " for " + location.Building + "/" + location.Floor);
                        try
                        {
                            await mapTable.InsertAsync(newMap);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            return null;
                        }
                    }

                    maps = await mapTable.Where(p => p.Building == location.Building && p.Floor == location.Floor).ToListAsync();
                    if (maps.Count() == 0)
                    {
                        await MessageBoxShow("Still wrong!");
                        return null;
                    }
#endif
                }

                // Map is on the server
                string svg = string.Join("", maps.OrderBy(p => p.Part).Select(p => p.SVG));

#if !NETFX_CORE
                if (!store.DirectoryExists("Maps"))
                {
                    store.CreateDirectory("Maps");
                }

                var content2 = store.OpenFile(filename, FileMode.OpenOrCreate, FileAccess.Write);
                byte[] bytes = Encoding.UTF8.GetBytes(svg);
                content2.Write(bytes, 0, bytes.Length);
                content2.Flush();
                content2.Close();
#else
                try
                {
                    var folders = await localFolder.GetFoldersAsync();
                    var mapsFolder = folders.FirstOrDefault(p => p.Name == foldername);
                    if (mapsFolder == null)
                    {
                        mapsFolder = await localFolder.CreateFolderAsync(foldername);
                    }

                    var file = await mapsFolder.CreateFileAsync(filename);
                    Stream content2 = await file.OpenStreamForWriteAsync();
                    byte[] bytes = Encoding.UTF8.GetBytes(svg);
                    content2.Write(bytes, 0, bytes.Length);
                    content2.Flush();
                    content2.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
#endif

                return svg;
            }
            finally
            {
                waiter(false);
            }
        }

#if NETFX_CORE
        private static string[] Chunker(string input, int chunkLength)
        {
            int numChunks = input.Length / chunkLength + (input.Length % chunkLength == 0 ? 0 : 1);
            string[] chunks = new string[numChunks];
            for (int i = 0; i < input.Length / chunkLength; i++)
            {
                chunks[i] = input.Substring(i * chunkLength, chunkLength);
            }
            if (input.Length % chunkLength != 0)
            {
                chunks[chunks.Length - 1] = input.Substring((chunks.Length - 1) * chunkLength, input.Length % chunkLength);
            }
            return chunks;
        }
#endif

        private static Dictionary<string, MobileServiceAuthenticationProvider?> providers = new Dictionary<string, MobileServiceAuthenticationProvider?>() {
            {"Microsoft", MobileServiceAuthenticationProvider.MicrosoftAccount},
            {"Google", MobileServiceAuthenticationProvider.Google},
        };

        public static void Logout()
        {
            service.Logout();
        }

        public static async Task<bool> Authenticate(bool quiet = false)
        {
            if (session == null)
            {
#if !NETFX_CORE
                if (IsolatedStorageSettings.ApplicationSettings.Contains("CachedLoginToken"))
                {
                    string token = (string)IsolatedStorageSettings.ApplicationSettings["CachedLoginToken"];
                    try
                    {
                        MobileServiceUser loginResult = await service.LoginAsync(token);

                        if (!quiet)
                        {
                            string title = string.Format("Welcome!");
                            var message = string.Format("You are now logged in - {0}", loginResult.UserId);
                            await MessageBoxShow(message, title);
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
#endif

                LiveLoginResult result = await liveIdClient.LoginAsync(new[] { "wl.signin" });
                if (result.Status == LiveConnectSessionStatus.Connected)
                {
                    session = result.Session;
                    LiveConnectClient client = new LiveConnectClient(result.Session);
                    LiveOperationResult meResult = await client.GetAsync("me");
                    string token = result.Session.AuthenticationToken;

#if !NETFX_CORE
                    IsolatedStorageSettings.ApplicationSettings["CachedLoginToken"] = token;
                    IsolatedStorageSettings.ApplicationSettings.Save();
#endif

                    MobileServiceUser loginResult = await service.
#if NETFX_CORE
LoginWithMicrosoftAccountAsync
#else
LoginAsync
#endif
(token);

                    if (!quiet)
                    {
                        string title = string.Format("Welcome {0}!", meResult.Result["first_name"]);
                        var message = string.Format("You are now logged in - {0}", loginResult.UserId);
                        await MessageBoxShow(message, title);
                    }
                }
                else
                {
                    if (!quiet)
                    {
                        await MessageBoxShow("Could not log in automatically!");
                    }
                    session = null;
                }
            }

            return session == null;
        }

        public static async Task<bool> TryLogin()
        {
            if (service.CurrentUser == null)
            {
                // Try to log in
                MobileServiceAuthenticationProvider? provider = null; //  await PickProvider();
                if (!provider.HasValue)
                {
                    return false;
                }

                try
                {
                    await service.LoginAsync(provider.Value);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            if (service.CurrentUser == null)
            {
                await MessageBoxShow("You must log in. Login Required");
                return false;
            }

            // Is the user registered?
            var appUserTable = service.GetTable<AppUser>();
            var appUsers = await appUserTable.Where(p => p.UserId == service.CurrentUser.UserId).ToListAsync();
            if (appUsers.Count() != 1)
            {
                AppUser user = null; // TODO await GetRegistration();
                if (user != null)
                {
                    await appUserTable.InsertAsync(user);
                    // Reload the user
                    appUsers = await appUserTable.Where(p => p.UserId == service.CurrentUser.UserId).ToListAsync();
                }

                if (appUsers.Count() != 1)
                {
                    await MessageBoxShow("registration failed");
                    return false;
                }
            }

            // Is authorized?
            var appUser = appUsers[0];
            if (!appUser.IsAuthorized)
            {
                appUser.IsAuthorized = true;
                await appUserTable.UpdateAsync(appUser);
                await MessageBoxShow("You have not been authorized yet. Try again later");
                return false;
            }

            return true;
        }

        private static Task MessageBoxShow(string message)
        {
#if NETFX_CORE
            MessageDialog dlg = new Windows.UI.Popups.MessageDialog(message);
            return dlg.ShowAsync().AsTask();
#else
            MessageBox.Show(message);
            return DummyTask();
#endif
        }

        private static Task MessageBoxShow(string message, string title)
        {
#if NETFX_CORE
            MessageDialog dlg = new Windows.UI.Popups.MessageDialog(message, title);
            return dlg.ShowAsync().AsTask();
#else
            MessageBox.Show(message, title, MessageBoxButton.OK);
            return DummyTask();
#endif
        }

        private static Task<object> DummyTask()
        {
            var t = new Task<object>(() => null);
            t.Start();
            return t;
        }

        public static bool IsLoggedin()
        {
            return (service.CurrentUser != null);
        }

        //        private static Task<MobileServiceAuthenticationProvider?> PickProvider()
        //        {
        //            var tcs = new TaskCompletionSource<MobileServiceAuthenticationProvider?>();
        //            MobileServiceAuthenticationProvider? value = null;

        //            //            ListPicker listPicker = new ListPicker() {
        //            //                Header = "Available providers:",
        //            ListBox listPicker = new ListBox()
        //            {
        //                ItemsSource = providers.Keys,
        //                Margin = new Thickness(12, 42, 24, 18)
        //            };

        //            CustomMessageBox messageBox = new CustomMessageBox()
        //            {
        //                Title = "Login Required",
        //                Message = "Choose which provider to use",
        //                Content = listPicker,
        //                LeftButtonContent = "OK",
        //                RightButtonContent = "Cancel",
        //                IsFullScreen = false,
        //            };

        //            //messageBox.Dismissing += (s1, e1) =>
        //            //{
        //            //    if (listPicker.ListPickerMode == ListPickerMode.Expanded)
        //            //    {
        //            //        e1.Cancel = true;
        //            //    }
        //            //};

        //            messageBox.Dismissed += (s2, e2) =>
        //            {
        //                value = (e2.Result == CustomMessageBoxResult.LeftButton && listPicker.SelectedItem != null ? providers[(string)listPicker.SelectedItem] : null);
        //            };

        //            messageBox.Unloaded += (s2, e2) => tcs.SetResult(value);

        //#if NETFX_CORE
        //            throw new Exception("zasgasg");
        //#else
        //            messageBox.Show();
        //#endif
        //            return tcs.Task;
        //        }

        //        public static Task<AppUser> GetRegistration()
        //        {
        //            var tcs = new TaskCompletionSource<AppUser>();
        //            AppUser value = null;

        //            Grid g = new Grid() { Margin = new Thickness(12, 42, 24, 18) };
        //            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        //            g.ColumnDefinitions.Add(new ColumnDefinition());
        //            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        //            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

        //            TextBlock Name = new TextBlock() { Text = "Name", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        //            g.Children.Add(Name);
        //            Grid.SetColumn(Name, 0);
        //            Grid.SetRow(Name, 0);


        //            TextBox UserName = new TextBox() { VerticalAlignment = VerticalAlignment.Center };
        //            g.Children.Add(UserName);
        //            Grid.SetColumn(UserName, 1);
        //            Grid.SetRow(UserName, 0);

        //            TextBlock Email = new TextBlock() { Text = "Email", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        //            g.Children.Add(Email);
        //            Grid.SetColumn(Email, 0);
        //            Grid.SetRow(Email, 1);

        //            TextBox UserEmail = new TextBox() { VerticalAlignment = VerticalAlignment.Center };
        //            g.Children.Add(UserEmail);
        //            Grid.SetColumn(UserEmail, 1);
        //            Grid.SetRow(UserEmail, 1);

        //            CustomMessageBox messageBox = new CustomMessageBox()
        //            {
        //                Title = "Registration Required",
        //                Message = "Complete all fields to create your account",
        //                Content = g,
        //                LeftButtonContent = "OK",
        //                RightButtonContent = "Cancel",
        //                IsFullScreen = false,
        //            };

        //            messageBox.Dismissing += (s1, e1) =>
        //            {
        //                if (e1.Result == CustomMessageBoxResult.LeftButton &&
        //                    (string.IsNullOrWhiteSpace(UserEmail.Text) || string.IsNullOrWhiteSpace(UserName.Text)))
        //                {
        //                    e1.Cancel = true;
        //                }
        //            };

        //            messageBox.Dismissed += (s2, e2) =>
        //            {
        //                if (e2.Result == CustomMessageBoxResult.LeftButton)
        //                {
        //                    value = new AppUser() { Email = UserEmail.Text, Name = UserName.Text };
        //                }
        //            };

        //            messageBox.Unloaded += (s2, e2) => tcs.SetResult(value);

        //#if NETFX_CORE
        //            throw new Exception("zasgasg");
        //#else
        //            messageBox.Show();
        //#endif

        //            return tcs.Task;
        //        }

    }
}
