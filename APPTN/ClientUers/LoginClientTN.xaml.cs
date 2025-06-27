using ClientApp;
using System;
using System.Net.Sockets;
using System.Windows;

namespace QuizClient
{
    public partial class LoginClientTN : Window
    {
        public LoginClientTN()
        {
            InitializeComponent();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string playerName = txtPlayerName.Text.Trim();
            string ip = txtServerIP.Text.Trim();

            // Inline khai báo biến port ngay trong TryParse
            if (!int.TryParse(txtPort.Text.Trim(), out int port))
            {
                MessageBox.Show("Port không hợp lệ.");
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                MessageBox.Show("Vui lòng nhập tên người chơi.");
                return;
            }

            try
            {
                TcpClient client = new TcpClient();
                client.Connect(ip, port);

                // Mở giao diện chính (MainClient là cửa sổ đã thiết kế trước đó)
                MainClient mainWindow = new MainClient(playerName, ip, client);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối đến server: " + ex.Message);
            }
        }
    }
}
