using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Builder;
using Autofac.Core;

namespace DroidServiceTest.Core.Ioc
{
    /// <summary>
    /// This source will allow you to resolve components that have not been registered
    /// with the container.  Specifically concrete classes with default constructors.
    /// 
    /// public interface IFoo{
    /// }
    /// 
    /// public class Foo : IFoo{
    ///     public Foo(){}
    /// }
    /// 
    /// var foo = Container.Instance.Resolve<Foo>();
    /// 
    /// Additional information: http://nblumhardt.com/2010/01/resolve-anything/
    /// </summary>
    public class ResolveAnythingSource : IRegistrationSource
    {
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            var ts = service as TypedService;

            if (ts != null && !ts.ServiceType.GetTypeInfo().IsAbstract && ts.ServiceType.GetTypeInfo().IsClass)
            {
                var rb = RegistrationBuilder.ForType(ts.ServiceType);
                return new[] { rb.CreateRegistration() };
            }

            return Enumerable.Empty<IComponentRegistration>();
        }

        public bool IsAdapterForIndividualComponents { get; private set; }
    }
}