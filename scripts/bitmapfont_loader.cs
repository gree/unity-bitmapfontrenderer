/*
 * Copyright (c) 2012 GREE, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BitmapFont {

public partial class Header
{
	public Header(BinaryReader br)
	{
		fontSize = br.ReadInt16();
		fontAscent = br.ReadInt16();
		metricCount = br.ReadInt16();
		sheetWidth = br.ReadInt16();
		sheetHeight = br.ReadInt16();
	}
}

public partial class Metric
{
	public Metric()
	{
	}

	public Metric(BinaryReader br)
	{
		advance = br.ReadSingle();
		u = br.ReadInt16();
		v = br.ReadInt16();
		bearingX = br.ReadSByte();
		bearingY = br.ReadSByte();
		width = br.ReadByte();
		height = br.ReadByte();
		first = br.ReadByte();
		second = br.ReadByte();
		prevNum = br.ReadByte();
		nextNum = br.ReadByte();
	}
}

public partial class Data
{
	public Data(byte[] bytes)
	{
		Stream s = new MemoryStream(bytes);
		BinaryReader br = new BinaryReader(s);

		header = new Header(br);
		indecies = new short[256];
		for (int i = 0; i < 256; ++i)
			indecies[i] = br.ReadInt16();

		metrics = new Metric[header.metricCount];
		for (int i = 0; i < header.metricCount; ++i)
			metrics[i] = new Metric(br);

		List<byte> bs = new List<byte>();
		while (true) {
			byte b = br.ReadByte();
			if (b == 0)
				break;
			bs.Add(b);
		}
		textureName = Encoding.UTF8.GetString(bs.ToArray());
	}
}

}	// namespace BitmapFont
