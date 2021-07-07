using System;
using UnityEngine;

public enum BufferType
{
	Velocity,
	Atmosphere,
}

[Serializable]
public class DebugInfo
{
	private static readonly int DebugFontMargin = 20;

    private static int TotalFlameCount = 0;
	
	private float _DeltaTime;
	private RenderTexture _DebugZYTexture;
	private RenderTexture _DebugZXTexture;

	public bool Enabled;
	public int DebugViewRatio = 2;
	public BufferType DebugBufferType;
	[Range(0, 127)]
	public int DebugSizeIndex = 32;
	public bool IsZYField;
	public bool UseFluid;
	public Vector3 PickPosition;

	public Vector4[] DebugBufferData = new Vector4[2];
	public ComputeBuffer ShaderDebugBuffer;

	public void Initialize(int width, int height, int depth, float deltaTime)
    {
		this._DebugZYTexture = this.CreateDebug2DTexture(depth, height);
		this._DebugZXTexture = this.CreateDebug2DTexture(depth, width);
        this._DeltaTime = deltaTime;

        this.ShaderDebugBuffer = new ComputeBuffer(8, sizeof(float), ComputeBufferType.Default);
	}

	public void Clean()
    {
		this._DebugZYTexture.Release();
		this._DebugZXTexture.Release();

		this.ShaderDebugBuffer.Release();
	}

	public RenderTexture GetRenderTexture() => this.IsZYField ? this._DebugZYTexture : this._DebugZXTexture;

	public void DrawDebugInfo(int width, int height, int depth)
	{
		TotalFlameCount++;

		if (!this.Enabled)
        {
			return;
        }

		this.PickPosition = this.CalcPickPosition(new Vector3Int(width, height, depth));
		this.ShaderDebugBuffer.GetData(this.DebugBufferData);

		var r00 = new Rect(0, 0, depth, (this.IsZYField ? height : width));
		GUI.DrawTexture(r00, this.GetRenderTexture());
		GUI.Label(r00, this.IsZYField ? "YZ Field" : "XZ Field");
		GUI.Label(new Rect(depth - 100, 0, 100, DebugFontMargin), $"T {String.Format("{0, 10:0000000000}", this._DeltaTime * TotalFlameCount)}");
		var r01 = new Rect(0, 0, depth + DebugFontMargin * 3, (this.IsZYField ? height : width) + DebugFontMargin);
		GUI.Box(r01, "");

		this.DrawHorizontalAxisValues(width, height, depth);
		this.DrawVertexAxisValues(width, height, depth);
	}

    private void DrawVertexAxisValues(int width, int height, int depth)
    {
		//	ZX平面時はスキップ
		if (!this.IsZYField)
        {
			return;
        }

		var split = 5;
		for (var i = 0; i <= split; i++)
		{
			var y = this.IsZYField ? (height / split * i) - DebugFontMargin : (width / split * i) - DebugFontMargin;
			var rec = new Rect(depth, y, depth + DebugFontMargin, DebugFontMargin);
			var pressure = 1000 / split * i;
			GUI.Label(rec, $"{pressure} hPa");
		}
	}

	private void DrawHorizontalAxisValues(int width, int height, int depth)
    {
		var split = 6;
		for (var i = 0; i <= split; i++)
		{
			var rec = new Rect((float)(depth / split * i), this.IsZYField ? height : width, depth, DebugFontMargin);
			var latitude = -90 + 180 / split * i;
			GUI.Label(rec, latitude.ToString());
		}
	}

	/// <summary>
	/// マウス選択したデバッグテクスチャ座標を返します。
	/// </summary>
	private Vector3Int CalcPickPosition(Vector3Int gridSize)
    {
		var debugViewHeight = this.IsZYField ? gridSize.y : gridSize.x;
		var input = new Vector3Int(
			Math.Max(0, (int)Input.mousePosition.x / this.DebugViewRatio), 
			Math.Max(0, ((int)Input.mousePosition.y - (Screen.height - debugViewHeight)) / this.DebugViewRatio), 
			Math.Max(0, (int)Input.mousePosition.z / this.DebugViewRatio));
		
		return this.IsZYField
			? new Vector3Int(DebugSizeIndex,　Math.Min((int)input.y, this._DebugZYTexture.height - 1), Math.Min((int)input.x, this._DebugZYTexture.width - 1))
			: new Vector3Int(Math.Min((int)input.y, this._DebugZYTexture.height - 1), DebugSizeIndex, Math.Min((int)input.x, this._DebugZYTexture.width - 1));
	}

	private RenderTexture CreateDebug2DTexture(int x, int y)
	{
		var rt = new RenderTexture(x, y, 0, RenderTextureFormat.ARGBHalf);

		rt.filterMode = FilterMode.Bilinear;
		rt.wrapMode = TextureWrapMode.Clamp;
		rt.hideFlags = HideFlags.DontSave;
		rt.enableRandomWrite = true;
		rt.Create();

		return rt;
	}
}
