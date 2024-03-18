using System;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator : MonoBehaviour
{
    private List<Color> generatedColors;
    private System.Random random;
    public float minDistance = 0.5f;

    public ColorGenerator()
    {
        generatedColors = new List<Color>();
        random = new System.Random(12345);
    }

    public Color GenerateColor()
    {
        Color newColor;
        bool isTooClose;

        do
        {
            isTooClose = false;
            newColor = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

            foreach (var color in generatedColors)
            {
                if (Vector3.Distance(new Vector3(color.r, color.g, color.b), new Vector3(newColor.r, newColor.g, newColor.b)) < minDistance)
                {
                    isTooClose = true;
                    break;
                }
            }
        }
        while (isTooClose);

        generatedColors.Add(newColor);
        return newColor;
    }
}