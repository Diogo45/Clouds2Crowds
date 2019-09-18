using System.Collections;
using System.Collections.Generic;
using System;
using static Unity.Mathematics.math;


namespace Utilities
{
    using Unity.Mathematics;

    public static class Sampling
    {
        private static System.Random rnd_gen = new System.Random();

        public static void SetSeed() { rnd_gen = new System.Random();  }
        public static void SetSeed(int seed) { rnd_gen = new System.Random(seed); }


        private static float Distance(float2 a, float2 b)
        {
            return ((a.x - b.x) * (a.x - b.x)) + ((a.y - b.y) * (a.y - b.y));
        }

        //http://www.anderswallin.net/2009/05/uniform-random-points-in-a-circle-using-polar-coordinates/
        // Samples point in  annulus  centered in origin.
        public static float2 SampleCircle(float minRadius, float maxRadius)
        {
            float sqrMinRadius = minRadius * minRadius;
            float r = math.sqrt(((float)rnd_gen.NextDouble() * (maxRadius * maxRadius - sqrMinRadius)) + sqrMinRadius);

            float randAngle = 2f * (float)Math.PI * (float)rnd_gen.NextDouble();

            return new float2((float)(r * Math.Cos(randAngle)), (float)(r * Math.Sin(randAngle)));
        }

        //http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
        //Fast Poisson Disk Sampling in Arbitrary Dimensions
        //by Robert Bridson
        public static float2[] PoissonDisc2D(float[,] domain, float r, int k)
        {
            //step 0
            float cell_size = r / (float)Math.Sqrt(2);

            float twoR = 2f * r;

            int cell_rows = (int)((domain[0,1] - domain[0,0]) / cell_size);
            int cell_cols = (int)((domain[1,1] - domain[1,0]) / cell_size);

            float2[] samples = new float2[cell_rows * cell_cols];

            int[,] grid = new int[cell_rows, cell_cols];

            for (int i = 0; i < cell_rows; i++)
                for (int j = 0; j < cell_cols; j++)
                    grid[i, j] = -1;

            //step 1
            List<int> activeList = new List<int>();

            float2 current = SampleCircle(r, twoR);

            current.x += (float)(rnd_gen.NextDouble() * (int)((domain[0, 1] - domain[0, 0]))) + domain[0, 0];
            current.y += (float)(rnd_gen.NextDouble() * (int)((domain[1, 1] - domain[1, 0]))) + domain[1, 0];

            activeList.Add(0);

            samples[0] = current;

            grid[(int)(current.x / cell_size), (int)(current.y / cell_size)] = 0;

            int sample_count = 1;

            //step 3
            while(activeList.Count > 0)
            {
                int _k = 0;
                bool selected = true;

                while (_k < k)
                {
                    int i = rnd_gen.Next(activeList.Count);
                    int index = activeList[i];

                    current = SampleCircle(r, twoR);
                    current = current + samples[index];

                    int curr_gx = (int)(current.x / cell_size);
                    int curr_gy = (int)(current.y / cell_size);

                    if (grid[curr_gx, curr_gy] != -1)
                        continue;

                    for (int m = math.max(curr_gx - 1, 0); m < math.min(curr_gx + 2, cell_rows - 1); m++) { 
                        for (int n = math.max(curr_gy - 1, 0); n < math.min(curr_gy + 2, cell_cols - 1); n++)
                        {
                            if (grid[m, n] == -1)
                                continue;

                            if (Distance(current, samples[grid[m, n]]) < r)
                            {
                                selected = false;
                                break;
                            }

                        }

                        if (!selected)
                            break;
                    }

                    if (selected)
                    {
                        samples[sample_count] = current;
                        grid[curr_gx, curr_gy] = sample_count++;
                        break;
                    }

                    _k++;
                    
                }

            }

            return samples;
        }
    }
}

