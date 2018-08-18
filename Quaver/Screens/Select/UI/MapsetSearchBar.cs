﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Quaver.Assets;
using Quaver.Database.Maps;
using Quaver.Graphics;
using Quaver.Graphics.Backgrounds;
using Quaver.Helpers;
using Wobble.Assets;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Transformations;
using Wobble.Graphics.UI.Buttons;
using Wobble.Graphics.UI.Form;
using Wobble.Window;
using Color = Microsoft.Xna.Framework.Color;

namespace Quaver.Screens.Select.UI
{
    public class MapsetSearchBar : ImageButton
    {
        /// <summary>
        ///     Reference to the select screen.
        /// </summary>
        public SelectScreen Screen { get; }

        /// <summary>
        ///     Reference to the select ScreenView.
        /// </summary>
        public SelectScreenView ScreenView { get; }

        /// <summary>
        ///     The actual textbox to start searching in.
        /// </summary>
        public Textbox SearchBox { get; private set; }

        /// <summary>
        ///     The little search icon to the right of the text box.
        /// </summary>
        private Sprite SearchIcon { get; set; }

        /// <summary>
        ///     Creates the divider line
        /// </summary>
        private Sprite DividerLine { get; set; }

        /// <summary>
        ///     Displays the amount of mapsets available.
        /// </summary>
        private SpriteText SetsAvailableText { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="view"></param>
        public MapsetSearchBar(SelectScreen screen, SelectScreenView view) : base(UserInterface.BlankBox)
        {
            Screen = screen;
            ScreenView = view;

            Size = new ScalableVector2(585, 80);
            Alignment = Alignment.TopRight;
            Y = ScreenView.Toolbar.Y + ScreenView.Toolbar.Height + 1;
            X = 1;
            Tint = Color.Black;
            Alpha = 0.45f;

            CreateSearchBox();
            CreateSearchIcon();
            CreateDividerLine();
            CreateSetsAvailableText();
        }

        /// <summary>
        ///     Creates the box to search for mapsets.
        /// </summary>
        private void CreateSearchBox()
        {
            // ReSharper disable once ArrangeMethodOrOperatorBody
            SearchBox = new Textbox(TextboxStyle.SingleLine, new ScalableVector2(550, 30), Fonts.Exo2Regular24,
                SelectScreen.PreviousSearchTerm, "Start typing to search...", 0.60f, null, text =>
                {
                    // Update previous search term
                    SelectScreen.PreviousSearchTerm = text;

                    // Search for new mapsets.
                    var sets = !string.IsNullOrEmpty(text) ? MapsetHelper.SearchMapsets(MapManager.Mapsets, text) : MapManager.Mapsets;

                    ReadjustSetsAvailableText(sets);

                    // Don't continue if there aren't any mapsets.
                    if (sets.Count == 0)
                        return;

                    // Set the new available sets, and reinitialize the mapset buttons.
                    Screen.AvailableMapsets = sets;
                    ScreenView.MapsetContainer.InitializeMapsetButtons();

                    // Check to see if the current mapset is already in the new search.
                    var foundMapset = sets.FindIndex(x => x == MapManager.Selected.Value.Mapset);

                    // If the new map is in the search, go straight to it.
                    if (foundMapset != -1)
                    {
                        ScreenView.MapsetContainer.SelectMap(foundMapset, MapManager.Selected.Value, false, true);
                        ChangeMapsetButtonThumbnail();
                    }
                    // Select the first map in the first mapset, if it's a completely new mapset.
                    else if (MapManager.Selected.Value != Screen.AvailableMapsets.First().Maps.First())
                        ScreenView.MapsetContainer.SelectMap(0, Screen.AvailableMapsets.First().Maps.First(), true, true);
                    // Otherwise just make sure the mapset thumbnail is up to date anyway.
                    else
                        ChangeMapsetButtonThumbnail();
                })
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                Y = 10,
                X = -20,
                Image = UserInterface.SearchBar,
                AlwaysFocused = true,
                StoppedTypingActionCalltime = 300
            };
        }

        /// <summary>
        ///     Creates the search icon at the right of the search box.
        /// </summary>
        private void CreateSearchIcon()
        {
            // ReSharper disable once ArrangeMethodOrOperatorBody
            SearchIcon = new Sprite()
            {
                Parent = SearchBox,
                Alignment = Alignment.MidRight,
                Image = FontAwesome.Search,
                Size = new ScalableVector2(SearchBox.Height * 0.50f, SearchBox.Height * 0.50f),
                Tint = Color.White,
                X = -10
            };
        }

        /// <summary>
        ///     Creates the divider line sprite under the search box.
        /// </summary>
        private void CreateDividerLine() => DividerLine = new Sprite
        {
            Parent = SearchBox,
            Size = new ScalableVector2(SearchBox.Width, 1),
            Tint = Color.White,
            Alignment = Alignment.TopCenter,
            Y = SearchBox.Height + SearchBox.Y + 2
        };

        /// <summary>
        ///     Creates the sets available text.
        /// </summary>
        private void CreateSetsAvailableText()
        {
            SetsAvailableText = new SpriteText(Fonts.Exo2Regular24, $"Found {Screen.AvailableMapsets.Count} mapsets.")
            {
                Parent = DividerLine,
                TextColor = Color.White,
                TextScale = 0.45f,
                Transformations =
                {
                    new Transformation(Easing.Linear, Color.White, Colors.MainAccent, 500)
                }
            };

            ReadjustSetsAvailableText(Screen.AvailableMapsets);
        }

        /// <summary>
        ///    Adjusts the sets available text
        /// </summary>
        private void ReadjustSetsAvailableText(IReadOnlyCollection<Mapset> sets)
        {
            // Initially change color to white.
            SetsAvailableText.TextColor = Color.White;

            Color newColor;

            if (sets.Count > 0)
            {
                SetsAvailableText.Text = $"Found {sets.Count} mapsets";
                newColor = Colors.MainAccent;
            }
            else
            {
                SetsAvailableText.Text = "No mapsets found :(";
                newColor = Colors.Negative;
            }

            // Readjust position.
            var size = SetsAvailableText.MeasureString() / 2f;
            SetsAvailableText.X = size.X;
            SetsAvailableText.Y = DividerLine.Height + 3 + size.Y;

            // Fade to the new color.
            SetsAvailableText.Transformations.Clear();
            SetsAvailableText.Transformations.Add(new Transformation(Easing.Linear, Color.White, newColor, 500));
        }

        /// <summary>
        ///     Makes sure the mapset button's thumbnail is up to date with the newly selected map.
        /// </summary>
        private void ChangeMapsetButtonThumbnail()
        {
            var thumbnail = ScreenView.MapsetContainer.MapsetButtons[ScreenView.MapsetContainer.SelectedMapsetIndex].Thumbnail;

            thumbnail.Image = BackgroundManager.Background.Sprite.Image;
            thumbnail.Transformations.Clear();
            var t = new Transformation(TransformationProperty.Alpha, Easing.Linear, 0, 1, 250);
            thumbnail.Transformations.Add(t);
        }
    }
}