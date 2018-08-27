using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;



public class Simulation : MonoBehaviour
{
    Coroutine _co;

    IGameOfLife syncGameOfLife;
    Texture2D texture;
    SpriteRenderer rend;

    int width = 8;
    int height = 8;

    void Start()
    {
        syncGameOfLife = new SyncGameOfLife(width, height);

        texture = new Texture2D(width, height);

        rend = GetComponent<SpriteRenderer>();
        rend.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero);

        _co = StartCoroutine(Simulate());
    }

    IEnumerator Simulate()
    {
        while (true)
        {
            syncGameOfLife.ProcessBoard();
            var colors = syncGameOfLife.GetColors();


            texture.SetPixels32(colors);

            // actually apply all SetPixels, don't recalculate mip levels
            texture.Apply(false);

            yield return new WaitForSeconds(1f);
        }
    }
}

public interface ICellState
{

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
    void ProcessBoard();
    void ProcessBoard(IntPoint2D from, IntPoint2D to);
    Color32[] GetColors();
}

public class AsyncGameOfLife : IGameOfLife
{
    Toroidal2DBoard<AsyncGameOfLifeState> board; // , stagingBoard;
    CellularAutomaton<AsyncGameOfLifeState, IntPoint2D> cellularAutomaton;

    int width, height;

    public AsyncGameOfLife(int width, int height)
    {
        this.width = width;
        this.height = height;

        board = new Toroidal2DBoard<AsyncGameOfLifeState>(new IntPoint2D() { x = width, y = height });
        //stagingBoard = new Toroidal2DBoard<AsyncGameOfLifeState>(new IntPoint2D() { x = width, y = height });
        cellularAutomaton = new CellularAutomaton<AsyncGameOfLifeState, IntPoint2D>(board, new AsyncGameOfLifeRule());

        board.SetState(new IntPoint2D() { x = 0, y = 0 }, new AsyncGameOfLifeState() { alive = true });
        board.SetState(new IntPoint2D() { x = 0, y = 1 }, new AsyncGameOfLifeState() { alive = true });
        board.SetState(new IntPoint2D() { x = 0, y = 2 }, new AsyncGameOfLifeState() { alive = true });
    }

    public void ProcessBoard(IntPoint2D from, IntPoint2D to)
    {
        for (int i = from.x; i < to.x; ++i)
        {
            for (int j = from.y; j < to.y; j++)
            {
                var coords = new IntPoint2D() { x = i, y = j };
                var nextState = cellularAutomaton.NextState(coords);
                board.SetState(coords, nextState);
            }
        }
    }

    public Color32[] GetColors()
    {
        Color32[] colors = new Color32[width * height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var state = board.GetState(new IntPoint2D() { x = i, y = j });

                colors[i * height + j] = state.previousAlive?state.alive?Color.blue:Color.yellow:state.alive?Color.black:Color.white;
            }
        }

        return colors;
    }

    public void ProcessBoard()
    {
        throw new NotImplementedException();
    }
}

public class SyncGameOfLife : IGameOfLife
{
    Toroidal2DBoard<SyncGameOfLifeState> board, stagingBoard;
    CellularAutomaton<SyncGameOfLifeState, IntPoint2D> cellularAutomaton;

    int width, height;

    public SyncGameOfLife(int width, int height)
    {
        this.width = width;
        this.height = height;

        board = new Toroidal2DBoard<SyncGameOfLifeState>(new IntPoint2D() { x = width, y = height });
        stagingBoard = new Toroidal2DBoard<SyncGameOfLifeState>(new IntPoint2D() { x = width, y = height });
        cellularAutomaton = new CellularAutomaton<SyncGameOfLifeState,IntPoint2D>(board, new SyncGameOfLifeRule());

        board.SetState(new IntPoint2D() { x = 0, y = 0 }, new SyncGameOfLifeState() { alive = true });
        board.SetState(new IntPoint2D() { x = 0, y = 1}, new SyncGameOfLifeState() { alive = true });
        board.SetState(new IntPoint2D() { x = 0, y = 2},new SyncGameOfLifeState() { alive = true });
    }
   
    public void ProcessBoard()
    {
        var size = new IntPoint2D() { x = width, y = height };
        ProcessBoard(new IntPoint2D(), size);
        board.Copy(stagingBoard, new IntPoint2D(), size);
    }

    public void ProcessBoard(IntPoint2D from, IntPoint2D to)
    {
        for (int i = from.x; i < to.x; ++i)
        {
            for (int j = from.y; j < to.y; j++)
            {
                var coords = new IntPoint2D() { x = i, y = j };
                var nextState = cellularAutomaton.NextState(coords);
                stagingBoard.SetState(coords, nextState);
            }
        }
    }

    public Color32[] GetColors()
    {
        Color32[] colors = new Color32[width * height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var state = board.GetState(new IntPoint2D() { x = i, y = j });

                colors[i * height + j] = state.alive ? Color.black : Color.white;
            }
        }

        return colors;
    }
}

