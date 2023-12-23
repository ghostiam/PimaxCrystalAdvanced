namespace VRCFT_Tobii_Advanced.Tobii;

internal interface ITobiiEyeData : IDisposable
{
    public Action<EyeData>? OnData { get; set; }
    void Subscribe();
    void Unsubscribe();
    void Update();
}