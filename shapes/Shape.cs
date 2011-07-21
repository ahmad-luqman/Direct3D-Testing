﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using SlimDX;
using SlimDX.DXGI;
using System.Runtime.InteropServices;
using SlimDX.Direct3D10;
using Buffer = SlimDX.Direct3D10.Buffer;
using Device = SlimDX.Direct3D10.Device;
using SlimDX.D3DCompiler;
using System.Drawing;

namespace Direct3DLib
{
    /// <summary>
    /// A class that consists of a number of ColoredVertices, and an index buffer specifying
    /// how it is made up of flat triangles.
    /// </summary>
    [Serializable]
    public class Shape : Object3D, IRenderable, IDisposable
    {
        
        private bool mPick = true;
        public virtual bool CanPick { get { return mPick; } set { mPick = value; } }
		private Vertex [] mSelectedVerts = new Vertex[3];
		public Vertex[] SelectedVertices { get { return mSelectedVerts; } set { mSelectedVerts = value; } }

        private VertexList mVList = new VertexList();
        public VertexList Vertices { get { return mVList; } set { mVList = value; } }
		public int SelectedVertexIndex { get; set; }

        protected Device mDevice;
		private Color mSolidColor = Color.Empty;
		public Color SolidColor { get { return mSolidColor; } set { SetUniformColor(value); mSolidColor = value; Update(); } }

        public virtual PrimitiveTopology Topology { get { return Vertices.Topology; } set { Vertices.Topology = value; AutoGenerateIndices(); Update(); } }

		private int textureIndex = -1;
		public int TextureIndex { get { return textureIndex; } set { textureIndex = value; } }

        public void SetUniformColor(Color color)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertex v = Vertices[i];
				if (color == Color.Empty)
					v.Color = Vertex.FloatToColor(v.Position);
				else
	                v.Color = color;
                Vertices[i] = v;
            }
                
        }

        protected SlimDX.Direct3D10.Buffer vertexBuffer;
        protected SlimDX.Direct3D10.Buffer indexBuffer;
        protected InputLayout vertexLayout;

		

        public Shape()
            : base()
        {
            Vertices = new VertexList();
        }
        public Shape(Vertex[] vertices)
            : this()
        {
            Vertices = new VertexList(vertices);
        }

        public virtual void AutoGenerateIndices()
        {
            Vertices.Indices = null;
        }


		public void Update() { Update(mDevice); }
		public virtual void Update(Device device) { Update(device, null); }
        public virtual void Update(Device device, Effect effect)
        {
            if (device == null || device.Disposed) return;
			
            mDevice = device;
            // If there is less than 1 vertex then we can't make a point, let alone a shape!
            if (Vertices == null || Vertices.Count < 1) return;

            // Add Vertices to a datastream.
            DataStream dataStream = new DataStream(Vertices.NumBytes, true, true);
            dataStream.WriteRange(this.Vertices.ToArray());
            dataStream.Position = 0;

            
            // Create a new data buffer description and buffer
            BufferDescription desc = new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                //SizeInBytes = 3 * Marshal.SizeOf(typeof(ColoredVertex)),
                SizeInBytes = Vertices.NumBytes,
                Usage = ResourceUsage.Default
            };
            vertexBuffer = new SlimDX.Direct3D10.Buffer(device, dataStream, desc);
            dataStream.Close();

            // Get the shader effects signature
			if(effect == null)
				effect = Effect.FromString(device, Properties.Resources.RenderWithLighting, "fx_4_0");
			EffectPass effectPass = effect.GetTechniqueByIndex(0).GetPassByName("ColoredWithLighting");


            if (Vertices != null && Vertices.Count > 0)
            {
                // Set the input layout.
                InputElement[] inputElements = Vertices[0].GetInputElements();
                vertexLayout = new InputLayout(device, effectPass.Description.Signature, inputElements);

                // Draw Indexed
                if (Vertices.Indices != null && Vertices.Indices.Count > 0)
                {
                    DataStream iStream = new DataStream(sizeof(int) * Vertices.Indices.Count, true, true);
                    iStream.WriteRange(Vertices.Indices.ToArray());
                    iStream.Position = 0;
                    desc = new BufferDescription()
                    {
                        Usage = ResourceUsage.Default,
                        SizeInBytes = sizeof(int) * Vertices.Indices.Count,
                        BindFlags = BindFlags.IndexBuffer,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    };
                    indexBuffer = new Buffer(device, iStream, desc);
                    iStream.Close();

                    

                }
                else
                {
                    if (indexBuffer != null) indexBuffer.Dispose();
                    indexBuffer = null;
                }
            }
            else
            {
                if (vertexBuffer != null) vertexBuffer.Dispose();
                vertexBuffer = null;
            }
        }

        //public void Render(DeviceContext context, Matrix worldViewProj)
        public virtual void Render(Device context, ShaderHelper shaderHelper)
        {
            if (vertexBuffer != null && Topology != PrimitiveTopology.Undefined)
            {
                context.InputAssembler.SetInputLayout(vertexLayout);
                context.InputAssembler.SetPrimitiveTopology(Topology);
                
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
                if (indexBuffer != null)
                    context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);


				shaderHelper.ConstantBufferSet.World = World;
				shaderHelper.ConstantBufferSet.LocalRotation = RotationMatrix;
				shaderHelper.ConstantBufferSet.TextureIndex = TextureIndex;
				shaderHelper.ApplyEffects();

                if (indexBuffer != null)
                    context.DrawIndexed(Vertices.NumElements, 0, 0);
                else
                    context.Draw(Vertices.NumElements, 0);
            }
        }

        public virtual bool RayIntersects(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            if (Vertices == null || Vertices.Count < 1) return false;
			if (!this.CanPick) return false;
			bool ints = false;
			bool done = false;
			if (Vertices.Indices == null || Vertices.Indices.Count < 1)
			{
				if (Topology == PrimitiveTopology.TriangleList)
				{
					for (int i = 0; i < Vertices.Count-2; i += 3)
					{
						Vector3 v1 = Vector3.TransformCoordinate(Vertices[i].Position,World);
						Vector3 v2 = Vector3.TransformCoordinate(Vertices[i+1].Position,World);
						Vector3 v3 = Vector3.TransformCoordinate(Vertices[i +2].Position, World);
						float d = float.MaxValue;
						bool ii = Ray.Intersects(ray, v1,v2,v3, out d);
						if (ii && d < distance)
						{
							distance = d;
							ints = true;
							SelectedVertices[0] = Vertices[i];
							SelectedVertices[1] = Vertices[i+1];
							SelectedVertices[2] = Vertices[i+2];
							SelectedVertexIndex = i;
						}
					}
					done = true;
				}
			}
			
			// Use default method of a Bounding Box around the maximum extremities of the object if not yet done.
			if(!done)
			{
				BoundingBox bb = GetSurroundingBox(this);
				ints = Ray.Intersects(ray, bb, out distance);
			}
            return ints;
        }
        private BoundingBox GetSurroundingBox(IRenderable s)
        {
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            for (int i = 0; i < s.Vertices.Count; i++)
            {
                Vector3 v = s.Vertices[i].Position;
                v = Vector3.TransformCoordinate(v, s.World);
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }
            return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        public override void Dispose()
        {
            if(vertexBuffer != null) vertexBuffer.Dispose();
            if(indexBuffer != null) indexBuffer.Dispose();
            if(vertexLayout != null) vertexLayout.Dispose();
            base.Dispose();
        }
    }


}
