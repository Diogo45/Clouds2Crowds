using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowQuadTree : MonoBehaviour
{
    public static QuadTree qt;
    public int maxShowHeigth;


    void Update()
    {
        qt.Draw(maxShowHeigth);
    }
}