/*
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using UnityEngine;

public class SampleBitmapFontText : MonoBehaviour
{
	public string text;
	public int size;
	public int width;
	public BitmapFont.Renderer.Align align;
	public Color color;
	public string font;
	BitmapFont.Renderer mRenderer;

	void Start()
	{
		/*
		 * Create BitmapFont.Renderer instance.
		 */
		mRenderer = new BitmapFont.Renderer(
			"BitmapFont/" + font, size, width, 0, align);
		mRenderer.SetText(text, color);

		/*
		 * Set the Mesh to MeshFilter and set the Material to MeshRenderer.
		 */
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = mRenderer.mesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = mRenderer.material;
	}

	void OnDestroy()
	{
		mRenderer.Destruct();
	}
}
