﻿/* Originally based on project by Frank McCown in 2010 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScreenSaver
{
	public partial class MainForm : Form
	{
		#region Win32 API functions

		[DllImport("user32.dll")]
		static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

		#endregion

		private const int SplitWidth = 4;
		private const int FontScaleFactor = 3;

		private Point mouseLocation;
		private readonly bool previewMode = false;
		private readonly bool showSeconds = false;
		private int lastMinute = -1;
		private readonly int fontSize = 350;
		private Font timeFont;
		private Font smallFont;
		private Graphics _graphics;

		private const string familyName = "Oswald";

		private Graphics Gfx
		{
			get { return _graphics ?? (_graphics = CreateGraphics()); }
		}

		private Font TheFont
		{
			get { return timeFont ?? (timeFont = new Font(familyName, fontSize, FontStyle.Bold)); }
		}

		private Font SmallFont
		{
			get { return smallFont ?? (smallFont = new Font(familyName, fontSize / 9, FontStyle.Bold)); }
		}

		private static readonly Color backColorTop = Color.FromArgb(255, 15, 15, 15);
		private static readonly Color backColorBottom = Color.FromArgb(255, 10, 10, 10);

		private Brush backFillTop = new SolidBrush(backColorTop);
		private Brush backFillBottom = new SolidBrush(backColorBottom);
		private readonly Brush FontBrush = new SolidBrush(Color.FromArgb(255, 183, 183, 183));
		private readonly Pen SplitPen = new Pen(Color.Black, SplitWidth);

		public MainForm()
		{
			InitializeComponent();
		}

		public MainForm(Rectangle bounds)
		{
			InitializeComponent();
			Bounds = bounds;
			fontSize = bounds.Height / FontScaleFactor;
		}

		public MainForm(IntPtr previewWndHandle)
		{
			InitializeComponent();

			// Set the preview window as the parent of this window
			SetParent(Handle, previewWndHandle);

			// Make this a child window so it will close when the parent dialog closes
			SetWindowLong(Handle, -16, new IntPtr(GetWindowLong(Handle, -16) | 0x40000000));

			// Place our window inside the parent
			Rectangle parentRect;
			GetClientRect(previewWndHandle, out parentRect);
			Size = parentRect.Size;
			Location = new Point(0, 0);

			// Make text smaller for preview window
			fontSize = Size.Height / FontScaleFactor;

			previewMode = true;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			Cursor.Hide();
			TopMost = true;

			moveTimer_Tick(null, null);
			moveTimer.Interval = 1000;
			moveTimer.Tick += moveTimer_Tick;
			moveTimer.Start();
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{

		}

		private void moveTimer_Tick(object sender, EventArgs e)
		{
			var now = DateTime.Now;

			var minute = now.Minute;
			if (lastMinute != minute)
			{
				// Update every minute
				lastMinute = minute;
				DrawIt();
			}
			if (showSeconds)
			{
				DrawIt();
			}

			// Move text to new location
			//			if (now.Second % 5 == 0)
			//			{
			//		        textLabel.Left = rand.Next(Math.Max(1, Bounds.Width - textLabel.Width));
			//		        textLabel.Top = rand.Next(Math.Max(1, Bounds.Height - textLabel.Height));
			//	        }
		}

		private void DrawIt()
		{
			Gfx.TextRenderingHint = TextRenderingHint.AntiAlias;

			var height = TheFont.Height*10/11;
			var width = !showSeconds ? Convert.ToInt32(2.05*height) : Convert.ToInt32(3.1 * height);

			var x = (Width - width)/2;
			var y = (Height - height)/2;

			var pm = DateTime.Now.Hour >= 12;
			DrawIt(x, y, height, DateTime.Now.ToString("%h"), pm ? null : "AM", pm ? "PM" : null); // The % avoids a FormatException https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx#UsingSingleSpecifiers

			x += height + (height/20);
			DrawIt(x, y, height, DateTime.Now.ToString("mm"));

			if (showSeconds)
			{
				x += height + (height/20);
				DrawIt(x, y, height, DateTime.Now.ToString("ss"));
			}
		}

		private void DrawIt(int x, int y, int size, string s, string topString = null, string bottomString = null)
		{
			// Draw the background
			var diff = size/10;
			var textRect = new Rectangle(x - diff, y + diff/2, size + diff*2, size);

			var radius = size/20;
			var diameter = radius*2;
			Gfx.FillEllipse(backFillTop, x, y, diameter, diameter); // top left
			Gfx.FillEllipse(backFillTop, x + size - diameter, y, diameter, diameter); // top right
			Gfx.FillEllipse(backFillBottom, x, y + size - diameter, diameter, diameter); // bottom left
			Gfx.FillEllipse(backFillBottom, x + size - diameter, y + size - diameter, diameter, diameter); //bottom right

			Gfx.FillRectangle(backFillTop, x + radius, y, size - diameter, diameter);
			Gfx.FillRectangle(backFillBottom, x + radius, y + size - diameter, size - diameter, diameter);

			var linGrBrush = new LinearGradientBrush(
				new Point(10, y + radius),
				new Point(10, y + size - radius),
				backColorTop,
				backColorBottom);
			Gfx.FillRectangle(linGrBrush, x, y + radius, size, size - diameter);
			linGrBrush.Dispose();

//			if (s.Length == 1)
//			{
//				s = "\u2002" + s; // Add an EN SPACE which is 1/2 em
//			}

			// Draw the text	
			var stringFormat = new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center};
			Gfx.DrawString(s, TheFont, FontBrush, textRect, stringFormat);

			
			if (topString != null)
			{
				//Gfx.DrawString(bottomString, SmallFont, FontBrush, textRect.X, textRect.Bottom - SmallFont.Height);
				Gfx.DrawString(topString, SmallFont, FontBrush, x + diameter, y + diameter);
			}
			if (bottomString != null)
			{
				Gfx.DrawString(bottomString, SmallFont, FontBrush, x + diameter, y + size - diameter - SmallFont.Height);
			}

			// Horizontal dividing line
			if (!previewMode)
			{
				var penY = y + (size/2) - (SplitWidth/2);
				Gfx.DrawLine(SplitPen, x, penY, x + size, penY);
			}
			else
			{
				Gfx.DrawLine(Pens.Black, x, y + (size / 2), x + size, y + (size / 2));
			}
		}

		private void MainForm_MouseMove(object sender, MouseEventArgs e)
		{
			if (!previewMode)
			{
				if (!mouseLocation.IsEmpty)
				{
					// Terminate if mouse is moved a significant distance
					if (Math.Abs(mouseLocation.X - e.X) > 5 ||
						Math.Abs(mouseLocation.Y - e.Y) > 5)
						Application.Exit();
				}

				// Update current mouse location
				mouseLocation = e.Location;
			}
		}

		private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!previewMode)
			{
				Application.Exit();
			}
		}

		private void MainForm_MouseClick(object sender, MouseEventArgs e)
		{
			if (!previewMode)
			{
				Application.Exit();
			}
		}

		private void MainForm_Paint(object sender, PaintEventArgs e)
		{
			DrawIt();
		}
	}
}
