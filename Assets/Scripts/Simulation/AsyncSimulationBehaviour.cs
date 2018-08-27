using System.Threading;
using UnityEngine;

public class AsyncSimulationBehaviour : SimulationBehaviour<AsyncGameOfLife>
{
    protected override void Start()
    {
        base.Start();

        new Thread(() => ThrededSimulate(Vector2Int.zero, new Vector2Int(width, height / 4))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height / 4), new Vector2Int(width, height / 2))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height / 2), new Vector2Int(width, height * 3 / 4))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height * 3 / 4), new Vector2Int(width, height))).Start();
    }

    void ThrededSimulate(Vector2Int from, Vector2Int to)
    {
        while (true)
        {
            _gameOfLife.ProcessBoard(new IntPoint2D() { x = from.x, y = from.y }, new IntPoint2D() { x = to.x, y = to.y });
            Thread.Sleep(250);
        }
    }
}