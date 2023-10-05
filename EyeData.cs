using VRCFaceTracking.Core.Types;

namespace VRCFT_Tobii_Advanced;

public struct EyeData
{
    public struct Eye
    {
        public bool GlazeDirectionIsValid;
        public Vector2 GlazeDirection;

        public bool PupilDiameterIsValid;
        public float PupilDiameterMm;

        public bool OpennessIsValid;
        public float Openness;
    }

    public Eye Left;
    public Eye Right;
}