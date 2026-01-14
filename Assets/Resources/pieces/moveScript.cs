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
            default:
                availableMoves = 0;
                break;
        }
        drawPossibleMoves();
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
                selectedPiece.position = new Vector2(targetX, targetY);

                //posodobi stanje boarda v igri
                Debug.Log(game.UpdatePosition((int)(originalPosition.y * 8 + originalPosition.x), targetIndex, selectedPiece.name[6].ToString() + selectedPiece.name[7].ToString()));
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
