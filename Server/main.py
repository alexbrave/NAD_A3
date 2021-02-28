import asyncio
import os
import socket
import threading
from datetime import datetime
from time import sleep

import aiofiles as aiof
import yaml


# Client service thread: receives message from client and increments the connection counter.
# passes message to process_message() to be processed
class ClientThread(threading.Thread):
    def __init__(self, clientsocket):
        threading.Thread.__init__(self)
        self.csocket = clientsocket

    def run(self):
        # Receive data from client
        data = self.csocket.recv(RECEIVE_BUFFER)
        msg = data.decode()

        # Retrieve client ID to be monitored to check against spamming
        message_elements = msg.split(MESSAGE_DELIMITER)

        if len(message_elements) == CLIENT_MESSAGE_ELEMENTS and message_elements[INDEX_CLIENT_ID].isnumeric():
            # If new client, add to dictionary with 0 starting value,
            #   if existing client, increment counter by 1
            if client_ids.get(int(message_elements[INDEX_CLIENT_ID])) is None:
                client_ids[int(message_elements[INDEX_CLIENT_ID])] = 0
            else:
                client_ids[int(message_elements[INDEX_CLIENT_ID])] += 1

            #  for debugging
            # print("client ids: ", client_ids)

            # If it's not a "noisy" client (less messages than the limit),
            #   process the message
            if client_ids[int(message_elements[INDEX_CLIENT_ID])] < MESSAGE_LIMIT:
                process_message(message_elements)


# Client Manager Thread: clears client message
#   count every specified interval
class ManageClients(object):
    def __init__(self):
        thread = threading.Thread(target=self.run)
        thread.daemon = True  # Daemonize thread
        thread.start()

    @staticmethod
    def run():
        # Clear the client_id dictionary every <interval> seconds
        while True:
            client_ids.clear()
            sleep(CLEAR_INTERVAL)


# Process message (received from client) and write data to log file
def process_message(message_elements):
    # If the data is valid (need to add more checks to check for
    #   [int] [int], [int], [string] format)
    if message_elements[INDEX_TIME].isnumeric() and message_elements[INDEX_LOG_LEVEL].isnumeric():

        # convert the int log level to a string (only if it's in the valid range (0-7))
        if 0 <= int(message_elements[INDEX_LOG_LEVEL]) < len(LOG_LEVELS):
            # Change the log level to a string value (using dictionary key-values)
            message_elements[INDEX_LOG_LEVEL] = LOG_LEVELS.get((int(message_elements[INDEX_LOG_LEVEL])))

            # Create new coroutine to write to file
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)

            try:
                loop.run_until_complete(write_to_file(LOG_DIRECTORY + LOG_FILE_NAME, message_elements))
            finally:
                loop.run_until_complete(loop.shutdown_asyncgens())
                loop.close()


# Async write to file
async def write_to_file(filename, message_elements):
    # Get the current time
    current_time = datetime.utcfromtimestamp(float(message_elements[INDEX_TIME])
                                             / MILLISECONDS_IN_SECOND).strftime(TIME_FORMAT)[:-3]

    # Write to the file (using specified format from config.yaml)
    async with aiof.open(filename, "a") as out:
        await out.write(LOG_FORMAT.format(time=current_time,
                                          id=message_elements[INDEX_TIME],
                                          log_level=message_elements[INDEX_CLIENT_ID],
                                          message=message_elements[INDEX_MESSAGE]))
        await out.flush()


def load_config():
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
    global MESSAGE_LIMIT
    global CLEAR_INTERVAL

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

    # load noise settings (manage clients)
    noise_settings = data["noise_handling"]
    MESSAGE_LIMIT = noise_settings.get("max_number_of_messages")
    CLEAR_INTERVAL = noise_settings.get("time_limit")


def main():
    # load server app settings
    load_config()

    # Create the log directory if it doesn't exist
    if not os.path.exists(LOG_DIRECTORY):
        os.makedirs(LOG_DIRECTORY)

    # Create empty file if it doesn't exist
    if not os.path.exists(LOG_DIRECTORY + LOG_FILE_NAME):
        open(LOG_DIRECTORY + LOG_FILE_NAME, 'w').close()

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
        # print("\nConnection from : ", client_address)
        newthread = ClientThread(clientsock)
        newthread.start()


# Config file containing program settings
CONFIG_FILE_NAME = "config.yaml"

# settings are loaded from config.yaml
LOCALHOST = ""
PORT = 0
RECEIVE_BUFFER = 0
LOG_LEVELS = {}
TIME_FORMAT = ""
MILLISECONDS_IN_SECOND = 0
CLEAR_INTERVAL = 0
MESSAGE_LIMIT = 0
MESSAGE_DELIMITER = ""
LOG_FILE_NAME = ""
LOG_DIRECTORY = ""
LOG_FORMAT = ""

# Elements which compose the client's full message, split by '|'
INDEX_TIME = 0  # the index of UTC Time in the client's message
INDEX_CLIENT_ID = 1  # the index of the client's ID in their message
INDEX_LOG_LEVEL = 2  # the index of the log level in the message
INDEX_MESSAGE = 3  # the index of the message text in the complete message

CLIENT_MESSAGE_ELEMENTS = 4  # the number of elements sent by the client

# running total of each client's message count, cleared as specified
client_ids = {}

if __name__ == "__main__":
    main()
