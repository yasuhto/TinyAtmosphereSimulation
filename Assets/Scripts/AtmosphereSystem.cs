using UnityEngine;

public class AtmosphereSystem : MonoBehaviour
{
    private struct KernelDefine
    {
        public int CalcDynamicProcessId;
        public int CalcPhysicalProcessId;

        public int ApplyAdvectionId;
        public int ApplyAdvectionVelocityId;
        public int CalcVorticityId;
        public int ApplyConfinementId;
        public int CalcDivergenceId;
        public int CalcPressureId;
        public int UpdateVelocityId;
    }

    private struct PropertyDefine
    {
        public int SizeId;
        public int GridSizeId;
        public int InverseGridSizeId;
        public int ScaleId;
        public int TranslateId;
        public int DeltaTimeId;
        public int DissipatesId;
        public int ForwardId;
        public int EpsilonId;
        public int GForcesId;
        public int RotationRateId;

        public int UpdateBufferId;
        public int SourceBufferId;
        public int SourceAtmosphereId;
        public int SourceObstaclesId;
        public int SourceVelocityId;
        public int SourceVorticityId;
        public int SourceDivergenceId;
        public int SourcePressureId;
    }

    private KernelDefine _KernelDefine = default;
    private PropertyDefine _PropertyDefine = default;

    private uint KernelThreadSize_X;
    private uint KernelThreadSize_Y;
    private uint KernelThreadSize_Z;

    public DebugInfo DebugInfo;
    public WorldModel WorldModel;

    public int Width = 128;
    public int Height = 128;
    public int Depth = 128;

    public int Iterations = 10;
    public float VorticityStrength = 1.0f;
    public float DensityDissipation = 0.999f;
    public float TemperatureDissipation = 0.995f;
    public float VelocityDissipation = 0.995f;

    public ComputeShader Atmosphere;
    public ComputeShader Obstacles;
    public ComputeShader Fluid;

    Vector4 TextureSize;
    DoubleRenderTexture VelocityTextrue;
    DoubleRenderTexture VorticityTexture;   //	HACK:2枚も必要ないけど流用
    DoubleRenderTexture DivergenceTexture;  //	HACK:2枚も必要ないけど流用
    DoubleRenderTexture PressureTexture;

    /// <summary> x = 温度、yzw =（予備）</summary>
    DoubleRenderTexture AtmosphereTexture;
    DoubleRenderTexture ObstacleTexture;    //	HACK:2枚も必要ないけど流用

    private void Start()
    {
        this.Initialize();
        this.GetShaderPropertyIds();

        this.SetupWorld();

        this.DebugInfo.Initialize(this.Width, this.Height, this.Depth, this.WorldModel.DeltaTime);
    }

    private void Update()
    {
        this.UpdateModel();
        this.UpdateView();
    }

    private void OnDestroy()
    {
        this.DebugInfo.Clean();
        this.ReleaseBuffers();
    }

    private void OnGUI()
    {
        this.DebugInfo.DrawDebugInfo(this.Width * this.DebugInfo.DebugViewRatio, this.Height * this.DebugInfo.DebugViewRatio, this.Depth * this.DebugInfo.DebugViewRatio);
    }

    private void UpdateModel()
    {
        this.UpdateAtmosphereModel();
        if (DebugInfo.UseFluid) this.UpdateFluidModel();
    }

    private void UpdateView()
    {
        transform.rotation = Quaternion.identity;

        GetComponent<Renderer>().material.SetVector(this._PropertyDefine.TranslateId, transform.localPosition);
        GetComponent<Renderer>().material.SetVector(this._PropertyDefine.ScaleId, transform.localScale);
        GetComponent<Renderer>().material.SetTexture(this._PropertyDefine.SourceAtmosphereId, this.AtmosphereTexture.Active);
        GetComponent<Renderer>().material.SetVector(this._PropertyDefine.SizeId, TextureSize);
    }

    private void Initialize()
    {
        Width = Mathf.ClosestPowerOfTwo(Width);
        Height = Mathf.ClosestPowerOfTwo(Height);
        Depth = Mathf.ClosestPowerOfTwo(Depth);

        //	オブジェクトのスケールを合わせる
        transform.localScale = new Vector3(Width, Height, Depth);
        transform.localPosition = new Vector3(0, Height / 2, 0);

        TextureSize = new Vector4(Width, Height, Depth, 0.0f);

        this.CreateBuffers();
    }

    private void GetShaderPropertyIds()
    {
        this.Fluid.GetKernelThreadGroupSizes(0, out KernelThreadSize_X, out KernelThreadSize_Y, out KernelThreadSize_Z);

        this._KernelDefine.CalcDynamicProcessId = this.Atmosphere.FindKernel("CalcDynamicProcess");
        this._KernelDefine.CalcPhysicalProcessId = this.Atmosphere.FindKernel("CalcPhysicalProcess");

        this._KernelDefine.ApplyAdvectionId = this.Fluid.FindKernel("ApplyAdvection");
        this._KernelDefine.ApplyAdvectionVelocityId = this.Fluid.FindKernel("ApplyAdvectionVelocity");
        this._KernelDefine.CalcVorticityId = this.Fluid.FindKernel("CalcVorticity");
        this._KernelDefine.ApplyConfinementId = this.Fluid.FindKernel("ApplyConfinement");
        this._KernelDefine.CalcDivergenceId = this.Fluid.FindKernel("CalcDivergence");
        this._KernelDefine.CalcPressureId = this.Fluid.FindKernel("CalcPressure");
        this._KernelDefine.UpdateVelocityId = this.Fluid.FindKernel("UpdateVelocity");

        this._PropertyDefine.SizeId = Shader.PropertyToID("_Size");
        this._PropertyDefine.GridSizeId = Shader.PropertyToID("_GridSize");
        this._PropertyDefine.InverseGridSizeId = Shader.PropertyToID("_InverseGridSize");
        this._PropertyDefine.ScaleId = Shader.PropertyToID("_Scale");
        this._PropertyDefine.TranslateId = Shader.PropertyToID("_Translate");
        this._PropertyDefine.DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        this._PropertyDefine.DissipatesId = Shader.PropertyToID("_Dissipate");
        this._PropertyDefine.ForwardId = Shader.PropertyToID("_Forward");
        this._PropertyDefine.EpsilonId = Shader.PropertyToID("_Epsilon");

        this._PropertyDefine.GForcesId = Shader.PropertyToID("_GForces");
        this._PropertyDefine.RotationRateId = Shader.PropertyToID("_AngularVelocityOfRotation");

        this._PropertyDefine.SourceBufferId = Shader.PropertyToID("_Read");
        this._PropertyDefine.UpdateBufferId = Shader.PropertyToID("_Write");

        this._PropertyDefine.SourceObstaclesId = Shader.PropertyToID("_Obstacles");
        this._PropertyDefine.SourceAtmosphereId = Shader.PropertyToID("_Atmosphere");

        this._PropertyDefine.SourceVelocityId = Shader.PropertyToID("_Velocity");
        this._PropertyDefine.SourceVorticityId = Shader.PropertyToID("_Vorticity");
        this._PropertyDefine.SourceDivergenceId = Shader.PropertyToID("_Divergence");
        this._PropertyDefine.SourcePressureId = Shader.PropertyToID("_Pressure");
    }

    private void CreateBuffers()
    {
        this.AtmosphereTexture = new DoubleRenderTexture(Width, Height, Depth);
        this.ObstacleTexture = new DoubleRenderTexture(Width, Height, Depth);
        this.VelocityTextrue = new DoubleRenderTexture(Width, Height, Depth);
        this.VorticityTexture = new DoubleRenderTexture(Width, Height, Depth);
        this.DivergenceTexture = new DoubleRenderTexture(Width, Height, Depth);
        this.PressureTexture = new DoubleRenderTexture(Width, Height, Depth);
    }

    private void ReleaseBuffers()
    {
        this.AtmosphereTexture.Release();
        this.ObstacleTexture.Release();
        this.VelocityTextrue.Release();
        this.VorticityTexture.Release();
        this.DivergenceTexture.Release();
        this.PressureTexture.Release();
    }

    private void UpdateAtmosphereModel()
    {
        this.AtmosphereShaderSetup();

        this.CalcDynamicProcess();
        this.CalcPhysicalProcess();
    }

    private void UpdateFluidModel()
    {
        this.FluidShaderSetup();

        this.ApplyAdvection();
        this.ApplyAdvectionVelocity();
        this.CalcDivergence();
        this.CalcPressure();
        this.UpdateVelocity();
    }

    private void AtmosphereShaderSetup()
    {
        this.Atmosphere.SetFloat(this._PropertyDefine.DeltaTimeId, this.WorldModel.DeltaTime);
        this.Atmosphere.SetVector(this._PropertyDefine.SizeId, this.TextureSize);
        this.Atmosphere.SetVector(this._PropertyDefine.GridSizeId, this.WorldModel.GridSize);
        this.Atmosphere.SetVector(this._PropertyDefine.InverseGridSizeId, new Vector3(1f / this.WorldModel.GridSize.x, 1f / this.WorldModel.GridSize.y, 1f / this.WorldModel.GridSize.z));
        this.Atmosphere.SetFloat(this._PropertyDefine.GForcesId, this.WorldModel.GForces);
        this.Atmosphere.SetFloat(this._PropertyDefine.RotationRateId, this.WorldModel.RotationRate);

        this.Atmosphere.SetInt("_DebugBufferType", (int)this.DebugInfo.DebugBufferType);
        this.Atmosphere.SetInt("_DebugSizeIndex", this.DebugInfo.DebugSizeIndex);
        this.Atmosphere.SetBool("_DebugIsZYField", this.DebugInfo.IsZYField);
        this.Atmosphere.SetVector("_DebugPick", this.DebugInfo.PickPosition);
    }

    private void CalcDynamicProcess()
    {
        this.Atmosphere.SetTexture(this._KernelDefine.CalcDynamicProcessId, this._PropertyDefine.UpdateBufferId, this.VelocityTextrue.Inactive);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcDynamicProcessId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcDynamicProcessId, this._PropertyDefine.SourceAtmosphereId, this.AtmosphereTexture.Active);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcDynamicProcessId, "_DebugTexture", this.DebugInfo.GetRenderTexture());
        this.Atmosphere.SetBuffer(this._KernelDefine.CalcDynamicProcessId, "_DebugBuffer", this.DebugInfo.ShaderDebugBuffer);

        this.Atmosphere.Dispatch(this._KernelDefine.CalcDynamicProcessId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.VelocityTextrue.Swap();
    }

    private void CalcPhysicalProcess()
    {
        this.Atmosphere.SetTexture(this._KernelDefine.CalcPhysicalProcessId, this._PropertyDefine.SourceAtmosphereId, this.AtmosphereTexture.Active);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcPhysicalProcessId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcPhysicalProcessId, this._PropertyDefine.UpdateBufferId, this.AtmosphereTexture.Inactive);
        this.Atmosphere.SetTexture(this._KernelDefine.CalcPhysicalProcessId, "_DebugTexture", this.DebugInfo.GetRenderTexture());
        this.Atmosphere.SetBuffer(this._KernelDefine.CalcPhysicalProcessId, "_DebugBuffer", this.DebugInfo.ShaderDebugBuffer);

        this.Atmosphere.Dispatch(this._KernelDefine.CalcPhysicalProcessId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.AtmosphereTexture.Swap();
    }

    private void FluidShaderSetup()
    {
        this.Fluid.SetFloat(this._PropertyDefine.DeltaTimeId, this.WorldModel.DeltaTime);
        this.Fluid.SetFloat(this._PropertyDefine.ForwardId, 1.0f);
        this.Fluid.SetFloat(this._PropertyDefine.EpsilonId, VorticityStrength);
        this.Fluid.SetVector(this._PropertyDefine.SizeId, this.TextureSize);
        this.Fluid.SetVector(this._PropertyDefine.GridSizeId, this.WorldModel.GridSize);
        this.Fluid.SetVector(this._PropertyDefine.InverseGridSizeId, new Vector3(1f / this.WorldModel.GridSize.x, 1f / this.WorldModel.GridSize.y, 1f / this.WorldModel.GridSize.z));
        this.Fluid.SetVector(this._PropertyDefine.DissipatesId, new Vector4(this.TemperatureDissipation, this.DensityDissipation, 0f, 0f));
    }

    private void SetupWorld()
    {
        this.Atmosphere.SetVector(this._PropertyDefine.SizeId, TextureSize);
        this.Atmosphere.SetTexture(0, this._PropertyDefine.UpdateBufferId, this.AtmosphereTexture.Active);
        this.Atmosphere.Dispatch(0, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        Obstacles.SetVector(this._PropertyDefine.SizeId, TextureSize);
        Obstacles.SetTexture(0, this._PropertyDefine.UpdateBufferId, this.ObstacleTexture.Active);
        Obstacles.Dispatch(0, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);
    }

    private void ApplyAdvection()
    {
        Fluid.SetTexture(this._KernelDefine.ApplyAdvectionId, this._PropertyDefine.SourceBufferId, this.AtmosphereTexture.Active);
        Fluid.SetTexture(this._KernelDefine.ApplyAdvectionId, this._PropertyDefine.UpdateBufferId, this.AtmosphereTexture.Inactive);
        Fluid.SetTexture(this._KernelDefine.ApplyAdvectionId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);
        Fluid.SetTexture(this._KernelDefine.ApplyAdvectionId, this._PropertyDefine.SourceObstaclesId, this.ObstacleTexture.Active);

        Fluid.Dispatch(this._KernelDefine.ApplyAdvectionId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.AtmosphereTexture.Swap();
    }

    private void ApplyAdvectionVelocity()
    {
        this.Fluid.SetVector(this._PropertyDefine.DissipatesId, new Vector4(VelocityDissipation, VelocityDissipation, VelocityDissipation, 0f));

        this.Fluid.SetTexture(this._KernelDefine.ApplyAdvectionVelocityId, this._PropertyDefine.SourceBufferId, this.VelocityTextrue.Active);
        this.Fluid.SetTexture(this._KernelDefine.ApplyAdvectionVelocityId, this._PropertyDefine.UpdateBufferId, this.VelocityTextrue.Inactive);
        this.Fluid.SetTexture(this._KernelDefine.ApplyAdvectionVelocityId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);
        this.Fluid.SetTexture(this._KernelDefine.ApplyAdvectionVelocityId, this._PropertyDefine.SourceObstaclesId, this.ObstacleTexture.Active);

        this.Fluid.Dispatch(this._KernelDefine.ApplyAdvectionVelocityId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.VelocityTextrue.Swap();
    }

    private void CalcVorticity()
    {
        this.Fluid.SetTexture(this._KernelDefine.CalcVorticityId, this._PropertyDefine.UpdateBufferId, this.VorticityTexture.Active);
        this.Fluid.SetTexture(this._KernelDefine.CalcVorticityId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);

        this.Fluid.Dispatch(this._KernelDefine.CalcVorticityId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);
    }

    private void ApplyConfinementVelocity()
    {
        this.Fluid.SetTexture(this._KernelDefine.ApplyConfinementId, this._PropertyDefine.UpdateBufferId, this.VelocityTextrue.Inactive);
        this.Fluid.SetTexture(this._KernelDefine.ApplyConfinementId, this._PropertyDefine.SourceBufferId, this.VelocityTextrue.Active);
        this.Fluid.SetTexture(this._KernelDefine.ApplyConfinementId, this._PropertyDefine.SourceVorticityId, this.VorticityTexture.Active);

        this.Fluid.Dispatch(this._KernelDefine.ApplyConfinementId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.VelocityTextrue.Swap();
    }

    private void CalcDivergence()
    {
        this.Fluid.SetTexture(this._KernelDefine.CalcDivergenceId, this._PropertyDefine.UpdateBufferId, this.DivergenceTexture.Active);
        this.Fluid.SetTexture(this._KernelDefine.CalcDivergenceId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);
        this.Fluid.SetTexture(this._KernelDefine.CalcDivergenceId, this._PropertyDefine.SourceObstaclesId, this.ObstacleTexture.Active);

        this.Fluid.Dispatch(this._KernelDefine.CalcDivergenceId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);
    }

    private void CalcPressure()
    {
        this.Fluid.SetTexture(this._KernelDefine.CalcPressureId, this._PropertyDefine.SourceDivergenceId, this.DivergenceTexture.Active);
        this.Fluid.SetTexture(this._KernelDefine.CalcPressureId, this._PropertyDefine.SourceObstaclesId, this.ObstacleTexture.Active);

        for (int i = 0; i < this.Iterations; i++)
        {
            this.Fluid.SetTexture(this._KernelDefine.CalcPressureId, this._PropertyDefine.UpdateBufferId, this.PressureTexture.Inactive);
            this.Fluid.SetTexture(this._KernelDefine.CalcPressureId, this._PropertyDefine.SourcePressureId, this.PressureTexture.Active);

            this.Fluid.Dispatch(this._KernelDefine.CalcPressureId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

            this.PressureTexture.Swap();
        }
    }

    private void UpdateVelocity()
    {
        this.Fluid.SetTexture(this._KernelDefine.UpdateVelocityId, this._PropertyDefine.UpdateBufferId, this.VelocityTextrue.Inactive);
        this.Fluid.SetTexture(this._KernelDefine.UpdateVelocityId, this._PropertyDefine.SourceObstaclesId, this.ObstacleTexture.Active);
        this.Fluid.SetTexture(this._KernelDefine.UpdateVelocityId, this._PropertyDefine.SourcePressureId, this.PressureTexture.Active);
        this.Fluid.SetTexture(this._KernelDefine.UpdateVelocityId, this._PropertyDefine.SourceVelocityId, this.VelocityTextrue.Active);

        this.Fluid.Dispatch(this._KernelDefine.UpdateVelocityId, (int)this.TextureSize.x / (int)this.KernelThreadSize_X, (int)this.TextureSize.y / (int)this.KernelThreadSize_Y, (int)this.TextureSize.z / (int)this.KernelThreadSize_Z);

        this.VelocityTextrue.Swap();
    }
}
