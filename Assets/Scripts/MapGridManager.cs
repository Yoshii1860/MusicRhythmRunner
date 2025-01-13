using UnityEngine;

public class MapGridManager : MonoBehaviour
{
    [System.Serializable]
    public class MapData
    {
        public bool IsOccupied;
        public bool IsCloseToObstacle;
        public bool IsJump;
        public string Type;
        public bool IsSet;
        public Vector2Int Position;

        public MapData(int x, int y, bool isOccupied = false, string type = "", bool isCloseToObstacle = false, bool isJump = false, bool isSet = false)
        {
            Position = new Vector2Int(x, y);
            IsOccupied = isOccupied;
            IsCloseToObstacle = isCloseToObstacle;
            IsJump = isJump;
            Type = type;
            IsSet = isSet;
        }
    }

    public MapData[,] Grid;
    public int CloseToObstacleMinDistance = -3;
    public int CloseToObstacleMaxDistance = 4;
    public int OccupationMinDistance = -1;
    public int OccupationMaxDistance = 2;
    public bool DebugMode = false;
    private int _rows;
    private int _columns;

    // Initialize the grid
    public void InitializeGrid(int rows, int columns)
    {
        _rows = rows;
        _columns = columns;
        Grid = new MapData[rows, columns];
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                Grid[x, y] = new MapData(x, y);
            }
        }
    }

    // Get data at a specific grid position
    public MapData GetMapData(int row, int column)
    {
        if (row >= 0 && column >= 0 && row < _rows && column < _columns)
        {
            return Grid[row, column];
        }
        Debug.LogWarning("Requested grid position is out of bounds.");
        return null;
    }

    // Set data at a specific grid position
    public void SetMapData(int row, int column, bool isOccupied, string type = "", bool isCloseToObstacle = false, bool isJump = false)
    {
        if (row >= 0 && column >= 0 && row < _rows && column < _columns)
        {
            if (!Grid[row, column].IsSet) 
                Grid[row, column].IsOccupied = isOccupied;
            Grid[row, column].Type = type;
            Grid[row, column].IsCloseToObstacle = isCloseToObstacle;
            Grid[row, column].IsJump = isJump;
            Grid[row, column].IsSet = true;
        }
        else
        {
            Debug.LogWarning("Attempted to set data out of bounds.");
        }
    }

    // Debug: Print the grid status
    public void PrintGridStatus()
    {
        for (int x = 0; x < _rows; x++)
        {
            string rowStatus = $"Row {x}: ";
            for (int y = 0; y < _columns; y++)
            {
                rowStatus += Grid[x, y].IsOccupied ? "[X] " : "[ ] ";
            }
            Debug.Log(rowStatus);
        }
    }

    // Add obstacle to a grid cell
    public void AddObstacle(int row, int column)
    {
        for (int x = row + OccupationMinDistance; x < row + OccupationMaxDistance; x++)
        {
            if (x >= 0 && x < _rows)
            {
                SetMapData(x, column, true, "Move Obstacle");
            }
        }

        for (int x = row + CloseToObstacleMinDistance; x < row + CloseToObstacleMaxDistance; x++)
        {
            if (x >= 0 && x < _rows)
            {
                SetCloseToObstacle(x, column);
            }
        }

        Debug.Log($"Added obstacle at ({row}, {column}).");
    }

    // Add close to obstacle to a grid cell
    private void SetCloseToObstacle(int row, int column)
    {
        SetMapData(row, column, false, "Move Obstacle", true);
    }

    // Add jump obstacle to a grid cell
    public void AddJumpObstacle(int row)
    {
        for (int i = 0; i < _columns; i++)
        {
            SetMapData(row, i, true, "Jump Obstacle", false, true);
        }
        for (int x = row + CloseToObstacleMinDistance; x < row + CloseToObstacleMaxDistance; x++)
        {
            for (int y = 0; y < _columns; y++)
            {
                if (x >= 0 && y >= 0 && x < _rows && y < _columns)
                {
                    SetCloseToJumpObstacle(x, y);
                }
            }
        }
        Debug.Log($"Added jump obstacle at ({row}).");
    }

    // Add close to jump obstacle to a grid cell
    private void SetCloseToJumpObstacle(int row, int column)
    {
        SetMapData(row, column, false, "Jump Obstacle", true, true);
    }

    // Clear a grid cell
    public void ClearCell(int row, int column)
    {
        SetMapData(row, column, false);
        Debug.Log($"Cleared cell at ({row}, {column}).");
    }

    void Update()
    {
        if (DebugMode)
        {
            PrintGridStatus();
            DebugMode = false;
        }
    }
}
