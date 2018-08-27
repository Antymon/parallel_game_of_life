using System.Collections;
using UnityEngine;

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


