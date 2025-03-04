using Chopsticks.Dependencies;
using UnityEngine;

public class OutputSystem : MonoDependency, ISystem
{
    [SerializeField]
    private string _configuredOutput;

    public void Perform()
    {
        Debug.Log(_configuredOutput);
    }


    protected override void OnRegistration()
    {
        RegisterAs<ISystem>();
    }
}
