using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using PluginInterface;

namespace SnakeGenetic

{
    public class Genetic
    {
        public static void Start()
        {
            var rnd = new Random();
            var s = new GeneticSnake();
            //ReadXML(s, "D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeightsTempLLL.xml");
            //WriteXML(s,"D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeightsTempLLL.xml");
            var pop = new Population(5000);
            while (pop.avgScore < 3000)
            {
                _mutationRate = (float) rnd.Next(5, 31) / 100;
                while (!pop.Done())
                {
                    if (!pop.bestSnake.dead)
                    {
                        //pop.bestSnake.ShowGrid();
                    }

                    pop.Update();
                }

                var highScore = pop.bestSnake.score;
                Debug.Write("\n");
                pop.bestSnake.ShowGrid();

                pop.CalculateFitness();
                pop.NaturalSelection();

                Debug.WriteLine("GEN : " + pop.gen);
                Debug.WriteLine("MUTATION RATE : " + _mutationRate * 100 + "%");
                Debug.WriteLine("SCORE : " + highScore);
                Debug.WriteLine("HIGH SCORE : " + pop.bestSnakeScore);
                Debug.WriteLine("Average score: " + pop.avgScore);
                Debug.WriteLine("Best fitness: " + pop.bestFitness);
                Debug.WriteLine("Average fitness: " + pop.avgFitness);
                Debug.WriteLine("Best average fitness: " + pop.avgFitnessBest);
                //WriteXML(pop.bestSnake, "D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeightsTemp.xml");
            }

            //WriteXML(pop.bestSnake, "D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeights.xml");
        }

        private static float _mutationRate;

        private const int HiddenNodes = 8;
        private const int HiddenLayers = 2;

        private static void SaveWeights(Population pop, string path)
        {
            var mat = pop.bestSnake.brain.Pull();
            Stream saveFileStream = File.Create(path);
            var serializer = new BinaryFormatter();
            serializer.Serialize(saveFileStream, mat);
            saveFileStream.Close();
        }

        private static void LoadWeights(GeneticSnake snake)
        {
            var fileName = "D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeightsTempLLL.bin";
            if (!File.Exists(fileName)) return;
            Stream openFileStream = File.OpenRead(fileName);
            var deserializer = new BinaryFormatter();
            snake.brain.Load((Matrix[]) deserializer.Deserialize(openFileStream));
            openFileStream.Close();
        }

        private static void WriteXml(GeneticSnake snake, string path)
        {
            var mat = snake.brain.Pull();
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(Matrix[]));
            var file = File.Create(path);
            writer.Serialize(file, mat);
            file.Close();
        }

        private static void ReadXml(GeneticSnake snake, string path)
        {
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(Matrix[]));
            var file = new StreamReader(path);
            snake.brain.Load((Matrix[]) reader.Deserialize(file));
            file.Close();
        }

        [Serializable()]
        public class Matrix
        {
            private int _r;

            private const double Mean = 0;
            private const double StdDev = 1;

            static Normal _normalDist = new Normal(Mean, StdDev);
            Random _random = new Random();
            private double _randomGaussian;
            public int rows, cols;
            public double[][] matrix;

            public Matrix()
            {
            }

            public Matrix(int r, int c)
            {
                rows = r;
                cols = c;
                matrix = new double[rows][];
                for (var i = 0; i < rows; i++)
                {
                    matrix[i] = new double[cols];
                }
            }

            public Matrix(double[][] m)
            {
                matrix = m;
                rows = matrix.Length;
                cols = matrix[0].Length;
            }

            public void Output()
            {
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        Console.WriteLine(matrix[i][j] + " ");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();
            }

            public Matrix Dot(Matrix n)
            {
                var result = new Matrix(rows, n.cols);

                if (cols != n.rows) return result;
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < n.cols; j++)
                    {
                        double sum = 0;
                        for (var k = 0; k < cols; k++)
                        {
                            sum += matrix[i][k] * n.matrix[k][j];
                        }

                        result.matrix[i][j] = sum;
                    }
                }

                return result;
            }

            public void Randomize()
            {
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        _r = _random.Next(0, 2);
                        switch (_r)
                        {
                            case 0:
                                matrix[i][j] = _random.NextDouble();
                                break;
                            case 1:
                                matrix[i][j] = _random.NextDouble() * -1;
                                break;
                            default:
                                matrix[i][j] = _random.NextDouble();
                                break;
                        }
                    }
                }
            }

            public Matrix SingleColumnMatrixFromArray(float[] arr)
            {
                var n = new Matrix(arr.Length, 1);
                for (var i = 0; i < arr.Length; i++)
                {
                    n.matrix[i][0] = arr[i];
                }

                return n;
            }

            public double[] ToArray()
            {
                var arr = new double[rows * cols];
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        arr[j + i * cols] = matrix[i][j];
                    }
                }

                return arr;
            }

            public Matrix AddBias()
            {
                var n = new Matrix(rows + 1, 1);
                for (var i = 0; i < rows; i++)
                {
                    n.matrix[i][0] = matrix[i][0];
                }

                n.matrix[rows][0] = 1;
                return n;
            }

            public Matrix Activate()
            {
                var n = new Matrix(rows, cols);
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        n.matrix[i][j] = Relu(matrix[i][j]);
                    }
                }

                return n;
            }

            private double Relu(double x)
            {
                return Math.Max(0, x);
            }

            public void Mutate(double mutationRate)
            {
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        var rand = _random.NextDouble();
                        if (!(rand < mutationRate)) continue;
                        _randomGaussian = _normalDist.Sample();
                        matrix[i][j] += _randomGaussian / 5;

                        if (matrix[i][j] > 1)
                        {
                            matrix[i][j] = 1;
                        }

                        if (matrix[i][j] < -1)
                        {
                            matrix[i][j] = -1;
                        }
                    }
                }
            }

            public Matrix Crossover(Matrix partner)
            {
                var child = new Matrix(rows, cols);

                var randC = _random.Next(cols);
                var randR = _random.Next(rows);

                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        if (i < randR || i == randR && j <= randC)
                        {
                            child.matrix[i][j] = matrix[i][j];
                        }
                        else
                        {
                            child.matrix[i][j] = partner.matrix[i][j];
                        }
                    }
                }

                return child;
            }

            public Matrix Clone()
            {
                var clone = new Matrix(rows, cols);
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        clone.matrix[i][j] = matrix[i][j];
                    }
                }

                return clone;
            }
        }

        public class NeuralNet
        {
            private readonly int _iNodes, _hNodes, _oNodes, _hLayers;
            private readonly Matrix[] _weights;

            public NeuralNet(int input, int hidden, int output, int hiddenLayers)
            {
                _iNodes = input;
                _hNodes = hidden;
                _oNodes = output;
                _hLayers = hiddenLayers;

                _weights = new Matrix[_hLayers + 1];
                _weights[0] = new Matrix(_hNodes, _iNodes + 1);
                for (var i = 1; i < _hLayers; i++)
                {
                    _weights[i] = new Matrix(_hNodes, _hNodes + 1);
                }

                _weights[_weights.Length - 1] = new Matrix(_oNodes, _hNodes + 1);

                foreach (var w in _weights)
                {
                    w.Randomize();
                }
            }

            public void Mutate(float mr)
            {
                foreach (var w in _weights)
                {
                    w.Mutate(mr);
                }
            }

            public double[] Output(float[] inputsArr)
            {
                var inputs = _weights[0].SingleColumnMatrixFromArray(inputsArr);

                var currBias = inputs.AddBias();

                for (var i = 0; i < _hLayers; i++)
                {
                    var hiddenIp = _weights[i].Dot(currBias);
                    var hiddenOp = hiddenIp.Activate();
                    currBias = hiddenOp.AddBias();
                }

                var outputIp = _weights[_weights.Length - 1].Dot(currBias);
                var output = outputIp.Activate();

                return output.ToArray();
            }

            public NeuralNet Crossover(NeuralNet partner)
            {
                var child = new NeuralNet(_iNodes, _hNodes, _oNodes, _hLayers);
                for (var i = 0; i < _weights.Length; i++)
                {
                    child._weights[i] = _weights[i].Crossover(partner._weights[i]);
                }

                return child;
            }

            public NeuralNet Clone()
            {
                var clone = new NeuralNet(_iNodes, _hNodes, _oNodes, _hLayers);
                for (var i = 0; i < _weights.Length; i++)
                {
                    clone._weights[i] = _weights[i].Clone();
                }

                return clone;
            }

            public void Load(Matrix[] weight)
            {
                for (var i = 0; i < _weights.Length; i++)
                {
                    _weights[i] = weight[i];
                }
            }

            public Matrix[] Pull()
            {
                var model = new Matrix[_weights.Length];
                for (var i = 0; i < model.Length; i++)
                {
                    model[i] = _weights[i].Clone();
                }

                return model;
            }
        }

        public class GeneticSnake
        {
            private readonly Random _rnd;
            private readonly int _height, _width;
            public int score = 1;
            private int _lifeLeft = 200; //amount of moves the snake can make before it dies
            private int _lifetime; //amount of time the snake has been alive
            private int _xVel, _yVel;
            public float fitness;
            private byte[,] _grid;
            private List<Point> _foodList;
            public bool dead;
            private List<Point> _stones;
            private readonly float[] _vision; //snakes vision
            private double[] _decision; //snakes decision
            private Point _head;
            private List<Point> _body; //snakes body
            public NeuralNet brain;

            public GeneticSnake() : this(HiddenLayers)
            {
            }

            private GeneticSnake(int layers)
            {
                _vision = new float[24];
                _rnd = new Random();
                _height = _rnd.Next(25, 51);
                _width = _rnd.Next(25, 51);
//                _height = 25;
//                _width = 25;
                Generate(_width, _height);
                //ShowGrid();
                _decision = new double[4];
                brain = new NeuralNet(24, HiddenNodes, 4, layers);
            }

            private void Generate(int width, int height)
            {
                _stones = new List<Point>();
                _foodList = new List<Point>();
                _grid = new byte[width, height];
                _head = new Point(_rnd.Next(5, width - 5), _rnd.Next(5, height - 5));
                _body = new List<Point> {new Point(_head.X, _head.Y), new Point(_head.X, _head.Y)};
                _grid[_head.X, _head.Y] = 6;
                _grid[_body[0].X, _body[0].Y] = 5;
                for (var i = 0; i < width; i++)
                {
                    _grid[i, 0] = 1;
                    _grid[i, height - 1] = 1;
                }

                for (var i = 0; i < height; i++)
                {
                    _grid[0, i] = 1;
                    _grid[width - 1, i] = 1;
                }

                var terrainCount = 25;
                while (_stones.Count < terrainCount)
                {
                    var x = _rnd.Next(1, width);
                    var y = _rnd.Next(1, height);
                    if (_grid[x, y] == 5 || _grid[x, y] == 6) continue;
                    _grid[x, y] = 1;
                    _stones.Add(new Point(x, y));
                }

                GenerateFood();
            }

            private void GenerateFood()
            {
                while (_foodList.Count < 5)
                {
                    var x = _rnd.Next(1, _width);
                    var y = _rnd.Next(1, _height);
                    if (_grid[x, y] == 5 || _grid[x, y] == 1 || _grid[x, y] == 6) continue;
                    _foodList.Add(new Point(x, y));
                    _grid[x, y] = 2;
                }
            }

            public void ShowGrid()
            {
                var s = "";
                for (var i = 0; i < _width; i++)
                {
                    for (var j = 0; j < _height; j++)
                    {
                        switch (_grid[i, j])
                        {
                            case 0:
                                s += ".";
                                break;
                            case 1:
                                s += "#";
                                break;
                            case 3:
                                s += "3";
                                break;
                            case 4:
                                s += "4";
                                break;
                            case 5:
                                s += "8";
                                break;
                            case 6:
                                s += "2";
                                break;
                            case 7:
                                s += "7";
                                break;
                            case 9:
                                s += "9";
                                break;
                            default:
                                s += 1;
                                break;
                        }
                    }

                    s += "\n";
                }

                Debug.Write(s);
            }

            private bool BodyCollide(int x, int y)
            {
                foreach (var b in _body)
                {
                    if (!Equals(b, new Point(x, y))) continue;
                    return true;
                }

                return false;
            }

            private bool FoodCollide(int x, int y)
            {
                foreach (var food in _foodList)
                {
                    if (!Equals(food, new Point(x, y))) continue;
                    return true;
                }

                return false;
            }

            private bool WallCollide(int x, int y)
            {
                if (x < 1 || x >= _width - 1 || y < 1 || y >= _height - 1)
                    return true;
                foreach (var stone in _stones)
                {
                    if (!Equals(stone, new Point(x, y))) continue;
                    return true;
                }

                return false;
            }

            public void Move()
            {
                //move the snake
                if (dead) return;
                _lifetime++;
                _lifeLeft--;

                if (FoodCollide(_head.X, _head.Y))
                {
                    for (var i = 0; i < _foodList.Count; i++)
                    {
                        if (!Equals(_foodList[i], _head)) continue;
                        _foodList.Remove(_foodList[i]);
                    }

                    //_grid[_head.X, _head.Y] = 0;
                    Eat();
                    ShiftBody(true);
                }
                else
                {
                    ShiftBody(false);
                }

                if (WallCollide(_head.X, _head.Y))
                {
                    dead = true;
                    _grid[_head.X, _head.Y] = 7;
                }
                else if (BodyCollide(_head.X, _head.Y) && _lifetime > 1)
                {
                    dead = true;
                    _grid[_head.X, _head.Y] = 3;
                }
                else if (_lifeLeft <= 0)
                {
                    dead = true;
                    _grid[_head.X, _head.Y] = 4;
                }
            }

            private void Eat()
            {
                //eat food
                var len = _body.Count - 1;
                score++;
                if (_lifeLeft < 500)
                {
                    if (_lifeLeft > 400)
                    {
                        _lifeLeft = 500;
                    }
                    else
                    {
                        _lifeLeft += 100;
                    }
                }

                if (score % 5 == 0)
                    _body.Add(len >= 0 ? new Point(_body[len].X, _body[len].Y) : new Point(_head.X, _head.Y));

                GenerateFood();
            }

            private void ShiftBody(bool ate)
            {
                //shift the body to follow the head
                var tempX = _head.X;
                var tempY = _head.Y;
                _head.X += _xVel;
                _head.Y += _yVel;
                _grid[_head.X, _head.Y] = 6;
                //ShowGrid();
                for (var x = 0; x < _body.Count; x++)
                {
                    //ShowGrid();
                    var temp2X = _body[x].X;
                    var temp2Y = _body[x].Y;
                    _body[x] = new Point(tempX, tempY);
                    _grid[_body[x].X, _body[x].Y] = 5;
                    tempX = temp2X;
                    tempY = temp2Y;
                }

                if (!ate || score % 5 != 0)
//                if (!ate)
                {
                    _grid[tempX, tempY] = 0;
                    //ShowGrid();
                }

                //ShowGrid();
            }

            public GeneticSnake Clone()
            {
                var clone = new GeneticSnake(HiddenLayers) {brain = brain.Clone()};
                return clone;
            }

            public GeneticSnake Crossover(GeneticSnake parent)
            {
                //crossover the snake with another snake
                var child = new GeneticSnake(HiddenLayers) {brain = brain.Crossover(parent.brain)};
                return child;
            }

            public void Mutate()
            {
                //mutate the snakes brain
                brain.Mutate(_mutationRate);
            }

            public void CalculateFitness()
            {
                fitness = _lifetime * score;
            }

            public void Look()
            {
                const int c = 3;
                for (var i = 0; i < 24; i += 3)
                {
                    float[] temp;
                    switch (i)
                    {
                        case 0:
                            temp = LookInDirection(new Size(-1, 0));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 1:
                            temp = LookInDirection(new Size(-1, -1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 2:
                            temp = LookInDirection(new Size(0, -1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 3:
                            temp = LookInDirection(new Size(1, -1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 4:
                            temp = LookInDirection(new Size(1, 0));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 5:
                            temp = LookInDirection(new Size(1, 1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 6:
                            temp = LookInDirection(new Size(0, 1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                        case c * 7:
                            temp = LookInDirection(new Size(-1, 1));
                            _vision[i] = temp[0];
                            _vision[i + 1] = temp[1];
                            _vision[i + 2] = temp[2];
                            break;
                    }
                }
            }

            private float[] LookInDirection(Size direction)
            {
                var temp = (byte[,]) _grid.Clone();
                float[] look = {0, 0, 0};
                var pos = new Point(_head.X, _head.Y);
                var found = false;
                pos += direction;
                while (!WallCollide(pos.X, pos.Y))
                {
                    if (!found && FoodCollide(pos.X, pos.Y))
                    {
                        found = true;
                        look[0] = 1;
                    }

                    if (!found && BodyCollide(pos.X, pos.Y))
                    {
                        found = true;
                        look[1] = 1 / ((float) Math.Abs(_head.X - pos.X) + Math.Abs(_head.Y - pos.Y));
                    }

                    _grid[pos.X, pos.Y] = 9;
                    pos += direction;
                }

                look[2] = 1 / ((float) Math.Abs(_head.X - pos.X) + Math.Abs(_head.Y - pos.Y)) - 0.01f;
                //ShowGrid();
                _grid = temp;
                return look;
            }

            public void Think()
            {
                //ShowGrid();
                _decision = brain.Output(_vision);
                var maxIndex = 0;
                double max = 0;
                for (var i = 0; i < _decision.Length; i++)
                {
                    if (!(_decision[i] > max)) continue;
                    max = _decision[i];
                    maxIndex = i;
                }

                switch (maxIndex)
                {
                    case 0:
                        MoveUp();
                        break;
                    case 1:
                        MoveDown();
                        break;
                    case 2:
                        MoveLeft();
                        break;
                    case 3:
                        MoveRight();
                        break;
                }
            }

            private void MoveUp()
            {
                if (_xVel == 1) return;
                _xVel = -1;
                _yVel = 0;
            }

            private void MoveDown()
            {
                if (_xVel == -1) return;
                _xVel = 1;
                _yVel = 0;
            }

            private void MoveLeft()
            {
                if (_yVel == 1) return;
                _xVel = 0;
                _yVel = -1;
            }

            private void MoveRight()
            {
                if (_yVel == -1) return;
                _xVel = 0;
                _yVel = 1;
            }
        }

        private class Population
        {
            public float avgScore, avgFitness, avgFitnessBest;
            private readonly int _size;
            private readonly List<int> _evolution;
            private readonly RandomSource _rnd = new Mcg31m1();
            private readonly GeneticSnake[] _snakes;
            public GeneticSnake bestSnake;

            public int bestSnakeScore;
            public int gen;

            public float bestFitness;
            private double _fitnessSum;

            public Population(int size)
            {
                _size = size;
                _evolution = new List<int>();
                _snakes = new GeneticSnake[size];
                for (var i = 0; i < _snakes.Length; i++)
                {
                    _snakes[i] = new GeneticSnake();
                }

                bestSnake = _snakes[0].Clone();
                //bestSnake.replay = true;
            }

            public Population(int size, GeneticSnake best)
            {
                _size = size;
                _evolution = new List<int>();
                _snakes = new GeneticSnake[size];
                for (var i = 0; i < _snakes.Length; i++)
                {
                    _snakes[i] = best.Clone();
                }

                bestSnake = _snakes[0].Clone();
            }

            public bool Done()
            {
                return _snakes.Count(t => !t.dead) <= 0 && bestSnake.dead;
            }

            public void Update()
            {
                if (!bestSnake.dead)
                {
                    bestSnake.Look();
                    bestSnake.Think();
                    bestSnake.Move();
                }

                foreach (var snake in _snakes)
                {
                    if (snake.dead) continue;
                    snake.Look();
                    snake.Think();
                    snake.Move();
                }
            }

            private void SetBestSnake()
            {
                float max = 0;
                var maxIndex = 0;
                for (var i = 0; i < _snakes.Length; i++)
                {
                    if (!(_snakes[i].fitness > max)) continue;
                    max = _snakes[i].fitness;
                    maxIndex = i;
                }

                if (max > bestFitness)
                {
                    bestFitness = max;
                    bestSnake = _snakes[maxIndex].Clone();
                    bestSnakeScore = _snakes[maxIndex].score;
                }
                else
                {
                    bestSnake = bestSnake.Clone();
                }
            }

            private GeneticSnake SelectParent()
            {
                float rand = _rnd.Next((int) _fitnessSum);
                float summation = 0;
                foreach (var snake in _snakes)
                {
                    summation += snake.fitness;
                    if (summation > rand)
                    {
                        return snake;
                    }
                }

                return _snakes[0];
            }

            public void NaturalSelection()
            {
                SetBestSnake();
                CalculateFitnessSum();
                avgScore /= _size;
                avgFitness = (float) _fitnessSum / _size;
                if (avgFitnessBest <= avgFitness)
                {
                    avgFitnessBest = avgFitness;
                }

                var newSnakes = new GeneticSnake[_size];
                _snakes[0] = bestSnake.Clone();
                for (var i = 1; i < _snakes.Length; i++)
                {
                    var child = SelectParent().Crossover(SelectParent());
                    child.Mutate();
                    newSnakes[i] = child;
                }

                for (var i = 1; i < _snakes.Length; i++)
                {
                    _snakes[i] = newSnakes[i].Clone();
                }

                _evolution.Add(bestSnakeScore);
                gen += 1;
            }

            public void Mutate()
            {
                for (var i = 1; i < _snakes.Length; i++)
                {
                    _snakes[i].Mutate();
                }
            }

            public void CalculateFitness()
            {
                foreach (var snake in _snakes)
                {
                    snake.CalculateFitness();
                }
            }

            private void CalculateFitnessSum()
            {
                _fitnessSum = 0;
                avgScore = 0;
                foreach (var snake in _snakes)
                {
                    _fitnessSum += snake.fitness;
                    avgScore += snake.score;
                }
            }
        }
    }

    public class RandomSnake : ISmartSnake
    {
        public Move Direction { get; set; }
        public bool Reverse { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public static Point pos;
        private List<Point> _stones;
        Genetic _gen = new Genetic();
        private Genetic.NeuralNet _net;
        public static double[] decision;
        public static float[] vision;
        private int _xVel, _yVel;


        private bool BodyCollide(int x, int y, IEnumerable<Point> body)
        {
            return body.Contains(new Point(x, y));
        }

        private bool FoodCollide(int x, int y, IEnumerable<Point> food)
        {
            return food.Contains(new Point(x, y));
        }

        private bool WallCollide(int x, int y, IEnumerable<Point> stones)
        {
            return stones.Contains(new Point(x, y));
        }
        private float[] Look(Snake snake, IReadOnlyCollection<Point> food, IReadOnlyCollection<Point> stones)
        {
            var visionLook = new float[40];
            for (var i = 0; i < 40; i += 5)
            {
                float[] temp;
                switch (i)
                {
                    case 0:
                        temp = LookInDirection(new Size(0, -1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 1:
                        temp = LookInDirection(new Size(-1, -1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 2:
                        temp = LookInDirection(new Size(-1, 0), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 3:
                        temp = LookInDirection(new Size(-1, 1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 4:
                        temp = LookInDirection(new Size(0, 1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 5:
                        temp = LookInDirection(new Size(1, 1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 6:
                        temp = LookInDirection(new Size(1, 0), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                    case 5 * 7:
                        temp = LookInDirection(new Size(1, -1), snake, food, stones);
                        visionLook[i] = temp[0];
                        visionLook[i + 1] = temp[1];
                        visionLook[i + 2] = temp[2];
                        visionLook[i + 3] = temp[3];
                        visionLook[i + 4] = temp[4];
                        break;
                }
            }

            return visionLook;
        }

        private float[] LookInDirection(Size direction, Snake snake, IReadOnlyCollection<Point> food, IReadOnlyCollection<Point> stones)
        {
            float[] look = {0, 0, 0, 0, 0};
            var posSnake = new Point(snake.Position.X, snake.Position.Y);
            var found = false;
            posSnake += direction;
            while (!WallCollide(posSnake.X, posSnake.Y, stones))
            {
                if (!found && FoodCollide(posSnake.X, posSnake.Y, food))
                {
                    found = true;
                    look[0] = 1;
                }

                if (!found && BodyCollide(posSnake.X, posSnake.Y, snake.Tail))
                {
                    found = true;
                    look[1] = 1 / ((float) Math.Abs(snake.Position.X - posSnake.X) + Math.Abs(snake.Position.Y - posSnake.Y));
                }

                posSnake += direction;
            }

            look[2] = 1 / ((float) Math.Abs(snake.Position.X - posSnake.X) + Math.Abs(snake.Position.Y - posSnake.Y));
            look[3] = posSnake.X;
            look[4] = posSnake.Y;
            return look;
        }

        private Move MoveUp()
        {
            if (_xVel == 1)
                return Move.Down;
            _xVel = -1;
            _yVel = 0;
            return Move.Up;
        }

        private Move MoveDown()
        {
            if (_xVel == -1)
                return Move.Up;
            _xVel = 1;
            _yVel = 0;
            return Move.Down;
        }

        private Move MoveLeft()
        {
            if (_yVel == 1) return Move.Right;
            _xVel = 0;
            _yVel = -1;
            return Move.Left;
        }

        private Move MoveRight()
        {
            if (_yVel == -1) return Move.Left;
            _xVel = 0;
            _yVel = 1;
            return Move.Right;
        }

        private Move MoveSnake(int i)
        {
            switch (i)
            {
                case 0:
                    return MoveUp();
                case 1:
                    return MoveDown();
                case 2:
                    return MoveLeft();
                case 3:
                    return MoveRight();
            }

            return Move.Nothing;
        }

        public void Startup(Size size, List<Point> stones)
        {
            vision = new float[24];
            _net = new Genetic.NeuralNet(24, 8, 4, 2);
            Name = "Genetic Snake";
            Color = Color.Black;
            _stones = stones;
//            new Thread(Genetic.Start).Start();
            var reader = new System.Xml.Serialization.XmlSerializer(typeof(Genetic.Matrix[]));
            var file = new StreamReader("D:\\SnakeBattle\\Neralion\\SnakeBattle\\SnakeGenetic\\GeneticSnakeWeightsTempLLL.xml");
            _net.Load((Genetic.Matrix[]) reader.Deserialize(file));
            file.Close();
        }

        public void Update(Snake snake, List<Snake> enemies, List<Point> food, List<Point> dead)
        {
            var foods = new List<Point>(food);
            var stones = new List<Point>(_stones);
            foreach (var enemy in enemies)
            {
                foods.Add(enemy.Position);
            }

            foreach (var enemy in enemies)
            {
                foreach (var tail in enemy.Tail)
                {
                    stones.Add(tail);
                }
            }

            foreach (var deadPoint in dead)
            {
                stones.Add(deadPoint);
            }

            var v = Look(snake, foods, stones);
            var vis = new List<float>();
            for (var i = 0; i < v.Length; i += 5)
            {
                vis.Add(v[i]);
                vis.Add(v[i + 1]);
                vis.Add(v[i + 2]);
            }

            vision = (float[]) v.Clone();
            decision = _net.Output(vis.ToArray());
            var maxIndex = 0;
            double max = 0;
            for (var i = 0; i < decision.Length; i++)
            {
                if (!(decision[i] > max)) continue;
                max = decision[i];
                maxIndex = i;
            }

            pos = snake.Position;
            Direction = MoveSnake(maxIndex);
        }
    }
}