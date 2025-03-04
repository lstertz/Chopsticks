using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using System.Collections.Generic;
using UnityEngine;

public class CountingSystem : MonoBehaviour, IMonoDependency, ISystem
{
    public List<DependencyRegistration> Registrations { get; } = new();

    public DependencyContainer Container { get; set; }

    public ContainerSetting ContainerSetting => ContainerSetting.HierarchyWithGlobal;


    private int _count;



    public void Perform()
    {
        _count++;
        Debug.Log($"Current performance count: {_count}");
    }


    public void OnDisable() => 
        this.DeregisterAll();

    public void OnEnable() => 
        this.SetContainer(this, null, OnRegistration);

    public void OnTransformParentChanged() =>
        this.UpdateContainer(this, null, null, null);

    protected void OnRegistration()
    {
        this.RegisterAs<DependencyContainer, MonoContainerService, ISystem>();
    }
}
