using System.Windows;

namespace CameraVision2
{
    public partial class App : Application
    {
        void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
        }
    }
}
