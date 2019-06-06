using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BioClouds
{

    public class CollisionMaskSettings : MonoBehaviour
    {
        public string fileName;
        public Renderer maskRenderer;
        [Range(0f, 1f)]
        public float textureAlpha;
        [HideInInspector]
        public int maskWidth;
        [HideInInspector]
        public int maskHeight;
        public bool[,] collisionMask;

        public void Init()
        {
            if (fileName == null)
                return;
            
            Texture2D __tex =  Resources.Load<Texture2D>("CollisionMasks/" + fileName);
            if (__tex == null)
                Debug.LogError("CollisionMask: Texture not found.");

            maskWidth = __tex.width;
            maskHeight = __tex.height;

            collisionMask = new bool[maskWidth, maskHeight];
            for (int y = 0; y < maskWidth; y ++)
            {
                for (int x = 0; x < maskWidth; x++)
                {
                    collisionMask[x, y] = __tex.GetPixel(x, y).r > 0.5f ? false : true;
                }
            }


            maskRenderer.material.mainTexture = __tex;
            maskRenderer.material.color = new Color(1f, 1f, 1f, textureAlpha);
        }
    }
}