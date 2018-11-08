﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Quaver.Database.Maps;
using Quaver.Database.Scores;
using Quaver.Graphics;
using Quaver.Resources;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Input;
using Wobble.Logging;

namespace Quaver.Screens.SongSelect.UI.Leaderboard
{
    public abstract class LeaderboardScoreSection : ScrollContainer
    {
        /// <summary>
        ///     The type of leaderboard section this is.
        /// </summary>
        public abstract LeaderboardType Type { get; }

        /// <summary>
        ///     Reference to the parent leaderboard.
        /// </summary>
        protected LeaderboardContainer Leaderboard { get; }

        /// <summary>
        ///     Dictates if the section is currently fetching.
        /// </summary>
        public bool IsFetching { get; set; }

        /// <summary>
        ///     The wheel that displays the at the section is currently loading.
        /// </summary>
        private Sprite LoadingWheel { get; set; }

        /// <summary>
        ///     The scores that are displayed.
        /// </summary>
        private List<DrawableLeaderboardScore> Scores { get; } = new List<DrawableLeaderboardScore>();

        /// <summary>
        ///     Cached scores for individual maps.
        /// </summary>
        public Dictionary<Map, List<LocalScore>> ScoreCache { get; } = new Dictionary<Map, List<LocalScore>>();

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="leaderboard"></param>
        protected LeaderboardScoreSection(LeaderboardContainer leaderboard) : base(
            new ScalableVector2(leaderboard.Width, leaderboard.Height),
            new ScalableVector2(leaderboard.Width, leaderboard.Height))
        {
            Leaderboard = leaderboard;
            Alpha = 0;
            Tint = Color.CornflowerBlue;

            InputEnabled = true;
            Scrollbar.Tint = Color.White;
            Scrollbar.Width = 5;
            Scrollbar.X += 10;
            ScrollSpeed = 150;
            EasingType = Easing.OutQuint;
            TimeToCompleteScroll = 1500;

            CreateLoadingWheel();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            InputEnabled = GraphicsHelper.RectangleContains(ScreenRectangle, MouseManager.CurrentState.Position) && DialogManager.Dialogs.Count == 0;
            HandleLoadingWheelAnimations();
            base.Update(gameTime);
        }

        /// <summary>
        ///     Creates the loading wheel that is displayed when looking for new scores.
        /// </summary>
        private void CreateLoadingWheel() => LoadingWheel = new Sprite
        {
            Parent = this,
            Alignment = Alignment.MidCenter,
            Image = UserInterface.LoadingWheel,
            Size = new ScalableVector2(50, 50),
            Visible = IsFetching,
            Tint = Color.White
        };

        /// <summary>
        ///     Animates the loading wheel.
        /// </summary>
        private void HandleLoadingWheelAnimations()
        {
            LoadingWheel.Visible = IsFetching;

            if (LoadingWheel.Animations.Count != 0)
                return;

            var rotation = MathHelper.ToDegrees(LoadingWheel.Rotation);
            LoadingWheel.Animations.Add(new Animation(AnimationProperty.Rotation, Easing.Linear, rotation, rotation + 360, 1000));
        }

        /// <summary>
        ///     Fetches scores to display on this section.
        /// </summary>
        public abstract List<LocalScore> FetchScores();

        /// <summary>
        ///     Gets a string that is displayed when no scores are available.
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public abstract string GetNoScoresAvailableString(Map map);

        /// <summary>
        ///     Clears all the scores on the leaderboard.
        /// </summary>
        public void ClearScores()
        {
            Scores.ForEach(x => x.Destroy());
            Scores.Clear();
        }

        /// <summary>
        ///     Updates the leaderboard with new scores.
        /// </summary>
        public void UpdateWithScores(Map map, List<LocalScore> scores, CancellationToken cancellationToken = default)
        {
            var newScores = new List<DrawableLeaderboardScore>();

            try
            {
                if (map != MapManager.Selected.Value)
                    return;

                cancellationToken.ThrowIfCancellationRequested();

                // Calculate the height of the scroll container based on how many scores there are.
                var totalUserHeight =  scores.Count * DrawableLeaderboardScore.HEIGHT + 10 * (scores.Count - 1);

                if (totalUserHeight > Height)
                    ContentContainer.Height = totalUserHeight;
                else
                    ContentContainer.Height = Height;

                for (var i = 0; i < scores.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var score = scores[i];

                    var drawable = new DrawableLeaderboardScore(score, i + 1)
                    {
                        Parent = this,
                        Y = i * DrawableLeaderboardScore.HEIGHT + i * 10,
                        X = -DrawableLeaderboardScore.WIDTH,
                    };

                    drawable.MoveToX(0, Easing.OutQuint, 300 + i * 50);
                    newScores.Add(drawable);
                    Scores.Add(drawable);
                    AddContainedDrawable(drawable);
                }

                // This is a hack... It's to place the leaderboard selector on top so that the
                // buttons are technically on top of the leaderboard score ones.
                Leaderboard.View.LeaderboardSelector.Parent = Leaderboard.View.Container;
            }
            catch (Exception e)
            {
                // ignored.
                newScores.ForEach(x =>
                {
                    x.Parent = null;
                    x.Destroy();
                });
                newScores = null;
            }
        }
    }
}