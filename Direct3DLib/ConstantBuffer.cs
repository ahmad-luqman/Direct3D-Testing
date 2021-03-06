﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D10;

namespace Direct3DLib
{
	public class ConstantBuffer
	{
		private Type type;
		private float floVal;
		private int intVal;
		private Matrix matVal;
		private Vector3 vecVal;
		private bool requiresUpdate = true;
		private EffectVariable effectVar;
		private string variableName;
		public Matrix AsMatrix
		{
			get { return (Matrix)matVal; }
			set
			{
				Matrix m = (Matrix)matVal;
				if (m != value)
				{
					matVal = value;
					requiresUpdate = true;
				}
			}
		}
		public float AsFloat
		{
			get { return (float)floVal; }
			set
			{
				float m = (float)floVal;
				if (m != value)
				{
					floVal = value;
					requiresUpdate = true;
				}
			}
		}
		public int AsInt
		{
			get { return intVal; }
			set
			{
				int m = intVal;
				if (m != value)
				{
					intVal = value;
					requiresUpdate = true;
				}
			}
		}
		public Vector3 AsVector3
		{
			get { return (Vector3)vecVal; }
			set
			{
				Vector3 m = (Vector3)vecVal;
				if (m != value)
				{
					vecVal = value;
					requiresUpdate = true;
				}
			}
		}
		public ConstantBuffer(string variableName,float initialValue)
		{
			type = typeof(float);
			this.variableName = variableName;
			//EffectVariable var = effect.GetVariableByName(variableName); 
			floVal = (float)initialValue; 
			//objVar = var.AsScalar();
			requiresUpdate = true;
		}
		public ConstantBuffer(string variableName, int initialValue)
		{
			type = typeof(int);
			this.variableName = variableName;
			//EffectVariable var = effect.GetVariableByName(variableName);
			intVal = initialValue;
			//objVar = var.AsScalar();
			requiresUpdate = true;
		}
		public ConstantBuffer(string variableName, Matrix initialValue)
		{
			type = typeof(Matrix);
			this.variableName = variableName;
			//EffectVariable var = effect.GetVariableByName(variableName);
			matVal = (Matrix)initialValue;
			//objVar = var.AsMatrix();
			requiresUpdate = true;
		}
		public ConstantBuffer(string variableName, Vector3 initialValue)
		{
			type = typeof(Vector3);
			this.variableName = variableName;
			//EffectVariable var = effect.GetVariableByName(variableName);
			vecVal = (Vector3)initialValue;
			//objVar = var.AsVector();
			requiresUpdate = true;
		}

		public void Initialize(Effect effect)
		{
			effectVar = effect.GetVariableByName(variableName);
			requiresUpdate = true;
		}

		/// <summary>
		/// Writes the variable to the Graphics card if it has changed (ie, requiresUpdate = true)
		/// </summary>
		/// <returns>True if the variable has been updated, implying an effect.Apply() is called for</returns>
		public bool Apply()
		{
			if (requiresUpdate)
			{
				if (effectVar != null)
				{
					if (type == typeof(float))
						effectVar.AsScalar().Set(AsFloat);
					if (type == typeof(int))
						effectVar.AsScalar().Set(AsInt);
					if (type == typeof(Matrix))
						effectVar.AsMatrix().SetMatrix(AsMatrix);
					if (type == typeof(Vector3))
						effectVar.AsVector().Set(AsVector3);
					requiresUpdate = false;
					return true;
				}
			}
			return false;
		}
	}

	public class ConstantBufferHelper
	{
		private Effect effect;

		private ConstantBuffer lightAmbInt;
		private ConstantBuffer lightDirInt;
		private ConstantBuffer viewProj;
		private ConstantBuffer world;
		private ConstantBuffer lightDir;
		private ConstantBuffer localRot;
		private ConstantBuffer textureIndex;
		private List<ConstantBuffer> allBuffers = new List<ConstantBuffer>();

		public Matrix World { get { return world.AsMatrix; } set { world.AsMatrix = value; } }
		public Matrix ViewProj { get { return viewProj.AsMatrix; } set { viewProj.AsMatrix = value; } }
		public Matrix LocalRotation { get { return localRot.AsMatrix; } set { localRot.AsMatrix = value; } }
		public Vector3 LightDirection { get { return lightDir.AsVector3; } set { lightDir.AsVector3 = value; } }
		public float LightDirectionalIntensity { get { return lightDirInt.AsFloat; } set { lightDirInt.AsFloat = value; } }
		public float LightAmbientIntensity { get { return lightAmbInt.AsFloat; } set { lightAmbInt.AsFloat = value; } }
		public int TextureIndex { get { return textureIndex.AsInt; } set { textureIndex.AsInt = value; } }

		public ConstantBufferHelper()
		{
			allBuffers.Clear();
			world = new ConstantBuffer("World", Matrix.Identity);
			allBuffers.Add(world);
			lightDir = new ConstantBuffer("LightDir", new Vector3(1, 1, 1));
			allBuffers.Add(lightDir);
			localRot = new ConstantBuffer("LocalRotation", Matrix.Identity);
			allBuffers.Add(localRot);
			viewProj = new ConstantBuffer("ViewProj", Matrix.Identity);
			allBuffers.Add(viewProj);
			lightAmbInt = new ConstantBuffer("AmbientIntensity", 0.3f);
			allBuffers.Add(lightAmbInt);
			lightDirInt = new ConstantBuffer("DirectionalIntensity", 0.7f);
			allBuffers.Add(lightDirInt);
			textureIndex = new ConstantBuffer("TextureIndex", (int)0);
			allBuffers.Add(textureIndex);
		}

		public void Initialize(Effect effect)
		{
			this.effect = effect;
			foreach (ConstantBuffer cb in allBuffers)
				cb.Initialize(effect);
		}
		/*
		public void Update()
		{
			
		}
		 */

		public bool ApplyEffects()
		{
			bool doApply = false;
			foreach (ConstantBuffer cb in allBuffers)
				if (cb.Apply()) doApply = true;
			return doApply;
		}

	}
}
