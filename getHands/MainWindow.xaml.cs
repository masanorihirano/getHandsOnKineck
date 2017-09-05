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
using Microsoft.Kinect;
using System.ComponentModel;

namespace getHands
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private KinectSensor kinect = null;
        private Body[] bodies = null;
        private DrawingGroup dg;
        private DrawingImage imageSource;
        private int displayWidth, displayHeight;
        // Coordinate mapper to map one type of point to another
        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;
        private string textData="init";
        public MainWindow()
        {
            //get the first kinectsensor
            kinect = KinectSensor.GetDefault();
            //get the frame size
            FrameDescription fd = kinect.DepthFrameSource.FrameDescription;
            displayHeight = fd.Height;
            displayWidth = fd.Width;
            coordinateMapper = kinect.CoordinateMapper;
            bodyFrameReader = kinect.BodyFrameSource.OpenReader();
            //start the kinect
            kinect.Open();
            dg = new DrawingGroup();
            imageSource = new DrawingImage(dg);
            // use the window object as the view model in this simple example
            this.DataContext = this;
            InitializeComponent();
        }
        public ImageSource ImageSource
        {
            get
            {
                return imageSource;
            }
        }
        public String DataText
        {
            get
            {
                return textData;
            }
            set
            {
                textData = value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DataText"));
                }
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (bodyFrameReader != null)
            {
                bodyFrameReader.FrameArrived += Reader_FrameArrived;
            }
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }
            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            //check whether it get the new frame or not
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                //check the number of bodies
                if (bodyFrame != null)
                {
                    if (bodies == null)
                    {
                        bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                //if it get the new frame,
                String tempText = "";
                using (DrawingContext dc = dg.Open())
                {
                    //drawing the black background, which needs display width & Height in the initialization
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, displayWidth, displayHeight));
                    DataText = "initialized";
                    int bodyCount = 0;
                    foreach(Body body in bodies)
                    {
                        //convert kinect's joints to position list
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = 0.1f;
                            }

                            DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }
                        //do jobs for each body
                        DrawHand(jointPoints[JointType.HandLeft], dc);
                        DrawHand(jointPoints[JointType.HandRight], dc);
                        tempText += "Body[" + (bodyCount++) + "]\n";
                        if (body.IsTracked)
                        {
                            tempText += "  L[" + body.HandLeftState + "]:(" + (int)jointPoints[JointType.HandLeft].X + "," + (int)jointPoints[JointType.HandLeft].Y + ")\n";
                            tempText += "  R[" + body.HandRightState + "]:(" + (int)jointPoints[JointType.HandRight].X + "," + (int)jointPoints[JointType.HandRight].Y + ")\n";
                        }
                        else
                        {
                            tempText += " Not Tracked\n";
                        }
                    }
                }
                // prevent drawing outside of our render area
                Console.Write(tempText);
                DataText = tempText;
                dg.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, displayWidth, displayHeight));
            }
        }
        private void DrawHand(Point handPosition, DrawingContext drawingContext)
        {
            //drawn the circle of hands
            drawingContext.DrawEllipse(Brushes.Blue, null, handPosition, 30, 30);
        }
        //INotifyPropertyChanged members
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
