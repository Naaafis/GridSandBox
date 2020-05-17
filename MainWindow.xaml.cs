//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GridSandbox
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    using System.Windows.Controls;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //BEGIN: for EK131 Students -- feel free to change these variables

        int numGridRows = 5, numGridCols = 5;
        //END: for EK131

        // do not change the following variables
        bool useLeftHand = true;
        const double gridHeight = 600, gridWidth = 600;
        Point playerOneHandLocation;

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        ///
        private const double HandSize = 20;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 2;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        bool needsAllocationPlayerOne;

        ulong playerOneTrackingId;

        System.Windows.Media.Brush playerOneBrush;


        /////////////////////
        // Variables to use//
        /////////////////////
        int delayFrames;
        int[,] gridArray;
        int[,] gridArray1;
        int[,] gridArray2;
        int[,] gridArray3;
        int[,] gridArray4;
        int[,] gridArray5;
        int score = 0;
        int rowHit;
        int colHit;
        Rectangle[,] rectArray;
        bool isGameOver, waitingOnBot;
        bool isPlayerOneHandClosed, hasPlayerOneHandOpened;
        Random rand = new Random();
        
        Point playerTwoHandLocation;
        bool playerMoved = false;

        ////////////////
        //DO NOT TOUCH//
        ////////////////
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

           //dynamically add a grid

           uniGrid.Rows = numGridRows; uniGrid.Columns = numGridCols;
            //rows left->right
           rectArray = new Rectangle[numGridRows,numGridCols];
           for (int rr = 0; rr < numGridRows; rr++)
           {
               for (int cc = 0; cc < numGridCols; cc++)
               {
                   rectArray[rr, cc] = new Rectangle() {
                       Stroke = System.Windows.Media.Brushes.Black,
                      // Height = (int)(gridHeight / numGridRows),
                       //Width = (int)(gridWidth / numGridRows),
                   };
                   uniGrid.Children.Add(rectArray[rr,cc]);
               }
           }

          //student storage initialization
           gridArray1 = new int[numGridRows, numGridCols];
           gridArray2 = new int[numGridRows, numGridCols];
           gridArray3 = new int[numGridRows, numGridCols];
           gridArray4 = new int[numGridRows, numGridCols];
           gridArray5 = new int[numGridRows, numGridCols];
           RandGridArray();

           playerOneBrush = System.Windows.Media.Brushes.Green;

           isPlayerOneHandClosed = false; hasPlayerOneHandOpened = false;

           playerOneHandLocation = new Point();


           delayFrames = 900;
           isGameOver = false;
           waitingOnBot = false;

           playerOneTrackingId = 0;
           needsAllocationPlayerOne = true;
            rowHit = -1;
            colHit = -1;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    //every frame we try to allocate a skeleton to a user -- if they are NOT found set them after this loop occurs
                    needsAllocationPlayerOne = true;

                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    if(needsAllocationPlayerOne)
                    {
                        //this state should really only be reached if the game has just started
                    }

                    //check if skeletons from last time are found -- if NOT allocate skeletons here
                    foreach (Body body in this.bodies)
                    {
                        if(body.IsTracked)
                        {
                            if (playerOneTrackingId == 0) // this has never been allocated
                            {
                                playerOneTrackingId = body.TrackingId;
                                break;
                            }


                        }
                    }

                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                           // Console.WriteLine(body.TrackingId);

                            this.DrawClippedEdges(body, dc);

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
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);

                            // A state change from Open -> Closed for a given hand is required for a closed hand to trigger
                            // the intention behind this is for code based on a closed hand only triggers ONCE
                            // otherwise code that only checks if a hand s closed will trigger every frame until it's open

                            //body.HandLeftState

                            HandState givenHandState;

                            if(useLeftHand)
                            {
                                givenHandState = body.HandLeftState;
                            }
                            else
                            {
                                givenHandState = body.HandRightState;
                            }

                            // Find the left hand state
                            switch (givenHandState)
                            {
                                case HandState.Open:
                                    //isPlayerOneHandClosed = false;
                                    hasPlayerOneHandOpened = true;
                                    isPlayerOneHandClosed = false;
                                    break;
                                case HandState.Closed:
                                    if (hasPlayerOneHandOpened)
                                    {
                                        isPlayerOneHandClosed = true;
                                        hasPlayerOneHandOpened = false;
                                        break;
                                    }

                                    isPlayerOneHandClosed = false;
                                    break;
                                default:
                                    isPlayerOneHandClosed = false;
                                    break;
                            }
                            //update location on board

                            //playerOneHandLocation
                            //playerTwoHandLocation


                            double shoulderLengthScale = Math.Sqrt(Math.Pow(jointPoints[JointType.ShoulderLeft].X - jointPoints[JointType.ShoulderRight].X, 2)
                                + Math.Pow(jointPoints[JointType.ShoulderLeft].Y - jointPoints[JointType.ShoulderRight].Y, 2));

                            //region is intended to scale hand values from 0-1, we have this region to the LEFT of the user
                            // which is scaled to the length of the left shoulder - to - right shoulder

                            Rect handRegion;

                            if(useLeftHand){
                                handRegion = new Rect(jointPoints[JointType.ShoulderLeft].X - (1.3 * shoulderLengthScale),
                                jointPoints[JointType.ShoulderLeft].Y - (1.0 * shoulderLengthScale),
                                2*shoulderLengthScale,
                                2*shoulderLengthScale);
                                playerOneHandLocation.X = (jointPoints[JointType.HandLeft].X - handRegion.X) / (handRegion.Width) * 600;
                                playerOneHandLocation.Y = (jointPoints[JointType.HandLeft].Y - handRegion.Y) / (handRegion.Height) * 600;
                            }else{
                                //using right hand
                                handRegion = new Rect(jointPoints[JointType.ShoulderRight].X - (.7 * shoulderLengthScale),
                                jointPoints[JointType.ShoulderRight].Y - (1.0 * shoulderLengthScale),
                                2 * shoulderLengthScale,
                                2 * shoulderLengthScale);
                                playerOneHandLocation.X = (jointPoints[JointType.HandRight].X - handRegion.X) / (handRegion.Width) * 600;
                                playerOneHandLocation.Y = (jointPoints[JointType.HandRight].Y - handRegion.Y) / (handRegion.Height) * 600;
                            }

                            dc.DrawRectangle(null, drawPen, handRegion);

                            Canvas.SetLeft(this.Hand,playerOneHandLocation.X - 25); // -25 to center hand on location
                            Canvas.SetTop(this.Hand,playerOneHandLocation.Y -25); // -25 to center hand on location

                            studentWork();
                        }
                    }

                    //JointType.Hand
                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        ///////////////////////////
        // Functions for Students//
        ///////////////////////////

        // reset all grid square colors to white
        /* Rather than changing the grid color to all white, randomize the colors */
        void clearBoard1()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                  if (gridArray1[rr,cc] == 0)
                    { 
                    rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Red;
                    }
                  if (gridArray1[rr,cc] == 1)
                    {
                    rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Green;
                    }
                    if (gridArray1[rr, cc] == 2)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    }

                }
            }
        }
        void clearBoard2()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray2[rr, cc] == 0)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Red;
                    }
                    if (gridArray2[rr, cc] == 1)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Green;
                    }
                    if (gridArray2[rr, cc] == 2)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    }
                }
            }
        }
        void clearBoard3()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray3[rr, cc] == 0)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Red;
                    }
                    if (gridArray3[rr, cc] == 1)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Green;
                    }
                    if (gridArray3[rr, cc] == 2)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    }
                }
            }
        }
        void clearBoard4()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray4[rr, cc] == 0)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Red;
                    }
                    if (gridArray4[rr, cc] == 1)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Green;
                    }
                    if (gridArray4[rr, cc] == 2)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    }
                }
            }
        }
        void clearBoard5()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    if (gridArray5[rr, cc] == 0)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Red;
                    }
                    if (gridArray5[rr, cc] == 1)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.Green;
                    }
                    if (gridArray5[rr, cc] == 2)
                    {
                        rectArray[rr, cc].Fill = System.Windows.Media.Brushes.White;
                    }
                }
            }
        }

        // specify a location (row, col) to clear the color and reset to white.
        void clearGridLocation(int row, int col)
        {
            if (row < 0 || col < 0) return;
            rectArray[row, col].Fill = System.Windows.Media.Brushes.White;
        }

        // set the status box below the grid to a given string with a given color.
        // brush color examples:
        // System.Windows.Media.Brushes.Black
        // System.Windows.Media.Brushes.White
        // System.Windows.Media.Brushes.Green
        // System.Windows.Media.Brushes.Red
        void setText(string text, System.Windows.Media.Brush brush) { textHere.Text = text; textHere.Foreground = brush; }

        // update the instructions to the right side of the grid to a given string, color, and size.
        void setInstructionText(string text, System.Windows.Media.Brush brush, double fontSize)
        {
            instructionText.Text = text;
            instructionText.Foreground = brush;
            instructionText.FontSize = fontSize;
        }

        // update the text of a button given by a number (1,2,or 3)
        void setButtonText(string text, int buttonNum)
        {
            switch (buttonNum)
            {
                case 1:
                    Button1.Content = text;
                    break;
                case 2:
                    Button2.Content = text;
                    break;
                case 3:
                    Button3.Content = text;
                    break;
            }
        }

        // update color of a grid square given by row and column.
        void highlightGridLocationWithColor(int row, int col, System.Windows.Media.Brush brush)
        {
            if (row < 0 || col < 0) return;
            rectArray[row, col].Fill = brush;
            InvalidateVisual(); // render layout again
        }

        // update color of a grid square with a player's color
        // grid square is given by row and column
        void highlightGridLocation(int row, int col, int playerNumber)
        {
            if (row < 0 || col < 0) return;

            rectArray[row, col].Fill = playerOneBrush;

            InvalidateVisual(); // render layout again
        }

        // reset Grid Array to zero
        // this function is called upon for grid initialization
        void RandGridArray()
        {
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray1[rr, cc] = rand.Next(2);
                }
            }
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray2[rr, cc] = rand.Next(2);
                }
            }
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray3[rr, cc] = rand.Next(2);
                }
            }
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray4[rr, cc] = rand.Next(2);
                }
            }
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    gridArray5[rr, cc] = rand.Next(2);
                }
            }
        }

        // sets a player’s color to a given brush color (accepted player numbers are 1 and 2).
        void setPlayerColor(int playerNumber, System.Windows.Media.Brush brush)
        {
            if (playerNumber == 1)
                playerOneBrush = brush;

        }

        // If true, the given’s player hand is detected as closed
        // if false, it is detected as not closed (open).
        bool isPlayerHandClosed(int playerNumber)
        {
            if (playerNumber == 1)
                return isPlayerOneHandClosed;

            return false;
        }

        // returns a normalized point (0 to 1) of either player one hand's location or player two's
        // cannot return null point, so use with care
        // Note: values can be returned outside this range if the hand has left the region!
        Point getPlayerHandLocation(int playerNumber)
        {

                return new Point(playerOneHandLocation.X / gridWidth,  playerOneHandLocation.Y / gridHeight);


        }

        // if the player’s hand is at the specified location, the function returns true, otherwise, false.
        // note: this function doesn't tell WHICH location the playerHand is in
        // so it is necessary for the players to check EACH location and perform logic
        bool isPlayerHandInGridLocation(int row, int col, int playerNumber)
        {
            if (row < 0 || col < 0) return false;

            //resolution of drawing region is 600x600 -- we segment each region into parts
            //gridHeight, gridWidth

            double xLowerBound = col * (gridWidth / numGridCols), xUpperBound = (col + 1) * (gridWidth / numGridCols),
                yLowerBound = row * (gridHeight / numGridRows), yUpperBound = (row + 1) * (gridHeight / numGridRows);

            Point HandLocation;

            if (playerNumber == 1) HandLocation = playerOneHandLocation;
            else HandLocation = playerTwoHandLocation;

            if (HandLocation.X >= xLowerBound && HandLocation.X < xUpperBound &&
                HandLocation.Y >= yLowerBound && HandLocation.Y < yUpperBound)
            {
                return true;
             }

            return false;
        }

        /////////////////////////////////////
        // Functions for Students to Change//
        /////////////////////////////////////


        // used in tic tac toe and returns 0 no winner, returns 1 or 2 if that player has won respectively
        // this only works for a 3x3 game of tic tac toe.

        void round1()
        {
            clearBoard1();
          for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
          {
              for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
              {
                  // if the triggered location is already filled ignore it
                  if (gridArray1[rr, cc] == 1 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score + 1;
                      playerMoved = true;
                        gridArray1[rr, cc] = 2;
                    }
                  if (gridArray1[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score - 1;
                      playerMoved = true;
                        gridArray1[rr, cc] = 2;
                    }
              }
          }
        }
        void round2()
        {
            clearBoard2();
          for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
          {
              for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
              {
                  // if the triggered location is already filled ignore it
                  if (gridArray2[rr, cc] == 1 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score + 1;
                      playerMoved = true;
                        gridArray2[rr, cc] = 2;
                    }
                  if (gridArray2[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score - 1;
                      playerMoved = true;
                        gridArray2[rr, cc] = 2;
                    }
              }
          }
        }
        void round3()
        {
            clearBoard3();
          for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
          {
              for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
              {
                  // if the triggered location is already filled ignore it
                  if (gridArray3[rr, cc] == 1 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score + 1;
                      playerMoved = true;
                        gridArray3[rr, cc] = 2;
                    }
                  if (gridArray3[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score - 1;
                      playerMoved = true;
                        gridArray3[rr, cc] = 2;
                    }
              }
          }
        }
        void round4()
        {
            clearBoard4();
          for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
          {
              for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
              {
                  // if the triggered location is already filled ignore it
                  if (gridArray4[rr, cc] == 1 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score + 1;
                      playerMoved = true;
                        gridArray4[rr, cc] = 2;
                    }
                  if (gridArray4[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score - 1;
                      playerMoved = true;
                        gridArray4[rr, cc] = 2;
                    }
              }
          }
        }
        void round5()
        {
            clearBoard5();
          for (int rr = 0; rr < numGridRows; rr++) //Indexes through the rows
          {
              for (int cc = 0; cc < numGridCols; cc++) //For each row, indexes through the column (Nested FOR loop)
              {
                  // if the triggered location is already filled ignore it
                  if (gridArray5[rr, cc] == 1 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score + 1;
                      playerMoved = true;
                        gridArray5[rr, cc] = 2;
                    }
                  if (gridArray5[rr, cc] == 0 && isPlayerHandInGridLocation(rr, cc, 1) &&
                      isPlayerHandClosed(1)) //Hand is closed within the given row and column
                  {
                      rowHit = rr; // Record row
                      colHit = cc; // Record column
                      score = score - 1;
                      playerMoved = true;
                        gridArray5[rr, cc] = 2;
                  }
              }
          }
        }








        bool start_game;

        // Student generated code should be in this location. Currently, the game is tic tac toe.
        void studentWork()
        {
            // Reminder: Boolean Value is either true or false, "playerMoved" will be used later on in the code
            bool playerMoved = false;
            Console.WriteLine("RowHit = " + rowHit + " ColHit= " + colHit);
            Console.WriteLine("Score = " + score);
            // rowHit and colHit are variables that are set to whatever row/col the player closes their hand in (You will see later on)

            // set instruction text -- naive set text, gets set EVERY function call
            // Newline => \r\n (Windows Format)
            setInstructionText(" 1. Select Green boxes for a point \r\n 2. Red boxes result in losing a point \r\n 3. Board refreshes every six seconds" +
            "\r\n 4. Score is summed after every round \r\n 5. Set the highest score! \n\n\n Score: " + score, System.Windows.Media.Brushes.Black, 30);

            // set button text

            setButtonText("New Game", 1);
            setButtonText("Second \nChance", 2);
            setButtonText("Doesn't \ndo \nanything", 3);

            // We use the gridArray as follows: 0 means the spot is BLANK, 1 means OCCUPIED by player 1, 2 means OCCUPIED by player 2
            // for the purposes of this bot (Computer AI), the user is always player 1

            // BOT MOVE

            // delayFrames can be set to a certain number in order to "stall" the program
            // REMEMBER: Camera operates at 30 frames per second. That means, if delayFrames == 90 then the program waits 3 seconds until it moves to the "else if" statement

            if (start_game)
            {


                if (delayFrames > 720)
                {
                    // Post-decrement delayFrames
                    delayFrames--;
                    round1();
                  
                    return;
                }
                else if (delayFrames > 540)
                {
                    // Post-decrement delayFrames
                    delayFrames--;
                    round2();
                 
                    return;
                }
                else if (delayFrames > 360)
                {
                    // Post-decrement delayFrames
                    delayFrames--;
                    round3();
                   
                    return;
                }
                else if (delayFrames > 180)
                {
                    // Post-decrement delayFrames
                    delayFrames--;
                    round4();
                    
                    return;
                }
                else if (delayFrames > 0)
                {
                    // Post-decrement delayFrames
                    delayFrames--;
                    round5();

                   
                    return;
                }


                else if (delayFrames == 0)
                {
                    isGameOver = true;
                    setInstructionText(" 1. Select Green boxes for a point \r\n 2. Red boxes result in losing a point \r\n 3. Board refreshes every six seconds" +
                                       "\r\n 4. Score is summed after every round \r\n 5. Set the highest score! \r\n 6. Press Second Chance to add on to your previous score \n\n\n Score: " + score + "\n GAME OVER", System.Windows.Media.Brushes.Black, 30);
                    return;
                }

                

            }
 
        }

        // Button Logic
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            start_game = true;
            delayFrames = 900;
            RandGridArray();
            score = 0;
            return;
        }


        // optional: add functionality
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            start_game = true;
            delayFrames = 900;

        }

        // optional: add functionality
        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            

            /* int numGridRows = 4, numGridCols = 4;
            uniGrid.Rows = numGridRows; uniGrid.Columns = numGridCols;
            //rows left->right
            rectArray = new Rectangle[numGridRows, numGridCols];
            for (int rr = 0; rr < numGridRows; rr++)
            {
                for (int cc = 0; cc < numGridCols; cc++)
                {
                    rectArray[rr, cc] = new Rectangle()
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        // Height = (int)(gridHeight / numGridRows),
                        //Width = (int)(gridWidth / numGridRows),
                    };
                    uniGrid.Children.Add(rectArray[rr, cc]);
                }
            }

            //student storage initialization
            gridArray1 = new int[numGridRows, numGridCols];
            gridArray2 = new int[numGridRows, numGridCols];
            gridArray3 = new int[numGridRows, numGridCols];
            gridArray4 = new int[numGridRows, numGridCols];
            gridArray5 = new int[numGridRows, numGridCols];
            RandGridArray();

            start_game = true;
            delayFrames = 900;
            RandGridArray();
            score = 0;
            */


        }

    } // end bracket of studentWork()
} // end bracket of MainWindow()
