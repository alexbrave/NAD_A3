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
using System.IO;

namespace NAD_A3_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int instanceID = GenerateID();
            int loggerPort = 0;
            IPAddress loggerIP = null;
            if (args.Length != 2 || 
                !int.TryParse(args[0], out loggerPort) ||
                !IPAddress.TryParse(args[1], out loggerIP))
            {
                if (File.Exists(Constants.cmdArgsErrorPath))
                {
                    using (StreamReader sr = File.OpenText(Constants.cmdArgsErrorPath))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(s);
                        }
                    }
                }
                else
                    Console.WriteLine(Constants.FileIOError);
                return;
            }

            string userInput = "";
            while(userInput != Constants.quit)
            {
                userInput = ProcessLogs(loggerPort, loggerIP, instanceID);
            }
        }


        static string ProcessLogs(int loggerPort, IPAddress loggerIP, int instanceID)
        {
            const int minLevel = 1;
            const int maxLevel = 7;

            var randomLevel = new Random();
            string userInput;
            Console.WriteLine("Please enter a command:");
            userInput = Console.ReadLine();
            //if (userInput == null || userInput == "")
            //    return strings.continueProgram;

            if (string.Compare(userInput, Constants.help) == 0)
            {
                if (File.Exists(Constants.helpTextPath))
                {
                    using (StreamReader sr = File.OpenText(Constants.helpTextPath))
                    {
                        string s;
                        while ((s = sr.ReadLine()) != null)
                            Console.WriteLine(s);
                        return Constants.continueProgram;
                    }
                }
                else 
                    Console.WriteLine(Constants.FileIOError);
                return Constants.continueProgram;
            }
            else if(string.Compare(userInput, Constants.automatic) == 0)
            {
                if (File.Exists(Constants.automaticTestTextPath))
                {

                    using (StreamReader sr = File.OpenText(Constants.automaticTestTextPath))
                    {
                        string s;
                        Console.WriteLine("Sending array of different logs.\n");
                        while ((s = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(s);
                            // SendMessage(loggerIP, loggerPort, instanceID, randomLevel.Next(minLevel, maxLevel + 1), s);
                            Thread.Sleep(100); // This is to prevent too many messages being sent at once
                        }

                        Console.WriteLine("Logs Sent.\n");
                        return Constants.continueProgram;
                    }
                }
            }
            else if(string.Compare(userInput, Constants.noise) == 0)
            {
                if (File.Exists(Constants.noiseTextPath))
                {

                    using (StreamReader sr = File.OpenText(Constants.noiseTextPath))
                    {
                        string s;
                        Console.WriteLine("Sending noise.\n");
                        while ((s = sr.ReadLine()) != null)
                        {
                            // SendMessage(loggerIP, loggerPort, instanceID, randomLevel.Next(minLevel, maxLevel + 1), s);
                            Console.WriteLine(s);
                        }
                        Console.WriteLine("\nLogs Sent.\n");
                        return Constants.continueProgram;
                    }
                }
            }
            else if (string.Compare(userInput, Constants.manual) == 0)
            {
                int logLevel = 0;
                Console.WriteLine("Please enter a log level: ");
                if (!int.TryParse(Console.ReadKey().KeyChar.ToString(), out logLevel) ||
                    logLevel > 7 || logLevel < 1)
                {
                    Console.WriteLine("\nSorry, log level out of range, or not a number\n");
                    return Constants.continueProgram;
                }
                Console.WriteLine("\nPlease enter a log message, then hit enter:");
                userInput = Console.ReadLine();
                if (userInput.Length == 0 || userInput.Length > 200)
                {
                    Console.WriteLine("Sorry, cannot log a blank message, or a message longer than 200 characters.");
                    return Constants.continueProgram;
                }
                Console.WriteLine("\nSending log...");
                // SendMessage(loggerIP, loggerPort, instanceID, logLevel, s);
                Console.WriteLine(userInput);
                Console.WriteLine("Log sent.");
            }
            else if(string.Compare(userInput, Constants.quit) == 0)
            {
                Console.WriteLine("Until next time!\n");
                return Constants.quit;
            }
            else
            {
                Console.WriteLine("Sorry, command not recognized. Please enter a valid command,\n" +
                    "or enter \"help\" for a list of commands.\n");
                return Constants.continueProgram;
            }
            return Constants.continueProgram;
        }


        static int GenerateID()
        {
            string instanceIDString = DateTime.UtcNow.ToString("HHmmssfff");
            int instanceID = 0;
            int.TryParse(instanceIDString, out instanceID);
            return instanceID;
        }

        /*
        Function     :  Send_Message()
        Parameter    :  String server       : server IP address
                        String message      : message to send
        Return Value :  void    : This method has no returns
        Description  :  The basic send message function, that sends a message to the server.
        */
        static void SendMessage(IPAddress server, int port, int instanceID, int logLevel, String message)
        {
            try
            {
                // Create a TcpClient.

                TcpClient client = new TcpClient(server.ToString(), port);

                string formattedLog = instanceID.ToString() + "|";

                if (logLevel != Constants.preexistingLevel)
                    formattedLog += logLevel.ToString() + "|";

                formattedLog += message;

                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = Encoding.ASCII.GetBytes(formattedLog);

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
