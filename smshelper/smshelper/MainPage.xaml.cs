using Microsoft.Phone.Controls;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;

namespace SmsHelper
{
    public class TodoItem
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }
        [JsonProperty(PropertyName = "from")]
        public string From { get; set; }


        [JsonProperty(PropertyName = "sent")]
        public DateTimeOffset Sent { get; set; }

    }

    public class MyText
    {
        private readonly TodoItem source;
        private static string[] myNumbers = new string[] { "+14259472529", "425-947-2529" };
        private static string[] filterOut = new string[] { "201351", "+1712382" };

        public MyText(TodoItem source)
        {
            this.source = source;
        }

        public string Phone
        {
            get
            {
                return !this.IsFromMe ? this.source.From : this.source.To;
            }
        }

        public string Message
        {
            get { return this.source.Text; }
        }

        public string Sent
        {
            get
            {
                var localtime = this.source.Sent.ToLocalTime().DateTime;
                return localtime.ToString("M/d HH:mm");
            }
        }

        public string Rendered
        {
            get { return this.Sent + ": " + this.Message; }
        }

        public bool IsFromMe
        {
            get
            {
                return myNumbers.Contains(this.source.From);
            }
        }

        public bool IsHidden
        {
            get
            {
                if (this.Phone == null) return false;
                var ret = filterOut.Any(p => this.Phone.Contains(p));
                Debug.WriteLine(this.Phone + " : " + ret);
                return ret;
            }
        }
    }

    public sealed class BooleanToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool && (bool)value) ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is HorizontalAlignment && (HorizontalAlignment)value != HorizontalAlignment.Left;
        }
    }
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BooleanToBrushConverter : IValueConverter
    {
        private static Brush TrueBrush = new SolidColorBrush(Colors.DarkGray);
        private static Brush FalseBrush = new SolidColorBrush(Colors.Green);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        private ObservableCollection<MyText> items = new ObservableCollection<MyText>();

        private IMobileServiceTable<TodoItem> todoTable = App.MobileService.GetTable<TodoItem>();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private async void InsertTodoItem(TodoItem todoItem)
        {
            // This code inserts a new TodoItem into the database. When the operation completes
            // and Mobile Services has assigned an Id, the item is added to the CollectionView
            await todoTable.InsertAsync(todoItem);
            items.Add(new MyText(todoItem));
        }

        private async void RefreshTodoItems()
        {
            // This code refreshes the entries in the list view be querying the TodoItems table.
            // The query excludes completed TodoItems
            try
            {
                var tmp = await todoTable.ToCollectionAsync();
                items.Clear();
                foreach (var t in tmp)
                {
                    items.Add(new MyText(t));
                }
            }
            catch (MobileServiceInvalidOperationException e)
            {
                MessageBox.Show(e.Message, "Error loading items", MessageBoxButton.OK);
            }

            ListItems.ItemsSource = items;
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshTodoItems();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var todoItem = new TodoItem { Text = TodoInput.Text, To = this.Phone.Text };
            InsertTodoItem(todoItem);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RefreshTodoItems();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            var t = b.DataContext as MyText;
            this.Phone.Text = t.Phone ?? "";
        }
    }
}