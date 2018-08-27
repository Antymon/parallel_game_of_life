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
                var nextTense = (AsyncGameOfLifeState.Tense)((((int)currentState.tense) + 1) % 3);
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

        for (int i = 0; i < neigboursStates.Length; ++i)
        {
            if (neigboursStates[i].tense == AsyncGameOfLifeState.Tense.Present)
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
            if (state.tense == forbiddenTense)
            {
                return false;
            }
        }

        return true;
    }
}