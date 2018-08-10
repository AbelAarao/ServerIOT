using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Timers;
using System.Runtime.InteropServices;

using System.Configuration;



namespace ServicoServidorSIS
{


    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public long dwServiceType;
        public ServiceState dwCurrentState;
        public long dwControlsAccepted;
        public long dwWin32ExitCode;
        public long dwServiceSpecificExitCode;
        public long dwCheckPoint;
        public long dwWaitHint;
    };

    // **************** Classe Principal ***********************

    public partial class ServicoServidorSIS : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        public static Thread SocketThread;
        public static Thread SocketUDPThread;

        public ServicoServidorSIS()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("ServidorSIS"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "ServidorSIS", "ServidorSISLog");
            }
            eventLog1.Source = "ServidorSIS";
            eventLog1.Log = "ServidorSISLog";

            eventLog1.WriteEntry("**** Início Servidor SIS -- ServicoServidorSIS.ServicoServidorSIS");

        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("ServicoServidorSIS OnStart.");

            // Update the service state to Start Pending.  
            //--> Status iniciando o processamento do OnStart
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            //<----

            //--> Timer 
            // Set up a timer to trigger every minute.  
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 3600000; // = 1 Hora    90000 = 90 seconds  	
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
            //<-- Timer

            //*************************************************************************************
            //-->> INCLUIR AQUI TODA A INICIALIZAÇÃO DO SERVIÇO
            // Inicia o Servidor de TCP

            SocketServer.LogEventos = eventLog1;

            //SocketServer.NumeroPortaTCP = "52048";

            SocketServer.NumeroPortaTCP = ReadSetting("PortaTCP");
            eventLog1.WriteEntry("Porta TCP=" + SocketServer.NumeroPortaTCP , EventLogEntryType.Information);

            //Data Source=10.21.201.11\SQLEXPRESS;User Id=sa;Password=Guara284;Initial Catalog=Catodica;
            //SocketServer.ConectionString = "Data Source=DESKTOP-E8Q00J5\\SQLEXPRESS; Initial Catalog=Catodica; Integrated Security=True";

            String cs = ConfigurationManager.ConnectionStrings["StringDeConexao"].ConnectionString;

            SocketServer.ConectionString = cs;

            eventLog1.WriteEntry("ConnectionString=" + cs, EventLogEntryType.Information);

            //SocketServer.ConectionString = @"Data Source = SRVALPHAOUT; User Id = sa; Password = sis@123; Initial Catalog = Catodica;";

            SocketThread = new Thread(SocketServer.StartListening);

            //To put in form load event, run IPsFunction.InterfaceIPs() before thread start, SocketThread.Start();.
            SocketThread.Start(); //To put in form function 

            //<--
            //*************************************************************************************

            // Inicia o Servidor UDP
            

            if (ReadSetting("LigaServidorUDP") == "sim")
            {
                eventLog1.WriteEntry("Servidor UDP Ligado!");

                ServerUDP.serverUDP = ReadSetting("serverUDP");
                ServerUDP.portaUDP = ReadSetting("portaUDP");

                SocketUDPThread = new Thread(new ThreadStart(ServerUDP.StartListening));
                SocketUDPThread.Start();

            }
            else
                eventLog1.WriteEntry("Servidor UDP Desligado!");

            // ******

           // Update the service state to Running.  
            //--> Finalizando o processamento do OnStart
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            //<--

            eventLog1.WriteEntry("Saindo do OnStart.");
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("ServicoServidorSIS OnStop.");

            SocketThread.Resume();

            SocketUDPThread.Resume();


        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.  
            eventLog1.WriteEntry("ServidorSIS Pulso.", EventLogEntryType.Information);
        }

        public string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                eventLog1.WriteEntry("Erro no retorno da chave:" + key + " do App.config", EventLogEntryType.Error);
                return null;
            }

        }
    }
}
