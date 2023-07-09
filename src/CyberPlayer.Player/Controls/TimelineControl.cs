using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CyberPlayer.Player.Controls
{
    public class TimelineControl : TemplatedControl
    {
        public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(Minimum), 0d);

        public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(Maximum), 1d);

        public static readonly StyledProperty<double> LowerValueProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(LowerValue),  0d, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<double> UpperValueProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(UpperValue), 1d, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<double> SeekValueProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(SeekValue), 0.5d, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsLowerDraggingProperty = AvaloniaProperty.Register<TimelineControl, bool>(
            nameof(IsLowerDragging), defaultBindingMode: BindingMode.OneWayToSource);

        public static readonly StyledProperty<bool> IsUpperDraggingProperty = AvaloniaProperty.Register<TimelineControl, bool>(
            nameof(IsUpperDragging), defaultBindingMode: BindingMode.OneWayToSource);

        public static readonly StyledProperty<bool> IsSeekDraggingProperty = AvaloniaProperty.Register<TimelineControl, bool>(
            nameof(IsSeekDragging), defaultBindingMode: BindingMode.OneWayToSource);

        public static readonly StyledProperty<double> SnapThresholdProperty = AvaloniaProperty.Register<TimelineControl, double>(
            nameof(SnapThreshold), double.NaN);

        public double SnapThreshold
        {
            get => GetValue(SnapThresholdProperty);
            set => SetValue(SnapThresholdProperty, value);
        }

        public bool IsSeekDragging
        {
            get => GetValue(IsSeekDraggingProperty);
            set => SetValue(IsSeekDraggingProperty, value);
        }

        public double SeekValue
        {
            get => GetValue(SeekValueProperty);
            set => SetValue(SeekValueProperty, value);
        }
        
        public bool IsUpperDragging
        {
            get => GetValue(IsUpperDraggingProperty);
            set => SetValue(IsUpperDraggingProperty, value);
        }

        public bool IsLowerDragging
        {
            get => GetValue(IsLowerDraggingProperty);
            set => SetValue(IsLowerDraggingProperty, value);
        }

        public double UpperValue
        {
            get => GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }

        public double LowerValue
        {
            get => GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        private CustomSlider _lowerSlider;
        private CustomSlider _upperSlider;
        private Thumb _lowerThumb;
        private Thumb _upperThumb;
        private Button _selectionPart;
        private IDisposable _lowerThumbDragCompleteDispose;
        private IDisposable _upperThumbDragCompleteDispose;
        private IDisposable _selectionPartPressDispose;
        private IDisposable _selectionPartReleaseDispose;
        private IDisposable _pointerMovedDispose;
        private double _xOffsetLower;
        private double _xOffsetUpper;
        private bool _isSnapping;

        private bool _selectionDragging; //MAY NEED TO MAKE THIS A PROPERTY TO ACCESS IN MAIN WINDOW VIEWMODEL

        static TimelineControl()
        {
            /*Thumb.DragStartedEvent.AddClassHandler<TimelineControl>((x, e) => x.OnThumbDragStarted(e),
                RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<TimelineControl>((x, e) => x.OnThumbDragCompleted(e),
                RoutingStrategies.Bubble);*/
            Thumb.DragDeltaEvent.AddClassHandler<TimelineControl>((x, e) => x.OnThumbDelta(e),
                RoutingStrategies.Bubble);
        }
        
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            _lowerSlider = e.NameScope.Find<CustomSlider>("SliderLower");
            _lowerSlider.TemplateApplied += (_, t) =>
            {
                var lowerSliderTrackPart = t.NameScope.Find<Track>("PART_Track");
                _lowerThumb = lowerSliderTrackPart.Thumb;
                _lowerThumbDragCompleteDispose?.Dispose();
                _lowerThumbDragCompleteDispose = _lowerThumb.AddDisposableHandler(PointerReleasedEvent,
                    OnThumbDragCompleted, RoutingStrategies.Tunnel);
                
                _selectionPart = t.NameScope.Find<RepeatButton>("SelectionPart");
                _selectionPartPressDispose?.Dispose();
                _selectionPartReleaseDispose?.Dispose();
                _selectionPartPressDispose =
                    _selectionPart.AddDisposableHandler(PointerPressedEvent, SelectionPressed, RoutingStrategies.Tunnel);
                _selectionPartReleaseDispose =
                    _selectionPart.AddDisposableHandler(PointerReleasedEvent, SelectionRelease,
                        RoutingStrategies.Tunnel);
            };
            
            _upperSlider = e.NameScope.Find<CustomSlider>("SliderUpper");
            _upperSlider.TemplateApplied += (_, t) =>
            {
                var upperSliderTrackPart = t.NameScope.Find<Track>("PART_Track");
                _upperThumb = upperSliderTrackPart.Thumb;
                _upperThumbDragCompleteDispose?.Dispose();
                _upperThumbDragCompleteDispose = _upperThumb.AddDisposableHandler(PointerReleasedEvent,
                    OnThumbDragCompleted, RoutingStrategies.Tunnel);
            };
            
            _pointerMovedDispose?.Dispose();
            _pointerMovedDispose =
                this.AddDisposableHandler(PointerMovedEvent, PointerSelectionMoved, RoutingStrategies.Tunnel);
        }

        /*private void OnThumbDragStarted(VectorEventArgs e)
        {
            //this will be called on main seek slider as well as trim points since it uses static declaration///////////////////////////////////////////////////////////////////////////////
            Debug.WriteLine("START");
        }*/

        private void OnThumbDragCompleted(object? sender, PointerReleasedEventArgs e)
        {
            ArrangeSelection();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyModifiers & KeyModifiers.Shift) == 0) return;
            if (!_isSnapping)
                _isSnapping = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (_isSnapping)
                _isSnapping = false;
        }

        private void OnThumbDelta(VectorEventArgs e)
        {
            if (_isSnapping && !double.IsNaN(SnapThreshold))
            {
                if (IsUpperDragging)
                {
                    if (SeekValue < UpperValue && UpperValue < SeekValue + SnapThreshold)
                        UpperValue = SeekValue;
                    else if (SeekValue > UpperValue && UpperValue > SeekValue - SnapThreshold)
                        UpperValue = SeekValue;
                }
                else if (IsLowerDragging)
                {
                    if (SeekValue < LowerValue && LowerValue < SeekValue + SnapThreshold)
                        LowerValue = SeekValue;
                    else if (SeekValue > LowerValue && LowerValue > SeekValue - SnapThreshold)
                        LowerValue = SeekValue;
                }
                else if (IsSeekDragging)
                {
                    if (Math.Abs(SeekValue - LowerValue) > Math.Abs(SeekValue - UpperValue))
                    {
                        //Snap to upper value
                        if (UpperValue < SeekValue && SeekValue < UpperValue + SnapThreshold)
                            SeekValue = UpperValue;
                        else if (UpperValue > SeekValue && SeekValue > UpperValue - SnapThreshold)
                            SeekValue = UpperValue;
                    }
                    else
                    {
                        //Snap to lower value
                        if (LowerValue < SeekValue && SeekValue < LowerValue + SnapThreshold)
                            SeekValue = LowerValue;
                        else if (LowerValue > SeekValue && SeekValue > LowerValue - SnapThreshold)
                            SeekValue = LowerValue;
                    }
                }
            }
            ArrangeSelection();
        }

        private void SelectionPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _xOffsetLower = e.GetCurrentPoint(null).Position.X - _lowerThumb.Bounds.X;
                _xOffsetUpper = _upperThumb.Bounds.X - e.GetCurrentPoint(null).Position.X;
                _selectionDragging = true;
            }
        }
        
        private void SelectionRelease(object? sender, PointerReleasedEventArgs e)
        {
            _selectionDragging = false;
            ArrangeSelection();
        }

        private void PointerSelectionMoved(object? sender, PointerEventArgs e)
        {
            if (!IsEnabled)
            {
                if (_selectionDragging)
                    _selectionDragging = false;
                return;
            }
            if (_selectionDragging)
            {
                MoveSelection(e.GetCurrentPoint(null).Position);
            }
        }

        private void MoveSelection(Point p)
        {
            _lowerSlider.MoveThumb(p.WithX(p.X - _xOffsetLower));
            _upperSlider.MoveThumb(p.WithX(p.X + _xOffsetUpper));
            
            ArrangeSelection();
        }

        private void ArrangeSelection()
        {
            var width = _upperThumb.Bounds.X - _lowerThumb.Bounds.X;
            var x = _lowerThumb.Bounds.X + _lowerThumb.Bounds.Width / 2;
            var rect = new Rect(_selectionPart.Bounds.Position.WithX(x), _selectionPart.Bounds.Size.WithWidth(width));
            _selectionPart.Arrange(rect);
        }
    }
}
