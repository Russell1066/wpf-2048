using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpf2048App
{
    /// <summary>
    /// Interaction logic for Cell.xaml
    /// </summary>
    public partial class Cell : UserControl, INotifyPropertyChanged
    {
        public const double SHIFTTIME = 2;//.2;
        private uint cellValue = 0;
        public string CellValue
        {
            get
            {
                return Value < 2 ? "" : $"{Value}";
            }
        }

        static Color[] colorSet = {
            Colors.Linen,
            Colors.Beige,
            Colors.BlanchedAlmond,
            Colors.LemonChiffon,
            Colors.Coral,
            Colors.Goldenrod,
            Colors.LightSeaGreen,
            Colors.MediumOrchid,
            Colors.Firebrick,
            Colors.IndianRed,
            Colors.ForestGreen,
            Colors.White,
        };

        public Brush CellColor
        {
            get
            {
                if (Value == 0)
                {
                    return new SolidColorBrush(Colors.AntiqueWhite);
                }

                int i = 0;
                for (; i < colorSet.Length - 1; ++i)
                {
                    if (Value == 1 << (i + 1))
                    {
                        break;
                    }
                }

                return new SolidColorBrush(colorSet[i]);
            }
        }

        public uint Value
        {
            get { return cellValue; }
            set
            {
                if (cellValue != value)
                {
                    cellValue = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(CellValue));
                    OnPropertyChanged(nameof(CellColor));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Cell()
        {
            InitializeComponent();
        }

        public bool AnimateTo(Cell newLocation, uint newValue, RefCount refCount)
        {
            if (newLocation == this && newValue == Value)
            {
                return false;
            }

            var cell = FindName("CellItem") as Panel;
            if(newLocation != this)
            {
                AnimateAsShift(newLocation, newValue, refCount, cell);
            }
            else
            {
                AnimateAsFade(newLocation, newValue, refCount, cell);
            }

            return true;
        }

        private void AnimateAsShift(Cell newLocation, uint newValue, RefCount refCount, Panel cell)
        {
            var newCell = newLocation.FindName("CellItem") as Panel;
            var src = VisualTreeHelper.GetOffset(this);
            var dest = VisualTreeHelper.GetOffset(newLocation);
            var move = dest - src;
            bool isX = (move.X) != 0;
            double distance = move.X + move.Y;

            var shift = new DoubleAnimation(0, distance, TimeSpan.FromSeconds(SHIFTTIME))
            {
                FillBehavior = FillBehavior.Stop
            };

            TranslateTransform t = new TranslateTransform();
            cell.RenderTransform = t;

            Panel.SetZIndex(this, 100);
            refCount.Increment();
            shift.Completed += (object sender, EventArgs e) =>
            {
                newLocation.Value = newValue;
                Value = 0;
                cell.RenderTransform = Transform.Identity;
                Panel.SetZIndex(this, 0);

                refCount.Decrement();
            };

            var dependencyProp = isX ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            t.BeginAnimation(dependencyProp, shift);
        }

        private void AnimateAsFade(Cell newLocation, uint newValue, RefCount refCount, Panel cell)
        {
            var fade = new DoubleAnimation(.8, 0, TimeSpan.FromSeconds(SHIFTTIME / 2))
            {
                FillBehavior = FillBehavior.Stop
            };

            //Panel.SetZIndex(this, 200);
            refCount.Increment();
            fade.Completed += (object sender, EventArgs e) =>
            {
                newLocation.Value = newValue;
                Value = 0;
                Opacity = 1;
                //Panel.SetZIndex(this, 0);

                refCount.Decrement();
            };

            cell.BeginAnimation(OpacityProperty, fade);
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class RefCount
        {
            private int v;

            public bool IsZero { get { return v == 0; } }

            // Can be replaced with thread safe if needed
            public void Increment()
            {
                v += 1;
            }

            // Can be replaced with thread safe if needed
            public void Decrement()
            {
                v -= 1;
                if (v == 0)
                {
                    OnZero?.Invoke(this, new EventArgs());
                }
            }

            public event EventHandler OnZero;
        }

    }
}
