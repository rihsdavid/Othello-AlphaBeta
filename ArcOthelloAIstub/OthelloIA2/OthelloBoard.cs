using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IAStub
{

    // Tile states
    public enum TileState
    {
        EMPTY = -1,
        WHITE = 0,
        BLACK = 1
    }

    public class OthelloBoard : IPlayable.IPlayable
    {
        const int BOARDSIZE_X = 9;
        const int BOARDSIZE_Y = 7;
        readonly int[] coeff = {100, -20, 10, 5, 5, 5, 10, -20, 100,
                             -20, -50, -2, -2, -2, -2, -2, -50, -20,
                             10, -2, -1, -1, -1, -1, -1, -2, 10,
                             5, -2, -1, -1, -1, -1, -1, -2, 5,
                             10, -2, -1, -1, -1, -1, -1, -2, 10,
                             -20, -50, -2, -2, -2, -2, -2, -50, -20,
                             100, -20, 10, 5, 5, 5, 10, -20, 100
        };

        const int LATE_GAME_TRIGGER = 53;

        int[,] theBoard = new int[BOARDSIZE_X, BOARDSIZE_Y];
        int whiteScore = 0;
        int blackScore = 0;
        public bool GameFinish { get; set; }

        private Random rnd = new Random();

        public OthelloBoard()
        {
            initBoard();
        }

        public OthelloBoard(OthelloBoard other)
        {
            this.theBoard = (int[,]) other.GetBoard().Clone();
            this.computeScore();
        }


        public void DrawBoard()
        {
            Console.WriteLine("REFERENCE" + "\tBLACK [X]:" + blackScore + "\tWHITE [O]:" + whiteScore);
            Console.WriteLine("  A B C D E F G H I");
            for (int line = 0; line < BOARDSIZE_Y; line++)
            {
                Console.Write($"{(line + 1)}");
                for (int col = 0; col < BOARDSIZE_X; col++)
                {
                    Console.Write((theBoard[col, line] == (int)IAStub.TileState.EMPTY) ? " -" : (theBoard[col, line] == (int)IAStub.TileState.WHITE) ? " O" : " X");
                }
                Console.Write("\n");
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        /// <summary>
        /// Returns the board game as a 2D array of int
        /// with following values
        /// -1: empty
        ///  0: white
        ///  1: black
        /// </summary>
        /// <returns></returns>
        public int[,] GetBoard()
        {
            return (int[,])theBoard;
        }

        #region IPlayable
        public int GetWhiteScore() { return whiteScore; }
        public int GetBlackScore() { return blackScore; }
        public string GetName() { return "DR & TS"; }

        /// <summary>
        /// plays randomly amon the possible moves
        /// </summary>
        /// <param name="game"></param>
        /// <param name="level"></param>
        /// <param name="whiteTurn"></param>
        /// <returns>The move it will play, will return {-1,-1} if it has to PASS its turn (no move is possible)</returns>
        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            List<Tuple<int, int>> possibleMoves = GetPossibleMove(whiteTurn);
            if (possibleMoves.Count == 0)
                return new Tuple<int, int>(-1, -1);
            else
                return alphaBeta(possibleMoves, level, whiteTurn);
        }

        public bool PlayMove(int column, int line, bool isWhite)
        {
            //0. Verify if indices are valid
            if ((column < 0) || (column >= BOARDSIZE_X) || (line < 0) || (line >= BOARDSIZE_Y))
                return false;
            //1. Verify if it is playable
            if (IsPlayable(column, line, isWhite) == false)
                return false;

            //2. Create a list of directions {dx,dy,length} where tiles are flipped
            int c = column, l = line;
            bool playable = false;
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();

            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (theBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0))
                                   && (theBoard[c, l] == (int)opponent)) // pour éviter les trous
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (theBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                theBoard[column, line] = (int)ownColor;
                                catchDirections.Add(new Tuple<int, int, int>(dCol, dLine, counter));
                            }
                        }
                    }
                }
            }
            // 3. Flip ennemy tiles
            foreach (var v in catchDirections)
            {
                int counter = 0;
                l = line;
                c = column;
                while (counter++ < v.Item3)
                {
                    c += v.Item1;
                    l += v.Item2;
                    theBoard[c, l] = (int)ownColor;
                }
            }
            //Console.WriteLine("CATCH DIRECTIONS:" + catchDirections.Count);
            computeScore();
            return playable;
        }

        /// <summary>
        /// More convenient overload to verify if a move is possible
        /// </summary>
        /// <param name=""></param>
        /// <param name="isWhite"></param>
        /// <returns></returns>
        public bool IsPlayable(Tuple<int, int> move, bool isWhite)
        {
            return IsPlayable(move.Item1, move.Item2, isWhite);
        }

        public bool IsPlayable(int column, int line, bool isWhite)
        {
            //1. Verify if the tile is empty !
            if (theBoard[column, line] != (int)TileState.EMPTY)
                return false;
            //2. Verify if at least one adjacent tile has an opponent tile
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            int c = column, l = line;
            bool playable = false;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();
            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (theBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (theBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (theBoard[c, l] == (int)opponent)
                                continue;
                            else if (theBoard[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable;
        }
        #endregion

        /// <summary>
        /// Returns all the playable moves in a human readable way (e.g. "G3")
        /// </summary>
        /// <param name="v"></param>
        /// <param name="whiteTurn"></param>
        /// <returns></returns>
        public List<Tuple<char, int>> GetPossibleMoves(bool whiteTurn, bool show = false)
        {
            char[] colonnes = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            List<Tuple<char, int>> possibleMoves = new List<Tuple<char, int>>();
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                {
                    if (IsPlayable(i, j, whiteTurn))
                    {
                        possibleMoves.Add(new Tuple<char, int>(colonnes[i], j + 1));
                        if (show == true)
                            Console.Write((colonnes[i]).ToString() + (j + 1).ToString() + ", ");
                    }
                }
            return possibleMoves;
        }

        /// <summary>
        /// Returns all the playable moves in a computer readable way (e.g. "<3, 0>")
        /// </summary>
        /// <param name="v"></param>
        /// <param name="whiteTurn"></param>
        /// <returns></returns>
        public List<Tuple<int, int>> GetPossibleMove(bool whiteTurn, bool show = false)
        {
            char[] colonnes = "ABCDEFGHIJKL".ToCharArray();
            List<Tuple<int, int>> possibleMoves = new List<Tuple<int, int>>();
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                {
                    if (IsPlayable(i, j, whiteTurn))
                    {
                        possibleMoves.Add(new Tuple<int, int>(i, j));
                        if (show == true)
                            Console.Write((colonnes[i]).ToString() + (j + 1).ToString() + ", ");
                    }
                }
            return possibleMoves;
        }

        private void initBoard()
        {
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                    theBoard[i, j] = (int)TileState.EMPTY;

            theBoard[3, 3] = (int)TileState.WHITE;
            theBoard[4, 4] = (int)TileState.WHITE;
            theBoard[3, 4] = (int)TileState.BLACK;
            theBoard[4, 3] = (int)TileState.BLACK;

            computeScore();
        }

        private void computeScore()
        {
            whiteScore = 0;
            blackScore = 0;
            foreach (var v in theBoard)
            {
                if (v == (int)TileState.WHITE)
                    whiteScore++;
                else if (v == (int)TileState.BLACK)
                    blackScore++;
            }
            GameFinish = ((whiteScore == 0) || (blackScore == 0) ||
                        (whiteScore + blackScore == 63));
        }

        private Tuple<int, int> alphaBeta(List<Tuple<int, int>> possibleMoves, int level, bool whiteTurn)
        {
            Tuple<int, Tuple<int, int>> bestMove = alphaBetaMax(int.MaxValue, whiteTurn);
            return bestMove.Item2;
        }


        private Tuple<int, Tuple<int, int>> alphaBetaMax(int scoreParent, bool whiteTurn, int depth = 5)
        {
            List<Tuple<int, int>> possibleMove = this.GetPossibleMove(whiteTurn);

            if (depth == 0 || this.GameFinish == true || possibleMove.Count == 0)
            {
                return new Tuple<int, Tuple<int, int>>(evaluateGameState(whiteTurn), null);
            }

            int maxVal = int.MinValue;
            Tuple<int, int> maxOp = null;

            foreach (Tuple<int, int> move in possibleMove)
            {
                OthelloBoard tempOthelloBoard = new OthelloBoard(this);
                tempOthelloBoard.PlayMove(move.Item1, move.Item2, false);
                Tuple<int, Tuple<int,int>> score = tempOthelloBoard.alphaBetaMin(maxVal, !whiteTurn, depth - 1);
                if (score.Item1 > maxVal)
                {
                    maxVal = score.Item1;
                    maxOp = move;
                    if (maxVal > scoreParent)
                    {
                        break;
                    }
                }

            }
            return new Tuple<int, Tuple<int, int>>(maxVal, maxOp);
        }
        private Tuple<int, Tuple<int,int>> alphaBetaMin(int scoreParent, bool whiteTurn, int depth = 5)
        {
            List<Tuple<int, int>> possibleMove = this.GetPossibleMove(whiteTurn);

            if (depth == 0 || this.GameFinish == true || possibleMove.Count == 0)
            {
                return new Tuple<int, Tuple<int, int>>(evaluateGameState(!whiteTurn), null);
            }

            int minVal = int.MaxValue;
            Tuple<int, int> minOp = null;

            foreach (Tuple<int, int> move in possibleMove)
            {
                OthelloBoard tempOthelloBoard = new OthelloBoard(this);
                tempOthelloBoard.PlayMove(move.Item1, move.Item2, false);
                Tuple<int, Tuple<int,int>> score = tempOthelloBoard.alphaBetaMax(minVal, !whiteTurn, depth - 1);
                if (score.Item1 < minVal)
                {
                    minVal = score.Item1;
                    minOp = move;
                    if (minVal < scoreParent)
                    {
                        break;
                    }
                }

            }
            return new Tuple<int, Tuple<int, int>>(minVal, minOp);
        }

        private int evaluateGameState(bool isWhite)
        {
            int whiteScore = this.whiteScore;
            int blackScore = this.blackScore;

            int turn = whiteScore + blackScore - 4;
            
            if (turn < LATE_GAME_TRIGGER)
            {
                for(int i=0; i<BOARDSIZE_Y; i++)
                {
                    for(int j=0; j<BOARDSIZE_X; j++)
                    {
                        if (theBoard[j,i] == 1)
                            blackScore += coeff[i * BOARDSIZE_X + j];
                        else if(theBoard[j,i] == 0)
                            whiteScore += coeff[i * BOARDSIZE_X + j];
                    }
                }
                
            }
            
            if (isWhite)
                return whiteScore - blackScore;
            else
                return blackScore - whiteScore;
        }
    }

}