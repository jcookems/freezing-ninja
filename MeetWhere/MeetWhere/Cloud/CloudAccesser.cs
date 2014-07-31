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
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading;
using MeetWhere.XPlat;

#if NETFX_CORE
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Newtonsoft.Json;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#else
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
#endif
#if WINDOWS_PHONE
using Microsoft.Phone.Controls;
#endif

namespace MeetWhere.Cloud
{
    static class CloudAccesser
    {
        private static MobileServiceClient _service = null;

        private static MobileServiceClient service
        {
            get
            {
                string appKey = null;
                if (_service == null)
                {
                    // Set the install ID first.
#if NETFX_CORE || WINDOWS_PHONE
#if NETFX_CORE
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values
#else
                    IsolatedStorageSettings.ApplicationSettings
#endif
["MobileServices.Installation.config"] =
                            @"{ ""applicationInstallationId"": ""No Unique Device ID for you!"" }";
#else // Win desktop
                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(
                        IsolatedStorageScope.Assembly | IsolatedStorageScope.User, null, null))
                    {
                        using (IsolatedStorageFileStream fileStream = isoStore.OpenFile(
                            "applicationInstallationId", FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (var writer = new StreamWriter(fileStream))
                            {
                                writer.WriteLine(@"{ ""applicationInstallationId"": ""No Unique Device ID for you!"" }");
                            }
                        }
                    }
                    appKey = "ZDGayNoTiIChnLUmElbrCKwywbgKtE49";
#endif

                    _service = new MobileServiceClient("https://jcookedemo.azure-mobile.net/", appKey, new MyHandler()

#if !NETFX_CORE && !WINDOWS_PHONE
, new CustomParametersServiceFilter()
#endif
);
                }
                return _service;
            }
        }

#if NETFX_CORE || WINDOWS_PHONE
        private static LiveAuthClient liveIdClient = new LiveAuthClient(
#if NETFX_CORE
"https://jcookedemo.azure-mobile.net/"
#else
"00000000440F59EB"
#endif
);
#endif

        public class MyHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Debug.WriteLine("Installation id: " + String.Join(";", request.Headers.FirstOrDefault(p => p.Key == "X-ZUMO-INSTALLATION-ID").Value.ToArray()));
                return await base.SendAsync(request, cancellationToken);
            }
        }

#if !NETFX_CORE && !WINDOWS_PHONE
        public class CustomParametersServiceFilter : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Get previous uri query
                var uriBuilder = new UriBuilder(request.RequestUri);
                uriBuilder.Query = (uriBuilder.Query == null ? "" : uriBuilder.Query.TrimStart('?') + "&") + "noAuth=MyMagicKey";
                request.RequestUri = uriBuilder.Uri;
                return await base.SendAsync(request, cancellationToken);
            }
        }
#endif

#if NETFX_CORE || WINDOWS_PHONE
        private static LiveConnectSession session = null;
#endif
        private static string foldername = "Maps";

        public static async void ClearCachedMaps()
        {
            bool ok = await Cache.DeleteFolder(foldername);
            await UI.MessageBoxShow((ok ? "Successfully cleaned" : "Error cleaning") + " the cache");
        }

        async static public Task<MapMetadata> LoadMapMetadata(RoomInfo location)
        {
            var mapMetadataTable = service.GetTable<MapMetadata>();
            MapMetadata y = null;
            try
            {
                var x = await mapMetadataTable.Where(p => p.Building == location.Building && p.Floor == location.Floor).ToListAsync();
                y = x.Where(p => p.Id.HasValue).OrderByDescending(p => p.Id).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            if (y == null)
            {
                try
                {
                    var x = await mapMetadataTable.Where(p => p.Building == location.Building).ToListAsync();
                    y = x.Where(p => p.Id.HasValue).OrderByDescending(p => p.Id).FirstOrDefault();
                    // Need to fix up the fields to make unique
                    y.Id = null;
                    y.Floor = location.Floor;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            if (y == null)
            {
                // Start with some defaults.
                y = new MapMetadata()
                {
                    Building = location.Building,
                    Floor = location.Floor,
                    CenterLat = 47.636425,
                    CenterLong = -122.133110,
                    MapSize = 300,
                    GeoSize = 0.0004,
                    Angle = 90,
                    Scale = 1.53418608096759,
                    OffsetX = -117.849160438776,
                    OffsetY = 143.735496115895,
                };
            }

            return y;
        }

        async static public void SaveMapMetadata(MapMetadata mapMetadata)
        {
            var mapMetadataTable = service.GetTable<MapMetadata>();
            await mapMetadataTable.InsertAsync(mapMetadata);
        }

        async static public Task<string> LoadMapSvg(RoomInfo location, Action<bool> waiter)
        {
            // Check cache
            waiter(true);
            try
            {
                string fileName = location.Building + "-" + location.Floor + ".svg";
                string content = await Cache.ReadFileContent(foldername, fileName);
                if (!string.IsNullOrEmpty(content)) return content;

#if NETFX_CORE || WINDOWS_PHONE
                // Not in cache, so need to access files on server.
                if (!await TryLogin())
                {
                    return null;
                }
#endif

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
                    await UI.MessageBoxShow("Server does not have a map for: " + location.ToString());
#if !NETFX_CORE
                    return null;
#else

                    // Upload mode

                    // FilePicker APIs will not work if the application is in a snapped state.
                    if (ApplicationView.Value == ApplicationViewState.Snapped)
                    {
                        await UI.MessageBoxShow("Cannot browse files when snapped");
                        return null;
                    }

                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.ViewMode = PickerViewMode.List;
                    openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                    openPicker.FileTypeFilter.Add(".vdx");
                    openPicker.CommitButtonText = "File for " + location.Building + "/" + location.Floor;

                    StorageFile file = await openPicker.PickSingleFileAsync();
                    if (file == null)
                    {
                        await UI.MessageBoxShow("Did not select file");
                        return null;
                    }

                    var s = await file.OpenSequentialReadAsync();
                    string content0 = new StreamReader(s.AsStreamForRead()).ReadToEnd();

                    // Verify that the file is OK before uploading.
                    //                    var mapObjects = Mapper.Parser.SvgParser.ParseSvgDoc(location, content0);
                    var mapObjects = Mapper.Parser.VisioParser.ParseVisioDoc(location, content0);
                    // Filter out some of the greebles
                    mapObjects = mapObjects.Where(p => p.Parent == Mapper.ParentType.Labels || p.Parent == Mapper.ParentType.Rooms);

                    var mapObjectsJson = JsonConvert.SerializeObject(mapObjects, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                    var chunks = Chunker(mapObjectsJson, 1024 * 128);

                    for (int i = 0; i < chunks.Length; i++)
                    {
                        var newMap = new Map() { Floor = location.Floor, Building = location.Building, Part = i, Description = chunks[i] };
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
                        await UI.MessageBoxShow("Still wrong!");
                        return null;
                    }
#endif
                }

                // Map is on the server
                string svg = string.Join("", maps.OrderBy(p => p.Part).Select(p => p.Description));
                await Cache.WriteFileContent(foldername, fileName, svg);
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

#if NETFX_CORE || WINDOWS_PHONE
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
                string token = Cache.GetSetting("CachedLoginToken");
                if (token != null)
                {
                    try
                    {
                        MobileServiceUser loginResult = await service.LoginAsync(
                            MobileServiceAuthenticationProvider.MicrosoftAccount,
                            new JObject(new JProperty("authenticationToken", token)));

                        if (!quiet)
                        {
                            string title = string.Format("Welcome!");
                            var message = string.Format("You are now logged in - {0}", loginResult.UserId);
                            await UI.MessageBoxShow(message, title);
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }

                LiveLoginResult result = await liveIdClient.LoginAsync(new[] { "wl.signin" });
                if (result.Status == LiveConnectSessionStatus.Connected)
                {
                    session = result.Session;
                    LiveConnectClient client = new LiveConnectClient(result.Session);
                    LiveOperationResult meResult = await client.GetAsync("me");
                    token = result.Session.AuthenticationToken;
                    Cache.SetSetting("CachedLoginToken", token);
                    MobileServiceUser loginResult = await service.LoginAsync(
                        MobileServiceAuthenticationProvider.MicrosoftAccount,
                        new JObject(new JProperty("authenticationToken", token)));

                    if (!quiet)
                    {
                        string title = string.Format("Welcome {0}!", meResult.Result["first_name"]);
                        var message = string.Format("You are now logged in - {0}", loginResult.UserId);
                        await UI.MessageBoxShow(message, title);
                    }
                }
                else
                {
                    if (!quiet)
                    {
                        await UI.MessageBoxShow("Could not log in automatically!");
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
                MobileServiceAuthenticationProvider? provider = await PickProvider();
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
                await UI.MessageBoxShow("You must log in. Login Required");
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
                    await UI.MessageBoxShow("registration failed");
                    return false;
                }
            }

            // Is authorized?
            var appUser = appUsers[0];
            if (!appUser.IsAuthorized)
            {
                appUser.IsAuthorized = true;
                await appUserTable.UpdateAsync(appUser);
                await UI.MessageBoxShow("You have not been authorized yet. Try again later");
                return false;
            }

            return true;
        }

        public static bool IsLoggedin()
        {
            return (service.CurrentUser != null);
        }

        private static Task<MobileServiceAuthenticationProvider?> PickProvider()
        {
            var tcs = new TaskCompletionSource<MobileServiceAuthenticationProvider?>();
            MobileServiceAuthenticationProvider? value = null;

            //            ListPicker listPicker = new ListPicker() {
            //                Header = "Available providers:",
            ListBox listPicker = new ListBox()
            {
                ItemsSource = providers.Keys,
                Margin = new Thickness(12, 42, 24, 18)
            };

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Title = "Login Required",
                Message = "Choose which provider to use",
                Content = listPicker,
                LeftButtonContent = "OK",
                RightButtonContent = "Cancel",
                IsFullScreen = false,
            };

            //messageBox.Dismissing += (s1, e1) =>
            //{
            //    if (listPicker.ListPickerMode == ListPickerMode.Expanded)
            //    {
            //        e1.Cancel = true;
            //    }
            //};

            messageBox.Dismissed += (s2, e2) =>
            {
                value = (e2.Result == CustomMessageBoxResult.LeftButton && listPicker.SelectedItem != null ? providers[(string)listPicker.SelectedItem] : null);
            };

            messageBox.Unloaded += (s2, e2) => tcs.SetResult(value);
            messageBox.Show();

            return tcs.Task;
        }

        public static Task<AppUser> GetRegistration()
        {
            var tcs = new TaskCompletionSource<AppUser>();
            AppUser value = null;

            Grid g = new Grid() { Margin = new Thickness(12, 42, 24, 18) };
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            TextBlock Name = new TextBlock() { Text = "Name", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            g.Children.Add(Name);
            Grid.SetColumn(Name, 0);
            Grid.SetRow(Name, 0);


            TextBox UserName = new TextBox() { VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(UserName);
            Grid.SetColumn(UserName, 1);
            Grid.SetRow(UserName, 0);

            TextBlock Email = new TextBlock() { Text = "Email", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            g.Children.Add(Email);
            Grid.SetColumn(Email, 0);
            Grid.SetRow(Email, 1);

            TextBox UserEmail = new TextBox() { VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(UserEmail);
            Grid.SetColumn(UserEmail, 1);
            Grid.SetRow(UserEmail, 1);

            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Title = "Registration Required",
                Message = "Complete all fields to create your account",
                Content = g,
                LeftButtonContent = "OK",
                RightButtonContent = "Cancel",
                IsFullScreen = false,
            };

            messageBox.Dismissing += (s1, e1) =>
            {
                if (e1.Result == CustomMessageBoxResult.LeftButton &&
                    (string.IsNullOrWhiteSpace(UserEmail.Text) || string.IsNullOrWhiteSpace(UserName.Text)))
                {
                    e1.Cancel = true;
                }
            };

            messageBox.Dismissed += (s2, e2) =>
            {
                if (e2.Result == CustomMessageBoxResult.LeftButton)
                {
                    value = new AppUser() { Email = UserEmail.Text, Name = UserName.Text };
                }
            };

            messageBox.Unloaded += (s2, e2) => tcs.SetResult(value);
            messageBox.Show();

            return tcs.Task;
        }
#endif

    }
}
