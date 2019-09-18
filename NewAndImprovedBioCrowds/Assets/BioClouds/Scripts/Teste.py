import cv2
import numpy as np
import math

def getLen(p1):
    return math.sqrt(pow(p1[0],2) + pow(p1[1],2))

def diff(initial, final):
    return final[0] - initial[0] , final[1] - initial[1]

def dot(p1, p2):
    dot =0
    for i in range(len(p1)):
        dot += (p1[i] * p2[i])
    return dot

def getF(agentPos, markerPos, goalVector):

    ymod = getLen(diff( agentPos,markerPos))
    xmod = getLen(diff(goalVector, (0,0)))
    d = dot(diff(agentPos,markerPos),goalVector)
    if(xmod * ymod == 0):
         return 0
    return (1.0 / (1.0 + ymod)) * (1.0 + (d / (xmod * ymod)))

height = 110
width = 110

max_ = 0
point = (55, 55)
radius = 50
goal = (55, 0)
minRange =50 
m = (0.0, 0.0)

slate = np.zeros((height,width,3), np.uint8)
total = 0

for i in range (height):
    for j in range(width):
        if(j < minRange):
            continue
        fi = float(i)
        fj = float(j)
        if( getLen(diff(point, (fi,fj))) > radius):
            continue
        a = getF(point, (fi,fj), diff(point,goal))
        if (a > max_):
            max_ = a
        total += a

for i in range (height):
    for j in range(width):
        if(j < minRange):
            continue    
        fi = float(i)
        fj = float(j)
        if(getLen(diff(point, (fi, fj))) <= radius):
            a = getF(point, (fi,fj), diff(point, goal))
            dif = diff(point, (fi,fj))
            slate[i][j][0] = (a  * 255)
            slate[i][j][1] = (a  * 255)
            slate[i][j][2] = (a  * 255)
            m = (m[0] + ( dif[0] * (float(a) / float(total)) ), m[1] + ( dif[1] * (float(a) / float(total)) ))
#m = int(m[0] * 3.1415), int(m[1] * 3.1415)

cv2.line(slate, (int(m[1]) + point[1], int(m[0])+point[0]), point, (255, 255, 255))
slate = cv2.resize(slate, (6*height,6*width))
cv2.imshow("janela", slate)
cv2.waitKey(0)