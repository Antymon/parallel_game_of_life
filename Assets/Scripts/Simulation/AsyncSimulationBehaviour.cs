using System.Threading;
using UnityEngine;

public class AsyncSimulationBehaviour : SimulationBehaviour<AsyncGameOfLife>
{
    private static readonly byte NUM_THREADS = 4;

    private static readonly int MULTI_PROCESS_BOARD_UPDATE_TIME_MS = (int)(SINGLE_PROCESS_BOARD_UPDATE_TIME_SEC / NUM_THREADS * 1000);

    protected override void Start()
    {
        base.Start();

        for (byte i = 0; i < NUM_THREADS; ++i)
        {
            new Thread(() => ThrededSimulate(new Vector2Int(0, i * height / 4), new Vector2Int(width, (i + 1) * height / 4))).Start();
        }
    }

    void ThrededSimulate(Vector2Int from, Vector2Int to)
    {
        while (true)
        {
            _gameOfLife.ProcessBoard(new IntPoint2D() { x = from.x, y = from.y }, new IntPoint2D() { x = to.x, y = to.y });
            Thread.Sleep(MULTI_PROCESS_BOARD_UPDATE_TIME_MS);
        }
    }
}