using System;
using System.Collections.Generic;
using System.Text;

namespace GoreRemoting
{
	[System.AttributeUsage(System.AttributeTargets.Parameter)]
	public class StreamingFuncAttribute : Attribute
	{
		public StreamingFuncAttribute()
		{
		}
	}

	public class StreamingDoneException : Exception
	{
	}

}
