namespace VRCFT_Tobii_Advanced.Tobii;

internal interface IWearable : IDisposable
{
    public Action<EyeData>? OnData { get; set; }
    void Subscribe();
    void Unsubscribe();
    void Update();
}