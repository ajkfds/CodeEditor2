using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Views
{
    public class PopupMenuFlyout : PopupFlyoutBase
    {
        //public PopupMenuFlyout():base() {
        //    // FlyoutPresenterThemeを設定
        //    this.FlyoutPresenterTheme = new ControlTheme(typeof(FlyoutPresenter)) {
        //        Setters = { 
        ////            new Setter(FlyoutPresenter.BackgroundProperty, Avalonia.Media.Brushes.LightBlue),
        ////            new Setter(FlyoutPresenter.BorderBrushProperty, Avalonia.Media.Brushes.DarkBlue),
        //            new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(2)) 
        //        } 
        //    };
        //}

        /// <summary>
        /// Defines the <see cref="Content"/> property
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<Flyout, object>(nameof(Content));

        private Classes? _classes;

        /// <summary>
        /// Gets the Classes collection to apply to the FlyoutPresenter this Flyout is hosting
        /// </summary>
        public Classes FlyoutPresenterClasses => _classes ??= new Classes();

        /// <summary>
        /// Defines the <see cref="FlyoutPresenterTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> FlyoutPresenterThemeProperty =
            AvaloniaProperty.Register<Flyout, ControlTheme?>(nameof(FlyoutPresenterTheme));

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for the flyout presenter.
        /// </summary>
        public ControlTheme? FlyoutPresenterTheme
        {
            get => GetValue(FlyoutPresenterThemeProperty);
            set => SetValue(FlyoutPresenterThemeProperty, value);
        }

        /// <summary>
        /// Gets or sets the content to display in this flyout
        /// </summary>
        [Content]
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        protected override Control CreatePresenter()
        {
            FlyoutPresenter presenter = new FlyoutPresenter
            {
                [!ContentControl.ContentProperty] = this[!ContentProperty]
            };
            return presenter;
        }


        protected override void OnOpening(CancelEventArgs args)
        {
            if (Popup.Child is { } presenter)
            {
                //if (_classes != null)
                //{
                //    SetPresenterClasses(presenter, FlyoutPresenterClasses);
                //}

                //if(FlyoutPresenterTheme == null)
                //{
                //    this.FlyoutPresenterTheme = new ControlTheme(typeof(FlyoutPresenter)) 
                //    { 
                //        Setters = { 
                //            new Setter(FlyoutPresenter.BackgroundProperty, Brushes.LightBlue),
                //            new Setter(FlyoutPresenter.BorderBrushProperty, Brushes.DarkBlue),
                //            new Setter(FlyoutPresenter.BorderThicknessProperty, new Thickness(2)) 
                //        } 
                //    };
                //}

                if (FlyoutPresenterTheme is { } theme)
                {
                    presenter.SetValue(Control.ThemeProperty, theme);
                    //presenter.SetValue(Control.MarginProperty, 0);
                    //presenter.SetValue(Control.MaxWidthProperty, 10);
                }
            }

            base.OnOpening(args);
//            VerticalOffset = 100;
//            HorizontalOffset = 100;

            View.OnOpen(args);
        }

        public PopupMenuView View
        {
            get
            {
                if (!(Popup.Child is FlyoutPresenter)) throw new Exception();
                if ((Popup.Child as FlyoutPresenter)?.Content == null) return null;
                PopupMenuView view = (Popup.Child as FlyoutPresenter).Content as PopupMenuView;
                return view;
            }
        }

        //protected override bool ShowAtCore(Control placementTarget, bool showAtPointer = false)
        //{
        //    base.ShowAtCore(placementTarget, showAtPointer);

        //    return true;
        //}
        //private void PositionPopup(bool showAtPointer)
        //{
        //    Size sz;
        //    // Popup.Child can't be null here, it was set in ShowAtCore.
        //    if (Popup.Child!.DesiredSize == default)
        //    {
        //        // Popup may not have been shown yet. Measure content
        //        sz = LayoutHelper.MeasureChild(Popup.Child, Size.Infinity, new Thickness());
        //    }
        //    else
        //    {
        //        sz = Popup.Child.DesiredSize;
        //    }

        //    Popup.VerticalOffset = VerticalOffset;
        //    Popup.HorizontalOffset = HorizontalOffset;
        //    Popup.PlacementAnchor = PlacementAnchor;
        //    Popup.PlacementGravity = PlacementGravity;
        //    if (showAtPointer)
        //    {
        //        Popup.Placement = PlacementMode.Pointer;
        //    }
        //    else
        //    {
        //        Popup.Placement = Placement;
        //        Popup.PlacementConstraintAdjustment =
        //            Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideX |
        //            Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY;
        //    }
        //}

    }
}

