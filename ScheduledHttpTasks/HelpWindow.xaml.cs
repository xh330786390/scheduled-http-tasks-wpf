using System.Windows;

namespace ScheduledHttpTasks
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            LoadQrCodeImage();
        }

        private void LoadQrCodeImage()
        {
            try
            {
                // 从嵌入式资源加载二维码图片
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // 尝试不同的资源名称格式
                string[] possibleResourceNames = {
                    "ScheduledHttpTasks.resources.wxin_erweima.png",
                    "ScheduledHttpTasks.wxin_erweima.png",
                    "wxin_erweima.png"
                };
                
                System.IO.Stream resourceStream = null;
                string foundResourceName = null;
                
                foreach (var resourceName in possibleResourceNames)
                {
                    resourceStream = assembly.GetManifestResourceStream(resourceName);
                    if (resourceStream != null)
                    {
                        foundResourceName = resourceName;
                        break;
                    }
                }
                
                if (resourceStream != null)
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = resourceStream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    QrCodeImage.Source = bitmap;
                }
                else
                {
                    // 如果资源不存在，显示所有可用的资源名称用于调试
                    QrCodeImage.Source = null;
                    var allResources = assembly.GetManifestResourceNames();
                    string availableResources = string.Join("\n", allResources);
                    System.Windows.MessageBox.Show($"二维码图片资源不存在，尝试了以下名称:\n{string.Join("\n", possibleResourceNames)}\n\n可用的资源:\n{availableResources}", 
                        "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载二维码图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}