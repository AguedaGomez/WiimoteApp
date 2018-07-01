using GestureLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
using WiimoteLib;

namespace DemoGesture
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> prototypesList { get; set; }

        Wiimote wm;
        GestureCapturer gestureCapturer;
        Gesture gesture;
        GestureRecognizer gestureRecognizer;
        enum Mode
        {
            TRAINING,
            RECOGNIZER
        };
        Mode mode;
        int currentIndexGesture;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            wm = new Wiimote();
            mode = Mode.TRAINING;
            currentIndexGesture = 0;
            gesture = new Gesture();
            gestureCapturer = new GestureCapturer();
            prototypesList = new ObservableCollection<string>();
            gestureRecognizer = new GestureRecognizer();
            GestureListbox.ItemsSource = prototypesList;
            this.KeyDown += MainWindow_KeyDown;

        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Add2List();
        }

        private void Wm_WiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action<WiimoteState>((state) => gestureCapturer.OnWiimoteChanged(state)), new object[] { e.WiimoteState });
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Add2List();
        }

        private void Add2List()
        {
            if (InputNameGestureTextBox.Text != "")
            {
                prototypesList.Add(InputNameGestureTextBox.Text);
                InputNameGestureTextBox.Text = "";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            Stream stream;
            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == true)
            {
                if ((stream = saveFileDialog1.OpenFile()) != null)
                {

                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, gestureRecognizer.Prototypes);
                    stream.Close();
                }
            }

        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {

            Stream stream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                try
                {
                    if ((stream = openFileDialog1.OpenFile()) != null)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<Gesture> prototypes = (List<Gesture>)bf.Deserialize(stream);
                        prototypesList.Clear();
                        gestureRecognizer.Clear();
                        foreach (var p in prototypes)
                        {
                            gestureRecognizer.AddPrototype(p);
                            prototypesList.Add(p.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se puede leer el archivo " + ex.Message);
                }

            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (prototypesList.Count > 0)
            {
                if (mode == Mode.TRAINING)
                {
                    gestureRecognizer.Clear();
                    RecogGesture.Text = prototypesList[currentIndexGesture];
                    wm.WiimoteChanged += Wm_WiimoteChanged;
                    wm.Connect();
                    wm.SetReportType(InputReport.ButtonsAccel, true);
                    EnableAllOptions(false);

                    gestureCapturer.GestureCaptured += GestureCapturer_GestureCaptured;
                }
                else if (mode == Mode.RECOGNIZER)
                {
                    if (StartButton.Content.ToString() == "Start")
                    {
                        wm.WiimoteChanged += Wm_WiimoteChanged;
                        wm.Connect();
                        wm.SetReportType(InputReport.ButtonsAccel, true);
                        gestureCapturer.GestureCaptured += GestureCapturer_GestureCaptured1;
                        gestureRecognizer.GestureRecognized += GestureRecognizer_GestureRecognized;
                        EnableAllOptions(false);
                        StartButton.IsEnabled = true;
                        StartButton.Content = "Stop";
                    }
                    else
                    {
                        wm.WiimoteChanged -= Wm_WiimoteChanged;
                        gestureCapturer.GestureCaptured -= GestureCapturer_GestureCaptured1;
                        EnableAllOptions(true);
                        StartButton.Content = "Start";
                    }
                }
            }

            else
            {
                MessageBox.Show("No se ha introducido ningún gesto");
            }

        }

        private void GestureRecognizer_GestureRecognized(string obj)
        {
            RecogGesture.Text = obj;
        }

        private void GestureCapturer_GestureCaptured1(Gesture obj)
        {
            gestureRecognizer.OnGestureCaptured(obj);
        }

        private void GestureCapturer_GestureCaptured(Gesture obj)
        {

            if (currentIndexGesture < prototypesList.Count)
            {
                obj.Name = prototypesList[currentIndexGesture];
                gestureRecognizer.AddPrototype(obj);
                currentIndexGesture++;
                if (currentIndexGesture != prototypesList.Count)
                    RecogGesture.Text = prototypesList[currentIndexGesture];
                else
                {
                    RecogGesture.Text = "--";
                    wm.WiimoteChanged -= Wm_WiimoteChanged;
                    gestureCapturer.GestureCaptured -= GestureCapturer_GestureCaptured;
                    EnableAllOptions(true);
                    currentIndexGesture = 0;
                }

            }

        }

        private void TrainingMode_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.TRAINING;
        }

        private void RecognitionMode_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.RECOGNIZER;
        }

        private void EnableAllOptions(bool enabled)
        {
            StartButton.IsEnabled = enabled;
            TrainingMode.IsEnabled = enabled;
            RecognitionMode.IsEnabled = enabled;
            Add.IsEnabled = enabled;
            Clear.IsEnabled = enabled;
            LoadButton.IsEnabled = enabled;
            SaveButton.IsEnabled = enabled;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            prototypesList.Clear();
            gestureRecognizer.Clear();
            RecogGesture.Text = "--";
        }
    }
}
