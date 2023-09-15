namespace VRCFT_Tobii_Advanced.Tobii;

internal interface IWearable : IDisposable
{
    void Subscribe();
    void Unsubscribe();
    void Update();
    EyeData GetEyeData();
}