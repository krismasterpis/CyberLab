using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CyberLab.Resources.Controls
{
    public class IconTile : Control
    {
        public static readonly RoutedEvent ClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(Click), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(IconTile));

        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        private bool _isPressed;
        static IconTile()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconTile), new FrameworkPropertyMetadata(typeof(IconTile)));
        }
        private void OnMouseDown(object s, MouseButtonEventArgs e)
        {
            _isPressed = true;
            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseUp(object s, MouseButtonEventArgs e)
        {
            if (_isPressed)
            {
                _isPressed = false;
                ReleaseMouseCapture();

                var pos = e.GetPosition(this);
                if (pos.X >= 0 && pos.Y >= 0 && pos.X <= ActualWidth && pos.Y <= ActualHeight)
                    RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            }
            e.Handled = true;
        }
        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(Geometry), typeof(IconTile), new PropertyMetadata(null));




        public int IconWidth
        {
            get { return (int)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register("IconWidth", typeof(int), typeof(IconTile), new PropertyMetadata(32));



        public int IconHeight
        {
            get { return (int)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register("IconHeight", typeof(int), typeof(IconTile), new PropertyMetadata(32));



        public Brush FillColor
        {
            get { return (Brush)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }

        public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register("FillColor", typeof(Brush), typeof(IconTile), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff7b8792"))));


    }
}
