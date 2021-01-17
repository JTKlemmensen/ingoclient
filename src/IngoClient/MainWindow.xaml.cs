using IngoClient.Interfaces;
using IngoClient.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IngoClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IConsole
    {
        private IIngoClient client;
        private List<User> Users { get; set; } = new List<User>();
        public MainWindow()
        {
            InitializeComponent();
            client = new IngoClientClass(this, new HttpWrapper());

            if (File.Exists("users.txt"))
                Users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText("users.txt"));

            var info = client.GetGameSession();
            client.Play(Users.ElementAt(2), info);
            SaveUsers();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // generate ids and print to console
            foreach (var user in Users)
            {
                client.GenerateGameToken(user);
                Task.Delay(1000).Wait();
            }

            SaveUsers();
        }

        public void Write<T>(T obj)
        {
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            Console.Text += JsonConvert.SerializeObject(obj, settings) + "\n";
        }

        public void WriteLine(string str)
        {
            Console.Text += str + "\n";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var user = new User
            {
                Username=Username.Text,
                Password=Password.Text,
                DeviceName=DeviceName.Text
            };
            Users.Add(user);
            client.Login(user);

            SaveUsers();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            client.Confirm(Code.Text);
            SaveUsers();
        }

        private void SaveUsers()
        {
            File.WriteAllText("users.txt", JsonConvert.SerializeObject(Users));
        }
    }
}
