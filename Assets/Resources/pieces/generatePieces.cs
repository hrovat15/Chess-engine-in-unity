using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    void Start()
    {
        renderPieces();
    }

    void renderPieces()
    {

        DrawBitboard(game.WhitePawns, "white-pawn");
        DrawBitboard(game.WhiteRooks, "white-rook");
        DrawBitboard(game.WhiteKnights, "white-knight");
        DrawBitboard(game.WhiteBishops, "white-bishop");
        DrawBitboard(game.WhiteQueens, "white-queen");
        DrawBitboard(game.WhiteKing, "white-king");
        DrawBitboard(game.BlackPawns, "black-pawn");
        DrawBitboard(game.BlackRooks, "black-rook");
        DrawBitboard(game.BlackKnights, "black-knight");
        DrawBitboard(game.BlackBishops, "black-bishop");
        DrawBitboard(game.BlackQueens, "black-queen");
        DrawBitboard(game.BlackKing, "black-king");
    }

    private void DrawBitboard(ulong bitboard, string pieceName)
    {
        for (int i = 0; i < 64; i++)
        {
            // Preverimo, ce je i-ti bit vklopljen
            if (((bitboard >> i) & 1) == 1)
            {
                int x = i % 8;
                int y = i / 8;

                GameObject generatedPiece = new GameObject($"{pieceName}(Piece{x+y})");
                generatedPiece.transform.position = new Vector3(x, y, 0);

                SpriteRenderer renderer = generatedPiece.AddComponent<SpriteRenderer>();

                Texture2D texture = Resources.Load<Texture2D>($"pieces/{pieceName}");

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.zero,
                    100f
                );

                renderer.sprite = sprite;
                renderer.sortingLayerName = "pieces";

                BoxCollider2D collider = generatedPiece.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.27f, 1.27f);

                float targetSize = 100f;
                float scale = targetSize / Mathf.Max(sprite.rect.width, sprite.rect.height);
                generatedPiece.transform.localScale = Vector3.one * scale;
            }
        }
    }
}
