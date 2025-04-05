using VContainer;
using VContainer.Unity;
using Unity.Assets.Scripts.Data;
namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{



    public class DataInstaller : IModuleInstaller
    {
        public ModuleType ModuleType => ModuleType.GameData;


        public void Install(IContainerBuilder builder)
        {

            builder.Register<DataLoader>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<RemoteDataRepository>(Lifetime.Singleton).As<IDataRepository>();
            builder.Register<CurrencyManager>(Lifetime.Singleton); 
            builder.Register<GameDataManager>(Lifetime.Singleton);
            
        }
    

    }
}
