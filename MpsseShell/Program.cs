// Copyright (c) 2016-2017 Ilium VR, Inc.
// Licensed under the MIT License - https://raw.github.com/IliumVR/MpsseShell/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using IliumVR.Bindings.Ftdi.Mpsse;
using IliumVR.Bindings.Ftdi.Mpsse.I2C;

namespace IilumVR.Tools.MpsseShell
{
	enum OutputMode
	{
		Binary,
		Decimal,
		Hexadecimal
	}

	class Command
	{
		public string HelpText { get; set; }
		public Func<string[], bool> Run { get; set; }
	}

	class Program
	{
		//Dictionary of possible commands
		static Dictionary<string, Command> Commands = new Dictionary<string, Command>()
		{
			{ "exit",  new Command() { HelpText = "Exits the MPSSE REPL", Run = RunExitCmd } },
			{ "quit",  new Command() { HelpText = "Exits the MPSSE REPL", Run = RunExitCmd } },
			{ "e",  new Command() { HelpText = "Exits the MPSSE REPL", Run = RunExitCmd } },
			{ "q",  new Command() { HelpText = "Exits the MPSSE REPL", Run = RunExitCmd } },
			{ "help",  new Command() { HelpText = "Shows help text", Run = RunHelpCmd } },
			{ "h",  new Command() { HelpText = "Shows help text", Run = RunHelpCmd } },
			{ "read", new Command() { HelpText = "Reads from an I2C device. Args: <dev addr> <num bytes> [reg addr]", Run = RunReadCmd } },
			{ "r", new Command() { HelpText = "Reads from an I2C device. Args: <dev addr> <num bytes> [reg addr]", Run = RunReadCmd } },
			{ "readloop", new Command() { HelpText = "Reads from an I2C device in a loop. Args: <dev addr> <num bytes> <num loops> [reg addr]", Run = RunReadLoopCmd } },
			{ "rl", new Command() { HelpText = "Reads from an I2C device in a loop. Args: <dev addr> <num bytes> <num loops> [reg addr]", Run = RunReadLoopCmd } },
			{ "write", new Command() { HelpText = "Writes to an I2C device. Args: <dev addr> <data>", Run = RunWriteCmd } },
			{ "w", new Command() { HelpText = "Writes to an I2C device. Args: <dev addr> <data>", Run = RunWriteCmd } },
			{ "scan", new Command() { HelpText = "Scans for I2C devices. Args: [start] [end]", Run = RunScanCmd } },
			{ "s", new Command() { HelpText = "Scans for I2C devices. Args: [start] [end]", Run = RunScanCmd } },
			{ "output", new Command() { HelpText = "Sets the numeric output mode. Args: <bin/dec/hex> Default: dec", Run = RunOutputCmd } },
			{ "o", new Command() { HelpText = "Sets the numeric output mode. Args: <bin/dec/hex> Default: dec", Run = RunOutputCmd } },
			{ "load", new Command() { HelpText = "Loads a script or exits out of one. Args: <file>/0", Run = RunLoadCmd } },
			{ "l", new Command() { HelpText = "Loads a script or exits out of one. Args: <file>/0", Run = RunLoadCmd } },
			{ "fileout", new Command() { HelpText = "Redirects all output to a specified file or back to the console. Args: <file>/0", Run = RunFileOutCmd } },
			{ "fo", new Command() { HelpText = "Redirects all output to a specified file or back to the console. Args: <file>/0", Run = RunFileOutCmd } },
		};

		static bool running = true;
		static CancellationTokenSource cmdCancelSource;

		static Channel channel;

		static TransferOptions writeOptions = TransferOptions.StartBit | TransferOptions.StopBit;
		static TransferOptions readOptions = TransferOptions.StartBit | TransferOptions.StopBit | TransferOptions.NackLastByte;

		static OutputMode byteOutputMode = OutputMode.Decimal;

		static byte[] buffer = new byte[256];

		static void Main(string[] args)
		{

			Console.CancelKeyPress += (s, e) =>
			{
				if (e.SpecialKey == ConsoleSpecialKey.ControlC)
				{
					if (cmdCancelSource != null && !cmdCancelSource.IsCancellationRequested)
					{
						e.Cancel = true;
						cmdCancelSource.Cancel();
						Console.WriteLine("Cancelled");
					}
				}
			};

			//TODO command line args to select channel and config
			FtdiStatus status;
			uint channels = Channel.GetNumChannels();
			if (channels == 0)
			{
				ConsoleManager.WriteLine("No channels detected");
				return;
			}

			channel = new Channel(0);
			if (channel.IsDisposed)
			{
				ConsoleManager.WriteLine("Error initializing channel");
				return;
			}

			ChannelConfig con = new ChannelConfig(ClockRate.FastMode, 5, 0);
			status = channel.Initialize(ref con);
			if (status != FtdiStatus.Ok)
			{
				ConsoleManager.WriteLine("Error Initialize: " + status);
				return;
			}

			ConsoleManager.WriteLine("Initialized channel 0");

			//main loop
			running = true;
			while (running)
			{
				//write input string, read line
				ConsoleManager.Write("MPSSE >");
				string line = ConsoleManager.ReadLine();

				ConsoleManager.WriteCommand(line);

				//Handle user pressing enter without typing anything
				if (string.IsNullOrWhiteSpace(line))
				{
					ConsoleManager.WriteLine("Type h or help to view a list of commands.");
					continue;
				}

				//strip comments
				int commentIndex = line.IndexOf('#');
				if (commentIndex != -1)
					line = line.Substring(0, commentIndex);

				//split into args
				string[] lineWords = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				//comment-only lines get skipped
				if (lineWords.Length == 0)
					continue;

				//find the command
				Command cmd;
				if (!Commands.TryGetValue(lineWords[0], out cmd))
				{
					ConsoleManager.WriteLine("Unknown command \"" + lineWords[0] + "\".");
					continue;
				}

				//reset cancellation token
				cmdCancelSource = new CancellationTokenSource();

				//create task to run cmd async
				Task cmdRunTask = new Task(() => cmd.Run(lineWords), cmdCancelSource.Token);

				//run and wait for the task to complete
				cmdRunTask.Start();
				cmdRunTask.Wait(Timeout.Infinite);
			}

			//cleanly exit
			channel.Dispose();
		}

		static bool RunExitCmd(string[] args)
		{
			ConsoleManager.WriteLine("Exiting...");

			running = false;
			return true;
		}

		static bool RunHelpCmd(string[] args)
		{
			ConsoleManager.WriteLine("Valid commands: ");
			foreach (var cmd in Commands)
			{
				ConsoleManager.WriteLine("\t" + cmd.Key + " - " + cmd.Value.HelpText);
			}

			ConsoleManager.WriteLine();
			return true;
		}

		static bool RunReadCmd(string[] args)
		{
			byte deviceAddress, registerAddress;

			if (args.Length < 3 || args.Length > 4)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Invalid number of arguments arguments");
				Console.ResetColor();
				return false;
			}

			if (!TryParseByte(args[1], out deviceAddress))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse device address");
				Console.ResetColor();
				return false;
			}

			uint bytesToTransfer;
			if (!uint.TryParse(args[2], out bytesToTransfer))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse number of bytes to read");
				Console.ResetColor();
				return false;
			}

			if (args.Length == 3)
			{
				if (cmdCancelSource.Token.IsCancellationRequested)
					return false;

				uint bytesTransferred;
				FtdiStatus status = channel.Read(deviceAddress, bytesToTransfer, buffer, out bytesTransferred, readOptions | TransferOptions.NoAddress);
				if (status != FtdiStatus.Ok)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("FTDI Error: " + status);
					Console.ResetColor();
					return false;
				}

				ConsoleManager.WriteLine("Read values: " + BitConverter.ToString(buffer, 0, (int)bytesTransferred));
			}
			else if (args.Length == 4)
			{
				if (!TryParseByte(args[3], out registerAddress))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Could not parse register address");
					Console.ResetColor();
					return false;
				}

				if (cmdCancelSource.Token.IsCancellationRequested)
					return false;

				uint bytesTransferred;
				FtdiStatus status = channel.ReadFromRegister(deviceAddress, registerAddress, bytesToTransfer, buffer, out bytesTransferred, writeOptions, readOptions);
				if (status != FtdiStatus.Ok)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("FTDI Error: " + status);
					Console.ResetColor();
					return false;
				}

				//TODO format byte output
				ConsoleManager.WriteLine("Reg " + FormatByteOutput(registerAddress) + ": " + BitConverter.ToString(buffer, 0, (int)bytesTransferred));
			}

			return true;
		}

		static bool RunReadLoopCmd(string[] args)
		{
			byte deviceAddress, registerAddress;
			uint bytesToTransfer, numLoops;

			if (args.Length < 4 || args.Length > 5)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Invalid number of arguments arguments");
				Console.ResetColor();
				return false;
			}

			if (!TryParseByte(args[1], out deviceAddress))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse device address");
				Console.ResetColor();
				return false;
			}

			if (!uint.TryParse(args[2], out bytesToTransfer))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse number of bytes to read");
				Console.ResetColor();
				return false;
			}

			if (!uint.TryParse(args[3], out numLoops))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse number of loops");
				Console.ResetColor();
				return false;
			}

			if (args.Length == 4)
			{
				for (int i = 0; i < numLoops; i++)
				{
					if (cmdCancelSource.Token.IsCancellationRequested)
						return false;

					uint bytesTransferred;
					FtdiStatus status = channel.Read(deviceAddress, bytesToTransfer, buffer, out bytesTransferred, readOptions | TransferOptions.NoAddress);
					if (status != FtdiStatus.Ok)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						ConsoleManager.WriteLine("FTDI Error: " + status);
						Console.ResetColor();
						return false;
					}

					ConsoleManager.WriteLine("Loop " + i + ": " + BitConverter.ToString(buffer, 0, (int)bytesTransferred));
				}
			}
			else if (args.Length == 5)
			{
				if (!TryParseByte(args[4], out registerAddress))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Could not parse register address");
					Console.ResetColor();
					return false;
				}

				for (int i = 0; i < numLoops; i++)
				{
					if (cmdCancelSource.Token.IsCancellationRequested)
						return false;

					uint bytesTransferred;
					FtdiStatus status = channel.ReadFromRegister(deviceAddress, registerAddress, bytesToTransfer, buffer, out bytesTransferred, writeOptions, readOptions);
					if (status != FtdiStatus.Ok)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						ConsoleManager.WriteLine("FTDI Error: " + status);
						Console.ResetColor();
						return false;
					}

					//TODO format output
					ConsoleManager.WriteLine("Loop " + i + ": " + BitConverter.ToString(buffer, 0, (int)bytesTransferred));
				}
			}

			return true;
		}

		static bool RunWriteCmd(string[] args)
		{
			byte deviceAddress;

			if (args.Length < 3)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Not enough arguments");
				Console.ResetColor();
				return false;
			}

			if (!TryParseByte(args[1], out deviceAddress))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Could not parse device address");
				Console.ResetColor();
				return false;
			}

			uint bytesToTransfer = 0;
			for (int i = 2; i < args.Length; i++)
			{
				byte dataByte;

				if (!TryParseByte(args[i], out dataByte))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Could not parse register address");
					Console.ResetColor();
					return false;
				}

				buffer[bytesToTransfer++] = dataByte;
			}

			if (cmdCancelSource.Token.IsCancellationRequested)
				return false;

			uint bytesTransferred;
			FtdiStatus status = channel.Write(deviceAddress, bytesToTransfer, buffer, out bytesTransferred, writeOptions);
			if (status != FtdiStatus.Ok)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("FTDI Error: " + status);
				Console.ResetColor();
				return false;
			}

			ConsoleManager.WriteLine("Successfully wrote " + bytesToTransfer + " bytes to device address " + FormatByteOutput(deviceAddress));

			return true;
		}

		static bool RunScanCmd(string[] args)
		{
			byte start = 0, end = 128;

			if (args.Length > 3)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Too many arguments");
				Console.ResetColor();
				return false;
			}

			if (args.Length > 1)
			{
				if (!TryParseByte(args[1], out start))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Could not parse scan range start");
					Console.ResetColor();
					return false;
				}
			}
			if (args.Length > 2)
			{
				if (!TryParseByte(args[2], out end))
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Could not parse scan range end");
					Console.ResetColor();
					return false;
				}
			}

			byte deviceAddress = start;
			for (int i = start; i < end; i++)
			{
				if (cmdCancelSource.Token.IsCancellationRequested)
					return false;

				uint bytesTransferred;
				FtdiStatus status = channel.ReadFromRegister(deviceAddress, 0, 0, buffer, out bytesTransferred, writeOptions, readOptions);
				if (status == FtdiStatus.Ok)
				{
					ConsoleManager.WriteLine("Found device: " + FormatByteOutput(deviceAddress));
				}
				else if (status != FtdiStatus.DeviceNotFound)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("FTDI Error: " + status);
					Console.ResetColor();
				}

				deviceAddress++;
			}

			return true;
		}

		static bool RunOutputCmd(string[] args)
		{
			if (args.Length != 2)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Invalid number of arguments");
				Console.ResetColor();
				return false;
			}

			switch (args[1].ToUpperInvariant())
			{
				case "BIN":
					byteOutputMode = OutputMode.Binary;
					return true;
				case "DEC":
					byteOutputMode = OutputMode.Decimal;
					return true;
				case "HEX":
					byteOutputMode = OutputMode.Hexadecimal;
					return true;
				default:
					Console.ForegroundColor = ConsoleColor.Red;
					ConsoleManager.WriteLine("Invalid output format \"" + args[1] + "\"");
					Console.ResetColor();
					return false;
			}
		}

		static bool RunLoadCmd(string[] args)
		{
			//TODO paths with spaces
			if (args.Length != 2)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Invalid number of arguments (spaces in path not supported yet)");
				Console.ResetColor();

				return false;
			}

			//clears script
			if (args[1] == "0")
			{
				ConsoleManager.LoadScript(null);
				return true;
			}

			try
			{
				var sr = File.OpenText(args[1]);
				ConsoleManager.LoadScript(sr);
				return true;
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine(e.ToString());
				Console.ResetColor();

				return false;
			}
		}

		static bool RunFileOutCmd(string[] args)
		{
			//TODO paths with spaces
			if (args.Length != 2)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine("Invalid number of arguments (spaces in path not supported yet)");
				Console.ResetColor();

				return false;
			}

			//clears output
			if (args[1] == "0")
			{
				ConsoleManager.DuplicateOutput(null);
				return true;
			}

			try
			{
				var sw = File.CreateText(args[1]);
				sw.AutoFlush = true;
				ConsoleManager.DuplicateOutput(sw);
				return true;
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				ConsoleManager.WriteLine(e.ToString());
				Console.ResetColor();

				return false;
			}
		}

		static string FormatByteOutput(byte b)
		{
			switch (byteOutputMode)
			{
				case OutputMode.Binary:
					return Convert.ToString(b, 2).PadLeft(8, '0');
				case OutputMode.Hexadecimal:
					return Convert.ToString(b, 16);
				case OutputMode.Decimal:
				default:
					return b.ToString();

			}
		}

		static bool TryParseByte(string value, out byte b)
		{
			b = 0;

			if (string.IsNullOrWhiteSpace(value))
				return false;

			//remove whitespace
			value = value.Trim();

			//need at least 3 chars to have a prefix, e.g. 0x3
			if (value.Length > 2)
			{
				string prefix = value.Substring(0, 2);
				string num = value.Substring(2);

				switch (prefix)
				{
					case "0b":
						if (num.Length > 8)
							return false;
						else
						{
							try
							{
								b = Convert.ToByte(num, 2);
								return true;
							}
							catch
							{
								return false;
							}
						}
					case "0x":
						return byte.TryParse(num, System.Globalization.NumberStyles.HexNumber, null, out b);
					default:
						return byte.TryParse(value, out b);
				}
			}
			else
			{
				return byte.TryParse(value, out b);
			}
		}
	}
}
