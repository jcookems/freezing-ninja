using Mapper.Consumer;
using Microsoft.Phone.Controls;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MeetWhere.Cloud
{
    static class CloudAccesser
    {
        private static MobileServiceClient service = new MobileServiceClient("https://jcookedemo.azure-mobile.net/");

        static public void ClearCachedMaps()
        {
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
                }
            }
        }

        async static public Task<string> LoadMapSvg(RoomInfo location, Action<bool> waiter)
        {
            // Check cache
            waiter(true);
            try
            {
                string filename = "Maps/" + location.Building + "-" + location.Floor + ".svg";
                IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
                if (store.FileExists(filename))
                {
                    // TODO: Add app-wide support for clearing cache
                    var content = store.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    string svgDocContent = new System.IO.StreamReader(content).ReadToEnd();
                    return svgDocContent;
                }

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
                    MessageBox.Show("Server does not have a map for: " + location.ToString());
#if !NETFX_CORE
                    return null;
#else
                // Upload mode
                StreamResourceInfo resource = Application.GetResourceStream(new Uri(filename, UriKind.Relative));
                string content0 = new StreamReader(resource.Stream).ReadToEnd();
                var chunks = Chunker(content0, 1024 * 128);

                for (int i = 0; i < chunks.Length; i++)
                {
                    var newMap = new Map() { Floor = location.Floor, Building = location.Building, Part = i, SVG = chunks[i] };
                    Debug.WriteLine("inserting " + i + " of " + chunks.Length);
                    await mapTable.InsertAsync(newMap);
                }

                maps = await mapTable.Where(p => p.Building == location.Building && p.Floor == location.Floor).ToListAsync();
                if (maps.Count() == 0)
                {
                    MessageBox.Show("Still wrong!");
                    return null;
                }
#endif
                }

                // Map is on the server
                string svg = string.Join("", maps.OrderBy(p => p.Part).Select(p => p.SVG));

                if (!store.DirectoryExists("Maps"))
                {
                    store.CreateDirectory("Maps");
                }

                var content2 = store.OpenFile(filename, FileMode.OpenOrCreate, FileAccess.Write);
                byte[] bytes = Encoding.UTF8.GetBytes(svg);
                content2.Write(bytes, 0, bytes.Length);
                content2.Flush();
                content2.Close();

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
                MessageBox.Show("You must log in. Login Required");
                return false;
            }

            // Is the user registered?
            var appUserTable = service.GetTable<AppUser>();
            var appUsers = await appUserTable.Where(p => p.UserId == service.CurrentUser.UserId).ToListAsync();
            if (appUsers.Count() != 1)
            {
                AppUser user = await GetRegistration();
                if (user != null)
                {
                    await appUserTable.InsertAsync(user);
                    // Reload the user
                    appUsers = await appUserTable.Where(p => p.UserId == service.CurrentUser.UserId).ToListAsync();
                }

                if (appUsers.Count() != 1)
                {
                    MessageBox.Show("registration failed");
                    return false;
                }
            }

            // Is authorized?
            var appUser = appUsers[0];
            if (!appUser.IsAuthorized)
            {
                appUser.IsAuthorized = true;
                await appUserTable.UpdateAsync(appUser);
                MessageBox.Show("You have not been authorized yet. Try again later");
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

            ListPicker listPicker = new ListPicker()
            {
                Header = "Available providers:",
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

            messageBox.Dismissing += (s1, e1) =>
            {
                if (listPicker.ListPickerMode == ListPickerMode.Expanded)
                {
                    e1.Cancel = true;
                }
            };

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

    }
}
