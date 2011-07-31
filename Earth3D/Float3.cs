﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Direct3DLib
{
	[TypeConverter(typeof(Float3TypeConverter))]
	public class Float3
	{
		private SlimDX.Vector3 vec = new SlimDX.Vector3();
		public Float3() { }
		public Float3(float x, float y, float z) { vec.X = x; vec.Y = y; vec.Z = z; }
		public Float3(SlimDX.Vector3 original) { vec = original; }
		public Float3(Float3 original) : this(original.AsVector3()) { }
		public float X { get { return vec.X; } set { vec.X = value; } }
		public float Y { get { return vec.Y; } set { vec.Y = value; } }
		public float Z { get { return vec.Z; } set { vec.Z = value; } }
		public SlimDX.Vector3 AsVector3() { return vec; }
		public override string ToString()
		{
			return "" + vec.X + ", " + vec.Y + ", " + vec.Z;
		}
		public static bool TryParse(string s, out Float3 result)
		{
			result = new Float3();
			string [] split = s.Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries);
			if(split.Length != 3) return false;
			float f = 0;
			if(!float.TryParse(split[0],out f)) return false;
			result.X = f;
			if (!float.TryParse(split[1], out f)) return false;
			result.Y = f;
			if (!float.TryParse(split[2], out f)) return false;
			result.Z = f;
			return true;
		}
	}

	public class Float3TypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType.Equals(typeof(SlimDX.Vector3)))
				return true;
			if (sourceType.Equals(typeof(string)))
				return true;
			return false;
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType.Equals(typeof(SlimDX.Vector3)))
				return true;
			if(destinationType.Equals(typeof(string)))
				return true;
			return false;
		}

		public override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
		{
			Float3 value = new Float3();
			value.X = (float)propertyValues["X"];
			value.Y = (float)propertyValues["Y"];
			value.Z = (float)propertyValues["Z"];
			return value;
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if(value is SlimDX.Vector3)
				return new Float3((SlimDX.Vector3)value);
			Float3 ret = new Float3();
			if (!Float3.TryParse(value as string, out ret))
				throw new FormatException("Could not convert from string '" + value + "' to Float3");
			return ret;
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{			
			Float3 thisVal = value as Float3;
			if (destinationType == typeof(SlimDX.Vector3))
			{
				return new SlimDX.Vector3(thisVal.X, thisVal.Y, thisVal.Z);
			}
			else
				return "" + thisVal;
		}
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			return TypeDescriptor.GetProperties(typeof(Float3));
		}
	}
}