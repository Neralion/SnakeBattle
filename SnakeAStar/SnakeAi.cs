using System;
using System.Collections.Generic;
using System.Drawing;
using PluginInterface;
using System.Collections.ObjectModel;
using System.Linq;
using static System.Math;

namespace SnakeAStar
{
    public class SnakeAi : ISmartSnake
    {
        public Move Direction { get; set; }
        public bool Reverse { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }

        private static readonly Random Rnd = new Random();
        private int _randInt;
        private int _predict;

        private byte[,] _field, _frameField;

        private bool _moveToFood;
        private Point _posFood;
        private List<Point> _pathToFood, _lsDead;
        
        private byte[,] FieldUpdate(ref byte[,] frameField, ref Snake snake, ref List<Point> food,
            ref List<Snake> enemies)
        {
            foreach (var tail in snake.Tail)
            {
                _frameField[tail.X, tail.Y] = 2;
            }

            foreach (var f in food)
            {
                _frameField[f.X, f.Y] = 1;
            }


            foreach (var enemy in enemies)
            {
                foreach (var enemyTail in enemy.Tail)
                {
                    _frameField[enemyTail.X, enemyTail.Y] = 2;
                }
            }

            return frameField;
        }

        private Point FindFood(ref List<Point> food, Point snake)
        {
            int minFood = 100000;
            int index = 0;
            for (int i = 0; i < food.Count; i++)
            {
                if ((Abs(snake.X - food[i].X) + Abs(snake.Y - food[i].Y)) < minFood)
                {
                    minFood = Abs(snake.X - food[i].X) + Abs(snake.Y - food[i].Y);
                    index = i;
                }
            }

            return food[index];
        }

        private int MovingSnake(Point point, Point snake)
        {
            if (snake.X == point.X + 1 && point.Y == snake.Y)
                return 4;
            if (snake.X == point.X - 1 && point.Y == snake.Y)
                return 2;
            if (snake.X == point.X && snake.Y == point.Y + 1)
                return 1;
            if (snake.X == point.X && snake.Y == point.Y - 1)
                return 3;
            return 0;
        }

        public void Startup(Size size, List<Point> stones)
        {
            Name = "Snake A";
            Color = Color.Blue;
            _field = new byte[size.Width, size.Height];
            for (int i = 0; i < size.Width; i++)
            {
                for (int j = 0; j < size.Height; j++)
                {
                    _field[i, j] = 0;
                }
            }

            foreach (var stone in stones)
            {
                _field[stone.X, stone.Y] = 2;
            }

            _frameField = (byte[,]) _field.Clone();
        }

        public void Update(Snake snake, List<Snake> enemies, List<Point> food, List<Point> dead)
        {
            if (_lsDead != dead)
            {
                foreach (var posDead in dead)
                {
                    _field[posDead.X, posDead.Y] = 2;
                }

                _lsDead = dead;
            }

            _frameField = FieldUpdate(ref _frameField, ref snake, ref food, ref enemies);
            if (snake.Position == _posFood)
            {
                _moveToFood = false;
            }

            if (snake.Position != FindFood(ref food, snake.Position) && _moveToFood == false)
            {
                _posFood = FindFood(ref food, snake.Position);
                _moveToFood = true;
            }

            try
            {
                _pathToFood = PathNode.FindPath(ref _frameField, snake.Position, _posFood);
                GC.Collect();
                _predict = MovingSnake(_pathToFood[1], snake.Position);
                Direction = (Move) _predict;
            }
            catch (NullReferenceException)
            {
                if (_randInt % 2 == 0)
                {
                    Direction = (Move) Rnd.Next(1, 5);
                    _randInt++;
                }
                else
                {
                    Reverse = true;
                    _randInt++;
                }
            }

            _moveToFood = false;
            _frameField = (byte[,]) _field.Clone();
        }

        private class PathNode
        {
            private Point Position { get; set; }

            private int PathLengthFromStart { get; set; }

            private PathNode CameFrom { get; set; }

            private int HeuristicEstimatePathLength { get; set; }

            private int EstimateFullPathLength
            {
                get { return PathLengthFromStart + HeuristicEstimatePathLength; }
            }

            public static List<Point> FindPath(ref byte[,] field, Point start, Point goal)
            {
                var closedSet = new Collection<PathNode>();
                var openSet = new Collection<PathNode>();
                var startNode = new PathNode()
                {
                    Position = start,
                    CameFrom = null,
                    PathLengthFromStart = 0,
                    HeuristicEstimatePathLength = GetHeuristicPathLength(start, goal)
                };
                openSet.Add(startNode);
                while (openSet.Count > 0)
                {
                    var currentNode = openSet.OrderBy(node =>
                        node.EstimateFullPathLength).First();
                    if (currentNode.Position == goal)
                        return GetPathForNode(currentNode);
                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode);
                    foreach (var neighbourNode in GetNeighbours(currentNode, goal, ref field))
                    {
                        if (closedSet.Count(node => node.Position == neighbourNode.Position) > 0)
                            continue;
                        var openNode = openSet.FirstOrDefault(node =>
                            node.Position == neighbourNode.Position);
                        if (openNode == null)
                            openSet.Add(neighbourNode);
                        else if (openNode.PathLengthFromStart > neighbourNode.PathLengthFromStart)
                        {
                            openNode.CameFrom = currentNode;
                            openNode.PathLengthFromStart = neighbourNode.PathLengthFromStart;
                        }
                    }
                }

                return null;
            }

            private static int GetDistanceBetweenNeighbours()
            {
                return 1;
            }

            private static int GetHeuristicPathLength(Point from, Point to)
            {
                return Abs(from.X - to.X) + Abs(from.Y - to.Y);
            }

            private static IEnumerable<PathNode> GetNeighbours(PathNode pathNode,
                Point goal, ref byte[,] field)
            {
                var result = new Collection<PathNode>();

                var neighbourPoints = new Point[4];
                neighbourPoints[0] = new Point(pathNode.Position.X + 1, pathNode.Position.Y);
                neighbourPoints[1] = new Point(pathNode.Position.X - 1, pathNode.Position.Y);
                neighbourPoints[2] = new Point(pathNode.Position.X, pathNode.Position.Y + 1);
                neighbourPoints[3] = new Point(pathNode.Position.X, pathNode.Position.Y - 1);

                foreach (var point in neighbourPoints)
                {
                    if (point.X < 0 || point.X >= field.GetLength(0))
                        continue;
                    if (point.Y < 0 || point.Y >= field.GetLength(1))
                        continue;
                    if ((field[point.X, point.Y] != 0) && (field[point.X, point.Y] != 1))
                        continue;
                    var neighbourNode = new PathNode()
                    {
                        Position = point,
                        CameFrom = pathNode,
                        PathLengthFromStart = pathNode.PathLengthFromStart +
                                              GetDistanceBetweenNeighbours(),
                        HeuristicEstimatePathLength = GetHeuristicPathLength(point, goal)
                    };
                    result.Add(neighbourNode);
                }

                return result;
            }

            private static List<Point> GetPathForNode(PathNode pathNode)
            {
                var result = new List<Point>();
                var currentNode = pathNode;
                while (currentNode != null)
                {
                    result.Add(currentNode.Position);
                    currentNode = currentNode.CameFrom;
                }

                result.Reverse();
                return result;
            }
        }
    }
}