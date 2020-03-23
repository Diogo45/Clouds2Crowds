using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Linq;
using Unity.Transforms;
using Unity.Jobs;
using UnityEditor;



public abstract class IModuleManager : MonoBehaviour
{
    public abstract void Enable();
    public abstract void Disable();
}