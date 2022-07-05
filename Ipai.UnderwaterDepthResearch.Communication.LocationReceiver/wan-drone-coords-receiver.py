#!/usr/bin/python
import socket
import numpy
import os


serverIp = '<SERVER_IP_ADDRESS>'
serverPort = 4444

tty = os.open("/dev/ttyS0", os.O_RDWR)

sock = socket.socket()
while True:
    try:
        sock.connect((serverIp, serverPort))

        while True:
            data = sock.recv(40)
            if (len(data)>3):
                os.write(tty, data)
        sock.close()
    except:
        l = 1
