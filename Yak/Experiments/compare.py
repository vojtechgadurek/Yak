import json
import sys

# Load the JSON file

first_file_name1 = sys.argv[1]
first_file_name2 = sys.argv[2]


with open(first_file_name1, "r") as file:
    data1 = json.load(file)

with open(first_file_name2, "r") as file:
    data2 = json.load(file)

# Compare the two JSON files

set1 = set(data1)
set2 = set(data2)

answer = set1.symmetric_difference(set2)

print(len(answer))
