using System.ComponentModel;
using System.Drawing;
using System.Net.Sockets;
using System.Text;

namespace BattleshipsModel;

public class GameModel
{
    public enum Roles
    {
        Server,
        Client
    }

    public enum CellStatus
    {
        Empty,
        EmptyMiss,
        Alive,
        Sunk,
        Unknown
    }

    public enum Orientation
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum Types
    {
        None,
        Destroyer,
        Submarine,
        Cruiser,
        Battleship,
        Carrier
    }

    public enum GameState
    {
        Preparation,
        Playing,
        Win,
        Loss
    }

    public Roles Role;
    public Socket Connection;
    public List<List<CellStatus>> PlayerGrid = new List<List<CellStatus>>();
    public List<List<CellStatus>> OpponentGrid = new List<List<CellStatus>>();
    public List<(Point, Orientation, Types)> PlayerShips = new List<(Point, Orientation, Types)>();
    public List<(Point, Orientation, Types)> OpponentShips = new List<(Point, Orientation, Types)>();
    private Task _awaiter;
    public GameState State = GameState.Preparation;

    public GameModel(Roles role, Socket connection)
    {
        Role = role;
        Connection = connection;
        const int fieldSide = 10;
        for (int i = 0; i < fieldSide; i++)
        {
            PlayerGrid.Add(new List<CellStatus>());
            OpponentGrid.Add(new List<CellStatus>());
            for (int j = 0; j < fieldSide; j++)
            {
                PlayerGrid[i].Add(CellStatus.Empty);
                OpponentGrid[i].Add(CellStatus.Unknown);
            }
        }

        Task.Run(MessageAwaiter);
    }

    public void AddShip(Point coordinate, Orientation orientation, Types type)
    {
        PlayerShips.Add((coordinate, orientation, type));
        int shipSize;
        switch (type)
        {
            case Types.Destroyer:
                shipSize = 2;
                break;
            case Types.Submarine or Types.Cruiser:
                shipSize = 3;
                break;
            case Types.Battleship:
                shipSize = 4;
                break;
            case Types.Carrier:
                shipSize = 5;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        switch (orientation)
        {
            case Orientation.Up or Orientation.Down:
            {
                for (int i = coordinate.Y; i < coordinate.Y + shipSize; i++)
                {
                    PlayerGrid[coordinate.X][i] = CellStatus.Alive;
                }

                break;
            }
            case Orientation.Left or Orientation.Right:
            {
                for (int i = coordinate.X; i < coordinate.X + shipSize; i++)
                {
                    PlayerGrid[i][coordinate.Y] = CellStatus.Alive;
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
        }
    }

    public Types RemoveShip(int xCoord, int yCoord)
    {
        Types? type = null;
        foreach (var ship in PlayerShips)
        {
            int shipSize;
            switch (ship.Item3)
            {
                case Types.Destroyer:
                    shipSize = 2;
                    break;
                case Types.Submarine or Types.Cruiser:
                    shipSize = 3;
                    break;
                case Types.Battleship:
                    shipSize = 4;

                    break;
                case Types.Carrier:
                    shipSize = 5;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ship.Item3), ship.Item3, null);
            }

            bool found = false;
            switch (ship.Item2)
            {
                case Orientation.Up or Orientation.Down:
                {
                    for (int i = ship.Item1.Y; i < ship.Item1.Y + shipSize; i++)
                    {
                        if (xCoord == ship.Item1.X && yCoord == i)
                        {
                            found = true;
                            break;
                        }
                    }

                    break;
                }
                case Orientation.Left or Orientation.Right:
                {
                    for (int i = ship.Item1.X; i < ship.Item1.X + shipSize; i++)
                    {
                        if (xCoord == i && yCoord == ship.Item1.Y)
                        {
                            found = true;
                            break;
                        }
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(ship.Item2), ship.Item2, null);
            }

            if (found)
            {
                type = ship.Item3;
                switch (ship.Item2)
                {
                    case Orientation.Up or Orientation.Down:
                    {
                        for (int i = ship.Item1.Y; i < ship.Item1.Y + shipSize; i++)
                        {
                            PlayerGrid[ship.Item1.X][i] = CellStatus.Empty;
                        }

                        break;
                    }
                    case Orientation.Left or Orientation.Right:
                    {
                        for (int i = ship.Item1.X; i < ship.Item1.X + shipSize; i++)
                        {
                            PlayerGrid[i][ship.Item1.Y] = CellStatus.Empty;
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ship.Item2), ship.Item2, null);
                }
            }
        }

        return type.Value;
    }

    public void AttemptAttack(int xCoord, int yCoord)
    {
        if (CurrentMove % 2 != 0 || !EnemyReady) return;
        LastAction = $"sink {xCoord}{yCoord}";
        Connection.SendAsync(Encoding.Default.GetBytes(LastAction), SocketFlags.None);
        CurrentMove += 1;
    }

    private string? LastAction = null;
    public event Action<int, int>? OnAttack;
    public event Action<int, int, Types>? OnReceive;
    public event Action<bool>? OnOpponentReady;
    public event Action<Point, Orientation, Types>? OnOpponentShipSunk;

    public void StartGame()
    {
        State = GameState.Playing;
        CurrentMove ??= (Random.Shared.NextDouble() < 0.5 ? 0 : 1);
        Connection.SendAsync(Encoding.Default.GetBytes($"ready {CurrentMove}"), SocketFlags.None);
    }

    public int? CurrentMove { get; private set; }
    public int PlayerRevealedShips { get; private set; } = 0;
    public bool EnemyReady { get; private set; } = false;

    private void MessageAwaiter()
    {
        while (true)
        {
            byte[] buffer = new byte[1024];
            int receivedBytes = Connection.Receive(buffer, SocketFlags.None);
            if (receivedBytes <= 0)
            {
                return;
            }

            string message = Encoding.Default.GetString(buffer, 0, receivedBytes);
            if (message.Length >= 5 && message.Substring(0, 5) == "ready")
            {
                CurrentMove ??= int.Parse(message.Split(" ")[1]) == 1 ? 0 : 1;
                EnemyReady = true;
                OnOpponentReady?.Invoke(CurrentMove == 0);
            }
            else if (message.Length >= 7 && message.Substring(0, 7) == "retract")
            {
                CurrentMove -= 1;
            }
            else if (message.Length >= 4 && message.Substring(0, 4) == "sink")
            {
                string coordinate = message.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                switch (PlayerGrid[xCoord][yCoord])
                {
                    case CellStatus.Sunk or CellStatus.EmptyMiss or CellStatus.Unknown:
                        Connection.SendAsync(Encoding.Default.GetBytes("retract"), SocketFlags.None);
                        break;
                    case CellStatus.Alive:
                        PlayerGrid[xCoord][yCoord] = CellStatus.Sunk;
                        foreach (var ship in PlayerShips)
                        {
                            int shipSize;
                            switch (ship.Item3)
                            {
                                case Types.Destroyer:
                                    shipSize = 2;
                                    break;
                                case Types.Submarine or Types.Cruiser:
                                    shipSize = 3;
                                    break;
                                case Types.Battleship:
                                    shipSize = 4;

                                    break;
                                case Types.Carrier:
                                    shipSize = 5;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(ship.Item3), ship.Item3, null);
                            }

                            bool found = false;
                            switch (ship.Item2)
                            {
                                case Orientation.Up or Orientation.Down:
                                {
                                    for (int i = ship.Item1.Y; i < ship.Item1.Y + shipSize; i++)
                                    {
                                        if (xCoord == ship.Item1.X && yCoord == i)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }

                                    break;
                                }
                                case Orientation.Left or Orientation.Right:
                                {
                                    for (int i = ship.Item1.X; i < ship.Item1.X + shipSize; i++)
                                    {
                                        if (xCoord == i && yCoord == ship.Item1.Y)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }

                                    break;
                                }
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(ship.Item2), ship.Item2, null);
                            }

                            if (found)
                            {
                                int sunkParts = 0;
                                switch (ship.Item2)
                                {
                                    case Orientation.Up or Orientation.Down:
                                    {
                                        for (int i = ship.Item1.Y; i < ship.Item1.Y + shipSize; i++)
                                        {
                                            if (PlayerGrid[ship.Item1.X][i] == CellStatus.Sunk)
                                            {
                                                sunkParts += 1;
                                            }
                                        }

                                        break;
                                    }
                                    case Orientation.Left or Orientation.Right:
                                    {
                                        for (int i = ship.Item1.X; i < ship.Item1.X + shipSize; i++)
                                        {
                                            if (PlayerGrid[i][ship.Item1.Y] == CellStatus.Sunk)
                                            {
                                                sunkParts += 1;
                                            }
                                        }

                                        break;
                                    }
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(ship.Item2), ship.Item2, null);
                                }

                                if (sunkParts == shipSize)
                                {
                                    PlayerRevealedShips += 1;
                                    Connection.SendAsync(Encoding.Default.GetBytes($"reveal {ship.Item1.X}{ship.Item1.Y} {ship.Item2} {ship.Item3}"), SocketFlags.None);
                                    OnReceive?.Invoke(xCoord, yCoord, ship.Item3);
                                }
                                else
                                {
                                    Connection.SendAsync(Encoding.Default.GetBytes("hit"), SocketFlags.None);
                                    OnReceive?.Invoke(xCoord, yCoord, ship.Item3);
                                }
                            }
                        }
                        break;
                    case CellStatus.Empty:
                        PlayerGrid[xCoord][yCoord] = CellStatus.EmptyMiss;
                        Connection.SendAsync(Encoding.Default.GetBytes("miss"), SocketFlags.None);
                        OnReceive?.Invoke(xCoord, yCoord, Types.None);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CurrentMove += 1;
                if (PlayerRevealedShips == 5)
                {
                    State = GameState.Loss;
                    return;
                }
            }
            else if (message.Length >= 3 && message.Substring(0, 3) == "hit")
            {
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.Sunk;
                OnAttack?.Invoke(xCoord, yCoord);
            }
            else if (message.Length >= 4 && message.Substring(0, 4) == "miss")
            {
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.EmptyMiss;
                OnAttack?.Invoke(xCoord, yCoord);
            }
            else if (message.Length >= 6 && message.Substring(0, 6) == "reveal")
            {
                string coordinate = message.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                Orientation orient = message.Split(" ")[2] switch
                {
                    "Up" => Orientation.Up,
                    "Down" => Orientation.Down,
                    "Left" => Orientation.Left,
                    "Right" => Orientation.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };
                Types shipType = message.Split(" ")[3] switch
                {
                    "Destroyer" => Types.Destroyer,
                    "Submarine" => Types.Submarine,
                    "Cruiser" => Types.Cruiser,
                    "Battleship" => Types.Battleship,
                    "Carrier" => Types.Carrier,
                    _ => throw new ArgumentOutOfRangeException()
                };
                OpponentShips.Add((new Point(xCoord, yCoord), orient, shipType));
                OnOpponentShipSunk?.Invoke(new Point(xCoord, yCoord), orient, shipType);
                if (OpponentShips.Count == 5)
                {
                    State = GameState.Win;
                    return;
                }
            }
        }
    }
}