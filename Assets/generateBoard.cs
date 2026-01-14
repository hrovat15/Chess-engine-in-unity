using UnityEngine;

public class generateBoard : MonoBehaviour
{
    public Color color1 = Color.white;
    public Color color2 = Color.black;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                GameObject tile = new GameObject($"Tile_{i}_{j}");
                tile.GetComponentInParent<Transform>().SetParent(this.transform);
                tile.transform.position = new Vector3(i, j, 0);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = "board";
                Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero, 1);
                renderer.sprite = sprite;
                if ((i + j) % 2 == 0)
                {
                    renderer.color = color1;
                }
                else
                {
                    renderer.color = color2;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
