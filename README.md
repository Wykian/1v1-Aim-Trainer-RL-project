# 1v1-Aim-Trainer-RL-project
1v1 Aim Trainer RL project is a 1v1 training sandbox where you fight against an AI opponent that actually learns — powered by Unity ML-Agents and Reinforcement Learning.

You can read the full procress here : https://medium.com/@sualpoven2/i-built-an-ai-dodgeball-opponent-that-learns-to-beat-you-using-reinforcement-learning-in-unity-921951fdc923
You can test the game out here : https://sualp-oven.itch.io/1v1-aim-trainer-rl-project

# Noted
1. The files that I attached in this project is just files that I modify, so not to confused you need to originated git clone Dodge Ball Unity Official and then modify in later in the Unity Editor
2. The name of the run ID and yaml files name could be change so it up to you

# Project Overview
Conventional aim trainers (Aim Lab, KovaaK's) use scripted bots that move in predetermined patterns. Players memorize those patterns rather than developing transferable aim skills.

This project replaces scripted bots with an RL agent trained through self-play — an opponent that develops genuine strategies by competing against itself. The result is an adaptive training partner whose difficulty scales naturally with training time.

## The Three-Stage Training Pipeline
| Stage | Steps | Peak ELO | Behavior |
|---|---|---|---|
| Stage 1 | 2M | ~1,350 | Basic throwing |
| Stage 2 | 25M | ~1,600 | Integrated aim + dodge |
| Stage 3 | 35M | ~1,200 | Adaptive |

The Stage 3 ELO drop is proof the system is working — the AI deliberately loses rounds against simulated novice opponents to learn restraint.

## Environment Modifications (4v4 → 1v1)
| Change | Original | Modified |
|---|---|---|
| Agents per team | 4 | 1 |
| Balls in arena | 10 | 4 |
| Arena scale | 1.0x | 0.6x |
| Max episode steps | 5,000 | 5,000 |

## Files and Folders

- `config/dodgeball_1v1.yaml` — POCA training configuration
- `results/` — Exported ONNX model checkpoints
- `requirements.txt` - List of ML Agent dependencies.
- `Scripts/DodgeBallGameController.cs` — Modified game controller (4v4 → 1v1)
- `Scripts/DodgeBallAgent.cs` — Modified
- `Scripts/DifficultyMenu.cs` — UI script for difficulty selection
- `Brain/` — Exported ONNX model checkpoints at different training stages

## Installation

Requirements :
- Python 3.10
- Unity 6 (6000.3.11f1)
- NVIDIA GPU with CUDA 12.4 (recommended)
- Visual Studio 2022 (recommended it much easier)

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

For CPU only:

```bash
py -3.10 -m pip install torch torchvision torchaudio
py -3.10 -m pip install mlagents==1.1.0
   ```

Step 3 — Verify installation

```bash
py -3.10 -c "import torch; print(torch.cuda.is_available())"
py -3.10 -c "import mlagents; print('ML-Agents ready!')"
   ```

## How to Train

Step 1 — Open Unity

Open Unity Hub → Add project → point to the project folder
Open scene: Assets/Dodgeball/Scenes/Elimination_Training

Step 2 — Start training

```bash
py -3.10 -c "from mlagents.trainers.learn import main; main()" "\config\dodgeball_1v1.yaml" --run-id dodgeball_1v1_v1
   ```

Step 3 — Press Play in Unity
Wait for terminal to show:

[INFO] Listening on port 5004

Step 4 - Monitor through the terminal in the Visual Studio 2022
it will update in terminal that should show something like this

results/dodgeball_1v1_v1/DodgeBall-<step>.onnx

Step 5 — Resume training

```bash
py -3.10 -c "from mlagents.trainers.learn import main; main()" "D:\config\dodgeball_1v1.yaml" --run-id dodgeball_1v1_v1 --resume
   ```
## 🙏 Credits
- Original DodgeBall environment by [Unity Technologies](https://github.com/Unity-Technologies/ml-agents-dodgeball-env) — Apache 2.0 License
- ML-Agents framework by [Unity Technologies](https://github.com/Unity-Technologies/ml-agents) — Apache 2.0 License
