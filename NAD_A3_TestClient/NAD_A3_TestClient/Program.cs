﻿/*
* FILE          : Program.cs
* PROJECT       : SENG2040-21W-Sec1-Network Application Development - Assignment #3
* PROGRAMMER    : Andrey Takhtamirov, Alex Braverman
* FIRST VERSION : February 28, 2020
* DESCRIPTION   : 
*           This file contains the logic for a test client that can send different types of test logs
*           to a logging mircroservice at a known IP/port on the network.
*           This test client has the functionality to run: 
*               - an automatic test of all logs that the logger is capable of logging including misformatted logs
*               - a manual test, where the user is asked to enter log information and will format the log 
*                 before transmission
*               - a noise test, where the client will test the logger's ability to identify noisy clients
*                 and block them
*/

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
            
            // Each client needs to generate a unique ID for itself
            int instanceID = GenerateID();

            int loggerPort = 0;
            IPAddress loggerIP = null;

            // The port and IP address of the logger will be passed via command line parameters
            // But if something isn't right with those parameters, we print an error message to
            // the screen
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
                else // Couln't open error message file!
                    Console.WriteLine(Constants.FileIOError);
                return;
            }

            // Here we find the main loop of the program
            string userInput = "";
            while(userInput != Constants.quit)
            {
                userInput = ProcessLogs(loggerPort, loggerIP, instanceID);
            }
        }

        /*
        Method       :  ProcessLogs()
        Parameter    :  int loggerPort     : the logger's port
                        IPAddress loggerIP : the logger's IP address
                        int instanceID     : the instance ID generated by the 
                                             client to identify itself to the logger
        Return Value :  string    : Our return value communicates to the loop in Main()
                                    if the user wishes to continue or quit
        Description  :  The ProcessLogs() method will get user input to decide what
                        kind of test they would like to run. It supports an automatic,
                        manual, and noise tests, described more below.
        */
        static string ProcessLogs(int loggerPort, IPAddress loggerIP, int instanceID)
        {
            // Minimum and maximum supported log levels 
            const int minLevel = 0;
            const int maxLevel = 7;
            const int smallPause = 50;

            // needed to generate random log levels in the noise test
            var randomLevel = new Random(); 

            // Here we get some user input
            string userInput;
            string time;
            Console.WriteLine("Please enter a command:");
            userInput = Console.ReadLine().Trim();

            // Then execute logic dependingg on the users choice
            if (string.Compare(userInput, Constants.help) == 0)
            {
                // Print help file messages if they exist
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
                else // Error if not
                    Console.WriteLine(Constants.FileIOError);
                return Constants.continueProgram;
            }

            // User has chosen to run an automatic test of all the features of the logger
            else if(string.Compare(userInput, Constants.automatic) == 0)
            {
                // We attempt to open the file with all the automatic test logs
                if (File.Exists(Constants.testLogsPath))
                {
                    using (StreamReader reader = File.OpenText(Constants.testLogsPath))
                    {
                        string line;
                        Console.WriteLine("Sending array of different logs.\n");

                        // Here we send a message for each log level
                        // All these messages are valid!
                        for(int i = minLevel; i < maxLevel + 1; i++)
                        {
                            time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                            line = reader.ReadLine();
                            Console.WriteLine("Sending a valid log with the level: " + i.ToString());
                            SendMessage(loggerIP, loggerPort, time, instanceID.ToString(), i.ToString(), 
                                line, Constants.formatLog);

                            // This is to prevent too many messages being sent at once
                            Thread.Sleep(smallPause); 
                        }

                        // Next we'll send a request with invalid time
                        time = Constants.invalidNumber;
                        line = reader.ReadLine();
                        Console.WriteLine("Sending a log with an invalid time.");
                        SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                            randomLevel.Next(minLevel, maxLevel + 1).ToString(),
                            line, Constants.formatLog);
                        Thread.Sleep(smallPause);


                        // Now we're going to send a log request with an invalid instance ID
                        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        line = reader.ReadLine();
                        Console.WriteLine("Sending a log with an invalid instance ID.");
                        SendMessage(loggerIP, loggerPort, time, Constants.invalidNumber, 
                            randomLevel.Next(minLevel, maxLevel + 1).ToString(),
                            line, Constants.formatLog);
                        Thread.Sleep(smallPause);


                        // Next we send a log request with a log level that is invalid
                        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        line = reader.ReadLine();
                        Console.WriteLine("Sending a log with an invalid level");
                        SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                            Constants.levelInvalid, line, Constants.formatLog);
                        Thread.Sleep(smallPause);


                        // Then we'll send a log request with a level that is too low
                        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        line = reader.ReadLine();
                        Console.WriteLine("Sending a log with a log level that is too low");
                        SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                            Constants.levelTooLow, line, Constants.formatLog);
                        Thread.Sleep(smallPause);


                        // Next we'll send a log request with a level that is too high
                        time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        line = reader.ReadLine();
                        Console.WriteLine("Sending a log with a log level that is too high");
                        SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                            Constants.levelTooHigh, line, Constants.formatLog);
                        Thread.Sleep(smallPause);


                        // Then we send an entirely misformatted message.
                        // This message is so misformatted in fact, that we set a special flag
                        // to SendMessage to not attempt any formatting
                        Console.WriteLine("Sending a random string as an invalid log request.");
                        SendMessage(loggerIP, loggerPort, Constants.placeholder, Constants.placeholder,
                            Constants.placeholder, Constants.misformattedLog, Constants.sendAsIs);
                        Thread.Sleep(smallPause);


                        Console.WriteLine("Logs Sent.\n");

                        // Send a message to the main program loop to continue the program
                        return Constants.continueProgram;
                    }
                }
            }

            // User has chosen to send noise to test the logger's ability to handle noisy clients
            else if(string.Compare(userInput, Constants.noise) == 0)
            {
                Console.WriteLine("Please enter the number of requests you'd like to send,\n" +
                    "or enter \"noise\" to send the all log requests to test the logger noise prevention.\n" +
                    "The maximum number of requests is 1000: ");
                userInput = Console.ReadLine().Trim();
                int numberOfRequestsToSend = 0;


                if(int.TryParse(userInput, out numberOfRequestsToSend) || userInput == Constants.noise)
                {
                    // We should make sure that the user has not entered a 0 or negative number of requests to send
                    if(userInput != Constants.noise && numberOfRequestsToSend <= 0)
                    {
                        Console.WriteLine("Sorry the number of requests to send cannot be 0 or negative!");
                        return Constants.continueProgram;
                    }

                    // We double check that our text file actually exists!
                    if (File.Exists(Constants.testLogsPath))
                    {
                        using (StreamReader reader = File.OpenText(Constants.testLogsPath))
                        {
                            // To improve speed, we want to read all of the lines from the file into 
                            // a list before firehosing the logger with requests!
                            List<string> noiseText = new List<string>();
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                noiseText.Add(line);
                            }

                            // If the user entered a large number or "noise", we will send all the logs in the file
                            if (userInput == Constants.noise || numberOfRequestsToSend >= Constants.maxNumOfTestLogs)
                            {
                                Console.WriteLine("You have chosen to send all test logs!\n");
                                foreach (string str in noiseText)
                                {
                                    time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                                    SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                                        randomLevel.Next(minLevel, maxLevel + 1).ToString(),
                                        str, Constants.formatLog);
                                    Thread.Sleep(smallPause); // Only a very brief pause between requests
                                    Console.WriteLine(str);
                                }
                            }

                            // Otherwise the user has chosen to send a custom number of logs between 1 and 148
                            else
                            {
                                Console.WriteLine("You have chosen to send {0} test log(s)!\n", numberOfRequestsToSend);
                                for(int i = 0; i < numberOfRequestsToSend; i++)
                                {
                                    time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                                    SendMessage(loggerIP, loggerPort, time, instanceID.ToString(),
                                        randomLevel.Next(minLevel, maxLevel + 1).ToString(),
                                        noiseText[i], Constants.formatLog);
                                    Thread.Sleep(smallPause); // Only a very brief pause between requests
                                    Console.WriteLine(noiseText[i]);
                                }
                            }

                            Console.WriteLine("\nLogs Sent.\n");
                        }
                        return Constants.continueProgram;
                    }
                }
                else 
                {
                    Console.WriteLine("Sorry, that doesn't seem to be a number or the command \"noise\".");
                    return Constants.continueProgram;
                }
                
            }

            // The user has chosen to send a manual log request
            else if (string.Compare(userInput, Constants.manual) == 0)
            {
                Console.WriteLine("Please input a manually formatted log:\n");
                userInput = Console.ReadLine().Trim();

                Console.WriteLine("\nSending log...");
                SendMessage(loggerIP, loggerPort, Constants.placeholder, Constants.placeholder, Constants.placeholder, 
                    userInput, Constants.sendAsIs);
                Console.WriteLine(userInput);
                Console.WriteLine("Log sent.");
            }

            // User has chosen to quit
            else if(string.Compare(userInput, Constants.quit) == 0)
            {
                Console.WriteLine("Until next time!\n");
                return Constants.quit;
            }

            // Oops, user has submitted an invalid command
            else
            {
                Console.WriteLine("Sorry, command not recognized. Please enter a valid command,\n" +
                    "or enter \"help\" for a list of commands.\n");
                return Constants.continueProgram;
            }
            return Constants.continueProgram;
        }

        /*
        Function     :  GenerateID()
        Parameter    :  none    : this method does not require any inputs
        Return Value :  int     : This method returns a an instance ID for the instance to use,
                                  when sending log requests to the logger.
        Description  :  This method is used to create a relatively unique instance ID based on the current time.
                        The instance of the test client uses this ID to identify itself when sending log requests
                        to the logger.
        */
        static int GenerateID()
        {
            string instanceIDString = DateTime.UtcNow.ToString("HHmmssfff");
            int instanceID = 0;
            int.TryParse(instanceIDString, out instanceID);
            return instanceID;
        }

        /*
        Function     :  Send_Message()
        Parameter    :  IPAddress logger    : this is the logger's IP address
                        int port            : this is the logger's port number
                        string time         : the test client captures the time at which the log was created
                                              and sends this time as part of the log request
                        string instanceID   : the instance identifies itself using it's instance ID, ususally an integer
                                              in properly formatted logs
                        string logLevel     : this is expected to be an integer by the logger, and in properly formatted
                                              logs it is, but in intentionally misformatted logs, it might not be!
                        string log          : this is the actual message that will be logged
                        bool formatLog      : this tells SendMessage if we want it to properly format the log with all the
                                              other arguments that we inputted, including intanceID, logLevel, and time,
                                              or if SendMessage should ignore those other arguments, and send the "log" 
                                              argument to the logger as is without any formatting.
                        String message      : message to send
        Return Value :  void    : This method has no returns
        Description  :  The basic send message function, that sends a message to the server.
        */
        static void SendMessage(IPAddress logger, int port, string time, string instanceID, 
            string logLevel, string log, bool formatLog)
        {
            try
            {
                // Create a TcpClient.
                TcpClient client = new TcpClient(logger.ToString(), port);

                string formattedLog = "";

                // if formatLog is equal to sendAsIs, we will not do any formatting to the log message and send it 
                // unchanged to the logger
                if (formatLog == Constants.sendAsIs)
                    formattedLog = log;
                // otherwise the calling method does indeed want the log to be formatted
                else
                {
                    formattedLog = time + "|";
                    formattedLog += instanceID + "|";
                    formattedLog += logLevel.ToString() + "|";
                    formattedLog += log;
                }
                

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
