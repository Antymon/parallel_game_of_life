using System.Collections.Generic;

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
    public static readonly Dictionary<Tense, Tense> NOT_READY_MAP = new Dictionary<Tense, Tense>
    {
        {Tense.Present, Tense.Past},
        {Tense.Future, Tense.Present},
        {Tense.Past, Tense.Future }
    };

    public enum Tense : byte
    {
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