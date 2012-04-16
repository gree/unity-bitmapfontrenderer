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

using UnityEngine;
using System;
using System.Collections.Generic;

using DataLoader = System.Func<string, byte[]>;
using TextureLoader = System.Func<string, UnityEngine.Texture2D>;

using DataItem = BitmapFont.CacheItem<BitmapFont.Data>;
using TextureItem = BitmapFont.CacheItem<UnityEngine.Material>;

using DataCache = System.Collections.Generic.Dictionary<
	string, BitmapFont.CacheItem<BitmapFont.Data>>;
using TextureCache = System.Collections.Generic.Dictionary<
	string, BitmapFont.CacheItem<UnityEngine.Material>>;
using ShaderCache = System.Collections.Generic.Dictionary<
	string, UnityEngine.Shader>;

namespace BitmapFont {

public class CacheItem<Type>
{
	private Type m_entity;
	private int m_refCount;

	public CacheItem(Type entity) {
		m_entity = entity;
		m_refCount = 0;
	}
	public int Ref() {return ++m_refCount;}
	public int Unref() {return --m_refCount;}
	public Type Entity() {return m_entity;}
}

public class ResourceCache
{
	private static ResourceCache s_instance;
	private DataLoader m_dataLoader;
	private TextureLoader m_textureLoader;
	private DataCache m_dataCache;
	private TextureCache m_textureCache;
	private ShaderCache m_shaderCache;

	public static ResourceCache SharedInstance()
	{
		if (s_instance == null)
			s_instance = new ResourceCache();
		return s_instance;
	}

	private ResourceCache()
	{
		m_dataCache = new DataCache();
		m_textureCache = new TextureCache();
		m_shaderCache = new ShaderCache();
		SetLoader();
	}

	public void SetLoader(DataLoader dataLoader = null,
		TextureLoader textureLoader = null)
	{
		m_dataLoader = dataLoader;
		m_textureLoader = textureLoader;

		if (m_dataLoader == null) {
			m_dataLoader = (name) => {
				TextAsset asset = (TextAsset)Resources.Load(name);
				return asset.bytes;
			};
		}

		if (m_textureLoader == null) {
			m_textureLoader = (name) => {
				return (Texture2D)Resources.Load(name);
			};
		}
	}

	public Data LoadData(string name)
	{
		DataItem item;
		if (!m_dataCache.TryGetValue(name, out item)) {
			Data data = new Data(m_dataLoader(name));
			item = new DataItem(data);
			m_dataCache[name] = item;
		}
		item.Ref();
		return item.Entity();
	}

	public void UnloadData(string name)
	{
		DataItem item;
		if (m_dataCache.TryGetValue(name, out item)) {
			if (item.Unref() <= 0)
				m_dataCache.Remove(name);
		}
	}

	public Material LoadTexture(string name)
	{
		TextureItem item;
		if (!m_textureCache.TryGetValue(name, out item)) {
			Shader shader = GetShader("BitmapFont");
			Material material = new Material(shader);
			material.mainTexture = m_textureLoader(name);
			material.color = new UnityEngine.Color(1, 1, 1, 1);
			item = new TextureItem(material);
			m_textureCache[name] = item;
		}
		item.Ref();
		return item.Entity();
	}

	public void UnloadTexture(string name)
	{
		TextureItem item;
		if (m_textureCache.TryGetValue(name, out item)) {
			if (item.Unref() <= 0)
				m_textureCache.Remove(name);
		}
	}

	public Shader GetShader(string name)
	{
		Shader shader;
		if (!m_shaderCache.TryGetValue(name, out shader)) {
			shader = Shader.Find(name);
			m_shaderCache[name] = shader;
		}
		return shader;
	}

	public void UnloadAll()
	{
		m_dataCache.Clear();
		m_textureCache.Clear();
		m_shaderCache.Clear();
	}
}

}	// namespace BitmapFont
