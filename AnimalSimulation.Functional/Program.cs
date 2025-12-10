namespace FunctionalAnimalSimulation
{
    public record Position(int X, int Y);

    public enum AnimalType { Herbivore, Carnivore }

    public record Animal(Position Pos, int Energy, int Id, AnimalType Type)
    {
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
        int Turn,
        Random Rng
    );

    public static class FunctionalSimulation
    {
        public static SimulationState Initialize(int width, int height, int herbivoreCount, int carnivoreCount, int plantCount, int obstacleCount)
        {
            var random = new Random();

            var herbivores = Enumerable.Range(0, herbivoreCount)
                .Select(id => new Animal(
                    new Position(random.Next(width), random.Next(height)),
                    50,
                    id,
                    AnimalType.Herbivore
                ))
                .ToList();

            var carnivores = Enumerable.Range(herbivoreCount, carnivoreCount)
                .Select(id => new Animal(
                    new Position(random.Next(width), random.Next(height)),
                    50,
                    id,
                    AnimalType.Carnivore
                ))
                .ToList();

            var allAnimals = herbivores.Concat(carnivores).ToList();

            var plants = Enumerable.Range(0, plantCount)
                .Select(_ => new Plant(
                    new Position(random.Next(width), random.Next(height))
                ))
                .ToList();

            var obstacles = Enumerable.Range(0, obstacleCount)
                .Select(_ => new Obstacle(
                    new Position(random.Next(width), random.Next(height))
                ))
                .ToList();

            return new SimulationState(width, height, allAnimals, plants, obstacles, new List<SimulationEvent>(), 0, random);
        }

        private static IEnumerable<Position> GetNeighbors(Position pos, int width, int height, IEnumerable<Obstacle> obstacles)
        {
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            var obstaclePositions = new HashSet<Position>(obstacles.Select(o => o.Pos));

            return directions
                .Select(d => new Position(pos.X + d.Item1, pos.Y + d.Item2))
                .Where(p => p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height && !obstaclePositions.Contains(p));
        }

        private static Animal MoveAnimal(Animal animal, int width, int height, IEnumerable<Obstacle> obstacles, Random rng)
        {
            var neighbors = GetNeighbors(animal.Pos, width, height, obstacles).ToList();
            if (neighbors.Count == 0) return animal.ChangeEnergy(-1);

            var newPos = neighbors[rng.Next(neighbors.Count)];
            return animal.Move(newPos).ChangeEnergy(-1);
        }

        private static List<Animal> MoveAnimalsRecursive(IReadOnlyList<Animal> animals, int index, int width, int height, IEnumerable<Obstacle> obstacles, Random rng)
        {
            if (index >= animals.Count)
                return new List<Animal>();

            var movedAnimal = MoveAnimal(animals[index], width, height, obstacles, rng);
            var remainingMoved = MoveAnimalsRecursive(animals, index + 1, width, height, obstacles, rng);

            return new[] { movedAnimal }.Concat(remainingMoved).ToList();
        }

        private static (Animal updatedAnimal, bool atePlant) HerbivoreEat(Animal animal, IEnumerable<Plant> plants)
        {
            var hasPlant = plants.Any(p => p.Pos == animal.Pos);
            return hasPlant
                ? (animal.ChangeEnergy(20), true)
                : (animal, false);
        }

        private static (List<Animal> fedHerbivores, HashSet<Position> eatenPlants, List<SimulationEvent> events)
            FeedHerbivoresRecursive(IReadOnlyList<Animal> herbivores, int index, IReadOnlyList<Plant> plants)
        {
            if (index >= herbivores.Count)
                return (new List<Animal>(), new HashSet<Position>(), new List<SimulationEvent>());

            var herb = herbivores[index];
            var (updatedHerb, atePlant) = HerbivoreEat(herb, plants);

            var (restAnimals, restEatenPlants, restEvents) = FeedHerbivoresRecursive(herbivores, index + 1, plants);

            var allAnimals = new[] { updatedHerb }.Concat(restAnimals).ToList();
            var allEatenPlants = new HashSet<Position>(restEatenPlants);
            var allEvents = new List<SimulationEvent>(restEvents);

            if (atePlant)
            {
                allEatenPlants.Add(herb.Pos);
                allEvents.Insert(0, new SimulationEvent($"Herbivore #{herb.Id} ate a plant at point ({herb.Pos.X}, {herb.Pos.Y})."));
            }

            return (allAnimals, allEatenPlants, allEvents);
        }

        private static (Animal updatedCarnivore, Animal? eatenHerbivore) CarnivoreEat(Animal carnivore, IEnumerable<Animal> herbivores)
        {
            var prey = herbivores.FirstOrDefault(h => h.Pos == carnivore.Pos && h.Type == AnimalType.Herbivore);
            return prey != null
                ? (carnivore.ChangeEnergy(30), prey)
                : (carnivore, null);
        }

        private static (List<Animal> fedCarnivores, HashSet<int> eatenHerbivoreIds, List<SimulationEvent> events)
            FeedCarnivoresRecursive(IReadOnlyList<Animal> carnivores, int index, IReadOnlyList<Animal> herbivores)
        {
            if (index >= carnivores.Count)
                return (new List<Animal>(), new HashSet<int>(), new List<SimulationEvent>());

            var carn = carnivores[index];
            var (updatedCarn, eatenPrey) = CarnivoreEat(carn, herbivores);

            var (restAnimals, restEatenIds, restEvents) = FeedCarnivoresRecursive(carnivores, index + 1, herbivores);

            var allAnimals = new[] { updatedCarn }.Concat(restAnimals).ToList();
            var allEatenIds = new HashSet<int>(restEatenIds);
            var allEvents = new List<SimulationEvent>(restEvents);

            if (eatenPrey != null)
            {
                allEatenIds.Add(eatenPrey.Id);
                allEvents.Insert(0, new SimulationEvent($"Carnivore #{carn.Id} ate Herbivore #{eatenPrey.Id} at point ({carn.Pos.X}, {carn.Pos.Y})."));
            }

            return (allAnimals, allEatenIds, allEvents);
        }

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

        private static (List<Animal> allAnimals, List<SimulationEvent> events, int nextId)
            ReproduceAnimalsRecursive(IReadOnlyList<Animal> animals, int index, int nextId)
        {
            if (index >= animals.Count)
                return (new List<Animal>(), new List<SimulationEvent>(), nextId);

            var animal = animals[index];
            var offspring = TryReproduce(animal, nextId).ToList();

            var newNextId = offspring.Count > 1 ? nextId + 1 : nextId;
            var (restAnimals, restEvents, finalNextId) = ReproduceAnimalsRecursive(animals, index + 1, newNextId);

            var allAnimals = offspring.Concat(restAnimals).ToList();
            var allEvents = new List<SimulationEvent>(restEvents);

            if (offspring.Count > 1)
            {
                var animalType = animal.Type == AnimalType.Herbivore ? "Herbivore" : "Carnivore";
                allEvents.Insert(0, new SimulationEvent($"{animalType} #{animal.Id} reproduced an offspring with id (#{offspring[1].Id}) at point ({animal.Pos.X}, {animal.Pos.Y})."));
            }

            return (allAnimals, allEvents, finalNextId);
        }

        public static SimulationState Step(SimulationState state)
        {
            var events = new List<SimulationEvent>();

            var movedAnimals = MoveAnimalsRecursive(state.Animals, 0, state.Width, state.Height, state.Obstacles, state.Rng);

            var herbivores = movedAnimals.Where(a => a.Type == AnimalType.Herbivore).ToList();
            var carnivores = movedAnimals.Where(a => a.Type == AnimalType.Carnivore).ToList();

            var (fedHerbivores, eatenPlantPositions, herbEvents) =
                FeedHerbivoresRecursive(herbivores, 0, state.Plants);
            events.AddRange(herbEvents);

            var remainingPlants = state.Plants
                .Where(p => !eatenPlantPositions.Contains(p.Pos))
                .ToList();

            var (fedCarnivores, eatenHerbivoreIds, carnEvents) =
                FeedCarnivoresRecursive(carnivores, 0, fedHerbivores);
            events.AddRange(carnEvents);

            var remainingHerbivores = fedHerbivores
                .Where(h => !eatenHerbivoreIds.Contains(h.Id))
                .ToList();

            var allFedAnimals = remainingHerbivores.Concat(fedCarnivores).ToList();

            var nextId = state.Animals.Any() ? state.Animals.Max(a => a.Id) + 1 : 0;
            var (reproducedAnimals, reproEvents, finalNextId) =
                ReproduceAnimalsRecursive(allFedAnimals, 0, nextId);
            events.AddRange(reproEvents);

            var aliveAnimals = reproducedAnimals
                .Where(a => a.Energy > 0)
                .ToList();

            int newPlantCount = state.Rng.Next(1, 8);
            var newPlants = Enumerable.Range(0, newPlantCount)
                .Select(_ => new Plant(new Position(state.Rng.Next(state.Width), state.Rng.Next(state.Height))))
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
                state.Turn + 1,
                state.Rng
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