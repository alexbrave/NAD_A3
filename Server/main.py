import asyncio
import os
import socket
import threading
from datetime import datetime

import aiofiles as aiof


class ClientThread(threading.Thread):
    def __init__(self, clientsocket):
        threading.Thread.__init__(self)
        self.csocket = clientsocket

    def run(self):
        data = self.csocket.recv(RECEIVE_BUFFER)
        msg = data.decode()
        server_response = process_message(msg)
        self.csocket.send(bytes(server_response, "UTF-8"))
        print("Client disconnected")


def process_message(message):
    # print("message received: " + message)
    message_elements = message.split("|")

    if len(message_elements) == 3:
        message_elements[1] = set_log_level(int(message_elements[1]))
        print("ID: ", message_elements[0])
        print("Log Level: ", message_elements[1])
        print("Log Message: ", message_elements[2])

        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)

        try:
            loop.run_until_complete(write_to_file("./logs/messages.log", message_elements))
        finally:
            loop.run_until_complete(loop.shutdown_asyncgens())
            loop.close()

        return "Write Successful."
    else:
        return "ERROR: Unknown log format."


async def write_to_file(filename, message_elements):
    current_time = datetime.utcnow().strftime(time_format)[:-3]
    print(current_time)

    if os.path.exists(filename):
        file_permission = "a"
    else:
        file_permission = "w"

    async with aiof.open(filename, file_permission) as out:
        await out.write(current_time + " " + message_elements[0] + " " +
                        message_elements[1] + " " +
                        message_elements[2] + "\n")
        await out.flush()


def set_log_level(log_level):
    return log_levels.get(log_level)


def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCALHOST, PORT))

    print("Server started")
    print("Waiting for client request")
    while True:
        server.listen(1)
        clientsock, client_address = server.accept()
        print("\nConnection from : ", client_address)
        newthread = ClientThread(clientsock)
        newthread.start()


# get these from .config file
LOCALHOST = "172.26.45.87"
PORT = 8080
RECEIVE_BUFFER = 2048
log_levels = {1: "DEBUG", 2: "INFO", 3: "WARN", 4: "ERROR", 5: "FATAL"}
time_format = "%Y-%m-%d %H:%M:%S.%f"

if __name__ == "__main__":
    main()
