using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies
{
    ///<inheritdoc cref="IMonoDependent"/>
    public abstract class MonoDependent : BaseMonoDependent<DependencyContainer, 
        MonoContainerService>, IMonoDependent { }
}