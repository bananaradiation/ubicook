//------------------------------------------------------------------------------
// adapted from color basics WPF found in Kinect toolbox
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using XnaFan.ImageComparison;
    using System.Timers;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Documents;
    using System.Data;

    public partial class MainWindow : Window
    {
        //paths to be changed
        string query_path = @"C:\Users\Hannah Chen\Documents\Visual Studio 2013\Projects\Ubicook\bin\Debug\query.txt";
        string webscrape_path = @"C:\Users\Hannah Chen\Documents\Visual Studio 2013\Projects\Ubicook\bin\Debug\webscrape.txt";
        string py_path = "C:\\Python27\\python.exe";
        string url_py = @"C:\bovw\bow\url.py";
            
        // TO UPDATE FOR EACH DEMO
        string background_path = @"C:\Users\Hannah Chen\Documents\Visual Studio 2013\Projects\Ubicook\bin\Debug\background.png";
        string save_path = @"C:\bovw\bow\classify\KinectSnapshot.jpg";
        string sift_path = @"C:\bovw\bow\classify\KinectSnapshot.jpg.sift";
        //to change with kinectsnapshot.jpg path
        string classify_path_args = "C:\\bovw\\bow\\classify.py -c C:\\bovw\\bow\\foodLib\\codebook.file -m C:\\bovw\\bow\\foodLib\\trainingdata.svm.model C:\\bovw\\bow\\classify\\KinectSnapshot.jpg";
        string empty_path = @"C:\Users\Hannah Chen\Documents\Visual Studio 2013\Projects\Ubicook\bin\Debug\empty.txt";

        int updateRate = 9;

        //multi-dimensional array for saving bs4 from webscrap.txt
        private const int row = 10;
        private const int col = 3;
        protected String[,] Recipe = new string[row, col];
        
        private KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;
        private DispatcherTimer dispatcherTimer;

        private TextBox placeH = new TextBox();

        public MainWindow()
        {
            InitializeComponent();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, updateRate);
            dispatcherTimer.Tick += new EventHandler(tick);
            dispatcherTimer.Start();
            
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
                this.Image.Source = this.colorBitmap;

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Ubicook.Properties.Resources.NoKinectReady;
            }

            setPlaceholder();
        }

        private void setPlaceholder()
        {
            ingredients.Text = "Scanning for ingredients...";
            ingredients.FontSize = 32;
            ingredients.Foreground = new SolidColorBrush(Colors.LightSteelBlue);
            ingredients.Background = new SolidColorBrush(Colors.White);
            ingredients.Opacity = 100;
            placeH.IsEnabled = false;
            placeH.Text = "Your Recipes will show up here...";
            placeH.BorderBrush = null;
            placeH.FontSize = 32;
            placeH.Foreground = new SolidColorBrush(Colors.LightSteelBlue);
            RecipeSpace.Items.Add(placeH);
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    // Write the pixel data into our bitmap
                    this.colorBitmap.WritePixels(
                        new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                        this.colorPixels,
                        this.colorBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        private void getRecipes(object sender, RoutedEventArgs e)
        {
            Console.Write("launching url.py" + "\n");
            runUrl();

            populateRecipes(webscrape_path);
            dispatcherTimer.Stop();
            Console.Write("Timer is stopped" + "\n");

            if (File.Exists(query_path)) { File.Delete(query_path); }
            Console.Write("Deleting contents of query.txt" + "\n");
        }

        private void savePic()
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(save_path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Ubicook.Properties.Resources.ScreenshotWriteSuccess, save_path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1}", Ubicook.Properties.Resources.ScreenshotWriteFailed, save_path);
            }
        }

        private void tick(object sender, EventArgs e)
        {
            if (File.Exists(sift_path))
                {
                    Console.Write("deleting old sift file now..." + "\n");
                    File.Delete(sift_path);
                }

            Console.Write("saving picture" + "\n");
            savePic();
            Console.Write("percentage pixel difference = " + background() + "\n");
            if (background() > 20)
            {
                Console.Write("running classify.py ..." + "\n");
                runClassify();
                Console.Write("populating ingredient list..." + "\n");
                populateIngredientList();
            }
            else
            {
                Console.Write("picture and background diff not significant!!!" + "\n");
            }
        }

        private void runClassify()
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo();
            ProcessInfo.FileName = py_path;
            ProcessInfo.Arguments = classify_path_args;
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            ProcessInfo.RedirectStandardOutput = true;

            using (Process p = Process.Start(ProcessInfo))
            {
                using (StreamReader reader = p.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write("Result : " + result);
                    p.WaitForExit();
                    ExitCode = p.ExitCode;
                    p.Close();
                }
            }
 
        }

        private int background()
        {
            int difference = (int)(ImageTool.GetPercentageDifference1(background_path, save_path) * 100);
            return difference;
        }
        
        private void runUrl()
        {
            ProcessStartInfo urlProcess = new ProcessStartInfo();
            urlProcess.FileName = py_path;
            urlProcess.Arguments = url_py;
            urlProcess.CreateNoWindow = true;
            urlProcess.UseShellExecute = false;
            urlProcess.RedirectStandardOutput = true;

            int ExitCode2;
            using (Process p2 = Process.Start(urlProcess))
            {
                using (StreamReader reader = p2.StandardOutput)
                {
                    string output = reader.ReadToEnd();
                    Console.Write("URL Result : " + output);
                    p2.WaitForExit();
                    ExitCode2 = p2.ExitCode;
                    p2.Close();
                }
            }

            Console.Write("ran url.py and created webscraper.txt" + "\n");
        }

        private void ButtonResetClick(object sender, RoutedEventArgs e)
        {
            Console.Write("reset button clicked!!!!" + "\n");
            // called every time query is updated
            if (File.Exists(webscrape_path))
            {
                populateRecipes(empty_path);
            }
            if (File.Exists(query_path))
            {   
                ingredients.Text = String.Empty;
            }
            dispatcherTimer.Tick += new EventHandler(tick);
            dispatcherTimer.Start();
            Console.Write("restarting timer!!!!" + "\n");

            setPlaceholder();
        }

        private void populateIngredientList()
        {
            ingredients.Text = "";
            ingredients.Foreground = new SolidColorBrush(Colors.Navy);
            int count = 0;
            string temp = "";
            string[] lines = System.IO.File.ReadAllLines(query_path);

            foreach (string line in lines)
            {
                //checks for duplicate items in query.txt
                if (line != "" && !ingredients.Text.Contains(line))
                {
                    count++;
                    if (count == 1)
                        temp = line;
                    else
                        temp = "; " + line;
                    ingredients.Text += temp;
                }
            }
        }

        //reads webScrape.txt and generates GUI
        private void populateRecipes(string webscrape_file)
        {
            int recipeCount = -1;
            int LabelCount = -1;
            if (File.Exists(webscrape_file))
            {
                string[] lines = System.IO.File.ReadAllLines(webscrape_file);
                foreach (string line in lines)
                {
                    recipeCount++;
                    string[] words = line.Split('^');
                    LabelCount = -1;
                    foreach (string word in words)
                    {
                        LabelCount++;
                        if (word == null) { Recipe[recipeCount, LabelCount] = @"Images/no_pic.jpg"; }
                        else
                        {
                            Recipe[recipeCount, LabelCount] = word;
                        }
                    }
                }
                if (recipeCount != -1)
                {
                    createRecipeView(recipeCount);
                }
                else
                {
                    Console.Write("clearing recipe space" + "\n");
                    RecipeSpace.Items.Clear();

                }
            }
            else
            {
                MessageBox.Show("Webscrape.txt does not exist");
            }           
        }

        private void createRecipeView(int rows)
        {
            RecipeSpace.Items.Clear();
            StackPanel[] dynamicStackPanel = new StackPanel[rows];
            Image[] img = new Image[rows];
            TextBlock[] title = new TextBlock[rows];
            Hyperlink[] hyper = new Hyperlink[rows];
            Run[] runURL = new Run[rows];

            // Create a StackPanel and set its properties
            for (int i = 0; i < rows; i++)
            {
                dynamicStackPanel[i] = new StackPanel();
                dynamicStackPanel[i].Width = 520;
                dynamicStackPanel[i].Height = 135;
                dynamicStackPanel[i].Background = new SolidColorBrush(Colors.LightBlue);
                dynamicStackPanel[i].Orientation = Orientation.Horizontal;

                img[i] = new Image();
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(Recipe[i, 2], UriKind.Absolute);
                bi3.EndInit();
                img[i].Stretch = Stretch.Fill;
                img[i].Source = bi3;
                img[i].Width = 143;

                var bc = new BrushConverter();
                
                Hyperlink hyp = new Hyperlink(new Run(Recipe[i, 0]))
                {
                    NavigateUri = new Uri(Recipe[i, 1])
                };

                //adds 
                hyp.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(hyp_RequestNavigate);

                title[i] = new TextBlock(new System.Windows.Documents.Hyperlink());
                title[i].Name = "title";
                title[i].FontSize = 20;
                title[i].TextWrapping = TextWrapping.Wrap;
                title[i].Width = 320;
                title[i].IsEnabled = true;
                title[i].Background = (Brush)bc.ConvertFrom("#add8e6");
                title[i].Height = 120;
                title[i].Inlines.Add(hyp);
                dynamicStackPanel[i].Children.Add(img[i]);
                dynamicStackPanel[i].Children.Add(title[i]);
                
                // displays stack panel in nice GUI form
                this.RecipeSpace.Items.Add(dynamicStackPanel[i]);
            }
        }

        //starts new process that launches recipe url
        private void hyp_RequestNavigate(object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void buttonFind_MouseEnter(object sender, MouseEventArgs e)
        {
            BitmapImage bi4 = new BitmapImage();
            bi4.BeginInit();
            bi4.UriSource = new Uri("Images/Get_Recipe_hover.png", UriKind.Relative);
            bi4.EndInit();
            buttonFind.Source = bi4;
        }

        private void buttonFind_MouseLeave(object sender, MouseEventArgs e)
        {
            BitmapImage bi5 = new BitmapImage();
            bi5.BeginInit();
            bi5.UriSource = new Uri("Images/Get_Recipe.png", UriKind.Relative);
            bi5.EndInit();
            buttonFind.Source = bi5;
        }

        private void buttonReset_MouseEnter(object sender, MouseEventArgs e)
        {
            BitmapImage bi1 = new BitmapImage();
            bi1.BeginInit();
            bi1.UriSource = new Uri("Images/reset_button_hover.png", UriKind.Relative);
            bi1.EndInit();
            buttonReset.Source = bi1;
        }

        private void buttonReset_MouseLeave(object sender, MouseEventArgs e)
        {
            BitmapImage bi2 = new BitmapImage();
            bi2.BeginInit();
            bi2.UriSource = new Uri("Images/reset_button.png", UriKind.Relative);
            bi2.EndInit();
            buttonReset.Source = bi2;
        }
    }
}