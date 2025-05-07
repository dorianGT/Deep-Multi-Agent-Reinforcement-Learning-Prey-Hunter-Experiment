# IHM-RL-Project
 
| Config File Name               | Trainer | RNN | Curiosity | Network Size         | Notes                                |
| ------------------------------ | ------- | --- | --------- | -------------------- | ------------------------------------ |
| `HunterPreyPOCARNN.yml`        | POCA    | V   | X         | Standard (e.g. 512)  | POCA baseline with RNN               |
| `HunterPreyPOCARNNCurious.yml` | POCA    | V   | V         | Standard             | POCA + RNN + Curiosity               |
| `HunterPreyPOCARNNSmall.yml`   | POCA    | V   | X         | **Small** (e.g. 256) | POCA + RNN with reduced network size |
| `HunterPreyPPORNN.yml`         | PPO     | V   | X         | Standard             | PPO + RNN                            |
| `HunterPreyPPORNNCurious.yml`  | PPO     | V   | V         | Standard             | PPO + RNN + Curiosity                |
| `HunterPreyPPORNNSmall.yml`    | PPO     | V   | X        | **Small**            | PPO + RNN with reduced network size  |
