﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NetworkApp
{
	public static class Utils
	{
		public static int FrameLength = 56;
		public static int Index = 0;

		public static Encoding Encoding = Encoding.UTF8;
		public static List<BitArray> Result = new List<BitArray>();
		public static bool[][] Data;
		public static bool isFile = false;

		private readonly static Random Random = new Random();
		private static int FrameId = 0;
		private static string FileExtension;
		private static bool isFinished = false;

		public static int CheckSum(bool[] array)
		{
			int result = 0;
			foreach (var item in array)
				if (item)
					result++;

			return result;
		}

		public static void IncrementIndex()
		{
			var LockObject = new object();
			lock (LockObject)
				Index++;
		}

		public static void AddDataInBuffer(int? index, BitArray data)
		{
			var LockObject = new object();
			lock (LockObject)
			{
				try
				{
					if (index == null)
						Result.Add(data);
					else
						Result.Insert((int)index, data);
				}
				catch (Exception) { }
			}
		}

		public static int GetIndexFrame => FrameId;

		public static int IncrementIndexFrame()
		{
			if (FrameId == 7)
				FrameId = 0;
			else
				FrameId++;

			return FrameId;
		}

		public static BitArray SetNoiseRandom(BitArray body)
		{
			if (Random.Next(1, 100) > 90)
				for (int i = 0; i < body.Length; i++)
					if (i % Random.Next(1, 5) == 0)
						body[i] = Random.Next(1, 10) < 5;

			return body;
		}

		public static byte[] BitArrayToByteArray(BitArray data)
		{
			if (data == null)
				return null;

			var array = new byte[(data.Length - 1) / 8 + 1];
			data.CopyTo(array, 0);
			return array;
		}

		public static object DeserializeObject(byte[] allBytes)
		{
			if (allBytes == null)
				return null;

			using var stream = new MemoryStream(allBytes);
			return DeserializeFromStream(stream);
		}

		private static object DeserializeFromStream(MemoryStream stream)
		{
			try
			{
				IFormatter formatter = new BinaryFormatter();
				stream.Seek(0, SeekOrigin.Begin);
				return formatter.Deserialize(stream);
			}
			catch
			{
				return null;
			}
		}

		public static byte[] SerializeObject(object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using var ms = new MemoryStream();
			bf.Serialize(ms, obj);
			return ms.ToArray();
		}

		public static void SerializeMessage(string message)
		{
			var bits = new BitArray(Encoding.GetBytes(message));
			var values = new bool[bits.Count];
			for (int m = 0; m < bits.Count; m++)
				values[m] = bits[m];

			int j = 0;
			Data = values.GroupBy(s => j++ / FrameLength).Select(g => g.ToArray()).ToArray();
		}

		public static void SerializeFile(string fileName)
		{
			BitArray bits;
			isFile = true;

			FileExtension = Path.GetExtension(fileName);

			using (FileStream fs = File.OpenRead(fileName))
			{
				var binaryReader = new BinaryReader(fs);
				bits = new BitArray(binaryReader.ReadBytes((int)fs.Length));
			}

			var values = new bool[bits.Count];
			for (int m = 0; m < bits.Count; m++)
				values[m] = bits[m];

			int j = 0;
			Data = values.GroupBy(s => j++ / FrameLength).Select(g => g.ToArray()).ToArray();
		}

		public static void DeserializeFile(string tag)
		{
			var LockObject = new object();
			lock (LockObject)
				if (!isFinished)
				{
					isFinished = true;
					var booleans = new List<bool>();

					for (int i = 0; i < Result.Count; i++)
						for (int j = 0; j < Result[i].Length; j++)
							booleans.Add(Result[i][j]);

					var byteArray = BitArrayToByteArray(new BitArray(booleans.ToArray()));

					try
					{
						using var fs = new FileStream(tag + FileExtension, FileMode.Create, FileAccess.Write);
						fs.Write(byteArray, 0, byteArray.Length);
						ConsoleHelper.WriteToConsole(tag + FileExtension, $"Файл успешно создан..");
					}
					catch
					{
						ConsoleHelper.WriteToConsole(tag + FileExtension, $"Что-то пошло не так...");
					}
				}
		}

		public static void DeserializeMessage(string tag)
		{
			var LockObject = new object();
			lock (LockObject)
				if (!isFinished)
				{
					isFinished = true;

					var booleans = new List<bool>();

					for (int i = 0; i < Result.Count; i++)
						for (int j = 0; j < Result[i].Length; j++)
							booleans.Add(Result[i][j]);

					ConsoleHelper.WriteToConsole(tag, $"Полученные данные: {Encoding.GetString(BitArrayToByteArray(new BitArray(booleans.ToArray())))}");
				}
		}
	}
}
