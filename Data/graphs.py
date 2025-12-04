import json
import glob
import matplotlib.pyplot as plt
import numpy as np

files = glob.glob("*.json")

if len(files) == 0:
    print("No files found")
    exit()

datasets = []

for f in files:
    with open(f, "r") as file:
        d = json.load(file)
        d["file"] = f
        datasets.append(d)

def label(d):
    return d["method"]

for i in range(0, len(datasets), 2):
    dPBD = datasets[i]
    dXPBD = datasets[i+1]
    sceneID = dPBD["file"].split(".")[0]
    print(sceneID)
    
    print("Looking at scene", dPBD["file"])
    
    plt.figure(figsize=(12, 10))
    plt.suptitle("Comparison of PBD and XPBD results for scene " + sceneID)
    
    plt.subplot(3, 2, 1)
    plt.plot(dPBD["distError"], label=label(dPBD), linewidth=2)
    plt.plot(dXPBD["distError"], label=label(dXPBD), linewidth=2)
    plt.title("Mean Distance Error")
    plt.grid(True)
    plt.legend()
    
    plt.subplot(3, 2, 2)
    plt.plot(dPBD["maxDistError"], label=label(dPBD), linewidth=2)
    plt.plot(dXPBD["maxDistError"], label=label(dXPBD), linewidth=2)
    plt.title("Max Distance Error")
    plt.grid(True)
    plt.legend()
    
    plt.subplot(3, 2, 3)
    plt.plot(dPBD["oscillations"], label=label(dPBD), linewidth=2)
    plt.plot(dXPBD["oscillations"], label=label(dXPBD), linewidth=2)
    plt.title("Mean Oscillations")
    plt.yscale("log")
    plt.grid(True)
    plt.legend()
    
    plt.subplot(3, 2, 4)
    plt.plot(dPBD["maxOscillations"], label=label(dPBD), linewidth=2)
    plt.plot(dXPBD["maxOscillations"], label=label(dXPBD), linewidth=2)
    plt.title("Max Oscillations")
    plt.yscale("log")
    plt.grid(True)
    plt.legend()
    
    plt.subplot(3, 2, 5)
    plt.plot(np.cumsum(np.array(dPBD["cpu"])), label=label(dPBD), linewidth=2)
    plt.plot(np.cumsum(np.array(dXPBD["cpu"])), label=label(dXPBD), linewidth=2)
    plt.title("CPU Time per Frame")
    plt.xlabel("Frame")
    plt.ylabel("ms")
    plt.grid(True)
    plt.legend()
    
    plt.tight_layout()
    plt.show()