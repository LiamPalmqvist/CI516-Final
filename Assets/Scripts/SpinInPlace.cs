using UnityEngine;

public class SpinInPlace : MonoBehaviour
{
    public int spinSpeed;
    public float floatHeight;
    public bool spinX;
    public bool spinY;
    public bool spinZ;
    void FixedUpdate()
    {
        transform.Rotate(new Vector3(spinX ? (spinSpeed * Time.deltaTime) : 0, spinY ? (spinSpeed * Time.deltaTime) : 0, spinZ ? (spinSpeed * Time.deltaTime) : 0));
        transform.position += new Vector3(0, floatHeight * Mathf.Sin(Time.time), 0);
    }
}
