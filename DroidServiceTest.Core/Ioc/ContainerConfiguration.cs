using System;
using System.Reflection;
using Autofac;

namespace DroidServiceTest.Core.Ioc
{
    /// <summary>
    /// Class used to register types into the container.
    /// </summary>
    public class ContainerConfiguration
    {
        internal ContainerConfiguration()
        {
        }

        private readonly ContainerBuilder _builder = new ContainerBuilder();

        public void RegisterType<T>(bool asSingleton, bool asSelf = false)
        {
            var builder = _builder.RegisterType<T>();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }

        }

        /// <summary>
        /// Registers Type Concrete as an Interface
        /// </summary>
        /// <param name="tConcrete"></param>
        /// <param name="tInterface"></param>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterTypeAs(System.Type tConcrete, System.Type tInterface, bool asSingleton = false, bool asSelf = false)
        {
            var builder = _builder.RegisterType(tConcrete).As(tInterface);

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }

        }

        /// <summary>
        /// Registers Type Concrete as an Interface
        /// </summary>
        /// <typeparam name="TConcrete">A concrete reference returned</typeparam>
        /// <typeparam name="TInterface">Interface or service type</typeparam>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterTypeAs<TConcrete, TInterface>(bool asSingleton, bool asSelf = false)
        {
            var builder = _builder.RegisterType<TConcrete>().As<TInterface>();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// Registers Type and as an Interface
        /// </summary>
        /// <typeparam name="TConcrete">A concrete reference returned</typeparam>
        /// <typeparam name="TInterface">Interface or service type</typeparam>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterTypeBoth<TConcrete, TInterface>(bool asSingleton, bool asSelf = false)
        {
            var builder = _builder.RegisterType<TConcrete>().AsSelf().As<TInterface>();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// <p>Registers a pre-generated object instance as its interface.
        /// Useful for dealing with pre-existing singletons and for unit
        /// testing with mocks.</p>
        /// 
        /// <p>Example usage, using Moq mocks</p>
        /// <code>
        /// IService mockService = Mock.Get&lt;IService&gt;();
        /// ... mockService setup
        /// containerConfig.RegisterInstanceAs(mockService, true);
        /// </code>
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="instance"></param>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterInstanceAs<TInterface>(TInterface instance, bool asSingleton = false, bool asSelf = false) where TInterface : class
        {
            var builder = _builder.RegisterInstance(instance).As<TInterface>();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// <p>Registers a pre-generated object instance as its interface.
        /// Useful for dealing with pre-existing singletons and for unit
        /// testing with mocks.</p>
        /// </summary>
        /// <param name="tInterface"></param>
        /// <param name="instance"></param>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterInstanceAs(System.Type tInterface, object instance, bool asSingleton = false, bool asSelf = false)
        {
            var builder = _builder.RegisterInstance(instance).As(tInterface);

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// <p>Registers a pre-generated object instance as its interface.
        /// Useful for dealing with pre-existing singletons and for unit
        /// testing with mocks.</p>
        /// </summary>
        /// <param name="constructor"></param>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterLazy<TInterface>(Func<TInterface> constructor, bool asSingleton = false, bool asSelf = false)
        {
            var builder = _builder.Register(cc => constructor()).As<TInterface>().AsSelf().SingleInstance();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// <p>Registers a pre-generated object instance as its interface.
        /// Useful for dealing with pre-existing singletons and for unit
        /// testing with mocks.</p>
        /// </summary>
        /// <param name="tInterface"></param>
        /// <param name="constructor"></param>
        /// <param name="asSingleton"></param>
        /// <param name="asSelf"></param>
        public void RegisterLazy(System.Type tInterface, Func<object> constructor, bool asSingleton = false, bool asSelf = false)
        {
            var builder = _builder.Register(cc => constructor()).As(tInterface).AsSelf().SingleInstance();

            if (asSelf)
            {
                builder.AsSelf();
            }

            if (asSingleton)
            {
                builder.SingleInstance();
            }
        }

        /// <summary>
        /// Filters the scanned types to include only those assignable to the provided
        /// type of TConcrete and TInterface.
        /// Configure the services that the component will provide. The generic parameter(s) to As()
        /// will be exposed as TypedService instances.
        /// </summary>
        /// <typeparam name="TConcrete">The type or interface which all classes must be assignable from.</typeparam>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="assembly"></param>
        public void RegisterAssemblyTypes<TConcrete, TInterface>(params Assembly[] assemblies)
        {
            _builder.RegisterAssemblyTypes(assemblies)
                .AssignableTo<TConcrete>()
                .As<TInterface, TConcrete>()
                .AsSelf();
        }

        internal ContainerBuilder Builder { get { return _builder; } }
    }
}