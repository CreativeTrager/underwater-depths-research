#!/usr/bin/python
import socket
import numpy


serverIp = '192.168.0.131'
serverPort = 4444

sock = socket.socket()
while True:
    try:
        sock.connect((serverIp, serverPort))

        while True:
            data = sock.recv(40)
            print(data)
            print(data.decode())

        sock.close()
    except:
        l = 1
