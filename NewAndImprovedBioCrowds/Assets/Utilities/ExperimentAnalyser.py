import numpy as np
import cv2
import sys
from math  import sqrt

def euclid(a, b):
    diff = (a[0] - b[0], a[1] - b[1])
    return sqrt(diff[0]*diff[0] + diff[1]*diff[1])

class AgentData():
    def __init__(self, id,  cloudID, firstFrame):
        self.id = id
        self.cloudID = cloudID
        self.startframe = firstFrame
        self.positions = {}

class CloudData():
    def __init__(self, id, agentsInCloud):
        self.id = id
        self.agentsInCloud = agentsInCloud
        self.positions = {}
        self.capturedCells = {}
        self.agentsPerFrame = {}




# Parse cells
cellDict = {}
cellsFileName = sys.argv[1]
cellsFile = open(cellsFileName)

for line in cellsFile:
    c_id, x, y = [float(l) for l in line.split(";")[:-1]]
    cellDict[int(c_id)] = (x,y)




# Parse Frames
# FrameDict = []

# Parse Clouds
cloudDict = {}
cloudsFileName = sys.argv[2]
cloudsFile = open(cloudsFileName)



for line in cloudsFile:
    if(line.startswith("#")):
        continue
    s_line = line.split(";")

    frame = int(s_line[0])
    c_id = int(s_line[1])
    c_qnt = int(s_line[2])
    c_x = float(s_line[3])
    c_y = float(s_line[4])
    capturedCells = [int(x) for x in s_line[6:]]

    if(c_id not in cloudDict):
        cloudDict[c_id] = CloudData(c_id, c_qnt)
    
    cloudDict[c_id].positions[frame] = (c_x, c_y)
    cloudDict[c_id].capturedCells[frame] = capturedCells


cloudEstimated = open("cloudEstimatedDensities.txt", 'w')
for cloudid, cloud in cloudDict.items():
    cloudEstimated.write("cloud " + str(cloudid)+'\n')
    for frame in cloud.capturedCells.keys():
            cloudEstimated.write(str(frame)+";" + str(float(cloud.agentsInCloud) / (len(cloud.capturedCells[frame])*4))+'\n')

cloudEstimated.close()

# Parse Agents
agentsInFrame = {}

agentDict = {}
agentsFileName = sys.argv[3]
agentsFile = open(agentsFileName)

for line in agentsFile:
    if(line.startswith("#")):
        continue
    s_line = line.split(";")
    frame = int(s_line[0])
    count = int(s_line[1])

    agentsInFrame[frame] = []

    for i in range(count):
        idx = i*4 + 2
        a_id = int(s_line[idx])
        cloud_id = int(s_line[idx+1])

        #print(frame, count, idx, a_id, cloud_id)

        if(a_id not in agentDict):
            agentDict[a_id] = AgentData(a_id, cloud_id,  frame)

        agentDict[a_id].positions[frame] = (float(s_line[idx+2]), float(s_line[idx+3]))

        if(frame not in cloudDict[cloud_id].agentsPerFrame):
            cloudDict[cloud_id].agentsPerFrame[frame] = []

        cloudDict[cloud_id].agentsPerFrame[frame].append(a_id)
        agentsInFrame[frame].append(a_id)

# Calculate Agent Densities
cloudDensOutput = open("cloudSoloAgentDensities.txt", 'w')

cloudSoloAgentDensities = {}

for k in cloudDict.keys():
    cloudSoloAgentDensities[k] = {}
    agentPositions = []

    cloudDensOutput.write("Cloud " + str(k) + '\n')

    for f in cloudDict[k].agentsPerFrame.keys():
        agents = cloudDict[k].agentsPerFrame[f]

        agentPositions = []
        for x in agents:
            agentPositions.append((agentDict[x].positions[f]))

        array = np.array(agentPositions, dtype="float32")
        hull = cv2.convexHull(array)

        agentArea = cv2.contourArea(hull)
        if(agentArea != 0):
            cloudSoloAgentDensities[k][f] = float(len(agentPositions))/agentArea
            cloudDensOutput.write(str(f) + ";" + str(cloudSoloAgentDensities[k][f]) + '\n')
            print("cloud", k, "frame", f, "density", cloudSoloAgentDensities[k][f])
        else:
            cloudSoloAgentDensities[k][f] = 0.0

cloudDensOutput.close()

cloudMultiDensOutput = open("cloudMultiAgentDensities.txt", 'w')
cloudMultiAgentDensity = {}
# for each cloud
for k in cloudDict.keys():
    cloudMultiAgentDensity[k] = {}
    agentPositions = []

    cloudMultiDensOutput.write("Cloud " + str(k) + '\n')

    #for each frame
    for f in cloudDict[k].agentsPerFrame.keys():
        agents = cloudDict[k].agentsPerFrame[f]

        agentPositions = []
        for x in agents:
            agentPositions.append((agentDict[x].positions[f]))

        array = np.array(agentPositions, dtype="float32")
        hull = cv2.convexHull(array)
        agentArea = cv2.contourArea(hull)

        # for each other cloud
        for k_ in cloudDict.keys():
            if k_ != k:
                #if the cloud has agents in that frame
                if f in cloudDict[k_].agentsPerFrame:
                    k2Agents = cloudDict[k_].agentsPerFrame[f]

                    #for each cloud agent in that frame
                    for y in k2Agents:
                        #if that agent is in the cloud convex hull
                        if cv2.pointPolygonTest(hull, (agentDict[y].positions[f]), measureDist=False) >= 0:

                            #count it for density
                            agentPositions.append(agentDict[y].positions[f])

        if(agentArea != 0):
            cloudMultiAgentDensity[k][f] = float(len(agentPositions))/agentArea
            cloudMultiDensOutput.write(str(f) + ";" + str(cloudMultiAgentDensity[k][f]) + '\n')
            #print("cloud", k, "frame", f, "density", cloudMultiAgentDensity[k][f])
        else:
            cloudMultiAgentDensity[k][f] = 0.0


avgDistsWindow = 10

cloudSpeedsOutput = open("cloudSpeeds.txt", 'w') 
# Calculate Cloud Speeds
for k in cloudDict.keys():
    cloudSpeedsOutput.write("Cloud" + str(k) + '\n')
    sortedPositions = sorted(cloudDict[k].positions.items())

    for i  in range(1, len(sortedPositions)):
        dist = euclid(sortedPositions[i-1][1], sortedPositions[i][1])

        #print(str(sortedPositions[i][0]) + ";" + str(dist) )
        cloudSpeedsOutput.write(str(sortedPositions[i][0]) + ";" + str(dist) +'\n')

cloudSpeedsOutput.close()




# Calculate Agent Average Speeds    
averageSpeepdsPerCloudPerFrame = {}

agentSpeedOutput = open("agentSpeeds.txt", 'w') 

for k in cloudDict.keys():
    averageSpeepdsPerCloudPerFrame[k] = {}

    sortedAgentsInFrame = sorted(cloudDict[k].agentsPerFrame.items())
    agentSpeedOutput.write("cloud " + str(k) + '\n')
    for i in range(1, len(sortedAgentsInFrame)):
        aggregateDeltas = 0
        counter = 0
        frame = sortedAgentsInFrame[i][0]
        previousFrame = sortedAgentsInFrame[i-1][0]

        # for each agent in frame
        for j in sortedAgentsInFrame[i][1]:

            #if agent already existed 
            if j in sortedAgentsInFrame[i-1][1]:
                oldPos = agentDict[j].positions[previousFrame]
                pos = agentDict[j].positions[frame]
                dist = euclid(pos, oldPos)
                aggregateDeltas += dist
                counter += 1

        #print(counter)

        averageSpeepdsPerCloudPerFrame[k][frame] = float(aggregateDeltas) / float(counter)
        agentSpeedOutput.write(str(frame)+';'+str(averageSpeepdsPerCloudPerFrame[k][frame])+'\n')

