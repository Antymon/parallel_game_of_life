using System.Collections;
using System.Threading;
using UnityEngine;

public class Async : MonoBehaviour
{
    Coroutine _co;

    IGameOfLife asyncGameOfLife;
    Texture2D texture;
    SpriteRenderer rend;

    int width = 8;
    int height = 8;

    void Start()
    {
        asyncGameOfLife = new AsyncGameOfLife(width, height);

        texture = new Texture2D(width, height);

        rend = GetComponent<SpriteRenderer>();
        rend.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero);

        new Thread(() => ThrededSimulate(Vector2Int.zero, new Vector2Int(width, height / 4))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height / 4), new Vector2Int(width, height / 2))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height / 2), new Vector2Int(width, height * 3 / 4))).Start();
        new Thread(() => ThrededSimulate(new Vector2Int(0, height * 3 / 4), new Vector2Int(width, height))).Start() ;

        _co = StartCoroutine(Simulate());
    }

    void ThrededSimulate(Vector2Int from, Vector2Int to)
    {
        while (true)
        {
            asyncGameOfLife.ProcessBoard(new IntPoint2D() { x = from.x, y = from.y }, new IntPoint2D() { x = to.x, y = to.y });
            Thread.Sleep(250);
        }
    }

    IEnumerator Simulate()
    {
        while (true)
        {
            var colors = asyncGameOfLife.GetColors();
            texture.SetPixels32(colors);
            texture.Apply(false);

            yield return new WaitForSeconds(0.1f);
        }
    }
}