using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class moveScript : MonoBehaviour
{
    private ChessInput inputActions;
    private Camera mainCamera;
    [SerializeField] private AudioClip moveAudio;
    private AudioSource audioSource;

    // Variables to track the currently dragged piece
    private Transform selectedPiece;
    private Vector2 originalPosition;

    ulong availableMoves;

    private void Awake()
    {
        inputActions = new ChessInput();
        mainCamera = Camera.main;

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // Subscribe to the "Click" events
        inputActions.Game.Click.started += OnClickStarted;
        inputActions.Game.Click.canceled += OnClickCanceled;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        inputActions.Game.Click.started -= OnClickStarted;
        inputActions.Game.Click.canceled -= OnClickCanceled;
    }

    private void Update()
    {
        // If we have a piece selected, move it
        if (selectedPiece != null)
        {
            Vector2 mousePos = GetMouseWorldPosition();
            selectedPiece.position = mousePos - new Vector2(0.5f, 0.5f);
        }
    }

    private void OnClickStarted(InputAction.CallbackContext context)
    {
        // IZBOLJŠAVA: Koda se izvede samo enkrat, ko se klik začne
        if (!context.started) return;

        Vector2 mousePos = GetMouseWorldPosition();
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.transform.name[0] == 'w' && game.isWhiteTurn == -1) return;
            if (hit.transform.name[0] == 'b' && game.isWhiteTurn == 1) return;
            selectedPiece = hit.transform;
            originalPosition = selectedPiece.position;

            // Izračun indeksa
            int index = Mathf.RoundToInt(selectedPiece.position.y) * 8 + Mathf.RoundToInt(selectedPiece.position.x);
            getMoves(index, selectedPiece.name);
        }
    }

    private void getMoves(int index, string name)
    {
        availableMoves = 0;

        string piece = name[6].ToString() + name[7].ToString();
        bool isWhite = name[0] == 'w';

        switch (piece)
        {
            case "pa":
                availableMoves = game.GetPawnMoves(index, isWhite);
                break;
            case "bi":
                availableMoves = game.GetBishopMoves(index, isWhite);
                break;
            case "kn":
                availableMoves = game.GetKnightMoves(index, isWhite);
                break;
            case "ro":
                availableMoves = game.GetRookMoves(index, isWhite);
                break;
            case "qu":
                availableMoves = game.GetQueenMoves(index, isWhite);
                break;
            case "ki":
                availableMoves = game.GetKingMoves(index, isWhite);
                break;
            default:
                availableMoves = 0;
                break;
        }

        // Remove moves that would leave own king in check
        availableMoves = FilterLegalMoves(index, availableMoves, isWhite, piece);

        drawPossibleMoves();
    }

    // Filters candidate moves by simulating each and removing those that leave mover's king in check
    private ulong FilterLegalMoves(int from, ulong moves, bool isWhite, string pieceName)
    {
        if (moves == 0) return 0UL;

        // backup board state
        ulong b_WhitePawns = game.WhitePawns;
        ulong b_WhiteRooks = game.WhiteRooks;
        ulong b_WhiteKnights = game.WhiteKnights;
        ulong b_WhiteBishops = game.WhiteBishops;
        ulong b_WhiteQueens = game.WhiteQueens;
        ulong b_WhiteKing = game.WhiteKing;

        ulong b_BlackPawns = game.BlackPawns;
        ulong b_BlackRooks = game.BlackRooks;
        ulong b_BlackKnights = game.BlackKnights;
        ulong b_BlackBishops = game.BlackBishops;
        ulong b_BlackQueens = game.BlackQueens;
        ulong b_BlackKing = game.BlackKing;

        ulong b_WhitePieces = game.WhitePieces;
        ulong b_BlackPieces = game.BlackPieces;
        ulong b_AllPieces = game.AllPieces;

        // backup castling/en-passant and last-move metadata
        bool b_WhiteCanCastleKingSide = game.WhiteCanCastleKingSide;
        bool b_WhiteCanCastleQueenSide = game.WhiteCanCastleQueenSide;
        bool b_BlackCanCastleKingSide = game.BlackCanCastleKingSide;
        bool b_BlackCanCastleQueenSide = game.BlackCanCastleQueenSide;
        int b_enPassantSquare = game.enPassantSquare;

        int b_lastCapturedSquare = game.lastCapturedSquare;
        bool b_lastMoveWasEnPassant = game.lastMoveWasEnPassant;
        int b_lastCastleRookFrom = game.lastCastleRookFrom;
        int b_lastCastleRookTo = game.lastCastleRookTo;
        int b_lastPromotedSquare = game.lastPromotedSquare;
        string b_lastPromotedPiece = game.lastPromotedPiece;

        ulong filtered = 0UL;
        ulong tmp = moves;
        while (tmp != 0)
        {
            // get least significant bit index
            int to = 0;
            ulong ttmp = tmp;
            while ((ttmp & 1UL) == 0)
            {
                ttmp >>= 1;
                to++;
            }

            // perform move
            game.UpdatePosition(from, to, pieceName);

            // if king is not in check after move, keep this move
            if (!game.IsKingInCheck(isWhite))
            {
                filtered |= (1UL << to);
            }

            // restore board (including castling/en-passant and metadata)
            game.WhitePawns = b_WhitePawns;
            game.WhiteRooks = b_WhiteRooks;
            game.WhiteKnights = b_WhiteKnights;
            game.WhiteBishops = b_WhiteBishops;
            game.WhiteQueens = b_WhiteQueens;
            game.WhiteKing = b_WhiteKing;

            game.BlackPawns = b_BlackPawns;
            game.BlackRooks = b_BlackRooks;
            game.BlackKnights = b_BlackKnights;
            game.BlackBishops = b_BlackBishops;
            game.BlackQueens = b_BlackQueens;
            game.BlackKing = b_BlackKing;

            game.WhitePieces = b_WhitePieces;
            game.BlackPieces = b_BlackPieces;
            game.AllPieces = b_AllPieces;

            game.WhiteCanCastleKingSide = b_WhiteCanCastleKingSide;
            game.WhiteCanCastleQueenSide = b_WhiteCanCastleQueenSide;
            game.BlackCanCastleKingSide = b_BlackCanCastleKingSide;
            game.BlackCanCastleQueenSide = b_BlackCanCastleQueenSide;
            game.enPassantSquare = b_enPassantSquare;

            game.lastCapturedSquare = b_lastCapturedSquare;
            game.lastMoveWasEnPassant = b_lastMoveWasEnPassant;
            game.lastCastleRookFrom = b_lastCastleRookFrom;
            game.lastCastleRookTo = b_lastCastleRookTo;
            game.lastPromotedSquare = b_lastPromotedSquare;
            game.lastPromotedPiece = b_lastPromotedPiece;

            // clear tested bit
            tmp &= tmp - 1;
        }

        return filtered;
    }

    private void PlayMoveSound()
    {
        if (moveAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(moveAudio);
        }
    }

    private void drawPossibleMoves()
    {
        for (int i = 0; i < 64; i++)
        {
            if (((availableMoves >> i) & 1) == 1)
            {
                int x = i % 8;
                int y = i / 8;
                GameObject marker = new GameObject($"MoveMarker_{x}_{y}");
                marker.transform.localScale = new Vector2(0.8f, 0.8f);
                marker.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);

                SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
                marker.tag = "MoveMarker";

                Texture texture = Resources.Load<Texture2D>($"pieces/indicator");

                Sprite sprite = Sprite.Create(
                    (Texture2D)texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                renderer.sprite = sprite;
                renderer.sortingLayerName = "indicators";
            }
        }
    }

    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        // The player let go of the mouse button
        if (selectedPiece == null) return;

        int targetX = Mathf.RoundToInt(selectedPiece.position.x);
        int targetY = Mathf.RoundToInt(selectedPiece.position.y);

        if (targetX >= 0 && targetX < 8 && targetY >= 0 && targetY < 8)
        {
            int targetIndex = targetY * 8 + targetX;
            ulong targetBit = 1UL << targetIndex;

            // 2. PREVERJANJE VELJAVNOSTI: Ali je ciljni bit v maski možnih potez?
            if ((targetBit & availableMoves) != 0)
            {
                // VELJAVNA POTEZA: Postavi figuro točno na sredino polja
                if (Vector2.Distance(selectedPiece.position, originalPosition) > 0.1f)
                {
                    PlayMoveSound();
                }

                //posodobi stanje boarda v igri
                bool moverIsWhite = selectedPiece.name[0] == 'w';
                string pieceName = selectedPiece.name[6].ToString() + selectedPiece.name[7].ToString();
                int fromIndex = (int)(originalPosition.y * 8 + originalPosition.x);

                // call update
                game.UpdatePosition(fromIndex, targetIndex, pieceName);

                // Destroy any piece that is now on the destination (normal capture)
                foreach(GameObject piece in GameObject.FindGameObjectsWithTag("ChessPiece"))
                {
                    if (piece.name != selectedPiece.name && piece.transform.position == selectedPiece.position)
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
                    // In case takePiece recorded a captured square not overlapping destination (rare), remove it too
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
                            // update name to reflect moved rook position (keep same color and piece type)
                            break;
                        }
                    }
                }

                // Handle promotion: replace sprite/name for promoted pawn (auto-promote to queen currently)
                if (game.lastPromotedSquare == targetIndex && game.lastPromotedPiece != null)
                {
                    // set sprite and rename selected piece to queen
                    string colorPrefix = moverIsWhite ? "white" : "black";
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

                selectedPiece.position = new Vector2(targetX, targetY);

                // After move: detect check or checkmate against opponent
                bool opponentIsWhite = !moverIsWhite;
                if (game.IsKingInCheck(opponentIsWhite))
                {
                    Debug.Log("Check on " + (opponentIsWhite ? "white" : "black"));
                }
                if (game.IsCheckmate(opponentIsWhite))
                {
                    Debug.Log("Checkmate on " + (opponentIsWhite ? "white" : "black"));
                }

                // flip turn
                game.isWhiteTurn *= -1;
            }
            else
            {
                // NEVELJAVNA POTEZA: Vrni figuro na začetno mesto
                selectedPiece.position = originalPosition;
            }
        }
        else
        {
            // Spustili smo izven šahovnice
            selectedPiece.position = originalPosition;
        }

        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("MoveMarker"))
        {
            Destroy(marker);
        }

        selectedPiece = null;
    }

    private Vector2 GetMouseWorldPosition()
    {
        // Read the "Point" value from our Input Action
        Vector2 screenPos = inputActions.Game.Point.ReadValue<Vector2>();
        return mainCamera.ScreenToWorldPoint(screenPos);
    }
}
