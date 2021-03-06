﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using SlimDX;

namespace Direct3DExtensions
{
	public class MeshFileReader
	{
		private delegate Mesh CreateShapeFromFile(Stream stream);
		public enum SupportedFileType { Unknown, STL, DXF, HGT }
		private static Dictionary<string, CreateShapeFromFile> TypeDictionary = new Dictionary<string, CreateShapeFromFile>
		{
			{".hgt",HGTFileReader.CreateFromStream},
			{"unknown",MeshFileReader.CreateFromUnknownFile}
		};
		/*
			{".stl",ShapeSTLFactory.CreateFromStream},
			{".dxf",ComplexShapeFactory.CreateFromDxfFile},
			{"jpg.",ShapeImageFactory.CreateFromStream},
			{"png.",ShapeImageFactory.CreateFromStream},
			{"bmp.",ShapeImageFactory.CreateFromStream},
			{"jpeg.",ShapeImageFactory.CreateFromStream}
		};
		 */

		public static string SupportedFileTypeFilter
		{
			get
			{
				string fileNames = "3D Model Files";
				string fileExts = "|";
				bool first = true;
				foreach (string ext in TypeDictionary.Keys)
				{
					if (ext.Substring(0, 1).Equals("."))
					{
						if (!first) fileExts += ";"; first = false;
						fileExts += "*" + ext;
					}
				}
				string allFiles = "|All Files|*.*";
				return fileNames + fileExts + allFiles;
			}
		}

		/// <summary>
		/// Reads a ComplexShape structure from a File.
		/// This method attempts to deduce what the file format is from the file extension
		/// and header contents.
		/// </summary>
		/// <param name="filename">Path of the file to read</param>
		/// <returns></returns>
		public static Mesh CreateFromFile(string filename)
		{
			Stream stream = new FileStream(filename, FileMode.Open);
			return CreateFromStream(stream, filename);
		}

		public static Mesh CreateFromStream(Stream stream, string filename)
		{
			CreateShapeFromFile functionDelegate = DetermineFileType(filename);
			Mesh retShape = functionDelegate(stream);
			return retShape;
		}

		private static CreateShapeFromFile DetermineFileType(string filename)
		{
			string key = GetFileExtension(filename);
			if (TypeDictionary.ContainsKey(key))
				return TypeDictionary[key];
			return MeshFileReader.CreateFromUnknownFile;
		}

		private static Mesh CreateFromUnknownFile(Stream stream)
		{
			throw new ArgumentException("Cannot create shape from unknown file type: " + stream);
		}

		private static string GetFileExtension(string filename)
		{
			return Path.GetExtension(filename).ToLower();
		}

		/// <summary>
		/// Reads a ComplexShape structure from a DXF file.
		/// </summary>
		/// <param name="filename">Path of the file to read</param>
		/// <returns></returns>
		private static Mesh CreateFromDxfFile(Stream stream)
		{
			throw new NotImplementedException("Create From DXF File not implemented. " + stream);
		}


		public static bool CheckNormal(Vector3 norm, Vector3 v1, Vector3 v2, Vector3 v3)
		{
			Vector3 calcNorm;
			return CheckNormal(norm, v1, v2, v3, out calcNorm);
		}
		public static bool CheckNormal(Vector3 norm, Vector3 v1, Vector3 v2, Vector3 v3, out Vector3 calcNorm)
		{
			norm = Vector3.Normalize(norm);
			float tol = 0.2f;
			calcNorm = new Plane(v1, v2, v3).Normal;
			if (Math.Abs(calcNorm.X - norm.X) > tol || Math.Abs(calcNorm.Y - norm.Y) > tol
				|| Math.Abs(calcNorm.Z - norm.Z) > tol)
			{
				return false;
			}
			return true;
		}
	}
}
