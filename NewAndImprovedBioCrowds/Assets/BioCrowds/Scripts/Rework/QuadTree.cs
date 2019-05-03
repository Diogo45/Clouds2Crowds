using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

public struct Rectangle
{
    public int x;
    public int y;
    public int w;
    public int h;
}


public class QuadTree
{

 
    //size <- (x,y,w,h)
    private Rectangle size;
    //Grows downwards
    private int heigth;
    private bool schedule = false;
    public List<int3> myCells;

    private bool subDivided = false;
    private QuadTree TopRight = null;
    private QuadTree TopLeft = null;
    private QuadTree BottomRight = null;
    private QuadTree BottomLeft = null;
   
    public QuadTree(Rectangle size, int heigth)
    {
        this.size = size;
        if (this.IsSubdividable())
        {
            //TopLeft = new QuadTree(size, heigth + 1);
            //TopRight = new QuadTree(size, heigth + 1);
            //BottomLeft = new QuadTree(size, heigth + 1);
            //BottomRight = new QuadTree(size, heigth + 1);
        }
        else
        {
            Debug.Log("AAAAAAAAAAA");
            float densityToQtd = BioCrowds.Settings.experiment.MarkerDensity / Mathf.Pow(BioCrowds.Settings.experiment.markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            myCells = new List<int3>();
            this.getCells();
        }

        
    }

    private void getCells()
    {
        int3 cell = new int3 { x = (int)math.floor(size.x / 2.0f) * 2 + 1, y = 0, z = (int)math.floor(size.y / 2.0f) * 2 + 1 };
        Debug.Log(cell);
        for(int i = cell.x; i < size.x + size.w; i = i + 2)
        {
            for(int j = cell.z; i < size.y + size.h; j = j + 2)
            {
                myCells.Add(new int3 {x = i, y = 0, z = j});
                
            }

        }
    }

    public bool IsSubdividable()
    {
        return heigth < BioCrowds.Settings.instance.treeHeight;
    }


}
