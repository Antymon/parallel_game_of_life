using UnityEngine;

public interface IGameOfLife
{
    void Init(int width, int height, IntPoint2D[] initialState);
    Color32[] GetColors();
}
public abstract class GameOfLife<StateType, RuleType> : IGameOfLife where StateType : ICellState, new() where RuleType : IRule<StateType>, new()
{
    protected int _width;
    protected int _height;

    protected Toroidal2DBoard<StateType> _board; // , stagingBoard;
    CellularAutomaton<StateType, IntPoint2D> _cellularAutomaton;

    protected void ProcessBoard(IntPoint2D from, IntPoint2D to, Toroidal2DBoard<StateType> board)
    {
        for (int i = from.x; i < to.x; ++i)
        {
            for (int j = from.y; j < to.y; ++j)
            {
                var coords = new IntPoint2D() { x = i, y = j };
                var nextState = _cellularAutomaton.NextState(coords);
                board.SetState(coords, nextState);
            }
        }
    }
    public virtual void Init(int width, int height, IntPoint2D[] initialState)
    {
        _width = width;
        _height = height;

        _board = new Toroidal2DBoard<StateType>(new IntPoint2D() { x = width, y = height });
        _cellularAutomaton = new CellularAutomaton<StateType, IntPoint2D>(_board, new RuleType());

        foreach (var point in initialState)
        {
            var newState = new StateType();
            newState.SetActive();
            _board.SetState(point, newState);
        }
    }

    protected abstract Color32 GetColorForState(StateType state, int x, int y);

    public virtual Color32[] GetColors()
    {
        Color32[] colors = new Color32[_width * _height];

        for (int i = 0; i < _width; ++i)
        {
            for (int j = 0; j < _height; ++j)
            {
                var state = _board.GetState(new IntPoint2D() { x = i, y = j });

                colors[i * _height + j] = GetColorForState(state, i, j);
            }
        }

        return colors;
    }
}

public class AsyncGameOfLife : GameOfLife<AsyncGameOfLifeState, AsyncGameOfLifeRule>
{
    private Color32[] _backgroundColors;

    public override void Init(int width, int height, IntPoint2D[] initialState)
    {
        base.Init(width, height, initialState);
        _backgroundColors = new Color32[1];
        _backgroundColors[0] = Color.white;
    }

    public void SetNumberOfBackgroundColors(byte num)
    {
        _backgroundColors = new Color32[num];

        for (int i = 0; i < num; ++i)
        {
            _backgroundColors[i] = Color32.Lerp(new Color(Random.value, Random.value, Random.value),Color.white , 0.75f);
        }
    }

    protected override Color32 GetColorForState(AsyncGameOfLifeState state, int x, int y)
    {
        if (state.previousAlive)
        {
            if (state.alive)
            {
                return Color.blue;
            }
            else
            {
                return Color.yellow;
            }
        }
        else
        {
            if (state.alive)
            {
                return Color.black;
            }
            else
            {
                Color32 backGroundColor = _backgroundColors[(int)(y / (float)_height * _backgroundColors.Length)];
                return backGroundColor;
            }
        }
    }

    public void ProcessBoard(IntPoint2D from, IntPoint2D to)
    {
        ProcessBoard(from, to, _board);
    }
}

public class SyncGameOfLife : GameOfLife<SyncGameOfLifeState, SyncGameOfLifeRule>
{
    private Toroidal2DBoard<SyncGameOfLifeState> _stagingBoard;

    public override void Init(int width, int height, IntPoint2D[] initialState)
    {
        base.Init(width, height, initialState);
        _stagingBoard = new Toroidal2DBoard<SyncGameOfLifeState>(new IntPoint2D() { x = width, y = height });
    }
    public void ProcessBoard()
    {
        IntPoint2D from = new IntPoint2D();
        IntPoint2D to = new IntPoint2D() { x = _width, y = _height };

        ProcessBoard(from, to, _stagingBoard);
        _board.Copy(_stagingBoard, from, to);
    }

    protected override Color32 GetColorForState(SyncGameOfLifeState state, int x, int y)
    {
        return state.alive ? Color.black : Color.white;
    }
}