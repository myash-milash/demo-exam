using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;

namespace DemoWpf
{
    /// <summary>
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        string connectionString = "Server=localhost;Database=demo;Uid=root;Pwd=root;";
        int selectedUserId = -1; // ID выбранного в таблице пользователя

        public AdminWindow()
        {
            InitializeComponent();
            LoadUsers();
        }
    
        private void LoadUsers()
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT idusers, login, password, isAdmin, isBlocked FROM users", connection);
            DataTable table = new DataTable();
            adapter.Fill(table);
            UsersGrid.ItemsSource = table.DefaultView;
        }
        //выбор строки, заполнение
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView row = UsersGrid.SelectedItem as DataRowView;
            if (row != null)
            {
                selectedUserId = Convert.ToInt32(row["idusers"]);
                LoginBox.Text = row["login"].ToString();
                PasswordBox.Text = row["password"].ToString();
                IsAdminCheck.IsChecked = Convert.ToBoolean(row["isAdmin"]);
                IsBlockedCheck.IsChecked = Convert.ToBoolean(row["isBlocked"]);
            }
        }
        // 3. Добавление нового пользователя с проверкой дубликата логина
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Text.Trim();

            if (login == "" || password == "")
            {
                MessageBox.Show("Заполните логин и пароль!");
                return;
            }

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            // Проверяем, есть ли уже такой логин
            MySqlCommand checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE login = @login", connection);
            checkCmd.Parameters.AddWithValue("@login", login);
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            {
                MessageBox.Show("Пользователь с таким логином уже существует!");
                connection.Close();
                return;
            }

            // Добавляем нового пользователя
            string sql = "INSERT INTO users (login, password, isAdmin, isBlocked) VALUES (@login, @pass, @admin, @blocked)";
            MySqlCommand insertCmd = new MySqlCommand(sql, connection);
            insertCmd.Parameters.AddWithValue("@login", login);
            insertCmd.Parameters.AddWithValue("@pass", password);
            insertCmd.Parameters.AddWithValue("@admin", IsAdminCheck.IsChecked == true ? 1 : 0);
            insertCmd.Parameters.AddWithValue("@blocked", IsBlockedCheck.IsChecked == true ? 1 : 0);

            insertCmd.ExecuteNonQuery();
            connection.Close();

            MessageBox.Show("Пользователь добавлен!");
            LoadUsers(); // Обновляем таблицу
        }

        // Изменение данных выбранного пользователя (включая снятие блокировки)
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUserId == -1)
            {
                MessageBox.Show("Сначала выберите пользователя в таблице!");
                return;
            }

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            string sql = "UPDATE users SET login = @login, password = @pass, isAdmin = @admin, isBlocked = @blocked WHERE idusers = @idusers";
            MySqlCommand updateCmd = new MySqlCommand(sql, connection);
            updateCmd.Parameters.AddWithValue("@login", LoginBox.Text.Trim());
            updateCmd.Parameters.AddWithValue("@pass", PasswordBox.Text.Trim());
            updateCmd.Parameters.AddWithValue("@admin", IsAdminCheck.IsChecked == true ? 1 : 0);
            updateCmd.Parameters.AddWithValue("@blocked", IsBlockedCheck.IsChecked == true ? 1 : 0);
            updateCmd.Parameters.AddWithValue("@idusers", selectedUserId);

            updateCmd.ExecuteNonQuery();
            connection.Close();

            MessageBox.Show("Данные обновлены!");
            LoadUsers(); 
        }

    }
}
