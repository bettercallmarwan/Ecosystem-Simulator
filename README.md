# Animal Ecosystem Simulation

##  Project Overview
This project implements a **grid-based animal ecosystem simulation** using two different programming paradigms:
1. **Functional Programming** (Declarative approach)
2. **Imperative Programming** (Procedural approach)


---

##  Simulation Description

The simulation models a simple ecosystem on a 20×20 grid with:
- **Herbivores (H)** - Green - Eat plants, reproduce at 60+ energy
- **Carnivores (C)** - Red - Eat herbivores, reproduce at 80+ energy
- **Plants (P)** - Yellow - Food for herbivores, spawn randomly
- **Obstacles (X)** - Gray - Block movement

### Rules:
- Animals move randomly to adjacent cells (up, down, left, right)
- Moving costs 1 energy per turn
- Herbivores gain 20 energy from eating plants
- Carnivores gain 30 energy from eating herbivores
- Animals reproduce when energy threshold is met (costs 30 energy)
- Animals die when energy reaches 0
- New plants spawn randomly each turn (1-7 plants)
- Simulation runs infinitely until all animals die

