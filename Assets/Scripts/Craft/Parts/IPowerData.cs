namespace ModApi.Craft.Parts
{
    using UnityEngine;

    public interface IPowerData
    {
        float InputVolt { get; }

        float MaxAmpere { get; }

        float Resistance { get; }
    }
}