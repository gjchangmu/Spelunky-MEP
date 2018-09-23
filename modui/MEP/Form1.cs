/*
NecroDancer Score Analyzor
by gjchangmu (gujianjiayi4@126.com)
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication1
{
	public partial class Form1 : Form
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll")]
		public static extern IntPtr CloseHandle(IntPtr handle);

		[DllImport("Kernel32.dll")]
		static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBufflpBuffer, int nSize, ref int lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int flAllocationType, int flProtect);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		static extern IntPtr VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int flFreeType);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int GetLastError();

		const int oldbase = 0x1270000;
		const int oldstorage = 0x0CCE0000;

		List<int> codejumps;
		List<int> storagejumps;
		List<int> storageabs;

		Process process;
		IntPtr processHandle = IntPtr.Zero;
		IntPtr baseaddress;
		byte[] originalcode = new byte[2048];
		int difflength;
		IntPtr newmem;

		byte[] prices = new byte[0x50];

		public Form1()
		{
			InitializeComponent();

			// list here addresses of all jump command from inside the original game code to newmem
			codejumps = new List<int>();
			codejumps.Add(0X69C15);
			codejumps.Add(0x63B99);
			codejumps.Add(0X651AD);
			codejumps.Add(0X54CC1);
			codejumps.Add(0X54D02);
			codejumps.Add(0X54E33);
			codejumps.Add(0X55D9F);
			codejumps.Add(0X55EAC);
			codejumps.Add(0X55C37);
			codejumps.Add(0XB5D16);

			// list here addresses of all jump command from newmem to inside the original game code
			storagejumps = new List<int>();
			storagejumps.Add(0x82);
			storagejumps.Add(0x101C);
			storagejumps.Add(0x2032);
			storagejumps.Add(0x302F);
			storagejumps.Add(0x401E);
			storagejumps.Add(0x501F);
			storagejumps.Add(0x6017);
			storagejumps.Add(0x6025);
			storagejumps.Add(0x70B9);
			storagejumps.Add(0x70C3);
			storagejumps.Add(0x70C8);
			storagejumps.Add(0x8042);
			storagejumps.Add(0x9013);


			// list here addresses of reference to inside the original game code in newmem
			storageabs = new List<int>();
			storageabs.Add(0x03);
			storageabs.Add(0x1D);
			storageabs.Add(0x23);
			storageabs.Add(0x29);
			storageabs.Add(0x2009);
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			bool ret;
			int rn = 0;

			if (processHandle == IntPtr.Zero)
			{
				Process[] Ps = Process.GetProcessesByName("Spelunky");
				if (Ps.Length == 0)
					return;
				System.Threading.Thread.Sleep(1000);
				process = Ps[0];
				processHandle = OpenProcess(0x0038, false, process.Id);
				if (processHandle == IntPtr.Zero)
					return;
				baseaddress = process.MainModule.BaseAddress;

				/*
				foreach (ProcessModule module in process.Modules)
				{
					Console.WriteLine(module.ModuleName);
					if (module.ModuleName.Contains("msvcrt.dll"))
					{
						randaddress = (IntPtr)((int)module.BaseAddress + 0x5c2e0);
						break;
					}
				}
				*/
				
				// check game version
				bool unknownversion = false;
				byte[] checkbuff = new byte[4];
				ret = ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + 0x10013), checkbuff, 4, ref rn);
				if (BitConverter.ToUInt32(checkbuff, 0) != 0x3A819CA1)
					unknownversion = true;
				ret = ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + 0x50008), checkbuff, 4, ref rn);
				if (BitConverter.ToUInt32(checkbuff, 0) != 0x5B5D5E5F)
					unknownversion = true;
				ret = ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + 0x70000), checkbuff, 4, ref rn);
				if (BitConverter.ToUInt32(checkbuff, 0) != 0xB60805D9)
					unknownversion = true;
				if (unknownversion)
				{
					this.Text = "base:" + baseaddress.ToString("X");
					timer1.Enabled = false;
					MessageBox.Show("Unknown version of game.");
					Application.Exit();
					return;
				}

				// read newmem
				FileStream fs = new FileStream("newmem.bin", FileMode.Open);
				if (fs == null)
					return;
				int newmemlength = (int)fs.Length;
				byte[] wbuff = new byte[newmemlength];
				fs.Read(wbuff, 0, newmemlength);
				fs.Close();

				// alloc newmem
				newmem = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)newmemlength, 0x3000, 0x40);
				if (newmem == IntPtr.Zero)
				{
					int error = GetLastError();
					lbStatus.Text = "Alloc failed, " + error.ToString();
					return;
				}

				// jmp or call command from newmem to game code
				foreach (int jmp in storagejumps)
				{
					int wbuffp = jmp + 1;
					int oldadd = BitConverter.ToInt32(wbuff, wbuffp);
					int offsetadd = oldstorage - (int)newmem + (int)baseaddress - oldbase;
					int newadd = oldadd + offsetadd;
					BitConverter.GetBytes(newadd).CopyTo(wbuff, wbuffp);
				}
				// references to game code in newmem
				foreach (int jmp in storageabs)
				{
					int wbuffp = jmp;
					int oldadd = BitConverter.ToInt32(wbuff, wbuffp);
					int offsetadd = (int)baseaddress - oldbase;
					int newadd = oldadd + offsetadd;
					BitConverter.GetBytes(newadd).CopyTo(wbuff, wbuffp);
				}
				// references to newmem in newmem, automatic search
				for (int i = 0; i < newmemlength - 4; i++)
				{
					int oldadd = BitConverter.ToInt32(wbuff, i);
					if (oldadd >= oldstorage && oldadd < oldstorage + newmemlength)
					{
						int offsetadd = (int)newmem - oldstorage;
						int newadd = oldadd + offsetadd;
						BitConverter.GetBytes(newadd).CopyTo(wbuff, i);
					}
				}

				// adjust prices according to price.ini
				Array.Copy(wbuff, 0x700, prices, 0, 0x50);
				ReadOptions("price.ini");
				Array.Copy(prices, 0, wbuff, 0x700, 0x50);

				// write to newmem
				ret = WriteProcessMemory(processHandle, newmem, wbuff, newmemlength, ref rn);
				if (!ret)
				{
					lbStatus.Text = "Write to storage " + newmem.ToString() + " failed.";
					return;
				}

				// SPELUNKY ONLY!!!
				// record money pointer
				byte[] p = new byte[4];
				byte[] pb = new byte[4];
				ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + 0x138558), p, 4, ref rn);
				ReadProcessMemory(processHandle, (IntPtr)(BitConverter.ToInt32(p, 0) + 0x30), p, 4, ref rn);
				ReadProcessMemory(processHandle, (IntPtr)(BitConverter.ToInt32(p, 0) + 0x280), p, 4, ref rn);
				BitConverter.GetBytes(BitConverter.ToInt32(p, 0) + 0x5298).CopyTo(pb, 0);
				WriteProcessMemory(processHandle, (IntPtr)((int)newmem + 0x650), pb, 4, ref rn);
				BitConverter.GetBytes(BitConverter.ToInt32(p, 0) + 0x88).CopyTo(pb, 0);
				WriteProcessMemory(processHandle, (IntPtr)((int)newmem + 0x654), pb, 4, ref rn);

				// read diffs
				byte[] diff = new byte[2048];
				fs = new FileStream("dif.bin", FileMode.Open);
				if (fs == null)
					return;
				difflength = fs.Read(diff, 0, 2048);
				fs.Close();

				// write the diffs, i.e. injecting
				for (int i = 0; i < difflength;)
				{
					int addr = BitConverter.ToInt32(diff, i);
					int len = BitConverter.ToInt32(diff, i + 4);

					Console.WriteLine(len);
					Array.Copy(diff, i, originalcode, i, 8);
					byte[] read = new byte[len];
					ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + addr), read, len, ref rn);
					Array.Copy(read, 0, originalcode, i + 8, len);

					//for (int b = 0; b < len; b++)
					//	wbuff[b] = diff[i + 8 + b];
					Array.Copy(diff, i + 8, wbuff, 0, len);
					ret = WriteProcessMemory(processHandle, (IntPtr)((int)baseaddress + addr), wbuff, len, ref rn);
					i += 8 + len;
				}

				// jmp command from game code to newmem
				foreach (int jmp in codejumps)
				{
					ReadProcessMemory(processHandle, (IntPtr)((int)baseaddress + jmp + 1), wbuff, 4, ref rn);
					int oldadd = BitConverter.ToInt32(wbuff, 0);
					int offsetadd = (int)newmem - oldstorage + oldbase - (int)baseaddress;
					int newadd = oldadd + offsetadd;
					BitConverter.GetBytes(newadd).CopyTo(wbuff, 0);
					WriteProcessMemory(processHandle, (IntPtr)((int)baseaddress + jmp + 1), wbuff, 4, ref rn);
				}


				if (!ret)
				{
					lbStatus.Text = "Write to 0x50000 failed.";
					return;
				}

				lbStatus.Text = "Spelunky game detected. newmem=" + newmem.ToString("X");
			}
			else
			{
				if (process.HasExited)
				{
					processHandle = IntPtr.Zero;
					process = null;
					lbStatus.Text = "Waiting for game process...";
				}
			}
		}

		private void cbRandom_CheckedChanged(object sender, EventArgs e)
		{
			byte[] wbuff = new byte[4];
			int rn = 0;
			if (cbRandom.Checked && processHandle != IntPtr.Zero)
			{
				BitConverter.GetBytes(1).CopyTo(wbuff, 0);
				WriteProcessMemory(processHandle, (IntPtr)((int)newmem + 0x510), wbuff, 4, ref rn);
			}
			else if (!cbRandom.Checked && processHandle != IntPtr.Zero)
			{
				BitConverter.GetBytes(0).CopyTo(wbuff, 0);
				WriteProcessMemory(processHandle, (IntPtr)((int)newmem + 0x510), wbuff, 4, ref rn);
			}
		}

		public void ReadOptions(string file)
		{
			if (!File.Exists(file))
			{
				return;
			}

			FileStream fini = new FileStream(file, FileMode.Open);
			StreamReader srini = new StreamReader(fini);
			string sLine = "";
			string sName = "", sPara = "";
			while (sLine != null)
			{
				sLine = srini.ReadLine();
				if
				(
					sLine != null &&
					sLine != "" &&
					sLine.Substring(0, 1) != "-" &&
					sLine.Substring(0, 1) != "%" &&
					sLine.Substring(0, 1) != "'" &&
					sLine.Substring(0, 1) != "/" &&
					sLine.Substring(0, 1) != "!" &&
					sLine.Substring(0, 1) != "[" &&
					sLine.Substring(0, 1) != "#" &&
					sLine.Contains("=") &&
					!sLine.Substring(sLine.IndexOf("=") + 1).Contains("=")
				)
				{
					sName = sLine.Substring(0, sLine.IndexOf("="));
					sName = sName.Trim();
					sName = sName.ToUpper();
					sPara = sLine.Substring(sLine.IndexOf("=") + 1);
					sPara = sPara.Trim();

					int iout;
					switch (sName)
					{
						case "STARTING_MONEY":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x00);
							break;

						case "SHOP_PRICE":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x0C);
							break;

						case "JUMP":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x04);
							break;
						case "WHIP":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x08);
							break;

						case "JETPACK_GAS":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout / 100).CopyTo(prices, 0x10);
							break;
						case "CAPE_USE":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x14);
							break;

						case "MATTOCK":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x20);
							break;
						case "BOOMERANG":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x24);
							break;
						case "MACHETE":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x28);
							break;
						case "WEBGUN":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x2C);
							break;
						case "SHOTGUN":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x30);
							break;
						case "FREEZEGUN":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x34);
							break;
						case "CANNON":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x38);
							break;
						case "CAMERA":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x3C);
							break;
						case "TELEPORTER":
							if (int.TryParse(sPara, out iout))
								BitConverter.GetBytes(iout).CopyTo(prices, 0x40);
							break;
					}
				}
			}
			fini.Close();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			int rn = 0;
			byte[] wbuff = new byte[100];
			for (int i = 0; i < difflength;)
			{
				int addr = BitConverter.ToInt32(originalcode, i);
				int len = BitConverter.ToInt32(originalcode, i + 4);
				Console.WriteLine(len);

				Array.Copy(originalcode, i + 8, wbuff, 0, len);
				bool ret = WriteProcessMemory(processHandle, (IntPtr)((int)baseaddress + addr), wbuff, len, ref rn);
				i += 8 + len;
			}

			VirtualFreeEx(processHandle, newmem, 0, 0x8000);
			CloseHandle(processHandle);
		}
	}
}
