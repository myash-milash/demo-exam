using MySql.Data.MySqlClient;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
namespace DemoWpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            puzzleImages = new Image[] { Img0, Img1, Img2, Img3 };
            ShufflePuzzle();
        }
        string connectionString = "Server=localhost;Database=demo;Uid=root;Pwd=root;";
        //Блок с каптчой 
        Image[] puzzleImages;
        int[] currentOrder = { 0, 1, 2, 3 }; // Текущий порядок фрагментов
        Image firstSelected = null;          // Первый выбранный фрагмент
        int failedAttempts = 0;              // Счётчик ошибок подряд
      //перемешивание паззла 
        private void ShufflePuzzle()
        {
            Random rnd = new Random();
            do
            {
                currentOrder = currentOrder.OrderBy(x => rnd.Next()).ToArray();
            }
            while (IsPuzzleSolved());

            UpdatePuzzleUI();
        }
       // обновление картинок
        private void UpdatePuzzleUI()
        {
            for (int i = 0; i < 4; i++)
            {
                int fileNumber = currentOrder[i] + 1; // 1.png, 2.png, 3.png, 4.png
                puzzleImages[i].Source = new BitmapImage(new Uri($"pack://application:,,,/Images/{fileNumber}.png"));
                puzzleImages[i].Opacity = 1.0;
            }
        }

        // обмен местами
        private void PuzzlePiece_Click(object sender, MouseButtonEventArgs e)
        {
            Image clicked = sender as Image;

            if (firstSelected == null)
            {
                firstSelected = clicked;
                firstSelected.Opacity = 0.5; // выделение полупрозрачностью
            }
            else
            {
                int index1 = Array.IndexOf(puzzleImages, firstSelected);
                int index2 = Array.IndexOf(puzzleImages, clicked);

                int temp = currentOrder[index1];
                currentOrder[index1] = currentOrder[index2];
                currentOrder[index2] = temp;

                firstSelected = null;
                UpdatePuzzleUI();
            }
        }
        //проверка правильности 
        private bool IsPuzzleSolved()
        {
            return currentOrder[0] == 0 && currentOrder[1] == 1 &&
                   currentOrder[2] == 2 && currentOrder[3] == 3;
        }
        
        //блок авторизации

        private bool CheckUser(string login, string password)
        { //  подключение к БД
            MySqlConnection connection = new MySqlConnection(connectionString);

            string sql = "SELECT COUNT(*) FROM users WHERE login = @login AND password = @password";
            MySqlCommand command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", password);
           
            connection.Open();
            int count = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();

            return count > 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string userLogin = LoginTextBox.Text;
            string userPassword = PasswordBox.Password;

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            // Проверка заблокирован ли пользователь 
            MySqlCommand checkBlock = new MySqlCommand("SELECT COUNT(*) FROM users WHERE login = @login AND isBlocked = 1", connection);
            checkBlock.Parameters.AddWithValue("@login", userLogin);

            if (Convert.ToInt32(checkBlock.ExecuteScalar()) > 0)
            {
                MessageBox.Show("Вы заблокированы. Обратитесь к администратору");
                connection.Close();
                return; 
            }

            //проверка
            if (CheckUser(userLogin, userPassword) && IsPuzzleSolved())
            {
                failedAttempts = 0;

                MySqlCommand adminCmd = new MySqlCommand("SELECT isAdmin FROM users WHERE login = @login", connection);
                adminCmd.Parameters.AddWithValue("@login", userLogin);
                bool isAdmin = Convert.ToBoolean(adminCmd.ExecuteScalar());

                connection.Close();
                MessageBox.Show("Успешный вход!");

                if (isAdmin)
                {
                    AdminWindow adminWin = new AdminWindow();
                    adminWin.Show();
                }
                else
                {
                    Main window = new Main();
                    window.Show();
                }
             
                this.Close();
            }
            else
            {
                failedAttempts++; // Прибавляем ошибку
                ShufflePuzzle();  // Перемешиваем пазл

                if (failedAttempts >= 3)
                {
                    // блокировка
                    MySqlCommand blockCmd = new MySqlCommand("UPDATE users SET isBlocked = 1 WHERE login = @login", connection);
                    blockCmd.Parameters.AddWithValue("@login", userLogin);
                    blockCmd.ExecuteNonQuery();

                    MessageBox.Show("Вы заблокированы. Обратитесь к администратору");
                }
                else
                {
                    MessageBox.Show("Неверный логин, пароль или пазл");
                }
            }

            connection.Close();
        }
    }
}
