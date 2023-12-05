using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQL.MLDatabase;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace SQL
{
    public class ContainerBootstrapper
    {
        public static void RegisterTypes(IUnityContainer container)
        {
            container.RegisterType<IMLData, MLDataDirectClient>("MLDatabase", new TransientLifetimeManager(),
                new InjectionConstructor(false));
            container.RegisterType<IMLData, MockedMLDataAccess>("MLMockedData", new TransientLifetimeManager(),
                new InjectionConstructor(false));
        }
    }
}
