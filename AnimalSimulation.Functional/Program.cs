using System;
using System.Collections.Generic;
using System.Linq;

// ============================================
// FUNCTIONAL (declarative) PROGRAMMING APPROACH
// ============================================


namespace FunctionalAnimalSimulation
{
    // immutable record types
    public record Position(int X, int Y);

    public enum AnimalType { Herbivore, Carnivore }

    public record Animal(Position Pos, int Energy, int Id, AnimalType Type)
    {
        // each method returns a new animal object instead of modifying the exsting one
        // "with" keyword => creates a copy of "this" object
        public Animal Move(Position newPos) => this with { Pos = newPos };
        public Animal ChangeEnergy(int delta) => this with { Energy = Energy + delta };
    }

    public record Plant(Position Pos);
    public record Obstacle(Position Pos);

    public record SimulationEvent(string Message);

    public record SimulationState(
        int Width,
        int Height,
        IReadOnlyList<Animal> Animals,
        IReadOnlyList<Plant> Plants,
        IReadOnlyList<Obstacle> Obstacles,
        IReadOnlyList<SimulationEvent> Events,
        int Turn
    );

    public static class FunctionalSimulation
    {
        private static Random _random = new Random();

        // initialize the simulation state
        public static SimulationState Initialize(int width, int height, int herbivoreCount, int carnivoreCount, int plantCount, int obstacleCount)
        {
            // initialize herbivores
            var herbivores = Enumerable.Range(0, herbivoreCount)
                .Select(id => new Animal(
                    new Position(_random.Next(width), _random.Next(height)),
                    50,
                    id,
                    AnimalType.Herbivore
                ))
                .ToList();

            // initialize carnivores
            var carnivores = Enumerable.Range(herbivoreCount, carnivoreCount)
                .Select(id => new Animal(
                    new Position(_random.Next(width), _random.Next(height)),
                    50,
                    id,
                    AnimalType.Carnivore
                ))
                .ToList();

            var allAnimals = herbivores.Concat(carnivores).ToList();

            // initialize plants
            var plants = Enumerable.Range(0, plantCount)
                .Select(_ => new Plant(
                    new Position(_random.Next(width), _random.Next(height))
                ))
                .ToList();

            // initialize obstacles
            var obstacles = Enumerable.Range(0, obstacleCount)
                .Select(_ => new Obstacle(
                    new Position(_random.Next(width), _random.Next(height))
                ))
                .ToList();

            return new SimulationState(width, height, allAnimals, plants, obstacles, new List<SimulationEvent>(), 0);
        }

        // get valid neighbors posistions
        private static IEnumerable<Position> GetNeighbors(Position pos, int width, int height, IEnumerable<Obstacle> obstacles)
        {
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            var obstaclePositions = new HashSet<Position>(obstacles.Select(o => o.Pos));

            return directions
                .Select(d => new Position(pos.X + d.Item1, pos.Y + d.Item2))
                .Where(p => p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height && !obstaclePositions.Contains(p));
        }

        // returns moved animal as a new object
        private static Animal MoveAnimal(Animal animal, int width, int height, IEnumerable<Obstacle> obstacles)
        {
            var neighbors = GetNeighbors(animal.Pos, width, height, obstacles).ToList();
            if (neighbors.Count == 0) return animal.ChangeEnergy(-1);

            var newPos = neighbors[_random.Next(neighbors.Count)];
            return animal.Move(newPos).ChangeEnergy(-1);
        }

        // herbivore tries to eat plant
        private static (Animal updatedAnimal, bool atePlant) HerbivoreEat(Animal animal, IEnumerable<Plant> plants)
        {
            var hasPlant = plants.Any(p => p.Pos == animal.Pos); // check if there plant in position of herb
            return hasPlant
                ? (animal.ChangeEnergy(20), true)
                : (animal, false);
        }

        // carnivore tries to eat herbivore
        private static (Animal updatedCarnivore, Animal? eatenHerbivore) CarnivoreEat(Animal carnivore, IEnumerable<Animal> herbivores)
        {
            var prey = herbivores.FirstOrDefault(h => h.Pos == carnivore.Pos && h.Type == AnimalType.Herbivore);
            return prey != null
                ? (carnivore.ChangeEnergy(30), prey)
                : (carnivore, null);
        }

        // returns animal + offspring (if there is)
        private static IEnumerable<Animal> TryReproduce(Animal animal, int nextId)
        {
            int threshold = animal.Type == AnimalType.Herbivore ? 60 : 80;

            if (animal.Energy >= threshold)
            {
                var offspring = new Animal(animal.Pos, 30, nextId, animal.Type);
                return new[] { animal.ChangeEnergy(-30), offspring };
            }

            return new[] { animal };
        }

        public static SimulationState Step(SimulationState state)
        {
            var events = new List<SimulationEvent>();

            // move all animals
            var movedAnimals = state.Animals
                .Select(a => MoveAnimal(a, state.Width, state.Height, state.Obstacles))
                .ToList();

            // herbivores eat plants
            var eatenPlantPositions = new HashSet<Position>();
            var herbivores = movedAnimals.Where(a => a.Type == AnimalType.Herbivore).ToList();
            var carnivores = movedAnimals.Where(a => a.Type == AnimalType.Carnivore).ToList();

            var fedHerbivores = herbivores
                .Select(herb =>
                {
                    var (updatedHerb, atePlant) = HerbivoreEat(herb, state.Plants);
                    if (atePlant)
                    {
                        eatenPlantPositions.Add(herb.Pos);
                        events.Add(new SimulationEvent($"Herbivore #{herb.Id} ate a plant at point ({herb.Pos.X}, {herb.Pos.Y})."));
                    }
                    return updatedHerb;
                })
                .ToList();

            // remove eaten plants
            var remainingPlants = state.Plants
                .Where(p => !eatenPlantPositions.Contains(p.Pos))
                .ToList();

            // carnivores eat herbivores
            var eatenHerbivoreIds = new HashSet<int>();
            var fedCarnivores = carnivores
                .Select(carn =>
                {
                    var (updatedCarn, eatenPrey) = CarnivoreEat(carn, fedHerbivores);
                    if (eatenPrey != null)
                    {
                        eatenHerbivoreIds.Add(eatenPrey.Id);
                        events.Add(new SimulationEvent($"Carnivore #{carn.Id} ate Herbivore #{eatenPrey.Id} at point ({carn.Pos.X}, {carn.Pos.Y})."));
                    }
                    return updatedCarn;
                })
                .ToList();

            // remove eaten herbivores
            var remainingHerbivores = fedHerbivores
                .Where(h => !eatenHerbivoreIds.Contains(h.Id))
                .ToList();

            var allFedAnimals = remainingHerbivores.Concat(fedCarnivores).ToList();

            // reproduce animals
            var nextId = state.Animals.Any() ? state.Animals.Max(a => a.Id) + 1 : 0;
            var reproducedAnimals = allFedAnimals
                .SelectMany((a, idx) =>
                {
                    var result = TryReproduce(a, nextId + idx).ToList();
                    if (result.Count > 1)
                    {
                        var animalType = a.Type == AnimalType.Herbivore ? "Herbivore" : "Carnivore";
                        events.Add(new SimulationEvent($"{animalType} #{a.Id} reproduced an offspring with id (#{result[1].Id}) at point ({a.Pos.X}, {a.Pos.Y})."));
                    }
                    return result;
                })
                .ToList();

            // remove dead animals
            var aliveAnimals = reproducedAnimals
                .Where(a => a.Energy > 0)
                .ToList();

            // spawn new plants
            int newPlantCount = _random.Next(1, 8);
            var newPlants = Enumerable.Range(0, newPlantCount)
                .Select(_ => new Plant(new Position(_random.Next(state.Width), _random.Next(state.Height))))
                .ToList();

            var allPlants = remainingPlants.Concat(newPlants).ToList();
            events.Add(new SimulationEvent($"{newPlantCount} new plant(s) spawned."));

            return new SimulationState(
                state.Width,
                state.Height,
                aliveAnimals,
                allPlants,
                state.Obstacles,
                events,
                state.Turn + 1
            );
        }

        public static void Display(SimulationState state)
        {
            var herbivorePositions = new HashSet<Position>(state.Animals.Where(a => a.Type == AnimalType.Herbivore).Select(a => a.Pos));
            var carnivorePositions = new HashSet<Position>(state.Animals.Where(a => a.Type == AnimalType.Carnivore).Select(a => a.Pos));
            var plantPositions = new HashSet<Position>(state.Plants.Select(p => p.Pos));
            var obstaclePositions = new HashSet<Position>(state.Obstacles.Select(o => o.Pos));

            var herbivoreCount = state.Animals.Count(a => a.Type == AnimalType.Herbivore);
            var carnivoreCount = state.Animals.Count(a => a.Type == AnimalType.Carnivore);

            Console.WriteLine($"\n=== Turn {state.Turn} | Herbivores: {herbivoreCount} | Carnivores: {carnivoreCount} | Plants: {state.Plants.Count} ===");

            for (int y = 0; y < state.Height; y++)
            {
                for (int x = 0; x < state.Width; x++)
                {
                    var pos = new Position(x, y);

                    if (carnivorePositions.Contains(pos))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("C ");
                    }
                    else if (herbivorePositions.Contains(pos))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("H ");
                    }
                    else if (obstaclePositions.Contains(pos))
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("X ");
                    }
                    else if (plantPositions.Contains(pos))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("P ");
                    }
                    else
                    {
                        Console.ResetColor();
                        Console.Write(". ");
                    }
                }
                Console.WriteLine();
            }

            Console.ResetColor();

            if (state.Events.Any())
            {
                Console.WriteLine("\nEvents:");
                foreach (var evt in state.Events)
                {
                    Console.WriteLine($"- {evt.Message}");
                }
            }
        }
    }

    class Program
    {
        static void Main()
        {
            int width = 20, height = 20;
            int herbivoreCount = 15, carnivoreCount = 5;
            int plantCount = 30, obstacleCount = 15;

            var startingState = FunctionalSimulation.Initialize(width, height, herbivoreCount, carnivoreCount, plantCount, obstacleCount);

            Console.WriteLine(" H=Herbivore, C=Carnivore, P=Plant, X=Obstacle, .=Empty");
            FunctionalSimulation.Display(startingState);

            Console.WriteLine("\nPress any key to start simulation...");
            Console.ReadKey();

            var currentState = startingState;

            while (true)
            {
                currentState = FunctionalSimulation.Step(currentState);
                FunctionalSimulation.Display(currentState);
                Thread.Sleep(2000);

                if (currentState.Animals.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n═══════════════════════════════════════════════");
                    Console.WriteLine("All animals died! Ecosystem collapsed.");
                    Console.WriteLine("═══════════════════════════════════════════════");
                    Console.ResetColor();
                    break;
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}