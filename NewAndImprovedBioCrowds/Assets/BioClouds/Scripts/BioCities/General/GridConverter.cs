using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;


/// <summary>
/// Grid Converter utility class.
/// Defines a mapping of a restricted spatial domain and discretized cells.
/// Computes the world positions into cell cell index and vice-versa.
/// </summary>
public class GridConverter
{
    struct Rect
    {
        public float minx;
        public float miny;
        public float maxx;
        public float maxy;

        public override string ToString()
        {
            return "minX=" + minx + " " + " minY=" + miny + " maxX=" + maxx + " maxY=" + maxy;
        }
    }


    public static int CellQuantity { get; private set; } = 0;

    public static float Width { get; set; }

    static Rect domain;
    static int rows;
    public static int Rows { get { return rows; } } 

    static int cols;
    public static int Cols { get { return cols; } }

    /// <summary>
    /// Returns the Cell ID of the cell which contains this position.
    /// </summary>
    /// <param name="position">The position to check. </param> 
    /// <returns>The ID for the Cell which contains position</returns> 
    public static int Position2CellID(float3 position)
    {
        var cell = PositionToGridCell(position);

        return GridCell2CellID(new int2(cell.x, cell.y));
    }
    
    /// <summary>
    /// Returns the Cell ID of a cell in the xth column and yth row of the cell region.
    /// </summary>
    /// <param name="gridCell">The column and row of the cell.</param>
    /// <returns>The Cell ID for the cell.</returns>
    public static int GridCell2CellID(int2 gridCell)
    {
        return gridCell.x  * rows + gridCell.y;
    }
    
    /// <summary>
    /// Returns the marker space row and column of a given world space position.
    /// </summary>
    /// <param name="position">a world space coordinate</param>
    /// <returns>The x column  and y row of the marker space grid.</returns>
    public static int2 PositionToGridCell(float3 position)
    {
        var x = (int)((position.x - domain.minx) / Width);
        var y = (int)((position.y - domain.miny) / Width);
        //Debug.Log("x y " + x + " " + y + " pos " + position);
        return new int2(x, y);
    }

    public static float3 GridCell2EstimatePosition(int2 gridCell)
    {

        float x = gridCell.x * Width + domain.minx;
        float y = gridCell.y * Width + domain.miny;


        return new float3(x, y, 0);

    }
    
    /// <summary>
    /// Returns the quantity of cells inside the radius centered in the world space position.
    /// </summary>
    /// <param name="position">a world space position</param>
    /// <param name="radius">a radius.</param>
    /// <returns>the quantity of marker cells inside the radius.</returns>
    public static int QuantityInRadius(float3 position, float radius)
    {
        var auxRadius = math.ceil(radius);
        var startPos = new float3(position.x - auxRadius, position.y - auxRadius, position.z);
        var endPos = new float3(position.x + auxRadius, position.y + auxRadius, position.z);

        var startCell = PositionToGridCell(startPos);
        var endCell = PositionToGridCell(endPos);
        int x = endCell.x - startCell.x;
        int y = endCell.y - startCell.y;

        return x * y;

    }

    /// <summary>
    /// Returns the ID of each cell inside a radius in world space position.
    /// </summary>
    /// <param name="position">a position in world space.</param>
    /// <param name="radius">a radius</param>
    /// <returns>The IDs of marker Cells inside this circle.</returns>
    public static int[] RadiusInGrid(float3 position, float radius)
    {
        var auxList = new List<int>();
        var auxRadius = math.ceil(radius);

        var startPos = new float3(position.x - auxRadius, position.y - auxRadius, position.z);
        var endPos = new float3(position.x + auxRadius, position.y + auxRadius, position.z);

        var startCell = PositionToGridCell(startPos);
        var endCell = PositionToGridCell(endPos);

        for (int i = startCell.x; i <= endCell.x; i++)
            for (int j = startCell.y; j <= endCell.y; j++)
                if(!(i < 0 || i > rows ||
                   j < 0 || j > cols ) &&
                   math.distance(position, GridCell2EstimatePosition(new int2(i,j))) < radius)
                    auxList.Add(GridCell2CellID(new int2(i, j)));

        return auxList.ToArray();
    }


    //By breshenham's line algorithm
    public static int[] LineInGrid(float3 pos1, float3 pos2)
    {
        var auxSet = new HashSet<int>();

        var cell1 = GridConverter.PositionToGridCell(pos1);
        var cell2 = GridConverter.PositionToGridCell(pos2);

        int x0 = cell1.x;
        int y0 = cell1.y;

        int x1 = cell2.x;
        int y1 = cell2.y;


        int dx = (int)math.abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = (int)-math.abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;

        int err = dx + dy;

        while (true)
        {
            auxSet.Add(GridConverter.GridCell2CellID(new int2(x0, y0)));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;

            if(e2 >= dy)
            {
                err += dy; 
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            } 

        }

        int[] list = new int[auxSet.Count];
        auxSet.CopyTo(list);


        return list;
    }

    /// <summary>
    /// Sets the simulation restricted Domain.
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="minY"></param>
    /// <param name="maxX"></param>
    /// <param name="maxY"></param>
    public static void SetDomain(float minX, float minY, float maxX, float maxY)
    {
        domain = new Rect() { maxx = maxX, minx = minX, maxy = maxY, miny = minY };

        rows = (int)((maxY - minY) / Width);
        cols = (int)((maxX - minX) / Width);

        CellQuantity = rows * cols;

        Debug.Log("rows : " + rows + " cols : " + cols);
    }


    


}
