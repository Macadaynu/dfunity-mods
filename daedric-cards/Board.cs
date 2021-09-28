using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    const int boardSize = 5;

    public GameObject cellPrefab;

    public static Cell[,] allCells = new Cell[boardSize, boardSize];

    private void Awake()
    {
        Create();
    }

    public void Create()
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                // Create the cell
                var cell = Instantiate(cellPrefab, transform);

                // Set position of the cell
                var rectTransform = cell.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2((x * 80) + 250, (y * 80) + 70);

                // Setup the cell
                allCells[x, y] = cell.GetComponent<Cell>();
                allCells[x, y].Setup(new Vector2Int(x, y), this, y <= 1);
            }
        }
    }

    public static void DisplayValidDropZone()
    {
        foreach (var cell in allCells)
        {
            if (cell.isValidDropZone && !cell.isOccupied)
            {
                cell.GetComponent<Image>().color = Color.green;
            }
        }
    }

    public static void ClearDisplayHelpers()
    {
        foreach (var cell in allCells)
        {
            cell.GetComponent<Image>().color = Color.white;
        }
    }

    public static void DisplayValidMoves(Cell cell)
    {
        var validCellMoves = GetValidCellMoves(cell);

        foreach (var validCell in validCellMoves)
        {
            validCell.GetComponent<Image>().color = Color.green;
        }
    }

    public static List<Cell> GetValidCellMoves(Cell cell)
    {
        var cellBoardPosition = cell.boardPosition;

        var validCellMoves = new List<Cell>();

        if (cellBoardPosition.x < 4)
        {
            validCellMoves.Add(allCells[cellBoardPosition.x + 1, cellBoardPosition.y]);
        }
        if (cellBoardPosition.x > 0)
        {
            validCellMoves.Add(allCells[cellBoardPosition.x - 1, cellBoardPosition.y]);
        }
        if (cellBoardPosition.y < 4)
        {
            validCellMoves.Add(allCells[cellBoardPosition.x, cellBoardPosition.y + 1]);
        }
        if (cellBoardPosition.y > 0)
        {
            validCellMoves.Add(allCells[cellBoardPosition.x, cellBoardPosition.y - 1]);
        }

        return validCellMoves;
    }

    public static double GetDistanceBetweenCells(Cell originCell, Cell targetCell)
    {
        return Math.Sqrt(Math.Pow((targetCell.boardPosition.x - originCell.boardPosition.x), 2)
            + Math.Pow((targetCell.boardPosition.y - originCell.boardPosition.y), 2));
    }

    public static void ApplyCanMoveOrAttackGlow()
    {
        var allMinionsOnBoard = DaedricCardsMod.boardCanvas.transform.GetComponentsInChildren(typeof(Minion), false);
        foreach (Minion minion in allMinionsOnBoard)
        {
            if (minion.isPlayerMinion && !minion.isDaedricPrince)
            {
                minion.ApplyCanMoveOrAttackGlow(true);
            }
        }
    }
}
