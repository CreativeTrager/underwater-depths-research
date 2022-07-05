#!/usr/bin/python
import socket
import cv2
import numpy
import sys

TCP_IP = '<SERVER_IP_ADDRESS>'
TCP_PORT = 4141

capture = cv2.VideoCapture(1)
while True:
    try:
        sock = socket.socket()
        sock.connect((TCP_IP, TCP_PORT))

        trash, frame = capture.read()

        encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), 90]
        result, imgencode = cv2.imencode('.jpg', frame, encode_param)
        data = numpy.array(imgencode)

        sock.send(data);
        sock.close()
    except:
        l = 1

sock.close()
