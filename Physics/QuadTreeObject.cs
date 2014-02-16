﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Regulus.Physics
{
    using Regulus.Utility;
	public interface IQuadObject
	{
		
		Rect Bounds { get; }
		event EventHandler BoundsChanged;
	}
}
