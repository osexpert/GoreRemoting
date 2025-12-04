using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoreRemoting.Tests;

internal class Ports
{
	static int _port = 9065;

	public static int GetNext()
	{
		while (true)
		{
			int current = _port;
			int next = current + 1;

			if (next > 65000)
				next = 9065;

			// Try to apply change: if _port == current, set it to next
			int original = Interlocked.CompareExchange(ref _port, next, current);

			if (original == current)
				return next;
							 
			// another thread changed _port; retry
		}
	}
}
