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

    public override string ToString()
    {
        return "(" + x + " " + y + ") " + w + ", " + h;
    }

}

public class ShowTree : MonoBehaviour
{
    public QuadTree qt;



    private void Update()
    {
        
    }
}


public class QuadTree
{

 
    //size <- (x,y,w,h)
    public Rectangle size;
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
        this.heigth = heigth;
        if (this.IsSubdividable())
        {
            Rectangle tl = new Rectangle
            {
                x = size.x,
                y = size.y + size.h / 2,
                h = size.h / 2,
                w = size.w / 2
            };
            Rectangle tr = new Rectangle
            {
                x = size.x + size.w/2,
                y = size.y + size.h / 2,
                h = size.h / 2,
                w = size.w / 2
            };
            Rectangle bl = new Rectangle
            {
                x = size.x,
                y = size.y,
                h = size.h / 2,
                w = size.w / 2
            };
            Rectangle br = new Rectangle
            {
                x = size.x + size.w/2,
                y = size.y,
                h = size.h / 2,
                w = size.w / 2
            };
            int nh = heigth + 1;
            //Debug.Log("TopLeft: " + tl);
            //Debug.Log("TopRight: " + tr);
            //Debug.Log("BottomLeft: " + bl);
            //Debug.Log("BottomRight: " + br);
            TopLeft = new QuadTree(tl, nh);
            TopRight = new QuadTree(tr, nh);
            BottomLeft = new QuadTree(bl, nh);
            BottomRight = new QuadTree(br, nh);

        }
        else
        {
            float densityToQtd = BioCrowds.Settings.experiment.MarkerDensity / Mathf.Pow(BioCrowds.Settings.experiment.markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            myCells = new List<int3>();
            this.getCells();
        }

        
    }

    private void getCells()
    {
        int3 cell = new int3 { x = (int)math.floor(size.x / 2.0f) * 2 + 1, y = 0, z = (int)math.floor(size.y / 2.0f) * 2 + 1 };
        Debug.Log(cell + " " + heigth);
        for(int i = cell.x; i < size.w + size.x; i = i + 2)
        {
            for(int j = cell.z; j < size.h + size.y; j = j + 2)
            {
                myCells.Add(new int3(i, 0, j));
                
            }

        }
        //Debug.Log(myCells.Count);
    }

    public bool IsSubdividable()
    {
        return this.heigth < BioCrowds.Settings.instance.treeHeight;
    }


}
