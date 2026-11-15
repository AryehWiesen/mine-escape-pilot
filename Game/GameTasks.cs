static class GameTasks
{
    // 0 רצפה, 1 קיר, 2 יהלום, 3 יציאה
    // 4 שחקן, 5 קריפר, 6 מכוש
    private const int Floor = GameEngine.Floor;
    private const int Wall = GameEngine.Wall;

    // TODO 1
    // בדקו שהשורה והעמודה בתוך הלוח. רק אחר כך בדקו שהיעד אינו קיר.
    internal static bool CanMoveTo(int[,] board, int row, int col)
    {
        return false;
    }

    // TODO 2
    // אם נותר שימוש במכוש והיעד הוא קיר בתוך הלוח, החליפו אותו ברצפה.
    // המשחק מפחית שימוש אחד לאחר שבירה מוצלחת.
    internal static bool TryBreakWall(int[,] board, int row, int col, int uses)
    {
        return false;
    }

    // משימת אתגר — מצאו צעד אחד במסלול הקצר ביותר מהקריפר אל השחקן.
    internal static bool TryGetCreeperStep(int[,] board, int fromRow, int fromCol,
        int playerRow, int playerCol, out int nextRow, out int nextCol)
    {
        nextRow = fromRow;
        nextCol = fromCol;
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int[,] distances = new int[rows, cols];
        int[] rowOffsets = { -1, 0, 1, 0 };
        int[] colOffsets = { 0, -1, 0, 1 };

        // מחפשים מהשחקן החוצה, שכבה אחר שכבה.
        // אפס מסמן תא שטרם נמצא. הערך בתא שנמצא גדול באחד מהמרחק האמיתי.
        // התור מיוצג בשני מערכים חד־ממדיים. באותו מקום נשמרים שורה ועמודה.
        // קוראים מראש התור ומוסיפים לסופו. תחילה התור מכיל רק את תא השחקן.
        int[] queueRows = new int[rows * cols];
        int[] queueCols = new int[rows * cols];
        // המקום של התא הבא שמחכה לבדיקה.
        int head = 0;
        // המקום הפנוי הבא להוספה לתור.
        int tail = 1;
        queueRows[0] = playerRow;
        queueCols[0] = playerCol;
        // המרחק של השחקן מעצמו הוא אפס, ולכן בתא שלו כותבים אחת.
        distances[playerRow, playerCol] = 1;

        while (head < tail && distances[fromRow, fromCol] == 0)
        {
            int row = queueRows[head];
            int col = queueCols[head];
            head++;

            for (int direction = 0; direction < 4; direction++)
            {
                int neighborRow = row + rowOffsets[direction];
                int neighborCol = col + colOffsets[direction];
                if (!CanMoveTo(board, neighborRow, neighborCol) ||
                    distances[neighborRow, neighborCol] != 0)
                    continue;

                // TODO 3.1
                // שמרו בתא של השכן את הערך שבתא הנוכחי ועוד צעד אחד.
                // הסימון לפני ההוספה לתור מבטיח שכל תא ייכנס לתור פעם אחת.
                distances[neighborRow, neighborCol] = distances[row, col] + 0;
                queueRows[tail] = neighborRow;
                queueCols[tail] = neighborCol;
                tail++;
            }
        }

        int distance = distances[fromRow, fromCol];
        if (distance == 0 || (fromRow == playerRow && fromCol == playerCol))
            return false;

        // עד שתשלימו את שתי השורות, המשחק משתמש בצעד ישיר כשהחיפוש אינו נותן צעד.
        if (distance == 1)
            return GameEngine.TryGetDirectStep(board, fromRow, fromCol,
                playerRow, playerCol, out nextRow, out nextCol);

        for (int direction = 0; direction < 4; direction++)
        {
            int row = fromRow + rowOffsets[direction];
            int col = fromCol + colOffsets[direction];
            // TODO 3.2
            // בדקו קודם שאפשר לעבור לשכן. רק אז קראו את הערך שלו.
            // לשכן הנבחר צריך להיות ערך קטן באחד מהערך בתא הקריפר.
            bool isCloser = false;
            if (isCloser)
            {
                nextRow = row;
                nextCol = col;
                return true;
            }
        }

        return GameEngine.TryGetDirectStep(board, fromRow, fromCol,
            playerRow, playerCol, out nextRow, out nextCol);
    }
}
