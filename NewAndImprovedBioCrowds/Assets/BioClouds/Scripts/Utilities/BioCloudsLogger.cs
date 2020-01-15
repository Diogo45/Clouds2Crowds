using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BioCloudsLogger
{

    public enum EventType
    {
        String = 1,
        CloudSplit = 2,
        CloudMerge = 4,
        CloudPositionUpdated = 8,
        CloudVelocityUpdated = 16,
        CloudGoalReached = 32,
        CloudDensityUpdated = 64,
        CloudPathFind = 128

    }

    public static int BufferSize = 100;

    public static BioCloudsLogger Instance(string instanceName)
    {
        BioCloudsLogger logger;
        if (BioCloudsLogger.dicts.TryGetValue(instanceName, out logger))
            return logger;
        else
        {
            logger = new BioCloudsLogger(instanceName);
            BioCloudsLogger.dicts.Add(instanceName, logger);

            return logger;
        }
    }

    private string name = "";

    private static Dictionary<string, BioCloudsLogger> dicts = new Dictionary<string, BioCloudsLogger>();

    private Dictionary<EventType, List<object>> dataBuffer = new Dictionary<EventType, List<object>>();

    private StreamWriter streamWriter;

    public BioCloudsLogger(string instanceName)
    {
        name = instanceName;
    }
    
    public bool FlushBuffer(EventType type)
    {
        return true;
    }

    public bool EnBuffer(EventType type, object data)
    {


        //if (BioCloudsLogger.dicts.TryGetValue(instanceName, out var logger))
        //    return logger;
        //else
        //{
        //    logger = new BioCloudsLogger(instanceName);
        //    BioCloudsLogger.dicts.Add(instanceName, logger);

        //    return logger;
        //}
        //var list = dataBuffer.TryGetValue

        return true;
    }

}


