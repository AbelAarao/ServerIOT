using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Data.SqlClient;


using System.Diagnostics;



namespace ServicoServidorSIS
{

    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;

        // Size of receive buffer.
        public const int BufferSize = 1024;

        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class SocketServer
    {
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static StateObject state;
        public static Socket listener;


        public static String NumeroPortaTCP;
        public static String ConectionString;
        private static Decimal StopServer = 0;
        public static String RetransmitePorUDP;

        public static EventLog LogEventos;            


        private static int _port = 52048;
        public int Porta
        {
            get { return _port; }
            set { _port = value; }
        }

        private string _host = "localhost";
        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        private object _frmMain;
        public object RemoteFrm
        {
            get { return _frmMain; }
            set { _frmMain = value; }
        }

        private static void LogaMsg(String sMsg)
        {

            LogEventos.WriteEntry(sMsg, EventLogEntryType.Information); 
        }

        private static void LogaMsg(String sMsg, Int32  TipoEvento)
        {


            switch (TipoEvento)
            {
                case 1:
                    // Write an 'Error' entry in specified log of event log.
                    LogEventos.WriteEntry(sMsg, EventLogEntryType.Error);
                    break;
                case 2:
                    // Write a 'Warning' entry in specified log of event log.
                    LogEventos.WriteEntry(sMsg, EventLogEntryType.Warning);
                    break;
                case 3:
                    // Write an 'Information' entry in specified log of event log.
                    LogEventos.WriteEntry(sMsg, EventLogEntryType.Information);
                    break;
                case 4:
                    // Write a 'FailureAudit' entry in specified log of event log.
                    LogEventos.WriteEntry(sMsg, EventLogEntryType.FailureAudit);
                    break;
                case 5:
                    // Write a 'SuccessAudit' entry in specified log of event log.
                    LogEventos.WriteEntry(sMsg, EventLogEntryType.SuccessAudit);
                    break;
                default:
                   
                    break;
            }

        }

        public SocketServer()
        {

        }

        public  static void StartListening()
        {
            LogaMsg("StartListening");

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".

#pragma warning disable CS0618 // O tipo ou membro é obsoleto
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
#pragma warning restore CS0618 // O tipo ou membro é obsoleto

            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(NumeroPortaTCP));

            LogaMsg("IP Servidor:[" + ipAddress +"]");

            // Create a TCP/IP socket.
             listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                StopServer = 0;

                while (StopServer == 0)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    LogaMsg("Waiting for a connection...");
                    

                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: StartListening " + s,1);
                return;
            }

            LogaMsg("Saiu: StartListening");
        }

        public static void FStopServer()
        {
            StopServer = 1;
            listener.Close();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {

      
                // Signal the main thread to continue.
                allDone.Set();

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                IPEndPoint remoteIpEndPoint = handler.RemoteEndPoint as IPEndPoint;
                System.Net.IPAddress lip = System.Net.IPAddress.Parse(remoteIpEndPoint.Address.ToString());
                LogaMsg("***********************************************");
                LogaMsg("CONECTOU IPAddres da Remota:(" + remoteIpEndPoint.Address + ")");
                //LogaMsg("MAC Address:(" + MacAddress.GetMacFromIP(lip).ToString() + ")");

                // Create the state object.
                state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: AcceptCallback " + s, 1);
            }

        }

        public static void ReadCallback(IAsyncResult ar)
        {
//            LogaMsg("ReadCallback...");


            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state2 = (StateObject)ar.AsyncState;
            Socket handler = state2.workSocket;

            try
            {

                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.
                    //state2.sb.Append(Encoding.ASCII.GetString(state2.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    //content = state2.sb.ToString();

                    content = Encoding.ASCII.GetString(state2.buffer, 0, bytesRead);

                    // All the data has been read from the 
                    // client. Display it on the console.
                    //LogaMsg("Read {0} bytes from socket. \n Data : {1}", content.Length, content);



                    TrataLeitura(handler,content);

                    // Echo the data back to the client.
                    //Send(handler, content);

                    // Not all data received. Get more.
                    handler.BeginReceive(state2.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state2);

                }

            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: ReadCallBack " + s, 1);

            }



        }

        public static void Send(Socket handler, String data)
        {
           LogaMsg("Send Tam(" + data.Length + ") Buffer:[" + data + "]" );

            try
            {
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: Send " + s, 1);
            }


        }
        


        public static void Send2( String data)
        {
            LogaMsg("Send:" + data);

            try
            {
                Socket handler = state.workSocket;

                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: Send2 " + s, 1);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            //LogaMsg("SendCallBack");

            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                //LogaMsg("Sent {0} bytes to client.", bytesSent);

                //handler.Shutdown(SocketShutdown.Both);
               // handler.Close();

            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: SendCallback " + s, 1);
            }
        }

        /*
                public static int Main(String[] args)
                {
                    StartListening();
                    return 0;
                }
        */
        public static void TrataLeitura(Socket handler, String content)
        {

            try
            {

                IPEndPoint remoteIpEndPoint = handler.RemoteEndPoint as IPEndPoint;

                System.Net.IPAddress lip = System.Net.IPAddress.Parse(remoteIpEndPoint.Address.ToString());

                LogaMsg("IPAddres da Remota:(" + remoteIpEndPoint.Address + ")");
                //LogaMsg(" MAC Address:(" + MacAddress.GetMacFromIP(lip).ToString() + ")");

                LogaMsg("Tamanho Pacote:(" + content.Length + ")");
                LogaMsg("FrameTotal: --[" + content + "]--");

                LogaMsg("Chamando GravaLeitura.");
                GravaLeitura(content); // Grava leitura no BD

                // verifica se é para retransmitir para a nuvem SIS
                if(RetransmitePorUDP == "sim")
                {
                    LogaMsg("Retransmitindo para servidor UDP Buffer:" + content);
                    ServerUDP.sendUDP(content);

                }

                LogaMsg("Chamando MontaRemota.");
                RemotaItallux LeituraRecebida = MontaRemota(content);

                LogaMsg("Chamando ObterRemmotaPorImei.");
                RemotaItallux rmi = ObterRemmotaPorImei(LeituraRecebida.IMEI);

                if (rmi.FlagConf == 99)
                {
                    LogaMsg("Remota não existe na base, salvar a recebida.");
                    LeituraRecebida.FlagConf = 0;
                    rmi = SalvaStatusRemota(LeituraRecebida);
                    
                }

                switch (rmi.FlagConf)
                {
                    case 0:
                        LogaMsg("Enviando Resposta Ok:1");
                        // respodne "1" = OK
                        LeituraRecebida.FlagConf = 0;
                        RemotaItallux rriC = AlteraStatusRemota(LeituraRecebida);
                        Send(handler, "1");
                        break;

                    case 1: // envia configuração


                        // restposta para configurar
                        String cIu;

                        //cIu = "2+0220+0110+1023+0100+1022+0080+1020+0070\r\n";

                        cIu = "2";
                        cIu = cIu + "+" + rmi.SetPointAltoCanal1;  // VCA Tensão de Entrada  de 0 a 1023 
                        cIu = cIu + "+" + rmi.SetPointBaixoCanal1; 
                        cIu = cIu + "+" + rmi.SetPointAltoCanal2;  // VCC Semi célula de 0 a -1023
                        cIu = cIu + "+" + rmi.SetPointBaixoCanal2;
                        cIu = cIu + "+" + rmi.SetPointAltoCanal3;  // VCC Tensão de Saída de 0 a 1023
                        cIu = cIu + "+" + rmi.SetPointBaixoCanal3;
                        cIu = cIu + "+" + rmi.SetPointAltoCanal4;   // ICC Corrente de Saída de 0 a 102,3
                        cIu = cIu + "+" + rmi.SetPointBaixoCanal4 + "\r\n";


                        LogaMsg("Enviando Configuração: --[" + cIu + "]--");

                        // respodne "2" = OK e envia nova configuração
                        Send(handler, cIu);

                        rmi.FlagConf = 2; // configuração enviada para Remota
                        rriC = AlteraStatusRemota(rmi);

                        break;

                    case 2: // compara o que chegou com o que está no Status da BAse


                        if( LeituraRecebida.SetPointAltoCanal1 == rmi.SetPointAltoCanal1 &&
                            LeituraRecebida.SetPointBaixoCanal1 == rmi.SetPointBaixoCanal1 &&
                            LeituraRecebida.SetPointAltoCanal2 == rmi.SetPointAltoCanal2 &&
                            LeituraRecebida.SetPointBaixoCanal2 == rmi.SetPointBaixoCanal2 &&
                            LeituraRecebida.SetPointAltoCanal3 == rmi.SetPointAltoCanal3 &&
                            LeituraRecebida.SetPointBaixoCanal3 == rmi.SetPointBaixoCanal3 &&
                            LeituraRecebida.SetPointAltoCanal4 == rmi.SetPointAltoCanal4 &&
                            LeituraRecebida.SetPointBaixoCanal4 == rmi.SetPointBaixoCanal4 )
                        {
                            LogaMsg("Configuração recebida igual a enviada anteriormente, OK!");
                            Send(handler, "1");
                        }else
                        {
                            LogaMsg("ERRO!!! Configuração recebida diferente da enviada anteriormente!");
                            Send(handler, "0"); // recebi errado
                        }

                        LeituraRecebida.FlagConf = 0; // configuração enviada para Remota
                        rriC = AlteraStatusRemota(LeituraRecebida);

                        break;


                }


            }
            catch
            {
                LogaMsg("ERRO no TrataLeitura...");
            }




        }




        public static void GravaLeitura(String buffer)
        {

            //SqlConnection conn = new SqlConnection(@"Data Source=DESKTOP-E8Q00J5\SQLEXPRESS;Initial Catalog=catodica;Integrated Security=True;");
            //SqlConnection conn = new SqlConnection(@"Data Source=10.21.201.11\SQLEXPRESS;User Id=sisteste;Password=sis@12345;Initial Catalog=catodica;Integrated Security=True");

            SqlConnection conn = new SqlConnection(ConectionString);

            DateTime date1 = DateTime.Now;
            String datahora = date1.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("pt-BR"));

            string sql = "INSERT INTO leituraSIS(DataHora, FrameTotal) VALUES ('" + datahora + "','" + buffer + "')";


            //string sql = "select * from leitura ";



            try
            {
                //Cria uma objeto do tipo comando passando como parametro a string sql e a string de conexão
                SqlCommand comando = new SqlCommand(sql, conn);
                //Adicionando o valor das textBox nos parametros do comando

                /***
                comando.Parameters.Add(new SqlParameter("@nome", this.txtNome.Text));
                comando.Parameters.Add(new SqlParameter("@endereco", this.txtEndereco.Text));
                comando.Parameters.Add(new SqlParameter("@numero", this.txtNumero.Text));
                comando.Parameters.Add(new SqlParameter("@RG", this.txtRG.Text));
                ***/

                //abre a conexao
                conn.Open();

                //executa o comando com os parametros que foram adicionados acima
                comando.ExecuteNonQuery();

                //fecha a conexao
                conn.Close();


                //Minha função para limpar os textBox
                //LimpaCamos();


                //Abaixo temos a ultlização de javascript para apresentar ao usuário um alert
                // referente ao msgbox
                //RegisterClientScriptBlock("cadastrado", "<script>alert(Operação concluida !)</script>");
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }

                LogaMsg(errorMessages.ToString());
            }
            finally
            {
                conn.Close();
            }
        }

        // Consulta se no cadastro da Remota foi ligado o Flag Configurar. Se sim le os SetPoints
        public static int ConsultaConfPendente(String IMEI)
        {

            try
            {

                RemotaItallux rm;

                rm = ObterRemmotaPorImei(IMEI);

                return rm.FlagConf;

            }
            catch (Exception e)
            {
                String s = e.ToString();
                LogaMsg("ERRO: ConsultaConfPendente " + s, 1);
                return 0;
            }

        }

        public static RemotaItallux ObterRemmotaPorImei(String IMEI)
        {
            RemotaItallux remota = new RemotaItallux();
            SqlConnection conn = new SqlConnection(ConectionString);

            DateTime date1 = DateTime.Now;
            String datahora = date1.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("pt-BR"));

            //string sql = "INSERT INTO leituraSIS(DataHora, FrameTotal) VALUES ('" + datahora + "','" + buffer + "')";

            string sql = "SELECT * from [catodica].[dbo].[Remota] WHERE imei = '" + IMEI + "'";



            try
            {
                //Cria uma objeto do tipo comando passando como parametro a string sql e a string de conexão
                SqlCommand cmd = new SqlCommand(sql, conn);

                //abre a conexao
                conn.Open();

                //executa o comando com os parametros que foram adicionados acima
                //SqlDataReader rdr = comando.ExecuteNonQuery();

                SqlDataReader rdr = cmd.ExecuteReader();
                if(rdr.HasRows)
                {

                    while (rdr.Read())
                    {
                        remota.IMEI = rdr["IMEI"].ToString();
                        remota.FlagConf = Convert.ToInt32(rdr["FlagConf"].ToString());
                        remota.Canal1 = rdr["Canal1"].ToString();
                        remota.Canal2 = rdr["Canal2"].ToString();
                        remota.Canal3 = rdr["Canal3"].ToString();
                        remota.Canal4 = rdr["Canal4"].ToString();
                        remota.Alarmes = rdr["Alarmes"].ToString();
                        remota.SetPointAltoCanal1 = rdr["SetPointAltoCanal1"].ToString();
                        remota.SetPointBaixoCanal1 = rdr["SetPointBaixoCanal1"].ToString();
                        remota.SetPointAltoCanal2 = rdr["SetPointAltoCanal2"].ToString();
                        remota.SetPointBaixoCanal2 = rdr["SetPointBaixoCanal2"].ToString();
                        remota.SetPointAltoCanal3 = rdr["SetPointAltoCanal3"].ToString();
                        remota.SetPointBaixoCanal3 = rdr["SetPointBaixoCanal3"].ToString();
                        remota.SetPointAltoCanal4 = rdr["SetPointAltoCanal4"].ToString();
                        remota.SetPointBaixoCanal4 = rdr["SetPointBaixoCanal4"].ToString();
                    }
                }
                else
                {

                    LogaMsg("Remota não encontrada na base!");
                    remota.FlagConf = 99; // remota nao existe
                }

                //fecha a conexao
                conn.Close();
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }

                SocketServer.LogaMsg(errorMessages.ToString(), 1);
                return null;
            }
            finally
            {
                conn.Close();
            }

            return remota;
        }

        public static RemotaItallux AlteraStatusRemota(RemotaItallux ri)
        {
            SqlConnection conn = new SqlConnection(ConectionString);

            DateTime date1 = DateTime.Now;
            String datahora = date1.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("pt-BR"));
            //String datahora = date1.ToString();

            /*
                        String sql = @"UPDATE dbo.Remota  SET  DataHora = '@DataHora',
                                  FlagConf = @FlagConf,
                                  Canal1 = '@Canal1',
                                  Canal2 = '@Canal2', 
                                  Canal3 = '@Canal3', 
                                  Canal4 = '@Canal4',
                                  Alarmes = '@Alarmes',
                                  SetPointAltoCanal1 = '@SetPointAltoCanal1',
                                  SetPointBaixoCanal1 = '@SetPointBaixoCanal1', 
                                  SetPointAltoCanal2 = '@SetPointAltoCanal2', 
                                  SetPointBaixoCanal2 = '@SetPointBaixoCanal2',
                                  SetPointAltoCanal3 = '@SetPointAltoCanal3', 
                                  SetPointBaixoCanal3 = '@SetPointBaixoCanal3', 
                                  SetPointAltoCanal4 = '@SetPointAltoCanal4', 
                                  SetPointBaixoCanal4 = '@SetPointBaixoCanal4' 
                             WHERE Imei = '@Imei2'";
            */

            String sql = @"UPDATE [catodica].[dbo].[Remota]  SET FlagConf = " + ri.FlagConf + "," +
                      "Canal1 =  '" + ri.Canal1 + "', " +
                      "Canal2 = '" + ri.Canal2 + "', " +
                      "Canal3 = '" + ri.Canal3 + "', " +
                      "Canal4 = '" + ri.Canal4 + "', " +
                      "Alarmes = '" + ri.Alarmes + "', " +
                      "SetPointAltoCanal1 = '" + ri.SetPointAltoCanal1 + "', " +
                      "SetPointBaixoCanal1 = '" + ri.SetPointBaixoCanal1 + "', " +
                      "SetPointAltoCanal2 = '" + ri.SetPointAltoCanal2 + "', " +
                      "SetPointBaixoCanal2 = '" + ri.SetPointBaixoCanal2 + "', " +
                      "SetPointAltoCanal3 = '" + ri.SetPointAltoCanal3 + "', " +
                      "SetPointBaixoCanal3 = '" + ri.SetPointBaixoCanal3 + "', " +
                      "SetPointAltoCanal4 = '" + ri.SetPointAltoCanal4 + "', " +
                      "SetPointBaixoCanal4 = '" + ri.SetPointBaixoCanal4 + "' " +
                      " where Imei = '" + ri.IMEI + "'";

            //sql = "UPDATE dbo.Remota SET  FlagConf = 2  Where Imei = '356889014766213'";

            try
            {
                //abre a conexao
                conn.Open();

                //Cria uma objeto do tipo comando passando como parametro a string sql e a string de conexão
                SqlCommand cmd = new SqlCommand(sql, conn);


                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }

                SocketServer.LogaMsg(errorMessages.ToString(), 1);
                return null;
            }
            finally
            {
                conn.Close();
            }

            return ri;
        }

        public static RemotaItallux SalvaStatusRemota(RemotaItallux ri)
        {

            SqlConnection conn = new SqlConnection(ConectionString);

            DateTime date1 = DateTime.Now;
            String datahora = date1.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("pt-BR"));
            //String datahora = date1.ToString();


            String sql = @"INSERT INTO [catodica].[dbo].[Remota] (
                                               Imei
                                               ,DataHora
                                               ,FlagConf
                                               ,Canal1
                                               ,Canal2
                                               ,Canal3
                                               ,Canal4
                                               ,Alarmes
                                               ,SetPointAltoCanal1
                                               ,SetPointBaixoCanal1
                                               ,SetPointAltoCanal2
                                               ,SetPointBaixoCanal2
                                               ,SetPointAltoCanal3
                                               ,SetPointBaixoCanal3
                                               ,SetPointAltoCanal4
                                               ,SetPointBaixoCanal4
                                                            )VALUES (
                                                                    @Imei, 
                                                                    @DataHora, 
                                                                    @FlagConf, 
                                                                    @Canal1, 
                                                                    @Canal2, 
                                                                    @Canal3, 
                                                                    @Canal4, 
                                                                    @Alarmes, 
                                                                    @SetPointAltoCanal1, 
                                                                    @SetPointBaixoCanal1, 
                                                                    @SetPointAltoCanal2, 
                                                                    @SetPointBaixoCanal2, 
                                                                    @SetPointAltoCanal3, 
                                                                    @SetPointBaixoCanal3, 
                                                                    @SetPointAltoCanal4, 
                                                                    @SetPointBaixoCanal4)";



            try
            {
                //abre a conexao
                conn.Open();

                //Cria uma objeto do tipo comando passando como parametro a string sql e a string de conexão
                SqlCommand cmd = new SqlCommand(sql, conn);


                cmd.Parameters.AddWithValue("@Imei", ri.IMEI);
                cmd.Parameters.AddWithValue("@DataHora", ri.DataHoraUltimoAlteração);


                cmd.Parameters.AddWithValue("@FlagConf", 0);


                cmd.Parameters.AddWithValue("@Canal1", ri.Canal1);
                cmd.Parameters.AddWithValue("@Canal2", ri.Canal2);
                cmd.Parameters.AddWithValue("@Canal3", ri.Canal3);
                cmd.Parameters.AddWithValue("@Canal4", ri.Canal4);
                cmd.Parameters.AddWithValue("@Alarmes", ri.Alarmes);

                cmd.Parameters.AddWithValue("@SetPointAltoCanal1", ri.SetPointAltoCanal1);
                cmd.Parameters.AddWithValue("@SetPointBaixoCanal1", ri.SetPointBaixoCanal1);
                cmd.Parameters.AddWithValue("@SetPointAltoCanal2", ri.SetPointAltoCanal2);
                cmd.Parameters.AddWithValue("@SetPointBaixoCanal2", ri.SetPointBaixoCanal2);
                cmd.Parameters.AddWithValue("@SetPointAltoCanal3", ri.SetPointAltoCanal3);
                cmd.Parameters.AddWithValue("@SetPointBaixoCanal3", ri.SetPointBaixoCanal3);
                cmd.Parameters.AddWithValue("@SetPointAltoCanal4", ri.SetPointAltoCanal4);
                cmd.Parameters.AddWithValue("@SetPointBaixoCanal4", ri.SetPointBaixoCanal4);



                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                StringBuilder errorMessages = new StringBuilder();

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorMessages.Append("Index #" + i + "\n" +
                        "Message: " + ex.Errors[i].Message + "\n" +
                        "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                        "Source: " + ex.Errors[i].Source + "\n" +
                        "Procedure: " + ex.Errors[i].Procedure + "\n");
                }

                SocketServer.LogaMsg(errorMessages.ToString(), 1);
                return null;
            }
            finally
            {
                conn.Close();
            }

            return ri;
        }
        /* 0               16           29   34     41   46    5254 57   62   6769         80 83   88   9395         106108  113  118120       131134  139  144146       157 160 165  170172       183186  191  196198
         * 356889014766212+180125111539+0021+052048+0004+00180+3+01+0001+0002+0+0000000000+01+0004+0005+0+0000000003+01+0007+0008+0+0000000006+01+0010+0011+0+0000000009+01+9999+9999+9+0000000012+00+9999+9999+9+9999999999
         *                                                          Canal2 C3   Canal1        Alarmes      Canal4       SPAC2 SPBC2  SPBC1        SPBC3 SPAC4  SPAC3                     SPBC4          
         *                                                                                          SPAC1
         * 356889014766212  IMEI
         * +180125111539    DataHora da Rede
         * +0021            Código Interno de Software
         * +052048          Porta TCP/IP
         * +0004            Numero de Frames no Pacote (no ZeeGbee será i numero totalizadores das 4 entradas analogicas)
         * +00180           Tempo em Segundos entre transmissões 180 = 3 Minutos
         * +3               Flag de Alarme (sem uso)
         * +01              Foi feita a leitura com Sucesso no Itallux na hora de trasmitir
         * +0001            Canal2 Itallux
         * +0002            Canal3 Itallux
         * +0               Sem Uso (no ZeeGbee pode ser uma fraude)
         * +0000000000      Canal1 Itallux
         * +01              Leu com sucesso
         * +0004            Alarmes Itallux
         * +0005            SetPointALtoCanal1 Itallux
         * +0               Sem uso
         * +0000000003      Canal4 Itallux
         * +01
         * +0007            SetPointAltoCanal2
         * +0008            SetPointBaixoCanal2
         * +0
         * +0000000006      SetPointBaixoCanal1
         * +01
         * +0010            SetPointBaixoCanal3
         * +0011            SetPointAltoCanal4
         * +0
         * +0000000009      SetPointAltoCanal3
         * +01
         * +9999
         * +9999
         * +9
         * +0000000012      SetPointBaixoCanal4
         * +00
         * +9999
         * +9999
         * +9
         * +9999999999
         * 
         * 
        */
        public static RemotaItallux MontaRemota(String content)
        {
            RemotaItallux ri = new RemotaItallux();

            ri.IMEI = content.Substring(0, 15);

            ri.DataHoraUltimoAlteração = content.Substring(16, 12);

            ri.Canal1 = content.Substring(75, 4);
            ri.Canal2 = content.Substring(57, 4);
            ri.Canal3 = content.Substring(62, 4);
            ri.Canal4 = content.Substring(101, 4);

            ri.Alarmes = content.Substring(83, 4);
            ri.SetPointAltoCanal1 = content.Substring(88, 4);
            ri.SetPointBaixoCanal1 = content.Substring(127, 4);
            ri.SetPointAltoCanal2 = content.Substring(109, 4);
            ri.SetPointBaixoCanal2 = content.Substring(114, 4);
            ri.SetPointAltoCanal3 = content.Substring(153, 4);
            ri.SetPointBaixoCanal3 = content.Substring(135, 4);
            ri.SetPointAltoCanal4 = content.Substring(140, 4);
            ri.SetPointBaixoCanal4 = content.Substring(173, 4);

            return ri;
        }

    }


}
