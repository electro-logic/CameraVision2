// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CameraVision;

public class ZoomBorder : Border
{
    UIElement _child;
    Point _origin, _start, _zoomPoint;
    TranslateTransform _tt;
    ScaleTransform _st;

    public double ZoomMinimum
    {
        get { return (double)GetValue(ZoomMinimumProperty); }
        set { SetValue(ZoomMinimumProperty, value); }
    }
    public static readonly DependencyProperty ZoomMinimumProperty = DependencyProperty.Register(nameof(ZoomMinimum), typeof(double), typeof(ZoomBorder), new PropertyMetadata(0.25));

    public double ZoomMaximum
    {
        get { return (double)GetValue(ZoomMaximumProperty); }
        set { SetValue(ZoomMaximumProperty, value); }
    }
    public static readonly DependencyProperty ZoomMaximumProperty = DependencyProperty.Register(nameof(ZoomMaximum), typeof(double), typeof(ZoomBorder), new PropertyMetadata(10.0));

    public double ZoomLevel
    {
        get { return (double)GetValue(ZoomLevelProperty); }
        set { SetValue(ZoomLevelProperty, value); }
    }
    public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(ZoomBorder), new PropertyMetadata(1.0, OnZoomLevelChanged));
    static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as ZoomBorder).Zoom((double)e.NewValue, false);

    public bool CanPan
    {
        get { return (bool)GetValue(CanPanProperty); }
        set { SetValue(CanPanProperty, value); }
    }
    public static readonly DependencyProperty CanPanProperty = DependencyProperty.Register(nameof(CanPan), typeof(bool), typeof(ZoomBorder), new PropertyMetadata(true));

    public override UIElement Child
    {
        get { return base.Child; }
        set
        {
            if (value != null && value != this.Child)
            {
                Initialize(value);
            }
            base.Child = value;
        }
    }

    void Initialize(UIElement element)
    {
        _child = element;
        var group = new TransformGroup();
        _st = new ScaleTransform();
        group.Children.Add(_st);
        _tt = new TranslateTransform();
        group.Children.Add(_tt);
        _child.RenderTransform = group;
        _child.RenderTransformOrigin = new Point(0.0, 0.0);
        MouseWheel += child_MouseWheel;
        MouseLeftButtonDown += child_MouseLeftButtonDown;
        MouseLeftButtonUp += child_MouseLeftButtonUp;
        MouseMove += child_MouseMove;
        //PreviewMouseRightButtonDown += new MouseButtonEventHandler(child_PreviewMouseRightButtonDown);
    }
    public void Reset()
    {
        _st.ScaleX = _st.ScaleY = 1.0;
        _tt.X = _tt.Y = 0.0;
    }
    public void Zoom(double zoomLevel, bool updateZoomLevel = true)
    {
        if (zoomLevel < ZoomMinimum)
            zoomLevel = ZoomMinimum;

        if (zoomLevel > ZoomMaximum)
            zoomLevel = ZoomMaximum;

        double absoluteX = _zoomPoint.X * _st.ScaleX + _tt.X;
        double absoluteY = _zoomPoint.Y * _st.ScaleY + _tt.Y;
        _st.ScaleX = _st.ScaleY = zoomLevel;
        _tt.X = absoluteX - _zoomPoint.X * _st.ScaleX;
        _tt.Y = absoluteY - _zoomPoint.Y * _st.ScaleY;
        if (updateZoomLevel)
        {
            ZoomLevel = zoomLevel;
        }
    }
    void child_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _zoomPoint = e.GetPosition(_child);
        double zoomFactor = e.Delta > 0 ? 1.25 : 0.8;
        Zoom(ZoomLevel * zoomFactor);
    }
    void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanPan)
            return;
        _start = e.GetPosition(this);
        _origin = new Point(_tt.X, _tt.Y);
        Cursor = Cursors.Hand;
        _child.CaptureMouse();
    }
    void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!CanPan)
            return;
        _child.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }
    void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) => Reset();
    void child_MouseMove(object sender, MouseEventArgs e)
    {
        if (!CanPan)
            return;
        if (_child.IsMouseCaptured)
        {
            Vector v = _start - e.GetPosition(this);
            _tt.X = _origin.X - v.X;
            _tt.Y = _origin.Y - v.Y;
        }
    }
}