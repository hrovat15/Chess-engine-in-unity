using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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

    // Za konja bosta prišla prav še stolpca B in G
    public const ulong notABFile = 0xFCFCFCFCFCFCFCFC;
    public const ulong notGHFile = 0x3F3F3F3F3F3F3F3F;

    public static string UpdatePosition(int from, int to, string pieceName)
    {
        ulong fromBit = 1UL << from;
        ulong toBit = 1UL << to;

        bool isWhite = (fromBit & WhitePieces) != 0 ? true: false;

        string a = "";

        if ((toBit & AllPieces) != 0) a = takePiece(toBit, isWhite);

        if (isWhite)
        {
            switch (pieceName)
            {
                case "pa":
                    WhitePawns ^= fromBit; // Odstrani s starega mesta
                    WhitePawns |= toBit;   // Dodaj na novo mesto
                    break;
                case "kn":
                    WhiteKnights ^= fromBit;
                    WhiteKnights |= toBit;
                    break;
                case "bi":
                    WhiteBishops ^= fromBit;
                    WhiteBishops |= toBit;
                    break;
                case "ro":
                    WhiteRooks ^= fromBit;
                    WhiteRooks |= toBit;
                    break;
                case "qu":
                    WhiteQueens ^= fromBit;
                    WhiteQueens |= toBit;
                    break;
                case "ki":
                    WhiteKing ^= fromBit;
                    WhiteKing |= toBit;
                    break;
            }

            WhitePieces &= ~fromBit;
            WhitePieces |= toBit;
        }
        else
        {
            switch (pieceName)
            {
                case "pa":
                    BlackPawns ^= fromBit; // Odstrani s starega mesta
                    BlackPawns |= toBit;   // Dodaj na novo mesto
                    break;
                case "kn":
                    BlackKnights ^= fromBit;
                    BlackKnights |= toBit;
                    break;
                case "bi":
                    BlackBishops ^= fromBit;
                    BlackBishops |= toBit;
                    break;
                case "ro":
                    BlackRooks ^= fromBit;
                    BlackRooks |= toBit;
                    break;
                case "qu":
                    BlackQueens ^= fromBit;
                    BlackQueens |= toBit;
                    break;
                case "ki":
                    BlackKing ^= fromBit;
                    BlackKing |= toBit;
                    break;
            }

            BlackPieces &= ~fromBit;
            BlackPieces |= toBit;
        }

        AllPieces &= ~fromBit;
        AllPieces |= toBit;

        return a;
    }
    private static string takePiece(ulong bit, bool isWhite)
    {
        if (!isWhite)
        {
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
            BlackPawns &= ~bit;
            BlackRooks &= ~bit;
            BlackKnights &= ~bit;
            BlackBishops &= ~bit;
            BlackQueens &= ~bit;
            BlackKing &= ~bit;
            BlackPieces &= ~bit;
        }

        AllPieces &= ~bit;

        return "captured";
    }

    public static ulong GetPawnMoves(int square, bool isWhite)
    {
        ulong bit = 1UL << square;

        ulong enemyPieces = isWhite ? BlackPieces : WhitePieces;
        ulong myPieces = isWhite ? WhitePieces : BlackPieces;

        ulong forwardMoves = 0;
        ulong captures = 0;

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

            // JEMANJE: Polje mora vsebovati NASPROTNIKA (& enemyPieces)
            captures |= (bit << 7) & notHFile & enemyPieces;
            captures |= (bit << 9) & notAFile & enemyPieces;
        }
        else // ?rni kmetje (obratno)
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
            captures |= (bit >> 7) & notAFile & enemyPieces;
            captures |= (bit >> 9) & notHFile & enemyPieces;
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
        int[] directions = { 7, 9 }; // top-left and top-right
        ulong moves = 0;

        foreach (int dir in directions)
        {
            ulong current = bit;

            // Slide up the board
            while (true)
            {
                // Check file boundaries before shift
                if ((dir == 9 && (current & notHFile) == 0) ||
                    (dir == 7 && (current & notAFile) == 0))
                    break;

                current = current << dir; // slide to next square
                if (current == 0) break;

                if ((current & myPieces) != 0) break;

                moves |= current;

                if ((current & enemyPieces) != 0) break;
            }
        }
        foreach (int dir in directions)
        {
            ulong current = bit;

            // Slide up the board
            while (true)
            {
                // Check file boundaries before shift
                if ((dir == 7 && (current & notHFile) == 0) ||
                    (dir == 9 && (current & notAFile) == 0))
                    break;

                current = current >> dir; // slide to next square
                if (current == 0) break;

                if ((current & myPieces) != 0) break;

                moves |= current;

                if ((current & enemyPieces) != 0) break;
            }
        }
        return moves;
    }
}
