﻿using Lunalipse.Common.Interfaces.II18N;
using Lunalipse.Common.Generic.Catalogue;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Lunalipse.Common.Generic.Themes;
using Lunalipse.Utilities;
using Lunalipse.Common.Data;
using System.Windows.Media.Animation;

namespace Lunalipse.Presentation.LpsComponent
{
    /// <summary>
    /// CatalogueSelectionList.xaml 的交互逻辑
    /// </summary>
    public partial class CatalogueSelectionList : UserControl, ITranslatable
    {
        private int __index = -5;
        public CatalogueSections TAG { get; protected set; }

        const string UI_COMPONENT_THEME_UID = "PR_COMP_CatalogueSelectionList";

        /// <summary>
        /// 选项更改事件。参数tag的类型是<see cref="CatalogueSections"/>
        /// </summary>
        public event Action<CatalogueSections> OnSelectionChange;

        public event Action OnConfigClicked;

        public event Action OnMenuButtonClicked;

        private Duration duration = new Duration(TimeSpan.FromMilliseconds(100));
        private ColorAnimation FadeIn, FadeOut;

        public CatalogueSelectionList()
        {
            InitializeComponent();
            ThemeManagerBase.OnThemeApplying += ThemeManagerBase_OnThemeApplying;
            FadeIn = new ColorAnimation();
            FadeOut = new ColorAnimation();
            FadeIn.Duration = duration;
            FadeOut.Duration = duration;
            ThemeManagerBase_OnThemeApplying(ThemeManagerBase.AcquireSelectedTheme());
        }



        public static readonly DependencyProperty ITEM_HOVER =
            DependencyProperty.Register("CATALIST_HOVERCOLOR",
                                        typeof(Brush),
                                        typeof(CatalogueSelectionList),
                                        new PropertyMetadata(Application.Current.FindResource("ItemHoverColorDefault")));
        public static readonly DependencyProperty ITEM_UNHOVER =
            DependencyProperty.Register("CATALIST_UNHOVERCOLOR",
                                        typeof(Brush),
                                        typeof(CatalogueSelectionList),
                                        new PropertyMetadata(Application.Current.FindResource("ItemUnhoverColorDefault")));

        public Brush ItemHovered
        {
            get
            {
                return GetValue(ITEM_HOVER) as Brush;
            }
            private set
            {
                SetValue(ITEM_HOVER, value);
            }
        }
        public Brush ItemUnhovered
        {
            get
            {
                return GetValue(ITEM_UNHOVER) as Brush;
            }
            private set
            {
                SetValue(ITEM_UNHOVER, value);
            }
        }
        //public SolidColorBrush ItemUnhovered
        //{
        //    get => (SolidColorBrush)GetValue(ITEM_UNHOVER);
        //    private set => SetValue(ITEM_UNHOVER, value);
        //}

        public int SelectedIndex
        {
            get => __index;
            set
            {
                CatalogueSelectionListItem csli = null;
                if (value == -1)
                {
                    csli = MainCatalogue;
                    TAG = CatalogueSections.BY_LOCATION;
                }
                else if (value == -2)
                {
                    csli = AlbumCollection;
                    TAG = CatalogueSections.ALBUM_COLLECTIONS;
                }
                else if (value == -3)
                {
                    csli = UserPlaylist;
                    TAG = CatalogueSections.USER_PLAYLISTS;
                }
                else if (value == -4)
                {
                    csli = ArtistCollection;
                    TAG = CatalogueSections.ARTIST_COLLECTIONS;
                }
                else if (value == -5)
                {
                    csli = InternetMusic;
                    TAG = CatalogueSections.INTERNET_MUSIC;
                }
                else
                {
                    return;
                }
                __index = value;
                csli.SetSelected();
                OnSelectionChange?.Invoke(TAG);
            }
        }

        private void ThemeManagerBase_OnThemeApplying(ThemeTuple obj)
        {
            if (obj == null) return;
            Foreground = obj.Foreground;
            ItemHovered = obj.Foreground.CloneCurrentValue().SetOpacity(0.15);
            ItemUnhovered = obj.Foreground.CloneCurrentValue().SetOpacity(0);
            
        }

        private void ItemConatiner_MouseDown(object sender, MouseButtonEventArgs args)
        {
            CatalogueSelectionListItem csli = (CatalogueSelectionListItem)sender;

            if (csli != null)
            {
                if (__index != -6)
                {
                    // For Item in listbox
                    if (__index == -1)
                        MainCatalogue.SetUnselected();
                    else if (__index == -2)
                        AlbumCollection.SetUnselected();
                    else if (__index == -3)
                        UserPlaylist.SetUnselected();
                    else if (__index == -4)
                        ArtistCollection.SetUnselected();
                    else if (__index == -5)
                        InternetMusic.SetUnselected();
                }
                switch ((string)csli.Tag)
                {
                    case "ALL_LOCATION":
                        __index = -1;
                        TAG = CatalogueSections.BY_LOCATION;
                        break;
                    case "ALBUM_COLLECTION":
                        __index = -2;
                        TAG = CatalogueSections.ALBUM_COLLECTIONS;
                        break;
                    case "USER_PLAYLIST":
                        __index = -3;
                        TAG = CatalogueSections.USER_PLAYLISTS;
                        break;
                    case "ARTIST_COLLECTION":
                        __index = -4;
                        TAG = CatalogueSections.ARTIST_COLLECTIONS;
                        break;
                    case "INTERNET_MUSIC":
                        __index = -5;
                        TAG = CatalogueSections.INTERNET_MUSIC;
                        break;
                }
                csli.SetSelected();
                OnSelectionChange?.Invoke(TAG);
            }
        }

        public void Translate(II18NConvertor i8c)
        {
            MainCatalogue.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_"+MainCatalogue.Tag);
            AlbumCollection.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_" + AlbumCollection.Tag);
            UserPlaylist.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_" + UserPlaylist.Tag);
            ArtistCollection.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_" + ArtistCollection.Tag);
            InternetMusic.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_" + InternetMusic.Tag);
            ConfigEntry.CatalogueText = i8c.ConvertTo(SupportedPages.CORE_FUNC, "CORE_CATALOGUE_" + ConfigEntry.Tag);
            DetailedMenu.CatalogueText = "";
        }

        private void GenericButtonDown(object sender, MouseButtonEventArgs e)
        {
            CatalogueSelectionListItem csli = (CatalogueSelectionListItem)sender;
            switch(csli.Tag as string)
            {
                case "GENERAL_CONFIG":
                    OnConfigClicked?.Invoke();
                    break;
                case "MENU_DETAIL":
                    OnMenuButtonClicked?.Invoke();
                    break;
            }
        }
    }
}
