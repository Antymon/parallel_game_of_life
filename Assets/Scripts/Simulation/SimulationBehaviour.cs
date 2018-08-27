using System.Collections;
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

    protected static readonly float SINGLE_PROCESS_BOARD_UPDATE_TIME_SEC = 0.25f;

    protected virtual void Start()
    {
        _gameOfLife = new GameOfLifeType();

        var translatedPoints = new IntPoint2D[initialPattern.Length];
        for (int i = 0; i < initialPattern.Length; ++i)
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