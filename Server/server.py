"""
FILE          : server.py
PROJECT       : Network Application Development A-03: Services and Logging
TEAM          : Andrey Takhtamirov and Alex Braverman
FIRST VERSION : Feb 24, 2021
DESCRIPTION   : This is the server CLI which receives data from incoming clients
                    and (if valid) logs the details to a configured file.
"""
import asyncio
import os
import socket
import threading
from datetime import datetime
from time import sleep

import aiofiles as aiof
import yaml


class ClientThread(threading.Thread):
    """
    Client service thread   : receives message from client and increments the
                                connection counter. passes message to process_message()
                                to be processed
    """

    def __init__(self, clientsocket):
        threading.Thread.__init__(self)
        self.csocket = clientsocket

    def run(self):
        # Receive data from client
        data = self.csocket.recv(RECEIVE_BUFFER)
        msg = data.decode()

        # Retrieve client ID to be monitored to check against spamming
        message_elements = msg.split(MESSAGE_DELIMITER)

        # Check that the message from the client is valid (includes all 4 elements),
        # The client ID should be a number
        if len(message_elements) == CLIENT_MESSAGE_ELEMENTS \
                and message_elements[INDEX_CLIENT_ID].isnumeric():
            # If new client, add to dictionary with 0 starting value,
            #   if existing client, increment counter by 1
            if client_ids.get(int(message_elements[INDEX_CLIENT_ID])) is None:
                client_ids[int(message_elements[INDEX_CLIENT_ID])] = 0
            else:
                client_ids[int(message_elements[INDEX_CLIENT_ID])] += 1

            # If it's not a "noisy" client (less messages than the limit),
            #   process the message
            if client_ids[int(message_elements[INDEX_CLIENT_ID])] < MESSAGE_LIMIT:
                process_message(message_elements)


class ManageClients:
    """
    Client Manager Thread   : clears client message count
                                every specified interval
    """

    def __init__(self):
        thread = threading.Thread(target=self.run)
        thread.daemon = True  # Daemonize thread
        thread.start()

    # Clear the client_id dictionary every <interval> seconds
    @staticmethod
    def run():
        while True:
            client_ids.clear()
            sleep(CLEAR_INTERVAL)


def process_message(message_elements):
    """
    Function    : process_message
    Description : Verifies that the message is valid and starts async writing task.
    :param message_elements: list containing the elements of a client's message.
    :return:
    """

    # If the data is valid, pass to logging function
    if message_elements[INDEX_TIME].isnumeric() and message_elements[INDEX_LOG_LEVEL].isnumeric():

        # only if the log level is in the valid range (0-7)
        if 0 <= int(message_elements[INDEX_LOG_LEVEL]) < len(LOG_LEVELS):
            # Change the log level to a string value (using dictionary key-values)
            message_elements[INDEX_LOG_LEVEL] = LOG_LEVELS.get((int(message_elements[INDEX_LOG_LEVEL])))

            # Check if the client is turning its logging on (only if the client was previously off)
            if int(message_elements[INDEX_CLIENT_ID]) in off_clients \
                    and message_elements[INDEX_LOG_LEVEL] == LOG_LEVELS.get(LOG_LEVEL_ON):
                # Remove client from "OFF" list
                off_clients.remove(int(message_elements[INDEX_CLIENT_ID]))

            # Make sure client isn't "OFF" before logging
            # the last "OFF" log will be logged before the client's logs turn off
            if int(message_elements[INDEX_CLIENT_ID]) not in off_clients:
                # Create new coroutine to write to file
                loop = asyncio.new_event_loop()
                asyncio.set_event_loop(loop)

                try:
                    loop.run_until_complete(write_to_file(LOG_DIRECTORY + LOG_FILE_NAME, message_elements))
                finally:
                    loop.run_until_complete(loop.shutdown_asyncgens())
                    loop.close()

                # Check if the client is stopping logging for itself
                if message_elements[INDEX_LOG_LEVEL] == LOG_LEVELS.get(LOG_LEVEL_OFF):
                    off_clients.append(int(message_elements[INDEX_CLIENT_ID]))


async def write_to_file(filename, message_elements):
    """
    Function    : write_to_file
    Description : Asynchronously writes to a file (appending).
                    File must exist before writing. Data is
                    written with the format specified.
    :param filename:    the file which will be appended.
    :param message_elements:    list of message elements to be written.
    :return:
    """
    # Get the current time
    current_time = datetime.utcfromtimestamp(float(message_elements[INDEX_TIME])
                                             / MILLISECONDS_IN_SECOND).strftime(TIME_FORMAT)[:-3]

    # Write to the file (using specified format from config.yaml)
    async with aiof.open(filename, "a") as out:
        await out.write(LOG_FORMAT.format(time=current_time,
                                          id=message_elements[INDEX_CLIENT_ID],
                                          log_level=message_elements[INDEX_LOG_LEVEL],
                                          message=message_elements[INDEX_MESSAGE]))
        await out.flush()


def load_config():
    """
    Function    : load_config
    Description : Loads data into global variables from the config file
    :return:
    """
    # Global variable declarations so they can be modified
    global LOCALHOST
    global PORT
    global RECEIVE_BUFFER
    global MESSAGE_DELIMITER
    global MILLISECONDS_IN_SECOND
    global LOG_FILE_NAME
    global LOG_DIRECTORY
    global TIME_FORMAT
    global LOG_FORMAT
    global LOG_LEVELS
    global LOG_LEVEL_OFF
    global LOG_LEVEL_ON
    global MESSAGE_LIMIT
    global CLEAR_INTERVAL

    # load from yaml config file
    data = yaml.safe_load(open(CONFIG_FILE_NAME))

    # load settings for basic server configuration
    server_settings = data["server"]
    LOCALHOST = server_settings.get("address")
    PORT = server_settings.get("port")
    RECEIVE_BUFFER = server_settings.get("max_message_length")
    MESSAGE_DELIMITER = server_settings.get("message_delimiter")
    MILLISECONDS_IN_SECOND = server_settings.get("milliseconds_in_second")

    # load file configuration settings
    file_settings = data["log_file"]
    LOG_FILE_NAME = file_settings.get("file_name")
    TIME_FORMAT = file_settings.get("log_time_format")
    LOG_FORMAT = file_settings.get("log_format")
    LOG_LEVELS = file_settings.get("log_levels")
    LOG_DIRECTORY = file_settings.get("log_directory")
    LOG_LEVEL_OFF = file_settings.get("log_level_off")
    LOG_LEVEL_ON = file_settings.get("log_level_on")

    # load noise settings (manage clients)
    noise_settings = data["noise_handling"]
    MESSAGE_LIMIT = noise_settings.get("max_number_of_messages")
    CLEAR_INTERVAL = noise_settings.get("time_limit")


def main():
    """
    Main function. Runs the config loader and creates dir/file
                    if needed. Starts a socket and the client manager.
                    Listens for clients (forever) and starts threads as needed.
    :return:
    """
    # load server app settings
    load_config()

    # Create the log directory if it doesn't exist
    if not os.path.exists(LOG_DIRECTORY):
        os.makedirs(LOG_DIRECTORY)

    # Create empty file if it doesn't exist
    if not os.path.exists(LOG_DIRECTORY + LOG_FILE_NAME):
        open(LOG_DIRECTORY + LOG_FILE_NAME, "w").close()

    # Create server socket
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCALHOST, PORT))

    # Start client manager
    ManageClients()

    print("Server started on", LOCALHOST, ":", PORT)
    print("Waiting for client(s)...")

    # Listening loop, on connection start client service thread
    while True:
        server.listen(1)
        clientsock, client_address = server.accept()
        newthread = ClientThread(clientsock)
        newthread.start()


# Config file containing program settings
CONFIG_FILE_NAME = "config.yaml"

# settings are loaded from config.yaml
LOCALHOST = ""  # the local ip address of the server
PORT = 0  # the port which the server is hosted on
RECEIVE_BUFFER = 0  # the buffer for receiving messages from client
LOG_LEVELS = {}  # log levels in a dictionary <int, string>
TIME_FORMAT = ""  # the format string for the time format
MILLISECONDS_IN_SECOND = 0
CLEAR_INTERVAL = 0  # the interval for clearing noise counter
MESSAGE_LIMIT = 0  # the limit of "noise" in an interval
MESSAGE_DELIMITER = ""  # delimiter in the client's message
LOG_FILE_NAME = ""  # file name of te log file
LOG_DIRECTORY = ""  # directory name of the log file
LOG_FORMAT = ""  # format of the log printout
LOG_LEVEL_OFF = 0  # log level which will turn client logging OFF
LOG_LEVEL_ON = 0  # log level which will turn client logging ON

# Elements which compose the client's full message, split by '|'
INDEX_TIME = 0  # the index of UTC Time in the client's message
INDEX_CLIENT_ID = 1  # the index of the client's ID in their message
INDEX_LOG_LEVEL = 2  # the index of the log level in the message
INDEX_MESSAGE = 3  # the index of the message text in the complete message

CLIENT_MESSAGE_ELEMENTS = 4  # the number of elements sent by the client

# running total of each client's message count, cleared with interval
client_ids = {}

# Clients which send the "OFF" logging level are ignored until they send the "ALL level".
# List of "OFF" clients.
off_clients = []

if __name__ == "__main__":
    main()
