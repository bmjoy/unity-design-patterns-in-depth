namespace AdvancedSceneManager.Core.AsyncOperations
{

    /// <summary>
    /// Performs startup sequence, but without splash screen and loading screens, see <see cref="Runtime.QuickStart"/>:
    /// <para><see cref="SetProfileInBuild"/>.</para>
    /// <para><see cref="ReloadAssets"/>.</para>
    /// <para><see cref="CloseAllUnityScenesAction"/>.</para>
    /// <para><see cref="OpenCollectionsAndScenesFlaggedToOpenAtStartAction"/>.</para>
    /// </summary>
    public class QuickStartupAction : StartupAction
    {

        ///<inheritdoc cref="QuickStartupAction"/>
        public QuickStartupAction()
            : base(skipSplashScreen: true)
        { }

    }

}
