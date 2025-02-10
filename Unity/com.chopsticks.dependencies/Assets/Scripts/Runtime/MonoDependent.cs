using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies
{


    ///<inheritdoc cref="IMonoDependent"/>
    public abstract class MonoDependent : 
        BaseMonoDependent<DependencyContainer, 
            UnityContainerService<DependencyContainer, DefaultDependencyContainerFactory,
            DependencyContainerDefinition>>, IMonoDependent { }
}
