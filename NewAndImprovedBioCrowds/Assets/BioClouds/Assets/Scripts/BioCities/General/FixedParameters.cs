using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BioClouds
{
    /// <summary>
    /// Class for the fixed objects of bioclouds.
    /// </summary>
    public class FixedParameters : MonoBehaviour
    {
        public MeshMaterial[] CloudRendererData;
        public MeshMaterial[] HeatQuadRendererData;

        public Texture2D HeatMapTexture;
    }
}
