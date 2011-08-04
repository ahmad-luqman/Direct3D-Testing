﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;


namespace Direct3DLib
{
	public class StaticMapFactory
	{
		#region Singleton Constructor
		private static StaticMapFactory instance;
		private StaticMapFactory() { }
		public static StaticMapFactory Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new StaticMapFactory();
				}
				return instance;
			}
		}
		#endregion

		private MapWebAccessor webAccessor = new MapWebAccessor();
		private MapFileAccessor fileAccessor = new MapFileAccessor();
		private NullImage nullImage = new NullImage();

		private int initialZoomLevel = 0;

		private string nullString = "";
		private bool automaticallyDownloadMaps = false;
		public bool AutomaticallyDownloadMaps
		{
			get { return automaticallyDownloadMaps; }
			set { automaticallyDownloadMaps = value; }
		}

		public Image GetTiledImage(LatLong bottomLeftLocation, int desiredZoomLevel, int logDelta)
		{
			initialZoomLevel = desiredZoomLevel;
			nullString = "bottomLeft: " + bottomLeftLocation + "\ndesiredZoom: " + desiredZoomLevel;
			return RecursivelyGetTiledImage(bottomLeftLocation, desiredZoomLevel, logDelta);
		}

		private Image RecursivelyGetTiledImage(LatLong bottomLeftLocation, int desiredZoomLevel, int logDelta)
		{
			if (desiredZoomLevel < 0)
			{
				return nullImage.ImageClone;
			}
			int minZoomLevel = CalculateZoomFromLogDelta(logDelta);
			if (minZoomLevel == desiredZoomLevel)
			{
				return GetImageFromSource(bottomLeftLocation, desiredZoomLevel, logDelta);
			}
			else if (minZoomLevel > desiredZoomLevel)
			{
				return CropFromLargerImage(bottomLeftLocation, desiredZoomLevel, logDelta);
			}
			else
			{
				return StitchFromImageTiles(bottomLeftLocation, desiredZoomLevel, logDelta);
			}
		}


		private Image CropFromLargerImage(LatLong bottomLeftLocation, int desiredZoomLevel, int logDelta)
		{
			int MinTextureSize = 8;
			double delta = Math.Pow(2.0,logDelta+1);
			LatLong newLocation = EarthProjection.CalculateNearestLatLongAtDelta(bottomLeftLocation, delta,false);
			Image image = RecursivelyGetTiledImage(newLocation, desiredZoomLevel, logDelta+1);
			float width = image.Width / 2;
			if (width < MinTextureSize) width = MinTextureSize;
			float height = image.Height / 2;
			if (height < MinTextureSize) height = MinTextureSize;
			float yOffset = height;
			if (bottomLeftLocation.Latitude > newLocation.Latitude) yOffset = 0;
			float xOffset = 0;
			if (bottomLeftLocation.Longitude > newLocation.Longitude) xOffset = width;
			return ImageConverter.CropImage(image,new RectangleF(xOffset,yOffset,width,height));
		}

		private Image StitchFromImageTiles(LatLong bottomLeftLocation, int desiredZoomLevel, int logDelta)
		{
			int nTiles = 2;
			Image[] imageTiles = new Image[nTiles * nTiles];
			int newLogDelta = logDelta - 1;
			double delta = Math.Pow(2.0, newLogDelta);
			for (int i = 0; i < nTiles; i++)
			{
				for (int k = 0; k < nTiles; k++)
				{
					LatLong tileLocation = new LatLong(bottomLeftLocation.Latitude + delta * i, bottomLeftLocation.Longitude + delta * k);
					imageTiles[(nTiles - i - 1) * nTiles + k] = RecursivelyGetTiledImage(tileLocation,desiredZoomLevel,newLogDelta);
				}
			}
			return ImageConverter.StitchImages(imageTiles,nTiles,nTiles);
		}


		private Image GetImageFromSource(LatLong bottomLeftLocation, int desiredZoomLevel, int logDelta)
		{
			int imageDelta = CalculateLogDeltaFromZoom(desiredZoomLevel);
			LatLong centreLocation = CalculateCentreLocation(bottomLeftLocation, imageDelta);
			MapDescriptor d = new MapDescriptor(centreLocation.Latitude, centreLocation.Longitude, desiredZoomLevel);
			Image image = GetImageFromFile(d);
			if (image == null)
			{
				FetchImageFromWeb(d);
				return RecursivelyGetTiledImage(bottomLeftLocation, desiredZoomLevel - 1, logDelta);
			}
			return image;
		}

		private void FetchImageFromWeb(MapDescriptor descriptor)
		{
			if (descriptor.ZoomLevel == initialZoomLevel)
			{
				if (AutomaticallyDownloadMaps)
					webAccessor.FetchAndSaveImageInNewThread(descriptor);
			}
		}

		private int CalculateZoomFromLogDelta(int logDelta)
		{
			return 9 - logDelta;
		}

		private int CalculateLogDeltaFromZoom(int zoomLevel)
		{
			return 9 - zoomLevel;
		}

		private LatLong CalculateCentreLocation(LatLong bottomLeftLocation, int logDelta)
		{
			double delta = Math.Pow(2.0, logDelta);
			double lat = bottomLeftLocation.Latitude + delta/2.0;
			double lng = bottomLeftLocation.Longitude + delta/2.0;
			return new LatLong(lat, lng);
		}

		public Image GetImageFromFile(MapDescriptor description)
		{
			nullImage.Text = nullString + "\n" + description;
			description.MapState = MapDescriptor.MapImageState.Partial;
			Image image = fileAccessor.GetImage(description);
			if (image == null) return null;
			RectangleF bounds = EarthProjection.CalculateImageBoundsAtLatitude(image.Width,image.Height,description.Latitude);
			image = ImageConverter.CropImage(image, bounds);
			return image;
		}

	}

	public class EarthProjection
	{
		private static double XScale = 256.0 / 360.0;

		public static double GetYScaleAtLatitude(double latitude)
		{
			double sec = 1 / Math.Cos(Math.Abs(latitude) * Math.PI / 180.0);
			double ys = XScale * sec;
			if (ys > 1) Console.WriteLine("Extreme Latitude: " + latitude + ", " + ys);
			return ys;
		}

		public static double GetXScaleAtLatitude(double latitude)
		{
			return XScale;
		}

		public static RectangleF CalculateImageBoundsAtLatitude(int imageWidth, int imageHeight, double latitude)
		{
			double w = (double)(imageWidth);
			double h = (double)(imageHeight);
			double xs = GetXScaleAtLatitude(latitude);
			double ys = GetYScaleAtLatitude(latitude);
			double width = xs * w;
			double height = ys * h;
			double left = (w - width) / 2.0;
			double top = Math.Floor((h - height) / 2.0);
			return new RectangleF((float)left, (float)top, (float)width, (float)height);
		}

		public static LatLong CalculateNearestLatLongAtDelta(LatLong latLong, double delta)
		{
			return CalculateNearestLatLongAtDelta(latLong, delta, true);
		}

		public static LatLong CalculateNearestLatLongAtDelta(LatLong latLong, double delta, bool roundToNearest)
		{
			double dLat = latLong.Latitude / delta;
			double dLng = latLong.Longitude / delta;
			if (!roundToNearest)
			{
				dLat = Math.Floor(dLat);
				dLng = Math.Floor(dLng);
			}
			long lat = Convert.ToInt64(dLat);
			long lng = Convert.ToInt64(dLng);
			LatLong ret = new LatLong((double)lat * delta, (double)lng * delta);
			return ret;
		}

		public static int GetZoomFromElevation(double elevation)
		{
			if (elevation <= 0)
				return EarthTiles.MaxGoogleZoom;
			double zoom = 25.0 - Math.Log(elevation, 2.0);
			int z = (int)zoom;
			return z;
		}
	}
}
