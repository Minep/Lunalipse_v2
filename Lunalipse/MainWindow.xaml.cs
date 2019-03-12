﻿using System;
using System.Windows;
using System.Windows.Input;
using Lunalipse.Core.PlayList;
using Lunalipse.Core.Metadata;
using Lunalipse.Common.Data;
using Lunalipse.Presentation.LpsWindow;
using System.Windows.Threading;
using System.Collections.Generic;
using Lunalipse.Common.Generic.AudioControlPanel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lunalipse.Common.Generic.Catalogue;
using Lunalipse.Core.Cache;
using Lunalipse.Core;
using Lunalipse.Pages;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Lunalipse.Windows;
using Lunalipse.Common.Interfaces.II18N;
using Lunalipse.Common.Bus.Event;
using Lunalipse.Common.Generic.I18N;
using Lunalipse.Common.Generic.Cache;
using Lunalipse.Auxiliary;
using Lunalipse.Core.LpsAudio;
using Lunalipse.Common;

namespace Lunalipse
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// （测试界面）
    /// </summary>
    public partial class MainWindow : LunalipseMainWindow, ITranslatable
    {
        const int sliderSize = 200;

        private MusicListPool mlp;
        private CataloguePool CPOOL;
        private MediaMetaDataReader mmdr;
        private CacheHub cacheSystem;
        private EventBus Bus;
        private PlaylistGuard playlistGuard;
        private LpsCore core;
        private GLS GlobalSetting = GLS.INSTANCE;
        private VersionHelper versionHelper;

        private CatalogueShowCase showcase;
        private MusicSelected musicList;

        private Duration elapseTime = new Duration(TimeSpan.FromMilliseconds(250));
        private DoubleAnimation ExpandPanel;
        private DoubleAnimation MinimizePanel;

        List<Catalogue> ByLocation, ByArtist, ByAlbum, ByUserDefined;
        private DesktopDisplay desktopDisplay;

        private string LinearMode, SingleLoop, ShuffleMode;

        public MainWindow() : base()
        {
            InitializeComponent();
            InitializeModules();
            ExpandPanel = new DoubleAnimation(48, sliderSize, elapseTime);
            MinimizePanel = new DoubleAnimation(sliderSize, 48, elapseTime);

            EventBus.OnBoardcastRecieved += EventBus_OnBoardcastRecieved;
            

            TranslationManagerBase.OnI18NEnvironmentChanged += Translate;
            Translate(TranslationManagerBase.AquireConverter());

            foreach (string path in GLS.INSTANCE.MusicBaseDirs)
            {
                mlp.AddToPool(path);
            }
            playlistGuard.Restore();

            CataloguesRefleshAll();

        }

        private void EventBus_OnBoardcastRecieved(EventBusTypes busTypes, object Tag)
        {
            if(busTypes == EventBusTypes.ON_ACTION_COMPLETE)
            {
                switch(Tag)
                {
                    // 广播源：GenralConfig
                    // 信息：Catalogue已完成添加
                    case "C_UPD":
                        CataloguesRefleshAll();
                        break;
                    case "C_UPD_ALB":
                        CataloguesRefleshAlbum();
                        break;
                    case "C_UPD_ART":
                        CataloguesRefleshArtist();
                        break;
                    case "C_UPD_USR":
                        CatalogueRefleshUser();
                        break;
                }
            }
        }

        #region Reflesh the list
        private void CataloguesRefleshAll()
        {
            Task.Factory.StartNew(() =>
            {
                mlp.CreateAlbumClasses();
                mlp.CreateArtistClasses();
                ByLocation = CPOOL.GetLocationClassified();
                ByAlbum = CPOOL.GetAlbumClassfied();
                ByArtist = CPOOL.GetArtistClassfied();
                ByUserDefined = CPOOL.GetUserDefined();
            });
        }

        private void CataloguesRefleshAlbum()
        {
            Task.Factory.StartNew(() =>
            {
                mlp.CreateAlbumClasses();
                ByAlbum = CPOOL.GetAlbumClassfied();
            });
        }

        private void CataloguesRefleshArtist()
        {
            Task.Factory.StartNew(() =>
            {
                mlp.CreateArtistClasses();
                ByArtist = CPOOL.GetArtistClassfied();
            });
        }

        private void CatalogueRefleshUser()
        {
            Task.Factory.StartNew(() =>
            {
                ByUserDefined = CPOOL.GetUserDefined();
                Dispatcher.Invoke(() =>
                {
                    if (CATALOGUES.TAG == CatalogueSections.USER_PLAYLISTS)
                    {
                        FPresentor.ShowContent(showcase, false, () => showcase.SetCatalogues(ByUserDefined));
                    }
                });
            });
        }
        #endregion

        public void Translate(II18NConvertor converter)
        {
            CATALOGUES.Translate(converter);
            playlistGuard.Translate(converter);
            LinearMode = converter.ConvertTo(SupportedPages.CORE_FUNC, "CORE_MAINUI_MODE_LINEAR");
            SingleLoop = converter.ConvertTo(SupportedPages.CORE_FUNC, "CORE_MAINUI_MODE_SINGLELOOP");
            ShuffleMode = converter.ConvertTo(SupportedPages.CORE_FUNC, "CORE_MAINUI_MODE_SHUFFLE");
        }

        /// <summary>
        /// 初始化Lunalipse运行时必需组件
        /// </summary>
        private void InitializeModules()
        {          
            CPOOL = CataloguePool.INSATNCE;
            core = LpsCore.Session();
            cacheSystem = CacheHub.INSTANCE(Environment.CurrentDirectory);
            mlp = MusicListPool.INSATNCE(mmdr = new MediaMetaDataReader());
            Bus = EventBus.Instance;
            playlistGuard = new PlaylistGuard();
            versionHelper = VersionHelper.Instance;

            core.OnMusicComplete += PlayFinished;
            core.OnMusicPrepared += MusicPerpeared;
            core.OnMusicProgressChanged += NotifyChanged;

            ControlPanel.Value = 0;
            core.CurrentMusicVolume = 70;

            ControlPanel.OnProgressChanged += ControlPanel_OnProgressChanged;
            ControlPanel.OnVolumeChanged += ControlPanel_OnVolumeChanged;
            ControlPanel.OnTrigging += ControlPanel_OnTrigging;
            ControlPanel.OnProfilePictureClicked += ControlPanel_OnProfilePictureClicked;
            ControlPanel.OnModeChange += ControlPanel_OnModeChange;

            CATALOGUES.OnSelectionChange += CATALOGUES_OnSelectionChange;
            CATALOGUES.OnMenuButtonClicked += CATALOGUES_OnMenuButtonClicked;
            CATALOGUES.OnConfigClicked += CATALOGUES_OnConfigClicked;

            showcase = new CatalogueShowCase();
            showcase.CatalogueSelected += Showcase_CatalogueSelected;

            musicList = new MusicSelected();
            musicList.OnSelectedMusicChange += MusicList_OnSelectedMusicChange;

            this.OnMinimizClicked += MainWindow_OnMinimizClicked;
        }

        private void ControlPanel_OnModeChange(PlayMode mode, object append)
        {
            core.MusicPlayMode = mode;
            switch(mode)
            {
                case PlayMode.RepeatList:
                    DesktopDisplay.ShowToast(LinearMode, 5);
                    break;
                case PlayMode.RepeatOne:
                    DesktopDisplay.ShowToast(SingleLoop, 5);
                    break;
                case PlayMode.Shuffle:
                    DesktopDisplay.ShowToast(ShuffleMode, 5);
                    break;
            }
        }

        private void CATALOGUES_OnConfigClicked()
        {
            //TestPage tp = new TestPage();
            //tp.Show();
            Settings settings = new Settings();
            settings.ShowDialog();
        }

        private void CATALOGUES_OnMenuButtonClicked()
        {
            if (CATALOGUES.Width < sliderSize)
            {
                CATALOGUES.BeginAnimation(WidthProperty, ExpandPanel);
            }
            else
            {
                CATALOGUES.BeginAnimation(WidthProperty, MinimizePanel);
            }
        }

        private void MainWindow_OnMinimizClicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ControlPanel_OnProfilePictureClicked()
        {
            if(core.AudioOut.Playing)
            {
                FPresentor.ShowContent(new MusicDetail(core.CurrentPlaying, ControlPanel.AlbumProfile));
            }
        }

        private void MusicList_OnSelectedMusicChange(MusicEntity arg1, object arg2)
        {
            if (core.CurrentPlaying == arg1) return;
            core.SetCatalogue(musicList.SelectedCatalogue);
            core.PerpareMusic(arg1);
        }

        private void Showcase_CatalogueSelected(Catalogue obj)
        {
            if (obj == null) return;
            FPresentor.ShowContent(musicList,false, () => musicList.SetCatalogue(obj));
        }

        /// <summary>
        /// 侧边栏音乐归类选择列表选项变化回调事件
        /// </summary>
        /// <param name="selected">选择的归类</param>
        /// <param name="tag">附加参数</param>
        private void CATALOGUES_OnSelectionChange(CatalogueSections tag)
        {
            CatalogueSections TAG = tag;
            switch (TAG)
            {
                case CatalogueSections.BY_LOCATION:
                    FPresentor.ShowContent(showcase, false, () => showcase.SetCatalogues(ByLocation));
                    break;
                case CatalogueSections.USER_PLAYLISTS:
                    FPresentor.ShowContent(showcase, false, () => showcase.SetCatalogues(ByUserDefined));
                    break;
                case CatalogueSections.ALBUM_COLLECTIONS:
                    FPresentor.ShowContent(showcase, false, () => showcase.SetCatalogues(ByAlbum));
                    break;
                case CatalogueSections.ARTIST_COLLECTIONS:
                    FPresentor.ShowContent(showcase, false, () => showcase.SetCatalogues(ByArtist));
                    break;
                case CatalogueSections.ALL_MUSIC:
                    FPresentor.ShowContent(musicList, false, () => musicList.SetCatalogue(CPOOL.MainCatalogue));
                    break;
            }
        }

        /// <summary>
        /// 音量更改回调事件
        /// </summary>
        /// <param name="value">修改的音量</param>
        private void ControlPanel_OnVolumeChanged(double value)
        {
            core.CurrentMusicVolume = (float)value;
            GlobalSetting.Volume = core.CurrentMusicVolume;
        }

        /// <summary>
        /// 进度非自然更改回调事件
        /// </summary>
        /// <param name="value"></param>
        private void ControlPanel_OnProgressChanged(double value)
        {
            core.PositionMoveTo(value);
        }

        /// <summary>
        /// 音乐控制面板开关选项发生状态改变触发事件
        /// </summary>
        /// <param name="identifier">改变的开关选项</param>
        /// <param name="args">附加参数</param>
        private void ControlPanel_OnTrigging(AudioPanelTrigger identifier, object args)
        {
            if (!core.AudioOut.isLoaded)
            {
                if (core.currentCatalogue == null && musicList.SelectedCatalogue != null)
                {
                    core.SetCatalogue(musicList.SelectedCatalogue);
                }
                else return;
            }
            switch (identifier)
            {
                case AudioPanelTrigger.PausePlay:
                    bool isPaused = (bool)args;
                    if(core.CurrentPlaying==null)
                    {
                        core.GetNext();
                    }
                    else
                    {
                        if (isPaused) core.Pause();
                        else core.Resume();
                    }
                    break;
                case AudioPanelTrigger.SkipNext:
                    core.GetNext();
                    break;
                case AudioPanelTrigger.SkipPrev:
                    core.PerpareMusic(core.currentCatalogue.getPrevious());
                    break;
                case AudioPanelTrigger.Lyric:
                    Bus.Unicast(
                        GlobalSetting.LyricEnabled ?
                            EventBusTypes.ON_ACTION_REQ_DISABLE :
                            EventBusTypes.ON_ACTION_REQ_ENABLE,
                        typeof(DesktopDisplay), "lyric");
                    GlobalSetting.LyricEnabled = !GlobalSetting.LyricEnabled;
                    break;
            }
        }

        /// <summary>
        /// 当音乐播放进度更改时（由<see cref="LpsAudio"/>.CountTimerDelegate定时触发）
        /// </summary>
        /// <param name="curPos"></param>
        private void NotifyChanged(TimeSpan curPos)
        {
            Dispatcher.Invoke(() =>
            {
                ControlPanel.Value = curPos.TotalSeconds;
                ControlPanel.Current = curPos;
            });
        }

        /// <summary>
        /// 告知当音乐已经准备好播放（加载完成）
        /// </summary>
        /// <param name="Music">音乐实体</param>
        /// <param name="mTrack">音轨信息</param>
        private void MusicPerpeared(MusicEntity Music, Track mTrack)
        {
            Dispatcher.Invoke(() =>
            {
                BitmapSource source;
                ControlPanel.AlbumProfile = (source = MediaMetaDataReader.GetPicture(Music.Path)) == null ? null : new ImageBrush(source);
                ControlPanel.StartPlaying();
                ControlPanel.MaxValue = mTrack.Duration.TotalSeconds;
                ControlPanel.Value = 0;
                musicList.PlayingIndex = musicList.SelectedCatalogue.CurrentIndex;
                ControlPanel.CurrentMusic = Music.Artist[0] +" - "+ Music.Name;
                ControlPanel.TotalLength = mTrack.Duration;
                AudioDelegations.LyricUpdated?.Invoke(null);
            });
        }

        /// <summary>
        /// 歌曲目录选项更改事件，当用户人为选定歌曲时触发
        /// </summary>
        /// <param name="selected">选择的音乐实体</param>
        /// <param name="tag">附加信息</param>
        private void DipMusic_ItemSelectionChanged(MusicEntity selected, object tag)
        {
            #region disposed
            //if (dia == null)
            //{
            //    dia = new Dialogue(new _3DVisualize(), "3D");
            //    dia.Show();
            //}
            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CATALOGUES.SelectedIndex = -1;
            LunalipseLogger.GetLogger().Info("Start Crossing Window");
            desktopDisplay = new DesktopDisplay();
            desktopDisplay.Show();
            LunalipseLogger.GetLogger().Info("Loaded complete, rendering UI");
#if BUILD
            this.SetVersion(versionHelper.getGenerationTypedVersion(LunalipseGeneration.Build));
#elif ALPHA
            this.SetVersion(versionHelper.getGenerationTypedVersion(LunalipseGeneration.Alpha));
#elif BETA
            this.SetVersion(versionHelper.getGenerationTypedVersion(LunalipseGeneration.Beta));
#elif RELEASE
            this.SetVersion(versionHelper.getGenerationTypedVersion(LunalipseGeneration.Release));
#endif

        }


        private void Window_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            desktopDisplay.Close();
            LunalipseLogger.GetLogger().Debug("Save Playlist");
            playlistGuard.SavePlaylist();
            LunalipseLogger.GetLogger().Info("Terminating Lunalipse.....");
            core.Dispose();
            LunalipseLogger.GetLogger().Release();
        }

        /// <summary>
        /// 播放完成时的回调方法
        /// </summary>
        private void PlayFinished()
        {
            //Dispatcher.Invoke(() => dipMusic.SelectedIndex = core.getCurrentPlayingIndex);
        }
    }
}
