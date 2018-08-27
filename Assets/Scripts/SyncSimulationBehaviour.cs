using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulationBehaviour<GameOfLifeType> : MonoBehaviour where GameOfLifeType : IGameOfLife, new()
{
    [SerializeField]
    protected int width = 64;
    [SerializeField]
    protected int height = 64;

    [SerializeField]
    protected Vector2Int[] initialPattern;

    private Coroutine _renderingCoroutine;
    
    protected GameOfLifeType _gameOfLife;
    private Texture2D _texture;
    private SpriteRenderer _renderer;

    protected virtual void Start()
    {
        _gameOfLife = new GameOfLifeType();

        var translatedPoints = new IntPoint2D[initialPattern.Length];
        for (int i = 0; i < initialPattern.Length; i++)
        {
            translatedPoints[i] = new IntPoint2D() { x = initialPattern[i].x, y = initialPattern[i].y };
        }

        _gameOfLife.Init(width, height, translatedPoints);

        _texture = new Texture2D(width, height);

        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = Sprite.Create(_texture, new Rect(0, 0, width, height), Vector2.zero);

        _renderingCoroutine = StartCoroutine(CoroutineRender());
    }

    protected IEnumerator CoroutineRender()
    {
        while (true)
        {
            var colors = _gameOfLife.GetColors();
            _texture.SetPixels32(colors);
            _texture.Apply(false);

            yield return new WaitForSeconds(0.1f);
        }
    }
}

public class SyncSimulationBehaviour : SimulationBehaviour<SyncGameOfLife>
{
    Coroutine _syncSimulationCoroutine;

    protected override void Start()
    {
        base.Start();
        _syncSimulationCoroutine = StartCoroutine(Simulate());
    }

    IEnumerator Simulate()
    {
        while (true)
        {
            _gameOfLife.ProcessBoard();
            yield return new WaitForSeconds(1f);
        }
    }
}

public interface ICellState
{
    void SetActive();
}

public interface ICellCoordinates
{

}

public interface IBoard<StateType, CoordinatesType> where StateType : ICellState where CoordinatesType : ICellCoordinates
{
    StateType GetState(CoordinatesType coordinates);
    StateType[] GetNeighboursStates(CoordinatesType coordinates);
    void SetState(CoordinatesType coordinates, StateType state);
    void Copy(IBoard<StateType, CoordinatesType> source, CoordinatesType from, CoordinatesType to);
}


public interface IRule<StateType> where StateType : ICellState
{
    StateType Apply(StateType currentState, StateType[] neigboursStates);
}

public interface ICellularAutomaton<StateType, CoordinatesType> where StateType : ICellState where CoordinatesType : ICellCoordinates
{
    IBoard<StateType, CoordinatesType> Board { get; }
    IRule<StateType> Rule { get; }
    StateType NextState(CoordinatesType coordinates);
}

public class CellularAutomaton<StateType, CoordinatesType> : ICellularAutomaton<StateType, CoordinatesType> where StateType : ICellState where CoordinatesType : ICellCoordinates
{
    public CellularAutomaton(IBoard<StateType, CoordinatesType> board, IRule<StateType> rule)
    {
        Board = board;
        Rule = rule;
    }

    public IBoard<StateType, CoordinatesType> Board { get; private set; }
    public IRule<StateType> Rule { get; private set; }

    public StateType NextState(CoordinatesType coordinates)
    {
        var currentState = Board.GetState(coordinates);
        var neigbourStates = Board.GetNeighboursStates(coordinates);
        return Rule.Apply(currentState, neigbourStates);
    }
}

public struct SyncGameOfLifeState : ICellState
{
    public bool alive;

    public SyncGameOfLifeState(bool alive)
    {
        this.alive = alive;
    }

    public void SetActive()
    {
        alive = true;
    }
}

public struct AsyncGameOfLifeState : ICellState
{
    //tense -> (Tense)((((int)tense) + 2) % 3)
    public static readonly Dictionary<Tense, Tense> NOT_READY_MAP = new Dictionary<Tense, Tense> {
        {Tense.Present, Tense.Past},
        {Tense.Future, Tense.Present},
        {Tense.Past, Tense.Future }
    };

     public enum Tense : byte{
        Present = 0,
        Future = 1,
        Past = 2,
     }

    public bool alive;
    public bool previousAlive;
    public Tense tense;

    public void SetActive()
    {
        alive = true;
    }
}

public struct IntPoint2D : ICellCoordinates
{
    public int x;
    public int y;
}

public class Toroidal2DBoard<StateType> : IBoard<StateType, IntPoint2D> where StateType : ICellState
{
    StateType[][] board;
    int width, height;

    private static readonly sbyte[] signs = {-1,0,1};

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
                if(i==0 && j == 0)
                {
                    continue;
                }
                else
                {
                    var newCoords = AdjustCoordinates(new IntPoint2D() {x = coordinates.x + i,y = coordinates.y + j });
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
        int x = ((coords.x % width) + width)%width;
        int y = ((coords.y % height) + height) % height;

        return new IntPoint2D() { x = x, y = y};
    }

    public void Copy(IBoard<StateType, IntPoint2D> source, IntPoint2D from, IntPoint2D to)
    {
        for (int i = from.x; i < to.x; ++i)
        {
            for (int j = from.y; j < to.y; j++)
            {
                var coords = new IntPoint2D() { x = i, y = j };

                SetState(coords,source.GetState(coords));
            }
        }
    }
}

public class SyncGameOfLifeRule : IRule<SyncGameOfLifeState>
{
    public SyncGameOfLifeState Apply(SyncGameOfLifeState currentState, SyncGameOfLifeState[] neigboursStates)
    {
        byte neighboursAlive = 0;

        foreach (var state in neigboursStates)
        {
            if (state.alive)
            {
                neighboursAlive++;
            }
        }
        
        //rules:
        //if alive:
        //  die from loneliness if having 0 or 1 neighbour
        //  die from owercrowdness if having 4 or more neighbours
        //  live otherwise
        //if dead:
        //  become alive if having 3 neighbours (by reproduction)
        //..stay dead otherwise

        return new SyncGameOfLifeState(currentState.alive && neighboursAlive >= 2 && neighboursAlive <= 3 || !currentState.alive && neighboursAlive == 3);
    }
}

public class AsyncGameOfLifeRule : SyncGameOfLifeRule, IRule<AsyncGameOfLifeState>
{
    public AsyncGameOfLifeState Apply(AsyncGameOfLifeState currentState, AsyncGameOfLifeState[] neigboursStates)
    {
        byte neighboursAlive = 0;

        foreach (var state in neigboursStates)
        {
            if (state.alive)
            {
                neighboursAlive++;
            }
        }

        if (IsReady(currentState.tense, neigboursStates))
        {
            if (currentState.tense == AsyncGameOfLifeState.Tense.Present)
            {
                var syncGameOfLifeStates = ToSyncNeighbours(neigboursStates);
                var syncGameOfLifeState = new SyncGameOfLifeState() { alive = currentState.alive };
                var newSyncState = base.Apply(syncGameOfLifeState, syncGameOfLifeStates);
                var newAsyncState = new AsyncGameOfLifeState() { alive = newSyncState.alive, previousAlive = currentState.alive, tense = AsyncGameOfLifeState.Tense.Future };
                return newAsyncState;
            }
            else
            {
                var nextTense = (AsyncGameOfLifeState.Tense)((((int)currentState.tense) + 1)%3);
                return new AsyncGameOfLifeState() { alive = currentState.alive, previousAlive = currentState.previousAlive, tense = nextTense };
            }
        }
        else
        {
            return currentState;
        }
    }

    SyncGameOfLifeState[] ToSyncNeighbours(AsyncGameOfLifeState[] neigboursStates)
    {
        SyncGameOfLifeState[] syncGameOfLifeStates = new SyncGameOfLifeState[neigboursStates.Length];

        for (int i = 0; i < neigboursStates.Length; i++)
        {
            if(neigboursStates[i].tense == AsyncGameOfLifeState.Tense.Present)
            {
                syncGameOfLifeStates[i] = new SyncGameOfLifeState() { alive = neigboursStates[i].alive };
            }
            else if (neigboursStates[i].tense == AsyncGameOfLifeState.Tense.Future)
            {
                syncGameOfLifeStates[i] = new SyncGameOfLifeState() { alive = neigboursStates[i].previousAlive };
            }
        }

        return syncGameOfLifeStates;
    }


    bool IsReady(AsyncGameOfLifeState.Tense tense, AsyncGameOfLifeState[] neigboursStates)
    {
        AsyncGameOfLifeState.Tense forbiddenTense = AsyncGameOfLifeState.NOT_READY_MAP[tense];

        foreach (var state in neigboursStates)
        {
            if(state.tense == forbiddenTense)
            {
                return false;
            }
        }

        return true;
    }
}

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
            for (int j = from.y; j < to.y; j++)
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

    public abstract Color32 GetColorForState(StateType state);

    public Color32[] GetColors()
    {
        Color32[] colors = new Color32[_width * _height];

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                var state = _board.GetState(new IntPoint2D() { x = i, y = j });

                colors[i * _height + j] = GetColorForState(state);
            }
        }

        return colors;
    }
}

public class AsyncGameOfLife : GameOfLife<AsyncGameOfLifeState, AsyncGameOfLifeRule>
{
    public void ProcessBoard()
    {
        throw new NotImplementedException();
    }

    public override Color32 GetColorForState(AsyncGameOfLifeState state)
    {
        return state.previousAlive? state.alive? Color.blue: Color.yellow : state.alive? Color.black: Color.white;
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

    public override Color32 GetColorForState(SyncGameOfLifeState state)
    {
        return state.alive? Color.black: Color.white;
    }
}

