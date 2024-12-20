using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NodeVisualiser : MonoBehaviour
{
    public Material material;
    public void Init(Color color)
    {
        try
        {
            material = GetComponentInChildren<Renderer>().material;
            material.color = color;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
}