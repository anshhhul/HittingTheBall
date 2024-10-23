using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GraphicsGame
{
    public partial class MainWindow : Window
    {
        private Point? firstClickPosition = null;
        private string currentShape = "Circle";
        private ArrayList shapes = new ArrayList();
        private Dictionary<UIElement, bool> shapeDirectionsHorizontal = new Dictionary<UIElement, bool>();
        private Dictionary<UIElement, bool> shapeDirectionsVertical = new Dictionary<UIElement, bool>();
        private List<Rectangle> bullets = new List<Rectangle>();
        private DispatcherTimer timer;
        private bool isPlaying = false;
        private double cannonAngle = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            this.KeyDown += MainWindow_KeyDown;

            // Position the gun at the bottom center of the canvas
            ShapeCanvas.SizeChanged += (s, e) =>
            {
                Canvas.SetLeft(Gun, (ShapeCanvas.ActualWidth - Gun.Width) / 2);
                Canvas.SetTop(Gun, ShapeCanvas.ActualHeight - Gun.Height);
            };
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.032);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                MoveShapes();
                MoveBullets();
                CheckCollisions();
            }
        }

        private void ShapeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (firstClickPosition == null)
            {
                firstClickPosition = e.GetPosition(ShapeCanvas);
            }
            else
            {
                Point secondClickPosition = e.GetPosition(ShapeCanvas);

                if (currentShape == "Circle")
                {
                    DrawCircle(firstClickPosition.Value, secondClickPosition);
                }
                else if (currentShape == "Rectangle")
                {
                    DrawRectangle(firstClickPosition.Value, secondClickPosition);
                }

                firstClickPosition = null;
            }
        }

        private void DrawCircle(Point center, Point rim)
        {
            double radius = Math.Sqrt(Math.Pow((rim.X - center.X), 2) + Math.Pow((rim.Y - center.Y), 2));

            Ellipse ellipse = new Ellipse()
            {
                Width = 2 * radius,
                Height = 2 * radius,
                StrokeThickness = 1,
                Stroke = GetSelectedColor(),
                Fill = GetSelectedColor() // Added fill color for collision detection
            };

            ellipse.SetValue(Canvas.LeftProperty, center.X - radius);
            ellipse.SetValue(Canvas.TopProperty, center.Y - radius);
            ShapeCanvas.Children.Add(ellipse);
            shapes.Add(ellipse);
            shapeDirectionsHorizontal[ellipse] = true;
            shapeDirectionsVertical[ellipse] = true;
        }

        private void DrawRectangle(Point topLeft, Point bottomRight)
        {
            Rectangle rectangle = new Rectangle()
            {
                Width = Math.Abs(bottomRight.X - topLeft.X),
                Height = Math.Abs(bottomRight.Y - topLeft.Y),
                StrokeThickness = 1,
                Stroke = GetSelectedColor(),
                Fill = GetSelectedColor() // Added fill color for collision detection
            };

            rectangle.SetValue(Canvas.LeftProperty, Math.Min(topLeft.X, bottomRight.X));
            rectangle.SetValue(Canvas.TopProperty, Math.Min(topLeft.Y, bottomRight.Y));
            ShapeCanvas.Children.Add(rectangle);
            shapes.Add(rectangle);
            shapeDirectionsHorizontal[rectangle] = true;
            shapeDirectionsVertical[rectangle] = true;
        }

        private SolidColorBrush GetSelectedColor()
        {
            ComboBoxItem selectedColorItem = (ComboBoxItem)ColorPicker.SelectedItem;
            if (selectedColorItem != null)
            {
                string colorName = selectedColorItem.Tag.ToString();
                Color selectedColor = (Color)ColorConverter.ConvertFromString(colorName);
                return new SolidColorBrush(selectedColor);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            currentShape = clickedButton.Content.ToString();
            firstClickPosition = null;
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            MoveShapes();
            MoveBullets();
            CheckCollisions();
        }

        private void MoveShapes()
        {
            double step = 10;
            double canvasWidth = ShapeCanvas.ActualWidth;
            double canvasHeight = ShapeCanvas.ActualHeight;

            List<UIElement> shapesToRemove = new List<UIElement>();

            foreach (UIElement element in shapes)
            {
                double left = (double)element.GetValue(Canvas.LeftProperty);
                double top = (double)element.GetValue(Canvas.TopProperty);

                bool movingRight = shapeDirectionsHorizontal[element];
                bool movingDown = shapeDirectionsVertical[element];

                if (movingRight)
                {
                    left += step;
                    if (left + ((FrameworkElement)element).Width >= canvasWidth)
                    {
                        left = canvasWidth - ((FrameworkElement)element).Width;
                        shapeDirectionsHorizontal[element] = false;
                    }
                }
                else
                {
                    left -= step;
                    if (left <= 0)
                    {
                        left = 0;
                        shapeDirectionsHorizontal[element] = true;
                    }
                }

                if (movingDown)
                {
                    top += step;
                    if (top + ((FrameworkElement)element).Height >= canvasHeight)
                    {
                        top = canvasHeight - ((FrameworkElement)element).Height;
                        shapeDirectionsVertical[element] = false;
                    }
                }
                else
                {
                    top -= step;
                    if (top <= 0)
                    {
                        top = 0;
                        shapeDirectionsVertical[element] = true;
                    }
                }

                element.SetValue(Canvas.LeftProperty, left);
                element.SetValue(Canvas.TopProperty, top);

                if (element is Rectangle && ((Rectangle)element).Fill == Brushes.White && top < 0)
                {
                    shapesToRemove.Add(element);
                }
            }

            foreach (UIElement shape in shapesToRemove)
            {
                ShapeCanvas.Children.Remove(shape);
                shapes.Remove(shape);
                shapeDirectionsHorizontal.Remove(shape);
                shapeDirectionsVertical.Remove(shape);
            }
        }

        private void MoveBullets()
        {
            double step = 10;
            List<UIElement> bulletsToRemove = new List<UIElement>();

            foreach (Rectangle bullet in bullets)
            {
                double top = (double)bullet.GetValue(Canvas.TopProperty);
                top -= step;
                bullet.SetValue(Canvas.TopProperty, top);

                if (top <= 0)
                {
                    bulletsToRemove.Add(bullet);
                }
            }

            foreach (Rectangle bullet in bulletsToRemove)
            {
                ShapeCanvas.Children.Remove(bullet);
                bullets.Remove(bullet);
            }
        }

        private void CheckCollisions()
        {
            List<UIElement> shapesToRemove = new List<UIElement>();
            List<Rectangle> bulletsToRemove = new List<Rectangle>();

            for (int i = 0; i < shapes.Count - 1; i++)
            {
                for (int j = i + 1; j < shapes.Count; j++)
                {
                    UIElement shape1 = (UIElement)shapes[i];
                    UIElement shape2 = (UIElement)shapes[j];

                    Rect rect1 = new Rect(Canvas.GetLeft(shape1), Canvas.GetTop(shape1), ((FrameworkElement)shape1).Width, ((FrameworkElement)shape1).Height);
                    Rect rect2 = new Rect(Canvas.GetLeft(shape2), Canvas.GetTop(shape2), ((FrameworkElement)shape2).Width, ((FrameworkElement)shape2).Height);

                    if (rect1.IntersectsWith(rect2))
                    {
                        // Bounce back
                        shapeDirectionsHorizontal[shape1] = !shapeDirectionsHorizontal[shape1];
                        shapeDirectionsVertical[shape1] = !shapeDirectionsVertical[shape1];
                        shapeDirectionsHorizontal[shape2] = !shapeDirectionsHorizontal[shape2];
                        shapeDirectionsVertical[shape2] = !shapeDirectionsVertical[shape2];

                        // Change color
                        ((Shape)shape1).Fill = GetRandomColor();
                        ((Shape)shape2).Fill = GetRandomColor();
                    }
                }
            }

            foreach (UIElement shape in shapes)
            {
                Rect shapeRect = new Rect(Canvas.GetLeft(shape), Canvas.GetTop(shape), ((FrameworkElement)shape).Width, ((FrameworkElement)shape).Height);

                foreach (Rectangle bullet in bullets)
                {
                    Rect bulletRect = new Rect(Canvas.GetLeft(bullet), Canvas.GetTop(bullet), bullet.Width, bullet.Height);

                    if (shapeRect.IntersectsWith(bulletRect))
                    {
                        shapesToRemove.Add(shape);
                        bulletsToRemove.Add(bullet);
                    }
                }
            }

            foreach (UIElement shape in shapesToRemove)
            {
                ShapeCanvas.Children.Remove(shape);
                shapes.Remove(shape);
                shapeDirectionsHorizontal.Remove(shape);
                shapeDirectionsVertical.Remove(shape);
            }

            foreach (Rectangle bullet in bulletsToRemove)
            {
                ShapeCanvas.Children.Remove(bullet);
                bullets.Remove(bullet);
            }
        }

        private SolidColorBrush GetRandomColor()
        {
            Random rand = new Random();
            Color randomColor = Color.FromArgb(255, (byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));
            return new SolidColorBrush(randomColor);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = true;
            timer.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            timer.Stop();
        }

        private void FireButton_Click(object sender, RoutedEventArgs e)
        {
            ShootBullet();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            double left = (double)Gun.GetValue(Canvas.LeftProperty);
            if (e.Key == Key.A && left > 0)
            {
                Gun.SetValue(Canvas.LeftProperty, left - 10);
            }
            else if (e.Key == Key.S && left + Gun.Width < ShapeCanvas.ActualWidth)
            {
                Gun.SetValue(Canvas.LeftProperty, left + 10);
            }
            else if (e.Key == Key.Space)
            {
                ShootBullet();
            }
        }

        private void ShootBullet()
        {
            Rectangle bullet = new Rectangle()
            {
                Width = 5,
                Height = 10,
                Fill = Brushes.White
            };

            double left = (double)Gun.GetValue(Canvas.LeftProperty) + Gun.Width / 2 - bullet.Width / 2;
            double top = (double)Gun.GetValue(Canvas.TopProperty) - bullet.Height;

            bullet.SetValue(Canvas.LeftProperty, left);
            bullet.SetValue(Canvas.TopProperty, top);
            ShapeCanvas.Children.Add(bullet);

            bullets.Add(bullet);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ShapeCanvas.Children.Clear();
            shapes.Clear();
            shapeDirectionsHorizontal.Clear();
            shapeDirectionsVertical.Clear();
            bullets.Clear();
            firstClickPosition = null;
            isPlaying = false;

            // Add the gun back to the canvas after clearing
            ShapeCanvas.Children.Add(Gun);
            Canvas.SetLeft(Gun, (ShapeCanvas.ActualWidth - Gun.Width) / 2);
            Canvas.SetTop(Gun, ShapeCanvas.ActualHeight - Gun.Height);
        }
    }
}
