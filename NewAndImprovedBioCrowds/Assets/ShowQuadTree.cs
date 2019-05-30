using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ShowQuadTree : MonoBehaviour
{
    public static QuadTree qt;
    public int maxShowHeigth;


    void Start()
    {
        maxShowHeigth = BioCrowds.Settings.instance.treeHeight;
    }

    void Update()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    HandleInput();
        //}
        if(qt != null)
            qt.Draw(maxShowHeigth);
    }

    private void HandleInput()
    {
        int x = (int)UnityEngine.Random.Range(qt.size.x, qt.size.w);
        int z = (int)UnityEngine.Random.Range(qt.size.y, qt.size.h);
        int3 cell = new int3(x, 0, z);
        qt.Insert(cell);

    }

}