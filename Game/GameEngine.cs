static partial class GameEngine
{
    internal const int Floor = 0, Wall = 1, Diamond = 2, Exit = 3;
    internal const int Player = 4, Creeper = 5, Pickaxe = 6;
    internal const int Spark = 7, Burst = 8;
    internal const int FinalLevel = 3;
    private static readonly (int Row, int Col)[] Directions =
        { (-1, 0), (0, -1), (1, 0), (0, 1) };

    internal static bool InBounds(int[,] board, int row, int col) =>
        row >= 0 && row < board.GetLength(0) && col >= 0 && col < board.GetLength(1);

    // Starter behaviour for the challenge task. The creeper moves directly
    // towards the player and can get stuck behind walls.
    internal static bool TryGetDirectStep(int[,] board, int fromRow, int fromCol,
        int playerRow, int playerCol, out int nextRow, out int nextCol)
    {
        int distance = Math.Abs(fromRow - playerRow) + Math.Abs(fromCol - playerCol);
        foreach (var (dr, dc) in Directions)
        {
            int row = fromRow + dr;
            int col = fromCol + dc;
            if (InBounds(board, row, col) && board[row, col] != Wall &&
                Math.Abs(row - playerRow) + Math.Abs(col - playerCol) < distance)
            {
                nextRow = row;
                nextCol = col;
                return true;
            }
        }
        nextRow = fromRow;
        nextCol = fromCol;
        return false;
    }

    internal sealed class Round
    {
        private readonly int[,] map;
        private readonly int level;
        private int patrolDirection;
        private int playerMoves;

        internal int[,] Board { get; private set; } = null!;
        internal int PlayerRow { get; private set; }
        internal int PlayerCol { get; private set; }
        internal int CreeperRow { get; private set; }
        internal int CreeperCol { get; private set; }
        internal int Diamonds { get; private set; }
        internal int PickaxeUses { get; private set; }
        internal bool Finished { get; private set; }
        internal bool Won { get; private set; }
        internal string Message { get; set; } = "";

        internal Round(int[,] map, int level)
        {
            this.map = map;
            this.level = level;
            Reset();
        }

        internal void Reset()
        {
            Board = (int[,])map.Clone();
            Diamonds = 0;
            PickaxeUses = 0;
            playerMoves = 0;
            Finished = false;
            Won = false;
            Message = "Collect all diamonds, then reach the exit.";

            for (int row = 0; row < Board.GetLength(0); row++)
            {
                for (int col = 0; col < Board.GetLength(1); col++)
                {
                    switch (Board[row, col])
                    {
                        case Player:
                            (PlayerRow, PlayerCol) = (row, col);
                            Board[row, col] = Floor;
                            break;
                        case Creeper:
                            (CreeperRow, CreeperCol) = (row, col);
                            Board[row, col] = Floor;
                            break;
                        case Diamond:
                            Diamonds++;
                            break;
                    }
                }
            }
            patrolDirection = level == 1 ? ChoosePatrolDirection() : -1;
        }

        internal int Move(int deltaRow, int deltaCol)
        {
            int row = PlayerRow + deltaRow;
            int col = PlayerCol + deltaCol;

            if (!GameTasks.CanMoveTo(Board, row, col))
            {
                // A failed action never advances the creeper.
                if (!InBounds(Board, row, col) || Board[row, col] != Wall ||
                    !GameTasks.TryBreakWall(Board, row, col, PickaxeUses))
                {
                    Message = "That move is blocked.";
                    return 0;
                }

                PickaxeUses--;
            }

            playerMoves++;
            (PlayerRow, PlayerCol) = (row, col);
            if (Caught())
                return Lose();

            int effect = 0;
            Message = "Keep going.";
            switch (Board[row, col])
            {
                case Diamond:
                    Board[row, col] = Floor;
                    Diamonds--;
                    effect = Spark;
                    Message = Diamonds == 0 ? "Exit unlocked!" : "Diamond collected.";
                    break;
                case Pickaxe:
                    Board[row, col] = Floor;
                    PickaxeUses++;
                    effect = Spark;
                    Message = "Pickaxe collected. Walk into a wall to use it.";
                    break;
                case Exit when Diamonds == 0:
                    Finished = true;
                    Won = true;
                    Message = "Exit reached!";
                    return Spark;
                case Exit:
                    Message = "Collect all diamonds to unlock the exit.";
                    break;
            }

            int burstEvery = level == FinalLevel ? 2 : 4;
            int creeperSteps = level > 1 && playerMoves % burstEvery == 0 ? 2 : 1;
            if (level > 1 && Message == "Keep going." && (playerMoves + 1) % burstEvery == 0)
                Message = "Heads up! Creeper lunges next turn.";
            for (int step = 0; step < creeperSteps; step++)
            {
                MoveCreeper();
                if (Caught())
                    return Lose();
            }
            return effect;
        }

        private bool Caught() => PlayerRow == CreeperRow && PlayerCol == CreeperCol;

        private int Lose()
        {
            Finished = true;
            Message = "The creeper caught you!";
            return Burst;
        }

        private void MoveCreeper()
        {
            if (level > 1)
            {
                if (GameTasks.TryGetCreeperStep(Board, CreeperRow, CreeperCol,
                    PlayerRow, PlayerCol, out int row, out int col) &&
                    Math.Abs(row - CreeperRow) + Math.Abs(col - CreeperCol) == 1 &&
                    InBounds(Board, row, col) && Board[row, col] != Wall)
                    (CreeperRow, CreeperCol) = (row, col);

                return;
            }

            if (patrolDirection < 0 || !CanPatrol(patrolDirection))
                patrolDirection = ChoosePatrolDirection();
            if (patrolDirection < 0)
                return;

            var (dr, dc) = Directions[patrolDirection];
            CreeperRow += dr;
            CreeperCol += dc;
        }

        private int ChoosePatrolDirection()
        {
            Span<int> available = stackalloc int[Directions.Length];
            int count = 0;
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                if (CanPatrol(direction))
                    available[count++] = direction;
            }
            return count == 0 ? -1 : available[Random.Shared.Next(count)];
        }

        private bool CanPatrol(int direction)
        {
            var (dr, dc) = Directions[direction];
            int row = CreeperRow + dr;
            int col = CreeperCol + dc;
            return InBounds(Board, row, col) && Board[row, col] != Wall;
        }
    }
}
