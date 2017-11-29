// Copyright (c) 2016-2017 Ilium VR, Inc.
// Licensed under the MIT License - https://raw.github.com/IliumVR/MpsseShell/master/LICENSE

using System;
using System.IO;

namespace IilumVR.Tools.MpsseShell
{
	public static class ConsoleManager
	{
		private static TextReader InputStream;
		private static TextWriter OutputStream;

		public static void LoadScript(TextReader file)
		{
			if (InputStream != null)
				InputStream.Dispose();

			InputStream = file;
		}

		public static void DuplicateOutput(TextWriter file)
		{
			if (OutputStream != null)
				OutputStream.Dispose();

			OutputStream = file;
		}

		public static string ReadLine()
		{
			string line = null;

			//read from script if available
			if (InputStream != null)
			{
				line = InputStream.ReadLine();

				//if file is empty, dispose of it and read from console
				if (line != null)
				{
					//write output in place of human input
					Console.WriteLine(line);
					return line;
				}
				else
				{
					InputStream.Dispose();
					InputStream = null;
				}
			}

			return Console.ReadLine();
		}

		/// <summary>
		/// Writes a command only to the output file.
		/// </summary>
		public static void WriteCommand(string command)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(command);
		}

		#region Write

		public static void Write(long value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(string value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(ulong value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(uint value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(object value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(float value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(decimal value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(double value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(char[] buffer)
		{
			if (OutputStream != null)
				OutputStream.Write(buffer);

			Console.Write(buffer);
		}

		public static void Write(char value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(bool value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(int value)
		{
			if (OutputStream != null)
				OutputStream.Write(value);

			Console.Write(value);
		}

		public static void Write(string format, object arg0)
		{
			if (OutputStream != null)
				OutputStream.Write(format, arg0);

			Console.Write(format, arg0);
		}

		public static void Write(string format, params object[] arg)
		{
			if (OutputStream != null)
				OutputStream.Write(format, arg);

			Console.Write(format, arg);
		}

		public static void Write(string format, object arg0, object arg1)
		{
			if (OutputStream != null)
				OutputStream.Write(format, arg0, arg1);

			Console.Write(format, arg0, arg1);
		}

		public static void Write(char[] buffer, int index, int count)
		{
			if (OutputStream != null)
				OutputStream.Write(buffer, index, count);

			Console.Write(buffer, index, count);
		}

		public static void Write(string format, object arg0, object arg1, object arg2)
		{
			if (OutputStream != null)
				OutputStream.Write(format, arg0, arg1, arg2);

			Console.Write(format, arg0, arg1, arg2);
		}

		public static void Write(string format, object arg0, object arg1, object arg2, object arg3)
		{
			if (OutputStream != null)
				OutputStream.Write(format, arg0, arg1, arg2, arg3);

			Console.Write(format, arg0, arg1, arg2, arg3);
		}

		#endregion

		#region WriteLine

		public static void WriteLine()
		{
			if (OutputStream != null)
				OutputStream.WriteLine();

			Console.WriteLine();
		}

		public static void WriteLine(object value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(string value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(decimal value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(int value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(float value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(char value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(double value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(bool value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(uint value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(long value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(char[] buffer)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(buffer);

			Console.WriteLine(buffer);
		}

		public static void WriteLine(ulong value)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(value);

			Console.WriteLine(value);
		}

		public static void WriteLine(string format, object arg0)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(format, arg0);

			Console.WriteLine(format, arg0);
		}

		public static void WriteLine(string format, params object[] arg)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(format, arg);

			Console.WriteLine(format, arg);
		}

		public static void WriteLine(string format, object arg0, object arg1)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(format, arg0, arg1);

			Console.WriteLine(format, arg0, arg1);
		}

		public static void WriteLine(char[] buffer, int index, int count)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(buffer, index, count);

			Console.WriteLine(buffer, index, count);
		}

		public static void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			if (OutputStream != null)
				OutputStream.WriteLine(format, arg0, arg1, arg2);

			Console.WriteLine(format, arg0, arg1, arg2);
		}

		#endregion

	}
}
