﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Map_Editor.Engine;
using Map_Editor.Engine.Map;
using Map_Editor.Engine.Models;
using Map_Editor.Engine.RenderManager;
using Map_Editor.Engine.Tools;
using Map_Editor.Misc;
using Map_Editor.Misc.Properties;
using Microsoft.Xna.Framework;
using XNA = Microsoft.Xna.Framework.Graphics;

namespace Map_Editor.Forms.Controls
{
    /// <summary>
    /// Interaction logic for AnimationTool.xaml
    /// </summary>
    public partial class AnimationTool : UserControl
    {
        /// <summary>
        /// Width and height of the images.
        /// </summary>
        public const int IMAGE_DIMENSION = 120;

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        public System.Windows.Forms.PropertyGrid Properties
        {
            get { return (System.Windows.Forms.PropertyGrid)PropertiesHost.Child; }
        }

        #region Image Colours

        /// <summary>
        /// Normal colour.
        /// </summary>
        public readonly SolidColorBrush COLOUR_NORMAL = new SolidColorBrush(Color.FromRgb(201, 210, 225));

        /// <summary>
        /// Hover colour.
        /// </summary>
        public readonly SolidColorBrush COLOUR_HOVER = new SolidColorBrush(Color.FromRgb(225, 201, 201));

        /// <summary>
        /// Selected colour.
        /// </summary>
        public readonly SolidColorBrush COLOUR_SELECTED = new SolidColorBrush(Color.FromRgb(170, 54, 54));

        #endregion

        #region Static Members

        /// <summary>
        /// Gets or sets a value indicating whether [generating images].
        /// </summary>
        /// <value><c>true</c> if [generating images]; otherwise, <c>false</c>.</value>
        public static bool GeneratingImages { get; set; }

        /// <summary>
        /// Gets or sets the model images.
        /// </summary>
        /// <value>The model images.</value>
        private static List<BitmapImage> modelImages { get; set; }

        /// <summary>
        /// Gets or sets the image thread.
        /// </summary>
        /// <value>The image thread.</value>
        public static Thread ImageThread { get; set; }

        /// <summary>
        /// Gets or sets the copied object.
        /// </summary>
        /// <value>The copied object.</value>
        public static IFO.BaseIFO CopiedObject { get; set; }

        #endregion

        #region Member Declarations

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public int Model { get; set; }

        /// <summary>
        /// Gets or sets the selected object.
        /// </summary>
        /// <value>The selected object.</value>
        private int selectedObject { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationTool"/> class.
        /// </summary>
        public AnimationTool()
        {
            InitializeComponent();

            if (modelImages == null || modelImages.Count != FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count - 1)
                LoadImages(this);
            else
            {
                for (int i = 0; i < modelImages.Count; i++)
                    AddImage(i);
            }

            DrawBoundingBoxes.IsChecked = ConfigurationManager.GetValue<bool>("Animation", "DrawBoundingBoxes");

            Width = double.NaN;
        }

        /// <summary>
        /// Handles the Click event of the DrawBoundingBoxes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DrawBoundingBoxes_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.SetValue("Animation", "DrawBoundingBoxes", DrawBoundingBoxes.IsChecked);
            ConfigurationManager.SaveConfig();
        }

        /// <summary>
        /// Selects the specified object.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public int Select(int index)
        {
            if (GeneratingImages)
            {
                MessageBox.Show("Please wait for all images to be generated", "Please wait...", MessageBoxButton.OK, MessageBoxImage.Error);

                return -1;
            }

            selectedObject = index;

            Add.IsChecked = false;
            Remove.IsEnabled = false;
            Clone.IsEnabled = false;
            Clone.IsChecked = false;

            IFO.BaseIFO ifoEntry = MapManager.Animation.WorldObjects[index].Entry;

            SelectModel(ifoEntry.ObjectID);

            Vector3 eularVector = ifoEntry.Rotation.ToEular();
            RotationYaw.Value = eularVector.X;
            RotationRoll.Value = eularVector.Y;
            RotationPitch.Value = eularVector.Z;

            Properties.SelectedObject = new ObjectProperty()
            {
                Description = ifoEntry.Description,
                EventID = ifoEntry.EventID,
                Position = ifoEntry.Position,
                Rotation = ifoEntry.Rotation,
                Scale = ifoEntry.Scale
            };

            Remove.IsEnabled = true;
            Clone.IsEnabled = true;

            App.Form.Cut.IsEnabled = true;
            App.Form.QuickCut.IsEnabled = true;

            App.Form.Copy.IsEnabled = true;
            App.Form.QuickCopy.IsEnabled = true;

            App.Form.Paste.IsEnabled = CopiedObject != null;
            App.Form.QuickPaste.IsEnabled = CopiedObject != null;

            return index;
        }

        /// <summary>
        /// Cleans this instance.
        /// </summary>
        public void Clean()
        {
            Remove.IsEnabled = false;
            Clone.IsEnabled = false;
            Clone.IsChecked = false;

            App.Form.Cut.IsEnabled = false;
            App.Form.QuickCut.IsEnabled = false;

            App.Form.Copy.IsEnabled = false;
            App.Form.QuickCopy.IsEnabled = false;

            App.Form.Paste.IsEnabled = false;
            App.Form.QuickPaste.IsEnabled = false;

            Properties.SelectedObject = null;

            RotationYaw.Value = 0.0f;
            RotationPitch.Value = 0.0f;
            RotationRoll.Value = 0.0f;

            selectedObject = -1;
        }

        /// <summary>
        /// Selects the model.
        /// </summary>
        /// <param name="index">The index.</param>
        public void SelectModel(int index)
        {
            imageHost_MouseLeftButtonDown(((StackPanel)(((StackPanel)ImageView.Content).Children[(int)Math.Floor((float)index / 2.0f)])).Children[index % 2], null);

            ImageView.ScrollToVerticalOffset(ImageOffset(index));
        }

        /// <summary>
        /// Calculates the vertical offset of a model image.
        /// </summary>
        /// <param name="index">The model index.</param>
        /// <returns>The vertical offset.</returns>
        public int ImageOffset(int index)
        {
            return (int)Math.Floor((float)index / 2.0f) * (IMAGE_DIMENSION + 7);
        }

        #region Static Image Functions

        /// <summary>
        /// Loads the images.
        /// </summary>
        /// <param name="toolWindow">The tool window.</param>
        public static void LoadImages(AnimationTool toolWindow)
        {
            DateTime loadStart = DateTime.Now;
            Output.WriteLine(Output.MessageType.Event, "Generating Animation Images");

            modelImages = new List<BitmapImage>();

            ImageThread = new Thread(new ThreadStart(delegate
            {
                GeneratingImages = true;

                using (XNA.GraphicsDevice graphicsDevice = new XNA.GraphicsDevice(XNA.GraphicsAdapter.DefaultAdapter, XNA.DeviceType.Hardware, new System.Windows.Forms.Panel().Handle, new XNA.PresentationParameters()
                {
                    AutoDepthStencilFormat = XNA.DepthFormat.Depth24,
                    BackBufferCount = 1,
                    BackBufferFormat = XNA.SurfaceFormat.Color,
                    BackBufferHeight = IMAGE_DIMENSION,
                    BackBufferWidth = IMAGE_DIMENSION,
                    EnableAutoDepthStencil = true,
                    FullScreenRefreshRateInHz = 0,
                    IsFullScreen = false,
                    MultiSampleQuality = 0,
                    MultiSampleType = XNA.MultiSampleType.NonMaskable,
                    PresentationInterval = XNA.PresentInterval.One,
                    PresentOptions = XNA.PresentOptions.None,
                    RenderTargetUsage = XNA.RenderTargetUsage.DiscardContents,
                    SwapEffect = XNA.SwapEffect.Discard
                }))
                {
                    graphicsDevice.RenderState.CullMode = XNA.CullMode.None;

                    XNA.Viewport viewport = graphicsDevice.Viewport;

                    viewport.Width = IMAGE_DIMENSION;
                    viewport.Height = IMAGE_DIMENSION;

                    graphicsDevice.Viewport = viewport;

                    using (XNA.BasicEffect shader = new XNA.BasicEffect(graphicsDevice, null))
                    {
                        int id = 0;

                        for (int i = 0; i < FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count; i++)
                        {
                            XNA.RenderTarget2D renderTarget = new XNA.RenderTarget2D(graphicsDevice, IMAGE_DIMENSION, IMAGE_DIMENSION, 1, graphicsDevice.PresentationParameters.BackBufferFormat, graphicsDevice.PresentationParameters.MultiSampleType, graphicsDevice.PresentationParameters.MultiSampleQuality, graphicsDevice.PresentationParameters.RenderTargetUsage);

                            graphicsDevice.SetRenderTarget(0, renderTarget);

                            graphicsDevice.Clear(XNA.ClearOptions.Target | XNA.ClearOptions.DepthBuffer, new XNA.Color(245, 247, 250), 1.0f, 0);

                            BoundingBox boundingBox = new BoundingBox();
                            string[] objectRow = FileManager.STBs["LIST_MORPH_OBJECT"].Cells[i].ToArray();

                            if (File.Exists(objectRow[2]) && File.Exists(objectRow[4]))
                            {
                                ZMS models = new ZMS();
                                models.Load(objectRow[2]);

                                Vector3[] modelPoints = new Vector3[models.VertexCount];

                                for (int j = 0; j < modelPoints.Length; j++)
                                    modelPoints[j] = models.Vertices[j].Position;

                                boundingBox = BoundingBox.CreateFromPoints(modelPoints);

                                viewport.MinDepth = 1.0f;
                                viewport.MaxDepth = 50000.0f;

                                Microsoft.Xna.Framework.Matrix view = Microsoft.Xna.Framework.Matrix.Identity;
                                Microsoft.Xna.Framework.Matrix projection = Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, viewport.AspectRatio, viewport.MinDepth, viewport.MaxDepth);

                                BoundingSphere modelSphere = BoundingSphere.CreateFromBoundingBox(boundingBox);

                                Vector3 cameraPosition = Vector3.Transform(modelSphere.Center + (Vector3.Forward * ((modelSphere.Radius + 0.5f) / (float)Math.Sin(MathHelper.PiOver4 / 2))), Microsoft.Xna.Framework.Matrix.CreateRotationX(MathHelper.ToRadians(270)));

                                view = Microsoft.Xna.Framework.Matrix.CreateLookAt(cameraPosition, modelSphere.Center, Vector3.Down);

                                using (XNA.Texture2D texture = XNA.Texture2D.FromFile(graphicsDevice, objectRow[4]))
                                {
                                    shader.World = Microsoft.Xna.Framework.Matrix.Identity;
                                    shader.View = view;
                                    shader.Projection = projection;

                                    shader.DiffuseColor = Microsoft.Xna.Framework.Graphics.Color.White.ToVector3();
                                    shader.Texture = texture;
                                    shader.TextureEnabled = true;

                                    shader.Begin();

                                    graphicsDevice.VertexDeclaration = new XNA.VertexDeclaration(graphicsDevice, ZMS.Vertex.VertexElements);
                                    graphicsDevice.Indices = models.CreateIndexBuffer(graphicsDevice);
                                    graphicsDevice.Vertices[0].SetSource(models.CreateVertexBuffer(graphicsDevice), 0, ZMS.Vertex.SIZE_IN_BYTES);

                                    graphicsDevice.RenderState.AlphaBlendEnable = true;
                                    graphicsDevice.RenderState.AlphaTestEnable = true;
                                    graphicsDevice.RenderState.SourceBlend = TextureManager.BlendingMode(Convert.ToInt32(objectRow[10]));
                                    graphicsDevice.RenderState.DestinationBlend = TextureManager.BlendingMode(Convert.ToInt32(objectRow[11]));
                                    graphicsDevice.RenderState.BlendFunction = XNA.BlendFunction.Add;
                                    graphicsDevice.RenderState.AlphaFunction = XNA.CompareFunction.GreaterEqual;
                                    graphicsDevice.RenderState.CullMode = XNA.CullMode.None;
                                    graphicsDevice.RenderState.DepthBufferEnable = true;
                                    graphicsDevice.RenderState.DepthBufferWriteEnable = true;

                                    for (int k = 0; k < shader.CurrentTechnique.Passes.Count; k++)
                                    {
                                        shader.CurrentTechnique.Passes[k].Begin();

                                        graphicsDevice.DrawIndexedPrimitives(XNA.PrimitiveType.TriangleList, 0, 0, models.VertexCount, 0, models.IndexCount);

                                        shader.CurrentTechnique.Passes[k].End();
                                    }

                                    shader.End();
                                }
                            }

                            graphicsDevice.SetRenderTarget(0, null);

                            renderTarget.GetTexture().Save("AnimationImage.bmp", XNA.ImageFileFormat.Bmp);

                            byte[] buffer = File.ReadAllBytes("AnimationImage.bmp");

                            BitmapImage previewImage = new BitmapImage();
                            previewImage.BeginInit();
                            previewImage.StreamSource = new MemoryStream(buffer);
                            previewImage.EndInit();
                            previewImage.Freeze();

                            modelImages.Add(previewImage);

                            int currentID = id;

                            toolWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)delegate
                            {
                                toolWindow.AddImage(currentID);

                                return null;

                            }, null);

                            renderTarget.Dispose();
                            renderTarget = null;

                            id++;

                            if (id == (FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count / 5))
                                Output.WriteLine(Output.MessageType.Normal, "- 20%...");
                            else if (id == (FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count / 5) * 2)
                                Output.WriteLine(Output.MessageType.Normal, "- 40%...");
                            else if (id == (FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count / 5) * 3)
                                Output.WriteLine(Output.MessageType.Normal, "- 60%...");
                            else if (id == (FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count / 5) * 4)
                                Output.WriteLine(Output.MessageType.Normal, "- 80%...");
                        }
                    }

                    File.Delete("AnimationImage.bmp");

                    Output.WriteLine(Output.MessageType.Normal, "- 100%...");
                    Output.WriteLine(Output.MessageType.Event, string.Format("Created {0} Images in {1} Second(s)", FileManager.STBs["LIST_MORPH_OBJECT"].Cells.Count, (DateTime.Now - loadStart).TotalSeconds));
                }

                GeneratingImages = false;
            }));
            ImageThread.IsBackground = true;
            ImageThread.Start();
        }

        /// <summary>
        /// Clears the images.
        /// </summary>
        public static void ClearImages()
        {
            if (modelImages == null)
                return;

            if (ImageThread.IsAlive)
                ImageThread.Abort();

            modelImages.Clear();
            modelImages = null;
        }

        #endregion

        /// <summary>
        /// Adds the image.
        /// </summary>
        /// <param name="index">The index.</param>
        public void AddImage(int index)
        {
            if (index % 2 == 0)
            {
                ((StackPanel)ImageView.Content).Children.Add(new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 7, 0, 0)
                });
            }

            Border imageHost = new Border()
            {
                BorderBrush = COLOUR_NORMAL,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(7, 0, 0, 0),
                Width = IMAGE_DIMENSION,
                Height = IMAGE_DIMENSION,
                SnapsToDevicePixels = true,
                Cursor = Cursors.Hand,
                Tag = index,
                ToolTip = string.Format("ID: {0}", index),
                Child = new Image()
                {
                    Margin = new Thickness(0),
                    Width = IMAGE_DIMENSION,
                    Height = IMAGE_DIMENSION,
                    Source = modelImages[index],
                    SnapsToDevicePixels = true
                }
            };

            imageHost.MouseEnter += new MouseEventHandler(imageHost_MouseEnter);
            imageHost.MouseLeave += new MouseEventHandler(imageHost_MouseLeave);
            imageHost.MouseLeftButtonDown += new MouseButtonEventHandler(imageHost_MouseLeftButtonDown);

            ((StackPanel)(((StackPanel)ImageView.Content).Children[((StackPanel)ImageView.Content).Children.Count - 1])).Children.Add(imageHost);
        }

        #region Image Events

        /// <summary>
        /// Handles the MouseEnter event of the imageHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void imageHost_MouseEnter(object sender, MouseEventArgs e)
        {
            Border imageHost = (Border)sender;

            if (imageHost.BorderBrush == COLOUR_SELECTED)
                return;

            imageHost.BorderBrush = COLOUR_HOVER;
        }

        /// <summary>
        /// Handles the MouseLeave event of the imageHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseEventArgs"/> instance containing the event data.</param>
        private void imageHost_MouseLeave(object sender, MouseEventArgs e)
        {
            Border imageHost = (Border)sender;

            if (imageHost.BorderBrush == COLOUR_SELECTED)
                return;

            imageHost.BorderBrush = COLOUR_NORMAL;
        }

        /// <summary>
        /// Handles the MouseLeftButtonDown event of the imageHost control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void imageHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GeneratingImages)
            {
                MessageBox.Show("Please wait for all images to be generated", "Please wait...", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Border imageHost = (Border)sender;

            if (imageHost.BorderBrush == COLOUR_SELECTED)
                return;

            if (Model >= 0)
            {
                Border previousModel = (Border)((StackPanel)(((StackPanel)ImageView.Content).Children[(int)Math.Floor((float)Model / 2.0f)])).Children[Model % 2];

                previousModel.BorderBrush = COLOUR_NORMAL;
                previousModel.Cursor = Cursors.Hand;
            }

            imageHost.BorderBrush = COLOUR_SELECTED;
            imageHost.Cursor = Cursors.Arrow;

            Model = (int)imageHost.Tag;

            if (!Add.IsEnabled)
                Add.IsEnabled = true;

            if (Remove.IsEnabled || Add.IsChecked == true)
                ((Animation)ToolManager.Tool).ChangeModel(Model);
        }

        #endregion

        #region Button Events

        /// <summary>
        /// Handles the Checked event of the Add control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Add_Checked(object sender, RoutedEventArgs e)
        {
            if (Clone.IsChecked == true)
                Clone.IsChecked = false;

            MapManager.Animation.Tool.StartAdding(Model);
        }

        /// <summary>
        /// Handles the Unchecked event of the Add control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Add_Unchecked(object sender, RoutedEventArgs e)
        {
            MapManager.Animation.Tool.StopAdding();
        }

        /// <summary>
        /// Handles the Click event of the Remove control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            MapManager.Animation.Tool.Remove(false);
        }

        /// <summary>
        /// Handles the Checked event of the Clone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Clone_Checked(object sender, RoutedEventArgs e)
        {
            if (Add.IsChecked == true)
                Add.IsChecked = false;

            MapManager.Animation.Tool.StartAdding(Model);
        }

        /// <summary>
        /// Handles the Unchecked event of the Clone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Clone_Unchecked(object sender, RoutedEventArgs e)
        {
            MapManager.Animation.Tool.StopAdding();
        }

        /// <summary>
        /// Handles the Click event of the Find control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Find_Click(object sender, RoutedEventArgs e)
        {
            if (GeneratingImages)
            {
                MessageBox.Show("Please wait for all images to be generated", "Please wait...", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Search findObject = new Search(Forms.Search.FindType.GoTo);

            findObject.GoToClicked += new Search.GoTo(delegate(int value)
            {
                if (value < FileManager.ZSCs["Decoration"].Objects.Count)
                {
                    if (MessageBox.Show("Object found, do you wish to select it?", "Select Object", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        SelectModel(value);

                        findObject.Close();
                    }

                    return true;
                }

                return false;
            });

            findObject.ShowDialog();
        }

        #endregion

        /// <summary>
        /// Handles the PropertyValueChanged event of the Properties control.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.PropertyValueChangedEventArgs"/> instance containing the event data.</param>
        private void Properties_PropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
        {
            ObjectProperty objectValues = (ObjectProperty)Properties.SelectedObject;

            IFO.BaseIFO ifoEntry = MapManager.Animation.WorldObjects[selectedObject].Entry;

            UndoManager.AddCommand(new Engine.Commands.Animation.ValueChanged()
            {
                ObjectID = selectedObject,
                Object = MapManager.Animation.WorldObjects[selectedObject],
                OldValue = new Engine.Commands.Animation.ValueChanged.ObjectValue()
                {
                    Description = ifoEntry.Description,
                    EventID = ifoEntry.EventID,
                    Position = ifoEntry.Position,
                    Rotation = ifoEntry.Rotation,
                    Scale = ifoEntry.Scale
                },
                NewValue = new Engine.Commands.Animation.ValueChanged.ObjectValue()
                {
                    Description = objectValues.Description,
                    EventID = objectValues.EventID,
                    Position = objectValues.Position,
                    Rotation = objectValues.Rotation,
                    Scale = objectValues.Scale
                }
            });

            ifoEntry.Description = objectValues.Description;
            ifoEntry.EventID = objectValues.EventID;

            MapManager.Animation.Tool.ChangeWorld(objectValues.Position, objectValues.Rotation, objectValues.Scale);
        }

        /// <summary>
        /// Handles the ValueChanged event of the Rotation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedPropertyChangedEventArgs&lt;System.Double&gt;"/> instance containing the event data.</param>
        private void Rotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!Remove.IsEnabled)
                return;

            IFO.BaseIFO ifoEntry = MapManager.Animation.WorldObjects[selectedObject].Entry;

            MapManager.Animation.Tool.ChangeWorld(ifoEntry.Position, Quaternion.CreateFromYawPitchRoll((float)RotationRoll.Value, (float)RotationYaw.Value, (float)RotationPitch.Value), ifoEntry.Scale);
        }

        /// <summary>
        /// Handles the PreviewMouseUp event of the RotationRoll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void Rotation_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Properties.SelectedObject == null)
                return;

            ObjectProperty objectValues = (ObjectProperty)Properties.SelectedObject;

            IFO.BaseIFO ifoEntry = MapManager.Animation.WorldObjects[selectedObject].Entry;

            UndoManager.AddCommand(new Engine.Commands.Animation.ValueChanged()
            {
                ObjectID = selectedObject,
                Object = MapManager.Animation.WorldObjects[selectedObject],
                OldValue = new Engine.Commands.Animation.ValueChanged.ObjectValue()
                {
                    Description = objectValues.Description,
                    EventID = objectValues.EventID,
                    Position = objectValues.Position,
                    Rotation = objectValues.Rotation,
                    Scale = objectValues.Scale
                },
                NewValue = new Engine.Commands.Animation.ValueChanged.ObjectValue()
                {
                    Description = ifoEntry.Description,
                    EventID = ifoEntry.EventID,
                    Position = ifoEntry.Position,
                    Rotation = ifoEntry.Rotation,
                    Scale = ifoEntry.Scale
                }
            });

            if (((ObjectProperty)Properties.SelectedObject).Rotation != ifoEntry.Rotation)
            {
                ((ObjectProperty)Properties.SelectedObject).Rotation = ifoEntry.Rotation;
                Properties.Refresh();
            }
        }
    }
}