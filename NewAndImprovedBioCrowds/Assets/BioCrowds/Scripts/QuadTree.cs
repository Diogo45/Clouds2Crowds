using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using System.Linq;

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
    private int heigth;
    public bool schedule = false;
    public List<int3> myCells;
    public static int LastId = 0;
    public int Id = 0;

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
            this.subDivided = true;
        }
        else
        {
            float densityToQtd = BioCrowds.Settings.experiment.MarkerDensity / Mathf.Pow(BioCrowds.Settings.experiment.markerRadius, 2f);
            int qtdMarkers = Mathf.FloorToInt(densityToQtd);
            myCells = new List<int3>();
            this.getCells();
            this.subDivided = false;
            this.Id = LastId;
            LastId++;
        }

        
    }

    private void getCells()
    {
        int3 cell = new int3 { x = (int)math.floor(size.x / 2.0f) * 2 + 1, y = 0, z = (int)math.floor(size.y / 2.0f) * 2 + 1 };

        for(int i = cell.x; i < size.w + size.x; i = i + 2)
        {
            for(int j = cell.z; j < size.h + size.y; j = j + 2)
            {
                myCells.Add(new int3(i, 0, j));
                
            }

        }
    }

    public void PrintQuadrants()
    {
        String print = "";

    }

    public List<int> GetQuadrants()
    {
        
        List<int> res = new List<int>();
        if (!this.subDivided)
        {
            res.Add(Id);
            return res;
        }
        else{
            if(this.subDivided)
            {
                if (TopLeft.schedule)
                {
                    res.AddRange(TopLeft.GetQuadrants());
                }
                if (TopRight.schedule)
                {
                    res.AddRange(TopRight.GetQuadrants());
                }
                if (BottomLeft.schedule)
                {
                    res.AddRange(BottomLeft.GetQuadrants());
                }
                if (BottomRight.schedule)
                {
                    res.AddRange(BottomRight.GetQuadrants());
                }
            }
        }


        return res;

    }

    public void Insert(int3 cell)
    {
        int x = cell.x;
        int z = cell.z;

        this.schedule = true;

        if (subDivided)
        {
            if(x < BottomRight.size.x)
            {
                //LeftSide
                if(z < TopRight.size.y)
                {
                    //BottomLeft
                    BottomLeft.Insert(cell);
                }
                else
                {
                    //TopLeft
                    TopLeft.Insert(cell);
                }

            }
            else
            {
                //RigthSide
                if (z < TopRight.size.y)
                {
                    //BottomRigth
                    BottomRight.Insert(cell);
                }
                else
                {
                    //TopRigth
                    TopRight.Insert(cell);
                }
            }
        }
        else
        {
            this.schedule = true;
        }

    }

    public bool IsSubdividable()
    {
        return this.heigth < BioCrowds.Settings.instance.treeHeight;
    }


    public int3[] GetScheduled() {

        var flattenedUniqueValues = GetScheduledAux().ToArray().SelectMany(x => x);
        return flattenedUniqueValues.ToArray();
    }

    public List<int3[]> GetScheduledAux()
    {
        
        List<int3[]> res = new List<int3[]>();

        if(!this.subDivided && this.schedule)
        {
            res.Add(myCells.ToArray());
            return res;
        }
        else 
        if(this.subDivided && this.schedule)
        {
            if (TopLeft.schedule)
            {
                res.AddRange(TopLeft.GetScheduledAux());
            }
            if (TopRight.schedule)
            {
                res.AddRange(TopRight.GetScheduledAux());
            }
            if (BottomLeft.schedule)
            {
                res.AddRange(BottomLeft.GetScheduledAux());
            }
            if (BottomRight.schedule)
            {
                res.AddRange(BottomRight.GetScheduledAux());
            }
        }
        return res;

    }

    public void Reset()
    {
        this.schedule = false;
        if (subDivided)
        {
            TopLeft.Reset();
            TopRight.Reset();
            BottomLeft.Reset();
            BottomRight.Reset();
        }

    }

    public void Draw(int max)
    {
        Color sc = schedule ? Color.red : Color.white; 

        Vector3 x1, x2, x3, x4;
        x1 = new Vector3( size.x, 0, size.y );
        x2 = new Vector3( size.x + size.w, 0, size.y );
        x3 = new Vector3( size.x, 0, size.y + size.h);
        x4 = new Vector3( size.x + size.w , 0, size.y + size.h);

        Debug.DrawLine(x1, x2, sc);
        Debug.DrawLine(x1, x3, sc);
        Debug.DrawLine(x2, x4, sc);
        Debug.DrawLine(x3, x4, sc);

        if(heigth < max )
        {
            TopRight.Draw(max);
            TopLeft.Draw(max);
            BottomLeft.Draw(max);
            BottomRight.Draw(max);
        }

    }

}
