using System;
using System.Runtime.InteropServices;
using System.Text;
using Aiv.Engine;
using OpenTK.Input;
using System.Collections;
using System.Collections.Generic;

namespace Aiv.Engine
{

	public class TKButtons<T>
	{
		private int openTKindex;

		public TKButtons (int openTKindex)
		{
			this.openTKindex = openTKindex;
		}

		public bool this[int i]
		{
			get
			{
				return Joystick.GetState(openTKindex).GetButton((JoystickButton)i) == OpenTK.Input.ButtonState.Pressed;
			}
			set { } // not supposed to, as well
		}
	}

	public class TKJoystick : Engine.Joystick
	{
		private int openTKindex;

		// -128 127
		public new int x 
		{
			get { return (int)(Joystick.GetState(openTKindex).GetAxis(JoystickAxis.Axis0) * 127); }
			set { } // not supposed to
		}
		public new int y
		{
			get { return (int)(Joystick.GetState(openTKindex).GetAxis(JoystickAxis.Axis1) * 127); }
			set { } // not supposed to
		}

		public TKButtons<bool> obuttons; // can't ovveride buttons since it's a bool[]

		public TKJoystick (int index, int openTKindex, int hashCode)
		{
			this.index = index;
			this.openTKindex = openTKindex;
			id = hashCode; // Joystick.GetGuid is internal, how to get an id?
			name = hashCode.ToString();

			obuttons = new TKButtons<bool> (openTKindex);
		}

		public new int GetAxis(int axisIndex)
		{
			return (int)(Joystick.GetState(openTKindex).GetAxis((JoystickAxis)axisIndex) * 127);
		}

		public new bool GetButton (int buttonIndex)
		{
			return obuttons [buttonIndex];
		}

		public bool IsConnected () 
		{
			return OpenTK.Input.Joystick.GetCapabilities (openTKindex).IsConnected;
		}
	}

	public class Input
	{
		private static Engine engine;
		public static void Initialize (Engine _engine)
		{
			engine = _engine;
			engine.OnAfterUpdate += new Engine.AfterUpdateEventHandler (RunLoopStep);
		}

		// this is run at each refresh
		private static void RunLoopStep (object sender)
		{
			ManageJoysticks ();
		}

		private static void ManageJoysticks ()
		{
			for (var i = 0; i < 8; i++)
			{
				var joystickCapabilities = OpenTK.Input.Joystick.GetCapabilities (i);
				if (joystickCapabilities.IsConnected && (engine.joysticks [i] == null || engine.joysticks [i].id != joystickCapabilities.GetHashCode ())) {
					engine.joysticks [i] = new TKJoystick (i, i, joystickCapabilities.GetHashCode());
				} else if (!joystickCapabilities.IsConnected && engine.joysticks [i] != null) {
					engine.joysticks [i] = null;
				}
			}
		}
	}
}