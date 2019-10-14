import sys
import re
import os


class Parameter():
    def __init__(self, input1):

        input1 = input1[1:].split('{')
        id_type = input1[0]
        self.id = id_type.split('-')[1]
        self.type = id_type.split('-')[0]
        
        params_text = input1[1][:-1]

        self.params = params_text.split(',')


def create_experiment(index, folder, param_dict):
    

# \$[a-z]+\{(.+)\} captures the group ranges and type.

with open (sys.argv[1], 'r') as f:
    file_text = f.read()

if(not sys.argv[1].endswith( ".json")):
    exit()

if(not os.path.isdir(sys.argv[1][:-4])):
    folder = sys.argv[1][:-4]
    os.mkdir(folder)

pattern = re.compile(r"(\$[a-z]+-[0-9]+\{.+\})")

match = re.findall( pattern, file_text)

print(match)

group_dicts = dict()


for item in match:
    print(item)
    group_dicts[item] = Parameter(item)


total_experiments = 1
for k, v in group_dicts.items():
    print(v.id, v.type, v.params)
    total_experiments *= len(v.params)

print(total_experiments)

for i in range(0, total_experiments):
    create_experiment(i, folder)