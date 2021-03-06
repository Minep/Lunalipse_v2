﻿using Lunalipse.Common;
using Lunalipse.Common.Data;
using Lunalipse.Common.Generic.Cache;
using Lunalipse.Core;
using Lunalipse.Core.Cache;
using Lunalipse.Core.GlobalSetting;
using Lunalipse.Core.I18N;
using Lunalipse.Core.PlayList;
using Lunalipse.Core.Theme;
using Lunalipse.Core.WebMusic;
using Lunalipse.I18N;
using Lunalipse.Resource.Generic.Types;
using Lunalipse.Utilities;
using NetEaseHijacker;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Lunalipse
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        I18NTokenizer I18T;
        CacheHub cacheSystem;
        CataloguePool cp;
        ResourcesHandler resourcesHandler;
        SearchEngineManager engineManager;
        string currentFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        LunalipseLogger Log;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log = LunalipseLogger.GetLogger();
#if !DEBUG
            DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif
            I18T = new I18NTokenizer();
            cp = CataloguePool.Instance;
            cacheSystem = CacheHub.Instance(currentFolder);
            engineManager = SearchEngineManager.Instance;
            resourcesHandler = new ResourcesHandler(Assembly.GetEntryAssembly().GetName().Version);

            AppConst.APP_EXE_DIRECTORY = currentFolder;

            engineManager.ApplicationBaseDir = currentFolder;

            Log.Info($"Lunalipse Music Player (Version: {VersionHelper.Instance.getFullVersion()})");
            Log.Info("Logger started");

            CheckResources();
            InitializeI18NEnvironemnt();
            RegisterOperators();

            RestoringConfig();
            InitializeWebSearchEngines();

            PerpearThemeColor();

            ReadLicenses();
        }

        private void InitializeWebSearchEngines()
        {
            Log.Info("Registering web music search engines...");
            engineManager.RegisterEngine("Netease", new MainEngine());
            engineManager.SelectEngine("Netease");
            engineManager.UpdateProxySetting(GLS.INSTANCE.ProxySetting);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Exception(e.Exception);
            Log.Release();
            e.Handled = true;
            MessageBox.Show(e.Exception.Message, "致命错误 (Fatal Error)", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }

        void RegisterOperators()
        {
            Log.Info("Registering caching operators..");
            cacheSystem.RegisterOperator(CacheType.WebAlbumPic, new AlbumCoverImageCacher()
            {
                UseLZ78Compress = true,
                Responsiblity = CacheType.WebAlbumPic
            });
            cacheSystem.RegisterOperator(CacheType.ALBUM_PIC, new AlbumCoverImageCacher()
            {
                UseLZ78Compress = true,
                Responsiblity = CacheType.ALBUM_PIC
            });
            cacheSystem.RegisterOperator(CacheType.WebAudioStuff, new WebAudioFileCacher()
            {
                UseLZ78Compress = true
            });
            cacheSystem.RegisterOperator(CacheType.MusicList, new MusicPoolCache());
            cacheSystem.RegisterOperator(CacheType.PlayList, new PlaylistCache());
        }

        void InitializeI18NEnvironemnt()
        {
            Log.Info("Perpearing i18n environemnt");
            SupportLanguages supportLanguages;
            if(GLS.INSTANCE.UseSystemDefaultLanguage)
            {
                supportLanguages = TranslationManager.GetSystemLanguage();
            }
            else
            {
                if(!Enum.TryParse(GLS.INSTANCE.CurrentSelectedLanguage,out supportLanguages))
                {
                    supportLanguages = SupportLanguages.CHINESE_SIM;
                    Log.Warning("Unknow Language '{0}'. Use default".FormateEx(GLS.INSTANCE.CurrentSelectedLanguage));
                }
            }
            TranslationManager.Instance.SetLanguage(supportLanguages);
        }

        void RestoringConfig()
        {
            GlobalSettingHelper<GLS>.Instance.UseLZ78Compress =
#if DEBUG
                false
#else
                false
#endif
                ;
            GLS.SetINSTANCE(GlobalSettingHelper<GLS>.Instance.ReadSetting());
            GLS.INSTANCE.PrepareProxy();
        }

        void CheckResources()
        {
            Log.Info("Checking and releasing external resources");
            resourcesHandler.ReleaseResources(Assembly.GetExecutingAssembly().GetManifestResourceNames(),
                currentFolder,
                Assembly.GetExecutingAssembly());
        }

        void PerpearThemeColor()
        {
            Log.Info("Loaidng themes");
            LThemeManager.Instance.SelectTheme(GLS.INSTANCE.DefaultThemeUUID);
        }


        
        async void ReadLicenses()
        {
            foreach (LrssResource lrr in await resourcesHandler.getResourcesAsync(currentFolder + "\\" + AppConst.LUNALIPSE_LICENSES)) 
            {
                string text = Encoding.UTF8.GetString(lrr.Data);
                switch (lrr.Name)
                {
                    case "GNU_GPL":
                        AppConst.LICENSE_GUNGPL_LUNALIPSE = text;
                        break;
                    case "GNU_LGPL":
                        AppConst.LICENSE_GUNLGPL_TAGLIB = text;
                        break;
                    case "MIT":
                        AppConst.LICENSE_MIT_JSON = text;
                        break;
                    case "MSPL":
                        AppConst.LICENSE_MS_PL_CSCORE = text;
                        break;
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //engineManager.UnloadEngines();
        }
    }
}
