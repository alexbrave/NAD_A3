using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Net;
using System.Net.Sockets;
namespace NAD_A3_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(ClientStrings.welcomeMessage);
            string userInput = Console.ReadLine();
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


                //// Close everything.
                stream.Close();
                client.Close();


            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine( "ArgumentNullException: " + e.ToString() + "\n\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: " + e.ToString() + "\n\n");
            }
        }
    }
}
