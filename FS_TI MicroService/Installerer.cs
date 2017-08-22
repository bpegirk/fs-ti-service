using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;

namespace FS_TI_MicroService
{
    [RunInstaller(true)]
    public partial class Installerer : System.Configuration.Install.Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;

        public Installerer()
        {
            InitializeComponent();

            serviceInstaller = new ServiceInstaller();
            processInstaller = new ServiceProcessInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = ServiceMain.srvTitle;
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
