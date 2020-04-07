using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class InteractionModuleManager : IModuleManager
{

    public static InteractionModuleManager instance;

    private AddInteractionBuffer addInteractionBuffer;
    private InitializeInteractionBuffer initializeInteractionBuffer;
    private DataGatheringSystem dataGatheringSystem;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        var world = World.Active;

        addInteractionBuffer = world.GetExistingManager<AddInteractionBuffer>();
        initializeInteractionBuffer = world.GetExistingManager<InitializeInteractionBuffer>();
        dataGatheringSystem = world.GetExistingManager<DataGatheringSystem>();


        Disable();

    }


    public override void Disable()
    {
        addInteractionBuffer.Enabled = false;
        initializeInteractionBuffer.Enabled = false;
        dataGatheringSystem.Enabled = false;

    }

    public override void Enable()
    {
        addInteractionBuffer.Enabled = true;
        initializeInteractionBuffer.Enabled = true;
        dataGatheringSystem.Enabled = true;

    }





}