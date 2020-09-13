using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace CmdTetris
{
    class Program
    {
        static void Main(string[] args)
        {
            Game tetris = new Game();

            tetris.Start();
        }
    }

    class Game
    {
        enum BlockType
        {
            I = 0,
            J = 1,
            L = 2,
            O = 3,
            S = 4,
            T = 5,
            Z = 6
        }

        struct Block
        {
            public ConsoleColor color;
            public bool isFilled;

            public int x;
            public int y;

            public int tileNum;
        }
        
        public static int Width = 10, Height = 20;
        readonly char square = '□', space = '　';

        string HighScoreFile = "HighScore";

        Block[][] map;
        Block[] FallingBlock;

        int frame;
        ConsoleKey input;

        int score;
        int line;

        bool gameOver;

        Random random;
        int nextBlock;
        int direction;
        BlockType type;

        #region Blocks
        static readonly Block[] I =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 2 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 5 },
            new Block {x = Width / 2 - 1, y = 2, tileNum = 8 },
            new Block {x = Width / 2 - 1, y = 3, tileNum = 11 },
        };
        static readonly Block[] J =
        {
            new Block {x = Width / 2,     y = 0, tileNum = 2 },
            new Block {x = Width / 2,     y = 1, tileNum = 5 },
            new Block {x = Width / 2,     y = 2, tileNum = 8 },
            new Block {x = Width / 2 - 1, y = 2, tileNum = 7 },
        };
        static readonly Block[] L =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 2 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 5 },
            new Block {x = Width / 2 - 1, y = 2, tileNum = 8 },
            new Block {x = Width / 2,     y = 2, tileNum = 9 },
        };
        static readonly Block[] O =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 1 },
            new Block {x = Width / 2,     y = 0, tileNum = 2 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 4 },
            new Block {x = Width / 2,     y = 1, tileNum = 5 },
        };
        static readonly Block[] S =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 2 },
            new Block {x = Width / 2,     y = 0, tileNum = 3 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 5 },
            new Block {x = Width / 2 - 2, y = 1, tileNum = 4 },
        };
        static readonly Block[] T =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 2 },
            new Block {x = Width / 2,     y = 1, tileNum = 6 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 5 },
            new Block {x = Width / 2 - 2, y = 1, tileNum = 4 },
        };
        static readonly Block[] Z =
        {
            new Block {x = Width / 2 - 1, y = 0, tileNum = 2 },
            new Block {x = Width / 2 - 2, y = 0, tileNum = 1 },
            new Block {x = Width / 2 - 1, y = 1, tileNum = 5 },
            new Block {x = Width / 2,     y = 1, tileNum = 6 },
        };
        static readonly Block[][] Blocks = { I, J, L, O, S, T, Z };
        static readonly BlockType[] types = { BlockType.I, BlockType.J, BlockType.L, BlockType.O, BlockType.S, BlockType.T, BlockType.Z };
        #endregion

        public void Start()
        {
            Init();

            new Thread(() => GetInput()).Start();

            new Thread(() => Cycle()).Start();
        }

        void Init()
        {
            Console.Title = "CmdTetris";
            Console.SetWindowSize(Width * 2 + 18, Height + 3);
            //Console.BufferHeight = Height + 3;
            //Console.BufferWidth = Width * 2 + 16;
            Console.CursorVisible = false;

            random = new Random();
            direction = 0;
            input = ConsoleKey.A;

            frame = 0;
            score = 0;
            line = 0;
            gameOver = false;
            
            map = new Block[Height][];
            for (int i = 0; i < Height; i++)
            {
                map[i] = new Block[Width];
                for (int o = 0; o < Width; o++)
                {
                    map[i][o] = new Block
                    {
                        color = ConsoleColor.White,
                        isFilled = false
                    };
                }
            }

            for (int i = 0; i < 4; i++)
            {
                I[i].color = ConsoleColor.Red;
                J[i].color = ConsoleColor.Yellow;
                L[i].color = ConsoleColor.DarkMagenta;
                O[i].color = ConsoleColor.Blue;
                S[i].color = ConsoleColor.Cyan;
                T[i].color = ConsoleColor.Green;
                Z[i].color = ConsoleColor.DarkYellow;
            }
            for (int i = 0; i < Blocks.Length; i++)
                for (int o = 0; o < Blocks[i].Length; o++)
                    Blocks[i][o].isFilled = true;

            nextBlock = random.Next(7);
            GenerateFallBlock();
        }

        void Cycle()
        {
            while (true)
            {
                Input();
                FallBlock();

                if(gameOver)
                {
                    Console.SetCursorPosition(7, 3);
                    Console.WriteLine("GAME OVER!");
                    
                    return;
                }

                Render();

                frame++;
                Thread.Sleep(10);
            }
        }

        void GetInput()
        {
            while (true)
            {
                input = Console.ReadKey(true).Key;
            }
        }

        void Input()
        {
            if (input == ConsoleKey.A) return;

            switch (input)
            {
                case ConsoleKey.LeftArrow:
                    foreach (Block b in FallingBlock)
                        if (b.x == 0 || map[b.y][b.x - 1].isFilled)
                            return;

                    MoveBlock(-1, 0);

                    break;
                case ConsoleKey.RightArrow:
                    foreach (Block b in FallingBlock)
                        if (b.x == Width - 1 || map[b.y][b.x + 1].isFilled)
                            return;

                    MoveBlock(1, 0);

                    break;
                case ConsoleKey.DownArrow:
                    foreach (Block b in FallingBlock)
                        if (b.y == Height - 1 || map[b.y + 1][b.x].isFilled)
                            return;

                    MoveBlock(0, 1);

                    break;
                case ConsoleKey.Z:
                    direction++;
                    if (direction == 5)
                        direction = 1;

                    if (type == BlockType.O) return;

                    Block[] tmpBlocks = (Block[])FallingBlock.Clone();

                    for (int i = 0; i < FallingBlock.Length; i++)
                    {
                        int x = 0, y = 0, n = 0;

                        switch (FallingBlock[i].tileNum)
                        {
                            case 1:
                                y = 2; n = 7;
                                break;
                            case 2:
                                x = -1; y = 1; n = 4;
                                break;
                            case 3:
                                x = -2; n = 1;
                                break;
                            case 4:
                                x = 1; y = 1; n = 8;
                                break;
                            case 6:
                                x = -1; y = -1; n = 2;
                                break;
                            case 7:
                                x = 2; n = 9;
                                break;
                            case 8:
                                x = 1; y = -1; n = 6;
                                break;
                            case 9:
                                y = -2; n = 3;
                                break;
                            case 10:
                                x = -2; y = 2; n = 12;
                                break;
                            case 11:
                                x = 2; y = -2; n = 10;
                                break;
                        }

                        if (FallingBlock[i].x + x < 0 || FallingBlock[i].x + x >= Width || FallingBlock[i].y + y < 0 || FallingBlock[i].y + y >= Height || map[FallingBlock[i].y + y][FallingBlock[i].x + x].isFilled)
                            return;

                        tmpBlocks[i].x += x;
                        tmpBlocks[i].y += y;
                        tmpBlocks[i].tileNum = n;
                    }

                    FallingBlock = tmpBlocks;

                    break;
            }

            input = ConsoleKey.A;
        }

        void FallBlock()
        {
            if (frame % 20 == 0)
            {
                foreach (Block b in FallingBlock)
                {
                    if (b.y == Height - 1 || map[b.y + 1][b.x].isFilled)
                    {
                        foreach (Block bb in FallingBlock)
                        {
                            map[bb.y][bb.x] = bb;
                        }
                        DeleteLine();
                        GenerateFallBlock();
                        return;
                    }
                }

                MoveBlock(0, 1);
            }
        }

        void GenerateFallBlock()
        {
            FallingBlock = (Block[])Blocks[nextBlock].Clone();
            type = types[nextBlock];

            nextBlock = random.Next(7);

            direction = 1;

            foreach(Block b in FallingBlock)
            {
                if(map[b.y][b.x].isFilled)
                {
                    gameOver = true;
                    return;
                }
            }
        }

        void DeleteLine()
        {
            int s = 0;

            for (int i = 0; i < Height; i++)
            {
                if (map[i].Count(b => b.isFilled) == Width)
                {
                    for (int o = i; o > 0; o--)
                    {
                        map[o] = (Block[])map[o - 1].Clone();
                    }

                    for (int o = 0; o < Width; o++)
                    {
                        map[0][o] = new Block
                        {
                            color = ConsoleColor.White,
                            isFilled = false
                        };
                    }

                    s++;
                }
            }

            score += 10 * s * s;
            line += s;

            if(score > GetHighScore())
                SetHighScore(score);
        }

        void MoveBlock(int x, int y)
        {
            for (int i = 0; i < FallingBlock.Length; i++)
            {
                FallingBlock[i].x += x;
                FallingBlock[i].y += y;
            }
        }

        void Render()
        {
            /*    PlayBox    */
            Console.SetCursorPosition(0, 0);

            PrintLn(new string(square, Width + 2), ConsoleColor.White);

            Block[][] renderMap = new Block[Height][];
            for (int i = 0; i < renderMap.Length; i++)
            {
                renderMap[i] = (Block[])map[i].Clone();
            }

            foreach (Block b in FallingBlock)
            {
                renderMap[b.y][b.x] = b;
            }

            for (int i = 0; i < Height; i++)
            {
                Print(square, ConsoleColor.White);

                for (int o = 0; o < Width; o++)
                {
                    Print(renderMap[i][o].isFilled ? square : space, renderMap[i][o].color);
                }

                PrintLn(square, ConsoleColor.White);
            }

            PrintLn(new string(square, Width + 2), ConsoleColor.White);

            /*    NextBlock    */
            Console.SetCursorPosition(Width * 2 + 6, 2);
            Print(square, ConsoleColor.White);
            Print("=NEXT=", ConsoleColor.White);
            Print(square, ConsoleColor.White);

            for (int i = 1; i <= 3; i++)
            {
                Console.SetCursorPosition(Width * 2 + 6, 2 + i);
                Print(square, ConsoleColor.White);

                for (int o = 1; o <= 3; o++)
                {
                    if (Blocks[nextBlock].Where(b => b.tileNum == i * 3 - 3 + o).Count() > 0)
                        Print(square, Blocks[nextBlock].First().color);
                    else
                        Print(space, ConsoleColor.White);
                }

                Print(square, ConsoleColor.White);
            }

            Console.SetCursorPosition(Width * 2 + 6, 6);

            Print(square, ConsoleColor.White);
            Print(space, ConsoleColor.White);

            if (types[nextBlock] == BlockType.I)
                Print(square, Blocks[nextBlock].First().color);
            else
                Print(space, ConsoleColor.White);

            Print(space, ConsoleColor.White);
            Print(square, ConsoleColor.White);

            Console.SetCursorPosition(Width * 2 + 6, 7);
            Print(new string(square, 5), ConsoleColor.White);

            /*    Score && Line    */
            PrintCenter("SCORE", 10, ConsoleColor.Red);
            PrintCenter(score.ToString(), 11, ConsoleColor.White);

            PrintCenter("HIGH SCORE", 13, ConsoleColor.Green);
            int hs = GetHighScore();
            PrintCenter(hs.ToString(), 14, score != 0 && score == hs ? ConsoleColor.Cyan : ConsoleColor.White);

            PrintCenter("LINE", 16, ConsoleColor.Blue);
            PrintCenter(line.ToString(), 17, ConsoleColor.White);

            //PrintCenter(frame.ToString(), 20, ConsoleColor.Green);
        }

        void Print(object content, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            //Console.BackgroundColor = color;
            Console.Write(content);
        }

        void PrintLn(object content, ConsoleColor color)
        {
            Print(content + "\n", color);
        }

        void PrintCenter(string content, int line, ConsoleColor color)
        {
            Console.SetCursorPosition(Width * 2 + 4 + (14 - content.Length) / 2, line);
            Print(content, color);
        }

        int GetHighScore()
        {
            if(!File.Exists(HighScoreFile))
            {
                SetHighScore(0);
                return 0;
            }

            return int.Parse(File.ReadAllText(HighScoreFile));
        }

        void SetHighScore(int s)
        {
            File.WriteAllText(HighScoreFile, s.ToString());
        }
    }
}