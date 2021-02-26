import asyncio
import os
import socket
import threading
from datetime import datetime
from time import sleep

import aiofiles as aiof


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
        message_elements = msg.split(message_delimiter)

        # If new client, add to dictionary with 0 starting value,
        #   if existing client, increment counter by 1
        if client_ids.get(int(message_elements[0])) is None:
            client_ids[int(message_elements[0])] = 0
        else:
            client_ids[int(message_elements[0])] += 1

        #  for debugging
        print("client ids: ", client_ids)

        # If it's not a "noisy" client (less messages than the limit),
        #   process the message
        if client_ids[int(message_elements[0])] < message_limit:
            process_message(msg)


# Client Manager Thread: clears client message
#   count every specified interval
class ManageClients(object):
    def __init__(self):
        thread = threading.Thread(target=self.run, args=())
        thread.daemon = True  # Daemonize thread
        thread.start()  # Start the thread

    def run(self):
        # Clear the client_id dictionary every <interval> seconds
        while True:
            client_ids.clear()
            print("\nCleared\n")
            sleep(clear_interval)


# Process message (received from client) and write data to log file
def process_message(message):
    message_elements = message.split(message_delimiter)

    # If the data is valid (need to add more checks to check for
    #   [int], [int], [string] format)
    if len(message_elements) == 3:
        # Change the log level to a string value
        message_elements[1] = set_log_level(int(message_elements[1]))

        # For debugging
        print("ID: ", message_elements[0])
        # print("Log Level: ", message_elements[1])
        # print("Log Message: ", message_elements[2])

        # Create new coroutine to write to file
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            loop.run_until_complete(write_to_file("./logs/messages.log", message_elements))
        finally:
            loop.run_until_complete(loop.shutdown_asyncgens())
            loop.close()


# Sets the log level to a string using a dictionary
def set_log_level(log_level):
    return log_levels.get(log_level)


# Async write to file
async def write_to_file(filename, message_elements):
    # Get the current time
    current_time = datetime.utcnow().strftime(time_format)[:-3]

    # Check if file exists, if not, need to write to create it,
    #   if exists, append to it (maybe should create the file on server init)
    if os.path.exists(filename):
        file_permission = "a"
    else:
        file_permission = "w"

    # Write to the file format: [UTC date, time] [client ID] [Log level] [Log message]
    async with aiof.open(filename, file_permission) as out:
        await out.write(current_time + " " + message_elements[0] + " " +
                        message_elements[1] + "\t" +
                        message_elements[2] + "\n")
        await out.flush()


def main():
    # Create server socket
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCALHOST, PORT))

    # Start client manager
    ManageClients()

    print("Server started")
    print("Waiting for client request")

    # Listening loop, on connection start client service thread
    while True:
        server.listen(1)
        clientsock, client_address = server.accept()
        print("\nConnection from : ", client_address)
        newthread = ClientThread(clientsock)
        newthread.start()


# need to get these from .config file
LOCALHOST = "172.26.45.87"
PORT = 8080
RECEIVE_BUFFER = 2048
log_levels = {0: "ALL", 1: "DEBUG", 2: "INFO", 3: "WARN", 4: "ERROR", 5: "FATAL", 6: "OFF", 7: "TRACE"}
time_format = "%Y-%m-%d %H:%M:%S.%f"
clear_interval = 120
message_limit = 100
message_delimiter = "|"

# running total of each client's message count, cleared as specified
client_ids = {}

if __name__ == "__main__":
    main()
