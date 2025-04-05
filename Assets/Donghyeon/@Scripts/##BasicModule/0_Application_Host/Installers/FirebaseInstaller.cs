// FirebaseInstaller.cs 또는 적절한 위치의 Installer 클래스
using VContainer;
using VContainer.Unity;
using UnityEngine;
using System.Collections; // 네임스페이스 추가

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers
{
    public class FirebaseInstaller : IModuleInstaller
    {
        public ModuleType ModuleType => ModuleType.ThirdParty; // 적절한 ModuleType 사용

        public void Install(IContainerBuilder builder)
        {
            // FirebaseManager 싱글톤 인스턴스 등록
            // builder.RegisterComponentOnNewGameObject<FirebaseManager>(Lifetime.Singleton, "FirebaseManager")
            //        .AsSelf() // FirebaseManager 타입으로 등록
            //        .AsImplementedInterfaces(); // 구현된 인터페이스로도 등록

            // // UserDataManager 싱글톤 인스턴스 등록 (일반 클래스 방식)
            // builder.Register<UserDataManager>(Lifetime.Singleton) // Register<T> 사용
            //        .AsSelf()
            //        .AsImplementedInterfaces(); // 인터페이스로도 등록

            // // 빌드 콜백: FirebaseManager 초기화 후 UserDataManager 초기화
            // builder.RegisterBuildCallback(resolver =>
            // {
            //     var firebaseManager = resolver.Resolve<FirebaseManager>();
            //     var userDataManager = resolver.Resolve<UserDataManager>();
            //     // Debug.Log("[FirebaseInstaller] UserDataManager initialized via BuildCallback."); // 이전 로그 제거

            //     // FirebaseManager의 MonoBehaviour 컨텍스트를 사용하여 코루틴 시작
            //     firebaseManager.StartCoroutine(InitializeUserDataManagerWhenFirebaseReady(firebaseManager, userDataManager));
            // });
        }

        // Helper Coroutine
        // private IEnumerator InitializeUserDataManagerWhenFirebaseReady(FirebaseManager fm, UserDataManager udm)
        // {
        //     // FirebaseManager가 완전히 초기화될 때까지 기다림
        //     // (fm.IsInit()이 true를 반환할 때까지)
        //     yield return new WaitUntil(() => fm.IsInit());

        //     Debug.Log("[FirebaseInstaller] FirebaseManager is initialized. Initializing UserDataManager...");
        //     // UserDataManager 초기화
        //     udm.Init();
        // }
    }
}