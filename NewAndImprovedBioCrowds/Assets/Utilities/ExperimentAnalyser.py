import numpy as np
import cv2
import sys

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


# Calculate Agent Densities

cloudAgentDensities = {}

for k in cloudDict.keys():
    cloudAgentDensities[k] = {}
    agentPositions = []
    for f in cloudDict[k].agentsPerFrame.keys():
        agents = cloudDict[k].agentsPerFrame[f]

        agentPositions = []
        for x in agents:
            agentPositions.append((agentDict[x].positions[f]))

        array = np.array(agentPositions, dtype="float32")
        hull = cv2.convexHull(array)

        agentArea = cv2.contourArea(hull)
        if(agentArea != 0):
            cloudAgentDensities[k][f] = float(len(agentPositions))/agentArea
        else:
            cloudAgentDensities[k][f] = 0.0

            



# Calculate Cloud Speeds

# Calculate Agent Average Speeds

