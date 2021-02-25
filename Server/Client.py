import socket

SERVER = "172.26.45.87"
PORT = 8080
message = ""

client = None
# while message != "q":
message = "1623301234" + "|" + "1" + "|" + "Log message"
# print("sent: " + message)
# time.sleep(0.05)
client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((SERVER, PORT))
client.sendall(bytes(message, 'UTF-8'))
in_data = client.recv(1024)
print("From Server :", in_data.decode())

client.close()
