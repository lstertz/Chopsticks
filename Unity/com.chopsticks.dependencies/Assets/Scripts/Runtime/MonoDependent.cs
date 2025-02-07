using Chopsticks.Dependencies.Consumers;
using Chopsticks.Dependencies.Containers;
using UnityEngine;

namespace Chopsticks.Dependencies
{
    /// <summary>
    /// Defines a consumer of dependencies that is itself not a dependency.
    /// </summary>
    public class MonoDependent : MonoBehaviour, IMonoDependent
    {
        ///<inheritdoc/>
        public MonoContainer Container { get; protected set; }

        ///<inheritdoc/>
        public ContainerSetting ContainerSetting { get; protected set; }


        ///<inheritdoc/>
        public virtual void OnEnable()
        {
            GetUpdatedContainer(out var updatedContainer);
            Container = updatedContainer;
        }

        ///<inheritdoc/>
        public virtual void OnTransformParentChanged()
        {
            if (!enabled)
                return;

            if (!GetUpdatedContainer(out var updatedContainer))
                return;

            Container = updatedContainer;
            OnContainerChanged();
        }

        /// <summary>
        /// Invoked when the container is changed as the result of a change in the 
        /// MonoBehaviour's parent hierarchy.
        /// </summary>
        protected virtual void OnContainerChanged() { }


        protected bool GetUpdatedContainer(out MonoContainer updatedContainer)
        {
            updatedContainer = this.FindContainer();
            return Container != updatedContainer;
        }


        // TODO :: Wrappers to Resolve dependencies through the interface extensions.
    }
}
