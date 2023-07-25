using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CyberPlayer.Player.ViewModels;
using DynamicData.Binding;

namespace CyberPlayer.Player.Controls;

public class ContentPopup : ContentControl
{
    public static readonly StyledProperty<double> PopupSizeProperty = AvaloniaProperty.Register<ContentPopup, double>(
        nameof(PopupSize), defaultValue: double.NaN);
    
    public double PopupSize
    {
        get => GetValue(PopupSizeProperty);
        set => SetValue(PopupSizeProperty, value);
    }

    private TimeSpan _animationDuration = TimeSpan.FromSeconds(0.1);

    public static readonly DirectProperty<ContentPopup, TimeSpan> AnimationDurationProperty = AvaloniaProperty.RegisterDirect<ContentPopup, TimeSpan>(
        nameof(AnimationDuration), o => o.AnimationDuration, (o, v) => o.AnimationDuration = v);

    public TimeSpan AnimationDuration
    {
        get => _animationDuration;
        set => SetAndRaise(AnimationDurationProperty, ref _animationDuration, value);
    }

    private bool _isOpen;

    public static readonly DirectProperty<ContentPopup, bool> IsOpenProperty = AvaloniaProperty.RegisterDirect<ContentPopup, bool>(
        nameof(IsOpen), o => o.IsOpen, (o, v) => o.IsOpen = v);

    public bool IsOpen
    {
        get => _isOpen;
        set => SetAndRaise(IsOpenProperty, ref _isOpen, value);
    }

    private bool _isAnimating;

    public static readonly DirectProperty<ContentPopup, bool> IsAnimatingProperty = AvaloniaProperty.RegisterDirect<ContentPopup, bool>(
        nameof(IsAnimating), o => o.IsAnimating, (o, v) => o.IsAnimating = v);

    public new bool IsAnimating
    {
        get => _isAnimating;
        set => SetAndRaise(IsAnimatingProperty, ref _isAnimating, value);
    }

    public static readonly StyledProperty<bool> AnimateOpacityProperty = AvaloniaProperty.Register<ContentPopup, bool>(
        nameof(AnimateOpacity), defaultValue: true);

    public bool AnimateOpacity
    {
        get => GetValue(AnimateOpacityProperty);
        set => SetValue(AnimateOpacityProperty, value);
    }

    public static readonly StyledProperty<bool> CloseOnClickAwayProperty = AvaloniaProperty.Register<ContentPopup, bool>(
        nameof(CloseOnClickAway), defaultValue: false);

    public bool CloseOnClickAway
    {
        get => GetValue(CloseOnClickAwayProperty);
        set => SetValue(CloseOnClickAwayProperty, value);
    }

    public static readonly StyledProperty<double> UnderlayOpacityProperty = AvaloniaProperty.Register<ContentPopup, double>(
        nameof(UnderlayOpacity), defaultValue: 0.5);

    public double UnderlayOpacity
    {
        get => GetValue(UnderlayOpacityProperty);
        set => SetValue(UnderlayOpacityProperty, value);
    }
    
    private Size _desiredSize;
    private readonly DispatcherTimer _animationTimer;
    private int _currentAnimationTick;
    private readonly Panel _underlayPanel;
    private Panel _parentPanel;
    private bool _underlayInserted;
    private double _originalOpacity;
    private IDisposable? closeSubscription;
    
    private readonly TimeSpan _animationFrameRate = TimeSpan.FromSeconds(1 / 120.0);
    
    private int _totalAnimationTicks => (int)(_animationDuration.TotalSeconds / _animationFrameRate.TotalSeconds);

    private bool _sizeFound = true;

    //private readonly Timer _sizingTimer;

    //private double _originalOpacity;

    //private bool _firstAnimation = true;
    
    public ContentPopup()
    {
        _animationTimer = new DispatcherTimer
        {
            Interval = _animationFrameRate
        };
        
        _animationTimer.Tick += (_, _) => AnimationTick();

        _underlayPanel = new Panel
        {
            Background = Brushes.Black,
        };

        ZIndex = 1;
    }

    private void AnimationTick()
    {
        //Beginning of opening animation
        if (_isOpen && _currentAnimationTick == 0 && !_underlayInserted)
        {
            _underlayPanel.Opacity = 0;
            _parentPanel.Children.Add(_underlayPanel);
            if (_parentPanel is Grid grid)
            {
                Grid.SetColumnSpan(_underlayPanel, grid.ColumnDefinitions.Count);
                Grid.SetRowSpan(_underlayPanel, grid.RowDefinitions.Count);
            }
            _underlayInserted = true;
        }
        //End of closing animation
        else if (!_isOpen && _currentAnimationTick == 0)
        {
            _parentPanel.Children.Remove(_underlayPanel);
            _underlayInserted = false;
            
            _animationTimer.Stop();
            
            Width = 0;
            Height = 0;
            
            IsAnimating = false;
            
            return;
        }
        //End of opening animation
        else if (_isOpen && _currentAnimationTick >= _totalAnimationTicks)
        {
            _animationTimer.Stop();
            
            Width = _desiredSize.Width;
            Height = _desiredSize.Height;
            
            IsAnimating = false;
            
            return;
        }
        
        IsAnimating = true;
        
        _currentAnimationTick += _isOpen ? 1 : -1;

        var percentageAnimated = (double)_currentAnimationTick / _totalAnimationTicks;

        var quadraticEasing = new QuadraticEaseIn();
        var linearEasing = new LinearEasing();

        var newWidth = _desiredSize.Width * quadraticEasing.Ease(percentageAnimated);
        var newHeight = _desiredSize.Height * quadraticEasing.Ease(percentageAnimated);

        Width = newWidth;
        Height = newHeight;

        if (AnimateOpacity)
            Opacity = _originalOpacity * linearEasing.Ease(percentageAnimated);

        _underlayPanel.Opacity = UnderlayOpacity * quadraticEasing.Ease(percentageAnimated);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _originalOpacity = Opacity;
        
        var highestParent = GetHighestParentControl();
        if (highestParent != null)
        {
            _underlayPanel.Height = highestParent.DesiredSize.Height;
            _underlayPanel.Width = highestParent.DesiredSize.Width;
        }
        
        var parentPanel = GetHighestParentPanel();
        if (parentPanel != null)
        {
            _parentPanel = parentPanel;
        }

        if (CloseOnClickAway) //TODO add this to setter
            _underlayPanel.PointerPressed += (_, _) => this.Close();

        if (Content is Control)
        {
            var dataContext = ((Control)Content).DataContext;
            if (dataContext is IDialogContent dialogContent)
            {
                closeSubscription = dialogContent.CloseDialog.Subscribe(x =>
                {
                    if (x)
                        Close();
                });
            }
        }

        if (!double.IsNaN(PopupSize))
        {
            if (highestParent != null)
            {
                _desiredSize = new Size(highestParent.DesiredSize.Width * PopupSize,
                    highestParent.DesiredSize.Height * PopupSize);
            }
            else
            {
                throw new Exception("You have specified a Popup Size but there does not seem to be a parent control");
            }
        }
        else if (Content is Control contentControl)
        {
            if (!double.IsNaN(Width) && !double.IsNaN(Height))
            {
                _desiredSize = new Size(Width, Height) + Padding;
            }
            else if (double.IsNaN(Width) && double.IsNaN(Height) && !double.IsNaN(contentControl.Width) && !double.IsNaN(contentControl.Height))
            {
                _desiredSize = new Size(contentControl.Width, contentControl.Height) + Padding;
            }
            else if (double.IsNaN(Width) && !double.IsNaN(contentControl.Width))
            {
                _desiredSize = new Size(contentControl.Width, Height) + Padding;
            }
            else if (double.IsNaN(Height) && !double.IsNaN(contentControl.Height))
            {
                _desiredSize = new Size(Width, contentControl.Height) + Padding;
            }
            else
            {
                //Have to get desired size from render
                _sizeFound = false;
                IsVisible = false;
            }
        }
        else if (double.IsNaN(Width) || double.IsNaN(Height))
        {
            //Have to get desired size from render
            _sizeFound = false;
            IsVisible = false;
        }
        else
        {
            _desiredSize = new Size(Width, Height) + Padding;
        }
        
        //Assumes IsOpen is never set to true in xaml outside of design mode
        if (_isOpen && Design.IsDesignMode)
        {
            _currentAnimationTick = _totalAnimationTicks;
            _underlayPanel.Opacity = UnderlayOpacity;
        }
        else if (_sizeFound)
        {
            Width = 0;
            Height = 0;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        closeSubscription?.Dispose();
        base.OnUnloaded(e);
    }

    //private int _skipFirstRenderPass;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        //Debug.WriteLine(DesiredSize);

        /*if (_skipFirstRenderPass < 1)
        {
            _skipFirstRenderPass++;
        }
        else */if (!_sizeFound)
        {
            _desiredSize = DesiredSize;
            _sizeFound = true;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Width = 0;
                Height = 0;
                IsVisible = true;
                _currentAnimationTick = 0;
            });
        }
    }

    public void Open()
    {
        IsOpen = true;
        //_parentPanel.Children.Add(_underlayPanel);
        _animationTimer.Start();
    }
    
    public async Task OpenAsync()
    {
        IsOpen = true;
        _animationTimer.Start();
        IsAnimating = true;
        await this.WhenValueChanged(x => x.IsAnimating).TakeUntil(x => x == false);
    }

    public void Close()
    {
        IsOpen = false;
        _animationTimer.Start();
    }
    
    public async Task CloseAsync()
    {
        IsOpen = false;
        _animationTimer.Start();
        IsAnimating = true;
        await this.WhenValueChanged(x => x.IsAnimating).TakeUntil(x => x == false);
    }

    private Control? GetHighestParentControl()
    {
        var parentControl = Parent as Control;
        if (parentControl == null) return null;

        var returnControl = parentControl;
        
        while (parentControl != null)
        {
            parentControl = parentControl.Parent as Control;
            if (parentControl != null)
                returnControl = parentControl;
        }

        return returnControl;
    }
    
    private Panel? GetHighestParentPanel()
    {
        var parentPanel = Parent as Panel;
        if (parentPanel == null) return null;

        var returnPanel = parentPanel;
        
        while (parentPanel != null)
        {
            parentPanel = parentPanel.Parent as Panel;
            if (parentPanel != null)
                returnPanel = parentPanel;
        }

        return returnPanel;
    }
}