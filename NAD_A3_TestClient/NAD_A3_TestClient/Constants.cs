using System;
using System.Collections.Generic;
using System.Text;

namespace NAD_A3_TestClient
{
    static public class Constants
    {
        // On startup get server IP and PORT from cmd line args

        public const string welcomeMessage = "Welcome to the NAD A3 Test Client.\n " +
            "Please enter a command, or enter \"help\" for a list of commands:\n";


        //////////////
        // COMMANDS //
        //////////////

        // help
        public const string help = "help";

        // abuse prevention test from config file
        public const string noise = "noise";

        // send log message manually
        public const string manual = "manual";

        // send automatic test with different level logs
        public const string automatic = "automatic";

        // quit program
        public const string quit = "quit";

        // continue main loop
        public const string continueProgram = "";


        /////////////////////
        // Text File Paths //
        /////////////////////
        public const string cmdArgsErrorPath = @"..\..\..\..\textFiles\cmdArgsError.txt";
        public const string helpTextPath = @"..\..\..\..\textFiles\helpText.txt";
        public const string automaticTestTextPath = @"..\..\..\..\textFiles\automaticTestText.txt";
        public const string testLogsPath = @"..\..\..\..\textFiles\testLogs.txt";


        // Other constants
        public const bool sendAsIs = true;
        public const bool formatLog = false;
        public const string misformattedLog = "this is a misformatted log!";
        public const string invalidNumber = "1234notANumber";
        public const string levelTooHigh = "10";
        public const string levelTooLow = "-1";
        public const string levelInvalid = "Oof";
        public const string placeholder = "placeholder";
        // This is the maximum number of test logs that we can send, because this is how many 
        // lines of test logs we have in our text logs file
        public const int maxNumOfTestLogs = 1000; 


        ////////////
        // Errors //
        ////////////
        public const string FileIOError = "Sorry something went wrong when trying to open a file!\n";
    }
}
