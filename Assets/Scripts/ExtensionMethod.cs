using UnityEngine;

public static class ExtensionMethod
{
    public static Object InstantiateUnit(this Object thisObj, Object original, Vector3 position, Quaternion rotation, Transform parent, TeamClass team)
    {
        GameObject newObj = Object.Instantiate(original, position, rotation, parent) as GameObject;
        newObj.GetComponent<UnitInformation>().SetInformation(team);
        return newObj;
    }
}