import os
import sys
import cv2

def read_image(path):
    return cv2.imread(path, cv2.IMREAD_COLOR)


dir_path = sys.argv[1]
root_name = sys.argv[2]
quantity = int(sys.argv[3])

video = cv2.VideoWriter(dir_path + ".avi", cv2.VideoWriter_fourcc(*'XVID'), 30, ( 1280, 720) )

for i in range(0, quantity):
    name = root_name + '{0:04d}'.format(i) + '.png'

    path = dir_path + '/' + name

    img = read_image(path)

    video.write(img)


video.release()