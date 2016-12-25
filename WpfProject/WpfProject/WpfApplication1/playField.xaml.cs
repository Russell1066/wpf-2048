using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace wpf2048App
{
    /// <summary>
    /// Interaction logic for playField.xaml
    /// </summary>
    public partial class playField : UserControl, INotifyPropertyChanged
    {
        public enum ShiftDirection
        {
            Up,
            Down,
            Left,
            Right
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private const double ANIMATIONINTERVAL = 1; // in seconds
        private const int percent4 = 10;

        private List<Cell> cells = new List<Cell>();
        private bool canAdd = false; // Artifact of not separating the model from the display
        private bool gameOver = false;
        private uint score = 0;
        private uint turnScore = 0;
        private RandomValueMgr rand = new RandomValueMgr();

        private DispatcherTimer dt = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(ANIMATIONINTERVAL),
            IsEnabled = false
        };
        Cell.RefCount refCount = new Cell.RefCount(); // Artifact of not separating the model from the display

        public bool GameOver
        {
            get { return gameOver; }
            private set
            {
                if (value != gameOver)
                {
                    gameOver = value;
                    OnPropertyChanged(nameof(GameOver));
                    OnPropertyChanged(nameof(GameOverVisibility));
                }
            }
        }

        public uint Score
        {
            get { return score; }
            private set
            {
                if (score != value)
                {
                    score = value;
                    OnPropertyChanged(nameof(Score));
                }
            }
        }
        public uint MaxValue { get; private set; }

        public Visibility GameOverVisibility
        {
            get { return GameOver ? Visibility.Visible : Visibility.Hidden; }
        }

        public playField()
        {
            InitializeComponent();

            FindCells();

            bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());

            if (!designTime)
            {
                InitializeBoard();
                dt.Tick += Dt_Tick;
            }

            refCount.OnZero += RefCount_IsZero;
        }

        public void Restart()
        {
            InitializeBoard();
        }

        public bool Shift(ShiftDirection dir)
        {
            bool retv = false;

            refCount.Increment();

            switch (dir)
            {
                case ShiftDirection.Up:
                    retv = ShiftVertical(false);
                    break;

                case ShiftDirection.Down:
                    retv = ShiftVertical(true);
                    break;

                case ShiftDirection.Left:
                    retv = ShiftHorizontal(false);
                    break;

                case ShiftDirection.Right:
                    retv = ShiftHorizontal(true);
                    break;

                default:
                    Debug.Assert(false, "Impossible code");
                    return false;
            }

            Trace.WriteLine($"shift {dir} = {retv}");

            canAdd = retv;
            refCount.Decrement();

            Trace.WriteLine($"Score = {Score}");

            return retv;
        }

        // Another artifact of mixing the logic and the display
        private void RefCount_IsZero(object sender, EventArgs e)
        {
            AddNewValue();
            Score += turnScore;
            turnScore = 0;
        }

        private void FindCells()
        {
            for (var y = 0; y < 4; ++y)
            {
                for (var x = 0; x < 4; ++x)
                {
                    cells.Add(FindName("cell" + x.ToString() + y.ToString()) as Cell);
                }
            }
        }

        private void InitializeBoard()
        {
            foreach (var cell in cells)
            {
                cell.Value = 0;
            }

            GameOver = false;
            Score = 0;
            MaxValue = 0;
            canAdd = true;
            AddNewValue();
            AddNewValue();
        }

        private bool ShiftHorizontal(bool reverse)
        {
            bool retv = false;
            for (int i = 0; i < 4; ++i)
            {
                var source = cells.GetRange(i * 4, 4);
                retv |= ShiftCells(source, reverse);
            }

            return retv;
        }

        private bool ShiftVertical(bool reverse)
        {
            bool retv = false;
            for (int i = 0; i < 4; ++i)
            {
                var source = new List<Cell>();
                for (int j = 0; j < 4; ++j)
                {
                    source.Add(cells[i + j * 4]);
                }

                retv |= ShiftCells(source, reverse);
            }

            return retv;
        }

        private bool ShiftCells(List<Cell> shift, bool reverse)
        {
            if (reverse)
            {
                shift.Reverse();
            }

            // Collect all of the non-zero cells
            var srcCells = (from cell in shift
                            where cell.Value != 0
                            select cell).ToList();

            if (srcCells.Count == 0)
            {
                return false;
            }

            var nextValues = new List<uint>();

            var updates = new List<Cell>();
            for (int i = 0; i < srcCells.Count; ++i)
            {
                uint nextValue = srcCells[i].Value;

                if (i + 1 < srcCells.Count && srcCells[i + 1].Value == nextValue)
                {
                    srcCells[i].AnimateTo(srcCells[i], 0, refCount);

                    nextValue *= 2;
                    turnScore += nextValue;
                    MaxValue = Math.Max(MaxValue, nextValue);
                    ++i;
                }

                updates.Add(srcCells[i]);
                nextValues.Add(nextValue);
            }

            bool updated = false;
            for (int i = 0; i < updates.Count; ++i)
            {
                updated |= updates[i].AnimateTo(shift[i], nextValues[i], refCount);
            }

            return updated;
        }

        private void AddNewValue()
        {
            // Can't add if turn hasn't processed
            if (!canAdd)
            {
                return;
            }

            var empty = (from c in cells
                         where c.Value == 0
                         select c).ToList();

            int index = rand.Next(empty.Count, "Cell");
            int random = rand.Next(100, "Value") + 1;
            uint value = (uint)(random < percent4 ? 4 : 2);
            empty[index].Value = value;

            TestGameOver();

            // Keep track of what's been updated
            Trace.WriteLine($"{empty[index].Name} = {value} ({random}) ");
        }

        private void EndGame()
        {
            Trace.WriteLine("Game Over");
            dt.IsEnabled = false;
            GameOver = true;
            return;
        }

        // Automated game play
        // Used during testing
        private void Dt_Tick(object sender, EventArgs e)
        {
            if (TestGameOver())
            {
                return;
            }

            if (!Shift(ShiftDirection.Left) &&
                !Shift(ShiftDirection.Up) &&
                !Shift(ShiftDirection.Right))
            {
                Trace.WriteLine("I don't think this is possible");
                Shift(ShiftDirection.Down);
            }
        }

        private bool TestGameOver()
        {
            if (GameOver)
            {
                return GameOver;
            }

            var emptyCells = from c in cells
                             where c.Value == 0
                             select c;

            if (emptyCells.Count() != 0)
            {
                return GameOver;
            }

            // Iterate through, rows and columns
            for (int y = 0; y < 4; ++y)
            {
                int offset = y * 4;
                for (int x = 0; x < 3; ++x)
                {
                    if (cells[offset + x].Value == cells[offset + x + 1].Value)
                    {
                        return GameOver;
                    }

                    if (cells[x * 4 + y].Value == cells[x * 4 + 4 + y].Value)
                    {
                        return GameOver;
                    }
                }
            }

            EndGame();

            return GameOver;
        }

        // Safe mechanism for invoking property changed callbacks
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Game_KeyDown(object sender, KeyEventArgs e)
        {
            if (!refCount.IsZero)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    Shift(ShiftDirection.Left);
                    break;

                case Key.Right:
                    Shift(ShiftDirection.Right);
                    break;

                case Key.Up:
                    Shift(ShiftDirection.Up);
                    break;

                case Key.Down:
                    Shift(ShiftDirection.Down);
                    break;
            }
        }

    }
}
