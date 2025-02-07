using Chopsticks.Dependencies.Consumers;

namespace Chopsticks.Dependencies
{
    /// <inheritdoc cref="IMonoDependency"/>
    public abstract class MonoDependency : MonoDependent, IMonoDependency
    {
        ///<inheritdoc/>
        public override void OnEnable()
        {
            base.OnEnable();
            OnRegistration();
        }

        ///<inheritdoc/>
        public virtual void OnDisable() => Deregister();

        ///<inheritdoc/>
        public override void OnTransformParentChanged()
        {
            if (!enabled)
                return;

            if (!GetUpdatedContainer(out var updatedContainer))
                return;

            Deregister();
            Container = updatedContainer;
            OnRegistration();

            OnContainerChanged();
        }


        /// <summary>
        /// Performs registration of this dependency for each of its contracts 
        /// using <see cref="RegisterAs{T}"/>.
        /// </summary>
        protected abstract void OnRegistration();

        protected void RegisterAs<T>()
        {
            // TODO :: Uses interface extension method to register.
            // TODO :: Tracks registrations.
        }


        private void Deregister()
        {
            // TODO :: Deregisters all tracked registrations.
        }
    }
}
