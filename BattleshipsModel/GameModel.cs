using System.ComponentModel;
using System.Drawing;
using System.Net.Sockets;
using System.Text;

namespace BattleshipsModel;

/// <summary>
/// Model of the game
/// </summary>
public class GameModel
{
    /// <summary>
    /// all possible states of a cell that can occur in any of the grids
    /// </summary>
    public enum CellStatus
    {
        Empty,
        EmptyMiss,
        Alive,
        Sunk,
        Unknown
    }
    /// <summary>
    /// Which way the ship is pointing at
    /// </summary>
    public enum Orientation
    {
        Up,
        Down,
        Left,
        Right
    }
    /// <summary>
    /// Possible ship types
    /// </summary>
    public enum Types
    {
        None,
        Destroyer,
        Submarine,
        Cruiser,
        Battleship,
        Carrier
    }
    /// <summary>
    /// States of the game
    /// </summary>
    public enum GameState
    {
        Preparation,
        Playing,
        Win,
        Loss
    }
    /// <summary>
    /// Connection to the Opponent
    /// </summary>
    public Socket Connection;
    /// <summary>
    /// Grid for the player
    /// </summary>
    public List<List<CellStatus>> PlayerGrid = new List<List<CellStatus>>();
    /// <summary>
    /// Grid for the opponent, shows only what is known to the player
    /// </summary>
    public List<List<CellStatus>> OpponentGrid = new List<List<CellStatus>>();
    /// <summary>
    /// Ships that the player has, used for parameters
    /// </summary>
    public List<(Point, Orientation, Types)> PlayerShips = new List<(Point, Orientation, Types)>();
    /// <summary>
    /// Ships that the player has sunk
    /// </summary>
    public List<(Point, Orientation, Types)> OpponentShips = new List<(Point, Orientation, Types)>();
    /// <summary>
    /// Loop that waits for opponents moves, runs outside the main thread
    /// </summary>
    private Task _awaiter;
    /// <summary>
    /// Current state of the game
    /// </summary>
    public GameState State = GameState.Preparation;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameModel"/> class.
    /// Player grid is filled with empty cells
    /// Opponent grid is filled with unknown cells
    /// Start the awaiter
    /// </summary>
    /// <param name="connection">Connection to the opponent</param>
    public GameModel(Socket connection)
    {
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

    /// <summary>
    /// Places a ship on the player's grid
    /// Fills an entry in the PlayerShips list
    /// Sets the appropriate cells in the PlayerGrid to alive state
    /// </summary>
    /// <param name="coordinate">Upper left corner of the ship</param>
    /// <param name="orientation">Orientation of the ship</param>
    /// <param name="type">Which type of ship to place</param>
    /// <exception cref="ArgumentOutOfRangeException">Attempted to place a ship that does not exist</exception>
    /// <exception cref="ArgumentOutOfRangeException">Attempted to place a ship in unavailable orientation</exception>
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
    /// <summary>
    /// Attempts to remove a ship from the player's grid part of which is at coordinates (xCoord, yCoord)
    /// </summary>
    /// <param name="xCoord">X coordinate to look for a ship</param>
    /// <param name="yCoord">Y coordinate to look for a ship</param>
    /// <returns>Type of the ship removed or none if no ship is found at specified coordinates</returns>
    /// <exception cref="ArgumentOutOfRangeException">Found a ship that can't exist</exception>
    public Types RemoveShip(int xCoord, int yCoord)
    {
        Types type = Types.None;
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
                return type;
            }
        }

        return type;
    }
    /// <summary>
    /// Attempts to send an attack on the opponent, will not fire if it is not player's turn or the opponent is not ready
    /// </summary>
    /// <param name="xCoord">X coordinate to fire at</param>
    /// <param name="yCoord">Y coordinate to fire at</param>
    public void AttemptAttack(int xCoord, int yCoord)
    {
        if (CurrentMove % 2 != 0 || !EnemyReady) return;
        LastAction = $"sink {xCoord}{yCoord}";
        Connection.SendAsync(Encoding.Default.GetBytes(LastAction), SocketFlags.None);
        CurrentMove += 1;
    }
    /// <summary>
    /// Last successful action 
    /// </summary>
    private string? LastAction = null;
    /// <summary>
    /// Made an attack that was successfully received, on handling it must noted that it may go from outside of the main thread
    /// </summary>
    public event Action<int, int>? OnAttack;
    /// <summary>
    /// Opponent sent a successfully received attack, on handling it must noted that it may go from outside of the main thread
    /// </summary>
    public event Action<int, int, Types>? OnReceive;
    /// <summary>
    /// Opponent has placed his ships, on handling it must noted that it may go from outside of the main thread
    /// </summary>
    public event Action<bool>? OnOpponentReady;
    /// <summary>
    /// Opponent lost a ship, on handling it must noted that it may go from outside of the main thread
    /// </summary>
    public event Action<Point, Orientation, Types>? OnOpponentShipSunk;

    /// <summary>
    /// Changes the state of the game and sends a message to the opponent stating that the player is ready
    /// </summary>
    public void StartGame()
    {
        State = GameState.Playing;
        CurrentMove ??= (Random.Shared.NextDouble() < 0.5 ? 0 : 1);
        Connection.SendAsync(Encoding.Default.GetBytes($"ready {CurrentMove}"), SocketFlags.None);
    }
    /// <summary>
    /// Counter for the number of moves, move can be made only when the counter is an even number
    /// </summary>
    public int? CurrentMove { get; private set; }
    /// <summary>
    /// How many ships the player has lost and revealed to the opponent
    /// </summary>
    public int PlayerRevealedShips { get; private set; } = 0;
    /// <summary>
    /// Is the opponent ready
    /// </summary>
    public bool EnemyReady { get; private set; } = false;
    /// <summary>
    /// Loop receiving messages from the opponent, and sends responses when required, is running in the separate thread
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Received a message that contradicts the rules</exception>
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
                // Opponent is ready
                CurrentMove ??= int.Parse(message.Split(" ")[1]) == 1 ? 0 : 1;
                EnemyReady = true;
                OnOpponentReady?.Invoke(CurrentMove == 0);
            }
            else if (message.Length >= 7 && message.Substring(0, 7) == "retract")
            {
                // Opponent has fired on the cell that was already attacked before
                CurrentMove -= 1;
            }
            else if (message.Length >= 4 && message.Substring(0, 4) == "sink")
            {
                // Opponent has fired on a specific cell
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
                // Player has hit a ship
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.Sunk;
                OnAttack?.Invoke(xCoord, yCoord);
            }
            else if (message.Length >= 4 && message.Substring(0, 4) == "miss")
            {
                // Player has missed
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.EmptyMiss;
                OnAttack?.Invoke(xCoord, yCoord);
            }
            else if (message.Length >= 6 && message.Substring(0, 6) == "reveal")
            {
                // Player has sunk a ship
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