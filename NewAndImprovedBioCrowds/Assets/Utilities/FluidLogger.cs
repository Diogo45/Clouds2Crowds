using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public struct FluidLog
{
    public int frame;
    public float3[] agentPos;
    public float3[] agentVel;
    public BioCrowds.SpringSystem.Spring[] currentSprings;

    public override string ToString()
    {
        return frame + ";" + string.Join(",", agentPos) + ";" + string.Join(",", agentVel) + ";" + string.Join(",", currentSprings) + "\n"; 
    }

}

public struct InitialParameters
{
    public float[] tau;
    public float[] mass;

    public override string ToString()
    {
        return string.Join(",", tau) + ";" + string.Join(",", mass) + "\n";
    }

}

public static class FluidLogger
{
    private static string allText = "";

    public static FluidLog currentLog;

    public static void WriteInitalParam(InitialParameters par)
    {
        allText += par.ToString();
    }

    public static void WriteFrame(FluidLog log)
    {
        allText += log.ToString();
    }

    public static void WriteToFile(string filename)
    {
        System.IO.File.WriteAllText(filename, allText);
    }

}

