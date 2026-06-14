# 1v1-Aim-Trainer-RL-project
1v1 Aim Trainer RL project is a 1v1 training sandbox where you fight against an AI opponent that actually learns — powered by Unity ML-Agents and Reinforcement Learning.

# Project Overview
Conventional aim trainers (Aim Lab, KovaaK's) use scripted bots that move in predetermined patterns. Players memorize those patterns rather than developing transferable aim skills.

This project replaces scripted bots with an RL agent trained through self-play — an opponent that develops genuine strategies by competing against itself. The result is an adaptive training partner whose difficulty scales naturally with training time.


## Files and Folders

- `config/[files_name].yaml` — POCA training configuration
- `results/` — Exported ONNX model checkpoints
- `requirements.txt` - List of ML Agent dependencies.
- `Scripts/DodgeBallGameController.cs` — Modified game controller (4v4 → 1v1)
- `Scripts/DodgeBallAgent.cs` — Original agent script (no changes needed)
- `Scripts/DifficultyMenu.cs` — UI script for difficulty selection
- `Brain/` — Exported ONNX model checkpoints at different training stages
- `train.py` — Script to start training
- `resume.py` — Script to resume training from last checkpoint
- `setup.py` — Script to install all dependencies

## Installation

Requirements :
- Python 3.10
- Unity 6 (6000.3.11f1)
- NVIDIA GPU with CUDA 12.4 (recommended)

## Set up
Step 1 — Install Python 3.10

Download from: https://www.python.org/downloads/release/python-31011/
⚠️ Make sure to check "Add Python to PATH" during installation!

Step 2 — Install dependencies

For NVIDIA GPU (recommended):

```bash
py -3.10 -m pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
py -3.10 -m pip install mlagents==1.1.0
   ```





