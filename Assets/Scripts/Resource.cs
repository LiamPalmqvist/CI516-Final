using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Resource : MonoBehaviour
{
    public enum ResourceType
    {
        Gold,
        Silver,
        Copper,
        Stone,
        None
    }
    
    public static readonly List<ResourceType> resourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();

    public Vector2 currentPosition;
    public Vector3 originalPosition;
    private ResourceType resourceType;
    public int resourcesHeld;
    public int depletionRate;
    public int gatherRange = 2;

    public void FixedUpdate()
    {
        if (resourcesHeld <= 0) DestroyResource();
    }

    public void SpawnResource(ResourceType type, int amount, Vector2 position)
    {
        resourcesHeld = amount;
        resourceType = type;
        currentPosition = new Vector2(position.x, position.y);
    }

    private void DestroyResource()
    {
        resourcesHeld = 0;
        resourceType = ResourceType.None;
        currentPosition = Vector2.zero;
        transform.position = originalPosition;
    }
}