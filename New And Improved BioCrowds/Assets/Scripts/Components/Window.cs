using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class WindowManager: MonoBehaviour
{
    public static WindowManager instance;
    public float2 size;
    public float3 pivot;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(instance);

        }
    }

    public static float3 Clouds2Crowds(float3 pos)
    {
        float3 auxPos = pos - instance.pivot;
        return new float3(auxPos.x, auxPos.z, auxPos.y);
    }

    public static float3 Crowds2Clouds(float3 pos)
    {
        float3 auxPos = new float3(pos.x, pos.z, pos.y);
        return auxPos + instance.pivot;

    }


}