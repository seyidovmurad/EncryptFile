using Microsoft.Win32;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace EncryptFile
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public string FilePath { get; set; }

        public bool IsEncrypt { get; set; }

        public int ProgressPercentage { get; set; }

        Action cancelOperation;

        public MainWindow()
        {
            InitializeComponent();
            IsEncrypt = true;
            StartBtn.IsEnabled = false;
            DataContext = this;
        }

        private void EncryptDecryptFile(CancellationToken token)
        {
            var salt = !string.IsNullOrEmpty(Password.Password) ? Password.Password: "a6D53";

            string tempFilePath;
            try
            {
                tempFilePath = CreateTempFile(FilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            byte[] buffer = new byte[50];

            using (FileStream source = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                long fileLength = source.Length;
                using (FileStream dest = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    long totalBytes = 0;
                    int currentBlockSize = 0;

                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalBytes += currentBlockSize;
                        ProgressPercentage = (int)(totalBytes * 100.0 / fileLength);

                        Thread.Sleep(200);
                        dest.Write(EncryptDecrypt(buffer, salt), 0, currentBlockSize);

                        if(token.IsCancellationRequested)
                            break;
                    }
                }
            }

            if(token.IsCancellationRequested)
            {
                File.WriteAllText(FilePath, File.ReadAllText(tempFilePath));
                MessageBox.Show("File encryption\\decryption cancelled.");
            }
            else
            {
                MessageBox.Show($"File Encrypted\\Decrypted successfully");
            }

            ProgressPercentage = 0;
            File.Delete(tempFilePath);
            cancelOperation = null;
        }

        private byte[] EncryptDecrypt(byte[] text, string key)
        {
            var result = new StringBuilder();
            var temp = Encoding.UTF8.GetString(text);

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)temp[c] ^ (uint)key[c % key.Length]));

            return Encoding.UTF8.GetBytes(result.ToString());
        }

        private string GetTempFilePath(string path)
        {
            var folders = path.Split('\\').ToList();

            string temp = "";

            folders.RemoveAt(folders.Count - 1);
            foreach (var item in folders)
            {
                temp += item + "\\";
            }
            temp += "temp.txt";

            return temp;
        }

        private string CreateTempFile(string path)
        {
            string allData = File.ReadAllText(path);

            string tempFilePath = GetTempFilePath(path);

            File.WriteAllText(tempFilePath, allData);

            return tempFilePath;
        }




        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            cancelOperation?.Invoke();
            cancelOperation = null;
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "txt files (*.txt)|*.txt";

            if (fileDialog.ShowDialog() == true)
                FilePath = fileDialog.FileName;
        }

        

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            var cancellationToken = new CancellationTokenSource();
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                EncryptDecryptFile(cancellationToken.Token);
                cancellationToken.Dispose();
            });

            cancelOperation = new Action(() => cancellationToken.Cancel());            
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(sender is TextBox tb)
            {
                if (string.IsNullOrEmpty(tb.Text) || string.IsNullOrWhiteSpace(tb.Text))
                    StartBtn.IsEnabled = false;
                else
                    StartBtn.IsEnabled = true;
            }
        }
    }
}
