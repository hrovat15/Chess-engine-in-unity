using UnityEngine;
using chess_engine_v2;
using System.Xml.Schema;

public class engine : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public int sideToMove = 0; // 0 for white, 1 for black

    static chess_engine_v2.ChessEngineAPI ai = new chess_engine_v2.ChessEngineAPI();

    void Awake()
    {
        this.gameObject.active = false;
    }

    public void setMoveSide(int side)
    {
        sideToMove = side;
    }

    void Start()
    {
        if (sideToMove == 0)
        {
            MakeAiMove();
        }
    }

    public static void MakeAiMove()
    {
        Move move = ai.GetBestMove();
        Debug.Log(move);
        GameObject selectedPiece = null;
        foreach (var piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
        {
            if(piece.transform.position == new Vector3(move.From % 8, move.From / 8, 0))
            {
                selectedPiece = piece;
            }
        }
        game.UpdatePosition(move.From, move.To, getPieceName(move.From));
        ai.MakeMoveWithMove(move);

        selectedPiece.transform.position = new Vector3(move.To % 8, move.To / 8, 0);

        // Destroy any piece that is now on the destination (normal capture)
        foreach (GameObject piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
        {
            if (piece.name != selectedPiece.name && piece.transform.position == selectedPiece.transform.position)
            {
                Destroy(piece);
            }
        }

        // Handle en-passant removal (captured pawn sits on a different square)
        if (game.lastMoveWasEnPassant && game.lastCapturedSquare != -1)
        {
            int cx = game.lastCapturedSquare % 8;
            int cy = game.lastCapturedSquare / 8;
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
            {
                if (piece.transform.position == new Vector3(cx, cy, 0))
                {
                    Destroy(piece);
                }
            }
        }
        else if (game.lastCapturedSquare != -1)
        {
            // In case takePiece recorded a captured square not overlapping destination, remove it too
            int cx = game.lastCapturedSquare % 8;
            int cy = game.lastCapturedSquare / 8;
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
            {
                if (piece.transform.position == new Vector3(cx, cy, 0) && piece.name != selectedPiece.name)
                {
                    Destroy(piece);
                }
            }
        }

        // Handle castling rook movement in the scene
        if (game.lastCastleRookFrom != -1 && game.lastCastleRookTo != -1)
        {
            int rf_x = game.lastCastleRookFrom % 8;
            int rf_y = game.lastCastleRookFrom / 8;
            int rt_x = game.lastCastleRookTo % 8;
            int rt_y = game.lastCastleRookTo / 8;

            // find the rook GameObject at rookFrom and move it to rookTo
            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
            {
                if (piece.transform.position == new Vector3(rf_x, rf_y, 0))
                {
                    piece.transform.position = new Vector3(rt_x, rt_y, 0);
                    break;
                }
            }
        }

        // Handle promotion: replace sprite/name for promoted pawn (auto-promote to queen currently)
        int moveTo = move.To;
        if (game.lastPromotedSquare == moveTo && game.lastPromotedPiece != null)
        {
            // set sprite and rename selected piece to queen
            bool isWhite = selectedPiece.name[0] == 'w';
            string colorPrefix = isWhite ? "white" : "black";
            string queenResource = $"{colorPrefix}-queen";
            Texture2D texture = Resources.Load<Texture2D>($"pieces/{queenResource}");
            if (texture != null)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.zero,
                    100f
                );
                SpriteRenderer renderer = selectedPiece.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sprite = sprite;
            }
            // rename to "white-queen(...)" keeping the suffix "(PieceN)" if present
            int parenIdx = selectedPiece.name.IndexOf('(');
            string suffix = parenIdx >= 0 ? selectedPiece.name.Substring(parenIdx) : "";
            selectedPiece.name = $"{colorPrefix}-queen{suffix}";
        }

        if (game.IsCheckmate(selectedPiece.name[0] == 'w' ? true : false))
        {
            Debug.Log("Checkmate! " + (game.isWhiteTurn > 0 ? "Black" : "White") + " wins!");
        }


        game.isWhiteTurn *= -1;

    }

    public static void MakePlayerMove(int from, int to, ushort flags)
    {
        ai.MakeMoveWithFlags(from, to, flags);
        Debug.Log("My move: " + from + " to " + to + " with flags " + flags);
        game.isWhiteTurn *= -1;
    }

    private static string getPieceName(int index)
    {
        ulong bitboard = 1UL << index;
        if ((bitboard & game.WhitePawns) != 0 || (bitboard & game.BlackPawns) != 0) return "pa";
        else if ((bitboard & game.WhiteRooks) != 0 || (bitboard & game.BlackRooks) != 0) return "ro";
        else if ((bitboard & game.WhiteKnights) != 0 || (bitboard & game.BlackKnights) != 0) return "kn";
        else if ((bitboard & game.WhiteBishops) != 0 || (bitboard & game.BlackBishops) != 0) return "bi";
        else if ((bitboard & game.WhiteQueens) != 0 || (bitboard & game.BlackQueens) != 0) return "qu";
        else if ((bitboard & game.WhiteKing) != 0 || (bitboard & game.BlackKing) != 0) return "ki";
        else return "";
    }
}
