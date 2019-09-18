using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class CreateNoiseTexture {

	public static Texture2D GetNoiseTexture(int height, int width, float scale, float2 origin)
    {
        Texture2D noiseTex = new Texture2D(width, height);
        Color[] pix = new Color[height * width];

        float xOrg = origin.x;
        float yOrg = origin.y;
        // For each pixel in the texture...
        float y = 0.0F;

        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
                float xCoord = xOrg + x / noiseTex.width * scale;
                float yCoord = yOrg + y / noiseTex.height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                float sample2 = Mathf.PerlinNoise(xCoord * 2f, yCoord * 2f);
                float sample3 =  Mathf.PerlinNoise(xCoord * 4f, yCoord * 4f);

                float sample4 = sample + 0.5f * sample2 + 0.25f * sample3;

                pix[(int)y * noiseTex.width + (int)x] = new Color(sample4, sample4, sample4);
                x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTex.SetPixels(pix);
        noiseTex.Apply();

        return noiseTex;

    }

}

