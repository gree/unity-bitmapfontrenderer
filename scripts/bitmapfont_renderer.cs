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

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace BitmapFont {

public partial class Header
{
	public short fontSize;
	public short fontAscent;
	public short metricCount;
	public short sheetWidth;
	public short sheetHeight;
}

public partial class Metric
{
	public float advance;
	public short u;
	public short v;
	public sbyte bearingX;
	public sbyte bearingY;
	public byte width;
	public byte height;
	public byte first;
	public byte second;
	public byte prevNum;
	public byte nextNum;
}

public partial class Data
{
	public Header header;
	public short[] indecies;
	public Metric[] metrics;
	public string textureName;
}

public class Renderer
{
	public enum Align
	{
		LEFT,
		RIGHT,
		CENTER
	}

	public enum VerticalAlign
	{
		TOP,
		BOTTOM,
		MIDDLE
	}

	protected Data mData;
	protected Mesh mMesh;
	protected Material mMaterial;
	protected MaterialPropertyBlock mProperty;
	protected string mName;
	protected float mSize;
	protected float mWidth;
	protected float mHeight;
	protected float mLineSpacing;
	protected float mLetterSpacing;
	protected float mTabSpacing;
	protected float mLeftMargin;
	protected float mRightMargin;
	protected float mAsciiSpaceAdvance;
	protected float mNonAsciiSpaceAdvance;
	protected Align mAlign;
	protected VerticalAlign mVerticalAlign;
	protected bool mEmpty;

	public Mesh mesh {get {return mMesh;}}
	public Material material {get {return mMaterial;}}

	public Renderer(string fontName,
		float size = 0,
		float width = 0,
		float height = 0,
		Align align = Align.LEFT,
		VerticalAlign verticalAlign = VerticalAlign.TOP,
		float spaceAdvance = 0.25f,
		float lineSpacing = 1.0f,
		float letterSpacing = 0.0f,
		float tabSpacing = 4.0f,
		float leftMargin = 0.0f,
		float rightMargin = 0.0f)
	{
		ResourceCache cache = ResourceCache.SharedInstance();
		mName = fontName;
		mData = cache.LoadData(mName);
		mMaterial = cache.LoadTexture(
			System.IO.Path.GetDirectoryName(mName) + "/" + mData.textureName);
		mMesh = new Mesh();
		mProperty = new MaterialPropertyBlock();

		Metric asciiEm = SearchMetric("M");
		Metric nonasciiEm = SearchMetric("\u004d");

		mSize = size;
		mAlign = align;
		mVerticalAlign = verticalAlign;
		mWidth = width;
		mHeight = height;
		mLetterSpacing = letterSpacing * mSize;
		mAsciiSpaceAdvance = mLetterSpacing + (asciiEm == null ?
			1 : asciiEm.advance) * spaceAdvance * mSize;
		mNonAsciiSpaceAdvance = mLetterSpacing + (nonasciiEm == null ?
			1 : nonasciiEm.advance) * spaceAdvance * mSize;
		mTabSpacing = mAsciiSpaceAdvance * tabSpacing;
		mLineSpacing = lineSpacing * mSize;
		mLeftMargin = leftMargin * mSize;
		mRightMargin = rightMargin * mSize;
		mEmpty = true;
	}

	~Renderer()
	{
		ResourceCache cache = ResourceCache.SharedInstance();
		cache.UnloadTexture(mData.textureName);
		cache.UnloadData(mName);
	}

	public class compFirst : IComparer<Metric>
	{
		public int Compare(Metric a, Metric b)
		{
			return a.first.CompareTo(b.first);
		}
	}

	public class compSecond : IComparer<Metric>
	{
		public int Compare(Metric a, Metric b)
		{
			return a.second.CompareTo(b.second);
		}
	}

	protected virtual Metric SearchMetric(string c)
	{
		Metric[] metrics = mData.metrics;
		byte[] b = Encoding.Unicode.GetBytes(c);
		byte first = b[1];
		byte second = b[0];
		short index = mData.indecies[first];

		int offset = index + second;
		if (offset < 0) {
			// not found
			return null;
		}
		if (offset >= mData.header.metricCount)
			offset = mData.header.metricCount - 1;

		Metric m = new Metric();
		if (first != metrics[offset].first) {
			if (index < 0)
				index = 0;
			m.first = first;
			offset = Array.BinarySearch(metrics,
				index, offset - index + 1, m, new compFirst());
			if (offset < 0 || first != metrics[offset].first) {
				// not found
				return null;
			}
		}

		if (second != metrics[offset].second) {
			int left = offset - metrics[offset].prevNum;
			int right = offset + metrics[offset].nextNum;
			m.second = second;
			offset = Array.BinarySearch(metrics,
				left, right - left + 1, m, new compSecond());
		}

		if (offset < 0 || metrics[offset].second != second) {
			// not found
			return null;
		}

		return metrics[offset];
	}

	public virtual bool SetText(string text, Color color)
	{
		Color[] colors = new Color[text.Length];
		for (int i = 0; i < text.Length; ++i)
			colors[i] = color;
		return SetText(text, colors);
	}

	public virtual bool SetText(string text, Color[] colors)
	{
		bool result = true;
		if (text == null || text.Length == 0) {
			mEmpty = true;
			mMesh.Clear();
			return result;
		}

		mEmpty = false;
		int chars = text.Length;
		Vector3[] vertices = new Vector3[chars * 4];
		Vector2[] uv = new Vector2[chars * 4];
		int[] triangles = new int[chars * 6];
		Color[] vertexColors = new Color[chars * 4];
		float scale = mSize / (float)mData.header.fontSize;
		float x = mLeftMargin;
		float y = -(float)mData.header.fontAscent * scale;
		float sheetWidth = (float)mData.header.sheetWidth;
		float sheetHeight = (float)mData.header.sheetHeight;
		int lastAscii = -1;
		int lastIndex = -1;
		float left = mWidth;
		float right = 0;
		float top = mHeight;
		float bottom = 0;

		for (int i = 0; i < text.Length; ++i) {
			string c = text.Substring(i, 1);

			if (c.CompareTo("\n") == 0) {
				// LINEFEED
				x = mLeftMargin;
				y -= mLineSpacing;
				lastAscii = -1;
				continue;
			} else if (c.CompareTo(" ") == 0) {
				// SPACE
				x += mAsciiSpaceAdvance;
				lastAscii = -1;
				continue;
			} else if (c.CompareTo("\t") == 0) {
				// TAB
				x += mTabSpacing;
				lastAscii = -1;
				continue;
			} else if (c.CompareTo("\u3000") == 0) {
				// JIS X 0208 SPACE
				x += mNonAsciiSpaceAdvance;
				lastAscii = -1;
				continue;
			}

			if ((c.CompareTo("A") >= 0 && c.CompareTo("Z") <= 0) ||
					(c.CompareTo("a") >= 0 && c.CompareTo("z") <= 0)) {
				// ASCII
				if (lastAscii == -1) {
					// Save index for Auto linefeed
					lastAscii = i;
				}
			} else {
				// non-ASCII
				lastAscii = -1;
			}

			Metric metric = SearchMetric(c);
			if (metric == null) {
				// not found
				result = false;
				continue;
			}

			float advance = metric.advance * mSize + mLetterSpacing;

			float px = x + advance;
			if (mWidth != 0 && px > mWidth - mRightMargin) {
				// Auto linefeed.
				int index = lastAscii;
				lastAscii = -1;
				x = mLeftMargin;
				y -= mLineSpacing;
				if (index != -1 && (
						(c.CompareTo("A") >= 0 && c.CompareTo("Z") <= 0) ||
						(c.CompareTo("a") >= 0 && c.CompareTo("z") <= 0))) {
					// ASCII
					int nextIndex = index - 1;
					if (lastIndex != nextIndex) {
						i = nextIndex;
						lastIndex = i;
						continue;
					}
				}
			}

			float x0 = x + (float)metric.bearingX * scale;
			float x1 = x0 + (float)metric.width * scale;
			float y0 = y + (float)metric.bearingY * scale;
			float y1 = y0 - (float)metric.height * scale;

			if (left > x0)
				left = x0;
			if (right < x1)
				right = x1;
			if (top > y0)
				top = y0;
			if (bottom < y1)
				bottom = y1;

			x += advance;

			float w = 2.0f * sheetWidth;
			float u0 = (float)(2 * metric.u + 1) / w;
			float u1 = u0 + (float)(metric.width * 2 - 2) / w;
			float h = 2.0f * sheetHeight;
			float v0 = (float)(2 * (sheetHeight - metric.v) + 1) / h;
			float v1 = (v0 - (float)(metric.height * 2 + 2) / h);

			int vertexOffset = i * 4;
			vertices[vertexOffset + 0] = new Vector3(x1, y0, 0);
			vertices[vertexOffset + 1] = new Vector3(x1, y1, 0);
			vertices[vertexOffset + 2] = new Vector3(x0, y0, 0);
			vertices[vertexOffset + 3] = new Vector3(x0, y1, 0);

			uv[vertexOffset + 0] = new Vector2(u1, v0);
			uv[vertexOffset + 1] = new Vector2(u1, v1);
			uv[vertexOffset + 2] = new Vector2(u0, v0);
			uv[vertexOffset + 3] = new Vector2(u0, v1);

			int triangleOffset = i * 6;
			triangles[triangleOffset + 0] = 0 + vertexOffset;
			triangles[triangleOffset + 1] = 1 + vertexOffset;
			triangles[triangleOffset + 2] = 2 + vertexOffset;
			triangles[triangleOffset + 3] = 2 + vertexOffset;
			triangles[triangleOffset + 4] = 1 + vertexOffset;
			triangles[triangleOffset + 5] = 3 + vertexOffset;

			for (int n = 0; n < 4; ++n)
				vertexColors[vertexOffset + n] = colors[i];
		}

		if (mWidth != 0 && mAlign != Align.LEFT) {
			float tw = right - left;
			float offset;
			if (mAlign == Align.CENTER) {
				offset = (mWidth - mLeftMargin - mRightMargin - tw) / 2.0f;
			} else {
				// Align.RIGHT
				offset = mWidth - mRightMargin - tw;
			}

			for (int i = 0; i < vertices.Length; ++i)
				vertices[i].x += offset;
		}

		if (mHeight != 0 && mVerticalAlign != VerticalAlign.TOP) {
			float th = bottom - top;
			float offset;
			if (mVerticalAlign == VerticalAlign.MIDDLE) {
				offset = (mHeight - th) / 2.0f;
			} else {
				// VerticalAlign.BOTTOM
				offset = mHeight - th;
			}

			for (int i = 0; i < vertices.Length; ++i)
				vertices[i].y -= offset;
		}

		mMesh.Clear();
		mMesh.vertices = vertices;
		mMesh.uv = uv;
		mMesh.triangles = triangles;
		mMesh.colors = vertexColors;
		mMesh.RecalculateNormals();
		mMesh.RecalculateBounds();
		mMesh.Optimize();
		return result;
	}

	public virtual void Render(Matrix4x4 matrix, Camera camera = null)
	{
		if (mEmpty)
			return;
		Graphics.DrawMesh(mMesh, matrix, mMaterial, 0, camera);
	}
}

}	// namespace BitmapFont
