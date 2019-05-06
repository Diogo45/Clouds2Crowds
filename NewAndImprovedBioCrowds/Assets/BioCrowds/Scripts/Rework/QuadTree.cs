using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

public struct Rectangle
{
    public float x;
    public float y;
    public float w;
    public float h;

    public override string ToString()
    {
        return "(" + x + " " + y + ") " + w + ", " + h;
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
            Rectangle bl = new Rectangle
            {
                x = size.x,
                y = size.y,
                h = math.floor((size.h / 2) / 2) * 2f,
                w = math.floor((size.w / 2f) / 2) * 2f
            };
            Rectangle br = new Rectangle
            {
                x = size.x + bl.w,
                y = size.y,
                h = math.floor((size.h / 2f) / 2) * 2f,
                w = math.ceil((size.w / 2f) / 2) * 2f
            };

            Rectangle tl = new Rectangle
            {
                x = size.x,
                y = size.y + bl.h,
                h = math.ceil((size.h / 2) / 2) * 2f,
                w = math.floor((size.w / 2f) / 2) * 2f
            };
            Rectangle tr = new Rectangle
            {
                x = size.x + tl.w,
                y = size.y + br.h,
                h = math.ceil((size.h / 2f) / 2) * 2f,
                w = math.ceil((size.w / 2f) / 2) * 2
            };

            int nh = heigth + 1;

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
        //int3 cell = new int3 { x = size.x, y = 0, z = size.y };
        //Debug.Log(cell + " " + heigth);
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

    public void Draw(int max)
    {
        Vector3 x1, x2, x3, x4;
        x1 = new Vector3( size.x, 0, size.y );
        x2 = new Vector3( size.x + size.w, 0, size.y );
        x3 = new Vector3( size.x, 0, size.y + size.h);
        x4 = new Vector3( size.x + size.w , 0, size.y + size.h);

        Debug.DrawLine(x1, x2);
        Debug.DrawLine(x1, x3);
        Debug.DrawLine(x2, x4);
        Debug.DrawLine(x3, x4);

        if(heigth < max)
        {
            TopRight.Draw(max);
            TopLeft.Draw(max);
            BottomLeft.Draw(max);
            BottomRight.Draw(max);
        }

    }

}
