
using System.Windows;
using System.Windows.Input;

namespace NotKosem
{
    public partial class MainWindow : Window
    {
        // Pencere oluşturulduğunda çalışır.
        public MainWindow()
        {
            InitializeComponent();
        }

        // Başlık alanına basılı tutarak pencereyi sürüklememizi sağlar.
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Mouse sol tuşu basılıysa pencereyi sürükle.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Kapatma butonuna tıklandığında pencereyi kapatır.
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Pencerenin diğer pencerelerin üzerinde kalma özelliğini açıp kapatır.
        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }
    }
}
