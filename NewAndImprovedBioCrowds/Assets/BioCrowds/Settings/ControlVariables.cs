using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BioCrowds
{
    public class ControlVariables : MonoBehaviour
    {


        public static ControlVariables instance;

        //Global lock for completing biocrowds 
        [HideInInspector]
        public bool LockBioCrowds = false;

        public bool SyncWithFluidSimulator = false;    




        private void Start()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }





    }
}
