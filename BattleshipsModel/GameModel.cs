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
        Up, Down, Left, Right
    }

    public enum Types
    {
        Destroyer, Submarine, Cruiser, Battleship, Carrier
    }
    
    public Roles Role;
    public Socket Connection;
    public List<List<CellStatus>> PlayerGrid = new List<List<CellStatus>>();
    public List<List<CellStatus>> OpponentGrid = new List<List<CellStatus>>();
    public List<(Point, Orientation, Types)> PlayerShips = new List<(Point, Orientation, Types)>();
    public List<(Point, Orientation, Types)> OpponentShips = new List<(Point, Orientation, Types)>();
    private Task _awaiter;

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

        _awaiter = Task.Run(MessageAwaiter);
    }

    public void AddShip(Point coordinate, Orientation orientation, Types type)
    {
        PlayerShips.Add((coordinate, orientation, type));
        int shipSize = 0;
        switch (type)
        {
            case Types.Destroyer:
                shipSize = 2;
                break;
            case Types.Submarine:
                shipSize = 3;
                break;
            case Types.Cruiser:
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

        if (orientation is Orientation.Up or Orientation.Down)
        {
            for (int i = coordinate.Y; i < coordinate.Y + shipSize; i++)
            {
                PlayerGrid[coordinate.X][i] = CellStatus.Alive;
            }
        }
        else
        {
            for (int i = coordinate.X; i < coordinate.X + shipSize; i++)
            {
                PlayerGrid[i][coordinate.Y] = CellStatus.Alive;
            }
        }
    }

    public void RemoveShip(int xCoord, int yCoord)
    {
        //TODO: Remake
        PlayerGrid[xCoord][yCoord] = CellStatus.Empty;
    }
    
    public void AttemptAttack(int xCoord, int yCoord)
    {
        LastAction = $"sink {xCoord}{yCoord}";
        Connection.SendAsync(Encoding.Default.GetBytes(LastAction), SocketFlags.None);
    }

    public void Reveal()
    {
        //TODO: Make
    }

    public string? LastAction = null;
    private void MessageAwaiter()
    {
        while (true)
        {
            byte[] buffer = new byte[1024];
            int receivedBytes = Connection.Receive(buffer, SocketFlags.None);
            string message = Encoding.Default.GetString(buffer, 0 , receivedBytes);
            if (message.Length >= 4 && message.Substring(0, 4) == "sink")
            {
                string coordinate = message.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                switch (PlayerGrid[xCoord][yCoord])
                {
                    case CellStatus.Alive:
                        PlayerGrid[xCoord][yCoord] = CellStatus.Sunk;
                        Connection.SendAsync(Encoding.Default.GetBytes("hit"), SocketFlags.None);
                        break;
                    case CellStatus.Empty:
                        PlayerGrid[xCoord][yCoord] = CellStatus.EmptyMiss;
                        Connection.SendAsync(Encoding.Default.GetBytes("miss"), SocketFlags.None);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (message.Length >= 3 && message.Substring(0, 3) == "hit")
            {
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.Sunk;
            }
            else if (message.Length >= 4 &&message.Substring(0, 4) == "miss")
            {
                string coordinate = LastAction.Split(" ")[1];
                int xCoord = coordinate[0] - '0';
                int yCoord = coordinate[1] - '0';
                OpponentGrid[xCoord][yCoord] = CellStatus.EmptyMiss;
            }
            else if (message.Length >= 6 && message.Substring(0, 6) == "reveal")
            {
                //TODO: Remake
                var coords = message.Split(" ")[1..];
                foreach (var coord in coords)
                {
                    string coordinate = coord;
                    int xCoord = coordinate[0] - '0';
                    int yCoord = coordinate[1] - '0';
                    if (OpponentGrid[xCoord][yCoord] == CellStatus.Unknown)
                    {
                        OpponentGrid[xCoord][yCoord] = CellStatus.Alive;
                    }
                }
                return;
            }
        }
    }
}