/*
* FILE          : MainWindow.xaml.cs
* PROJECT       : PROG2121-20F-Sec1-Windows and Mobile Programming - Assignment #5
* PROGRAMMER    : Alex Braverman
* FIRST VERSION : November 11, 2020
* DESCRIPTION   : The purpose of this project is to demonstrate threads and the use
*                 of protocols to coordinate messages between clients and a server.
*                 This file is the WPF client side.
*/




using System;
using System.Collections.Generic;
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
using System.Net;
using System.Net.Sockets;


namespace NAD_A3_Client
{

    public partial class MainWindow : Window
    {
        volatile bool ConnectedToServer = false;
        volatile int listeningPort = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        /*
        Function     :  Connect_Click()
        Parameter    :  object sender       : necessary to process click events
                        RoutedEventArgs e   : necessary to process click events
        Return Value :  void    : This method has no returns
        Description  :  it starts a thread that listens to messages from the server
        */
        public void Connect_Click(object sender, RoutedEventArgs e)
        {
            ParameterizedThreadStart ts = new ParameterizedThreadStart(Listen_For_Messages);
            Thread clientThread = new Thread(ts);
            clientThread.Start();

        }

        /*
        Function     :  Button_Click()
        Parameter    :  object sender       : necessary to process click events
                        RoutedEventArgs e   : necessary to process click events
        Return Value :  void    : This method has no returns
        Description  :  When the connect button is clicked, it sends a message to the server to 
                        then resend that message to all connected users.
        */
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string server = ChosenServerIPAddress.Text;
            if (server == "")
            {
                server = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            }
            string message = MessageToSend.Text;
            Send_Message(server, message);
        }

        /*
        Function     :  Button_Click()
        Parameter    :  object sender       : necessary to process click events
                        RoutedEventArgs e   : necessary to process click events
        Return Value :  void    : This method has no returns
        Description  :  When the connect button is clicked, this thread is started to 
                        listen to messages coming from the server.
        */
        public void Listen_For_Messages(object o)
        {
            // Int32 port = 8686;
            IPAddress localAddr = null;
            string localChosenClientIPAddress = null;
            this.Dispatcher.Invoke(() =>
            {
                localChosenClientIPAddress = ChosenClientIPAddress.Text;
            });
            if (IPAddress.TryParse(localChosenClientIPAddress, out localAddr) == false)
            {
                localAddr = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            }


            if (listeningPort == 0)
            {
                // In order to get a unique port number for listening, 
                // we get the stack to assign a free port, then save it as the 
                // port that we'll listen on for server messages
                TcpListener l = new TcpListener(localAddr, 0);
                l.Start();
                int port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                listeningPort = port;
            }

            // Create a new listener
            TcpListener localServer = new TcpListener(localAddr, listeningPort);


            // This chunk of code is necessary only to format the "connect" message properly
            // with the correct port, user-chosen or default address and code word "connectClient"
            string localChosenServerIPAddress = null;
            this.Dispatcher.Invoke(() =>
            {
                localChosenServerIPAddress = ChosenServerIPAddress.Text;
            });
            IPAddress server = null;
            if (IPAddress.TryParse(localChosenServerIPAddress, out server) == false)
            {
                server = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            }
            string message = listeningPort.ToString() + ";" + localAddr + "!" + "connectClient";
            Send_Message(server.ToString(), message);

            try
            {
                // Listen for messages
                localServer.Start();

                // Loop for multiple messages
                while (true)
                {
                    TcpClient client = localServer.AcceptTcpClient();


                    // Byte array to store incoming data
                    Byte[] bytes = new Byte[256];
                    String data = null;

                    data = null;

                    // Declare stream object for reading
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Get all stream writes from the stream
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data from bytes to ASCII
                        data = Encoding.ASCII.GetString(bytes, 0, i);

                        this.Dispatcher.Invoke(() =>
                        {
                            ChatArea.Text += "Received: " + data;
                        });
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ChatArea.Text += "SocketException: " + e.ToString();
                });
            }
            finally
            {
                localServer.Stop();
            }
        }

        /*
        Function     :  Send_Message()
        Parameter    :  String server       : server IP address
                        String message      : message to send
        Return Value :  void    : This method has no returns
        Description  :  The basic send message function, that sends a message to the server.
        */
        public void Send_Message(String server, String message)
        {
            try
            {
                // Create a TcpClient.

                Int32 port = 8686;
                TcpClient client = new TcpClient(server, port);

                string formattedMessage =
                    Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString() +
                    ";" + message;

                if (message.Contains("connectClient"))
                {
                    formattedMessage = message;
                }

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = Encoding.ASCII.GetBytes(formattedMessage);

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();

                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);


                this.Dispatcher.Invoke(() =>
                {
                    ChatArea.Text += UsersName.Text + "  :    " + message + "\n\n";
                });

                //// Close everything.
                stream.Close();
                client.Close();


            }
            catch (ArgumentNullException e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ChatArea.Text += "ArgumentNullException: " + e.ToString() + "\n\n";
                });
            }
            catch (SocketException e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    ChatArea.Text += "SocketException: " + e.ToString() + "\n\n";
                });
            }
        }
    }
    /*
    This assignment was created using sample code in the course content(1.).
    
    1. Mika, N. (2020). WinProg-IPC-TCP[PowerPoint Slides]. 
		    Retrieved November 9, 2020, from eConestoga.
    */
}
