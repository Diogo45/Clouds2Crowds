using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;

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

    static Rect domain;
    static int rows;
    static int cols;
    
    public static int Position2CellID(float3 position)
    {
        var cell = PositionToGridCell(position);

        return GridCell2CellID(new int2(cell.x, cell.y));
    }
    
    public static int GridCell2CellID(int2 gridCell)
    {
        //Debug.Log("x y " + gridCell + " id " + (gridCell.x * cols + gridCell.y));

        return gridCell.x * rows + gridCell.y;
    }
    
    public static int2 PositionToGridCell(float3 position)
    {
        var x = (int)((position.x - domain.minx) / Width);
        var y = (int)((position.y - domain.miny) / Width);
        //Debug.Log("x y " + x + " " + y + " pos " + position);
        return new int2(x, y);
    }
    
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
                if(!(i < domain.minx || i > domain.maxx ||
                   j < domain.miny || j > domain.maxy))
                    auxList.Add(GridCell2CellID(new int2(i, j)));

        return auxList.ToArray();
    }
    public static int CellQuantity { get; private set; } = 0;

    public static float Width { get; set; }

    public static void SetDomain(float minX, float minY, float maxX, float maxY)
    {
        domain = new Rect() { maxx = maxX, minx = minX, maxy = maxY, miny = minY };

        rows = (int)((maxY - minY) / Width);
        cols = (int)((maxX - minX) / Width);

        CellQuantity = rows * cols;

        Debug.Log("rows : " + rows + " cols : " + cols);
    }



}
