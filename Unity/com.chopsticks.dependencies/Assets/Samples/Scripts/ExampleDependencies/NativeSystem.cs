using UnityEngine;

public class NativeSystem : ISystem
{
    public void Perform()
    {
        Debug.Log("The native system is performing its functionality.");
    }
}