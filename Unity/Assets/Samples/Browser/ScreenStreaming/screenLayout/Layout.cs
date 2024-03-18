using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Layout : MonoBehaviour
{
    private GameObject space;
    public ScreenManager screenManager;
    [HideInInspector] public List<Screen> Screens;
    public float spacing = 5.0f;
    public float scaling = 0.003f; 
    public static float spaceWidth;
    public static float spaceHeight;
    public bool useLayout = false;

        [HideInInspector]
    public float previousHeight;
	[HideInInspector]
    public float previousCenterRadius;
	[HideInInspector]
    public float previousAngle;

    public float Height;    
    public float centerRadius;
    public float Angle;
    public Vector3 center;
    public Vector3 direction;

    public bool HasChanged()
    {
        return centerRadius != previousCenterRadius || 
		        Height != previousHeight || 
				Angle != previousAngle;
    }

    public void UpdateValues()
    {
        previousCenterRadius = centerRadius;
        previousHeight = Height;
        previousAngle = Angle;
    }

    public Vector2 GetScreensSize()
    {
        return new Vector2(Mathf.Abs(Angle * Mathf.Deg2Rad * centerRadius), Height);
    }

    public Vector3 CoordsNormalized2World(float u, float v)
    {
        // Adjust the calculations to consider the center and direction
        Vector3 pointOnCylinder = new Vector3(centerRadius * Mathf.Cos(Mathf.Deg2Rad * u), v - (Height / 2), centerRadius * Mathf.Sin(Mathf.Deg2Rad * u));
        return center + Quaternion.LookRotation(direction) * pointOnCylinder;
    }

    public Vector3 CoordsUVToWorld(float u, float v)
    {
        // Adjust the calculations to consider the center and direction
        Vector3 pointOnCylinder = new Vector3(centerRadius * Mathf.Cos(Mathf.Deg2Rad * u), v - (Height / 2), centerRadius * Mathf.Sin(Mathf.Deg2Rad * u));
        return center + Quaternion.LookRotation(direction) * pointOnCylinder;
    }

    public bool RefreshPlacement()
    {
        if (Screens == null)
        {
            Screens = new List<Screen>();
        }
        Screens.Clear();
        foreach (var entry in screenManager.screens)
        {
            DictionaryEntry dictionaryEntry = (DictionaryEntry) entry;
            string key = dictionaryEntry.Key as string;
            Screen s = dictionaryEntry.Value as Screen;
            Texture tex = s.screenQuad.GetComponent<Renderer>().material.mainTexture;
            if (tex)
            {
                Screens.Add(new Screen(tex.width, tex.height, key));
            }
            else
            {
                return false;
            }
        }

        if (useLayout)
        {
            Vector2 size = GetScreensSize();
            spaceWidth = size.x;
            spaceHeight = size.y;
        }

        SetLayout(Screens, spaceWidth);
        return true;
    }

    public void SetLayout(List<Screen> screens, float spaceWidth)
    {

        if (screens.Count > 0)
        {
            SetLine(screens, spaceWidth, spaceHeight);
        }
    }

    private void SetLine(List<Screen> line, float w, float h)
    {
        float totalWidth = line.Sum(r => (r.width* scaling)) + (line.Count - 1) * spacing;
        float startX = (w - totalWidth) / 2;
        float centerY = h / 2;

        foreach (var r in line)
        {
            r.X = startX;
            r.Y = centerY - r.height * scaling / 2; 

            startX += (r.width * scaling) + spacing;

            Screen s = screenManager.screens[r.id] as Screen;
            s.texturePos = new Vector2(r.X, r.Y);
        }
    }
}
