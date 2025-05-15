# Deep Multi-Agent Reinforcement Learning : Expérience Chasseurs-Proies

Ce projet Unity explore l'apprentissage par renforcement multi-agent dans un environnement 3D simulé. Deux équipes, les chasseurs et les proies, s'affrontent dans un environnement avec obstacles. L'objectif est d'étudier des comportements émergents de coopération et de stratégie via l'apprentissage par renforcement profond.

## Contexte
Projet réalisé dans le cadre du Master 2 Vision et Intelligence Artificielle (Université Paris Cité).

Auteurs : Dorian Grouteau, Noureddine Bertrand et Samy Aghiles Aouabed.

Encadré par Nizar Ouarti.

Année universitaire : 2024–2025.

## Objectifs du projet
- Implémenter une simulation multi-agent avec Unity et ML-Agents.

- Étudier la coopération et la communication entre agents dans un contexte de chasse.

- Mettre en œuvre des comportements adaptatifs chez des proies poursuivies.

- Intégrer des raycasts pour la vision limitée des agents.

- Étudier des stratégies émergentes (embuscade, évitement, alerte).

- Créer un envrionnement procédurale et modifiable en partie.

## Description du gameplay

### Chasseurs

Doivent toucher chaque proie avant la fin du temps imparti.

Peuvent se coordonner pour coincer les proies.

### Proies

Disposent d’un délai de 5 secondes pour se cacher avant l’activation des chasseurs.

Coopèrent en partageant leur vision pour éviter d’être attrapées.

### Environnement
Grille 3D avec des blocs fixes et blocs déplaçables (via poussée).

Vision basée sur des raycasts directionnels.

Chaque agent dispose de deux actions :

- Rotation (valeurs continues de -1 à 1).
- Avancer (vitesse de 0 à 1).

## Technologies utilisées
Unity 6000.0.39.f1

ML-Agents Toolkit 

TensorFlow / PyTorch

Git pour le versioning

## Lancement du projet

### Prérequis
Unity installé avec le module ML-Agents

Python + ML-Agents Python package

### Installation
Cloner le repo :

```
git clone https://github.com/toncompte/Deep-Multi-Agent-Reinforcement-Learning-Prey-Hunter-Experiment.git
```

Ouvrir le projet dans Unity Hub.

Ouvrir la scene Demo

Pour entraîner un modèle :

```
mlagents-learn config/nomdelaconfig.yaml --run-id=test_run_1
```

## Config Testé
 
| Config File Name               | Trainer | RNN | Curiosity | Network Size         | Notes                                |
| ------------------------------ | ------- | --- | --------- | -------------------- | ------------------------------------ |
| `HunterPreyPOCARNN.yml`        | POCA    | V   | X         | Standard (e.g. 512)  | POCA baseline with RNN               |
| `HunterPreyPOCARNNCurious.yml` | POCA    | V   | V         | Standard             | POCA + RNN + Curiosity               |
| `HunterPreyPOCARNNSmall.yml`   | POCA    | V   | X         | **Small** (e.g. 256) | POCA + RNN with reduced network size |
| `HunterPreyPPORNN.yml`         | PPO     | V   | X         | Standard             | PPO + RNN                            |
| `HunterPreyPPORNNSmall.yml`    | PPO     | V   | X        | **Small**            | PPO + RNN with reduced network size  |

Et bien plus pour la partie d'apprentissage par phase!
