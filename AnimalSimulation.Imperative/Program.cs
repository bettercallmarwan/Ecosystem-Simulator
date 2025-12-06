// ============================================
// IMPERATIVE PROGRAMMING APPROACH
// ============================================

namespace ImperativeAnimalSimulation
{
    // mutable classes
    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Position other)
                return X == other.X && Y == other.Y;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    public enum AnimalType { Herbivore, Carnivore }

    public class Animal
    {
        public Position Pos { get; set; }
        public int Energy { get; set; }
        public int Id { get; set; }
        public AnimalType Type { get; set; }

        public Animal(Position pos, int energy, int id, AnimalType type)
        {
            Pos = pos;
            Energy = energy;
            Id = id;
            Type = type;
        }
    }

    public class Plant
    {
        public Position Pos { get; set; }

        public Plant(Position pos)
        {
            Pos = pos;
        }
    }

    public class Obstacle
    {
        public Position Pos { get; set; }

        public Obstacle(Position pos)
        {
            Pos = pos;
        }
    }

    public class Simulation
    {
        private int width;
        private int height;
        private List<Animal> animals;
        private List<Plant> plants;
        private List<Obstacle> obstacles;
        private List<string> events;
        private int turn;
        private Random random;
        private int nextAnimalId;

        public Simulation(int width, int height, int herbivoreCount, int carnivoreCount, int plantCount, int obstacleCount)
        {
            this.width = width;
            this.height = height;
            this.animals = new List<Animal>();
            this.plants = new List<Plant>();
            this.obstacles = new List<Obstacle>();
            this.events = new List<string>();
            this.turn = 0;
            this.random = new Random();
            this.nextAnimalId = 0;

            // initialize herbivores
            for (int i = 0; i < herbivoreCount; i++)
            {
                animals.Add(new Animal(
                    new Position(random.Next(width), random.Next(height)),
                    50,
                    nextAnimalId++,
                    AnimalType.Herbivore
                ));
            }

            // initialize carnivores
            for (int i = 0; i < carnivoreCount; i++)
            {
                animals.Add(new Animal(
                    new Position(random.Next(width), random.Next(height)),
                    50,
                    nextAnimalId++,
                    AnimalType.Carnivore
                ));
            }

            // initialize plants
            for (int i = 0; i < plantCount; i++)
            {
                plants.Add(new Plant(
                    new Position(random.Next(width), random.Next(height))
                ));
            }

            // initialize obstacles
            for (int i = 0; i < obstacleCount; i++)
            {
                obstacles.Add(new Obstacle(
                    new Position(random.Next(width), random.Next(height))
                ));
            }
        }

        // check if position has obstacle
        private bool IsObstacle(int x, int y)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].Pos.X == x && obstacles[i].Pos.Y == y)
                    return true;
            }
            return false;
        }

        // directly modify animal position
        private void MoveAnimal(Animal animal)
        {
            int[][] directions = { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };
            List<Position> validMoves = new List<Position>();

            // find valid moves
            for (int i = 0; i < directions.Length; i++)
            {
                int newX = animal.Pos.X + directions[i][0];
                int newY = animal.Pos.Y + directions[i][1];

                if (newX >= 0 && newX < width && newY >= 0 && newY < height && !IsObstacle(newX, newY))
                {
                    validMoves.Add(new Position(newX, newY));
                }
            }

            // move to random valid position
            if (validMoves.Count > 0)
            {
                Position newPos = validMoves[random.Next(validMoves.Count)];
                animal.Pos.X = newPos.X;
                animal.Pos.Y = newPos.Y;
            }

            animal.Energy -= 1;
        }

        // herbivores try to eat plant
        private void HerbivoreEat(Animal herbivore)
        {
            for (int i = plants.Count - 1; i >= 0; i--)
            {
                if (plants[i].Pos.Equals(herbivore.Pos))
                {
                    herbivore.Energy += 20;
                    events.Add($"Herbivore #{herbivore.Id} ate a plant at point ({herbivore.Pos.X}, {herbivore.Pos.Y}).");
                    plants.RemoveAt(i);
                    break;
                }
            }
        }

        // carnivore tries to eat herbivore
        private void CarnivoreEat(Animal carnivore, List<int> eatenIds)
        {
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i].Type == AnimalType.Herbivore &&
                    animals[i].Pos.Equals(carnivore.Pos) &&
                    !eatenIds.Contains(animals[i].Id))
                {
                    carnivore.Energy += 30;
                    events.Add($"Carnivore #{carnivore.Id} ate Herbivore with id #{animals[i].Id} at point ({carnivore.Pos.X}, {carnivore.Pos.Y}).");
                    eatenIds.Add(animals[i].Id);
                    break;
                }
            }
        }

        // directly add new animal if reproduction occurs
        private void TryReproduce(Animal animal)
        {
            int threshold = animal.Type == AnimalType.Herbivore ? 60 : 80;

            if (animal.Energy >= threshold)
            {
                animal.Energy -= 30;
                Animal offspring = new Animal(
                    new Position(animal.Pos.X, animal.Pos.Y),
                    30,
                    nextAnimalId++,
                    animal.Type
                );
                animals.Add(offspring);

                string animalType = animal.Type == AnimalType.Herbivore ? "Herbivore" : "Carnivore";
                events.Add($"{animalType} #{animal.Id} reproduced an offspring with id (#{offspring.Id}) at point ({animal.Pos.X}, {animal.Pos.Y}).");
            }
        }

        public void Step()
        {
            events.Clear();

            // move all animals
            for (int i = 0; i < animals.Count; i++)
            {
                MoveAnimal(animals[i]);
            }

            // herbivores eat plants
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i].Type == AnimalType.Herbivore)
                {
                    HerbivoreEat(animals[i]);
                }
            }

            // carnivores eat herbivores
            List<int> eatenHerbivoreIds = new List<int>();
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i].Type == AnimalType.Carnivore)
                {
                    CarnivoreEat(animals[i], eatenHerbivoreIds);
                }
            }

            // remove eaten herbivores
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                if (eatenHerbivoreIds.Contains(animals[i].Id))
                {
                    animals.RemoveAt(i);
                }
            }

            // reproduce animals
            int originalCount = animals.Count;
            for (int i = 0; i < originalCount; i++)
            {
                TryReproduce(animals[i]);
            }

            // remove dead animals
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                if (animals[i].Energy <= 0)
                {
                    animals.RemoveAt(i);
                }
            }

            // spawn new plants
            int newPlantCount = random.Next(1, 8);
            for (int i = 0; i < newPlantCount; i++)
            {
                plants.Add(new Plant(
                    new Position(random.Next(width), random.Next(height))
                ));
            }
            events.Add($"{newPlantCount} new plant(s) spawned.");

            turn++;
        }

        // display current state
        public void Display()
        {
            HashSet<string> herbivorePositions = new HashSet<string>();
            HashSet<string> carnivorePositions = new HashSet<string>();
            HashSet<string> plantPositions = new HashSet<string>();
            HashSet<string> obstaclePositions = new HashSet<string>();

            for (int i = 0; i < animals.Count; i++)
            {
                string key = $"{animals[i].Pos.X},{animals[i].Pos.Y}";
                if (animals[i].Type == AnimalType.Herbivore)
                    herbivorePositions.Add(key);
                else
                    carnivorePositions.Add(key);
            }

            for (int i = 0; i < plants.Count; i++)
            {
                plantPositions.Add($"{plants[i].Pos.X},{plants[i].Pos.Y}");
            }

            for (int i = 0; i < obstacles.Count; i++)
            {
                obstaclePositions.Add($"{obstacles[i].Pos.X},{obstacles[i].Pos.Y}");
            }

            int herbivoreCount = 0;
            int carnivoreCount = 0;
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i].Type == AnimalType.Herbivore)
                    herbivoreCount++;
                else
                    carnivoreCount++;
            }

            Console.WriteLine($"\n=== Turn {turn} | Herbivores: {herbivoreCount} | Carnivores: {carnivoreCount} | Plants: {plants.Count} ===");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    string key = $"{x},{y}";

                    if (carnivorePositions.Contains(key))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("C ");
                    }
                    else if (herbivorePositions.Contains(key))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("H ");
                    }
                    else if (obstaclePositions.Contains(key))
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("X ");
                    }
                    else if (plantPositions.Contains(key))
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

            if (events.Count > 0)
            {
                Console.WriteLine("\nEvents:");
                for (int i = 0; i < events.Count; i++)
                {
                    Console.WriteLine($"- {events[i]}");
                }
            }
        }

        public int GetAnimalCount() => animals.Count;
    }

    class Program
    {
        static void Main()
        {
            int width = 20, height = 20;
            int herbivoreCount = 15, carnivoreCount = 5;
            int plantCount = 30, obstacleCount = 15;

            Simulation sim = new Simulation(width, height, herbivoreCount, carnivoreCount, plantCount, obstacleCount);

            Console.WriteLine(" H=Herbivore, C=Carnivore, P=Plant, X=Obstacle, .=Empty");
            sim.Display();

            Console.WriteLine("\nPress any key to start simulation...");
            Console.ReadKey();

            while (true)
            {
                sim.Step();
                sim.Display();
                Thread.Sleep(2000);

                if (sim.GetAnimalCount() == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n═══════════════════════════════════════════════");
                    Console.WriteLine("All animals are dead, Ecosystem collapsed.");
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