using VRCFaceTracking.Core.Types;

namespace VRCFT_Tobii_Advanced.Tobii;

public struct EyeData
{
    public struct Eye
    {
        public bool GlazeDirectionIsValid;
        public Vector2 GlazeDirection;

        public bool PupilDiameterIsValid;
        public float PupilDiameterMm;

        public bool IsBlinkingIsValid;
        public bool IsBlink;
    }

    public Eye Left;
    public Eye Right;
}