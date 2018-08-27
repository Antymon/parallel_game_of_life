using System.Threading;
using UnityEngine;

public class AsyncSimulationBehaviour : SimulationBehaviour<AsyncGameOfLife>
{
    [SerializeField]
    private byte numThreads = 16;

    private int _multiThreadedUpdatetimeMs;

    private Thread[] threads;

    protected override void Start()
    {
        base.Start();

        _gameOfLife.SetNumberOfBackgroundColors(numThreads);

        _multiThreadedUpdatetimeMs = (int)(SINGLE_PROCESS_BOARD_UPDATE_TIME_SEC / numThreads * 1000);

        Debug.Log(string.Format("Thread will be woken up every {0} milliseconds", _multiThreadedUpdatetimeMs));

        threads = new Thread[numThreads];

        for (byte i = 0; i < numThreads; ++i)
        {
            int fromY = i * height / numThreads;
            int toY = (i + 1) * height / numThreads;
            threads[i] = new Thread(() => ThrededSimulate(new Vector2Int(0, fromY), new Vector2Int(width, toY)));
            threads[i].Start();

            Debug.Log(string.Format("Thread[i:{0},y0:{1},y1:{2}]",i,fromY,toY));
        }
    }

    void ThrededSimulate(Vector2Int from, Vector2Int to)
    {
        while (true)
        {
            _gameOfLife.ProcessBoard(new IntPoint2D() { x = from.x, y = from.y }, new IntPoint2D() { x = to.x, y = to.y });
            Thread.Sleep(_multiThreadedUpdatetimeMs);
        }
    }

    private void OnDestroy()
    {
        for (byte i = 0; i < numThreads; ++i)
        {
            threads[i].Interrupt();

            Debug.Log(string.Format("Thread[i:{0}] interrupted! Ouch!", i));
        }
    }
}