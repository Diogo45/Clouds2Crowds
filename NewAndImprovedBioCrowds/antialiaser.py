import os
import sys
import cv2

def read_image(path):
    return cv2.imread(path, cv2.IMREAD_COLOR)


dir_path = sys.argv[1]
root_name = sys.argv[2]
quantity = int(sys.argv[3])



for i in range(0, quantity):
    name = root_name + '{0:04d}'.format(i) + '.png'

    path = dir_path + '/' + name

    img = read_image(path)
    aspect_ratio = ( 1280, 720)
    img = cv2.resize(img, aspect_ratio, interpolation=cv2.INTER_AREA)


    aa_path = dir_path + '/AA_' + name
    
    cv2.imwrite(aa_path, img)

