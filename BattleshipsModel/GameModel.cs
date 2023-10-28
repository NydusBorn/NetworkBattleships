using System.Net.Sockets;

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
        Alive,
        Sunk,
        Unknown
    }
    
    public Roles Role;
    public Socket Connection;
    public List<List<CellStatus>> PlayerGrid = new List<List<CellStatus>>();
    public List<List<CellStatus>> OpponentGrid = new List<List<CellStatus>>();

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
    }

    public void AddShip(int xCoord, int yCoord)
    {
        PlayerGrid[xCoord][yCoord] = CellStatus.Alive;
    }

    public void RemoveShip(int xCoord, int yCoord)
    {
        PlayerGrid[xCoord][yCoord] = CellStatus.Empty;
    }
    
    
}