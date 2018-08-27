public struct IntPoint2D : ICellCoordinates
{
    public int x;
    public int y;
}

public class Toroidal2DBoard<StateType> : IBoard<StateType, IntPoint2D> where StateType : ICellState
{
    StateType[][] board;
    int width, height;

    private static readonly sbyte[] signs = { -1, 0, 1 };

    public Toroidal2DBoard(IntPoint2D dimensions)
    {
        width = dimensions.x;
        height = dimensions.y;

        board = new StateType[width][];
        for (int i = 0; i < board.Length; i++)
        {
            board[i] = new StateType[height];
        }
    }
    public StateType[] GetNeighboursStates(IntPoint2D coordinates)
    {
        StateType[] states = new StateType[8];
        sbyte index = 0;

        foreach (var i in signs)
        {
            foreach (var j in signs)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }
                else
                {
                    var newCoords = AdjustCoordinates(new IntPoint2D() { x = coordinates.x + i, y = coordinates.y + j });
                    states[index] = GetState(newCoords);
                    ++index;
                }
            }
        }
        return states;
    }

    public StateType GetState(IntPoint2D coordinates)
    {
        var newCoordinates = AdjustCoordinates(coordinates);

        return board[newCoordinates.x][newCoordinates.y];
    }

    public void SetState(IntPoint2D coordinates, StateType state)
    {
        board[coordinates.x][coordinates.y] = state;
    }

    private IntPoint2D AdjustCoordinates(IntPoint2D coords)
    {
        int x = ((coords.x % width) + width) % width;
        int y = ((coords.y % height) + height) % height;

        return new IntPoint2D() { x = x, y = y };
    }

    public void Copy(IBoard<StateType, IntPoint2D> source, IntPoint2D from, IntPoint2D to)
    {
        for (int i = from.x; i < to.x; ++i)
        {
            for (int j = from.y; j < to.y; j++)
            {
                var coords = new IntPoint2D() { x = i, y = j };

                SetState(coords, source.GetState(coords));
            }
        }
    }
}
