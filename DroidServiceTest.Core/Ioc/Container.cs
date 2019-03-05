using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;

namespace DroidServiceTest.Core.Ioc
{
    /// <summary>
    /// DO NOT ADD LOGGING TO THIS CLASS BECAUSE IT REQUIRES THE USE OF THE CONTAINER TO 
    /// RESOLVE THE IPlatformService WHICH IS NOT THERE YET!
    /// If you need logging use this: System.Diagnostics.Debug.WriteLine() and then you can filter
    /// Android logs for the tag mono-stdout
    /// </summary>
    public class Container : IDisposable
    {
        private static Container _myself;
        private readonly IContainer _container;
        private readonly List<EventHandler<ComponentRegisteredEventArgs>> _events;
        private TaskCompletionSource<bool> _initTcs;
        private static readonly object Lock = new object();

        private event EventHandler<ComponentRegisteredEventArgs> WhenRegistered
        {
            add
            {
                if (_container == null || _events == null) return;
                lock (Lock)
                {
                    _events.Add(value);
                    _container.ComponentRegistry.Registered += value;
                }
            }
            remove
            {
                if (_container == null || _events == null) return;
                lock (Lock)
                {
                    _events.Remove(value);
                    _container.ComponentRegistry.Registered -= value;
                }
            }
        }


        static Container()
        {
            _myself = new Container();

        }

        private Container()
        {
            _events = new List<EventHandler<ComponentRegisteredEventArgs>>();
            var builder = new ContainerBuilder();
            builder.RegisterSource(new ResolveAnythingSource());
            _container = builder.Build();
        }

        /// <summary>
        /// <para>Used for applications to initialize application and OS specific types.</para>
        /// <para>Example Usage:</para>
        /// <para><![CDATA[Container.Instance.Initialize(config =>]]></para>
        /// <para><![CDATA[{]]></para>
        /// <para><![CDATA[     config.RegisterTypeAs<TestPlatformServices,IPlatformService>(true)]]></para>
        /// <para><![CDATA[});]]></para>
        /// </summary>
        /// <param name="factoryFunction"></param>
        public void Initialize(Action<ContainerConfiguration> factoryFunction)
        {
            lock (Lock)
            {
                _initTcs = new TaskCompletionSource<bool>();
                var containerConfig = new ContainerConfiguration();
                factoryFunction(containerConfig);
                containerConfig.Builder.Update(_container);
                _initTcs.SetResult(true);
            }
        }

        /// <summary>
        /// Auto loads any of the classes found in the assembly that are marked
        /// with the IocServiceAttribute class.
        /// </summary>
        /// <param name="assemblies"></param>
        public void Initialize(params Assembly[] assemblies)
        {
            if (assemblies == null || !assemblies.Any()) return;
            lock (Lock)
            {
                _initTcs = new TaskCompletionSource<bool>();
                var builder = new ContainerBuilder();
                foreach (var assembly in assemblies)
                {
                    if (assembly == null) continue;
                    foreach (var type in assembly.DefinedTypes.Where(t => t.GetCustomAttributes(typeof(IocServiceAttribute), true).Any()))
                    {
                        var attribute = type.GetCustomAttribute<IocServiceAttribute>();
                        var ret = builder.RegisterType(type.AsType());
                        if (attribute.AsSelf)
                        {
                            ret.AsSelf();
                        }
                        if (attribute.AsInterface)
                        {
                            ret.AsImplementedInterfaces();
                        }
                        if (attribute.AsSingleton)
                        {
                            ret.SingleInstance();
                        }

                        builder.RegisterInstance(ret);
                    }
                }
                builder.Update(_container);
                _initTcs.SetResult(true);
            }
        }

        public static Container Instance => _myself ?? (_myself = new Container());

        internal void ResetContainer()
        {
            _myself = new Container();
        }

        public bool IsRegistered<T>()
        {
            return _container.IsRegistered(typeof(T));
        }

        public bool IsRegistered(Type type)
        {
            return _container.IsRegistered(type);
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            object ret;
            try
            {
                ret = _container.Resolve(type);
            }
            catch (ComponentNotRegisteredException cnre)
            {
                throw new ResolutionException($"Exception while resolving type: {type.Name}", cnre);
            }
            catch (DependencyResolutionException dre)
            {
                throw new ResolutionException($"Exception while resolving type: {type.Name}", dre);
            }
            return ret;
        }

        public T TryResolveOrDefault<T>()
        {

            T ret;
            try
            {
                ret = _container.Resolve<T>();
            }
            catch
            {
                ret = default(T);
            }
            return ret;

        }

        public T TryResolve<T>(T defaultValue)
        {
            T ret;
            try
            {
                ret = _container.Resolve<T>();
            }
            catch
            {
                ret = defaultValue;
            }
            return ret;
        }

        public bool TryResolve<T>(out T resolve)
        {
            return _container.TryResolve(out resolve);
        }

        public bool TryResolve(Type type, out object resolve)
        {
            return _container.TryResolve(type, out resolve);
        }

        public void CallbackWhenRegistered(Type type, Action action)
        {
            WhenRegistered += async (sender, args) =>
            {
                var typeName = type.Name;
                var activatorName = args.ComponentRegistration.Activator.LimitType.Name;

                // If the type isn't registered then we're done. This probably never happens.
                if (args.ComponentRegistration.Services.OfType<TypedService>().All(x => x.ServiceType != type)) return;

                // Need to wait until the Init is done otherwise a deadlock will happen when you try to resolve the type. 
                if (_initTcs?.Task != null && _initTcs.Task.IsCompleted != true)
                {
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss:fff} ~~~~ Type: {typeName} --- Awaiting Init Task -- START");
                    await _initTcs.Task;
                    System.Diagnostics.Debug.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss:fff} ~~~~ Type: {typeName} --- Awaiting Init Task -- END");
                }

                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss:fff} ~~~~ Type: {typeName} / ActivatorName: {activatorName} --- action Started");
                // This will cause a deadlock if in the Initialize(above) still.
                action();
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss:fff} ~~~~ Type: {typeName} / ActivatorName: {activatorName} --- action Finished");
            };
        }

        public void Dispose()
        {
            lock (Lock)
            {
                foreach (var handler in _events)
                {
                    _container.ComponentRegistry.Registered -= handler;
                }
                _events.Clear();
            }
        }
    }
}
