using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class game
{
    public static ulong WhitePawns = 0x000000000000FF00;
    public static ulong WhiteRooks = 0x0000000000000081;
    public static ulong WhiteKnights = 0x0000000000000042;
    public static ulong WhiteBishops = 0x0000000000000024;
    public static ulong WhiteQueens = 0x0000000000000008;
    public static ulong WhiteKing = 0x0000000000000010;
           
    public static ulong BlackPawns = 0x00FF000000000000;
    public static ulong BlackRooks = 0x8100000000000000;
    public static ulong BlackKnights = 0x4200000000000000;
    public static ulong BlackBishops = 0x2400000000000000;
    public static ulong BlackQueens = 0x0800000000000000;
    public static ulong BlackKing = 0x1000000000000000;

    public static ulong WhitePieces = WhitePawns | WhiteRooks | WhiteKnights | WhiteBishops | WhiteQueens | WhiteKing;
    public static ulong BlackPieces = BlackPawns | BlackRooks | BlackKnights | BlackBishops | BlackQueens | BlackKing;

    public static ulong AllPieces = WhitePieces | BlackPieces;

    public const ulong notAFile = 0xFEFEFEFEFEFEFEFE;
    public const ulong notHFile = 0x7F7F7F7F7F7F7F7F;

    public const ulong not1stRank = 0xFFFFFFFFFFFFFF00;
    public const ulong not8thRank = 0x00FFFFFFFFFFFFFF;

    // Za konja bosta prišla prav še stolpca B in G
    public const ulong notABFile = 0xFCFCFCFCFCFCFCFC;
    public const ulong notGHFile = 0x3F3F3F3F3F3F3F3F;

    public static int isWhiteTurn = 1;

    // --- Advanced state (castling / en-passant / last-move metadata for the UI integration) ---
    public static bool WhiteCanCastleKingSide = true;
    public static bool WhiteCanCastleQueenSide = true;
    public static bool BlackCanCastleKingSide = true;
    public static bool BlackCanCastleQueenSide = true;

    // en-passant target square index (the square a pawn can move to when capturing en-passant). -1 = none
    public static int enPassantSquare = -1;

    // Metadata produced by last UpdatePosition call (used by graphical layer to sync GameObjects)
    public static int lastCapturedSquare = -1;       // square index of last captured piece (or -1)
    public static bool lastMoveWasEnPassant = false;
    public static int lastCastleRookFrom = -1;
    public static int lastCastleRookTo = -1;
    public static int lastPromotedSquare = -1;      // if promotion happened, target square index
    public static string lastPromotedPiece = null;  // e.g. "qu"

    public static void UpdatePosition(int from, int to, string pieceName)
    {
        // reset last-move metadata
        lastCapturedSquare = -1;
        lastMoveWasEnPassant = false;
        lastCastleRookFrom = -1;
        lastCastleRookTo = -1;
        lastPromotedSquare = -1;
        lastPromotedPiece = null;

        ulong fromBit = 1UL << from;
        ulong toBit = 1UL << to;

        bool isWhite = (fromBit & WhitePieces) != 0 ? true : false;

        // --- EN PASSANT capture handling (target square is empty) ---
        if (pieceName == "pa" && enPassantSquare >= 0 && to == enPassantSquare)
        {
            // Captured pawn is on the square behind the enPassant target relative to the mover.
            int capturedSquare = isWhite ? to - 8 : to + 8;
            ulong capturedBit = 1UL << capturedSquare;
            takePiece(capturedBit, isWhite);
            lastCapturedSquare = capturedSquare;
            lastMoveWasEnPassant = true;
        }

        // --- Normal capture on destination (if any) ---
        if ((toBit & AllPieces) != 0)
        {
            takePiece(toBit, isWhite);
            // takePiece sets lastCapturedSquare as well
        }

        // --- Castling detection (king moves two squares) ---
        bool performedCastling = false;
        if (pieceName == "ki")
        {
            // moving king disables castling rights for that color
            if (isWhite)
            {
                WhiteCanCastleKingSide = false;
                WhiteCanCastleQueenSide = false;
            }
            else
            {
                BlackCanCastleKingSide = false;
                BlackCanCastleQueenSide = false;
            }

            // White castling (king from e1 index 4)
            if (isWhite && from == 4)
            {
                if (to == 6) // king-side: move rook h1 (7) -> f1 (5)
                {
                    ulong rookFrom = 1UL << 7;
                    ulong rookTo = 1UL << 5;
                    WhiteRooks &= ~rookFrom;
                    WhiteRooks |= rookTo;
                    WhitePieces &= ~rookFrom;
                    WhitePieces |= rookTo;
                    AllPieces &= ~rookFrom;
                    AllPieces |= rookTo;
                    lastCastleRookFrom = 7;
                    lastCastleRookTo = 5;
                    performedCastling = true;
                }
                else if (to == 2) // queen-side: rook a1 (0) -> d1 (3)
                {
                    ulong rookFrom = 1UL << 0;
                    ulong rookTo = 1UL << 3;
                    WhiteRooks &= ~rookFrom;
                    WhiteRooks |= rookTo;
                    WhitePieces &= ~rookFrom;
                    WhitePieces |= rookTo;
                    AllPieces &= ~rookFrom;
                    AllPieces |= rookTo;
                    lastCastleRookFrom = 0;
                    lastCastleRookTo = 3;
                    performedCastling = true;
                }
            }
            // Black castling (king from e8 index 60)
            if (!isWhite && from == 60)
            {
                if (to == 62) // king-side: rook h8 (63) -> f8 (61)
                {
                    ulong rookFrom = 1UL << 63;
                    ulong rookTo = 1UL << 61;
                    BlackRooks &= ~rookFrom;
                    BlackRooks |= rookTo;
                    BlackPieces &= ~rookFrom;
                    BlackPieces |= rookTo;
                    AllPieces &= ~rookFrom;
                    AllPieces |= rookTo;
                    lastCastleRookFrom = 63;
                    lastCastleRookTo = 61;
                    performedCastling = true;
                }
                else if (to == 58) // queen-side: rook a8 (56) -> d8 (59)
                {
                    ulong rookFrom = 1UL << 56;
                    ulong rookTo = 1UL << 59;
                    BlackRooks &= ~rookFrom;
                    BlackRooks |= rookTo;
                    BlackPieces &= ~rookFrom;
                    BlackPieces |= rookTo;
                    AllPieces &= ~rookFrom;
                    AllPieces |= rookTo;
                    lastCastleRookFrom = 56;
                    lastCastleRookTo = 59;
                    performedCastling = true;
                }
            }
        }

        // --- Piece movement and special cases (promotion, rook moved -> update castling rights) ---
        if (isWhite)
        {
            switch (pieceName)
            {
                case "pa":
                    WhitePawns &= ~fromBit; // remove from
                    // promotion?
                    if (to / 8 == 7)
                    {
                        // auto-promote to queen
                        WhiteQueens |= toBit;
                        lastPromotedSquare = to;
                        lastPromotedPiece = "qu";
                    }
                    else
                    {
                        WhitePawns |= toBit;
                    }
                    break;
                case "kn":
                    WhiteKnights &= ~fromBit;
                    WhiteKnights |= toBit;
                    break;
                case "bi":
                    WhiteBishops &= ~fromBit;
                    WhiteBishops |= toBit;
                    break;
                case "ro":
                    WhiteRooks &= ~fromBit;
                    WhiteRooks |= toBit;
                    // if rook moved from initial squares, revoke castling rights
                    if (from == 0) WhiteCanCastleQueenSide = false;
                    if (from == 7) WhiteCanCastleKingSide = false;
                    break;
                case "qu":
                    WhiteQueens &= ~fromBit;
                    WhiteQueens |= toBit;
                    break;
                case "ki":
                    WhiteKing &= ~fromBit;
                    WhiteKing |= toBit;
                    // king moved -> castling rights were cleared above
                    break;
            }

            // Update WhitePieces bitboard (remove fromBit and add toBit)
            WhitePieces &= ~fromBit;
            WhitePieces |= toBit;
        }
        else
        {
            switch (pieceName)
            {
                case "pa":
                    BlackPawns &= ~fromBit;
                    if (to / 8 == 0)
                    {
                        // auto-promote to queen
                        BlackQueens |= toBit;
                        lastPromotedSquare = to;
                        lastPromotedPiece = "qu";
                    }
                    else
                    {
                        BlackPawns |= toBit;
                    }
                    break;
                case "kn":
                    BlackKnights &= ~fromBit;
                    BlackKnights |= toBit;
                    break;
                case "bi":
                    BlackBishops &= ~fromBit;
                    BlackBishops |= toBit;
                    break;
                case "ro":
                    BlackRooks &= ~fromBit;
                    BlackRooks |= toBit;
                    if (from == 56) BlackCanCastleQueenSide = false;
                    if (from == 63) BlackCanCastleKingSide = false;
                    break;
                case "qu":
                    BlackQueens &= ~fromBit;
                    BlackQueens |= toBit;
                    break;
                case "ki":
                    BlackKing &= ~fromBit;
                    BlackKing |= toBit;
                    break;
            }

            BlackPieces &= ~fromBit;
            BlackPieces |= toBit;
        }

        // Update global AllPieces
        AllPieces &= ~fromBit;
        AllPieces |= toBit;

        // --- If move was a pawn double push, set enPassantSquare, otherwise clear it ---
        if (pieceName == "pa" && System.Math.Abs(to - from) == 16)
        {
            // en-passant target is the square the pawn jumped over
            enPassantSquare = isWhite ? from + 8 : from - 8;
        }
        else
        {
            enPassantSquare = -1;
        }
    }

    private static void takePiece(ulong bit, bool isWhite)
    {
        // record captured square index for graphical sync
        int capturedIdx = BitScanForward(bit);
        lastCapturedSquare = capturedIdx;

        if (!isWhite)
        {
            // mover is black -> remove white pieces
            // if we capture a rook on a1/h1, revoke castling rights for white
            if ((bit & (1UL << 0)) != 0) WhiteCanCastleQueenSide = false;
            if ((bit & (1UL << 7)) != 0) WhiteCanCastleKingSide = false;
            if ((bit & WhiteKing) != 0) { WhiteCanCastleKingSide = false; WhiteCanCastleQueenSide = false; }

            WhitePawns &= ~bit;
            WhiteRooks &= ~bit;
            WhiteKnights &= ~bit;
            WhiteBishops &= ~bit;
            WhiteQueens &= ~bit;
            WhiteKing &= ~bit;
            WhitePieces &= ~bit;
        }
        else
        {
            // mover is white -> remove black pieces
            if ((bit & (1UL << 56)) != 0) BlackCanCastleQueenSide = false;
            if ((bit & (1UL << 63)) != 0) BlackCanCastleKingSide = false;
            if ((bit & BlackKing) != 0) { BlackCanCastleKingSide = false; BlackCanCastleQueenSide = false; }

            BlackPawns &= ~bit;
            BlackRooks &= ~bit;
            BlackKnights &= ~bit;
            BlackBishops &= ~bit;
            BlackQueens &= ~bit;
            BlackKing &= ~bit;
            BlackPieces &= ~bit;
        }

        AllPieces &= ~bit;
    }

    public static ulong GetPawnMoves(int square, bool isWhite)
    {
        ulong bit = 1UL << square;

        ulong enemyPieces = isWhite ? BlackPieces : WhitePieces;
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;

        ulong forwardMoves = 0;
        ulong captures = 0;

        // enPassant bitboard to allow generation of that capture (target square is empty)
        ulong enPassantBit = enPassantSquare >= 0 ? (1UL << enPassantSquare) : 0UL;

        if (isWhite)
        {
            // PREMIK NAPREJ: Polje mora biti popolnoma prazno (& ~allPieces)
            ulong singlePush = (bit << 8) & ~AllPieces;
            if (singlePush != 0)
            {
                forwardMoves |= singlePush;
                // Dvojni skok (le ?e je prvi skok uspel in smo na 2. vrsti)
                if (square / 8 == 1)
                {
                    forwardMoves |= (bit << 16) & ~AllPieces;
                }
            }

            // JEMANJE: Polje mora vsebovati NASPROTNIKA (& enemyPieces) OR be the en-passant target
            captures |= (bit << 7) & notHFile & (enemyPieces | enPassantBit);
            captures |= (bit << 9) & notAFile & (enemyPieces | enPassantBit);
        }
        else // črni kmetje (obratno)
        {
            ulong singlePush = (bit >> 8) & ~AllPieces;
            if (singlePush != 0)
            {
                forwardMoves |= singlePush;
                if (square / 8 == 6)
                {
                    forwardMoves |= (bit >> 16) & ~AllPieces;
                }
            }
            captures |= (bit >> 7) & notAFile & (enemyPieces | enPassantBit);
            captures |= (bit >> 9) & notHFile & (enemyPieces | enPassantBit);
        }

        return forwardMoves | captures;
    }

    public static ulong GetKnightMoves(int square, bool isWhite)
    {
        ulong bit = 1UL << square; 
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;
        ulong moves = 0;
        moves |= (bit << 17) & notAFile; // 2 gor, 1 desno
        moves |= (bit << 15) & notHFile; // 2 gor, 1 levo
        moves |= (bit << 10) & notABFile; // 1 gor, 2 desno
        moves |= (bit << 6) & notGHFile; // 1 gor, 2 levo
        moves |= (bit >> 17) & notHFile; // 2 dol, 1 levo
        moves |= (bit >> 15) & notAFile; // 2 dol, 1 desno
        moves |= (bit >> 10) & notGHFile; // 1 dol, 2 levo
        moves |= (bit >> 6) & notABFile; // 1 dol, 2 desno
        // Odstranimo polja z lastnimi figurami
        moves &= ~myPieces;
        return moves;
    }

    public static ulong GetBishopMoves(int square, bool isWhite)
    {
        ulong bit = 1UL << square;
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;
        ulong enemyPieces = isWhite ? BlackPieces : WhitePieces;
        int[] directions = { 7, 9, -7, -9 }; // top-left and top-right
        ulong moves = 0;

        foreach (int dir in directions)
        {
            ulong current = bit;

            // Slide up the board
            while (true)
            {
                // Check file boundaries before shift
                if ((dir == 9 && (current & notHFile) == 0) ||
                    (dir == 7 && (current & notAFile) == 0) ||
                    (dir == -7 && (current & notHFile) == 0) ||
                    (dir == -9 && (current & notAFile) == 0))
                    break;

                current = dir > 0
                    ? current << dir
                    : current >> -dir;

                if (current == 0) break;

                if ((current & myPieces) != 0) break;

                moves |= current;

                if ((current & enemyPieces) != 0) break;
            }
        }
        return moves;
    }

    public static ulong GetRookMoves(int square, bool isWhite)
    {
        ulong bit = 1UL << square;
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;
        ulong enemyPieces = isWhite ? BlackPieces : WhitePieces;
        ulong moves = 0;

        int[] directions = { 1, -1, 8, -8 };

        foreach (int dir in directions)
        {
            ulong current = bit;

            while (true)
            {
                if (dir == 1 && (current & notHFile) == 0) break;
                if (dir == -1 && (current & notAFile) == 0) break;
                if (dir == 8 && (current & not8thRank) == 0) break;
                if (dir == -8 && (current & not1stRank) == 0) break;

                current = dir > 0
                    ? current << dir
                    : current >> -dir;

                if (current == 0) break;

                if ((current & myPieces) != 0) break;

                moves |= current;

                if ((current & enemyPieces) != 0) break;
            }
        }

        return moves;
    }

    public static ulong GetQueenMoves(int square, bool isWhite)
    {
        return GetBishopMoves(square, isWhite) | GetRookMoves(square, isWhite);
    }

    // Add an optional parameter to GetKingMoves
    public static ulong GetKingMoves(int square, bool isWhite, bool includeCastling = true)
    {
        ulong bit = 1UL << square;
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;
        ulong moves = 0;
        moves |= (bit << 8); // gor
        moves |= (bit >> 8); // dol
        moves |= (bit << 1) & notAFile; // desno
        moves |= (bit >> 1) & notHFile; // levo
        moves |= (bit << 9) & notAFile; // gor desno
        moves |= (bit << 7) & notHFile; // gor levo
        moves |= (bit >> 7) & notAFile; // dol desno
        moves |= (bit >> 9) & notHFile; // dol levo
        // Odstranimo polja z lastnimi figurami
        moves &= ~myPieces;

        // Only check castling if requested (to prevent infinite recursion)
        if (!includeCastling) return moves;

        // CASTLING generation: add king-side/queen-side if legal
        if (isWhite)
        {
            // White king must be on e1 (4). Check rights and empty squares and not attacked squares.
            if ((WhiteKing != 0) && (BitScanForward(WhiteKing) == 4))
            {
                // King-side
                if (WhiteCanCastleKingSide)
                {
                    // f1 (5) and g1 (6) must be empty
                    if (((AllPieces & ((1UL << 5) | (1UL << 6))) == 0) &&
                        !IsSquareAttacked(4, !isWhite) &&
                        !IsSquareAttacked(5, !isWhite) &&
                        !IsSquareAttacked(6, !isWhite))
                    {
                        moves |= (1UL << 6);
                    }
                }
                // Queen-side
                if (WhiteCanCastleQueenSide)
                {
                    // b1 (1), c1 (2), d1 (3) must be empty (pieces between king and rook)
                    if (((AllPieces & ((1UL << 1) | (1UL << 2) | (1UL << 3))) == 0) &&
                        !IsSquareAttacked(4, !isWhite) &&
                        !IsSquareAttacked(3, !isWhite) &&
                        !IsSquareAttacked(2, !isWhite))
                    {
                        moves |= (1UL << 2);
                    }
                }
            }
        }
        else
        {
            // Black king must be on e8 (60)
            if ((BlackKing != 0) && (BitScanForward(BlackKing) == 60))
            {
                if (BlackCanCastleKingSide)
                {
                    // f8 (61) and g8 (62) empty
                    if (((AllPieces & ((1UL << 61) | (1UL << 62))) == 0) &&
                        !IsSquareAttacked(60, !isWhite) &&
                        !IsSquareAttacked(61, !isWhite) &&
                        !IsSquareAttacked(62, !isWhite))
                    {
                        moves |= (1UL << 62);
                    }
                }
                if (BlackCanCastleQueenSide)
                {
                    // b8 (57), c8 (58), d8 (59) empty
                    if (((AllPieces & ((1UL << 57) | (1UL << 58) | (1UL << 59))) == 0) &&
                        !IsSquareAttacked(60, !isWhite) &&
                        !IsSquareAttacked(59, !isWhite) &&
                        !IsSquareAttacked(58, !isWhite))
                    {
                        moves |= (1UL << 58);
                    }
                }
            }
        }

        return moves;
    }

    // portable bit scan forward helper (counts trailing zeros). returns -1 if bb == 0
    private static int BitScanForward(ulong bb)
    {
        if (bb == 0) return -1;
        int idx = 0;
        while ((bb & 1UL) == 0)
        {
            bb >>= 1;
            idx++;
        }
        return idx;
    }

    // Checks whether a given square is attacked by the specified color (byWhite = true -> white attacks)
    public static bool IsSquareAttacked(int square, bool byWhite)
    {
        ulong targetBit = 1UL << square;

        // Pawn attacks (computed for all pawns of attacker color)
        ulong attackerPawns = byWhite ? WhitePawns : BlackPawns;
        ulong pawnAttacks;
        if (byWhite)
            pawnAttacks = ((attackerPawns << 7) & notHFile) | ((attackerPawns << 9) & notAFile);
        else
            pawnAttacks = ((attackerPawns >> 7) & notAFile) | ((attackerPawns >> 9) & notHFile);

        if ((pawnAttacks & targetBit) != 0) return true;

        // Knights
        ulong attackerKnights = byWhite ? WhiteKnights : BlackKnights;
        ulong tmp = attackerKnights;
        while (tmp != 0)
        {
            int from = BitScanForward(tmp);
            if ((GetKnightMoves(from, byWhite) & targetBit) != 0) return true;
            tmp &= tmp - 1;
        }

        // Bishops and queens (diagonals)
        ulong attackerBishops = byWhite ? WhiteBishops : BlackBishops;
        tmp = attackerBishops;
        while (tmp != 0)
        {
            int from = BitScanForward(tmp);
            if ((GetBishopMoves(from, byWhite) & targetBit) != 0) return true;
            tmp &= tmp - 1;
        }
        ulong attackerQueens = byWhite ? WhiteQueens : BlackQueens;
        tmp = attackerQueens;
        while (tmp != 0)
        {
            int from = BitScanForward(tmp);
            if ((GetQueenMoves(from, byWhite) & targetBit) != 0) return true;
            tmp &= tmp - 1;
        }

        // Rooks and queens (orthogonals)
        ulong attackerRooks = byWhite ? WhiteRooks : BlackRooks;
        tmp = attackerRooks;
        while (tmp != 0)
        {
            int from = BitScanForward(tmp);
            if ((GetRookMoves(from, byWhite) & targetBit) != 0) return true;
            tmp &= tmp - 1;
        }

        // King
        ulong attackerKing = byWhite ? WhiteKing : BlackKing;
        if (attackerKing != 0)
        {
            int from = BitScanForward(attackerKing);
            // Use includeCastling = false to prevent recursion
            if ((GetKingMoves(from, byWhite, false) & targetBit) != 0) return true;
        }

        return false;
    }

    // Returns true if the king of the given color is currently in check
    public static bool IsKingInCheck(bool isWhite)
    {
        ulong kingBB = isWhite ? WhiteKing : BlackKing;
        if (kingBB == 0) return false; // defensive
        int kingSquare = BitScanForward(kingBB);
        if (kingSquare < 0) return false;
        return IsSquareAttacked(kingSquare, !isWhite);
    }

    // Returns true if the side to move (isWhite) is checkmated
    public static bool IsCheckmate(bool isWhite)
    {
        // If not in check, not checkmate
        if (!IsKingInCheck(isWhite)) return false;

        // Backup board state
        ulong b_WhitePawns = WhitePawns;
        ulong b_WhiteRooks = WhiteRooks;
        ulong b_WhiteKnights = WhiteKnights;
        ulong b_WhiteBishops = WhiteBishops;
        ulong b_WhiteQueens = WhiteQueens;
        ulong b_WhiteKing = WhiteKing;

        ulong b_BlackPawns = BlackPawns;
        ulong b_BlackRooks = BlackRooks;
        ulong b_BlackKnights = BlackKnights;
        ulong b_BlackBishops = BlackBishops;
        ulong b_BlackQueens = BlackQueens;
        ulong b_BlackKing = BlackKing;

        ulong b_WhitePieces = WhitePieces;
        ulong b_BlackPieces = BlackPieces;
        ulong b_AllPieces = AllPieces;

        // Backup castling and en-passant and last-move metadata
        bool b_WhiteCanCastleKingSide = WhiteCanCastleKingSide;
        bool b_WhiteCanCastleQueenSide = WhiteCanCastleQueenSide;
        bool b_BlackCanCastleKingSide = BlackCanCastleKingSide;
        bool b_BlackCanCastleQueenSide = BlackCanCastleQueenSide;
        int b_enPassantSquare = enPassantSquare;

        int b_lastCapturedSquare = lastCapturedSquare;
        bool b_lastMoveWasEnPassant = lastMoveWasEnPassant;
        int b_lastCastleRookFrom = lastCastleRookFrom;
        int b_lastCastleRookTo = lastCastleRookTo;
        int b_lastPromotedSquare = lastPromotedSquare;
        string b_lastPromotedPiece = lastPromotedPiece;

        // Helper to test moves from a piece type bitboard using provided move generator and piece name
        bool TryMovesFromBitboard(ulong piecesBB, System.Func<int,bool,ulong> moveFunc, string pieceName)
        {
            ulong tmp = piecesBB;
            while (tmp != 0)
            {
                int from = BitScanForward(tmp);
                tmp &= tmp - 1;
                ulong moves = moveFunc(from, isWhite);
                ulong mtmp = moves;
                while (mtmp != 0)
                {
                    int to = BitScanForward(mtmp);
                    mtmp &= mtmp - 1;

                    // perform move
                    UpdatePosition(from, to, pieceName);

                    bool kingStillInCheck = IsKingInCheck(isWhite);

                    // restore board (including castling and en-passant and metadata)
                    WhitePawns = b_WhitePawns;
                    WhiteRooks = b_WhiteRooks;
                    WhiteKnights = b_WhiteKnights;
                    WhiteBishops = b_WhiteBishops;
                    WhiteQueens = b_WhiteQueens;
                    WhiteKing = b_WhiteKing;

                    BlackPawns = b_BlackPawns;
                    BlackRooks = b_BlackRooks;
                    BlackKnights = b_BlackKnights;
                    BlackBishops = b_BlackBishops;
                    BlackQueens = b_BlackQueens;
                    BlackKing = b_BlackKing;

                    WhitePieces = b_WhitePieces;
                    BlackPieces = b_BlackPieces;
                    AllPieces = b_AllPieces;

                    WhiteCanCastleKingSide = b_WhiteCanCastleKingSide;
                    WhiteCanCastleQueenSide = b_WhiteCanCastleQueenSide;
                    BlackCanCastleKingSide = b_BlackCanCastleKingSide;
                    BlackCanCastleQueenSide = b_BlackCanCastleQueenSide;
                    enPassantSquare = b_enPassantSquare;

                    lastCapturedSquare = b_lastCapturedSquare;
                    lastMoveWasEnPassant = b_lastMoveWasEnPassant;
                    lastCastleRookFrom = b_lastCastleRookFrom;
                    lastCastleRookTo = b_lastCastleRookTo;
                    lastPromotedSquare = b_lastPromotedSquare;
                    lastPromotedPiece = b_lastPromotedPiece;

                    if (!kingStillInCheck) return true; // found a legal escape
                }
            }
            return false;
        }

        // Try pawns
        if (isWhite)
        {
            if (TryMovesFromBitboard(WhitePawns, GetPawnMoves, "pa")) return false;
            if (TryMovesFromBitboard(WhiteKnights, GetKnightMoves, "kn")) return false;
            if (TryMovesFromBitboard(WhiteBishops, GetBishopMoves, "bi")) return false;
            if (TryMovesFromBitboard(WhiteRooks, GetRookMoves, "ro")) return false;
            if (TryMovesFromBitboard(WhiteQueens, GetQueenMoves, "qu")) return false;
            if (TryMovesFromBitboard(WhiteKing, (from, isWhite) => GetKingMoves(from, isWhite, false), "ki")) return false;
        }
        else
        {
            if (TryMovesFromBitboard(BlackPawns, GetPawnMoves, "pa")) return false;
            if (TryMovesFromBitboard(BlackKnights, GetKnightMoves, "kn")) return false;
            if (TryMovesFromBitboard(BlackBishops, GetBishopMoves, "bi")) return false;
            if (TryMovesFromBitboard(BlackRooks, GetRookMoves, "ro")) return false;
            if (TryMovesFromBitboard(BlackQueens, GetQueenMoves, "qu")) return false;
            if (TryMovesFromBitboard(BlackKing, (from, isWhite) => GetKingMoves(from, isWhite, false), "ki")) return false;
        }

        // No legal moves that remove check -> checkmate
        return true;
    }
}
