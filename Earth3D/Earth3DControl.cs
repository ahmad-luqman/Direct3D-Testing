﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using SlimDX;

namespace Direct3DLib
{
	public partial class Earth3DControl : Direct3DControl
	{
		#region Private Fields
		public const int TEXTURE_OFFSET = ShaderHelper.MAX_TEXTURES - (EarthTiles.TILE_COLUMNS * EarthTiles.TILE_ROWS);
		private List<string> localTextureFilenames = new List<string>();
		private List<string> externalTextureFilenames = new List<string>();
		private EarthTiles earthTiles = new EarthTiles();
		private Float3 previousCameraLocation = new Float3();
		private bool isInitialized { get { return Engine.IsInitialized; } }
		private EarthControlOptionsForm optionsForm = new EarthControlOptionsForm();
		#endregion

		public string debugString = "";
		private double currentLat;
		private double currentLong;
		private double currentEle;
		[CategoryAttribute("Camera, Lighting and Textures")]
		public LatLong CurrentLatLong
		{
			get
			{
				LatLong ll = earthTiles.ConvertCameraLocationToLatLong(new Float3(CameraLocation));
				currentLat = ll.Latitude; currentLong = ll.Longitude;
				return new LatLong(currentLat, currentLong);
			}
			set
			{
				currentLat = value.Latitude; currentLong = value.Longitude;
				SetCameraLocation(earthTiles.ConvertLatLongElevationToCameraLocation(value, CurrentElevation));
			}
		}
		[CategoryAttribute("Camera, Lighting and Textures")]
		public double CurrentElevation
		{
			get { currentEle = CameraLocation.Y; return currentEle; }
			set
			{
				currentEle = value;
				SetCameraLocation(earthTiles.ConvertLatLongElevationToCameraLocation(CurrentLatLong, value));
			}
		}
		public bool AutomaticallyDownloadMaps
		{
			get { return earthTiles.AutomaticallyDownloadMaps; }
			set { earthTiles.AutomaticallyDownloadMaps = value; }
		}
		public bool FixTerrain { get { return earthTiles.FixTerrain; } set { earthTiles.FixTerrain = value; } }
		public bool FixZoom { get { return earthTiles.FixZoom; } set { earthTiles.FixZoom = value; } }
		private bool UseTerrainData
		{
			get { return earthTiles.UseTerrainData; }
			set
			{
				Properties.Settings.Default.UseGISData = value;
				Properties.Settings.Default.Save(); 
				earthTiles.UseTerrainData = value;
			}
		}



		public Earth3DControl()
		{
			InitializeComponent();
			InitializeOthers();
			Engine.Camera.IsFirstPerson = true;
			earthTiles.EngineShapeList = Engine.ShapeList;
		}

		private void InitializeOthers()
		{
			DoubleClick += Earth3DControl_DoubleClick;
			CameraChanged +=new EventHandler(engine_CameraChanged);
			optionsForm.FormClosing += (o, ev) =>
			{
				optionsForm.Hide();
				ev.Cancel = true;
			};
			optionsForm.OptionsChanged += (o, ev) => { UpdateControlFromOptions(); };
		}

		private void engine_CameraChanged(object sender, EventArgs e)
		{
			if (this.isInitialized)
			{
				RestrictCameraElevation();
				if (previousCameraLocation.X != CameraLocation.X || previousCameraLocation.Z != CameraLocation.Z || previousCameraLocation.Y != CameraLocation.Y)
				{
					LatLong latLong = earthTiles.ConvertCameraLocationToLatLong(new Float3(CameraLocation));
					latLong = EarthProjection.CalculateNearestLatLongAtDelta(latLong, earthTiles.Delta);
					earthTiles.CameraLocationChanged(new Float3(CameraLocation));
				}
				UpdateDebugString();
			}
			previousCameraLocation = new Float3(CameraLocation);
		}

		private void UpdateDebugString()
		{
			if (SelectedObject != null)
			{
				Shape shape = SelectedObject as Shape;
				Matrix wvp = shape.World * CameraView * CameraProjection;
				BoundingBox newBB = shape.MaxBoundingBox;
				newBB = Direct3DEngine.BoundingBoxMultiplyMatrix(newBB, wvp);
				BoundingBox bbCam = new BoundingBox(new Vector3(-1.0f, -1.0f, 0), new Vector3(1.0f, 1.0f, 1.0f));
				bool onScreen = BoundingBox.Intersects(newBB, bbCam);
				debugString = printCorners(newBB)
					+ Environment.NewLine + BoundingBox.Intersects(newBB, bbCam);
			}
		}



		private string printCorners(Vector3[] corners)
		{
			string ret = printVector3(corners[0]);
			for (int i = 1; i < corners.Length; i ++)
			{
				ret += Environment.NewLine + printVector3(corners[i]);
			}
			return ret;
		}

		private string printCorners(BoundingBox bb)
		{
			Vector3[] corners = bb.GetCorners();
			return printCorners(corners);
		}

		private string printVector3(Vector3 vec)
		{
			string ret = "";
			ret += "" + vec.X.ToString("G8") + " ";
			ret += "" + vec.Y.ToString("G8") + " ";
			ret += "" + vec.Z.ToString("G8");
			return ret;
		}

		private void RestrictCameraElevation()
		{
			Float3 loc = new Float3(CameraLocation);
			if (loc.Y < -100)
			{
				loc.Y = -100;
				CameraLocation = loc;
			}
			if (loc.Y > (float)EarthTiles.MAX_ELEVATION)
			{
				loc.Y = (float)EarthTiles.MAX_ELEVATION;
				CameraLocation = loc;
			}
		}

		private void SetCameraLocation(Float3 camLoc)
		{
			CameraLocation = camLoc;
		}

		private void Earth3DControl_Load(object sender, EventArgs e)
		{
			if (this.isInitialized)
			{
				EarthControlOptionsForm.CheckGlobalSettings();
				UseTerrainData = Properties.Settings.Default.UseGISData;
				earthTiles.MapChanged += (o, ev) => { Engine.UpdateShapes(); };
				earthTiles.InitializeAtCameraLocation(new Float3(CameraLocation));
			}
		}

		private void Earth3DControl_DoubleClick(object sender, EventArgs e)
		{
			UpdateOptionsFromControl();
			optionsForm.Show();
		}

		private void UpdateControlFromOptions()
		{
			EarthTiles.MaxGoogleZoom = optionsForm.MaxGoogleZoom;
			earthTiles.Delta = optionsForm.Delta;
			FixTerrain = optionsForm.FixTerrain;
			KeyboardMovementSpeed = optionsForm.KeyboardSpeed;
			if (UseTerrainData != optionsForm.UseGIS)
			{
				UseTerrainData = optionsForm.UseGIS;
				EarthControlOptionsForm.UseGisCheckChanged();
			}
			FixZoom = optionsForm.FixZoom;
			AutomaticallyDownloadMaps = optionsForm.AutomaticallyDownloadMaps;
			Properties.Settings.Default.MapTerrainFolder = optionsForm.TerrainFolder;
			Properties.Settings.Default.MapTextureFolder = optionsForm.TextureFolder;
			Properties.Settings.Default.Save();
			CurrentLatLong = optionsForm.GotoLatLong;
			CurrentElevation = optionsForm.GotoElevation;
			UpdateOptionsFromControl();
		}
		private void UpdateOptionsFromControl()
		{
			Console.WriteLine("" + CurrentLatLong);
			optionsForm.GotoLatLong = CurrentLatLong;
			optionsForm.AutomaticallyDownloadMaps = AutomaticallyDownloadMaps;
			optionsForm.UseGIS = UseTerrainData;
			optionsForm.FixZoom = FixZoom;
			optionsForm.KeyboardSpeed = (int)KeyboardMovementSpeed;
			optionsForm.GotoElevation = CurrentElevation;
			optionsForm.MaxGoogleZoom = EarthTiles.MaxGoogleZoom;
			optionsForm.FixTerrain = FixTerrain;
			optionsForm.TerrainFolder = Properties.Settings.Default.MapTerrainFolder;
			optionsForm.TextureFolder = Properties.Settings.Default.MapTextureFolder;
			optionsForm.Delta = earthTiles.Delta;
		}

	}
}
