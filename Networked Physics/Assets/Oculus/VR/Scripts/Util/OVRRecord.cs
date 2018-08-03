using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.OVR.Scripts
{
	public class Record
	{
		public string category;
		public string message;
		public Record(string cat, string msg)
		{
			category = cat;
			message = msg;
		}
	}

	public class RangedRecord : Record
	{
		public float value;
		public float min;
		public float max;
		public RangedRecord(string cat, string msg, float val, float minVal, float maxVal)
			: base(cat, msg)
		{
			value = val;
			min = minVal;
			max = maxVal;
		}
	}

	public delegate void FixMethodDelegate(UnityEngine.Object obj, bool isLastInSet, int selectedIndex);

	public class FixRecord : Record
	{
		public FixMethodDelegate fixMethod;
		public UnityEngine.Object targetObject;
		public string[] buttonNames;
		public bool complete;

		public FixRecord(string cat, string msg, FixMethodDelegate fix, UnityEngine.Object target, string[] buttons)
			: base(cat, msg)
		{
			buttonNames = buttons;
			fixMethod = fix;
			targetObject = target;
			complete = false;
		}
	}
}
