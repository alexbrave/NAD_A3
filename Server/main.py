import socket
import threading


def main():
    class ClientThread(threading.Thread):
        def __init__(self, client_address, clientsocket):
            threading.Thread.__init__(self)
            self.csocket = clientsocket

        def run(self):
            print("Connection from : ", client_address)
            self.csocket.send(bytes("Connection to server established", 'utf-8'))
            msg = ''
            data = self.csocket.recv(2048)
            msg = data.decode()
            print("Message from client: ", msg)
            print("Client disconnected ", client_address)
            self.csocket.send(bytes(msg, 'UTF-8'))

    LOCALHOST = "172.26.45.87"
    PORT = 8080
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCALHOST, PORT))
    print("Server started")
    print("Waiting for client request")
    while True:
        server.listen(1)
        clientsock, client_address = server.accept()
        newthread = ClientThread(client_address, clientsock)
        newthread.start()


if __name__ == '__main__':
    main()
