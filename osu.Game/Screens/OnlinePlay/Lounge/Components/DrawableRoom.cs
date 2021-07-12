// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Online.Rooms;
using osu.Game.Screens.OnlinePlay.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Lounge.Components
{
    public class DrawableRoom : OsuClickableContainer, IStateful<SelectionState>, IFilterable, IHasContextMenu, IHasPopover, IKeyBindingHandler<GlobalAction>
    {
        public const float SELECTION_BORDER_WIDTH = 4;
        private const float corner_radius = 5;
        private const float transition_duration = 60;
        private const float content_padding = 10;
        private const float height = 110;
        private const float side_strip_width = 5;
        private const float cover_width = 145;

        public event Action<SelectionState> StateChanged;

        private readonly Box selectionBox;

        [Resolved(canBeNull: true)]
        private OnlinePlayScreen parentScreen { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<Room> selectedRoom { get; set; }

        [Resolved(canBeNull: true)]
        private LoungeSubScreen lounge { get; set; }

        private Container content;

        public readonly Room Room;

        private SelectionState state;

        public SelectionState State
        {
            get => state;
            set
            {
                if (value == state) return;

                state = value;

                if (state == SelectionState.Selected)
                    selectionBox.FadeIn(transition_duration);
                else
                    selectionBox.FadeOut(transition_duration);

                StateChanged?.Invoke(State);
            }
        }

        public IEnumerable<string> FilterTerms => new[] { Room.Name.Value };

        private bool matchingFilter;

        public bool MatchingFilter
        {
            get => matchingFilter;
            set
            {
                matchingFilter = value;

                if (!IsLoaded)
                    return;

                if (matchingFilter)
                    this.FadeIn(200);
                else
                    Hide();
            }
        }

        public bool FilteringActive { get; set; }

        public DrawableRoom(Room room)
        {
            Room = room;

            RelativeSizeAxes = Axes.X;
            Height = height + SELECTION_BORDER_WIDTH * 2;
            CornerRadius = corner_radius + SELECTION_BORDER_WIDTH / 2;
            Masking = true;

            // create selectionBox here so State can be set before being loaded
            selectionBox = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0f,
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            float stripWidth = side_strip_width * (Room.Category.Value == RoomCategory.Spotlight ? 2 : 1);

            Children = new Drawable[]
            {
                new StatusColouredContainer(transition_duration)
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = selectionBox
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(SELECTION_BORDER_WIDTH),
                    Child = content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = corner_radius,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Colour = Color4.Black.Opacity(40),
                            Radius = 5,
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4Extensions.FromHex(@"212121"),
                            },
                            new StatusColouredContainer(transition_duration)
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = stripWidth,
                                Child = new Box { RelativeSizeAxes = Axes.Both }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = cover_width,
                                Masking = true,
                                Margin = new MarginPadding { Left = stripWidth },
                                Child = new OnlinePlayBackgroundSprite(BeatmapSetCoverType.List) { RelativeSizeAxes = Axes.Both }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding
                                {
                                    Vertical = content_padding,
                                    Left = stripWidth + cover_width + content_padding,
                                    Right = content_padding,
                                },
                                Children = new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5f),
                                        Children = new Drawable[]
                                        {
                                            new RoomName { Font = OsuFont.GetFont(size: 18) },
                                            new ParticipantInfo(),
                                        },
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.BottomLeft,
                                        Origin = Anchor.BottomLeft,
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 5),
                                        Children = new Drawable[]
                                        {
                                            new RoomStatusInfo(),
                                            new BeatmapTitle { TextSize = 14 },
                                        },
                                    },
                                    new ModeTypeInfo
                                    {
                                        Anchor = Anchor.BottomRight,
                                        Origin = Anchor.BottomRight,
                                    },
                                },
                            },
                        },
                    },
                },
            };

            if (Room.HasPassword.Value)
            {
                content.Add(new PasswordProtectedIcon());
            }
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            return new CachedModelDependencyContainer<Room>(base.CreateChildDependencies(parent))
            {
                Model = { Value = Room }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (matchingFilter)
                this.FadeInFromZero(transition_duration);
            else
                Alpha = 0;
        }

        public bool OnPressed(GlobalAction action)
        {
            switch (action)
            {
                case GlobalAction.Select:
                    // TODO: this needs to be able to show the popover on demand.
                    return true;
            }

            return false;
        }

        public void OnReleased(GlobalAction action)
        {
        }

        protected override bool ShouldBeConsideredForInput(Drawable child) => state == SelectionState.Selected;

        private class RoomName : OsuSpriteText
        {
            [Resolved(typeof(Room), nameof(Online.Rooms.Room.Name))]
            private Bindable<string> name { get; set; }

            [BackgroundDependencyLoader]
            private void load()
            {
                Current = name;
            }
        }

        public MenuItem[] ContextMenuItems => new MenuItem[]
        {
            new OsuMenuItem("Create copy", MenuItemType.Standard, () =>
            {
                parentScreen?.OpenNewRoom(Room.CreateCopy());
            })
        };

        private class PasswordProtectedIcon : CompositeDrawable
        {
            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Anchor = Anchor.TopRight;
                Origin = Anchor.TopRight;

                Size = new Vector2(32);

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopCentre,
                        Colour = colours.Gray5,
                        Rotation = 45,
                        RelativeSizeAxes = Axes.Both,
                        Width = 2,
                    },
                    new SpriteIcon
                    {
                        Icon = FontAwesome.Solid.Lock,
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Margin = new MarginPadding(6),
                        Size = new Vector2(14),
                    }
                };
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (selectedRoom.Value != Room)
                return true;

            return base.OnMouseDown(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Room != selectedRoom.Value)
            {
                selectedRoom.Value = Room;
                return true;
            }

            if (Room.HasPassword.Value)
                // we want our popover to show. this is a bit of a hack.
                return false;

            lounge?.Join(Room, null);

            return base.OnClick(e);
        }

        public Popover GetPopover() => new PasswordEntryPopover(Room);

        public class PasswordEntryPopover : Popover
        {
            [Resolved(canBeNull: true)]
            private LoungeSubScreen lounge { get; set; }

            public PasswordEntryPopover(Room room)
            {
                OsuPasswordTextBox passwordTextbox;

                Child = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.OliveDrab,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new FillFlowContainer
                        {
                            Margin = new MarginPadding(10),
                            Spacing = new Vector2(5),
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                passwordTextbox = new OsuPasswordTextBox
                                {
                                    Width = 200,
                                },
                                new TriangleButton
                                {
                                    Width = 80,
                                    Text = "Join Room",
                                    Action = () => lounge?.Join(room, passwordTextbox.Text)
                                }
                            }
                        },
                    }
                };
            }

            protected override Drawable CreateArrow() => Drawable.Empty();
        }
    }
}
