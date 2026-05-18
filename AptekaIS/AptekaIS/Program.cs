using AptekaIS.Data;
using AptekaIS.Forms;

namespace AptekaIS;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.Initialize();
        Application.Run(new LoginForm());
    }
}
